using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lind.InfoExchange.Data;
using Lind.InfoExchange.Business;
using Lind.InfoExchange.Web.Models;

namespace Lind.InfoExchange.Web.Controllers
{
    public class QuoteController : Controller
    {
        //
        // GET: /Quote/
        private ICommodityRepository CommodityRepository { get; set; }
        public QuoteController(ICommodityRepository repository)
        {
            this.CommodityRepository = repository;
        }
        public QuoteController() : this(new CommodityRepository()) { }
        public ActionResult Index()
        {
            QuoteModel model = new QuoteModel();
            PopulateModel(model);
            model.Ask = new AskModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult Index(QuoteModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Ask.IsMarketQuote)
                    model.Order = OrderProcessor.Singleton.GetMarketQuote(model.Ask.ToAsk());
                else
                    model.Order = OrderProcessor.Singleton.GetQuote(model.Ask.ToAsk());
            }
            PopulateModel(model);
            return View(model);
        }

        public ActionResult Order()
        {
            QuoteModel model = new QuoteModel();
            model.IsOrder = true;
            PopulateModel(model);
            return View(model);
        }

        [HttpPost]
        public ActionResult Order(QuoteModel model)
        {
            if (ModelState.IsValid)
                model.Order = OrderProcessor.Singleton.ExecuteAsk(model.Ask.ToAsk());
            PopulateModel(model);
            model.IsOrder = true;
            return View(model);
        }
        private void PopulateModel(QuoteModel model)
        {
            var commodities = CommodityRepository.GetCommodities();
            model.Commodities = commodities.OrderBy(c => c.CommodityName).Select(
                c => new SelectListItem() { Text = c.CommodityName, Value = c.CommodityID.ToString() });
        //    if (model.Order != null)
        //    {
        //        var commoditiesDic = commodities.ToDictionary(c => c.CommodityID);
        //        model.Order.BuyCommodity = commoditiesDic[model.Order.CommodityBuyID];
        //        model.Order.SellCommodity = commoditiesDic[model.Order.CommoditySellID];
        //        model.Order.CommissionCommodity = commoditiesDic[model.Order.CommissionCommodityID];
        //        foreach (var leg in model.Order.OrderLegs)
        //        {
        //            leg.SellCommodity = commoditiesDic[leg.CommoditySellID];
        //            leg.BuyCommodity = commoditiesDic[leg.CommodityBuyID];
        //            leg.CommissionCommodity = commoditiesDic[leg.CommissionCommodityID];
        //        }
        //    }
        }
    }
}
