using SWLOR.Component.Ability.Definitions.Devices;
using SWLOR.Component.Ability.Definitions.Force;
using SWLOR.Component.Ability.Definitions.General;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Events;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Domain.Inventory.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Ability.EventHandlers
{
    /// <summary>
    /// Event handlers for Ability-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the appropriate services.
    /// </summary>
    public class AbilityEventHandlers
    {
        private readonly IAbilityService _abilityService;
        private readonly IRecastService _recastService;
        private readonly BurstOfSpeedAbilityDefinition _burstOfSpeedAbilityDefinition;
        private readonly GasBombAbilityDefinition _gasBombAbilityDefinition;
        private readonly StealthGeneratorAbilityDefinition _stealthGeneratorAbilityDefinition;
        private readonly IncendiaryBombAbilityDefinition _incendiaryBombAbilityDefinition;
        private readonly DashAbilityDefinition _dashAbilityDefinition;

        public AbilityEventHandlers(
            IAbilityService abilityService,
            IRecastService recastService,
            BurstOfSpeedAbilityDefinition burstOfSpeedAbilityDefinition,
            GasBombAbilityDefinition gasBombAbilityDefinition,
            StealthGeneratorAbilityDefinition stealthGeneratorAbilityDefinition,
            IncendiaryBombAbilityDefinition incendiaryBombAbilityDefinition,
            DashAbilityDefinition dashAbilityDefinition,
            IEventAggregator eventAggregator)
        {
            _abilityService = abilityService;
            _recastService = recastService;
            _burstOfSpeedAbilityDefinition = burstOfSpeedAbilityDefinition;
            _gasBombAbilityDefinition = gasBombAbilityDefinition;
            _stealthGeneratorAbilityDefinition = stealthGeneratorAbilityDefinition;
            _incendiaryBombAbilityDefinition = incendiaryBombAbilityDefinition;
            _dashAbilityDefinition = dashAbilityDefinition;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheData());
            eventAggregator.Subscribe<OnItemHit>(e => AddLeadershipCombatPoint());
            eventAggregator.Subscribe<OnModuleEnter>(e => OnModuleEnter());
            eventAggregator.Subscribe<OnBurstOfSpeedApply>(e => ApplyBurstOfSpeedEffect());
            eventAggregator.Subscribe<OnBurstOfSpeedRemoved>(e => RemoveBurstOfSpeedEffect());
            eventAggregator.Subscribe<OnGrenadeGas1Enable>(e => GasBomb1Enter());
            eventAggregator.Subscribe<OnGrenadeGas1Heartbeat>(e => GasBomb1Heartbeat());
            eventAggregator.Subscribe<OnGrenadeGas2Enable>(e => GasBomb2Enter());
            eventAggregator.Subscribe<OnGrenadeGas2Heartbeat>(e => GasBomb2Heartbeat());
            eventAggregator.Subscribe<OnGrenadeGas3Enable>(e => GasBomb3Enter());
            eventAggregator.Subscribe<OnGrenadeGas3Heartbeat>(e => GasBomb3Heartbeat());
            eventAggregator.Subscribe<OnHarvesterUsed>(e => ClearInvisibility());
            eventAggregator.Subscribe<OnPlayerDamaged>(e => ClearInvisibility());
            eventAggregator.Subscribe<OnGrenadeIncendiary1Enable>(e => IncendiaryBomb1Enter());
            eventAggregator.Subscribe<OnGrenadeIncendiary1Heartbeat>(e => IncendiaryBomb1Heartbeat());
            eventAggregator.Subscribe<OnGrenadeIncendiary2Enable>(e => IncendiaryBomb2Enter());
            eventAggregator.Subscribe<OnGrenadeIncendiary2Heartbeat>(e => IncendiaryBomb2Heartbeat());
            eventAggregator.Subscribe<OnGrenadeIncendiary3Enable>(e => IncendiaryBomb3Enter());
            eventAggregator.Subscribe<OnGrenadeIncendiary3Heartbeat>(e => IncendiaryBomb3Heartbeat());
        }

        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        public void CacheData()
        {
            _abilityService.CacheData();
            _recastService.CacheRecastGroups();
        }

        /// <summary>
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        public void AddLeadershipCombatPoint()
        {
            _abilityService.AddLeadershipCombatPoint();
        }

        /// <summary>
        /// When a player enters the server, apply the Aura AOE effect.
        /// </summary>
        public void OnModuleEnter()
        {
            _dashAbilityDefinition.EnterSpace();
        }

        /// <summary>
        /// When Burst of Speed effect is applied.
        /// </summary>
        public void ApplyBurstOfSpeedEffect()
        {
            BurstOfSpeedAbilityDefinition.ApplyEffect();
        }

        /// <summary>
        /// When Burst of Speed effect is removed.
        /// </summary>
        public void RemoveBurstOfSpeedEffect()
        {
            BurstOfSpeedAbilityDefinition.RemoveEffect();
        }

        /// <summary>
        /// When Gas Bomb 1 effect is applied.
        /// </summary>
        public void GasBomb1Enter()
        {
            _gasBombAbilityDefinition.GasBomb1Enter();
        }

        /// <summary>
        /// When Gas Bomb 1 heartbeat occurs.
        /// </summary>
        public void GasBomb1Heartbeat()
        {
            _gasBombAbilityDefinition.GasBomb1Heartbeat();
        }

        /// <summary>
        /// When Gas Bomb 2 effect is applied.
        /// </summary>
        public void GasBomb2Enter()
        {
            _gasBombAbilityDefinition.GasBomb2Enter();
        }

        /// <summary>
        /// When Gas Bomb 2 heartbeat occurs.
        /// </summary>
        public void GasBomb2Heartbeat()
        {
            _gasBombAbilityDefinition.GasBomb2Heartbeat();
        }

        /// <summary>
        /// When Gas Bomb 3 effect is applied.
        /// </summary>
        public void GasBomb3Enter()
        {
            _gasBombAbilityDefinition.GasBomb3Enter();
        }

        /// <summary>
        /// When Gas Bomb 3 heartbeat occurs.
        /// </summary>
        public void GasBomb3Heartbeat()
        {
            _gasBombAbilityDefinition.GasBomb3Heartbeat();
        }

        /// <summary>
        /// When Stealth Generator is used or player is damaged, clear invisibility.
        /// </summary>

        public void ClearInvisibility()
        {
            StealthGeneratorAbilityDefinition.ClearInvisibility();
        }

        /// <summary>
        /// When Incendiary Bomb 1 effect is applied.
        /// </summary>
        public void IncendiaryBomb1Enter()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb1Enter();
        }

        /// <summary>
        /// When Incendiary Bomb 1 heartbeat occurs.
        /// </summary>
        public void IncendiaryBomb1Heartbeat()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb1Heartbeat();
        }

        /// <summary>
        /// When Incendiary Bomb 2 effect is applied.
        /// </summary>
        public void IncendiaryBomb2Enter()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb2Enter();
        }

        /// <summary>
        /// When Incendiary Bomb 2 heartbeat occurs.
        /// </summary>
        public void IncendiaryBomb2Heartbeat()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb2Heartbeat();
        }

        /// <summary>
        /// When Incendiary Bomb 3 effect is applied.
        /// </summary>
        public void IncendiaryBomb3Enter()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb3Enter();
        }

        /// <summary>
        /// When Incendiary Bomb 3 heartbeat occurs.
        /// </summary>
        public void IncendiaryBomb3Heartbeat()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb3Heartbeat();
        }
    }
}
