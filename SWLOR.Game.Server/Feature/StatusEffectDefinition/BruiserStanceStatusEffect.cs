using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BruiserStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Bruiser Stance";
        public override EffectIconType Icon => EffectIconType.BruiserStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;

        public BruiserStanceStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 10;
            StatGroup.Stats[StatType.TechniqueLine] = 2;
            StatGroup.Stats[StatType.TechniquePresentationSkillType] = (int)SkillType.Unarmed;
            StatGroup.Stats[StatType.TechniqueRetaliationPower] = 24;
            StatGroup.Stats[StatType.TechniqueRetaliationDurationSeconds] = 8;
            StatGroup.Stats[StatType.TechniqueRetaliationCooldownSeconds] = 18;
        }
    }
}
