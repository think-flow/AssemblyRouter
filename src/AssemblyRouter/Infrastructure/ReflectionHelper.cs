using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace AssemblyRouter.Infrastructure
{
    internal static class ReflectionHelper
    {
        public const string MvcCoreAssemblyName = "Microsoft.AspNetCore.Mvc.Core";

        public const string ConventionalRouteEntryFullName = "Microsoft.AspNetCore.Mvc.Routing.ConventionalRouteEntry";

        public const string ActionEndpointFactoryFullName = "Microsoft.AspNetCore.Mvc.Routing.ActionEndpointFactory";

        public const string ApplicationModelFactoryFullName = "Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelFactory";

        public const string ControllerActionDescriptorBuilderFullName = "Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerActionDescriptorBuilder";

        public const string MvcMarkerServiceFullName = "Microsoft.Extensions.DependencyInjection.MvcMarkerService";

        public static readonly Assembly MvcCoreAssembly;

        private static readonly ConcurrentDictionary<string, Type> _cache;

        static ReflectionHelper()
        {
            MvcCoreAssembly = Assembly.Load(new AssemblyName(MvcCoreAssemblyName));
            _cache = new ConcurrentDictionary<string, Type>();
        }

        public static Type GetConventionalRouteEntryType() => GetOrAdd(ConventionalRouteEntryFullName);

        public static Type GetActionEndpointFactoryType() => GetOrAdd(ActionEndpointFactoryFullName);

        public static Type GetApplicationModelFactoryType() => GetOrAdd(ApplicationModelFactoryFullName);

        public static Type GetControllerActionDescriptorBuilderType() => GetOrAdd(ControllerActionDescriptorBuilderFullName);

        public static Type GetMvcMarkerServiceType() => GetOrAdd(MvcMarkerServiceFullName);

        private static Type GetOrAdd(string fullName)
        {
            return _cache.GetOrAdd(fullName, name => MvcCoreAssembly.GetTypes().Single(it => it.FullName == name));
        }
    }
}
