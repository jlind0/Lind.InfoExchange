using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Transactions;
using System.Threading;

namespace Lind.InfoExchange.Data
{
    public class AskRepository : IAskRepository
    {
        private ConcurrentQueue<Ask> AsksToUpdate { get; set; }
        private ConcurrentQueue<AskLeg> AskLegsToUpdate { get; set; }
        public AskRepository()
        {
            AsksToUpdate = new ConcurrentQueue<Ask>();
            AskLegsToUpdate = new ConcurrentQueue<AskLeg>();
        }
        public Task Start(CancellationTokenSource token)
        {
            return Task.Factory.StartNew(() =>
            {
                Ask askToUpDate;
                AskLeg askLegToUpdate;
                while (!token.IsCancellationRequested || AsksToUpdate.Count > 0)
                {
                    while (AsksToUpdate.TryDequeue(out askToUpDate))
                    {
                        UpdateAsk(askToUpDate);
                    }
                    while (AskLegsToUpdate.TryDequeue(out askLegToUpdate))
                    {
                        UpadateAskLeg(askLegToUpdate);
                    }
                    Thread.Sleep(10000);
                }
                
            }, token.Token);
        }
        public IEnumerable<Ask> GetAsks(int commodityID)
        {
            using (var context = new InfoExchangeContext())
            {
                return context.Asks.AsNoTracking().Include(a => a.CommodityBuy).Include(a => a.CommoditySell).Where(
                    a => a.CommodityBuyID == commodityID || a.CommoditySellID == commodityID).ToArray();
            }
        }

        private void SubmitAsk(Ask ask, InfoExchangeContext context)
        {
            ask.AskDate = DateTime.UtcNow;
            context.Asks.Add(ask);
            context.SaveChanges();
        }


        public IEnumerable<Ask> GetAsks()
        {
            using (var context = new InfoExchangeContext())
            {
                return context.Asks.AsNoTracking().Include(a => a.CommodityBuy).Include(a => a.CommoditySell).Include(a => a.AskLegs).Where(
                    a => (a.ValidToDate == null || a.ValidToDate >= DateTime.UtcNow) && (a.BuyQuantity > 0 || a.SellQuantity > 0)).ToArray();
            }
        }

        public void UpadateAskLeg(AskLeg leg)
        {
            using (var context = new InfoExchangeContext())
            {
                UpadateAskLeg(leg);
            }
        }
        public void UpdateAsk(Ask ask)
        {
            using (var context = new InfoExchangeContext())
            {
                UpdateAsk(ask, context);
            }
        }
        private void UpdateAsk(Ask ask, InfoExchangeContext context)
        {
            ask.CommodityBuy = null;
            ask.CommoditySell = null;
            ask.User = null;
            ask.AskLegs = null;
            context.Asks.Attach(ask);
            context.Entry(ask).State = EntityState.Modified;
            context.SaveChanges();
        }
        private void UpdateAskLeg(AskLeg leg, InfoExchangeContext context)
        {
            leg.Ask = null;
            context.AskLegs.Attach(leg);
            context.Entry(leg).State = EntityState.Modified;
            context.SaveChanges();
        }
        public void DeleteAsk(Ask ask)
        {
            using (var context = new InfoExchangeContext())
            {
                DeleteAsk(ask, context);
            }
        }
        private void DeleteAsk(Ask ask, InfoExchangeContext context)
        {
            var dAsk = context.Asks.Find(ask.AskID);
            if (dAsk != null)
            {
                context.Asks.Remove(dAsk);
                context.SaveChanges();
            }
        }

        private void SaveOrder(Order order, InfoExchangeContext context)
        {
            if(order.OrderID == Guid.Empty)
                order.OrderID = Guid.NewGuid();
            order.OrderDate = DateTime.UtcNow;
            foreach (var leg in order.OrderLegs)
            {
                leg.OrderID = order.OrderID;
                if(leg.OrderLegID == Guid.Empty)
                    leg.OrderLegID = Guid.NewGuid();
                context.OrderLegs.Add(leg);
            }
            context.Orders.Add(order);
            context.SaveChanges();
        }


        public void AddAsk(Ask ask)
        {
            using (var context = new InfoExchangeContext())
            {
                SubmitAsk(ask, context);
            }
        }

        public void ExecuteAsk(Ask ask, IEnumerable<Order> orders, IEnumerable<Ask> orderLegAsks, IEnumerable<AskLeg> askLegs)
        {
            using (var context = new InfoExchangeContext())
            {
                using (var scope = new TransactionScope())
                {
                    AsksToUpdate.Enqueue(ask);
                    foreach(var order in orders)
                    {
                        SaveOrder(order, context);
                        if (orderLegAsks != null)
                        {
                            foreach (var orderAsk in orderLegAsks)
                            {
                                AsksToUpdate.Enqueue(orderAsk);
                            }
                        }
                    }
                
                    scope.Complete();
                }
            }
        }
    }
}
