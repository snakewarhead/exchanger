using exchanger.Model.VO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace exchanger.Model.Exchange
{
    public enum SideType
    {
        BUY = 0,
        SELL
    }

    public interface IExchange
    {
        int Idx
        {
            get;
        }

        string Name
        {
            get;
        }

        string RealName
        {
            get;
        }

        List<Balance> GetBalances(List<string> coins, string apiID, string secret);

        List<TradeHistory> GetHistory(string coin1, string coin2);

        Price GetPrice(string coin1, string coin2);

        string PlaceOrder(string coin1, string coin2, decimal price, decimal quantity, SideType side, string apiID, string secret);
    }
}
