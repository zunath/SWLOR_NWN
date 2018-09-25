using System;
using System.Linq;
using System.Reflection;
using Autofac;
using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Mod.Contracts;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;

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

        public static bool RunEvent<T>(params object[] args)
            where T: IRegisteredEvent
        {
            try
            {
                using (var scope = _container.BeginLifetimeScope())
                {
                    IRegisteredEvent @event = scope.ResolveKeyed<IRegisteredEvent>(typeof(T).ToString());
                    return @event.Run(args);
                }
            }
            catch (Exception ex)
            {
                using (var scope = _container.BeginLifetimeScope())
                {
                    IErrorService errorService = scope.Resolve<IErrorService>();
                    errorService.LogError(ex, typeof(T).ToString());
                }

                throw;
            }
        }

        public static bool RunEvent(Type type, params object[] args)
        {
            try
            {
                using (var scope = _container.BeginLifetimeScope())
                {
                    IRegisteredEvent @event = scope.ResolveKeyed<IRegisteredEvent>(type.ToString());
                    return @event.Run(args);
                }
            }
            catch (Exception ex)
            {
                using (var scope = _container.BeginLifetimeScope())
                {
                    IErrorService errorService = scope.Resolve<IErrorService>();
                    errorService.LogError(ex, type.ToString());
                }

                throw;
            }
        }

        public delegate void AppResolveDelegate<in T>(T obj);
        public static void ResolveByInterface<T>(string typeName, AppResolveDelegate<T> action)
        {
            if (!typeof(T).IsInterface)
            {
                throw new Exception(nameof(T) + " must be an interface.");
            }

            using (var scope = _container.BeginLifetimeScope())
            {
                string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                typeName = typeName.Replace(assemblyName + ".", string.Empty);
                string @namespace = assemblyName + "." + typeName;
                var resolved = scope.ResolveKeyed<T>(@namespace);
                action.Invoke(resolved);
            }
        }

        public delegate T2 AppResolveDelegate<in T1, out T2>(T1 obj);
        public static T2 ResolveByInterface<T1, T2>(string typeName, AppResolveDelegate<T1, T2> action)
        {
            if (!typeof(T1).IsInterface)
            {
                throw new Exception(nameof(T1) + " must be an interface.");
            }

            using (var scope = _container.BeginLifetimeScope())
            {
                string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                typeName = typeName.Replace(assemblyName + ".", string.Empty);
                string @namespace = assemblyName + "." + typeName;
                var resolved = scope.ResolveKeyed<T1>(@namespace);
                return action.Invoke(resolved);
            }

        }

        public static INWScript GetNWScript()
        {
            return _container.Resolve<INWScript>();
        }

        public static AppState GetAppState()
        {
            return _container.Resolve<AppState>();
        }

        public static void Resolve<T>(AppResolveDelegate<T> action)
        {
            if (action == null)
            {
                throw new NullReferenceException(nameof(action));
            }

            using (var scope = _container.BeginLifetimeScope())
            {
                T resolved = (T)scope.Resolve(typeof(T));
                action.Invoke(resolved);
            }
        }

        public static T2 Resolve<T1, T2>(AppResolveDelegate<T1, T2> action)
        {
            if (action == null)
            {
                throw new NullReferenceException(nameof(action));
            }

            using (var scope = _container.BeginLifetimeScope())
            {
                T1 resolved = (T1)scope.Resolve(typeof(T1));
                return action.Invoke(resolved);
            }
        }

        public static ILifetimeScope BeginContainerScope()
        {
            return _container.BeginLifetimeScope();
        }
        
        public static bool IsKeyRegistered<T>(string key)
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                string @namespace = Assembly.GetExecutingAssembly().GetName().Name + "." + key;
                return scope.IsRegisteredWithKey<T>(@namespace);
            }
        }
        
        private static void BuildIOCContainer()
        {
            var builder = new ContainerBuilder();

            // Instances
            builder.RegisterInstance(new AppState());

            // Types
            builder.RegisterType<DataContext>().As<IDataContext>().InstancePerDependency();

            // Game Objects
            builder.RegisterType<NWObject>();
            builder.RegisterType<NWCreature>();
            builder.RegisterType<NWItem>();
            builder.RegisterType<NWPlayer>();
            builder.RegisterType<NWArea>();
            builder.RegisterType<NWModule>();
            builder.RegisterType<NWPlaceable>();

            // Services
            builder.RegisterType<AbilityService>().As<IAbilityService>();
            builder.RegisterType<ActivityLoggingService>().As<IActivityLoggingService>();
            builder.RegisterType<AreaService>().As<IAreaService>();
            builder.RegisterType<AuthorizationService>().As<IAuthorizationService>();
            builder.RegisterType<BackgroundService>().As<IBackgroundService>();
            builder.RegisterType<BasePermissionService>().As<IBasePermissionService>();
            builder.RegisterType<BaseService>().As<IBaseService>();
            builder.RegisterType<BehaviourService>().As<IBehaviourService>();
            builder.RegisterType<ChatCommandService>().As<IChatCommandService>();
            builder.RegisterType<ChatTextService>().As<IChatTextService>();
            builder.RegisterType<ColorTokenService>().As<IColorTokenService>();
            builder.RegisterType<ComponentBonusService>().As<IComponentBonusService>();
            builder.RegisterType<CraftService>().As<ICraftService>();
            builder.RegisterType<CreatureCorpseService>().As<ICreatureCorpseService>();
            builder.RegisterType<CustomEffectService>().As<ICustomEffectService>();
            builder.RegisterType<DeathService>().As<IDeathService>();
            builder.RegisterType<DialogService>().As<IDialogService>();
            builder.RegisterType<DurabilityService>().As<IDurabilityService>();
            builder.RegisterType<PlayerStatService>().As<IPlayerStatService>();
            builder.RegisterType<EnmityService>().As<IEnmityService>();
            builder.RegisterType<ErrorService>().As<IErrorService>();
            builder.RegisterType<ExaminationService>().As<IExaminationService>();
            builder.RegisterType<FarmingService>().As<IFarmingService>();
            builder.RegisterType<HelmetToggleService>().As<IHelmetToggleService>();
            builder.RegisterType<ImpoundService>().As<IImpoundService>();
            builder.RegisterType<ItemService>().As<IItemService>();
            builder.RegisterType<KeyItemService>().As<IKeyItemService>();
            builder.RegisterType<LocalVariableService>().As<ILocalVariableService>();
            builder.RegisterType<LootService>().As<ILootService>();
            builder.RegisterType<MapService>().As<IMapService>();
            builder.RegisterType<MapPinService>().As<IMapPinService>();
            builder.RegisterType<MenuService>().As<IMenuService>();
            builder.RegisterType<MigrationService>().As<IMigrationService>();
            builder.RegisterType<ModService>().As<IModService>();
            builder.RegisterType<ObjectProcessingService>().As<IObjectProcessingService>();
            builder.RegisterType<ObjectVisibilityService>().As<IObjectVisibilityService>();
            builder.RegisterType<PerkService>().As<IPerkService>();
            builder.RegisterType<PlayerDescriptionService>().As<IPlayerDescriptionService>();
            builder.RegisterType<PlayerService>().As<IPlayerService>();
            builder.RegisterType<PVPSanctuaryService>().As<IPVPSanctuaryService>();
            builder.RegisterType<QuestService>().As<IQuestService>();
            builder.RegisterType<RaceService>().As<IRaceService>();
            builder.RegisterType<RandomService>().As<IRandomService>().SingleInstance(); // Must be single instance to avoid RNG issues
            builder.RegisterType<ResourceService>().As<IResourceService>();
            builder.RegisterType<SearchService>().As<ISearchService>();
            builder.RegisterType<SerializationService>().As<ISerializationService>();
            builder.RegisterType<SkillService>().As<ISkillService>().SingleInstance();
            builder.RegisterType<SpawnService>().As<ISpawnService>();
            builder.RegisterType<StorageService>().As<IStorageService>();
            builder.RegisterType<TimeService>().As<ITimeService>();
            
            // Interfaces
            RegisterInterfaceImplementations<IRegisteredEvent>(builder);
            RegisterInterfaceImplementations<ICustomEffect>(builder);
            RegisterInterfaceImplementations<IChatCommand>(builder, true);
            RegisterInterfaceImplementations<IConversation>(builder);
            RegisterInterfaceImplementations<IActionItem>(builder);
            RegisterInterfaceImplementations<IPerk>(builder);
            RegisterInterfaceImplementations<IBehaviour>(builder);
            RegisterInterfaceImplementations<IMod>(builder);
            RegisterInterfaceImplementations<ISpawnRule>(builder);
            RegisterInterfaceImplementations<IQuestRule>(builder);
            RegisterInterfaceImplementations<IEventProcessor>(builder);

            // Third Party
            builder.RegisterType<BiowarePosition>().As<IBiowarePosition>();
            builder.RegisterType<BiowareXP2>().As<IBiowareXP2>();
            builder.RegisterType<NWNXChat>().As<INWNXChat>();
            builder.RegisterType<NWNXCreature>().As<INWNXCreature>();
            builder.RegisterType<NWNXDamage>().As<INWNXDamage>();
            builder.RegisterType<NWNXEvents>().As<INWNXEvents>();
            builder.RegisterType<NWNXItem>().As<INWNXItem>();
            builder.RegisterType<NWNXObject>().As<INWNXObject>();
            builder.RegisterType<NWNXItem>().As<INWNXItem>();
            builder.RegisterType<NWNXPlayer>().As<INWNXPlayer>();
            builder.RegisterType<NWNXPlayerQuickBarSlot>().As<INWNXPlayerQuickBarSlot>();
            builder.RegisterType<NWScript>().As<INWScript>().SingleInstance();
            builder.RegisterType<BehaviourTreeBuilder>().SingleInstance();
            
            _container = builder.Build();
        }


        private static void RegisterInterfaceImplementations<T>(ContainerBuilder builder, bool lowerCaseKey = false)
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
                
                builder.RegisterType(type).As<T>().Keyed<T>(key);
            }
        }
    }
}
