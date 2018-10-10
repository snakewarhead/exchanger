using exchanger.Util;
using System.Windows.Forms;
using exchanger.Model.Exchange;
using exchanger.Model.VO;
using System.Collections.Generic;
using exchanger.Model;
using System.Threading;

namespace exchanger.Controller
{
    public class ExchangeController
    {
        private exchanger.View.exchanger mainView;
        private ExchangeManager exchangeManager;

        private Configuration config;

        public ExchangeController()
        {
            config = Configuration.Load();
        }

        public void Start()
        {
            if (mainView == null)
            {
                // 虽然是单例，但是只能在Controller层使用
                exchangeManager = ExchangeManager.GetInstance();
                exchangeManager.Controller = this;

                mainView = new View.exchanger(this);
                mainView.FormClosed += FormClosed;

                mainView.Show();
            }
            mainView.Activate();
        }

        public void Stop()
        {

        }

        private void FormClosed(object sender, FormClosedEventArgs e)
        {
            mainView.Dispose();
            mainView = null;
            Utils.ReleaseMemory(true);

            System.Environment.Exit(0);
        }

        public Configuration GetConfigurationCopy()
        {
            return Configuration.Load();
        }

        public string[] GetExchangeRealNames()
        {
            return exchangeManager.ToArrayExchangeRealName();
        }

        public delegate void OnBalanceChanged(List<Balance> balances);
        public delegate void OnHistoryChanged(List<TradeHistory> histories);
        public delegate void OnLogAppended(string log, bool needStop);

        public event OnBalanceChanged BalanceChangedEventHandler;
        public event OnHistoryChanged HistoryChangedEventHandler;
        public event OnLogAppended LogAppendedEventHandler;

        public void Performance()
        {
            exchangeManager.Startup(
                mainView.coin1,
                mainView.coin2,
                mainView.lowestBalance1,
                mainView.lowestBalance2,
                mainView.apiID,
                mainView.secret,
                mainView.idxExchange
            );

            exchangeManager.Performance();
        }

        public void CurtainCall()
        {
            exchangeManager.CurtainCall();
        }

        public void NotifyBalanceChanged(List<Balance> balances)
        {
            BalanceChangedEventHandler?.Invoke(balances);
        }

        public void NotifyHistoryChanged(List<TradeHistory> histories)
        {
            HistoryChangedEventHandler?.Invoke(histories);
        }

        public void NotifyLogAppended(string log, bool needStop = false)
        {
            LogAppendedEventHandler?.Invoke(log, needStop);
        }
    }
}
