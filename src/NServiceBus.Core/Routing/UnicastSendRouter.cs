namespace NServiceBus
{
    using System;
    using System.Linq;
    using Extensibility;
    using Pipeline;
    using Routing;
    using Transport;

    class UnicastSendRouter
    {
        public enum RouteOption
        {
            None,
            ExplicitDestination,
            RouteToThisInstance,
            RouteToAnyInstanceOfThisEndpoint,
            RouteToSpecificInstance
        }

        public UnicastSendRouter(
            bool isSendOnly,
            string receiveQueueName,
            QueueAddress instanceSpecificQueue,
            IDistributionPolicy defaultDistributionPolicy,
            UnicastRoutingTable unicastRoutingTable,
            EndpointInstances endpointInstances,
            ITransportAddressResolver transportAddressResolver)
        {
            this.isSendOnly = isSendOnly;
            this.receiveQueueName = receiveQueueName;
            if (instanceSpecificQueue != null)
            {
                this.instanceSpecificQueue = new EndpointInstance(instanceSpecificQueue.BaseAddress, instanceSpecificQueue.Discriminator, instanceSpecificQueue.Properties);
            }
            this.defaultDistributionPolicy = defaultDistributionPolicy;
            this.unicastRoutingTable = unicastRoutingTable;
            this.endpointInstances = endpointInstances;
            this.transportAddressResolver = transportAddressResolver;
        }

        public virtual UnicastRoutingStrategy Route(IOutgoingSendContext context)
        {
            if (!context.GetOperationProperties().TryGet(out State state))
            {
                state = new State();
            }

            var route = SelectRoute(state, context);
            return ResolveRoute(route, context);
        }

        UnicastRoute SelectRoute(State state, IOutgoingSendContext context) => state.Option switch
        {
            RouteOption.ExplicitDestination => UnicastRoute.CreateFromPhysicalAddress(state.ExplicitDestination),
            RouteOption.RouteToThisInstance => RouteToThisInstance(),
            RouteOption.RouteToAnyInstanceOfThisEndpoint => RouteToAnyInstance(),
            RouteOption.RouteToSpecificInstance => RouteToSpecificInstance(context, state.SpecificInstance),
            RouteOption.None => RouteUsingTable(context),
            _ => throw new Exception($"Unsupported route option: {state.Option}")
        };

        UnicastRoute RouteToThisInstance()
        {
            if (isSendOnly)
            {
                throw new InvalidOperationException("Cannot route to this instance since the endpoint is configured to be in send-only mode.");
            }

            if (instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
            }

            return UnicastRoute.CreateFromEndpointInstance(instanceSpecificQueue);
        }

        UnicastRoute RouteToAnyInstance()
        {
            if (isSendOnly)
            {
                throw new InvalidOperationException("Cannot route to instances of this endpoint since it's configured to be in send-only mode.");
            }

            return UnicastRoute.CreateFromEndpointName(receiveQueueName);
        }

        UnicastRoute RouteToSpecificInstance(IOutgoingSendContext context, string specificInstance)
        {
            var route = RouteUsingTable(context);
            if (route.Endpoint == null)
            {
                throw new Exception("Routing to a specific instance is only allowed if route is defined for a logical endpoint, not for an address or instance.");
            }
            return UnicastRoute.CreateFromEndpointInstance(new EndpointInstance(route.Endpoint, specificInstance));
        }

        UnicastRoute RouteUsingTable(IOutgoingSendContext context)
        {
            var route = unicastRoutingTable.GetRouteFor(context.Message.MessageType) ?? throw new Exception($"No destination specified for message: {context.Message.MessageType}");
            return route;
        }

        UnicastRoutingStrategy ResolveRoute(UnicastRoute route, IOutgoingSendContext context)
        {
            if (route.PhysicalAddress != null)
            {
                return new UnicastRoutingStrategy(route.PhysicalAddress);
            }
            if (route.Instance != null)
            {
                return new UnicastRoutingStrategy(TranslateTransportAddress(route.Instance));
            }
            var instances = endpointInstances.FindInstances(route.Endpoint).Select(e => TranslateTransportAddress(e)).ToArray();
            var distributionContext = new DistributionContext(instances, context.Message, context.MessageId, context.Headers, transportAddressResolver, context.Extensions);
            var selectedInstanceAddress = defaultDistributionPolicy.GetDistributionStrategy(route.Endpoint, DistributionStrategyScope.Send).SelectDestination(distributionContext);
            return new UnicastRoutingStrategy(selectedInstanceAddress);
        }

        string TranslateTransportAddress(EndpointInstance instance) =>
            transportAddressResolver.ToTransportAddress(new QueueAddress(instance.Endpoint, instance.Discriminator, instance.Properties));

        readonly EndpointInstance instanceSpecificQueue;
        readonly EndpointInstances endpointInstances;
        readonly ITransportAddressResolver transportAddressResolver;
        readonly UnicastRoutingTable unicastRoutingTable;
        readonly IDistributionPolicy defaultDistributionPolicy;
        readonly bool isSendOnly;
        readonly string receiveQueueName;

        public class State
        {
            public string ExplicitDestination { get; set; }
            public string SpecificInstance { get; set; }

            public RouteOption Option
            {
                get => option;
                set
                {
                    if (option != RouteOption.None)
                    {
                        throw new Exception("Already specified routing option for this message: " + option);
                    }
                    option = value;
                }
            }

            RouteOption option;
        }
    }
}