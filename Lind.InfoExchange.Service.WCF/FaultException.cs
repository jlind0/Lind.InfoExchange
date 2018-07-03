using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace Lind.InfoExchange.Service.WCF
{
    [DataContract]
    public class FaultException
    {
        [DataMember]
        public string Exception { get; set; }
    }
}
