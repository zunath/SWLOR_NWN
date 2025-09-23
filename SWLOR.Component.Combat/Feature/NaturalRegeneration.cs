using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Combat.Feature
{
    public class NaturalRegeneration
    {
        private readonly IDatabaseService _db;
        private readonly IStatService _statService;
        private readonly IStatusEffectService _statusEffectService;

        public NaturalRegeneration(IDatabaseService db, IStatService statService, IStatusEffectService statusEffectService)
        {
            _db = db;
            _statService = statService;
            _statusEffectService = statusEffectService;
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
                var vitalityBonus = GetAbilityModifier(AbilityType.Vitality, player);
                if (vitalityBonus < 0)
                    vitalityBonus = 0;

                var playerId = GetObjectUUID(player);
                var dbPlayer = _db.Get<Player>(playerId);
                var hpRegen = dbPlayer.HPRegen + vitalityBonus * 4;
                var fpRegen = 1 + dbPlayer.FPRegen + vitalityBonus / 2;
                var stmRegen = 1 + dbPlayer.STMRegen + vitalityBonus / 2;
                var foodEffect = _statusEffectService.GetEffectData<FoodEffectData>(player, StatusEffectType.Food);

                if (foodEffect != null)
                {
                    hpRegen += foodEffect.HPRegen;
                    fpRegen += foodEffect.FPRegen;
                    stmRegen += foodEffect.STMRegen;
                }

                if (hpRegen > 0 && GetCurrentHitPoints(player) < GetMaxHitPoints(player))
                {
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(hpRegen), player);
                }

                if (fpRegen > 0)
                {
                    _statService.RestoreFP(player, fpRegen, dbPlayer);
                }

                if (stmRegen > 0)
                {
                    _statService.RestoreStamina(player, stmRegen, dbPlayer);
                }

                tick = 0;
            }

            SetLocalInt(player, "NATURAL_REGENERATION_TICK", tick);
        }
    }
}
