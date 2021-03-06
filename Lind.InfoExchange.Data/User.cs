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
    
    public partial class User
    {
        public User()
        {
            this.Orders = new HashSet<Order>();
            this.OrderLegs = new HashSet<OrderLeg>();
            this.Asks = new HashSet<Ask>();
        }
    
        public System.Guid UserID { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<OrderLeg> OrderLegs { get; set; }
        public virtual ICollection<Ask> Asks { get; set; }
    }
}
