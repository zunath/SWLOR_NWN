using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CadenceStatusEffect : StatusEffectBase
    {
        public int Charges { get; }
        public override string Name => $"Cadence ({Charges})";
        public override EffectIconType Icon => Charges switch
        {
            2 => EffectIconType.CadenceStatusEffect2,
            3 => EffectIconType.CadenceStatusEffect3,
            _ => EffectIconType.CadenceStatusEffect1
        };
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public CadenceStatusEffect() : this(1) { }
        public CadenceStatusEffect(int charges) => Charges = Math.Clamp(charges, 0, 3);
        public override IStatusEffect Clone() => new CadenceStatusEffect(Charges);
    }
}
