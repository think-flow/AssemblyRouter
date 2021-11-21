using AssemblyRouter.ApplicationModels;
using AssemblyRouter.Builder;
using AssemblyRouter.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace AssemblyRouter.Routing
{
    internal class AssemblyControllerActionEndpointDataSource : EndpointDataSource
    {
        private CancellationTokenSource _tokenSource;
        private IChangeToken _changeToken;
        private List<Endpoint> _endpoints;
        private readonly List<ConventionalRouteEntryWrapper> _routes;
        private readonly object Lock = new object();
        private readonly List<Action<EndpointBuilder>> Conventions;

        private readonly ActionEndpointFactoryWrapper _endpointFactory;
        private readonly OrderedEndpointsSequenceProvider _orderSequence;
        private readonly AssemblyControllerActionDescriptorProvide _actionDescriptorProvide;

        public AssemblyControllerActionEndpointDataSource(ActionEndpointFactoryWrapper endpointFactory, AssemblyControllerActionDescriptorProvide actionDescriptorProvide, OrderedEndpointsSequenceProvider orderSequence)
        {
            _endpointFactory = endpointFactory;
            _actionDescriptorProvide = actionDescriptorProvide;
            _orderSequence = orderSequence;
            _routes = new List<ConventionalRouteEntryWrapper>();
            Conventions = new List<Action<EndpointBuilder>>();
            DefaultBuilder = new AssemblyControllerActionEndpointConventionBuilder(Lock, Conventions);
        }

        public bool CreateInertEndpoints { get; set; }

        public HashSet<Assembly> ApplicationParts { get; } = new HashSet<Assembly>();

        public AssemblyControllerActionEndpointConventionBuilder DefaultBuilder { get; }

        public override IChangeToken GetChangeToken()
        {
            Initialize();
            return _changeToken;
        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                Initialize();
                return _endpoints;
            }
        }

        public AssemblyControllerActionEndpointConventionBuilder AddRoute(
            string routeName,
            string pattern,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens)
        {
            lock (Lock)
            {
                var conventions = new List<Action<EndpointBuilder>>();
                _routes.Add(new ConventionalRouteEntryWrapper(routeName, pattern, defaults, constraints, dataTokens, _orderSequence.GetNext(), conventions));
                return new AssemblyControllerActionEndpointConventionBuilder(Lock, conventions);
            }
        }

        private void Initialize()
        {
            if (_endpoints == null)
            {
                lock (Lock)
                {
                    if (_endpoints == null)
                    {
                        UpdateEndpoints();
                    }
                }
            }
        }

        private void UpdateEndpoints()
        {
            lock (Lock)
            {
                var descriptors = _actionDescriptorProvide.GetDescriptor(ApplicationParts);
                var endpoints = CreateEndpoints(descriptors, Conventions);

                // See comments in DefaultActionDescriptorCollectionProvider. These steps are done
                // in a specific order to ensure callers always see a consistent state.

                // Step 1 - capture old token
                var oldCancellationTokenSource = _tokenSource;

                // Step 2 - update endpoints
                _endpoints = endpoints;

                // Step 3 - create new change token
                _tokenSource = new CancellationTokenSource();
                _changeToken = new CancellationChangeToken(_tokenSource.Token);

                // Step 4 - trigger old token
                oldCancellationTokenSource?.Cancel();
            }
        }

        private List<Endpoint> CreateEndpoints(IReadOnlyList<ControllerActionDescriptor> descriptors, IReadOnlyList<Action<EndpointBuilder>> conventions)
        {
            var endpoints = new List<Endpoint>();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // MVC guarantees that when two of it's endpoints have the same route name they are equivalent.
            //
            // However, Endpoint Routing requires Endpoint Names to be unique.
            var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // For each controller action - add the relevant endpoints.
            //
            // 1. If the action is attribute routed, we use that information verbatim
            // 2. If the action is conventional routed
            //      a. Create a *matching only* endpoint for each action X route (if possible)
            //      b. Ignore link generation for now
            for (var i = 0; i < descriptors.Count; i++)
            {
                if (descriptors[i] is ControllerActionDescriptor action)
                {
                    _endpointFactory.AddEndpoints(endpoints, routeNames, action, _routes, conventions, CreateInertEndpoints);

                    if (_routes.Count > 0)
                    {
                        // If we have conventional routes, keep track of the keys so we can create
                        // the link generation routes later.
                        foreach (var kvp in action.RouteValues)
                        {
                            keys.Add(kvp.Key);
                        }
                    }
                }
            }

            // Now create a *link generation only* endpoint for each route. This gives us a very
            // compatible experience to previous versions.
            for (var i = 0; i < _routes.Count; i++)
            {
                var route = _routes[i];
                _endpointFactory.AddConventionalLinkGenerationRoute(endpoints, routeNames, keys, route, conventions);
            }

            return endpoints;
        }
    }
}
