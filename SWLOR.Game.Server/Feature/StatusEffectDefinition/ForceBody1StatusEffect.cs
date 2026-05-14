using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceBody1StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Body I";
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public ForceBody1StatusEffect()
        {
            MorePowerfulEffectTypes.Add(typeof(ForceBody2StatusEffect));

            StatGroup.Stats[StatType.DarkForceDamageFPRestore] = 1;
            StatGroup.Stats[StatType.DarkForceDamageHPCostPercent] = 2;
        }
    }
}
