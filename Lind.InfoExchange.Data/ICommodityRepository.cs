using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lind.InfoExchange.Data
{
    public interface ICommodityRepository
    {
        IEnumerable<Commodity> GetCommodities();
        Commodity GetCommodity(Guid commodityID);
        IEnumerable<CommodityPrice> GetCommodityPrices();
    }
}
