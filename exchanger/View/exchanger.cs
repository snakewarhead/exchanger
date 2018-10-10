using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using exchanger.Controller;
using exchanger.Model;
using exchanger.Model.VO;
using exchanger.Util;

namespace exchanger.View
{
    public partial class exchanger : Form
    {
        private ExchangeController exchangeController;
        private SynchronizationContext syncContext;

        public exchanger(ExchangeController exchangeController)
        {
            InitializeComponent();
            syncContext = SynchronizationContext.Current;

            this.exchangeController = exchangeController;

            exchangeController.BalanceChangedEventHandler += view_OnBalanceChanged;
            exchangeController.HistoryChangedEventHandler += view_OnHistoryChanged;
            exchangeController.LogAppendedEventHandler += view_OnLogAppended;

            FillComboBoxExchange();
            SetFromConfig();
        }

        private void FillComboBoxExchange()
        {
            string[] nms = exchangeController.GetExchangeRealNames();

            comboBoxExchange.BeginUpdate();
            comboBoxExchange.Items.Clear();
            comboBoxExchange.Items.AddRange(nms);
            comboBoxExchange.EndUpdate();
        }

        private void SetFromConfig()
        {
            Configuration config = exchangeController.GetConfigurationCopy();

            textBoxCoin1.Text = config.coin1;
            textBoxCoin2.Text = config.coin2;
            textBoxlowestBalance1.Text = config.lowestBalance1;
            textBoxlowestBalance2.Text = config.lowestBalance2;
            textBoxApiID.Text = config.apiID;
            textBoxSecret.Text = config.secret;
            comboBoxExchange.SelectedIndex = config.idxExchange;
        }

        private void buttonToggle_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                UpdateStatus();
                if (!isChecking)
                {
                    MessageBox.Show("参数配置错误",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                EnableControls(false);
                isRunning = true;

                // 开始你的表演
                exchangeController.Performance();
            }
            else
            {
                EnableControls(true);
                isRunning = false;

                // 谢幕
                exchangeController.CurtainCall();
            }
        }

        public bool isRunning = false;

        public bool isChecking = false;
        public string coin1;
        public string coin2;
        public string lowestBalance1;
        public string lowestBalance2;
        public string apiID;
        public string secret;
        public int idxExchange;

        private void UpdateStatus()
        {
            coin1 = textBoxCoin1.Text;
            coin2 = textBoxCoin2.Text;
            lowestBalance1 = textBoxlowestBalance1.Text;
            lowestBalance2 = textBoxlowestBalance2.Text;
            apiID = textBoxApiID.Text;
            secret = textBoxSecret.Text;
            idxExchange = comboBoxExchange.SelectedIndex;

            Configuration config = exchangeController.GetConfigurationCopy();
            config.coin1 = coin1;
            config.coin2 = coin2;
            config.lowestBalance1 = lowestBalance1;
            config.lowestBalance2 = lowestBalance2;
            config.apiID = apiID;
            config.secret = secret;
            config.idxExchange = idxExchange;
            Configuration.Save(config);

            isChecking = !coin1.IsNullOrEmpty() && !coin2.IsNullOrEmpty()
                && !apiID.IsNullOrEmpty() && !secret.IsNullOrEmpty()
                && !lowestBalance1.IsNullOrEmpty() && !lowestBalance2.IsNullOrEmpty()
                && idxExchange != -1;
        }

        private void EnableControls(bool enable)
        {
            textBoxCoin1.Enabled = enable;
            textBoxCoin2.Enabled = enable;
            textBoxlowestBalance1.Enabled = enable;
            textBoxlowestBalance2.Enabled = enable;
            textBoxApiID.Enabled = enable;
            textBoxSecret.Enabled = enable;
            comboBoxExchange.Enabled = enable;

            buttonToggle.Text = enable ? "开始" : "停止";
        }

        private void view_OnBalanceChanged(List<Balance> balances)
        {
            syncContext.Post(UpdateTextBoxCoinBalance, balances);
        }

        private void UpdateTextBoxCoinBalance(object state)
        {
            List<Balance> balances = (List<Balance>)state;

            if (balances.Count != 2)
            {
                textBoxCoinBalance1.Text = "查询错误";
                return;
            }

            textBoxCoinBalance1.Text = balances[0].Available.ToString();
            textBoxCoinBalance2.Text = balances[1].Available.ToString();
        }

        private void view_OnHistoryChanged(List<TradeHistory> histories)
        {
            syncContext.Post(UpdateTextBoxHistory, histories);
        }

        private void UpdateTextBoxHistory(object state)
        {
            List<TradeHistory> histories = (List<TradeHistory>)state;
            if (histories.Count == 0)
            {
                textBoxHistory.Text = "查询错误";
                return;
            }

            StringBuilder content = new StringBuilder("");
            foreach (var i in histories)
            {
                content.Append(i.Time);
                content.Append(" ");
                content.Append(i.Side);
                content.Append(" ");
                content.Append(i.Price);
                content.Append(" ");
                content.Append(i.Quantity);
                content.Append(" ");
                content.Append("\r\n");
            }
            textBoxHistory.Text = content.ToString();
        }

        private void view_OnLogAppended(string log, bool needStop)
        {
            syncContext.Post(UpdateButtonToggle, needStop);
            syncContext.Post(UpdateTextBoxLog, log);
        }

        private void UpdateTextBoxLog(object state)
        {
            string log = (string)state;

            string copy = textBoxLog.Text;
            textBoxLog.Text = log + "\r\n" + copy;

            Logging.Info(log);
        }

        private void UpdateButtonToggle(object state)
        {
            bool needStop = (bool)state;
            if (isRunning && needStop)
            {
                buttonToggle_Click(this, EventArgs.Empty);
            }
        }
    }
}
