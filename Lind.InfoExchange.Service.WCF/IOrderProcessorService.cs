using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Lind.InfoExchange.Data;

namespace Lind.InfoExchange.Service.WCF
{
    [ServiceContract]
    public interface IOrderProcessorService
    {
        [OperationContract]
        [FaultContract(typeof(FaultException))]
        OrderDTO ExecuteAsk(AskDTO ask);
        [OperationContract]
        [FaultContract(typeof(FaultException))]
        void UpdateAsk(AskDTO ask);
        [OperationContract]
        [FaultContract(typeof(FaultException))]
        OrderDTO GetQuote(AskDTO ask);
        [OperationContract]
        [FaultContract(typeof(FaultException))]
        OrderDTO GetMarketQuote(AskDTO ask);
    }
}
