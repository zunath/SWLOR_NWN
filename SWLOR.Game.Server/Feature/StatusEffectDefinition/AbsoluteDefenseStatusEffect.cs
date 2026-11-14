using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AbsoluteDefenseStatusEffect : StatusEffectBase
    {
        public override string Name => "Absolute Defense";
        public override EffectIconType Icon => EffectIconType.AbsoluteDefenseStatusEffect;
        public AbsoluteDefenseStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -15;
            StatGroup.Stats[StatType.HeavyVibrobladeDefenseRecoveryWindow] = 1;
        }

    }
}
