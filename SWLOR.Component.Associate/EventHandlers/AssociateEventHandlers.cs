using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Associate.Events;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Domain.Properties.Events;
using SWLOR.Shared.Domain.Space.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Associate.EventHandlers
{
    /// <summary>
    /// Event handlers for Associate-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class AssociateEventHandlers
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IBeastMasteryService> _beastMasteryService;
        private readonly Lazy<IDroidService> _droidService;

        public AssociateEventHandlers(
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _beastMasteryService = new Lazy<IBeastMasteryService>(() => _serviceProvider.GetRequiredService<IBeastMasteryService>());
            _droidService = new Lazy<IDroidService>(() => _serviceProvider.GetRequiredService<IDroidService>());

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheData());
            eventAggregator.Subscribe<OnCombatPointXPDistribute>(e => CombatPointXPDistributed());
            eventAggregator.Subscribe<OnSpaceEnter>(e => RemoveAssociate());
            eventAggregator.Subscribe<OnBeastTerminate>(e => RemoveAssociate());
            eventAggregator.Subscribe<OnBeastBlocked>(e => BeastOnBlocked());
            eventAggregator.Subscribe<OnBeastRoundEnd>(e => BeastOnEndCombatRound());
            eventAggregator.Subscribe<OnBeastHeartbeat>(e => BeastOnHeartbeat());
            eventAggregator.Subscribe<OnBeastPerception>(e => BeastOnPerception());
            eventAggregator.Subscribe<OnBeastAttacked>(e => BeastOnPhysicalAttacked());
            eventAggregator.Subscribe<OnBeastRest>(e => BeastOnRested());
            eventAggregator.Subscribe<OnBeastSpawn>(e => BeastOnSpawn());
            eventAggregator.Subscribe<OnBeastUserDefined>(e => BeastOnUserDefined());
            eventAggregator.Subscribe<OnBeastConversation>(e => BeastOnConversation());
            eventAggregator.Subscribe<OnBeastDamaged>(e => BeastOnDamaged());
            eventAggregator.Subscribe<OnBeastDeath>(e => BeastOnDeath());
            eventAggregator.Subscribe<OnBeastDisturbed>(e => BeastOnDisturbed());
            eventAggregator.Subscribe<OnEnterProperty>(e => OnEnterPropertyInstance());
            eventAggregator.Subscribe<OnItemEquipValidateBefore>(e => OnItemEquipHandler());
            eventAggregator.Subscribe<OnItemUnequipBefore>(e => OnItemUnequipHandler());
            eventAggregator.Subscribe<OnModuleEnter>(e => OnModuleEnterHandler());
        }

        // Lazy-loaded services to break circular dependencies
        private IBeastMasteryService BeastMasteryService => _beastMasteryService.Value;
        private IDroidService DroidService => _droidService.Value;

        /// <summary>
        /// When the module loads, cache all relevant associate data into memory.
        /// </summary>
        public void CacheData()
        {
            BeastMasteryService.CacheData();
            DroidService.CacheData();
        }

        /// <summary>
        /// When combat point XP is distributed, handle beast XP.
        /// </summary>
        public void CombatPointXPDistributed()
        {
            BeastMasteryService.CombatPointXPDistributed();
        }

        /// <summary>
        /// When a player enters space or forcefully removes a beast from the party, the beast gets despawned.
        /// </summary>

        public void RemoveAssociate()
        {
            BeastMasteryService.RemoveAssociate();
        }


        /// <summary>
        /// When a beast is blocked, execute the blocked script.
        /// </summary>
        public void BeastOnBlocked()
        {
            BeastMasteryService.BeastOnBlocked();
        }

        /// <summary>
        /// When a beast round ends, handle end combat round logic.
        /// </summary>
        public void BeastOnEndCombatRound()
        {
            BeastMasteryService.BeastOnEndCombatRound();
        }

        /// <summary>
        /// When a beast conversation starts, execute the conversation script.
        /// </summary>
        public void BeastOnConversation()
        {
            BeastMasteryService.BeastOnConversation();
        }

        /// <summary>
        /// When a beast is damaged, execute the damaged script.
        /// </summary>
        public void BeastOnDamaged()
        {
            BeastMasteryService.BeastOnDamaged();
        }

        /// <summary>
        /// When a beast dies, handle death logic.
        /// </summary>
        public void BeastOnDeath()
        {
            BeastMasteryService.BeastOnDeath();
        }

        /// <summary>
        /// When a beast is disturbed, execute the disturbed script.
        /// </summary>
        public void BeastOnDisturbed()
        {
            BeastMasteryService.BeastOnDisturbed();
        }

        /// <summary>
        /// When a beast heartbeat occurs, execute the heartbeat script and restore stats.
        /// </summary>
        public void BeastOnHeartbeat()
        {
            BeastMasteryService.BeastOnHeartbeat();
        }

        /// <summary>
        /// When a beast perception event occurs, execute the perception script.
        /// </summary>
        public void BeastOnPerception()
        {
            BeastMasteryService.BeastOnPerception();
        }

        /// <summary>
        /// When a beast is physically attacked, execute the attack script.
        /// </summary>
        public void BeastOnPhysicalAttacked()
        {
            BeastMasteryService.BeastOnPhysicalAttacked();
        }

        /// <summary>
        /// When a beast rests, handle rest logic.
        /// </summary>
        public void BeastOnRested()
        {
            BeastMasteryService.BeastOnRested();
        }

        /// <summary>
        /// When a beast spawns, handle spawn logic.
        /// </summary>
        public void BeastOnSpawn()
        {
            BeastMasteryService.BeastOnSpawn();
        }

        /// <summary>
        /// When a beast spell is cast, execute the spell cast script.
        /// </summary>
        public void BeastOnSpellCastAt()
        {
            BeastMasteryService.BeastOnSpellCastAt();
        }

        /// <summary>
        /// When a beast user defined event occurs, execute the user defined script.
        /// </summary>
        public void BeastOnUserDefined()
        {
            BeastMasteryService.BeastOnUserDefined();
        }

        /// <summary>
        /// When a beast terminates, open the stables menu.
        /// </summary>
        public void OpenStablesMenu()
        {
            BeastMasteryService.OpenStablesMenu();
        }

        /// <summary>
        /// When an incubator terminal is used, open the incubator UI.
        /// </summary>
        public void UseIncubator()
        {
            BeastMasteryService.UseIncubator();
        }

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        public void OnRemoveProperty()
        {
            BeastMasteryService.OnRemoveProperty();
        }

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        public void UseExtractDNAObject()
        {
            BeastMasteryService.UseExtractDNAObject();
        }

        /// <summary>
        /// When a player uses a droid assembly terminal, displays the UI.
        /// </summary>
        public void UseDroidAssemblyTerminal()
        {
            DroidService.UseDroidAssemblyTerminal();
        }

        /// <summary>
        /// When a player leaves the server, any droids they have actives are despawned.
        /// </summary>
        public void OnPlayerExit()
        {
            DroidService.OnPlayerExit();
        }

        /// <summary>
        /// When an associate acquires an item, handle it appropriately.
        /// </summary>
        public void OnAcquireItem()
        {
            BeastMasteryService.OnAcquireItem();
            DroidService.OnAcquireItem();
        }

        /// <summary>
        /// When entering a property, handle associate behavior.
        /// </summary>
        public void OnEnterPropertyInstance()
        {
            // Implementation for entering property
        }

        /// <summary>
        /// When an item is equipped, handle associate behavior.
        /// </summary>
        public void OnItemEquipHandler()
        {
            // Implementation for item equip
        }

        /// <summary>
        /// When an item is unequipped, handle associate behavior.
        /// </summary>
        public void OnItemUnequipHandler()
        {
            // Implementation for item unequip
        }

        /// <summary>
        /// When module enter event fires, handle associate behavior.
        /// </summary>
        public void OnModuleEnterHandler()
        {
            // Implementation for module enter
        }
    }
}
