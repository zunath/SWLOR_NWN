using System;
using System.Linq;
using System.Reflection;
using Autofac;
using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server
{
    // Compositional root for the app.
    internal static class App
    {
        private static IContainer _container;

        static App()
        {
            BuildIOCContainer();
        }
        
        public delegate void AppResolveDelegate<in T>(T obj);
        public static void ResolveByInterface<T>(string typeName, AppResolveDelegate<T> action)
        {
            if (!typeof(T).IsInterface)
            {
                throw new Exception(nameof(T) + " must be an interface.");
            }

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            typeName = typeName.Replace(assemblyName + ".", string.Empty);
            string @namespace = assemblyName + "." + typeName;
            using (new Profiler(typeName))
            {
                using (var scope = _container.BeginLifetimeScope())
                {
                    var resolved = scope.ResolveKeyed<T>(@namespace);

                    try
                    {
                        action.Invoke(resolved);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError(ex, typeof(T).ToString());
                    }
                }
            }
        }
        
        public static bool IsKeyRegistered<T>(string key)
        {
            bool isRegistered;
            using (var scope = _container.BeginLifetimeScope())
            {
                string @namespace = Assembly.GetExecutingAssembly().GetName().Name + "." + key;
                isRegistered = scope.IsRegisteredWithKey<T>(@namespace);
            }

            return isRegistered;
        }
        
        private static void BuildIOCContainer()
        {
            var builder = new ContainerBuilder();
            
            // Interfaces
            RegisterInterfaceImplementations<IAIBehaviour>(builder);
            
            // Third Party
            builder.RegisterType<BehaviourTreeBuilder>().SingleInstance();
            
            _container = builder.Build();
        }


        private static void RegisterInterfaceImplementations<T>(ContainerBuilder builder, bool lowerCaseKey = false, bool isSingleInstance = false)
        {
            if (!typeof(T).IsInterface)
            {
                throw new Exception("Only interfaces may be used with " + nameof(RegisterInterfaceImplementations));
            }

            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(T).IsAssignableFrom(p) && p.IsClass).ToArray();
            foreach (Type type in classes)
            {
                string key = type.Namespace;
                if (lowerCaseKey) key = key + "." + type.Name.ToLower();
                else key = key + "." + type.Name;

                if (isSingleInstance)
                    builder.RegisterType(type).As<T>().Keyed<T>(key).SingleInstance();
                else
                    builder.RegisterType(type).As<T>().Keyed<T>(key).InstancePerLifetimeScope();
            }
        }
    }
}
