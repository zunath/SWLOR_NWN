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
using SWLOR.Game.Server.GameObject.Contracts;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Mod.Contracts;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

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
                IRegisteredEvent @event = _container.ResolveKeyed<IRegisteredEvent>(typeof(T).ToString());
                return @event.Run(args);
            }
            catch (Exception ex)
            {
                IErrorService errorService = _container.Resolve<IErrorService>();
                errorService.LogError(ex, typeof(T).ToString());
                throw;
            }
        }

        public static bool RunEvent(Type type, params object[] args)
        {
            try
            {
                IRegisteredEvent @event = _container.ResolveKeyed<IRegisteredEvent>(type.ToString());
                return @event.Run(args);
            }
            catch (Exception ex)
            {
                IErrorService errorService = _container.Resolve<IErrorService>();
                errorService.LogError(ex, type.ToString());
                throw;
            }
        }
        
        public static T ResolveByInterface<T>(string typeName)
        {
            if (!typeof(T).IsInterface)
            {
                throw new Exception(nameof(T) + " must be an interface.");
            }

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            typeName = typeName.Replace(assemblyName + ".", string.Empty);
            string @namespace = assemblyName + "." + typeName;
            return _container.ResolveKeyed<T>(@namespace);
        }

        public static T Resolve<T>()
        {
            return (T)_container.Resolve(typeof(T));
        }
        
        public static bool IsKeyRegistered<T>(string key)
        {
            string @namespace = Assembly.GetExecutingAssembly().GetName().Name + "." + key;
            return _container.IsRegisteredWithKey<T>(@namespace);
        }
        
        private static void BuildIOCContainer()
        {
            var builder = new ContainerBuilder();

            // Instances
            builder.RegisterInstance(new AppState());

            // Types
            builder.RegisterType<DataContext>().As<IDataContext>();

            // Game Objects
            builder.RegisterType<NWObject>().As<INWObject>();
            builder.RegisterType<NWCreature>().As<INWCreature>();
            builder.RegisterType<NWItem>().As<INWItem>();
            builder.RegisterType<NWPlayer>().As<INWPlayer>();
            builder.RegisterType<NWArea>().As<INWArea>();
            builder.RegisterType<NWModule>().As<INWModule>();
            builder.RegisterType<NWPlaceable>().As<INWPlaceable>();

            // Services
            builder.RegisterType<AbilityService>().As<IAbilityService>();
            builder.RegisterType<ActivityLoggingService>().As<IActivityLoggingService>();
            builder.RegisterType<AppStateService>().As<IAppStateService>();
            builder.RegisterType<AreaService>().As<IAreaService>();
            builder.RegisterType<AuthorizationService>().As<IAuthorizationService>();
            builder.RegisterType<BackgroundService>().As<IBackgroundService>();
            builder.RegisterType<BaseService>().As<IBaseService>();
            builder.RegisterType<BehaviourService>().As<IBehaviourService>();
            builder.RegisterType<ChatCommandService>().As<IChatCommandService>();
            builder.RegisterType<ColorTokenService>().As<IColorTokenService>();
            builder.RegisterType<ComponentBonusService>().As<IComponentBonusService>();
            builder.RegisterType<CraftService>().As<ICraftService>();
            builder.RegisterType<CustomEffectService>().As<ICustomEffectService>();
            builder.RegisterType<DeathService>().As<IDeathService>();
            builder.RegisterType<DialogService>().As<IDialogService>();
            builder.RegisterType<DurabilityService>().As<IDurabilityService>();
            builder.RegisterType<EffectTrackerService>().As<IEffectTrackerService>();
            builder.RegisterType<EnmityService>().As<IEnmityService>();
            builder.RegisterType<ErrorService>().As<IErrorService>();
            builder.RegisterType<ExaminationService>().As<IExaminationService>();
            builder.RegisterType<FarmingService>().As<IFarmingService>();
            builder.RegisterType<FoodService>().As<IFoodService>();
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
            builder.RegisterType<ObjectProcessingService>().As<IObjectProcessingService>();
            builder.RegisterType<PerkService>().As<IPerkService>();
            builder.RegisterType<PlayerDescriptionService>().As<IPlayerDescriptionService>();
            builder.RegisterType<PlayerService>().As<IPlayerService>();
            builder.RegisterType<PVPSanctuaryService>().As<IPVPSanctuaryService>();
            builder.RegisterType<QuestService>().As<IQuestService>();
            builder.RegisterType<RandomService>().As<IRandomService>();
            builder.RegisterType<ModService>().As<IModService>();
            builder.RegisterType<SearchService>().As<ISearchService>();
            builder.RegisterType<SerializationService>().As<ISerializationService>();
            builder.RegisterType<SkillService>().As<ISkillService>();
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
            RegisterInterfaceImplementations<IAIComponent>(builder);
            RegisterInterfaceImplementations<IMod>(builder);

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
