using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DiseaseVitalityPenaltyStatusEffect : StaticAbilityStatusEffectBase
    {
        public override string Name => "Disease";
        public override EffectIconType Icon => EffectIconType.DiseaseVitalityPenaltyStatusEffect;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override bool PersistsOnLogout => false;

        public DiseaseVitalityPenaltyStatusEffect()
            : base(AbilityType.Vitality, -2)
        {
        }
    }
}
