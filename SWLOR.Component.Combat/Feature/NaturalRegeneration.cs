using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Combat.Feature
{
    public class NaturalRegeneration
    {
        private readonly IStatServiceNew _statService;
        private readonly ICharacterResourceService _characterResourceService;

        public NaturalRegeneration(
            IStatServiceNew statService,
            ICharacterResourceService characterResourceService)
        {
            _statService = statService;
            _characterResourceService = characterResourceService;
        }
        
        /// <summary>
        /// On module heartbeat, process a player's HP/FP/STM regeneration.
        /// </summary>
        [ScriptHandler<OnPlayerHeartbeat>]
        public void ProcessRegeneration()
        {
            ProcessRegenerationInternal();
        }

        private void ProcessRegenerationInternal()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var tick = GetLocalInt(player, "NATURAL_REGENERATION_TICK") + 1;
            if (tick >= 5) // 6 seconds * 5 = 30 seconds
            {
                var hpRegen = _statService.CalculateHPRegen(player);
                var fpRegen = _statService.CalculateFPRegen(player);
                var stmRegen = _statService.CalculateSTMRegen(player);

                if (hpRegen > 0 && GetCurrentHitPoints(player) < GetMaxHitPoints(player))
                {
                    _characterResourceService.RestoreHP(player, hpRegen);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(hpRegen), player);
                }

                if (fpRegen > 0)
                {
                    _characterResourceService.RestoreFP(player, fpRegen);
                }

                if (stmRegen > 0)
                {
                    _characterResourceService.RestoreSTM(player, stmRegen);
                }

                tick = 0;
            }

            SetLocalInt(player, "NATURAL_REGENERATION_TICK", tick);
        }
    }
}
