using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Warden Wall stance: while active, hardens the wielder's defenses. A Mimicry defensive stance.
    /// </summary>
    public sealed class WardenWallStatusEffect : StatusEffectBase
    {
        public override string Name => "Warden Wall";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override IStatusEffect Clone()
        {
            return new WardenWallStatusEffect();
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
        }
    }
}
