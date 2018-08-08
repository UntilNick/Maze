﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NuGet.Packaging.Core;
using Orcus.Modules.Api;
using Orcus.Modules.Api.Routing;
using Orcus.Service.Commander.Extensions;

namespace Orcus.Service.Commander.Routing
{
    /// <summary>
    ///     Extracts the routes of package controllers
    /// </summary>
    public class RouteCache : IRouteCache
    {
        private const string DefaultMethod = "GET";

        public IImmutableDictionary<RouteDescription, Route> Routes { get; set; }

        public void BuildCache(IReadOnlyDictionary<PackageIdentity, List<Type>> controllers)
        {
            var routes = new Dictionary<RouteDescription, Route>();

#if NETCOREAPP
            foreach (var (package, types) in controllers)
            {
#else
            foreach (var keyValuePair in controllers)
            {
                var package = keyValuePair.Key;
                var types = keyValuePair.Value;
#endif
                foreach (var controllerType in types)
                {
                    if (controllerType.IsAbstract)
                        continue;

                    var routeFragments = new Stack<IRouteFragment>();

                    var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
                    if (routeAttribute != null)
                        routeFragments.Push(routeAttribute);

                    foreach (var methodInfo in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                    {
                        var methodPath = new Stack<IRouteFragment>(routeFragments);

                        var methodAttributes = methodInfo.GetCustomAttributes().ToList();

                        if (methodAttributes.OfType<NonActionAttribute>().Any() ||
                            methodAttributes.OfType<CompilerGeneratedAttribute>().Any())
                            continue;

                        var methodAttribute = methodAttributes.OfType<IActionMethodProvider>().FirstOrDefault();
                        if (methodAttribute == null)
                            continue;

                        var method = methodAttribute.Method;

                        foreach (var routeFragment in methodAttributes.OfType<IRouteFragment>())
                            methodPath.Push(routeFragment);

                        var segments = GetSegments(methodPath, controllerType, methodInfo);
                        var description = new RouteDescription(package, method, segments);
                        routes.Add(description, new Route(description, controllerType, methodInfo));
                    }
                }
            }

            Routes = routes.ToImmutableDictionary();
        }

        public void BuildEmpty()
        {
            Routes = ImmutableDictionary<RouteDescription, Route>.Empty;
        }

        private static string[] GetSegments(IEnumerable<IRouteFragment> fragments, Type controllerType, MethodInfo methodInfo)
        {
            var segments = new List<string>();
            foreach (var routeFragment in fragments)
            {
                if (string.IsNullOrEmpty(routeFragment.Path))
                    continue;

                foreach (var segment in routeFragment.Path.Split(new[] {'/'}))
                {
                    if (segment.First() == '[' && segment.Last() == ']')
                        segments.Add(GetTokenSegmentValue(segment, controllerType, methodInfo));
                    else segments.Add(segment);
                }
            }

            return segments.ToArray();
        }

        private static string GetTokenSegmentValue(string segment, Type controllerType, MethodInfo methodInfo)
        {
            //https://docs.microsoft.com/de-de/aspnet/core/mvc/controllers/routing?view=aspnetcore-2.1#token-replacement-in-route-templates-controller-action-area

            var comparisonType = StringComparison.OrdinalIgnoreCase;
            if (segment.Equals("[controller]", comparisonType))
            {
                var value = controllerType.Name;
                if (value.EndsWith("OrcusController", comparisonType))
                    return value.TrimEnd("OrcusController", comparisonType);

                return value.TrimEnd("Controller", comparisonType);
            }

            if (segment.Equals("[action]", comparisonType))
                return methodInfo.Name.TrimEnd("Action", comparisonType);

            throw new ArgumentException($"Invalid token found: {segment}");
        }
    }
}