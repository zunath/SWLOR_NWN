using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PoisonDefensePenaltyStatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Poison";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override bool PersistsOnLogout => false;

        public PoisonDefensePenaltyStatusEffect()
            : base(StatType.Defense, -2)
        {
        }
    }
}
