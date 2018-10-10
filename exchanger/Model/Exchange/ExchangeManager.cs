using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using exchanger.Controller;
using exchanger.Model.Exchange.Impl;
using exchanger.Model.VO;
using exchanger.Util;

namespace exchanger.Model.Exchange
{
    public sealed class ExchangeManager
    {
        private static ExchangeManager instance;

        public static ExchangeManager GetInstance()
        {
            if (instance == null)
            {
                instance = new ExchangeManager();
            }
            return instance;
        }

        private ExchangeManager()
        {
            exchangesLs.Add(new ExchangeCoinbene());
            // TODO: add new，and must be the sequence which is defined in the Idx of property of IExchange

            foreach (var item in exchangesLs)
            {
                exchangesDic.Add(item.Name, item);
            }

        }

        /// <summary>
        /// 和对应的controller绑定
        /// </summary>
        private ExchangeController controller;

        public ExchangeController Controller
        {
            set { controller = value; }
        }

        private Dictionary<string, IExchange> exchangesDic = new Dictionary<string, IExchange>();
        private List<IExchange> exchangesLs = new List<IExchange>();

        public IExchange Get(string exname)
        {
            return exchangesDic[exname];
        }

        public IExchange Get(int idx)
        {
            IExchange ex = exchangesLs[idx];
            if (ex.Idx != idx)
            {
                throw new Exception("Bad sequence in exchanges list");
            }
            return ex;
        }

        public string[] ToArrayExchangeRealName()
        {
            List<string> nms = new List<string>();
            foreach (var item in exchangesLs)
            {
                nms.Add(item.RealName);
            }
            return nms.ToArray();
        }

        /// <summary>
        /// 余额的一定比例用于挖矿
        /// </summary>
        private const decimal SIDE_RATE = 0.5m;

        // 线程中只读
        private string coin1;
        private string coin2;
        private decimal lowestBalance1;
        private decimal lowestBalance2;
        private string apiID;
        private string secret;

        private int idxExchange;
        private IExchange currentExchange;

        private bool running = false;

        // 一个线程修改，另一个线程只读，可能修改不是原子操作，但汇编指令一定是原子操作（是吗？），线程再切换的时候
        private decimal currentBalance1;
        private decimal currentBalance2;

        private decimal lastPriceBuy = -1m;

        public void Startup(string coin1, string coin2, string lowestBalance1, string lowestBalance2, string apiID, string secret, int idxExchange)
        {
            this.coin1 = coin1;
            this.coin2 = coin2;
            this.lowestBalance1 = Convert.ToDecimal(lowestBalance1);
            this.lowestBalance2 = Convert.ToDecimal(lowestBalance2);
            this.apiID = apiID;
            this.secret = secret;

            this.idxExchange = idxExchange;
            currentExchange = Get(idxExchange);

            running = true;
        }

        public void Performance()
        {
            // 查询交易记录
            Thread threadHistory = new Thread(() => QueryHistory());
            threadHistory.IsBackground = true;
            threadHistory.Start();

            // 查询余额
            Thread threadBalance = new Thread(() => QueryBalance());
            threadBalance.IsBackground = true;
            threadBalance.Start();

            // 查询价格，这样会有问题，还是单独使用安全一点
            //Thread threadPrice = new Thread(() => QueryPrice());
            //threadPrice.IsBackground = true;
            //threadPrice.Start();

            // 检查挂单队列的状态

            // 开始挖矿-买
            Thread threadMiningBuy = new Thread(() => MiningBuy());
            threadMiningBuy.IsBackground = true;
            threadMiningBuy.Start();

            // 开始挖矿-卖
            Thread threadMiningSell = new Thread(() => MiningSell());
            threadMiningSell.IsBackground = true;
            threadMiningSell.Start();
        }

        public void CurtainCall()
        {
            running = false;
        }

        public void QueryBalance()
        {
            while (running)
            {
                List<string> coins = new List<string>();
                coins.Add(coin1);
                coins.Add(coin2);

                List<Balance> balances = currentExchange.GetBalances(coins, apiID, secret);
                controller.NotifyBalanceChanged(balances);

                if (balances.Count == 2)
                {
                    currentBalance1 = balances[0].Available;
                    currentBalance2 = balances[1].Available;
                }

                Thread.Sleep(500);
            }
        }

        public void QueryHistory()
        {
            while (running)
            {
                List<TradeHistory> histories = currentExchange.GetHistory(coin1, coin2);
                controller.NotifyHistoryChanged(histories);

                Thread.Sleep(1000);
            }
        }

        //public void QueryPrice()
        //{
        //    while (running)
        //    {
        //        price = currentExchange.GetPrice(coin1, coin2);

        //        Thread.Sleep(100);
        //    }
        //}


        /// <summary>
        /// coin2的数量按coin1/coin2的价格买入coin1
        /// </summary>
        public void MiningBuy()
        {
            while (running)
            {
                // 还挂着买单的斗嘛
                if (lastPriceBuy != -1m)
                {
                    Thread.Sleep(300);
                    continue;
                }

                Price price = currentExchange.GetPrice(coin1, coin2);
                if (price != null)
                {
                    // 预测一个价格
                    decimal priceBuy = price.GetPriceBuyPredicted();
                    if (priceBuy <= 0m)
                    {
                        // 致命错误
                        logFormat("Buy", "ERROR", "买1价格是负数了，垃圾交易所", true);
                        break;
                    }

                    // 最低交易余额，需要转换为买入币种的数量
                    decimal lowestBalanceCoin1ByCoin2 = lowestBalance2 / priceBuy;
                    if (!price.IsPredictedValid(lowestBalanceCoin1ByCoin2))
                    {
                        logFormat("Buy", "ERROR", "coin2的最低交易余额配置错误，低于最低挂单金额了", true);
                        break;
                    }

                    // 使用coin2当前余额的50%来进行购买
                    decimal quantityBuy = currentBalance2 * SIDE_RATE;
                    if (quantityBuy < lowestBalance2)
                    {
                        logFormat("Buy", "ERROR", "coin2余额的50%低于最低交易余额了");
                        Thread.Sleep(100);
                        continue;
                    }
                    decimal quantityBuyConvert = quantityBuy / priceBuy;

                    string orderBuy = currentExchange.PlaceOrder(
                        coin1,
                        coin2,
                        priceBuy,
                        quantityBuyConvert,
                        SideType.BUY,
                        apiID,
                        secret
                    );
                    if (orderBuy.IsNullOrEmpty())
                    {
                        logFormat("Buy", "NORMAL", $"挂单失败，{priceBuy} - {quantityBuy}");
                        lastPriceBuy = -1m;
                    }
                    else
                    {
                        logFormat("Buy", "NORMAL", $"挂单成功，{priceBuy} - {quantityBuy}");
                        lastPriceBuy = priceBuy;
                    }
                }

                Thread.Sleep(100);
            }
        }

        private int sellPriceWait = 0;

        public void MiningSell()
        {
            while (running)
            {
                Price price = currentExchange.GetPrice(coin1, coin2);
                if (price != null)
                {
                    // 还没买挂单
                    if (lastPriceBuy == -1m)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    if (!price.IsPredictedValid(lowestBalance1))
                    {
                        logFormat("Sell", "ERROR", "coin1的最低交易余额配置错误，低于最低挂单金额了", true);
                        break;
                    }

                    decimal quantitySell = currentBalance1 * SIDE_RATE;
                    if (quantitySell < lowestBalance1)
                    {
                        logFormat("Sell", "ERROR", "coin1余额的50%低于最低交易余额了");
                        Thread.Sleep(100);
                        continue;
                    }

                    // 卖单金额设定
                    decimal priceSell = price.GetAcceptedSellPrice(lastPriceBuy);
                    if (priceSell == -1m)
                    {
                        // 说明价格已经低于可接受范围了，且之前的买单肯定是已经被其他的收了
                        sellPriceWait++;
                        if (sellPriceWait >= 10)
                        {
                            // 那么我们是在等一会儿呢，还是继续呢？
                            logFormat("Sell", "ERROR", "出售价格会低于之前的买单的价格很多了哦", true);
                            break;
                        }
                        Thread.Sleep(300);
                        continue;
                    }

                    string orderSell = currentExchange.PlaceOrder(
                        coin1,
                        coin2,
                        priceSell,
                        quantitySell,
                        SideType.SELL,
                        apiID,
                        secret
                    );

                    if (orderSell.IsNullOrEmpty())
                    {
                        logFormat("Sell", "NORMAL", $"挂单失败，{priceSell} - {quantitySell}");
                    }
                    else
                    {
                        logFormat("Sell", "NORMAL", $"挂单成功，{priceSell} - {quantitySell}");
                        // 已经购买完毕，需要重新挂买
                        lastPriceBuy = -1m;
                    }
                }

                // 检查的时间需要频繁一点
                Thread.Sleep(50);
            }
        }

        private void logFormat(string tag, string state, string log, bool needStop = false)
        {
            controller.NotifyLogAppended($"[{tag}]  [{state}]  {Utils.CurrentDatetimeShort()}  {log}", needStop);
        }
    }
}
