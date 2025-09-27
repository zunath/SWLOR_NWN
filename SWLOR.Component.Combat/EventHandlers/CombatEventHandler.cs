using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.Service;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;

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
        private readonly Death _deathService;

        public CombatEventHandler(
            ICombatService combatService,
            IAttackOfOpportunityService attackOfOpportunityService,
            ICombatPointService combatPointService,
            IEnmityService enmityService,
            Death deathService)
        {
            _combatService = combatService;
            _attackOfOpportunityService = attackOfOpportunityService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _deathService = deathService;
        }

        /// <summary>
        /// When the module loads, add all valid damage types to the cache.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void LoadDamageTypes()
        {
            _combatService.LoadDamageTypes();
        }

        /// <summary>
        /// When a player enters the server, apply any defenses towards damage types they don't already have.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void OnModuleEnter()
        {
            _combatService.AddDamageTypeDefenses();
            _deathService.InitializeRespawnPoint();
        }

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        [ScriptHandler(ScriptName.OnIntervalPC6Seconds)]
        public void ClearCombatState()
        {
            _combatService.ClearCombatState();
        }

        /// <summary>
        /// Whenever an attack of opportunity is broadcast, skip the event to disable it.
        /// This should effectively disable AOOs across the board.
        /// </summary>
        [ScriptHandler<OnBroadcastAttackOfOpportunityBefore>]
        public void OnAttackOfOpportunity()
        {
            _attackOfOpportunityService.OnAttackOfOpportunity();
        }

        /// <summary>
        /// Adds a combat point to a given NPC creature for a given player and skill type.
        /// </summary>
        [ScriptHandler(ScriptName.OnItemHit)]
        public void OnHitCastSpell()
        {
            _combatPointService.OnHitCastSpell();
        }

        /// <summary>
        /// When a creature dies, skill XP is given to all players who contributed during battle.
        /// Then, those combat points are cleared out.
        /// </summary>
        [ScriptHandler<OnCreatureDeathAfter>]
        public void OnCreatureDeath()
        {
            _combatPointService.OnCreatureDeath();
            _enmityService.CreatureDeath();
        }

        /// <summary>
        /// When a player leaves an area or the server, we need to remove all combat points
        /// that may be referenced to their character.
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        [ScriptHandler<OnAreaExit>]
        public void OnPlayerExit()
        {
            _combatPointService.OnPlayerExit();
            _enmityService.PlayerExit();
        }

        /// <summary>
        /// When a player starts dying, instantly kill them.
        /// </summary>
        [ScriptHandler<OnModuleDying>]
        public void OnPlayerDying()
        {
            _deathService.OnPlayerDying();
        }

        /// <summary>
        /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
        /// </summary>
        [ScriptHandler<OnModuleDeath>]
        public void OnPlayerDeath()
        {
            _deathService.OnPlayerDeath();
            _enmityService.PlayerDeath();
        }

        /// <summary>
        /// Handles setting player's HP, FP, and STM to half of maximum,
        /// applies penalties for death, and teleports him or her to their home point.
        /// </summary>
        [ScriptHandler<OnModuleRespawn>]
        public void OnPlayerRespawn()
        {
            _deathService.OnPlayerRespawn();
        }

        /// <summary>
        /// When an enemy is damaged, increase enmity toward that creature by the amount of damage dealt.
        /// </summary>
        [ScriptHandler<OnCreatureDamagedBefore>]
        public void CreatureDamaged()
        {
            _enmityService.CreatureDamaged();
        }

        /// <summary>
        /// When a creature attacks an enemy, increase enmity by 1.
        /// </summary>
        [ScriptHandler<OnCreatureAttackBefore>]
        public void CreatureAttacked()
        {
            _enmityService.CreatureAttacked();
        }

        /// <summary>
        /// When a creature is destroyed with DestroyObject, remove all enmity tables it is associated with.
        /// </summary>
        [ScriptHandler(ScriptName.OnObjectDestroyed)]
        public void CreatureDestroyed()
        {
            _enmityService.CreatureDestroyed();
        }

        /// <summary>
        /// When a DM limbos creatures, ensure their enmity is wiped.
        /// </summary>
        [ScriptHandler<OnDMLimboBefore>]
        public void CreatureLimbo()
        {
            _enmityService.CreatureLimbo();
        }
    }
}
