using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lind.InfoExchange.Data;

namespace Lind.InfoExchange.Web.Models
{
    public class HomeModel
    {
        public IEnumerable<SelectListItem> Commodities { get; set; }
        public Guid? SelectedCommodityID { get; set; }
        public IEnumerable<Ask> Asks { get; set; }
    }
}