using SWLOR.Component.Ability.Feature.AbilityDefinition.Devices;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Force;
using SWLOR.Component.Ability.Feature.AbilityDefinition.General;
using SWLOR.Component.Ability.Service;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Events.Constants;

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
            DashAbilityDefinition dashAbilityDefinition)
        {
            _abilityService = abilityService;
            _recastService = recastService;
            _burstOfSpeedAbilityDefinition = burstOfSpeedAbilityDefinition;
            _gasBombAbilityDefinition = gasBombAbilityDefinition;
            _stealthGeneratorAbilityDefinition = stealthGeneratorAbilityDefinition;
            _incendiaryBombAbilityDefinition = incendiaryBombAbilityDefinition;
            _dashAbilityDefinition = dashAbilityDefinition;
        }

        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheData()
        {
            _abilityService.CacheData();
            _recastService.CacheRecastGroups();
        }

        /// <summary>
        /// Each tick, creatures with a concentration effect will be processed.
        /// This will drain FP and reapply whatever effect is associated with an ability.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorHeartbeat)]
        public void ProcessConcentrationEffects()
        {
            _abilityService.ProcessConcentrationEffects();
        }

        /// <summary>
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        [ScriptHandler(ScriptName.OnItemHit)]
        public void AddLeadershipCombatPoint()
        {
            _abilityService.AddLeadershipCombatPoint();
        }

        /// <summary>
        /// When a player enters the server, apply the Aura AOE effect.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void OnModuleEnter()
        {
            _abilityService.ApplyAuraAOE();
            _dashAbilityDefinition.EnterSpace();
        }

        /// <summary>
        /// When a player exits the server, remove all of their Aura effects.
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        public void OnModuleExit()
        {
            _abilityService.ClearAurasOnExit();
        }

        /// <summary>
        /// When a player dies, remove all of their Aura effects.
        /// </summary>
        [ScriptHandler<OnModuleDeath>]
        public void OnModuleDeath()
        {
            _abilityService.ClearAurasOnDeath();
        }

        /// <summary>
        /// When a player respawns, reapply the aura AOE effect
        /// </summary>
        [ScriptHandler<OnModuleRespawn>]
        public void OnModuleRespawn()
        {
            _abilityService.ReapplyAuraOnRespawn();
        }

        /// <summary>
        /// When a player enters space mode, remove all of their Aura effects.
        /// </summary>
        [ScriptHandler(ScriptName.OnSpaceEnter)]
        public void OnSpaceEnter()
        {
            _abilityService.ClearAurasOnSpaceEntry();
        }

        /// <summary>
        /// Whenever a creature enters the aura, add them to the cache.
        /// </summary>
        [ScriptHandler(ScriptName.OnAuraEnter)]
        public void AuraEnter()
        {
            _abilityService.AuraEnter();
        }

        /// <summary>
        /// Whenever a creature exits the aura, remove it from the cache.
        /// </summary>
        [ScriptHandler(ScriptName.OnAuraExit)]
        public void AuraExit()
        {
            _abilityService.AuraExit();
        }

        /// <summary>
        /// When Burst of Speed effect is applied.
        /// </summary>
        [ScriptHandler(ScriptName.OnBurstOfSpeedApply)]
        public void ApplyBurstOfSpeedEffect()
        {
            BurstOfSpeedAbilityDefinition.ApplyEffect();
        }

        /// <summary>
        /// When Burst of Speed effect is removed.
        /// </summary>
        [ScriptHandler(ScriptName.OnBurstOfSpeedRemoved)]
        public void RemoveBurstOfSpeedEffect()
        {
            BurstOfSpeedAbilityDefinition.RemoveEffect();
        }

        /// <summary>
        /// When Gas Bomb 1 effect is applied.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeGas1Enable)]
        public void GasBomb1Enter()
        {
            _gasBombAbilityDefinition.GasBomb1Enter();
        }

        /// <summary>
        /// When Gas Bomb 1 heartbeat occurs.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeGas1Heartbeat)]
        public void GasBomb1Heartbeat()
        {
            _gasBombAbilityDefinition.GasBomb1Heartbeat();
        }

        /// <summary>
        /// When Gas Bomb 2 effect is applied.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeGas2Enable)]
        public void GasBomb2Enter()
        {
            _gasBombAbilityDefinition.GasBomb2Enter();
        }

        /// <summary>
        /// When Gas Bomb 2 heartbeat occurs.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeGas2Heartbeat)]
        public void GasBomb2Heartbeat()
        {
            _gasBombAbilityDefinition.GasBomb2Heartbeat();
        }

        /// <summary>
        /// When Gas Bomb 3 effect is applied.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeGas3Enable)]
        public void GasBomb3Enter()
        {
            _gasBombAbilityDefinition.GasBomb3Enter();
        }

        /// <summary>
        /// When Gas Bomb 3 heartbeat occurs.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeGas3Heartbeat)]
        public void GasBomb3Heartbeat()
        {
            _gasBombAbilityDefinition.GasBomb3Heartbeat();
        }

        /// <summary>
        /// When Stealth Generator is used or player is damaged, clear invisibility.
        /// </summary>
        [ScriptHandler(ScriptName.OnHarvesterUsed)]
        [ScriptHandler<OnPlayerDamaged>]
        public void ClearInvisibility()
        {
            StealthGeneratorAbilityDefinition.ClearInvisibility();
        }

        /// <summary>
        /// When Incendiary Bomb 1 effect is applied.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeIncendiary1Enable)]
        public void IncendiaryBomb1Enter()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb1Enter();
        }

        /// <summary>
        /// When Incendiary Bomb 1 heartbeat occurs.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeIncendiary1Heartbeat)]
        public void IncendiaryBomb1Heartbeat()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb1Heartbeat();
        }

        /// <summary>
        /// When Incendiary Bomb 2 effect is applied.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeIncendiary2Enable)]
        public void IncendiaryBomb2Enter()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb2Enter();
        }

        /// <summary>
        /// When Incendiary Bomb 2 heartbeat occurs.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeIncendiary2Heartbeat)]
        public void IncendiaryBomb2Heartbeat()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb2Heartbeat();
        }

        /// <summary>
        /// When Incendiary Bomb 3 effect is applied.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeIncendiary3Enable)]
        public void IncendiaryBomb3Enter()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb3Enter();
        }

        /// <summary>
        /// When Incendiary Bomb 3 heartbeat occurs.
        /// </summary>
        [ScriptHandler(ScriptName.OnGrenadeIncendiary3Heartbeat)]
        public void IncendiaryBomb3Heartbeat()
        {
            _incendiaryBombAbilityDefinition.IncendiaryBomb3Heartbeat();
        }
    }
}
