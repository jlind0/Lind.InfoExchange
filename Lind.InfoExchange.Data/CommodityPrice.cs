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
    
    public partial class CommodityPrice
    {
        public int CommodityID { get; set; }
        public double PriceInGold { get; set; }
    
        public virtual Commodity Commodity { get; set; }
    }
}
