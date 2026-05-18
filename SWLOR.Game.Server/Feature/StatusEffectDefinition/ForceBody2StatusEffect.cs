using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceBody2StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Body II";
        public override EffectIconType Icon => EffectIconType.ForceBody2StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public ForceBody2StatusEffect()
        {
            LessPowerfulEffectTypes.Add(typeof(ForceBody1StatusEffect));

            StatGroup.Stats[StatType.DarkForceDamageFPRestore] = 1;
            StatGroup.Stats[StatType.DarkForceDamageHPCostPercent] = 2;
            StatGroup.Stats[StatType.DarkForceDamageLowTargetHPCostPercent] = 1;
            StatGroup.Stats[StatType.DarkForceDamageLowTargetHPThresholdPercent] = 50;
        }
    }
}
