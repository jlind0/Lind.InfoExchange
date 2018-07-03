using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lind.InfoExchange.Data;
using System.ComponentModel;

namespace Lind.InfoExchange.Business
{
    internal class ThreadSafeAsk : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public Ask Ask { get; private set; }
        private ICollection<int> commodityBuyID;
        private ICollection<int> commoditySellID;
        public ICollection<ThreadSafeAskLeg> Legs { get; private set; }
        public ThreadSafeAsk(Ask ask)
        {
            this.Ask = ask;
            Legs = ask.AskLegs.Select(a => new ThreadSafeAskLeg(a, this, sync)).ToArray();
            commodityBuyID = ask.CommodityBuyID != null ? new int[] { ask.CommodityBuyID.Value } : Legs.Select(c => c.CommodityID).ToArray();
            commoditySellID = ask.CommoditySellID != null ? new int[] { ask.CommoditySellID.Value } : Legs.Select(c => c.CommodityID).ToArray();
        }
        private readonly object sync = new object();
        public ICollection<int> CommodityBuyID { get { return commodityBuyID; } }
        public ICollection<int> CommoditySellID { get { return commoditySellID; } }
        public Guid UserID { get { return Ask.UserID; } }
        public long? SellRatio
        {
            get
            {
                return Ask.SellRatio;
            }
        }
        public long? BuyRatio
        {
            get
            {
                return Ask.BuyRatio;
            }
        }
        public double? GetBuyRatio(int sellID, int buyId)
        {
            if (Legs.Count > 0)
            {
                var leg = IsBuy ? Legs.Where(l => l.CommodityID == sellID).SingleOrDefault() : Legs.Where(l => l.CommodityID == buyId).SingleOrDefault();
                if (leg != null)
                    return (double)leg.SellRatio / (double)leg.BuyRatio;
            }
            else if (SellRatio != null && BuyRatio != null)
                return (double)SellRatio /  BuyRatio;
            return null;
        }
        public long AskID { get { return Ask.AskID; } }
        public bool AllowPartialFill
        {
            get
            {
                lock(sync)
                    return Ask.AllowPartialFill;
            }
        }
        public DateTime AskDate { get { return Ask.AskDate; } }
        public bool GetApplyCommissionToBuy(int commoditySellID, int commodityBuyID)
        {
            if (Legs.Count > 0)
            {
                if (IsBuy)
                    return Legs.Where(l => l.CommodityID == commoditySellID).Single().Leg.ApplyCommissionToBuy;
                else
                    return Legs.Where(l => l.CommodityID == commodityBuyID).Single().Leg.ApplyCommissionToBuy;
            }
            return Ask.ApplyCommissionToBuy;
        }
        private long askQuantity
        {
            get { return this.IsBuy ? this.Ask.BuyQuantity.Value : this.Ask.SellQuantity.Value; }
        }
        public long AskQuantity
        {
            get
            {
                lock (sync)
                {
                    return askQuantity;
                }
            }
        }
        public bool IsBuy
        {
            get { return this.Ask.BuyQuantity != null; }
        }
        public bool IsSell
        {
            get { return this.Ask.SellQuantity != null; }
        }
        private long lockQuantity;
        public long LockQuantity
        {
            get
            {
                lock (sync)
                {
                    return lockQuantity;
                }
            }
            private set
            {
                lock (sync)
                {
                    lockQuantity = value;
                }
            }
        }
        private long lockExecuteQuantity;
        public long LockExecuteQuantity
        {
            get
            {
                lock (sync)
                {
                    return lockExecuteQuantity;
                }
            }
            set
            {
                lock (sync)
                {
                    lockExecuteQuantity = value;
                }
            }
        }
        public long? MinBuyQuantity
        {
            get
            {
               
                    return Ask.MinBuyQuantity;
            }
        }
        public long? MinSellQuantity
        {
            get
            {
               
                    return Ask.MinSellQuantity;
            }
        }
        public long SetBuyQuantity(long targetValue, bool doLock = false, ThreadSafeAskLeg leg = null, long sellCap = long.MaxValue)
        {
            lock (sync)
            {
                long sellRatio = leg != null ? leg.SellRatio : SellRatio.Value;
                long buyRatio = leg != null ? leg.BuyRatio : BuyRatio.Value;
                long aQ = this.IsBuy ? availableQuantity : (((double)buyRatio / (double)sellRatio) * availableQuantity).ToFlooredInt();
                if (leg != null && leg.AvailableQuantity != null)
                {
                    if (leg.IsBuy)
                        aQ = leg.AvailableQuantity < aQ ? leg.AvailableQuantity.Value : aQ;
                    else
                    {
                        long sellAvailable = this.IsBuy ? (((double)sellRatio / (double)buyRatio) * availableQuantity).ToFlooredInt() : availableQuantity;
                        aQ = leg.AvailableQuantity < sellAvailable ? (((double)buyRatio / (double)sellRatio) * leg.AvailableQuantity.Value).ToFlooredInt() : aQ; 
                    }

                }
                long buyCap = (((double)sellRatio / (double)buyRatio) * aQ).ToFlooredInt();
                aQ = aQ > buyCap ? buyCap : aQ;
                long value = aQ >= targetValue ? targetValue : aQ;
                long sellValue = ((double)sellRatio / (double)buyRatio * value).ToFlooredInt();
                if (value > 0 && (value < MinBuyQuantity || sellValue < MinSellQuantity || (!AllowPartialFill && value != availableQuantity) || sellValue <= 0))
                {
                    value = 0;
                    sellValue = 0;
                }
                if (doLock)
                {
                    lockQuantity += IsBuy ? value : sellValue;
                    if (leg != null)
                        leg.SetLock(leg.IsBuy ? value : sellValue);
                    if (targetValue < 0 && PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("AvailableQuantity"));
                }
                return value;
            }
        }
        public int MaxLegDepth
        {
            get { return this.Ask.MaxLegDepth; }
        }
        public long SetSellQuantity(long targetValue, bool doLock = false, ThreadSafeAskLeg leg = null, long buyCap = long.MaxValue)
        {
            lock (sync)
            {
                long sellRatio = leg != null ? leg.SellRatio : SellRatio.Value;
                long buyRatio = leg != null ? leg.BuyRatio : BuyRatio.Value;
                long aQ = IsBuy ? availableQuantity : (((double)buyRatio/(double)sellRatio)*availableQuantity).ToFlooredInt();
                long available = IsBuy ? (((double)sellRatio / (double)buyRatio) * availableQuantity).ToFlooredInt() : availableQuantity;
                if (leg != null && leg.AvailableQuantity != null)
                {
                    if (leg.IsBuy)
                        aQ = leg.AvailableQuantity < aQ ? leg.AvailableQuantity.Value : aQ;
                    else
                    {
                        aQ = leg.AvailableQuantity < available ? (((double)buyRatio / (double)sellRatio) * leg.AvailableQuantity.Value).ToFlooredInt() : aQ; 
                    }
                    available = (((double)sellRatio/(double)buyRatio)*aQ).ToFlooredInt();
                }
                long sellCap = (((double)buyRatio / (double)sellRatio) * available).ToFlooredInt();
                available = available > sellCap ? sellCap : available;
                long value = available >= targetValue ? targetValue : available;
                long buyValue = (((double)buyRatio / (double)sellRatio) * value).ToCeilingInt();
                if (value > 0 && (value < MinSellQuantity || buyValue < MinBuyQuantity || (!AllowPartialFill && ((IsBuy && Ask.BuyQuantity != buyValue) || (!IsBuy && Ask.SellQuantity != value))) || buyValue <= 0))
                {
                    value = 0;
                    buyValue = 0;
                }
                if (doLock)
                {
                    lockQuantity += IsBuy ? buyValue : value;
                    if (leg != null)
                        leg.SetLock(leg.IsBuy ? buyValue : value);
                    if (targetValue < 0 && PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("AvailableQuantity"));
                }
                return value;
            }
        }
        public void SetLockExecuteQuantity()
        {
            lock (sync)
            {
                lockExecuteQuantity = availableQuantity;
                foreach (var leg in Legs)
                {
                    leg.SetLockExecuteQuantity();
                }
            }
        }
        public void RealizeLockExecuteQuantiy()
        {
            lock (sync)
            {
                bool hasExcess = this.AllowPartialFill && askQuantity - lockExecuteQuantity > 0;
                lockExecuteQuantity = 0;
                foreach (var leg in Legs)
                {
                    leg.RealizeLockExecuteQuantity();
                }
                if (availableQuantity > 0 && hasExcess && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("AvailableQuantity"));
            }
        }
        private long availableQuantity
        {
            get { return askQuantity - lockQuantity - lockExecuteQuantity; }
        }
        public long AvailableQuantity
        {
            get
            {
                lock (sync)
                {
                    return availableQuantity;
                }
            }
        }
        public long AvailableQuantityDirty
        {
            get { return availableQuantity; }
        }
        public void RealizeLock(long quanity, ThreadSafeAskLeg leg = null)
        {
            lock (sync)
            {
                if (IsBuy)
                    Ask.BuyQuantity -= quanity;
                else
                    Ask.SellQuantity -= quanity;
                lockQuantity -= quanity;
                if (leg != null)
                {
                    double ratio = leg.IsBuy ? (double)leg.BuyRatio / (double)leg.SellRatio : (double)leg.SellRatio / (double)leg.BuyRatio;
                    long legLock = leg.IsBuy ? (ratio * quanity).ToCeilingInt() : (ratio * quanity).ToFlooredInt();
                    leg.RealizeLock(legLock);
                }
            }
        }
        public bool FillOrder(long orderQuantity)
        {
            lock (sync)
            {
                if (orderQuantity >= askQuantity || Ask.AllowPartialFill)
                {
                    if (IsBuy)
                        Ask.BuyQuantity -= orderQuantity;
                    else
                        Ask.SellQuantity -= orderQuantity;
                    return true;
                }
                return false;
            }
        }
        public void Update(Ask ask, IAskRepository repository)
        {
            lock (sync)
            {
                this.Ask.AllowPartialFill = ask.AllowPartialFill;
                this.Ask.ApplyCommissionToBuy = ask.ApplyCommissionToBuy;
                this.Ask.BuyQuantity = ask.BuyQuantity;
                this.Ask.SellQuantity = ask.SellQuantity;
                this.Ask.BuyRatio = ask.BuyRatio;
                this.Ask.SellRatio = ask.SellRatio;
                this.Ask.ValidToDate = ask.ValidToDate;
                this.Ask.MinBuyQuantity = ask.MinBuyQuantity;
                this.Ask.MinSellQuantity = ask.MinSellQuantity;
                repository.UpdateAsk(this.Ask);
            }
        }

        
    }
}
