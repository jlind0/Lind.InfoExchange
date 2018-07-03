using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lind.InfoExchange.Business;
using Lind.InfoExchange.Service.WCF;
using System.ServiceModel;
using System.ServiceModel.Description;


namespace Lind.InfoExchange.Service.WCF.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            OrderProcessor.Singleton.Start().Wait();
            //Task askProc = OrderProcessor.Singleton.AskProcessor();
            string http = "http://localhost:6666/InfoExchange/OrderProcessor";
            string tcp = "net.tcp://localhost:6667/InfoExchange/OrderProcessor";

            Uri[] addresses = { new Uri(http), new Uri(tcp) };
            ServiceHost host = new ServiceHost(typeof(OrderProcessorService), addresses);
            ServiceMetadataBehavior metaBehavior = new ServiceMetadataBehavior();
            host.Description.Behaviors.Add(metaBehavior);

            BasicHttpBinding httpb = new BasicHttpBinding();
            host.AddServiceEndpoint(typeof(IOrderProcessorService), httpb, http);
            host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

            NetTcpBinding tcpb = new NetTcpBinding();
            host.AddServiceEndpoint(typeof(IOrderProcessorService), tcpb, tcp);
            host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");

            host.Open();
            Console.WriteLine("Service open");
            Console.ReadLine();
        }
    }
}
