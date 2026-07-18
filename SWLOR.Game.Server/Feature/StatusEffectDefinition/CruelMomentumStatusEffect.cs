using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// The Force Dark Ravager trait's reward for finishing an enemy off: after defeating a target
    /// damaged in the last few seconds, the wielder's Force abilities land more reliably for a short
    /// window. Applied (with the FP restore and trigger cooldown) by Combat.ApplyCruelMomentumEffect.
    /// </summary>
    public sealed class CruelMomentumStatusEffect : StatusEffectBase
    {
        private const int ForceAccuracyPercent = 5;

        public override string Name => "Cruel Momentum";
        public override EffectIconType Icon => EffectIconType.CruelMomentumStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public CruelMomentumStatusEffect()
        {
            // The accuracy bonus is scoped to Force abilities, so it carries the skill selector
            // alongside the magnitude; Combat reads the pair when resolving ability hit chance.
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType] = (int)SkillType.Force;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = ForceAccuracyPercent;
        }
    }
}
