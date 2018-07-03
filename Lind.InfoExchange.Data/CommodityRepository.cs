using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lind.InfoExchange.Data
{
    public class CommodityRepository : ICommodityRepository
    {

        public IEnumerable<Commodity> GetCommodities()
        {
            using (var context = new InfoExchangeContext())
            {
                return context.Commodities.ToArray();
            }
        }

        public Commodity GetCommodity(Guid commodityID)
        {
            using (var context = new InfoExchangeContext())
            {
                return context.Commodities.Find(commodityID);
            }
        }


        public IEnumerable<CommodityPrice> GetCommodityPrices()
        {
            using (var context = new InfoExchangeContext())
            {
                return context.CommodityPrices.ToArray();
            }
        }
    }
}
