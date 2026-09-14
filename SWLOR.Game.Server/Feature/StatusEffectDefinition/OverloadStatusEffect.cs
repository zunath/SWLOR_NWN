using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Short-lived buff from the Overclocked Analyzer capstone's Overload ability. Briefly overclocks
    /// the combat analyzer: a large boost to mimicked technique potency plus an added on-hit proc
    /// chance for every mimicked effect, amplifying equipped proc traits.
    /// </summary>
    public sealed class OverloadStatusEffect : StatusEffectBase
    {
        public override string Name => "Overload";
        public override EffectIconType Icon => EffectIconType.OverloadStatusEffect;

        public OverloadStatusEffect()
        {
            StatGroup.Stats[StatType.MimicryPotencyPercent] = 50;
            StatGroup.Stats[StatType.DamageDealtBleedChance] = 15;
            StatGroup.Stats[StatType.DamageDealtFreezingChance] = 15;
            StatGroup.Stats[StatType.DamageDealtShockChance] = 15;
            StatGroup.Stats[StatType.DamageDealtSunderChance] = 15;
            StatGroup.Stats[StatType.DamageDealtHemorrhageChance] = 15;
        }
    }
}
