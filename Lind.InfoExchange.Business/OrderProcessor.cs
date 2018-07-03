using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Lind.InfoExchange.Data;
using Lind.InfoExchange.Configuration;
using System.Threading;

namespace Lind.InfoExchange.Business
{
    public class OrderProcessor : IDisposable
    {
        private static readonly Lazy<OrderProcessor> singleton = new Lazy<OrderProcessor>(() => new OrderProcessor(), LazyThreadSafetyMode.PublicationOnly);
        public static OrderProcessor Singleton
        {
            get { return singleton.Value; }
        }
        private IAskRepository AskRepository { get; set; }
        private ICommodityRepository CommodityRepository { get; set; }
        private ConcurrentDictionary<int, ConcurrentDictionary<int, ConcurrentDictionary<long,ThreadSafeAsk>>> OpenAsksByBuyerCommdoity { get; set; }
        private ConcurrentDictionary<long, ThreadSafeAsk> OpenAsks { get; set; }
        private ConcurrentDictionary<long, ThreadSafeAsk> AsksToProcess { get; set; }
        private CancellationTokenSource Token { get; set; }
        public OrderProcessor(IAskRepository askRepository, ICommodityRepository commodityRepository)
        {
            this.AskRepository = askRepository;
            this.CommodityRepository = commodityRepository;
            OpenAsksByBuyerCommdoity = new ConcurrentDictionary<int, ConcurrentDictionary<int, ConcurrentDictionary<long, ThreadSafeAsk>>>();
            OpenAsks = new ConcurrentDictionary<long, ThreadSafeAsk>();
            AsksToProcess = new ConcurrentDictionary<long, ThreadSafeAsk>();
            
        }
        public Task AskRepositoryTask { get; private set; }
        public Task Start()
        {
            return Task.Factory.StartNew(() =>
            {
                if (Token == null)
                    Token = new CancellationTokenSource();
                AskRepositoryTask = AskRepository.Start(Token);
                OpenAsksByBuyerCommdoity.Clear();
                OpenAsks.Clear();
                var commodities = CommodityRepository.GetCommodities();
                Parallel.ForEach(commodities, c => OpenAsksByBuyerCommdoity.AddOrUpdate(c.CommodityID, new ConcurrentDictionary<int, ConcurrentDictionary<long, ThreadSafeAsk>>()  , (k, o) => o));
                Parallel.ForEach(OpenAsksByBuyerCommdoity, buyCommodityDictionary =>
                {
                    foreach (var commodity in commodities)
                    {
                        buyCommodityDictionary.Value.AddOrUpdate(commodity.CommodityID, new ConcurrentDictionary<long, ThreadSafeAsk>(), (k, o) => o);
                    }
                });
                Parallel.ForEach(AskRepository.GetAsks(), ask =>
                {
                    ThreadSafeAsk tAsk = CreateAsk(new ThreadSafeAsk(ask));
                    foreach (var commodityBuyID in tAsk.CommodityBuyID)
                    {
                        foreach (var commoditySellID in tAsk.CommoditySellID)
                        {
                            OpenAsksByBuyerCommdoity[commodityBuyID][commoditySellID].AddOrUpdate(tAsk.AskID, tAsk, (k, o) => o);
                        }
                    }
                    OpenAsks.AddOrUpdate(tAsk.AskID, tAsk, (k, o) => o);
                });
            });
        }
        public Task AskProcessorTask { get; private set; }
        public Task AskProcessor()
        {
            if(Token == null)
                Token = new CancellationTokenSource();
            AskProcessorTask =  Task.Factory.StartNew(() =>
            {
                while (!Token.IsCancellationRequested)
                {
                    foreach(var ask in AsksToProcess.ToArray())
                    {
                        long orderQuantity;
                        if (ask.Value.LockExecuteQuantity == 0)
                        {
                            if (GetQuote(ask.Value, out orderQuantity, maxLegDepth: ask.Value.MaxLegDepth) != null)
                            {
                                ask.Value.RealizeLockExecuteQuantiy();
                                ExecuteAsk(ask.Value, ask.Value.MaxLegDepth);
                            }
                            else
                                ask.Value.RealizeLockExecuteQuantiy();
                        }
                        ThreadSafeAsk tAsk;
                        AsksToProcess.TryRemove(ask.Key, out tAsk);
                    }
                    if(!Token.IsCancellationRequested)
                        Task.Delay(10000).Wait();
                }
            }, Token.Token);
            return AskProcessorTask;
        }
        public OrderProcessor() : this(new AskRepository(), new CommodityRepository()) { }
        public void UpdateAsk(Ask ask)
        {
            ThreadSafeAsk tAsk;
            if (OpenAsks.TryGetValue(ask.AskID, out tAsk))
            {
                if (ask.SellQuantity > 0 || ask.BuyQuantity > 0)
                {
                    tAsk.Update(ask, AskRepository);
                    ExecuteAsk(tAsk);
                }
                else
                {
                    OpenAsks.TryRemove(ask.AskID, out tAsk);
                    foreach (var buyID in tAsk.CommodityBuyID)
                    {
                        foreach (var sellID in tAsk.CommoditySellID)
                        {
                            OpenAsksByBuyerCommdoity[buyID][sellID].TryRemove(ask.AskID, out tAsk);
                        }
                    }
                    
                }
            }
        }
        public IEnumerable<Order> ExecuteAsk(Ask ask)
        {
            ask.StartBuyQuantity = ask.BuyQuantity;
            ask.StartSellQuantity = ask.StartSellQuantity;
            return ExecuteAsk(new ThreadSafeAsk(ask), ask.MaxLegDepth);
        }
        private IEnumerable<Order> ExecuteAsk(ThreadSafeAsk ask, int maxLegDepth = 5)
        {
            long orderQuantity;
            var orders = GetQuote(ask, out orderQuantity, maxLegDepth, true);
            ThreadSafeAsk tAsk;
            if (ask.AskID == 0 || !OpenAsks.TryGetValue(ask.AskID, out tAsk))
            {
                this.AskRepository.AddAsk(ask.Ask);
                tAsk = CreateAsk(ask);
                this.OpenAsks[ask.AskID] = tAsk;
                foreach (var buyID in tAsk.CommodityBuyID)
                {
                    foreach (var sellID in tAsk.CommoditySellID)
                    {
                        this.OpenAsksByBuyerCommdoity[buyID][sellID][ask.AskID] = tAsk;
                    }
                }
                
            }
            List<Ask> orderLegAsks = new List<Ask>();
            List<AskLeg> legs = new List<AskLeg>();
            if (orders != null)
            {
                if (!tAsk.FillOrder(orderQuantity))
                    orders = null;
                tAsk.RealizeLockExecuteQuantiy();
                if (tAsk.AskQuantity <= 0)
                {
                    if (this.OpenAsks.TryRemove(tAsk.AskID, out tAsk))
                    {
                        foreach (var buyID in tAsk.CommodityBuyID)
                        {
                            foreach (var sellID in tAsk.CommoditySellID)
                            {
                                this.OpenAsksByBuyerCommdoity[buyID][sellID].TryRemove(tAsk.AskID, out tAsk);
                            }
                        }
                        
                    }
                }
                
                if (orders != null)
                {
                    
                    foreach (var order in orders)
                    {
                        order.AskID = tAsk.AskID;
                        order.OrderID = Guid.NewGuid();
                        foreach (var orderLeg in order.OrderLegs)
                        {
                            orderLeg.OrderID = order.OrderID;
                            orderLeg.OrderLegID = Guid.NewGuid();
                            ThreadSafeAsk orderAsk;
                            if (OpenAsks.TryGetValue(orderLeg.AskID, out orderAsk))
                            {
                                orderAsk.RealizeLock(orderAsk.IsBuy ? orderLeg.SellQuantity : orderLeg.BuyQuantity);
                                if (orderAsk.Legs.Count > 0)
                                {
                                    var leg = orderAsk.Legs.Where(l => orderAsk.IsBuy ? l.CommodityID == orderLeg.CommodityBuyID : l.CommodityID == orderLeg.CommoditySellID).Single();
                                    leg.RealizeLock(orderAsk.IsBuy ? orderLeg.BuyQuantity : orderLeg.SellQuantity);
                                    legs.Add(leg.Leg);
                                }
                                if (orderAsk.AskQuantity <= 0)
                                {
                                    if (this.OpenAsks.TryRemove(orderAsk.AskID, out orderAsk))
                                    {
                                        foreach (var buyID in tAsk.CommodityBuyID)
                                        {
                                            foreach (var sellID in tAsk.CommoditySellID)
                                            {
                                                this.OpenAsksByBuyerCommdoity[buyID][sellID].TryRemove(tAsk.AskID, out orderAsk);
                                            }
                                        }
                                    }
                                    if (orderAsk != null)
                                        DisposeAsk(orderAsk);
                                }
                                if (orderAsk != null)
                                    orderLegAsks.Add(orderAsk.Ask);
                            }

                        }
                    }
                }
            }
            else
                tAsk.RealizeLockExecuteQuantiy();
            AskRepository.ExecuteAsk(ask.Ask, orders, orderLegAsks, legs);
            return orders;
        }
        private ThreadSafeAsk CreateAsk(ThreadSafeAsk ask)
        {
            ask.PropertyChanged += ThreadSafeAsk_PropertyChanged;
            return ask;
        }
        private void DisposeAsk(ThreadSafeAsk ask)
        {
            ask.PropertyChanged -= ThreadSafeAsk_PropertyChanged;
        }
        private void ThreadSafeAsk_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ThreadSafeAsk ask = (ThreadSafeAsk)sender;
            AsksToProcess.AddOrUpdate(ask.AskID, ask, (k, a) => a);
        }
        public IEnumerable<Order> GetQuote(Ask ask)
        {
            long orderQuantity;
            return GetQuote(new ThreadSafeAsk(ask), out orderQuantity, ask.MaxLegDepth);
        }
        public IEnumerable<Order> GetMarketQuote(Ask ask)
        {
            long orderQuantity;
            return GetQuote(new ThreadSafeAsk(ask), out orderQuantity, ask.MaxLegDepth, isMarketQuote: true);
        }
        private IEnumerable<Order> GetQuote(ThreadSafeAsk ask, out long orderQuantity, int maxLegDepth = 5, bool doLock = false, bool isMarketQuote = false)
        {
            IEnumerable<Order> orders = null;
            orderQuantity = 0;
            ask.SetLockExecuteQuantity();
            if (ask.LockExecuteQuantity > 0)
            {
                ConcurrentBag<IEnumerable<ProposedOrderLeg>> orderLegs = new ConcurrentBag<IEnumerable<ProposedOrderLeg>>();
                foreach (var sellID in ask.CommoditySellID)
                {
                    BuildOrderLegs(sellID, ask.CommodityBuyID, orderLegs, maxLegDepth: maxLegDepth, doLock: doLock);
                }
                
                if (orderLegs.Count > 0)
                {
                    orders = BuildOrder(ask, orderLegs.Select(orderLeg =>
                        BuildProposedOrder(ask, ask.LockExecuteQuantity, orderLeg, isMarketOrder: isMarketQuote)).ToArray(),
                        out orderQuantity, isMarketQuote, doLock);
                    if(orders.Count() == 0)
                        orders = null;
                }
                IEnumerable<ProposedOrderLeg> leg;
                while (orderLegs.TryTake(out leg))
                    leg = null;
                orderLegs = null;
                GC.Collect();
            }
            return orders;
        }
        private IEnumerable<Order> BuildOrder(ThreadSafeAsk ask, IEnumerable<ProposedOrder> orders, out long orderQuanity, bool isMarketQuote = false, bool doLock = false)
        {
            orderQuanity = 0;
            Dictionary<Tuple<int, int>, Order> finalOrders = new Dictionary<Tuple<int, int>, Order>();
            Dictionary<long, Tuple<ThreadSafeAsk, long>> asksInOrder = new Dictionary<long, Tuple<ThreadSafeAsk, long>>();
            var orderBuyRatios = orders.GroupBy(o => new Tuple<int, int>(o.SellCommodityID, o.BuyCommodityID)).Select(g => new { SellBuyCommodities = g.Key, AskBuyRatio = ask.GetBuyRatio(g.Key.Item1, g.Key.Item2), 
                AverageBuyRatio = g.Average(o => o.BuyRatio) }).ToDictionary(g => g.SellBuyCommodities);
            foreach (var propOrder in orders.Where(
                o => o.IsValidOrder && (isMarketQuote || o.BuyRatio >= orderBuyRatios[new Tuple<int, int>(o.SellCommodityID, o.BuyCommodityID)].AskBuyRatio)).OrderByDescending(o => isMarketQuote ? (o.BuyRatio - orderBuyRatios[new Tuple<int, int>(o.SellCommodityID, o.BuyCommodityID)].AverageBuyRatio) / orderBuyRatios[new Tuple<int, int>(o.SellCommodityID, o.BuyCommodityID)].AverageBuyRatio : (o.BuyRatio - orderBuyRatios[new Tuple<int, int>(o.SellCommodityID, o.BuyCommodityID)].AskBuyRatio) / orderBuyRatios[new Tuple<int, int>(o.SellCommodityID, o.BuyCommodityID)].AskBuyRatio))
            {
                ClearProposedOrderLegs(propOrder.OrderLegs);
                ProposedOrder newOrder = BuildProposedOrder(ask, ask.LockExecuteQuantity - orderQuanity, propOrder.OrderLegs, useLock: doLock);
                if (newOrder.IsValidOrder && (isMarketQuote || newOrder.BuyRatio >= ask.GetBuyRatio(newOrder.SellCommodityID, newOrder.BuyCommodityID)))
                {
                    foreach (var leg in BuildOrderLegs(ask, newOrder, asksInOrder, ref orderQuanity, doLock, isMarketQuote))
                    {
                        Order order = null;
                        if (!finalOrders.ContainsKey(new Tuple<int, int>(newOrder.SellCommodityID, newOrder.BuyCommodityID)))
                        {
                            order = new Order();
                            order.CommodityBuyID = newOrder.BuyCommodityID;
                            order.CommoditySellID = newOrder.SellCommodityID;
                            order.CommissionCommodityID = newOrder.Ask.GetApplyCommissionToBuy(newOrder.SellCommodityID, newOrder.BuyCommodityID) ? newOrder.BuyCommodityID : newOrder.SellCommodityID;
                            finalOrders[new Tuple<int, int>(newOrder.SellCommodityID, newOrder.BuyCommodityID)] = order;
                        }
                        else
                            order = finalOrders[new Tuple<int, int>(newOrder.SellCommodityID, newOrder.BuyCommodityID)];
                        order.OrderLegs.Add(leg);
                    }
                }
                if (orderQuanity >= ask.LockExecuteQuantity)
                    break;
            }
            orders = null;
            foreach (var order in finalOrders)
            {
                ApplyCommission(order.Value, ask.GetApplyCommissionToBuy(order.Key.Item1, order.Key.Item2));
            }
            return finalOrders.Values;
        }
        private IEnumerable<OrderLeg> BuildOrderLegs(ThreadSafeAsk ask, ProposedOrder order, Dictionary<long, Tuple<ThreadSafeAsk, long>> asksInOrder, ref long orderQuanity, bool doLock = false, bool isMarketQuote = false)
        {
            bool hasBrokenLeg = false;
            List<OrderLeg> legs = new List<OrderLeg>();
            IEnumerable<ProposedOrderLeg> orderLegs = null;
            if ((ask.IsBuy ? order.ProposedQuanityBuy : order.ProposedQuantitySell) + orderQuanity <= ask.LockExecuteQuantity)
                orderLegs = ProcessOrderLegs(order.OrderLegs, legs, asksInOrder, out hasBrokenLeg);
            else if (orderQuanity < ask.LockExecuteQuantity)
            {
                ClearProposedOrderLegs(order.OrderLegs, useLock: doLock);
                var newOrder = BuildProposedOrder(ask, ask.LockExecuteQuantity - orderQuanity, order.OrderLegs, useLock: doLock);
                if (newOrder.IsValidOrder && (isMarketQuote || newOrder.BuyRatio >= ask.GetBuyRatio(newOrder.SellCommodityID, newOrder.BuyCommodityID)))
                    orderLegs = ProcessOrderLegs(newOrder.OrderLegs, legs, asksInOrder, out hasBrokenLeg);
                else
                {
                    ClearProposedOrderLegs(newOrder.OrderLegs, useLock: doLock);
                    return new OrderLeg[] { };
                }
                order = newOrder;
            }
            if (hasBrokenLeg)
            {
                ClearProposedOrderLegs(order.OrderLegs, asksInOrder, useLock: doLock);
                var newOrder = BuildProposedOrder(ask, ask.LockExecuteQuantity - orderQuanity, orderLegs, useLock: doLock);
                legs.Clear();
                if (newOrder.IsValidOrder && (isMarketQuote || newOrder.BuyRatio >= ask.GetBuyRatio(newOrder.SellCommodityID, newOrder.BuyCommodityID)))
                {

                    ProcessOrderLegs(order.OrderLegs, legs, asksInOrder, out hasBrokenLeg);
                    if (hasBrokenLeg)
                    {
                        ClearProposedOrderLegs(newOrder.OrderLegs, asksInOrder, useLock: doLock);
                        return new OrderLeg[] { };
                    }
                }
                else
                {
                    ClearProposedOrderLegs(newOrder.OrderLegs, asksInOrder, useLock: doLock);
                    return new OrderLeg[] { };
                }
            }
            orderQuanity += ask.IsBuy ? legs.Where(l => l.CommodityBuyID == order.BuyCommodityID).Sum(l => l.BuyQuantity) : legs.Where(l => l.CommoditySellID == order.SellCommodityID).Sum(l => l.SellQuantity);
            return legs;
        }
        private IEnumerable<ProposedOrderLeg> ProcessOrderLegs(IEnumerable<ProposedOrderLeg> orderLegs, List<OrderLeg> legs, 
            Dictionary<long, Tuple<ThreadSafeAsk, long>> asksInOrder, out bool hasBrokenLeg)
        {
            List<ProposedOrderLeg> propOrderLegs = new List<ProposedOrderLeg>();
            hasBrokenLeg = false;
            foreach (var orderLeg in orderLegs)
            {
                Tuple<ThreadSafeAsk, long> orderLegValue;
                if (!asksInOrder.TryGetValue(orderLeg.Ask.AskID, out orderLegValue))
                {
                    orderLegValue = new Tuple<ThreadSafeAsk, long>(orderLeg.Ask, 0);
                    asksInOrder.Add(orderLeg.Ask.AskID, orderLegValue);
                }
                asksInOrder[orderLeg.Ask.AskID] = new Tuple<ThreadSafeAsk, long>(orderLeg.Ask, orderLegValue.Item2 + (orderLeg.Ask.IsBuy ? orderLeg.ProposedSellQuantity : orderLeg.ProposedBuyQuantity));
                if ((orderLeg.ProposedSellQuantity > 0 && orderLeg.ProposedBuyQuantity > 0) && 
                    (orderLeg.DoLock || ((orderLeg.Ask.IsBuy ? orderLeg.ProposedSellQuantity : orderLeg.ProposedBuyQuantity) + orderLegValue.Item2) <= orderLegValue.Item1.AvailableQuantity))
                {
                    var leg = orderLeg.ToOrderLeg();
                    ApplyCommissions(leg, orderLeg.ApplyCommisionToBuy);
                    legs.Add(leg);
                    
                }
                else
                {
                    hasBrokenLeg = true;
                }

                propOrderLegs.Add(orderLeg);
            }
            return propOrderLegs;
        }
        private void ClearProposedOrderLegs(IEnumerable<ProposedOrderLeg> legs, Dictionary<long, Tuple<ThreadSafeAsk, long>> asksInOrder = null, bool useLock = false)
        {
            foreach (var leg in legs)
            {
                if (asksInOrder != null && asksInOrder.ContainsKey(leg.Ask.AskID))
                    asksInOrder[leg.Ask.AskID] = new Tuple<ThreadSafeAsk, long>(leg.Ask, asksInOrder[leg.Ask.AskID].Item2 - (leg.Ask.IsBuy ? leg.ProposedSellQuantity : leg.ProposedBuyQuantity));
                if (!leg.Ask.IsBuy)
                    leg.SetBuyQuantity(-1 * leg.ProposedBuyQuantity, useLock: useLock);
                else
                    leg.SetSellQuantity(-1 * leg.ProposedSellQuantity, useLock: useLock);
            }
        }
        private void ApplyCommissions(OrderLeg leg, bool applyCommissionToBuy)
        {
            leg.CommissionCommodityID = applyCommissionToBuy ? leg.CommoditySellID : leg.CommodityBuyID;
            leg.Commission = (InfoExchangeConfigurationSection.Section.CommissionPercentage *
                (applyCommissionToBuy ? leg.SellQuantity : leg.BuyQuantity)).ToCeilingInt();
        }
        private void ApplyCommission(Order order, bool applyCommisionToBuy)
        {
            long commisionBase = applyCommisionToBuy ? order.OrderLegs.Where(ol => ol.CommodityBuyID == order.CommodityBuyID).Sum(ol => ol.BuyQuantity) :
                order.OrderLegs.Where(ol => ol.CommoditySellID == order.CommoditySellID).Sum(ol => ol.SellQuantity);
            order.Commission = (InfoExchangeConfigurationSection.Section.CommissionPercentage * commisionBase).ToCeilingInt();
        }
        private ProposedOrder BuildProposedOrder(ThreadSafeAsk ask, long askQuantity, IEnumerable<ProposedOrderLeg> orderLegs, bool useLock = false, bool isMarketOrder = false)
        {
            ProposedOrder propOrder = new ProposedOrder(ask);
            if (ask.IsBuy)
                BuildProposedOrderForBuy(ask, askQuantity, orderLegs, propOrder, useLock);
            else
                BuildProposedOrderForSell(ask, askQuantity, orderLegs, propOrder, useLock);
            propOrder.CleanUpOrder();
            if(!isMarketOrder && ask.AllowPartialFill && propOrder.IsValidOrder)
                RemoveWorstOrderLeg(ask, propOrder, useLock);
            
            orderLegs = null;
            return propOrder;
        }
        private void BuildProposedOrderForSell(ThreadSafeAsk ask, long askQuantity, IEnumerable<ProposedOrderLeg> orderLegs, ProposedOrder propOrder, bool useLock = false)
        {
            bool isFirst = true;
            int maxLegLevel = orderLegs.Max(l => l.LegLevel);
            foreach (var legs in orderLegs.GroupBy(o => o.LegLevel).OrderBy(o => o.Key))
            {
                long buyCap = long.MaxValue;
                if (ask.Legs.Count > 0 && legs.Key == maxLegLevel)
                {
                    int commodityBuyID = orderLegs.Where(l => l.LegLevel == maxLegLevel).First().CommodityBuyID;
                    var askLeg = ask.Legs.Where(l => l.CommodityID == commodityBuyID).First();
                    if (askLeg.AvailableQuantityWithoutLockExecute != null)
                        buyCap = askLeg.AvailableQuantityWithoutLockExecute.Value;
                }
                var propOrderLegs = FillProposedOrderLegsForSell(ask, askQuantity, legs, useLock: useLock, buyCap: buyCap);
                if (propOrderLegs.Count == 0)
                    break;
                propOrder.SetOrderLegs(propOrderLegs);
                long sellQuantity = propOrderLegs.Sum(p => p.ProposedSellQuantity);
                if (askQuantity > sellQuantity && sellQuantity > 0 && !isFirst)
                {
                    RebalanceOrderLegsForSell(ask, orderLegs, legs.Key - 1, sellQuantity, propOrder, useLock);
                    var previousOrderLegs = propOrder.GetOrderLegs(legs.Key - 1);

                    if (previousOrderLegs != null)
                    {
                        ClearProposedOrderLegs(propOrderLegs, useLock: useLock);
                        askQuantity = previousOrderLegs.Sum(p => p.ProposedBuyQuantity);
                        propOrderLegs = FillProposedOrderLegsForSell(ask, askQuantity, legs, useLock: useLock);
                        if (propOrderLegs.Count == 0 || askQuantity <= 0)
                            break;
                        propOrder.SetOrderLegs(propOrderLegs);
                    }
                }
                askQuantity = propOrderLegs.Sum(p => p.ProposedBuyQuantity);
                if (askQuantity <= 0 || sellQuantity <= 0)
                    break;
                isFirst = false;
            }
        }
        private void BuildProposedOrderForBuy(ThreadSafeAsk ask, long askQuantity, IEnumerable<ProposedOrderLeg> orderLegs, ProposedOrder propOrder, bool useLock = false)
        {
            bool isFirst = true;
            foreach (var legs in orderLegs.GroupBy(o => o.LegLevel).OrderByDescending(o => o.Key))
            {
                long sellCap = long.MaxValue;
                if (ask.Legs.Count > 0 && legs.Key == 1)
                {
                    int commoditySellID = orderLegs.Where(l => l.LegLevel == 1).First().CommoditySellID;
                    var askLeg = ask.Legs.Where(l => l.CommodityID == commoditySellID).First();
                    if (askLeg.AvailableQuantityWithoutLockExecute != null)
                        sellCap = askLeg.AvailableQuantityWithoutLockExecute.Value;
                }
                var propOrderLegs = FillProposedOrderLegsForBuy(ask, askQuantity, legs, useLock: useLock, sellCap: sellCap);
                if (propOrderLegs.Count == 0)
                    break;
                propOrder.SetOrderLegs(propOrderLegs);
                long sellQuantity = propOrderLegs.Sum(p => p.ProposedBuyQuantity);
                if (askQuantity > sellQuantity && sellQuantity > 0 && !isFirst)
                {
                    RebalanceOrderLegsForBuy(ask, orderLegs, legs.Key + 1, sellQuantity, propOrder, useLock);
                    var previousOrderLegs = propOrder.GetOrderLegs(legs.Key + 1);

                    if (previousOrderLegs != null)
                    {
                        ClearProposedOrderLegs(propOrderLegs, useLock: useLock);
                        askQuantity = previousOrderLegs.Sum(p => p.ProposedSellQuantity);
                        propOrderLegs = FillProposedOrderLegsForBuy(ask, askQuantity, legs, useLock: useLock);
                        if (propOrderLegs.Count == 0 || askQuantity <= 0)
                            break;
                        propOrder.SetOrderLegs(propOrderLegs);
                    }
                }
                askQuantity = propOrderLegs.Sum(p => p.ProposedSellQuantity);
                if (askQuantity <= 0 || sellQuantity <= 0)
                    break;
                isFirst = false;
            }
        }
        private void RemoveWorstOrderLeg(ThreadSafeAsk ask, ProposedOrder propOrder, bool useLock = false)
        {
            while (propOrder.IsValidOrder && propOrder.BuyRatio < ask.GetBuyRatio(propOrder.SellCommodityID, propOrder.BuyCommodityID) && propOrder.ProposedQuanityBuy > 0)
            {
                var worstPerfomingOrderLeg = propOrder.RemoveWorstPerforming(useLock);
                if (worstPerfomingOrderLeg == null)
                    break;
                RebalanceOrder(propOrder, useLock);
                propOrder.CleanUpOrder();
            }
            
        }
        
        private long RebalanceOrderLegForBuy(ProposedOrder propOrder, ProposedOrderLeg orderLeg, long buyQuantity, bool useLock = false)
        {
            long sellQuantityAdj = 0;
            if (!orderLeg.Ask.AllowPartialFill || orderLeg.ProposedBuyQuantity < buyQuantity)
                sellQuantityAdj = -1 * orderLeg.ProposedBuyQuantity;
            else
                sellQuantityAdj = -1 * (orderLeg.ProposedBuyQuantity - buyQuantity);
            sellQuantityAdj = orderLeg.SetBuyQuantity(sellQuantityAdj, useLock: useLock);
            return sellQuantityAdj;
        }
        private long RebalanceOrderLegForSell(ProposedOrder propOrder, ProposedOrderLeg orderLeg, long buyQuantity, bool useLock = false)
        {
            long sellQuantityAdj = 0;
            if (!orderLeg.Ask.AllowPartialFill || orderLeg.ProposedSellQuantity < buyQuantity)
                sellQuantityAdj = -1 * orderLeg.ProposedSellQuantity;
            else
                sellQuantityAdj = -1 * (orderLeg.ProposedSellQuantity - buyQuantity);
            sellQuantityAdj = orderLeg.SetSellQuantity(sellQuantityAdj, useLock: useLock);
            return sellQuantityAdj;
        }
        private void RebalanceOrder(ProposedOrder propOrder, bool useLock = false)
        {
            foreach (var legs in propOrder.OrderLegs.GroupBy(o => o.LegLevel).OrderByDescending(o => o.Key).Skip(1))
            {
                var sellQuantity = legs.Sum(l => l.ProposedSellQuantity);
                var nextLevelOrderLegs = propOrder.GetOrderLegs(legs.Key + 1);
                if (nextLevelOrderLegs == null)
                    break;
                var buyQuantity = nextLevelOrderLegs.Sum(o => o.ProposedBuyQuantity);
                if (buyQuantity == sellQuantity)
                    continue;
                if (sellQuantity <= 0 || buyQuantity <= 0)
                    break;
                if (sellQuantity < buyQuantity)
                {
                    long newBuyQuantity = sellQuantity;
                    foreach (var orderLeg in nextLevelOrderLegs.OrderBy(o => (double)o.ProposedBuyQuantity / (double)o.ProposedSellQuantity))
                    {
                        var adj = RebalanceOrderLegForBuy(propOrder, orderLeg, newBuyQuantity, useLock);
                        buyQuantity += adj;
                        newBuyQuantity -= orderLeg.ProposedBuyQuantity;
                        if (buyQuantity <= sellQuantity)
                            break;
                    }
                }
                else
                {
                    long newSellQuantity = buyQuantity;
                    foreach (var orderLeg in legs.OrderBy(o => (double)o.ProposedBuyQuantity / (double)o.ProposedSellQuantity))
                    {
                        var adj = RebalanceOrderLegForSell(propOrder, orderLeg, newSellQuantity, useLock);
                        sellQuantity += adj;
                        newSellQuantity -= orderLeg.ProposedSellQuantity;
                        if (sellQuantity <= buyQuantity)
                            break;
                    }
                }
                if (!propOrder.OrderQuantitiesInSync && sellQuantity > 0 && buyQuantity > 0)
                {
                    RebalanceOrder(propOrder);
                    break;
                }
                else if (sellQuantity <= 0 || buyQuantity <= 0)
                    break;
            }
        }
        private void RebalanceOrderLegsForBuy(ThreadSafeAsk ask, IEnumerable<ProposedOrderLeg> orderLegs, int legLevel, long sellQuantity, ProposedOrder propOrder, bool useLock = false)
        {
            var previousOrderLegs = propOrder.GetOrderLegs(legLevel);
            while (previousOrderLegs != null)
            {
                ClearProposedOrderLegs(previousOrderLegs, useLock: useLock);
                var newLegs = FillProposedOrderLegsForRebalanceForBuy(ask, sellQuantity, orderLegs.Where(o => o.LegLevel == legLevel), useLock);
                sellQuantity = newLegs.Sum(p => p.ProposedBuyQuantity);
                if(sellQuantity > 0)
                    propOrder.SetOrderLegs(newLegs);
                

                legLevel++;
                previousOrderLegs = propOrder.GetOrderLegs(legLevel);
            }
        }
        private void RebalanceOrderLegsForSell(ThreadSafeAsk ask, IEnumerable<ProposedOrderLeg> orderLegs, int legLevel, long sellQuantity, ProposedOrder propOrder, bool useLock = false)
        {
            var previousOrderLegs = propOrder.GetOrderLegs(legLevel);
            while (previousOrderLegs != null)
            {
                ClearProposedOrderLegs(previousOrderLegs, useLock: useLock);
                var newLegs = FillProposedOrderLegsForRebalanceForSell(ask, sellQuantity, orderLegs.Where(o => o.LegLevel == legLevel), useLock);
                sellQuantity = newLegs.Sum(p => p.ProposedSellQuantity);
                if (sellQuantity > 0)
                    propOrder.SetOrderLegs(newLegs);


                legLevel--;
                previousOrderLegs = propOrder.GetOrderLegs(legLevel);
            }
        }
        private ICollection<ProposedOrderLeg> FillProposedOrderLegsForBuy(ThreadSafeAsk ask, long askQuantity, IEnumerable<ProposedOrderLeg> orderLegs, bool useLock = false, long sellCap = long.MaxValue)
        {
            List<ProposedOrderLeg> propOrderLegs = new List<ProposedOrderLeg>();
            foreach (var leg in orderLegs.OrderBy(l => l.RealBuyRatio).ThenBy(l => l.Ask.AskDate))
            {
                
                leg.SetBuyQuantity(askQuantity, useLock, sellCap);
                if(leg.ProposedBuyQuantity > 0 || leg.ProposedSellQuantity > 0)
                    propOrderLegs.Add(leg);
                askQuantity -= leg.ProposedBuyQuantity;
                sellCap -= leg.ProposedSellQuantity;
                if (askQuantity <= 0)
                    break;
            }
            return propOrderLegs;
        }
        private ICollection<ProposedOrderLeg> FillProposedOrderLegsForSell(ThreadSafeAsk ask, long askQuantity, IEnumerable<ProposedOrderLeg> orderLegs, bool useLock = false, long buyCap = long.MaxValue)
        {
            List<ProposedOrderLeg> propOrderLegs = new List<ProposedOrderLeg>();
            foreach (var leg in orderLegs.OrderBy(l => l.RealBuyRatio).ThenBy(l => l.Ask.AskDate))
            {

                leg.SetSellQuantity(askQuantity, useLock, buyCap);
                if (leg.ProposedBuyQuantity > 0 || leg.ProposedSellQuantity > 0)
                    propOrderLegs.Add(leg);
                askQuantity -= leg.ProposedSellQuantity;
                buyCap -= leg.ProposedBuyQuantity;
                if (askQuantity <= 0)
                    break;
            }
            return propOrderLegs;
        }
        private ICollection<ProposedOrderLeg> FillProposedOrderLegsForRebalanceForBuy(ThreadSafeAsk ask, long buyQuantity, IEnumerable<ProposedOrderLeg> orderLegs, bool useLock = false)
        {
            List<ProposedOrderLeg> propOrderLegs = new List<ProposedOrderLeg>();
            foreach (var leg in orderLegs.OrderBy(l => l.RealBuyRatio).ThenBy(l => l.Ask.AskDate))
            {
                leg.SetSellQuantity(buyQuantity, useLock);
                if (leg.ProposedBuyQuantity > 0 && leg.ProposedSellQuantity > 0)
                    propOrderLegs.Add(leg);
                buyQuantity -= leg.ProposedSellQuantity;

                if (buyQuantity <= 0)
                    break;
            }
            return propOrderLegs;
        }
        private ICollection<ProposedOrderLeg> FillProposedOrderLegsForRebalanceForSell(ThreadSafeAsk ask, long buyQuantity, IEnumerable<ProposedOrderLeg> orderLegs, bool useLock = false)
        {
            List<ProposedOrderLeg> propOrderLegs = new List<ProposedOrderLeg>();
            foreach (var leg in orderLegs.OrderBy(l => l.RealBuyRatio).ThenBy(l => l.Ask.AskDate))
            {
                leg.SetBuyQuantity(buyQuantity, useLock);
                if (leg.ProposedBuyQuantity > 0 && leg.ProposedSellQuantity > 0)
                    propOrderLegs.Add(leg);
                buyQuantity -= leg.ProposedBuyQuantity;

                if (buyQuantity <= 0)
                    break;
            }
            return propOrderLegs;
        }
        private void BuildOrderLegs(int commoditySellID, ICollection<int> commodityBuyID, ConcurrentBag<IEnumerable<ProposedOrderLeg>> orderLegs, 
            Dictionary<int, Tuple<IEnumerable<Tuple<ThreadSafeAsk, ThreadSafeAskLeg>>, int>> proposedOrderLegs = null, int legLevel = 0, int maxLegDepth = 5, bool doLock = false)
        {
            legLevel++;
            var targetCommdoity = commoditySellID;
            ConcurrentDictionary<int, ConcurrentDictionary<long, ThreadSafeAsk>> openAsksByBuyer;
            if (OpenAsksByBuyerCommdoity.TryGetValue(targetCommdoity, out openAsksByBuyer))
            {
                if (proposedOrderLegs == null)
                    proposedOrderLegs = new Dictionary<int, Tuple<IEnumerable<Tuple<ThreadSafeAsk, ThreadSafeAskLeg>>, int>>();
                var openAsks = openAsksByBuyer.Where(a => (legLevel < maxLegDepth || commodityBuyID.Contains(a.Key)) && a.Value.Count > 0 && !proposedOrderLegs.ContainsKey(a.Key)).ToArray();

                if (legLevel == 1)
                    Parallel.ForEach(openAsks, commodityGroup => ProcessCommdoityGroup(commodityBuyID, commodityGroup, proposedOrderLegs, orderLegs, legLevel, maxLegDepth, doLock, targetCommdoity));
                else
                {
                    foreach (var commodityGroup in openAsks)
                    {
                        ProcessCommdoityGroup(commodityBuyID, commodityGroup, proposedOrderLegs, orderLegs, legLevel, maxLegDepth, doLock, targetCommdoity);
                    }
                }
            }
        }
        private void ProcessCommdoityGroup(ICollection<int> commodityBuyID, KeyValuePair<int, ConcurrentDictionary<long, ThreadSafeAsk>> commodityGroup,
            Dictionary<int, Tuple<IEnumerable<Tuple<ThreadSafeAsk, ThreadSafeAskLeg>>, int>> proposedOrderLegs, ConcurrentBag<IEnumerable<ProposedOrderLeg>> orderLegs, 
            int legLevel, int maxLegDepth, bool doLock, int previousTargetCommodity)
        {
            int commdoityGroupKey = commodityGroup.Key;
            Dictionary<int, Tuple<IEnumerable<Tuple<ThreadSafeAsk, ThreadSafeAskLeg>>, int>> groupOrderLegs = new Dictionary<int, Tuple<IEnumerable<Tuple<ThreadSafeAsk, ThreadSafeAskLeg>>, int>>();
            foreach (var legs in proposedOrderLegs)
            {
                groupOrderLegs.Add(legs.Key, legs.Value);
            }
            var buyAsks = commodityGroup.Value.Values.Where(a => a.AvailableQuantityDirty > 0).ToArray();
            if (buyAsks.Length > 0)
            {
                groupOrderLegs.Add(commdoityGroupKey, new Tuple<IEnumerable<Tuple<ThreadSafeAsk, ThreadSafeAskLeg>>, int>(
                    buyAsks.Select(a => new Tuple<ThreadSafeAsk, ThreadSafeAskLeg>(a, a.Legs.Where(l => l.CommodityID == commdoityGroupKey || l.CommodityID == previousTargetCommodity).SingleOrDefault())), legLevel));
                if (commodityBuyID.Contains(commdoityGroupKey))
                    orderLegs.Add(groupOrderLegs.SelectMany(i => i.Value.Item1.Select(a => new ProposedOrderLeg(a.Item1, i.Value.Item2, doLock, a.Item2))));
                if ((commodityBuyID.Count > 1 || !commodityBuyID.Contains(commdoityGroupKey)) && legLevel < maxLegDepth)
                    BuildOrderLegs(commdoityGroupKey, commodityBuyID, orderLegs, groupOrderLegs, legLevel, maxLegDepth, doLock);
               
            }
        }
        public void Dispose()
        {
            if (Token != null)
                Token.Cancel(false);
            if (AskProcessorTask != null)
                AskProcessorTask.Wait();
            if (AskRepositoryTask != null)
                AskRepositoryTask.Wait();
            OpenAsks.Clear();
            OpenAsksByBuyerCommdoity.Clear();
        }
    }
}
