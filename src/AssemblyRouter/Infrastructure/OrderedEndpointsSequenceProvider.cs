using Microsoft.AspNetCore.Routing;
using System.Collections.Concurrent;
using System.Threading;

namespace AssemblyRouter.Infrastructure
{
    internal class OrderedEndpointsSequenceProvider
    {
        private static ConcurrentDictionary<IEndpointRouteBuilder, OrderedEndpointsSequenceProvider> _sequenceProviderCache = new ConcurrentDictionary<IEndpointRouteBuilder, OrderedEndpointsSequenceProvider>();

        private OrderedEndpointsSequenceProvider()
        {

        }

        public static OrderedEndpointsSequenceProvider GetOrCreateOrderedEndpointsSequenceProvider(IEndpointRouteBuilder endpoints)
        {
            return _sequenceProviderCache.GetOrAdd(endpoints, new OrderedEndpointsSequenceProvider());
        }

        // In traditional conventional routing setup, the routes defined by a user have a order
        // defined by how they are added into the list. We would like to maintain the same order when building
        // up the endpoints too.
        //
        // Start with an order of '1' for conventional routes as attribute routes have a default order of '0'.
        // This is for scenarios dealing with migrating existing Router based code to Endpoint Routing world.
        private int _current;

        public int GetNext()
        {
            return Interlocked.Increment(ref _current);
        }
    }
}
