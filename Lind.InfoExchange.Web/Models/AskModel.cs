using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Lind.InfoExchange.Data;

namespace Lind.InfoExchange.Web.Models
{
    public class AskModel : IValidatableObject
    {
        public AskModel(Ask ask)
        {
            AskID = ask.AskID;
            CommodityBuyID = ask.CommodityBuyID;
            CommoditySellID = ask.CommoditySellID;
            SellRatio = ask.SellRatio;
            BuyRatio = ask.BuyRatio;
            AskQuantity = ask.AskQuantity;
            AllowPartialFill = ask.AllowPartialFill;
            ApplyCommissionToBuy = ask.ApplyCommissionToBuy;
        }
        public AskModel() { }
        public long AskID { get; set; }
        public int CommoditySellID { get; set; }
        public int CommodityBuyID { get; set; }
        public long SellRatio { get; set; }
        public long BuyRatio { get; set; }
        public long AskQuantity { get; set; }
        public bool IsMarketQuote { get; set; }
        public bool AllowPartialFill { get; set; }
        public bool ApplyCommissionToBuy { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsMarketQuote && (SellRatio <= 0 || BuyRatio <= 0))
                yield return new ValidationResult("Sell Ratio and Buy Ratio must be greater than 0 if not a market quote.");
            if (AskQuantity <= 0)
                yield return new ValidationResult("Ask Quantity must be positive.");
            if (CommodityBuyID == CommoditySellID)
                yield return new ValidationResult("The commodity buy is the same as sell.");
        }

        public Ask ToAsk()
        {
            return new Ask()
            {
                AskID = AskID,
                CommoditySellID = CommoditySellID,
                CommodityBuyID = CommodityBuyID,
                SellRatio = SellRatio,
                BuyRatio = BuyRatio,
                AskQuantity = AskQuantity,
                AllowPartialFill = AllowPartialFill,
                ApplyCommissionToBuy = ApplyCommissionToBuy,
                UserID = new Guid("D1C03047-7BFA-4135-B8E9-63401C78DE96")
            };
        }
        
    }
}