using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MomentumStatusEffect : StatusEffectBase
    {
        public int Charges { get; }
        public override string Name => $"Flow ({Charges})";
        public override EffectIconType Icon => Charges switch
        {
            2 => EffectIconType.MomentumStatusEffect2,
            3 => EffectIconType.MomentumStatusEffect3,
            _ => EffectIconType.MomentumStatusEffect1
        };
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public MomentumStatusEffect() : this(1) { }
        public MomentumStatusEffect(int charges) => Charges = Math.Clamp(charges, 0, 3);
        public override IStatusEffect Clone() => new MomentumStatusEffect(Charges);
    }
}
