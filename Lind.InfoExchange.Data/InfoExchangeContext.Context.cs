﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class InfoExchangeContext : DbContext
    {
        public InfoExchangeContext()
            : base("name=InfoExchangeContext")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Commodity> Commodities { get; set; }
        public virtual DbSet<CommodityPrice> CommodityPrices { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderLeg> OrderLegs { get; set; }
        public virtual DbSet<AskLeg> AskLegs { get; set; }
        public virtual DbSet<Ask> Asks { get; set; }
    }
}
