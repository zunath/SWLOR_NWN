using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class HarmonicRestorationStatusEffect : StatusEffectBase
    {
        public override string Name => "Harmonic Restoration";
        public override EffectIconType Icon => EffectIconType.HarmonicRestorationStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public HarmonicRestorationStatusEffect()
        {
            StatGroup.Stats[StatType.TraumaResistance] = 10;
        }
    }
}
