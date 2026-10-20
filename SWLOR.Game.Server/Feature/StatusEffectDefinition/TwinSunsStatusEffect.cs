using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TwinSunsStatusEffect : StatusEffectBase
    {
        public int Attempts { get; }
        public override string Name => $"Twin Suns ({Attempts} shots)";
        public override EffectIconType Icon => EffectIconType.TwinSunsStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public TwinSunsStatusEffect() : this(6) { }
        public TwinSunsStatusEffect(int attempts) => Attempts = Math.Clamp(attempts, 0, 6);
        public override IStatusEffect Clone() => new TwinSunsStatusEffect(Attempts);
    }
}
