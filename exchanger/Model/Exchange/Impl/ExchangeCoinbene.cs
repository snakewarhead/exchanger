using exchanger.Model.VO;
using exchanger.Util;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace exchanger.Model.Exchange.Impl
{
    public class ExchangeCoinbene : IExchange
    {
        private const string BASE_URL = "http://api.coinbene.com";
        private const string TRADE_HISTORY_URL = BASE_URL + "/v1/market/trades";
        private const string BALANCE_URL = BASE_URL + "/v1/trade/balance";
        private const string PRICE_URL = BASE_URL + "/v1/market/ticker";
        private const string PLACE_ORDER_URL = BASE_URL + "/v1/trade/order/place";

        public int Idx
        {
            get
            {
                return 0;
            }
        }

        public string Name
        {
            get
            {
                return "coinbene";
            }
        }

        public string RealName
        {
            get
            {
                return "满币交易所";
            }
        }

        public List<Balance> GetBalances(List<string> coins, string apiID, string secret)
        {
            List<Balance> ls = new List<Balance>();

            try
            {
                HttpClient req = new HttpClient(BALANCE_URL);
                req.ContentType = "application/json";

                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("account", "exchange");
                data.Add("apiid", apiID);
                data.Add("timestamp", Utils.GetCurrentTimestampMilliseconds().ToString());
                data.Add("sign", GetSign(data.ToDictionary(p => p.Key, o => o.Value), apiID, secret));

                req.PostingDataJson = JsonConvert.SerializeObject(data);

                string resp = req.GetString();
                JObject jo = JsonConvert.DeserializeObject<JObject>(resp);
                JArray arr = (JArray)jo["balance"];
                Dictionary<string, Balance> all = new Dictionary<string, Balance>();
                foreach (var i in arr)
                {
                    Balance bal = new Balance();

                    bal.CoinName = i["asset"].ToString();
                    bal.Available = Convert.ToDecimal(i["available"].ToString());
                    bal.Reserved = Convert.ToDecimal(i["reserved"].ToString());
                    bal.Total = Convert.ToDecimal(i["total"].ToString());

                    all.Add(bal.CoinName, bal);
                }

                foreach (var c in coins)
                {
                    ls.Add(all[c]);
                }
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }

            return ls;
        }

        public List<TradeHistory> GetHistory(string coin1, string coin2)
        {
            List<TradeHistory> histories = new List<TradeHistory>();

            try
            {
                string pms = $"symbol={coin1}{coin2}&size=20";
                HttpClient req = new HttpClient(TRADE_HISTORY_URL + "?" + pms);
                string resp = req.GetString();

                JObject jo = JsonConvert.DeserializeObject<JObject>(resp);
                JArray arr = (JArray)jo["trades"];
                foreach (var i in arr)
                {
                    TradeHistory his = new TradeHistory();
                    his.ID = i["tradeId"].ToString();
                    his.Side = i["take"].ToString();
                    his.Price = Convert.ToDecimal(i["price"].ToString());
                    his.Quantity = Convert.ToDecimal(i["quantity"].ToString());
                    his.Time = Utils.ConvertTimestamp2DatetimeShortStr((long)i["time"]);

                    histories.Add(his);
                }
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }

            return histories;
        }

        public Price GetPrice(string coin1, string coin2)
        {
            try
            {
                string pms = $"symbol={coin1}{coin2}";
                HttpClient req = new HttpClient(PRICE_URL + "?" + pms);
                string resp = req.GetString();

                JObject jo = JsonConvert.DeserializeObject<JObject>(resp);
                JArray arr = (JArray)jo["ticker"];
                JObject prjo = (JObject)arr[0];

                Price price = new Price();
                price.Symbol = prjo["symbol"].ToString();
                price.Last = Convert.ToDecimal(prjo["last"].ToString());
                price.Buy1 = Convert.ToDecimal(prjo["bid"].ToString());
                price.Sell1 = Convert.ToDecimal(prjo["ask"].ToString());

                return price;
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }

            return null;
        }

        public string PlaceOrder(string coin1, string coin2, decimal price, decimal quantity, SideType side, string apiID, string secret)
        {
            try
            {
                HttpClient req = new HttpClient(PLACE_ORDER_URL);
                req.ContentType = "application/json";

                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("apiid", apiID);
                data.Add("price", price.ToString("#0.00"));
                data.Add("quantity", quantity.ToString("#0.00"));
                data.Add("symbol", $"{coin1}{coin2}");
                data.Add("type", side == SideType.BUY ? "buy-limit" : "sell-limit");
                data.Add("timestamp", Utils.GetCurrentTimestampMilliseconds().ToString());
                data.Add("sign", GetSign(data.ToDictionary(p => p.Key, o => o.Value), apiID, secret));

                req.PostingDataJson = JsonConvert.SerializeObject(data);

                string resp = req.GetString();
                JObject jo = JsonConvert.DeserializeObject<JObject>(resp);
                if (jo["status"].ToString() == "ok")
                {
                    return jo["orderid"].ToString();
                }
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }
            return null;
        }


        private string GetSign(Dictionary<string, string> data, string apiID, string secret)
        {
            StringBuilder source = new StringBuilder();

            if (!data.ContainsKey("apiid"))
            {
                data.Add("apiid", apiID);
            }
            data.Add("secret", secret);

            Dictionary<string, string> dataSorted = data.OrderBy(p => p.Key).ToDictionary(p => p.Key, o => o.Value);
            foreach (var item in dataSorted)
            {
                source.Append(item.Key.ToUpper());
                source.Append("=");
                source.Append(item.Value.ToUpper());
                source.Append("&");
            }
            source.Remove(source.Length - 1, 1);

            return Utils.HashByMD5(source.ToString());
        }

    }
}
