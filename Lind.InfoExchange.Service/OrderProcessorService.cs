using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Description;
using Lind.InfoExchange.Service.WCF;
using OService = Lind.InfoExchange.Service.WCF.OrderProcessorService;
using Lind.InfoExchange.Business;
using log4net;

namespace Lind.InfoExchange.Service
{
    public partial class OrderProcessorService : ServiceBase
    {
        static OrderProcessorService()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
        public OrderProcessorService()
        {
            InitializeComponent();
        }
        private static readonly ILog Log = LogManager.GetLogger(typeof(OrderProcessorService));
        private ServiceHost Host { get; set; }
        protected override void OnStart(string[] args)
        {
            StartProcessor();
        }
        private async void StartProcessor()
        {
            try
            {
                await OrderProcessor.Singleton.Start();
                Task askProc = OrderProcessor.Singleton.AskProcessor();
                if (Host != null)
                    Host.Close();
                string http = "http://localhost:6666/InfoExchange/OrderProcessor";
                string tcp = "net.tcp://localhost:6667/InfoExchange/OrderProcessor";

                Uri[] addresses = { new Uri(http), new Uri(tcp) };
                Host = new ServiceHost(typeof(OService), addresses);
                ServiceMetadataBehavior metaBehavior = new ServiceMetadataBehavior();
                Host.Description.Behaviors.Add(metaBehavior);

                BasicHttpBinding httpb = new BasicHttpBinding();
                Host.AddServiceEndpoint(typeof(IOrderProcessorService), httpb, http);
                Host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

                NetTcpBinding tcpb = new NetTcpBinding();
                Host.AddServiceEndpoint(typeof(IOrderProcessorService), tcpb, tcp);
                Host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");

                Host.Open();
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't Start", ex);
                Stop();
            }
        }
        protected override void OnStop()
        {
            if (Host != null)
            {
                Host.Close();
                Host = null;
            }
            Task.Factory.StartNew(() => OrderProcessor.Singleton.Dispose());
        }
    }
}
