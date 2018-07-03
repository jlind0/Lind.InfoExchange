using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lind.InfoExchange.Data
{
    public partial class OrderLeg
    {
        public double BuyRatio
        {
            get { return (double)this.BuyQuantity / (double)this.SellQuantity; }
        }
    }
}
