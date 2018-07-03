//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Lind.InfoExchange.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class AskLeg
    {
        public long AskLegID { get; set; }
        public long AskID { get; set; }
        public Nullable<int> BuyCommodityID { get; set; }
        public Nullable<int> SellCommodityID { get; set; }
        public long BuyRatio { get; set; }
        public long SellRatio { get; set; }
        public Nullable<long> MinBuyQuantity { get; set; }
        public Nullable<long> MinSellQuantity { get; set; }
        public bool ApplyCommissionToBuy { get; set; }
        public Nullable<long> AvailableBuyQuantity { get; set; }
        public Nullable<long> AvailableSellQuantity { get; set; }
        public Nullable<long> StartingBuyQuantity { get; set; }
        public Nullable<long> StartingSellQuantity { get; set; }
    
        public virtual Commodity CommodityBuy { get; set; }
        public virtual Commodity CommoditySell { get; set; }
        public virtual Ask Ask { get; set; }
    }
}