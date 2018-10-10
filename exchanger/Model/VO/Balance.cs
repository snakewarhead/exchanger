using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace exchanger.Model.VO
{
    public class Balance
    {
        public string CoinName = "";

        public decimal Available = 0m;
        public decimal Reserved = 0m;
        public decimal Total = 0m;
    }
}
