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
    public class HomeController : Controller
    {
        private IAskRepository AskRepository { get; set; }
        private ICommodityRepository CommodityRepository { get; set; }
        public HomeController(IAskRepository askRepository = null, ICommodityRepository commodityRepository = null)
        {
            if (askRepository == null)
                AskRepository = new AskRepository();
            if (commodityRepository == null)
                CommodityRepository = new CommodityRepository();
        }
        public HomeController() : this(null, null) { }
        //
        // GET: /Home/

        public ActionResult Index()
        {
            HomeModel model = new HomeModel();
            PopulateModel(model);
            return View(model);
        }

        [HttpPost]
        public ActionResult Index(HomeModel model)
        {
            PopulateModel(model);
            return View(model);
        }

        private void PopulateModel(HomeModel model)
        {
            model.Commodities = new SelectListItem[] { new SelectListItem() { Text = "All", Value = null } }.Union(
                CommodityRepository.GetCommodities().OrderBy(c => c.CommodityName).Select(
                    c => new SelectListItem() { Text = c.CommodityName, Value = c.CommodityID.ToString() }));
            IEnumerable<Ask> asks;
            //if (model.SelectedCommodityID != null)
            //    asks = AskRepository.GetAsks(model.SelectedCommodityID.Value);
            //else
            //    asks = AskRepository.GetAsks();
            //model.Asks = asks.OrderBy(a => a.SellCommodity.CommodityName).ThenBy(a => a.BuyCommodity.CommodityName).ThenBy(a => a.AskDate);
        }
    }
}
