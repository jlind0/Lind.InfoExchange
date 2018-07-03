using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lind.InfoExchange.Data;

namespace Lind.InfoExchange.Web.Models
{
    public class QuoteModel
    {
        public AskModel Ask { get; set; }
        public Order Order { get; set; }
        public IEnumerable<SelectListItem> Commodities { get; set; }
        public bool IsOrder { get; set; }
    }
}