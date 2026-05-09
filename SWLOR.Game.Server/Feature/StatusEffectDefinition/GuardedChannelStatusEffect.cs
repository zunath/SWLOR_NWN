using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardedChannelStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarded Channel";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public GuardedChannelStatusEffect()
        {
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.AttackDeflection] = 20;
        }

    }
}
