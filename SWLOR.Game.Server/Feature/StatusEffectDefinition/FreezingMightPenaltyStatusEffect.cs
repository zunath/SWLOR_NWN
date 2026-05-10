using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FreezingMightPenaltyStatusEffect : StaticAbilityStatusEffectBase
    {
        public override string Name => "Freezing";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override bool PersistsOnLogout => false;
        public override ResistanceType ResistanceType => ResistanceType.Ice;

        public FreezingMightPenaltyStatusEffect()
            : base(AbilityType.Might, -2)
        {
        }
    }
}
