using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lind.InfoExchange.Data;
using System.Runtime.Serialization;

namespace Lind.InfoExchange.Service.WCF
{
    [DataContract]
    public class OrderDTO
    {
        [DataMember]
        public Guid OrderID { get; set; }
        [DataMember]
        public Guid UserID { get; set; }
        [DataMember]
        public DateTime OrderDate { get; set; }
        [DataMember]
        public int CommodityBuyID { get; set; }
        [DataMember]
        public int CommoditySellID { get; set; }
        [DataMember]
        public int CommissionCommodityID { get; set; }
        [DataMember]
        public long Commission { get; set; }
        [DataMember]
        public ICollection<OrderLegDTO> OrderLegs { get; set; }
        [DataMember]
        public long AskID { get; set; }
        public OrderDTO() { }
        public OrderDTO(Order order)
        {
            this.OrderID = order.OrderID;
            this.UserID = order.UserID;
            this.OrderDate = order.OrderDate;
            this.CommodityBuyID = order.CommodityBuyID;
            this.CommoditySellID = order.CommoditySellID;
            this.CommissionCommodityID = order.CommissionCommodityID;
            this.Commission = order.Commission;
            OrderLegs = new List<OrderLegDTO>();
            foreach (var leg in order.OrderLegs)
            {
                OrderLegs.Add(new OrderLegDTO(leg));
            }
        }
    }

    [DataContract]
    public class OrderLegDTO
    {
        [DataMember]
        public Guid OrderLegID { get; set; }
        [DataMember]
        public Guid OrderID { get; set; }
        [DataMember]
        public Guid BuyerUserID { get; set; }
        [DataMember]
        public int CommoditySellID { get; set; }
        [DataMember]
        public int CommodityBuyID { get; set; }
        [DataMember]
        public long SellQuantity { get; set; }
        [DataMember]
        public long BuyQuantity { get; set; }
        [DataMember]
        public int CommissionCommodityID { get; set; }
        [DataMember]
        public long Commission { get; set; }

        public OrderLegDTO() { }
        public OrderLegDTO(OrderLeg leg)
        {
            this.OrderLegID = leg.OrderLegID;
            this.OrderID = leg.OrderID;
            this.BuyerUserID = leg.BuyerUserID;
            this.CommoditySellID = leg.CommoditySellID;
            this.CommodityBuyID = leg.CommodityBuyID;
            this.SellQuantity = leg.SellQuantity;
            this.BuyQuantity = leg.BuyQuantity;
            this.CommissionCommodityID = leg.CommissionCommodityID;
            this.Commission = leg.Commission;
        }
    }
}
