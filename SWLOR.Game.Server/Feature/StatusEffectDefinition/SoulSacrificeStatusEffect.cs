using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoulSacrificeStatusEffect : StatusEffectBase
    {
        public override string Name => "Soul Sacrifice";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public SoulSacrificeStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 35;
        }

    }
}
