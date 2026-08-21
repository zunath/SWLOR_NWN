using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Anchors the Duelist's Distance icon identity for the gameplay-icon pipeline. Shared combat
    /// applies DamageDealtAdjustmentStatusEffect with all identity metadata supplied by StatType.
    /// </summary>
    public sealed class DuelistsDistanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Duelist's Distance";
        public override EffectIconType Icon => EffectIconType.DuelistsDistanceStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.TreatmentKit1;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
    }
}
