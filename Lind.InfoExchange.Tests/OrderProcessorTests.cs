using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lind.InfoExchange.Data;
using Lind.InfoExchange.Business;
using Moq;
using System.Linq;
using System.Diagnostics;

namespace Lind.InfoExchange.Tests
{
    /// <summary>
    /// Summary description for OrderProcessorTests
    /// </summary>
    [TestClass]
    public class OrderProcessorTests
    {
        public OrderProcessorTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
        int gold = 1;
        int copper = 2;
        int silver = 3;
        int platnium = 4;
        int palladium = 5;
        [TestMethod]
        public void TestCopperForGoldOrder()
        {
            

            var askRepositoryMock = new Mock<IAskRepository>();
            askRepositoryMock.Setup(f => f.GetAsks()).Returns(GetAsksForCopperForGoldOrder());
            var commodityRepositoryMock = new Mock<ICommodityRepository>();
            commodityRepositoryMock.Setup(f => f.GetCommodities()).Returns(GetCommodities());
            Ask ask = new Ask();
            ask.AllowPartialFill = true;
            ask.ApplyCommissionToBuy = true;
            ask.BuyQuantity = 1000000;
            ask.CommodityBuyID = copper;
            ask.CommoditySellID = gold;
            ask.SellRatio = 1295;
            ask.BuyRatio = 3;
            ask.MaxLegDepth = 4;
            var askRepository = askRepositoryMock.Object;
            OrderProcessor proc = new OrderProcessor(askRepository, commodityRepositoryMock.Object);
            Stopwatch watch = new Stopwatch();
            
            proc.Start().Wait();
            watch.Start();
            var order = proc.GetQuote(ask);
            watch.Stop();
            TestContext.WriteLine(watch.ElapsedMilliseconds.ToString());
            Assert.AreNotEqual(0, order.Sum(o => o.OrderLegs.Where(ol => ol.CommodityBuyID == copper).Sum(ol => ol.BuyQuantity)));

        }
        [TestMethod]
        public void TestCopperForGoldOrderSell()
        {


            var askRepositoryMock = new Mock<IAskRepository>();
            askRepositoryMock.Setup(f => f.GetAsks()).Returns(GetAsksForCopperForGoldOrder());
            var commodityRepositoryMock = new Mock<ICommodityRepository>();
            commodityRepositoryMock.Setup(f => f.GetCommodities()).Returns(GetCommodities());
            Ask ask = new Ask();
            ask.AllowPartialFill = true;
            ask.ApplyCommissionToBuy = true;
            ask.SellQuantity = 5000;
            ask.CommodityBuyID = copper;
            ask.CommoditySellID = gold;
            ask.SellRatio = 1294;
            ask.BuyRatio = 3;
            ask.MaxLegDepth = 4;
            var askRepository = askRepositoryMock.Object;
            OrderProcessor proc = new OrderProcessor(askRepository, commodityRepositoryMock.Object);
            proc.Start().Wait();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var order = proc.ExecuteAsk(ask);
            watch.Stop();
            TestContext.WriteLine(watch.ElapsedMilliseconds.ToString());
            Assert.AreNotEqual(0, order.Sum(o => o.OrderLegs.Where(ol => ol.CommodityBuyID == copper).Sum(ol => ol.BuyQuantity)));

        }
        [TestMethod]
        public void TestCopperForGoldOrderSellMarket()
        {
            var askRepositoryMock = new Mock<IAskRepository>();
            askRepositoryMock.Setup(f => f.GetAsks()).Returns(GetAsksForCopperForGoldOrder());
            var commodityRepositoryMock = new Mock<ICommodityRepository>();
            commodityRepositoryMock.Setup(f => f.GetCommodities()).Returns(GetCommodities());
            Ask ask = new Ask();
            ask.AllowPartialFill = true;
            ask.ApplyCommissionToBuy = true;
            ask.SellQuantity = 5000;
            ask.CommodityBuyID = copper;
            ask.CommoditySellID = gold;
            ask.MaxLegDepth = 4;
            var askRepository = askRepositoryMock.Object;
            OrderProcessor proc = new OrderProcessor(askRepository, commodityRepositoryMock.Object);
            proc.Start().Wait();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var order = proc.GetMarketQuote(ask);
            watch.Stop();
            TestContext.WriteLine(watch.ElapsedMilliseconds.ToString());
            Assert.AreNotEqual(0, order.Sum(o => o.OrderLegs.Where(ol => ol.CommodityBuyID == copper).Sum(ol => ol.BuyQuantity)));
        }
        private IEnumerable<Commodity> GetCommodities()
        {
            yield return new Commodity() { CommodityID = gold, CommodityName = "Gold" };
            yield return new Commodity() { CommodityID = copper, CommodityName = "Copper" };
            yield return new Commodity() { CommodityID = silver, CommodityName = "Silver" };
            yield return new Commodity() { CommodityID = platnium, CommodityName = "Platnium" };
            yield return new Commodity() { CommodityID = palladium, CommodityName = "Palladium" };
        }
        private IEnumerable<Ask> GetAsksForCopperForGoldOrder()
        {
            yield return new Ask()
            {
                AskID = 1,
                AllowPartialFill = true,
                CommodityBuyID = gold,
                CommoditySellID = silver,
                AskDate = DateTime.UtcNow,
                BuyQuantity = 1000,
                SellRatio = 1295,
                BuyRatio = 20
            };
            yield return new Ask()
            {
                AskID = 2,
                AllowPartialFill = true,
                CommodityBuyID = gold,
                CommoditySellID = silver,
                AskDate = DateTime.UtcNow,
                BuyQuantity = 1000,
                SellRatio = 1295,
                BuyRatio = 15
            };
            yield return new Ask()
            {
                AskID = 3,
                AllowPartialFill = true,
                CommodityBuyID = silver,
                CommoditySellID = platnium,
                AskDate = DateTime.UtcNow,
                ApplyCommissionToBuy = true,
                BuyQuantity = 1000000,
                SellRatio = 20,
                BuyRatio = 1466
            };
            yield return new Ask()
            {
                AskID = 4,
                AllowPartialFill = true,
                CommodityBuyID = platnium,
                CommoditySellID = copper,
                AskDate = DateTime.UtcNow,
                BuyQuantity = 100000,
                SellRatio = 1466,
                BuyRatio = 3
            };
            yield return new Ask()
            {
                AskID = 5,
                AllowPartialFill = true,
                CommodityBuyID = gold,
                AskDate = DateTime.UtcNow,
                BuyQuantity = 1000,
                AskLegs = new AskLeg[]
                {
                    new AskLeg()
                    {
                        SellCommodityID = silver,
                        BuyRatio = 20,
                        SellRatio = 1295
                    },
                    new AskLeg()
                    {
                        SellCommodityID = copper,
                        BuyRatio = 3,
                        SellRatio = 1295
                    }
                }
            };
        }
    }
}
