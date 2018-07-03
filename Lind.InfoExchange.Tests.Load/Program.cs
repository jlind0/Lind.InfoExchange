using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Lind.InfoExchange.Data;
using Lind.InfoExchange.Business;
using Lind.InfoExchange.Tests.Load.OrderProcessor;
using FX = Lind.InfoExchange.Service.WCF.FaultException;
using System.ServiceModel;
using log4net;
using System.Diagnostics;
using OP = Lind.InfoExchange.Business.OrderProcessor;

namespace Lind.InfoExchange.Tests.Load
{
    class Program
    {
        private const int Workers = 10;
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            
            log4net.Config.XmlConfigurator.Configure();
            try
            {
                OP.Singleton.Start().Wait();
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
                            Order order = null;
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
                                ask.AskQuantity = rand.Next(5000, 1500000);
                                ask.UserID = users[rand.Next(0, users.Length - 1)].UserID;
                                ask.MaxLegDepth = 4;
                                watch.Start();
                                order = OP.Singleton.ExecuteAsk(ask);
                                watch.Stop();
                            }
                            catch (FaultException<FX> ex)
                            {
                                Log.Error(string.Format("Error executing Ask: {0}", ex.Detail.Exception));
                                error = true;
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error executing ask", ex);
                                error = true;
                            }
                            Log.Debug(string.Format("Elapsed: {0}, AskID: {1}, OrderID: {2}", watch.ElapsedMilliseconds, ask.AskID, order != null ? order.OrderID.ToString() : "No Order"));
                            if (error)
                                break;
                        }
                    }, cancelationToken.Token);
                    tasks.Add(t);
                    i++;
                }
                Console.WriteLine("Press enter to terminate test.");
                Console.ReadLine();
                cancelationToken.Cancel(false);
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception ex)
            {
                Log.Error("Error in Main Method", ex);
            }
       }
        
    }
}
