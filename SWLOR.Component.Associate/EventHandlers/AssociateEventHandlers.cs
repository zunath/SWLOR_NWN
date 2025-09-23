using SWLOR.Component.Associate.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Associate.EventHandlers
{
    /// <summary>
    /// Event handlers for Associate-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class AssociateEventHandlers
    {
        private readonly IBeastMasteryService _beastMasteryService;
        private readonly IDroidService _droidService;

        public AssociateEventHandlers(IBeastMasteryService beastMasteryService, IDroidService droidService)
        {
            _beastMasteryService = beastMasteryService;
            _droidService = droidService;
        }

        /// <summary>
        /// When the module loads, cache all relevant associate data into memory.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _beastMasteryService.CacheData();
            _droidService.CacheData();
        }

        /// <summary>
        /// When combat point XP is distributed, handle beast XP.
        /// </summary>
        [ScriptHandler(ScriptName.OnCombatPointXPDistribute)]
        public void CombatPointXPDistributed()
        {
            _beastMasteryService.CombatPointXPDistributed();
        }

        /// <summary>
        /// When a player enters space or forcefully removes a beast from the party, the beast gets despawned.
        /// </summary>
        [ScriptHandler(ScriptName.OnSpaceEnter)]
        [ScriptHandler<OnAssociateRemoveBefore>]
        public void RemoveAssociate()
        {
            _beastMasteryService.RemoveAssociate();
        }


        /// <summary>
        /// When a beast is blocked, execute the blocked script.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastBlocked)]
        public void BeastOnBlocked()
        {
            _beastMasteryService.BeastOnBlocked();
        }

        /// <summary>
        /// When a beast round ends, handle end combat round logic.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastRoundEnd)]
        public void BeastOnEndCombatRound()
        {
            _beastMasteryService.BeastOnEndCombatRound();
        }

        /// <summary>
        /// When a beast conversation starts, execute the conversation script.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastConversation)]
        public void BeastOnConversation()
        {
            _beastMasteryService.BeastOnConversation();
        }

        /// <summary>
        /// When a beast is damaged, execute the damaged script.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastDamaged)]
        public void BeastOnDamaged()
        {
            _beastMasteryService.BeastOnDamaged();
        }

        /// <summary>
        /// When a beast dies, handle death logic.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastDeath)]
        public void BeastOnDeath()
        {
            _beastMasteryService.BeastOnDeath();
        }

        /// <summary>
        /// When a beast is disturbed, execute the disturbed script.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastDisturbed)]
        public void BeastOnDisturbed()
        {
            _beastMasteryService.BeastOnDisturbed();
        }

        /// <summary>
        /// When a beast heartbeat occurs, execute the heartbeat script and restore stats.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastHeartbeat)]
        public void BeastOnHeartbeat()
        {
            _beastMasteryService.BeastOnHeartbeat();
        }

        /// <summary>
        /// When a beast perception event occurs, execute the perception script.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastPerception)]
        public void BeastOnPerception()
        {
            _beastMasteryService.BeastOnPerception();
        }

        /// <summary>
        /// When a beast is physically attacked, execute the attack script.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastAttacked)]
        public void BeastOnPhysicalAttacked()
        {
            _beastMasteryService.BeastOnPhysicalAttacked();
        }

        /// <summary>
        /// When a beast rests, handle rest logic.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastRest)]
        public void BeastOnRested()
        {
            _beastMasteryService.BeastOnRested();
        }

        /// <summary>
        /// When a beast spawns, handle spawn logic.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastSpawn)]
        public void BeastOnSpawn()
        {
            _beastMasteryService.BeastOnSpawn();
        }

        /// <summary>
        /// When a beast spell is cast, execute the spell cast script.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastSpellCast)]
        public void BeastOnSpellCastAt()
        {
            _beastMasteryService.BeastOnSpellCastAt();
        }

        /// <summary>
        /// When a beast user defined event occurs, execute the user defined script.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastUserDefined)]
        public void BeastOnUserDefined()
        {
            _beastMasteryService.BeastOnUserDefined();
        }

        /// <summary>
        /// When a beast terminates, open the stables menu.
        /// </summary>
        [ScriptHandler(ScriptName.OnBeastTerminate)]
        public void OpenStablesMenu()
        {
            _beastMasteryService.OpenStablesMenu();
        }

        /// <summary>
        /// When an incubator terminal is used, open the incubator UI.
        /// </summary>
        [ScriptHandler(ScriptName.OnIncubatorTerminal)]
        public void UseIncubator()
        {
            _beastMasteryService.UseIncubator();
        }

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorDeleteProperty)]
        public void OnRemoveProperty()
        {
            _beastMasteryService.OnRemoveProperty();
        }

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        [ScriptHandler(ScriptName.OnDNAExtractUsed)]
        public void UseExtractDNAObject()
        {
            _beastMasteryService.UseExtractDNAObject();
        }

        /// <summary>
        /// When a player uses a droid assembly terminal, displays the UI.
        /// </summary>
        [ScriptHandler(ScriptName.OnDroidAssociateUsed)]
        public void UseDroidAssemblyTerminal()
        {
            _droidService.UseDroidAssemblyTerminal();
        }

        /// <summary>
        /// When a player leaves the server, any droids they have actives are despawned.
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        public void OnPlayerExit()
        {
            _droidService.OnPlayerExit();
        }

        /// <summary>
        /// When an associate acquires an item, handle it appropriately.
        /// </summary>
        [ScriptHandler<OnModuleAcquire>]
        public void OnAcquireItem()
        {
            _beastMasteryService.OnAcquireItem();
            _droidService.OnAcquireItem();
        }
    }
}
