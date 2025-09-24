using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.StatusEffect.EventHandlers
{
    public class StatusEffectEventHandler
    {
        private readonly IStatusEffectService _statusEffectService;

        public StatusEffectEventHandler(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheStatusEffects()
        {
            _statusEffectService.CacheStatusEffects();
        }

        /// <summary>
        /// When a player enters the server, if any of their status effects in limbo, re-add them to the
        /// dictionary for processing.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void PlayerEnter()
        {
            _statusEffectService.PlayerEnter();
        }

        /// <summary>
        /// When a player leaves the server, move their status effects to a different dictionary
        /// so they aren't processed unnecessarily.  
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        public void PlayerExit()
        {
            _statusEffectService.PlayerExit();
        }

        /// <summary>
        /// When the module heartbeat runs, execute and clean up status effects on all creatures.
        /// </summary>
        [ScriptHandler(ScriptName.OnSwlorHeartbeat)]
        public void TickStatusEffects()
        {
            _statusEffectService.TickStatusEffects();
        }

        /// <summary>
        /// When a player dies, remove any status effects which are present.
        /// </summary>
        [ScriptHandler<OnModuleDeath>]
        public void OnPlayerDeath()
        {
            _statusEffectService.OnPlayerDeath();
        }
    }
}
