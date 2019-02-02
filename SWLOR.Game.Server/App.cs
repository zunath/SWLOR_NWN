﻿using System;
using System.Linq;
using System.Net.Http;
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
using SWLOR.Game.Server.AreaInstance.Contracts;
using SWLOR.Game.Server.DoorRule.Contracts;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.Threading;
using SWLOR.Game.Server.Threading.Contracts;

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
                bool success;
                using (var scope = _container.BeginLifetimeScope())
                {
                    IRegisteredEvent @event = scope.ResolveKeyed<IRegisteredEvent>(typeof(T).ToString());
                    success = @event.Run(args);
                }
                return success;
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
                bool success;
                using (var scope = _container.BeginLifetimeScope())
                {
                    IRegisteredEvent @event = scope.ResolveKeyed<IRegisteredEvent>(type.ToString());
                    success = @event.Run(args);
                }

                return success;
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

                try
                {
                    action.Invoke(resolved);
                }
                catch (Exception ex)
                {
                    IErrorService errorService = scope.Resolve<IErrorService>();
                    errorService.LogError(ex, typeof(T).ToString());
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

            using (var scope = _container.BeginLifetimeScope())
            {
                string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                typeName = typeName.Replace(assemblyName + ".", string.Empty);
                string @namespace = assemblyName + "." + typeName;
                var resolved = scope.ResolveKeyed<T1>(@namespace);
                
                try
                {
                    result = action.Invoke(resolved);
                }
                catch (Exception ex)
                {
                    IErrorService errorService = scope.Resolve<IErrorService>();
                    errorService.LogError(ex, typeof(T1).ToString());
                    throw;
                }
            }

            return result;
        }

        public static INWScript GetNWScript()
        {
            return _container.Resolve<INWScript>();
        }

        public static AppCache GetAppState()
        {
            return _container.Resolve<AppCache>();
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

                try
                {
                    action.Invoke(resolved);
                }
                catch(Exception ex)
                {
                    IErrorService errorService = scope.Resolve<IErrorService>();
                    errorService.LogError(ex, typeof(T).ToString());
                }

            }
        }

        public static T2 Resolve<T1, T2>(AppResolveDelegate<T1, T2> action)
        {
            T2 result;
            if (action == null)
            {
                throw new NullReferenceException(nameof(action));
            }

            using (var scope = _container.BeginLifetimeScope())
            {
                T1 resolved = (T1)scope.Resolve(typeof(T1));
                try
                {
                    result = action.Invoke(resolved);
                }
                catch (Exception ex)
                {
                    IErrorService errorService = scope.Resolve<IErrorService>();
                    errorService.LogError(ex, typeof(T1).ToString());
                    throw;
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

            // Instances
            builder.RegisterInstance(new AppCache());
            builder.RegisterType<DatabaseMigrationRunner>()
                .As<IStartable>()
                .SingleInstance();
            builder.RegisterType<BackgroundThreadManager>()
                .As<IBackgroundThreadManager>()
                .SingleInstance();
            
            // Game Objects
            builder.RegisterType<NWObject>();
            builder.RegisterType<NWCreature>();
            builder.RegisterType<NWItem>();
            builder.RegisterType<NWPlayer>();
            builder.RegisterType<NWArea>();
            builder.RegisterType<NWModule>();
            builder.RegisterType<NWPlaceable>();

            // Services
            builder.RegisterType<AbilityService>().As<IAbilityService>().SingleInstance();
            builder.RegisterType<ActivityLoggingService>().As<IActivityLoggingService>().SingleInstance();
            builder.RegisterType<AreaService>().As<IAreaService>().SingleInstance();
            builder.RegisterType<AuthorizationService>().As<IAuthorizationService>().SingleInstance();
            builder.RegisterType<BackgroundService>().As<IBackgroundService>().SingleInstance();
            builder.RegisterType<BasePermissionService>().As<IBasePermissionService>().SingleInstance();
            builder.RegisterType<BaseService>().As<IBaseService>().SingleInstance();
            builder.RegisterType<BehaviourService>().As<IBehaviourService>().SingleInstance();
            builder.RegisterType<ChatCommandService>().As<IChatCommandService>().SingleInstance();
            builder.RegisterType<ChatTextService>().As<IChatTextService>().SingleInstance();
            builder.RegisterType<ColorTokenService>().As<IColorTokenService>().SingleInstance();
            builder.RegisterType<CombatService>().As<ICombatService>().SingleInstance();
            builder.RegisterType<ComponentBonusService>().As<IComponentBonusService>().SingleInstance();
            builder.RegisterType<CraftService>().As<ICraftService>().SingleInstance();
            builder.RegisterType<CreatureCorpseService>().As<ICreatureCorpseService>().SingleInstance();
            builder.RegisterType<CustomEffectService>().As<ICustomEffectService>().SingleInstance();
            builder.RegisterType<DataService>().As<IDataService>().SingleInstance(); // Database processing needs to be a single instance because it holds state.
            builder.RegisterType<DataPackageService>().As<IDataPackageService>().SingleInstance();
            builder.RegisterType<DeathService>().As<IDeathService>().SingleInstance();
            builder.RegisterType<DialogService>().As<IDialogService>().SingleInstance();
            builder.RegisterType<DurabilityService>().As<IDurabilityService>().SingleInstance();
            builder.RegisterType<EmoteStyleService>().As<IEmoteStyleService>().SingleInstance();
            builder.RegisterType<EnmityService>().As<IEnmityService>().SingleInstance();
            builder.RegisterType<ErrorService>().As<IErrorService>().SingleInstance();
            builder.RegisterType<ExaminationService>().As<IExaminationService>().SingleInstance();
            builder.RegisterType<FarmingService>().As<IFarmingService>().SingleInstance();
            builder.RegisterType<HelmetToggleService>().As<IHelmetToggleService>().SingleInstance();
            builder.RegisterType<ImpoundService>().As<IImpoundService>().SingleInstance();
            builder.RegisterType<ItemService>().As<IItemService>().SingleInstance();
            builder.RegisterType<KeyItemService>().As<IKeyItemService>().SingleInstance();
            builder.RegisterType<LanguageService>().As<ILanguageService>().SingleInstance();
            builder.RegisterType<LocalVariableService>().As<ILocalVariableService>().SingleInstance();
            builder.RegisterType<LootService>().As<ILootService>().SingleInstance();
            builder.RegisterType<MapService>().As<IMapService>().SingleInstance();
            builder.RegisterType<MapPinService>().As<IMapPinService>().SingleInstance();
            builder.RegisterType<MenuService>().As<IMenuService>().SingleInstance();
            builder.RegisterType<ModService>().As<IModService>().SingleInstance();
            builder.RegisterType<ObjectProcessingService>().As<IObjectProcessingService>().SingleInstance();
            builder.RegisterType<ObjectVisibilityService>().As<IObjectVisibilityService>().SingleInstance();
            builder.RegisterType<PerkService>().As<IPerkService>().SingleInstance();
            builder.RegisterType<PlayerDescriptionService>().As<IPlayerDescriptionService>().SingleInstance();
            builder.RegisterType<PlayerMigrationService>().As<IPlayerMigrationService>().SingleInstance();
            builder.RegisterType<PlayerValidationService>().As<IPlayerValidationService>().SingleInstance();
            builder.RegisterType<PlayerService>().As<IPlayerService>().SingleInstance();
            builder.RegisterType<PlayerStatService>().As<IPlayerStatService>().SingleInstance();
            builder.RegisterType<PVPSanctuaryService>().As<IPVPSanctuaryService>().SingleInstance();
            builder.RegisterType<QuestService>().As<IQuestService>().SingleInstance();
            builder.RegisterType<RaceService>().As<IRaceService>().SingleInstance();
            builder.RegisterType<RandomService>().As<IRandomService>().SingleInstance(); // Must be single instance to avoid RNG issues
            builder.RegisterType<ResourceService>().As<IResourceService>().SingleInstance();
            builder.RegisterType<SearchService>().As<ISearchService>().SingleInstance();
            builder.RegisterType<SerializationService>().As<ISerializationService>().SingleInstance();
            builder.RegisterType<SkillService>().As<ISkillService>().SingleInstance();
            builder.RegisterType<SpaceService>().As<ISpaceService>().SingleInstance();
            builder.RegisterType<SpawnService>().As<ISpawnService>().SingleInstance();
            builder.RegisterType<TimeService>().As<ITimeService>().SingleInstance();
            builder.RegisterType<WeatherService>().As<IWeatherService>().SingleInstance();
            
            // Background threads
            builder.RegisterType<DatabaseBackgroundThread>().As<IDatabaseThread>().SingleInstance();

            // Interfaces
            RegisterInterfaceImplementations<IRegisteredEvent>(builder);
            RegisterInterfaceImplementations<ICustomEffect>(builder, false, true);
            RegisterInterfaceImplementations<IChatCommand>(builder, true, true);
            RegisterInterfaceImplementations<IConversation>(builder);
            RegisterInterfaceImplementations<IActionItem>(builder, false, true);
            RegisterInterfaceImplementations<IPerk>(builder, false, true);
            RegisterInterfaceImplementations<IBehaviour>(builder);
            RegisterInterfaceImplementations<IMod>(builder, false, true);
            RegisterInterfaceImplementations<ISpawnRule>(builder, false, true);
            RegisterInterfaceImplementations<IQuestRule>(builder, false, true);
            RegisterInterfaceImplementations<IEventProcessor>(builder, false, true);
            RegisterInterfaceImplementations<IDoorRule>(builder, false, true);
            RegisterInterfaceImplementations<IAreaInstance>(builder, false, true);

            // Third Party
            builder.RegisterType<BiowarePosition>().As<IBiowarePosition>().SingleInstance();
            builder.RegisterType<BiowareXP2>().As<IBiowareXP2>().SingleInstance();
            builder.RegisterType<NWNXAdmin>().As<INWNXAdmin>().SingleInstance();
            builder.RegisterType<NWNXChat>().As<INWNXChat>().SingleInstance();
            builder.RegisterType<NWNXCreature>().As<INWNXCreature>().SingleInstance();
            builder.RegisterType<NWNXDamage>().As<INWNXDamage>().SingleInstance();
            builder.RegisterType<NWNXEvents>().As<INWNXEvents>().SingleInstance();
            builder.RegisterType<NWNXItem>().As<INWNXItem>().SingleInstance();
            builder.RegisterType<NWNXObject>().As<INWNXObject>().SingleInstance();
            builder.RegisterType<NWNXItem>().As<INWNXItem>().SingleInstance();
            builder.RegisterType<NWNXPlayer>().As<INWNXPlayer>().SingleInstance();
            builder.RegisterType<NWNXPlayerQuickBarSlot>().As<INWNXPlayerQuickBarSlot>().SingleInstance();
            builder.RegisterType<NWNXProfiler>().As<INWNXProfiler>().SingleInstance();
            builder.RegisterType<NWNXWeapon>().As<INWNXWeapon>().SingleInstance();
            builder.RegisterType<NWScript>().As<INWScript>().SingleInstance();
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
