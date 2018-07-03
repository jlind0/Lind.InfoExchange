using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lind.InfoExchange.Data;

namespace Lind.InfoExchange.Business
{
    internal class ProposedOrder
    {
        private SortedDictionary<int, ICollection<ProposedOrderLeg>> orderLegs { get; set; }
        public IEnumerable<ProposedOrderLeg> OrderLegs
        {
            get { return orderLegs.SelectMany(o => o.Value); }
        }
        public ThreadSafeAsk Ask { get; private set; }

        public ProposedOrder(ThreadSafeAsk ask)
        {
            this.Ask = ask;
            this.orderLegs = new SortedDictionary<int, ICollection<ProposedOrderLeg>>();
        }
        public ICollection<ProposedOrderLeg> GetOrderLegs(int level)
        {
            ICollection<ProposedOrderLeg> orderLeg;
            if (orderLegs.TryGetValue(level, out orderLeg))
                return orderLeg;
            return null;
        }
        public long ProposedQuantitySell
        {
            get
            {
                if(orderLegs.Count > 0)
                    return orderLegs.First().Value.Sum(c => c.ProposedSellQuantity);
                return 0;
            }
        }
        
        public long ProposedQuanityBuy
        {
            get
            {
                if(orderLegs.Count > 0)
                    return orderLegs.Last().Value.Sum(c => c.ProposedBuyQuantity);
                return 0;
            }
        }
        public void SetOrderLegs(ICollection<ProposedOrderLeg> legs)
        {
            if(legs.Count > 0)
                orderLegs[legs.First().LegLevel] = legs;
        }
        public double BuyRatio
        {
            get
            {
                return (double)ProposedQuanityBuy / (double)ProposedQuantitySell;
            }
        }
        public int BuyCommodityID
        {
            get
            {
                if (orderLegs.Count > 0)
                {
                    var lastOrder = orderLegs.Last().Value.FirstOrDefault();
                    if (lastOrder != null)
                        return lastOrder.CommodityBuyID;
                }
                return -1;
            }
        }
        public int SellCommodityID
        {
            get
            {
                if (orderLegs.Count > 0)
                {
                    var firstOrder = orderLegs.First().Value.FirstOrDefault();
                    if (firstOrder != null)
                        return firstOrder.CommoditySellID;
                }
                return -1;
            }
        }
        public bool IsValidOrder
        {
            get
            {
                if (orderLegs.Count > 0)
                {
                    var firstOrder = orderLegs.First().Value.FirstOrDefault();
                    var lastOrder = orderLegs.Last().Value.FirstOrDefault();
                    if (firstOrder != null && lastOrder != null)
                    {
                        return OrderQuantitiesInSync && orderLegs.All(o => o.Value.Count > 0) && Ask.CommoditySellID.Contains(firstOrder.CommoditySellID) &&
                            Ask.CommodityBuyID.Contains(lastOrder.CommodityBuyID) && ProposedQuanityBuy > 0 && ProposedQuantitySell > 0;
                    }
                }
                return false;
            }
        }
        public ProposedOrderLeg RemoveWorstPerforming(bool useLock = false)
        {
            double percentOfAverageForWorstLeg = double.MaxValue;
            ProposedOrderLeg leg = null;
            foreach (var legs in this.orderLegs.OrderByDescending(o => o.Key))
            {
                double avgBuyRatio = legs.Value.Average(o => (double)o.ProposedBuyQuantity / (double)o.ProposedSellQuantity);
                var worstOrderLeg = legs.Value.OrderBy(o => (double)o.ProposedBuyQuantity / (double)o.ProposedSellQuantity).First();
                if (worstOrderLeg.ProposedBuyQuantity > 0)
                {
                    double percentOfAverage = ((double)worstOrderLeg.ProposedBuyQuantity / (double)worstOrderLeg.ProposedSellQuantity) / avgBuyRatio;
                    if (percentOfAverage < percentOfAverageForWorstLeg)
                    {
                        percentOfAverageForWorstLeg = percentOfAverage;
                        leg = worstOrderLeg;
                    }
                }
            }
            if (leg != null)
            {
                if (!leg.Ask.IsBuy)
                    leg.SetBuyQuantity(-1 * leg.ProposedBuyQuantity, useLock: useLock);
                else
                    leg.SetSellQuantity(-1 * leg.ProposedSellQuantity, useLock: useLock);
            }
            return leg;
        }
        public bool OrderQuantitiesInSync
        {
            get
            {
                bool isInSync = true;
                foreach (var ol in orderLegs.OrderByDescending(ol => ol.Key))
                {
                    var nextLegs = this.GetOrderLegs(ol.Key - 1);
                    if (nextLegs == null)
                    {
                        isInSync = isInSync && ol.Key == 1;
                        break;
                    }
                    
                    var sellQty = nextLegs.Sum(l => l.ProposedBuyQuantity);
                    var buyQty = ol.Value.Sum(l => l.ProposedSellQuantity);
                    isInSync = isInSync && sellQty == buyQty && sellQty > 0 && buyQty > 0;
                }
                return isInSync;
            }
        }
        public void RemoveOrderLeg(ProposedOrderLeg leg)
        {
            this.orderLegs[leg.LegLevel].Remove(leg);
        }
        public void CleanUpOrder()
        {
            foreach (var ol in orderLegs)
            {
                foreach (var leg in ol.Value.Where(o => o.ProposedSellQuantity <= 0 || o.ProposedBuyQuantity <= 0).ToArray())
                {
                    ol.Value.Remove(leg);
                }
            }
        }
    }
}
