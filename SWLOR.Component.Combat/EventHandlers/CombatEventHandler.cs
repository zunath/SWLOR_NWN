using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.Native;
using SWLOR.Game.Server.Native;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using GetFortitudeSavingThrow = SWLOR.Component.Combat.Native.GetFortitudeSavingThrow;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Events.Events.Server;

namespace SWLOR.Component.Combat.EventHandlers
{
    /// <summary>
    /// Event handlers for Combat-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class CombatEventHandler
    {
        private readonly ICombatService _combatService;
        private readonly IAttackOfOpportunityService _attackOfOpportunityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;
        private readonly IDeathService _deathService;

        public CombatEventHandler(
            ICombatService combatService,
            IAttackOfOpportunityService attackOfOpportunityService,
            ICombatPointService combatPointService,
            IEnmityService enmityService,
            IDeathService deathService,
            IEventAggregator eventAggregator)
        {
            _combatService = combatService;
            _attackOfOpportunityService = attackOfOpportunityService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _deathService = deathService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleLoad>(e => LoadDamageTypes());
            eventAggregator.Subscribe<OnModuleEnter>(e => OnModuleEnter());
            eventAggregator.Subscribe<OnPlayerHeartbeat>(e => ClearCombatState());
            eventAggregator.Subscribe<OnBroadcastAttackOfOpportunityBefore>(e => OnAttackOfOpportunity());
            eventAggregator.Subscribe<OnItemHit>(e => OnHitCastSpell());
            eventAggregator.Subscribe<OnCreatureDeathAfter>(e => OnCreatureDeath());
            eventAggregator.Subscribe<OnModuleExit>(e => OnPlayerExit());
            eventAggregator.Subscribe<OnAreaExit>(e => OnPlayerExit());
            eventAggregator.Subscribe<OnModuleDying>(e => OnPlayerDying());
            eventAggregator.Subscribe<OnPlayerDeath>(e => OnPlayerDeath());
            eventAggregator.Subscribe<OnModuleRespawn>(e => OnPlayerRespawn());
            eventAggregator.Subscribe<OnCreatureDamagedBefore>(e => CreatureDamaged());
            eventAggregator.Subscribe<OnCreatureAttackBefore>(e => CreatureAttacked());
            eventAggregator.Subscribe<OnObjectDestroyed>(e => CreatureDestroyed());
            eventAggregator.Subscribe<OnDMLimboBefore>(e => CreatureLimbo());
            eventAggregator.Subscribe<OnHookNativeOverrides>(e => HookNativeOverrides());
        }

        /// <summary>
        /// When the module loads, add all valid damage types to the cache.
        /// </summary>
        public void LoadDamageTypes()
        {
            _combatService.LoadDamageTypes();
        }

        /// <summary>
        /// When a player enters the server, apply any defenses towards damage types they don't already have.
        /// </summary>
        public void OnModuleEnter()
        {
            _combatService.AddDamageTypeDefenses();
            _deathService.InitializeRespawnPoint();
        }

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        public void ClearCombatState()
        {
            _combatService.ClearCombatState();
        }

        /// <summary>
        /// Whenever an attack of opportunity is broadcast, skip the event to disable it.
        /// This should effectively disable AOOs across the board.
        /// </summary>
        public void OnAttackOfOpportunity()
        {
            _attackOfOpportunityService.OnAttackOfOpportunity();
        }

        /// <summary>
        /// Adds a combat point to a given NPC creature for a given player and skill type.
        /// </summary>
        public void OnHitCastSpell()
        {
            _combatPointService.OnHitCastSpell();
        }

        /// <summary>
        /// When a creature dies, skill XP is given to all players who contributed during battle.
        /// Then, those combat points are cleared out.
        /// </summary>
        public void OnCreatureDeath()
        {
            _combatPointService.OnCreatureDeath();
            _enmityService.CreatureDeath();
        }

        /// <summary>
        /// When a player leaves an area or the server, we need to remove all combat points
        /// that may be referenced to their character.
        /// </summary>

        public void OnPlayerExit()
        {
            _combatPointService.OnPlayerExit();
            _enmityService.PlayerExit();
        }

        /// <summary>
        /// When a player starts dying, instantly kill them.
        /// </summary>
        public void OnPlayerDying()
        {
            _deathService.OnPlayerDying();
        }

        /// <summary>
        /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
        /// </summary>
        public void OnPlayerDeath()
        {
            _deathService.OnPlayerDeath();
            _enmityService.PlayerDeath();
        }

        /// <summary>
        /// Handles setting player's HP, FP, and STM to half of maximum,
        /// applies penalties for death, and teleports him or her to their home point.
        /// </summary>
        public void OnPlayerRespawn()
        {
            _deathService.OnPlayerRespawn();
        }

        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        public void CreatureDamaged()
        {
            _enmityService.CreatureDamaged();
        }

        /// <summary>
        /// When a creature attacks an enemy, increase enmity by 1.
        /// </summary>
        public void CreatureAttacked()
        {
            _enmityService.CreatureAttacked();
        }

        /// <summary>
        /// When a creature is destroyed with DestroyObject, remove all enmity tables it is associated with.
        /// </summary>
        public void CreatureDestroyed()
        {
            _enmityService.CreatureDestroyed();
        }

        /// <summary>
        /// When a DM limbos creatures, ensure their enmity is wiped.
        /// </summary>
        public void CreatureLimbo()
        {
            _enmityService.CreatureLimbo();
        }
        public void HookNativeOverrides()
        {
            GetDamageRoll.RegisterHook();
            GetFortitudeSavingThrow.RegisterHook();
            ResolveAttackRoll.RegisterHook();
            OnAIActionAttackObject.RegisterHook();
        }
    }
}
