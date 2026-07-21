using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Sustain Burn stance: while active, the wielder's landed hits always apply Poison (which stacks
    /// and ramps its damage over time). A Mimicry damage-over-time stance; reuses the shared
    /// <see cref="StatType.DamageDealtPoisonChance"/> hit rider consumed by Combat.
    /// </summary>
    public sealed class SustainBurnStatusEffect : StatusEffectBase
    {
        public override string Name => "Sustain Burn";
        public override EffectIconType Icon => EffectIconType.SustainBurnStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override IStatusEffect Clone()
        {
            return new SustainBurnStatusEffect();
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageDealtPoisonChance] = 100;
            StatGroup.Stats[StatType.DamageDealtPoisonDurationSeconds] = 30;
        }
    }
}
