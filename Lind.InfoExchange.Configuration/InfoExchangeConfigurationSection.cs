using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Lind.InfoExchange.Configuration
{
    public class InfoExchangeConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("commissionPercentage")]
        public double CommissionPercentage
        {
            get { return (double)this["commissionPercentage"]; }
            set { this["commissionPercentage"] = value; }
        }

        public static InfoExchangeConfigurationSection Section
        {
            get
            {
                return (InfoExchangeConfigurationSection)ConfigurationManager.GetSection("infoExchange");
            }
        }
    }
}
