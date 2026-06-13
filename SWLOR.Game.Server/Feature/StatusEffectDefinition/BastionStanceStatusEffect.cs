using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BastionStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Bastion Stance";
        public override EffectIconType Icon => EffectIconType.BastionStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public BastionStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 15;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 20;
            StatGroup.Stats[StatType.HeavyVibrobladeDefenseRecoveryWindow] = 1;
        }

    }
}
