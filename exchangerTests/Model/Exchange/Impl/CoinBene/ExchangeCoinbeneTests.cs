using Microsoft.VisualStudio.TestTools.UnitTesting;
using exchanger.Model.Exchange.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace exchanger.Model.Exchange.Impl.CoinBene.Test
{
    [TestClass()]
    public class ExchangeCoinbeneTests
    {
        [TestMethod()]
        public void GetBalancesTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetHistoryTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void TestSign()
        {
            var dic = new Dictionary<string, string> {
            { "apiid","" },
            { "secret","" },
            { "timestamp","1529918947855" },
            { "type","buy-limit" },
            { "price","450.15" },
            { "quantity","14.394915088" },
            { "symbol","ETHUSDT" } };
            string signed = utils.sign(dic);
            Assert.AreEqual("", signed);
        }
    }
}