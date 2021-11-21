using AssemblyRouter.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AssemblyRouter.Routing
{
    /// <summary>
    /// wrap ActionEndpointFactory
    /// </summary>
    internal class ActionEndpointFactoryWrapper
    {
        public readonly object Original;

        public static readonly Type OriginalType;

        //存放ActionEndpointFactory.AddEndpoints方法的调用委托
        private static Action<
            object,
            List<Endpoint>,
            HashSet<string>,
            ActionDescriptor,
            IReadOnlyList<ConventionalRouteEntryWrapper>,
            IReadOnlyList<Action<EndpointBuilder>>,
            bool
            > _addEndpointsDelegate;

        //存放ActionEndpointFactory.AddConventionalLinkGenerationRoute方法的调用委托
        private static Action<
            object,
            List<Endpoint>,
            HashSet<string>,
            HashSet<string>,
            ConventionalRouteEntryWrapper,
            IReadOnlyList<Action<EndpointBuilder>>
            > _AddConventionalLinkGenerationRouteDelegate;

        static ActionEndpointFactoryWrapper()
        {
            OriginalType = ReflectionHelper.GetActionEndpointFactoryType();
            CreateAddEndpointsDelegate();
            CreateAddConventionalLinkGenerationRouteDelegate();
        }

        public ActionEndpointFactoryWrapper(IServiceProvider serviceProvider)
        {
            Original = serviceProvider.GetService(OriginalType);
        }

        public void AddEndpoints(
            List<Endpoint> endpoints,
            HashSet<string> routeNames,
            ActionDescriptor action,
            IReadOnlyList<ConventionalRouteEntryWrapper> routes,
            IReadOnlyList<Action<EndpointBuilder>> conventions,
            bool createInertEndpoints)
        {
            //通过委托调用ActionEndpointFactory的AddEndpoints方法来创建Endpoint
            _addEndpointsDelegate.Invoke(Original, endpoints, routeNames, action, routes, conventions, createInertEndpoints);
        }

        public void AddConventionalLinkGenerationRoute(
            List<Endpoint> endpoints,
            HashSet<string> routeNames,
            HashSet<string> keys,
            ConventionalRouteEntryWrapper route,
            IReadOnlyList<Action<EndpointBuilder>> conventions)
        {
            _AddConventionalLinkGenerationRouteDelegate.Invoke(Original, endpoints, routeNames, keys, route, conventions);
        }

        //创建调用ActionEndpointFactory.AddEndpoints方法的委托
        private static void CreateAddEndpointsDelegate()
        {
            var factoryParam = Expression.Parameter(typeof(object), "factory");
            var endpointsParam = Expression.Parameter(typeof(List<Endpoint>), "endpoints");
            var routeNamesParam = Expression.Parameter(typeof(HashSet<string>), "routeNames");
            var actionParam = Expression.Parameter(typeof(ActionDescriptor), "action");
            var routesParam = Expression.Parameter(typeof(IReadOnlyList<ConventionalRouteEntryWrapper>), "routes");
            var conventionsParam = Expression.Parameter(typeof(IReadOnlyList<Action<EndpointBuilder>>), "conventions");
            var createInertEndpointsParam = Expression.Parameter(typeof(bool), "createInertEndpoints");

            //List<ConventionalRouteEntry> originRoutes = new List<ConventionalRouteEntry>(routes.Count);
            var listType = typeof(List<>).MakeGenericType(ConventionalRouteEntryWrapper.OriginalType);
            var countProperym = Expression.Property(routesParam, typeof(IReadOnlyCollection<ConventionalRouteEntryWrapper>).GetProperty("Count"));
            var newListm = Expression.New(listType.GetConstructor(new Type[] { typeof(int) }), countProperym);
            var originRoutesVariablem = Expression.Variable(listType, "originRoutes");
            var originRoutesAssignm = Expression.Assign(originRoutesVariablem, newListm);

            //foreach (var item in routes)
            //{
            //    originRoutes.Add((ConventionalRouteEntry)item.Original);
            //}
            var itemParam = Expression.Parameter(typeof(ConventionalRouteEntryWrapper), "item");
            var OriginFieldm = Expression.Field(itemParam, "Original");
            var callAddm = Expression.Call(originRoutesVariablem, listType.GetMethod("Add"), Expression.Convert(OriginFieldm, ConventionalRouteEntryWrapper.OriginalType));
            var loop = CreateForeachExpression(routesParam, itemParam, callAddm);

            //((ActionEndpointFactory)factory).AddEndpoints(endpoints, routeNames, action, originRoutes, conventions, createInertEndpoints);
            var callAddEndpointsm = Expression.Call(Expression.Convert(factoryParam, OriginalType), OriginalType.GetMethod("AddEndpoints"), new Expression[] { endpointsParam, routeNamesParam, actionParam, originRoutesVariablem, conventionsParam, createInertEndpointsParam });

            var block = Expression.Block(new ParameterExpression[] { originRoutesVariablem }, originRoutesAssignm, loop, callAddEndpointsm);

            _addEndpointsDelegate = Expression.Lambda<Action<object, List<Endpoint>, HashSet<string>, ActionDescriptor, IReadOnlyList<ConventionalRouteEntryWrapper>, IReadOnlyList<Action<EndpointBuilder>>, bool>>(block, new ParameterExpression[] { factoryParam, endpointsParam, routeNamesParam, actionParam, routesParam, conventionsParam, createInertEndpointsParam }).Compile();

            Expression CreateForeachExpression(Expression collection, ParameterExpression loopVar, Expression loopContent)
            {
                var elementType = loopVar.Type;
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
                var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

                var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
                var getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod("GetEnumerator"));
                var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

                // The MoveNext method's actually on IEnumerator, not IEnumerator<T>
                var moveNextCall = Expression.Call(enumeratorVar, typeof(System.Collections.IEnumerator).GetMethod("MoveNext"));

                var breakLabel = Expression.Label("LoopBreak");

                var loop = Expression.Block(new[] { enumeratorVar },
                    enumeratorAssign,
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.Equal(moveNextCall, Expression.Constant(true)),
                            Expression.Block(new[] { loopVar },
                                Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
                                loopContent
                            ),
                            Expression.Break(breakLabel)
                        ),
                    breakLabel)
                );

                return loop;
            }
        }

        private static void CreateAddConventionalLinkGenerationRouteDelegate()
        {
            var factoryParam = Expression.Parameter(typeof(object), "factory");
            var endpointsParam = Expression.Parameter(typeof(List<Endpoint>), "endpoints");
            var routeNamesParam = Expression.Parameter(typeof(HashSet<string>), "routeNames");
            var keysParam = Expression.Parameter(typeof(HashSet<string>), "keys");
            var routeParam = Expression.Parameter(typeof(ConventionalRouteEntryWrapper), "route");
            var conventionsParam = Expression.Parameter(typeof(IReadOnlyList<Action<EndpointBuilder>>), "conventions");

            //ConventionalRouteEntry originRoute = (ConventionalRouteEntry)route.Original;
            var originFieldm = Expression.Field(routeParam, "Original");
            var originRouteVariablem = Expression.Variable(ConventionalRouteEntryWrapper.OriginalType, "originRoute");
            var assignm = Expression.Assign(originRouteVariablem, Expression.Convert(originFieldm, ConventionalRouteEntryWrapper.OriginalType));

            //((ActionEndpointFactory)factory).AddConventionalLinkGenerationRoute(endpoints, routeNames, keys, originRoute, conventions);
            var callAddConventionalm = Expression.Call(Expression.Convert(factoryParam, OriginalType), OriginalType.GetMethod("AddConventionalLinkGenerationRoute"), new Expression[] { endpointsParam, routeNamesParam, keysParam, originRouteVariablem, conventionsParam });

            var block = Expression.Block(new ParameterExpression[] { originRouteVariablem }, assignm, callAddConventionalm);

            _AddConventionalLinkGenerationRouteDelegate = Expression.Lambda<Action<object, List<Endpoint>, HashSet<string>, HashSet<string>, ConventionalRouteEntryWrapper, IReadOnlyList<Action<EndpointBuilder>>>>(block, new ParameterExpression[] { factoryParam, endpointsParam, routeNamesParam, keysParam, routeParam, conventionsParam }).Compile();
        }
    }
}
