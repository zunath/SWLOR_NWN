using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Active while a player moves in stealth. Carries the Stealth perk rank's effectiveness bonus
    /// and drains stamina every tick; stealth breaks when stamina runs out.
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
            // Drain-slowing stats stretch the tick interval rather than shrinking the
            // per-tick amount so small reductions are not lost to integer rounding.
            var drainReduction = Stat.GetStatAdjustment(creature, StatType.StealthStaminaDrainReductionPercent);
            if (drainReduction > 0)
            {
                _frequency = BaseFrequencySeconds * (100 + drainReduction) / 100f;
            }
        }

        protected override void Tick(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

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
