using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lind.InfoExchange.Data
{
    public class UserRepository : IUserRepository
    {
        public IEnumerable<User> GetUsers()
        {
            using (var context = new InfoExchangeContext())
            {
                return context.Users.ToArray();
            }
        }
    }
}
