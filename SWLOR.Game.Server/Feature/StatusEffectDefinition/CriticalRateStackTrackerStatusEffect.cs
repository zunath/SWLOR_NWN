using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class CriticalRateStackTrackerStatusEffect : StatusEffectBase
    {
        private readonly string _name;
        private readonly EffectIconType _icon;

        public override string Name => _name;
        public override EffectIconType Icon => _icon;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public CriticalRateStackTrackerStatusEffect()
            : this("Next ability Critical Rate", EffectIconType.Invalid)
        {
        }

        public CriticalRateStackTrackerStatusEffect(string name, EffectIconType icon)
        {
            _name = string.IsNullOrWhiteSpace(name) ? "Next ability Critical Rate" : name;
            _icon = icon;
        }

        public override string CanApply(uint creature)
        {
            return _icon == EffectIconType.Invalid
                ? "Critical Rate stack tracker requires a configured status icon."
                : string.Empty;
        }

        public override IStatusEffect Clone()
        {
            return new CriticalRateStackTrackerStatusEffect(_name, _icon);
        }
    }
}
