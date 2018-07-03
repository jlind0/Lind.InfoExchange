using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Lind.InfoExchange.Data;

namespace Lind.InfoExchange.Service.WCF
{
    [DataContract]
    public class AskDTO
    {
        [DataMember]
        public long AskID { get; set; }
        [DataMember]
        public Guid UserID { get; set; }
        [DataMember]
        public int CommoditySellID { get; set; }
        [DataMember]
        public int CommodityBuyID { get; set; }
        [DataMember]
        public long SellRatio { get; set; }
        [DataMember]
        public long BuyRatio { get; set; }
        [DataMember]
        public long AskQuantity { get; set; }
        [DataMember]
        public bool AllowPartialFill { get; set; }
        [DataMember]
        public bool ApplyCommissionToBuy { get; set; }
        [DataMember]
        public DateTime AskDate { get; set; }
        [DataMember]
        public DateTime? ValidToDate { get; set; }
        [DataMember]
        public long? MinBuyQuantity { get; set; }
        [DataMember]
        public long? MinSellQuantity { get; set; }
        [DataMember]
        public int MaxLegDepth { get; set; }
        public Ask ToAsk()
        {
            Ask ask = new Ask();
            ask.AskID = AskID;
            ask.UserID = UserID;
            ask.CommoditySellID = CommoditySellID;
            ask.CommodityBuyID = CommodityBuyID;
            ask.SellRatio = SellRatio;
            ask.BuyRatio = BuyRatio;
            ask.AskQuantity = AskQuantity;
            ask.AllowPartialFill = AllowPartialFill;
            ask.ApplyCommissionToBuy = ApplyCommissionToBuy;
            ask.AskDate = AskDate;
            ask.ValidToDate = ValidToDate;
            ask.MinBuyQuantity = MinBuyQuantity;
            ask.MinSellQuantity = MinSellQuantity;
            ask.MaxLegDepth = this.MaxLegDepth;
            return ask;
        }

    }
}
