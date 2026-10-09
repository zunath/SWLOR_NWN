using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class HostileAbilityPartyBuffStatusEffect : StatusEffectBase
    {
        private readonly int _damagePercent;
        private readonly int _accuracyPercent;
        private readonly int _nameStrRef;

        public override string Name => _nameStrRef > 0 ? GetStringByStrRef(_nameStrRef) : "Party Command";
        public override EffectIconType Icon { get; }
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;

        public HostileAbilityPartyBuffStatusEffect()
            : this(0, 0, 0, EffectIconType.Invalid)
        {
        }

        public HostileAbilityPartyBuffStatusEffect(int damagePercent, int accuracyPercent, int nameStrRef, EffectIconType icon)
        {
            _damagePercent = damagePercent;
            _accuracyPercent = accuracyPercent;
            _nameStrRef = nameStrRef;
            Icon = icon;
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = damagePercent;
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = accuracyPercent;
        }

        public override string CanApply(uint creature)
        {
            return (_damagePercent <= 0 && _accuracyPercent <= 0) || _nameStrRef <= 0 || Icon == EffectIconType.Invalid
                ? "Party Command requires bonuses, a name, and an icon."
                : string.Empty;
        }

        public override IStatusEffect Clone()
        {
            return new HostileAbilityPartyBuffStatusEffect(_damagePercent, _accuracyPercent, _nameStrRef, Icon);
        }
    }
}
