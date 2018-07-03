using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lind.InfoExchange.Data;
using Lind.InfoExchange.Business;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Lind.InfoExchange.Tests.Integration
{
    [TestClass]
    public class OrderProcessorTests
    {
        private TestContext testContextInstance;
        public TestContext TestContext
        {
            get{return this.testContextInstance;}
            set{this.testContextInstance = value;}
        }
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            OrderProcessor.Singleton.Start().Wait();
            OrderProcessor.Singleton.AskProcessor();
            
        }
        [ClassCleanup]
        public static void ClassCleanUp()
        {
            OrderProcessor.Singleton.Dispose();
        }
        [TestMethod]
        public void TestCopperForGoldQuote()
        {
            Ask ask = new Ask();
            ask.UserID = new Guid("D1C03047-7BFA-4135-B8E9-63401C78DE96");
            ask.AllowPartialFill = true;
            ask.BuyRatio = 1245;
            ask.SellRatio = 3;
            ask.CommoditySellID = 17;
            ask.CommodityBuyID = 14;
            ask.ValidToDate = null;
            ask.BuyQuantity = 100000;
            ask.ApplyCommissionToBuy = false;
            ask.MaxLegDepth = 4;
            var order = OrderProcessor.Singleton.GetQuote(ask);
            Assert.IsNotNull(order);
            Assert.AreNotEqual(0, order.Count());
        }
        private const int Workers = 5;
        [TestMethod]
        public void TestCopperForGoldMarketQuote()
        {
            Ask ask = new Ask();
            ask.UserID = new Guid("D1C03047-7BFA-4135-B8E9-63401C78DE96");
            ask.AllowPartialFill = true;
            ask.CommoditySellID = 17;
            ask.CommodityBuyID = 14;
            ask.ValidToDate = null;
            ask.BuyQuantity = 100000;
            ask.ApplyCommissionToBuy = false;
            ask.MaxLegDepth = 4;
            Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            var order = OrderProcessor.Singleton.GetMarketQuote(ask);
            watch.Stop();
            TestContext.WriteLine(watch.ElapsedMilliseconds.ToString());
            Assert.IsNotNull(order);
            Assert.AreNotEqual(0, order.Count());

        }
        [TestMethod]
        public void TestRawChickLegForOakBoard()
        {
            
            Ask ask = new Ask();
            ask.UserID = new Guid("D1C03047-7BFA-4135-B8E9-63401C78DE96");
            ask.AllowPartialFill = true;
            ask.CommodityBuyID = 45;
            ask.CommoditySellID = 43;
            ask.BuyRatio = 1000;
            ask.SellRatio = 900;
            ask.ValidToDate = null;
            ask.BuyQuantity = 100000;
            ask.MaxLegDepth = 4;
            Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            var order = OrderProcessor.Singleton.ExecuteAsk(ask);
            watch.Stop();
            TestContext.WriteLine(watch.ElapsedMilliseconds.ToString());
            Assert.IsNotNull(order);
            Assert.AreNotEqual(0, order.Count());
            
        }

        [TestMethod]
        public void LoadTest()
        {
            try
            {
                CommodityRepository commodityRepository = new CommodityRepository();
                var commodities = commodityRepository.GetCommodityPrices().ToArray();
                UserRepository userRepository = new UserRepository();
                var users = userRepository.GetUsers().ToArray();
                CancellationTokenSource cancelationToken = new CancellationTokenSource();
                List<Task> tasks = new List<Task>();
                double[] adjustments = new double[] { 0.8, 0.85, 0.85, 0.9, 0.9, 0.91, 0.91, 0.91, 0.92, 0.92, 0.92, 0.92, 0.93, 0.93, 0.93, 0.93, 0.93, 0.94, 0.94, 0.94, 0.94, 0.94,
                    0.95, 0.95, 0.95, 0.95, 0.95, 0.95, 0.95, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.96, 0.97, 0.97, 0.97, 0.97, 0.97, 0.97, 0.97, 0.97, 0.97,
                    0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.98, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99,
                    1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
                    1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.01, 1.02, 1.02, 1.02, 1.02, 1.02, 1.02, 1.02,
                    1.03, 1.03, 1.03, 1.03, 1.03, 1.03, 1.04, 1.04, 1.04, 1.04, 1.04, 1.04, 1.04, 1.04, 1.04, 1.05, 1.05, 1.05, 1.05, 1.05, 1.05 ,1.05, 1.05, 1.05,
                    1.06, 1.06, 1.06, 1.06, 1.06, 1.07, 1.07, 1.07, 1.07, 1.07, 1.08, 1.08, 1.08, 1.08, 1.08, 1.08, 1.08, 1.09, 1.09, 1.09, 1.09, 1.1, 1.1, 1.1, 1.15, 1.15, 1.2};
                int i = 0;
                while (i < Workers)
                {
                    Task t = Task.Factory.StartNew(() =>
                    {

                        while (!cancelationToken.IsCancellationRequested)
                        {
                            Stopwatch watch = new Stopwatch();
                            Ask ask = new Ask();
                            IEnumerable<Order> orders = null;
                            bool error = false;
                            try
                            {
                                Random rand = new Random((int)DateTime.Now.Ticks);

                                int buyCommodityIndex = rand.Next(0, commodities.Length - 1);
                                int sellCommodityIndex = rand.Next(0, commodities.Length - 1);
                                while (buyCommodityIndex == sellCommodityIndex)
                                    sellCommodityIndex = rand.Next(0, commodities.Length - 1);

                                var buyCommodity = commodities[buyCommodityIndex];
                                var sellCommodity = commodities[sellCommodityIndex];
                                double buyRatio = sellCommodity.PriceInGold / buyCommodity.PriceInGold;


                                ask.AskDate = DateTime.UtcNow;
                                ask.ApplyCommissionToBuy = buyRatio < 0;
                                ask.BuyRatio = (sellCommodity.PriceInGold * adjustments[rand.Next(0, adjustments.Length - 1)]).ToFlooredInt();
                                ask.SellRatio = (buyCommodity.PriceInGold * adjustments[rand.Next(0, adjustments.Length - 1)]).ToFlooredInt();
                                ask.CommodityBuyID = buyCommodity.CommodityID;
                                ask.CommoditySellID = sellCommodity.CommodityID;
                                ask.AllowPartialFill = rand.Next(0, 100) >= 30;
                                ask.BuyQuantity = rand.Next(5000, 1500000);
                                ask.UserID = users[rand.Next(0, users.Length - 1)].UserID;
                                ask.MaxLegDepth = 4;
                                watch.Start();
                                orders = OrderProcessor.Singleton.ExecuteAsk(ask);
                                watch.Stop();
                            }
                            catch (Exception ex)
                            {
                                TestContext.WriteLine(ex.ToString());
                                error = true;
                            }
                            TestContext.WriteLine(string.Format("Elapsed: {0}, AskID: {1}, OrderIDs: {2}", watch.ElapsedMilliseconds, ask.AskID, orders != null ? orders.Select(o => o.OrderID.ToString()).Aggregate((result,next) => result +","+next) : "No Order"));
                            if (error)
                                break;
                        }
                    }, cancelationToken.Token);
                    tasks.Add(t);
                    i++;
                }
                Task.Delay(60000).Wait();
                cancelationToken.Cancel(false);
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception)
            {
                throw;
            }  
        }
    }
}
