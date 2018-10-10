using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace exchanger.Model.VO
{
    public class Price
    {
        public string Symbol;
        public decimal Last;
        /// <summary>
        /// 买1
        /// </summary>
        public decimal Buy1;
        /// <summary>
        /// 卖2
        /// </summary>
        public decimal Sell1;

        /// <summary>
        /// 价格的精度，也是挂单的最小金额
        /// 
        /// 后面可以从Buy1和Sell1的价格差大致判断出来
        /// </summary>
        public int precision = 2;

        /// <summary>
        /// 暂时直接比买1高0.01
        /// </summary>
        /// <returns></returns>
        public decimal GetPriceBuyPredicted()
        {
            return Buy1 + 0.01m;
        }

        public bool IsPredictedValid(decimal num)
        {
            return num >= 0.01m;
        }

        /// <summary>
        /// 当前买1的价格和之前挂的买单价格比较，找到一个合理的出售价格
        /// </summary>
        /// <param name="lastBuyPrice"></param>
        /// <returns></returns>
        public decimal GetAcceptedSellPrice(decimal lastBuyPrice)
        {
            if (Buy1 >= lastBuyPrice)
            {
                return lastBuyPrice;
            }
            else
            {
                // 说明我们的买单已经被别人收了
                if (lastBuyPrice - Buy1 < 0.1m)
                {
                    // Buy1低于0.03都可接收
                    return Buy1;
                }
                else
                {
                    return -1m;
                }
            }
        }
    }
}
