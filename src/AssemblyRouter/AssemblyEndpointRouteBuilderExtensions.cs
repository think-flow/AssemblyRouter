using AssemblyRouter.ApplicationModels;
using AssemblyRouter.Builder;
using AssemblyRouter.Infrastructure;
using AssemblyRouter.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public static class AssemblyEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Adds endpoints for controller actions of specifying assembly to the <see cref="IEndpointRouteBuilder"/> without specifying any routes.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="applicationPart">The specifying assembly</param>
        /// <returns>
        /// An <see cref="AssemblyControllerActionEndpointConventionBuilder"/> for endpoints associated with controller actions.
        /// </returns>
        public static AssemblyControllerActionEndpointConventionBuilder MapAssemblyControllers(
           this IEndpointRouteBuilder endpoints,
           Assembly applicationPart)
        {
            return MapAssemblyControllers(endpoints, new[] { applicationPart });
        }

        /// <summary>
        /// Adds endpoints for controller actions of specifying assemblies to the <see cref="IEndpointRouteBuilder"/> without specifying any routes.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="applicationParts">The specifying assemblies</param>
        /// <returns>
        /// An <see cref="AssemblyControllerActionEndpointConventionBuilder"/> for endpoints associated with controller actions.
        /// </returns>
        public static AssemblyControllerActionEndpointConventionBuilder MapAssemblyControllers(
            this IEndpointRouteBuilder endpoints,
            ICollection<Assembly> applicationParts)
        {
            if (applicationParts == null)
            {
                throw new ArgumentNullException(nameof(applicationParts));
            }

            if (applicationParts.Count < 1)
            {
                throw new ArgumentException("element cannot be empty", nameof(applicationParts));
            }

            EnsureControllerServices(endpoints);

            return GetOrCreateDataSource(endpoints, applicationParts).DefaultBuilder;
        }

        /// <summary>
        /// Adds endpoints for controller actions of specifying assembly to the <see cref="IEndpointRouteBuilder"/> and specifies a route
        /// with the given <paramref name="name"/>, <paramref name="pattern"/>,
        /// <paramref name="defaults"/>, <paramref name="constraints"/>, and <paramref name="dataTokens"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="applicationPart">The specifying assembly</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="pattern">The URL pattern of the route.</param>
        /// <param name="defaults">
        /// An object that contains default values for route parameters. The object's properties represent the
        /// names and values of the default values.
        /// </param>
        /// <param name="constraints">
        /// An object that contains constraints for the route. The object's properties represent the names and
        /// values of the constraints.
        /// </param>
        /// <param name="dataTokens">
        /// An object that contains data tokens for the route. The object's properties represent the names and
        /// values of the data tokens.
        /// </param>
        /// <returns>
        /// An <see cref="AssemblyControllerActionEndpointConventionBuilder"/> for endpoints associated with controller actions for this route.
        /// </returns>
        public static AssemblyControllerActionEndpointConventionBuilder MapAssemblyControllerRoute(
           this IEndpointRouteBuilder endpoints,
           Assembly applicationPart,
           string name,
           string pattern,
           object defaults = null,
           object constraints = null,
           object dataTokens = null)
        {
            return MapAssemblyControllerRoute(endpoints, new[] { applicationPart }, name, pattern, defaults, constraints, dataTokens);
        }

        /// <summary>
        /// Adds endpoints for controller actions of specifying assemblies to the <see cref="IEndpointRouteBuilder"/> and specifies a route
        /// with the given <paramref name="name"/>, <paramref name="pattern"/>,
        /// <paramref name="defaults"/>, <paramref name="constraints"/>, and <paramref name="dataTokens"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="applicationParts">The specifying assemblies</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="pattern">The URL pattern of the route.</param>
        /// <param name="defaults">
        /// An object that contains default values for route parameters. The object's properties represent the
        /// names and values of the default values.
        /// </param>
        /// <param name="constraints">
        /// An object that contains constraints for the route. The object's properties represent the names and
        /// values of the constraints.
        /// </param>
        /// <param name="dataTokens">
        /// An object that contains data tokens for the route. The object's properties represent the names and
        /// values of the data tokens.
        /// </param>
        /// <returns>
        /// An <see cref="AssemblyControllerActionEndpointConventionBuilder"/> for endpoints associated with controller actions for this route.
        /// </returns>
        public static AssemblyControllerActionEndpointConventionBuilder MapAssemblyControllerRoute(
            this IEndpointRouteBuilder endpoints,
            ICollection<Assembly> applicationParts,
            string name,
            string pattern,
            object defaults = null,
            object constraints = null,
            object dataTokens = null)
        {
            if (applicationParts == null)
            {
                throw new ArgumentNullException(nameof(applicationParts));
            }

            if (applicationParts.Count < 1)
            {
                throw new ArgumentException("element cannot be empty", nameof(applicationParts));
            }

            EnsureControllerServices(endpoints);

            var dataSource = GetOrCreateDataSource(endpoints, applicationParts);
            return dataSource.AddRoute(
                name,
                pattern,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens));
        }

        private static AssemblyControllerActionEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder endpoints, IEnumerable<Assembly> applicationParts)
        {
            var dataSource = endpoints.DataSources.OfType<AssemblyControllerActionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                var endpontFactory = new ActionEndpointFactoryWrapper(endpoints.ServiceProvider);
                var descriptorProvider = new AssemblyControllerActionDescriptorProvide(endpoints.ServiceProvider);
                var orderSequence = OrderedEndpointsSequenceProvider.GetOrCreateOrderedEndpointsSequenceProvider(endpoints);
                dataSource = new AssemblyControllerActionEndpointDataSource(endpontFactory, descriptorProvider, orderSequence);
                endpoints.DataSources.Add(dataSource);
            }
            foreach (var applicationPart in applicationParts)
            {
                dataSource.ApplicationParts.Add(applicationPart);
            }
            return dataSource;
        }

        private static void EnsureControllerServices(IEndpointRouteBuilder endpoints)
        {
            var marker = endpoints.ServiceProvider.GetService(ReflectionHelper.GetMvcMarkerServiceType());
            if (marker == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddControllers' inside the call to 'ConfigureServices(...)' in the application startup code.”");
            }
        }
    }
}
