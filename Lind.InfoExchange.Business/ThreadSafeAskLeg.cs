using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lind.InfoExchange.Data;

namespace Lind.InfoExchange.Business
{
    internal class ThreadSafeAskLeg
    {
        public AskLeg Leg { get; private set; }
        public ThreadSafeAsk Ask { get; private set; }
        private readonly object sync;
        public ThreadSafeAskLeg(AskLeg leg, ThreadSafeAsk ask, object sync)
        {
            this.Leg = leg;
            this.sync = sync;
            this.Ask = ask;
        }
        public long BuyRatio
        {
            get { return this.Leg.BuyRatio; }
        }
        public long SellRatio
        {
            get { return this.Leg.SellRatio; }
        }
        public bool IsBuy
        {
            get { return Leg.BuyCommodityID != null; }
        }
        public bool IsSell
        {
            get { return Leg.SellCommodityID != null; }
        }
        public int CommodityID
        {
            get { return IsBuy ? Leg.BuyCommodityID.Value : Leg.SellCommodityID.Value; }
        }
        private long lockQuantity;
        public long LockQuantity
        {
            get { lock (sync)return lockQuantity; }
            private set { lock (sync)lockQuantity = value; }
        }
        private long lockExecuteQuantity;
        public long LockExecuteQuantity
        {
            get { lock (sync)return lockExecuteQuantity; }
            private set { lock (sync) lockExecuteQuantity = value; }
        }
        private long? askQuantity
        {
            get { return this.IsBuy ? this.Leg.AvailableBuyQuantity : this.Leg.AvailableSellQuantity; }
        }
        public long? AskQuantity
        {
            get { lock (sync)return askQuantity; }
        }
        private long? availableQuantityWithoutLockExecute
        {
            get { return askQuantity != null ? askQuantity.Value - lockQuantity : null as long?; }
        }
        public long? AvailableQuantityWithoutLockExecute
        {
            get { lock (sync)return availableQuantityWithoutLockExecute; }
        }
        private long? availableQuantity
        {
            get { return askQuantity != null ? askQuantity.Value - lockQuantity - lockExecuteQuantity : null as long?; }
        }
        public long? AvailableQuantity
        {
            get { lock (sync)return availableQuantity; }
        }
        public void SetLock(long quantity)
        {
            LockQuantity += quantity;
        }
        public void SetLockExecuteQuantity()
        {
            lock (sync)
            {
                if (availableQuantity != null)
                    lockExecuteQuantity = availableQuantity.Value;
            }
        }
        public void RealizeLockExecuteQuantity()
        {
            lockExecuteQuantity = 0;
        }
        public void RealizeLock(long quantity)
        {
            if (askQuantity != null)
            {
                if (this.IsBuy)
                    this.Leg.AvailableBuyQuantity -= quantity;
                else
                    this.Leg.AvailableSellQuantity -= quantity;
            }
            LockQuantity -= quantity;
        }
    }
}
