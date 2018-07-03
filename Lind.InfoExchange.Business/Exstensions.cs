using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lind.InfoExchange.Business
{
    public static class Exstensions
    {
        public static long ToFlooredInt(this double value)
        {
            if (value >= long.MaxValue)
                return long.MaxValue;
            return Convert.ToInt64(Math.Floor(value));
        }

        public static long ToCeilingInt(this double value)
        {
            if (value >= long.MaxValue)
                return long.MaxValue;
            return Convert.ToInt64(Math.Ceiling(value));
        }

    }
}
