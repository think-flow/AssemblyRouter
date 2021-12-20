using AssemblyRouter.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AssemblyRouter.Routing
{
    /// <summary>
    /// wrap ConventionalRouteEntry
    /// </summary>
    internal readonly struct ConventionalRouteEntryWrapper
    {
        public readonly object Original;

        public static readonly Type OriginalType;

        private static Func<
            string,
            string,
            RouteValueDictionary,
            IDictionary<string, object>,
            RouteValueDictionary,
            int,
            List<Action<EndpointBuilder>>,
            object
            > _newDelegate;

        static ConventionalRouteEntryWrapper()
        {
            OriginalType = ReflectionHelper.GetConventionalRouteEntryType();
            CreateNewDelegate();
        }

        public ConventionalRouteEntryWrapper(
            string routeName,
            string pattern,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens,
            int order,
            List<Action<EndpointBuilder>> conventions)
        {
            Original = _newDelegate.Invoke(routeName, pattern, defaults, constraints, dataTokens, order, conventions);
        }

        private static void CreateNewDelegate()
        {
            var routeNameParam = Expression.Parameter(typeof(string), "routeName");
            var patternParam = Expression.Parameter(typeof(string), "pattern");
            var defaultsParam = Expression.Parameter(typeof(RouteValueDictionary), "defaults");
            var constraintsParam = Expression.Parameter(typeof(IDictionary<string, object>), "constraints");
            var dataTokensParam = Expression.Parameter(typeof(RouteValueDictionary), "dataTokens");
            var orderParam = Expression.Parameter(typeof(int), "order");
            var conventionsParam = Expression.Parameter(typeof(List<Action<EndpointBuilder>>), "conventions");
            var paraArraym = new ParameterExpression[] { routeNameParam, patternParam, defaultsParam, constraintsParam, dataTokensParam, orderParam, conventionsParam };

            //return (object)new ConventionalRoute()
            var callNewm = Expression.New(OriginalType.GetConstructor(new Type[] { typeof(string), typeof(string), typeof(RouteValueDictionary), typeof(IDictionary<string, object>), typeof(RouteValueDictionary), typeof(int), typeof(List<Action<EndpointBuilder>>) }), paraArraym);
            var convertm = Expression.Convert(callNewm, typeof(object));

            _newDelegate = Expression.Lambda<Func<string, string, RouteValueDictionary, IDictionary<string, object>, RouteValueDictionary, int, List<Action<EndpointBuilder>>, object>>(convertm, paraArraym).Compile();
        }
    }
}
