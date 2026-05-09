using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardiansWrathStatusEffect : StatusEffectBase
    {
        public override string Name => "Guardian's Wrath";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public GuardiansWrathStatusEffect()
        {
            StatGroup.Stats[StatType.AttackDeflection] = 100;
        }

    }
}
