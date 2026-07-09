using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoulAscensionStatusEffect : StatusEffectBase
    {
        public override string Name => "Soul Ascension";
        public override EffectIconType Icon => EffectIconType.SoulAscensionStatusEffect;

        public SoulAscensionStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 8;
            StatGroup.Stats[StatType.PhysicalDamageDealtHPPercentRestore] = 8;
        }

    }
}
