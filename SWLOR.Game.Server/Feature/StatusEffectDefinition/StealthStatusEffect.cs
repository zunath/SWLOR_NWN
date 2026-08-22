using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Active while a player moves in stealth. Carries the Stealth perk rank's effectiveness bonus,
    /// drains stamina every tick (stealth breaks when stamina runs out), and samples movement for
    /// active Espionage infiltration attempts.
    /// </summary>
    public sealed class StealthStatusEffect : StatusEffectBase
    {
        private const int StaminaDrainPerTick = 2;
        private const float BaseFrequencySeconds = 6f;

        private float _frequency = BaseFrequencySeconds;

        public override string Name => "Stealth";
        public override EffectIconType Icon => EffectIconType.StealthStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => _frequency;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            var stealthBonus = Stat.GetStatAdjustment(creature, StatType.ActiveStealthBonus);
            if (stealthBonus != 0)
            {
                StatGroup.Stats[StatType.Stealth] = stealthBonus;
            }

            // Drain-slowing stats stretch the tick interval rather than shrinking the
            // per-tick amount so small reductions are not lost to integer rounding.
            var drainReduction = Stat.GetStatAdjustment(creature, StatType.StealthStaminaDrainReductionPercent);
            _frequency = CalculateDrainFrequencySeconds(drainReduction);

            var movementSpeedBonus = Stat.GetStatAdjustment(
                creature,
                StatType.StealthMovementSpeedPercentAdjustment);
            if (movementSpeedBonus != 0)
            {
                StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = movementSpeedBonus;
            }
        }

        public static float CalculateDrainFrequencySeconds(int drainReductionPercent)
        {
            var boundedReduction = Math.Clamp(drainReductionPercent, 0, 90);
            return BaseFrequencySeconds * 100f / (100f - boundedReduction);
        }

        protected override void Tick(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            // Self-heal any engine/status desynchronization so a stale icon can never keep draining
            // stamina after stealth mode has ended.
            if (!GetActionMode(creature, ActionMode.Stealth))
            {
                StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);
                return;
            }

            EspionageInfiltration.UpdateMovement(creature);

            Stat.ReduceStamina(creature, StaminaDrainPerTick);

            if (Stat.GetCurrentStamina(creature) > 0)
                return;

            SendMessageToPC(creature, ColorToken.Red("You are too exhausted to remain hidden."));
            AssignCommand(creature, () =>
            {
                SetActionMode(creature, ActionMode.Stealth, false);
            });
        }

    }
}
