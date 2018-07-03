using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Lind.InfoExchange.Data
{
    public interface IAskRepository
    {
        void ExecuteAsk(Ask ask, IEnumerable<Order> orders, IEnumerable<Ask> orderLegAsks, IEnumerable<AskLeg> askLegs);
        IEnumerable<Ask> GetAsks(int commodityID);
        IEnumerable<Ask> GetAsks();
        void UpdateAsk(Ask ask);
        void DeleteAsk(Ask ask);
        void AddAsk(Ask ask);
        Task Start(CancellationTokenSource token);
    }
}
