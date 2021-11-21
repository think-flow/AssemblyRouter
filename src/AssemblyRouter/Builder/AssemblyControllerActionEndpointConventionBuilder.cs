using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;

namespace AssemblyRouter.Builder
{
    public class AssemblyControllerActionEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly object _lock;
        private readonly List<Action<EndpointBuilder>> _conventions;

        internal AssemblyControllerActionEndpointConventionBuilder(object @lock, List<Action<EndpointBuilder>> conventions)
        {
            _lock = @lock;
            _conventions = conventions;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
            }
            lock (_lock)
            {
                _conventions.Add(convention);
            }
        }
    }
}
