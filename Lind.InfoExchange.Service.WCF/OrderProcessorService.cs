using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lind.InfoExchange.Data;
using Lind.InfoExchange.Business;
using System.ServiceModel;

namespace Lind.InfoExchange.Service.WCF
{
    public class OrderProcessorService : IOrderProcessorService
    {
        public OrderDTO ExecuteAsk(AskDTO ask)
        {
            try
            {
                var a = ask.ToAsk();
                var order = OrderProcessor.Singleton.ExecuteAsk(a);
                OrderDTO orderDTO = null;
                if (order != null)
                    orderDTO = new OrderDTO(order);
                else
                    orderDTO = new OrderDTO();
                orderDTO.AskID = a.AskID;
                return orderDTO;
            }
            catch (Exception ex)
            {
                FaultException fx = new FaultException();
                fx.Exception = ex.ToString();
                
                throw new FaultException<FaultException>(fx, "Unhandled exception");
            }
        }

        public void UpdateAsk(AskDTO ask)
        {
            try
            {
                OrderProcessor.Singleton.UpdateAsk(ask.ToAsk());
            }
            catch (Exception ex)
            {
                FaultException fx = new FaultException();
                fx.Exception = ex.ToString();
                throw new FaultException<FaultException>(fx, "Unhandled exception");
            }
        }

        public OrderDTO GetQuote(AskDTO ask)
        {
            try
            {
                var order = OrderProcessor.Singleton.GetQuote(ask.ToAsk());
                if (order != null)
                    return new OrderDTO(order);
                return null;
            }
            catch (Exception ex)
            {
                FaultException fx = new FaultException();
                fx.Exception = ex.ToString();
                throw new FaultException<FaultException>(fx, "Unhandled exception");
            }
        }

        public OrderDTO GetMarketQuote(AskDTO ask)
        {
            try
            {
                var order = OrderProcessor.Singleton.GetMarketQuote(ask.ToAsk());
                if (order != null)
                    return new OrderDTO(order);
                return null;
            }
            catch (Exception ex)
            {
                FaultException fx = new FaultException();
                fx.Exception = ex.ToString();
                throw new FaultException<FaultException>(fx, "Unhandled exception");
            }
        }
    }
}
