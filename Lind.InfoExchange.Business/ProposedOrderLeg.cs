using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lind.InfoExchange.Data;

namespace Lind.InfoExchange.Business
{
    internal class ProposedOrderLeg
    {
        public ThreadSafeAsk Ask { get; private set; }
        public ThreadSafeAskLeg Leg { get; private set; }
        public int LegLevel { get; private set; }
        public bool DoLock { get; private set; }
        private long proposedSellQuantity;
        public long ProposedSellQuantity
        {
            get
            {
                return proposedSellQuantity;
            }
            private set
            {
                proposedSellQuantity = value;
                
                proposedBuyQuantity = (RealSellRatio * proposedSellQuantity).ToFlooredInt();
            }
        }
        public int CommoditySellID
        {
            get { return this.Leg != null && this.Leg.IsBuy ? this.Leg.CommodityID : Ask.Ask.CommodityBuyID.Value; }
        }
        public int CommodityBuyID
        {
            get { return this.Leg != null && this.Leg.IsSell ? this.Leg.CommodityID : Ask.Ask.CommoditySellID.Value; }
        }
        private long proposedBuyQuantity;
        public long ProposedBuyQuantity
        {
            get { return proposedBuyQuantity; }
            private set
            {
                proposedBuyQuantity = value;
                proposedSellQuantity = (RealBuyRatio * proposedBuyQuantity).ToCeilingInt();
            }

        }
        public double RealBuyRatio
        {
            get
            {
                long sellRatio = Leg != null ? Leg.SellRatio : Ask.SellRatio.Value;
                long buyRatio = Leg != null ? Leg.BuyRatio : Ask.BuyRatio.Value;
                return (double)buyRatio / (double)sellRatio;
            }
        }
        public double RealSellRatio
        {
            get
            {
                long sellRatio = Leg != null ? Leg.SellRatio : Ask.SellRatio.Value;
                long buyRatio = Leg != null ? Leg.BuyRatio : Ask.BuyRatio.Value;
                return (double)sellRatio / (double)buyRatio;
            }
        }
        public bool ApplyCommisionToBuy
        {
            get { return this.Leg != null ? this.Leg.Leg.ApplyCommissionToBuy : this.Ask.Ask.ApplyCommissionToBuy; }
        }
        public long SetSellQuantity(long targetValue, bool useLock = false, long buyCap = long.MaxValue)
        {
            long value = Ask.SetBuyQuantity(targetValue, useLock ? DoLock : false, leg: Leg, sellCap: buyCap);
            ProposedSellQuantity += value;
            return value;
        }
        public long SetBuyQuantity(long targetValue, bool useLock = false, long sellCap = long.MaxValue)
        {
            long value = Ask.SetSellQuantity(targetValue, useLock ? DoLock : false, leg: Leg, buyCap: sellCap);
            ProposedBuyQuantity += value;
            return value;
        }
        public ProposedOrderLeg(ThreadSafeAsk ask, int legLevel, bool doLock = false, ThreadSafeAskLeg leg = null)
        {
            Ask = ask;
            LegLevel = legLevel;
            DoLock = doLock;
            Leg = leg;
        }

        public OrderLeg ToOrderLeg()
        {
            return new OrderLeg()
            {
                BuyerUserID = Ask.UserID,
                CommodityBuyID  = Ask.Ask.CommoditySellID != null ? Ask.Ask.CommoditySellID.Value : Leg.CommodityID,
                CommoditySellID = Ask.Ask.CommodityBuyID != null ? Ask.Ask.CommodityBuyID.Value : Leg.CommodityID,
                BuyQuantity = ProposedBuyQuantity,
                SellQuantity = ProposedSellQuantity,
                AskID = Ask.AskID
            };
        }
    }
}
