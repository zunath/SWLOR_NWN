using System;
using System.Linq;
using System.Reflection;
using Autofac;
using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Mod.Contracts;
using SWLOR.Game.Server.AreaInstance.Contracts;
using SWLOR.Game.Server.DoorRule.Contracts;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.SpawnRule.Contracts;
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

        public delegate T2 AppResolveDelegate<in T1, out T2>(T1 obj);
        public static T2 ResolveByInterface<T1, T2>(string typeName, AppResolveDelegate<T1, T2> action)
        {
            T2 result;
            if (!typeof(T1).IsInterface)
            {
                throw new Exception(nameof(T1) + " must be an interface.");
            }
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            typeName = typeName.Replace(assemblyName + ".", string.Empty);
            string @namespace = assemblyName + "." + typeName;

            using (new Profiler(typeName))
            {
                using (var scope = _container.BeginLifetimeScope())
                {
                    var resolved = scope.ResolveKeyed<T1>(@namespace);

                    try
                    {
                        result = action.Invoke(resolved);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError(ex, typeof(T1).ToString());
                        throw;
                    }
                }
            }
            
            return result;
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
            RegisterInterfaceImplementations<IConversation>(builder);
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
