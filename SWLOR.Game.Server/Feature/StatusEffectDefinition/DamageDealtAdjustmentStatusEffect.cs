using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class DamageDealtAdjustmentStatusEffect : StatusEffectBase
    {
        private readonly int _damageDealtPercentAdjustment;
        private readonly int _nameStrRef;

        public override string Name => _nameStrRef > 0
            ? GetStringByStrRef(_nameStrRef)
            : "Damage Dealt Adjustment";
        public override EffectIconType Icon { get; }
        public override StatusEffectCategory Categories => _damageDealtPercentAdjustment switch
        {
            < 0 => StatusEffectCategory.Debuff,
            > 0 => StatusEffectCategory.Buff,
            _ => StatusEffectCategory.None,
        };
        public override StatusEffectCleanseType CleanseTypes { get; }
        public override ResistanceType ResistanceType { get; }
        public override bool PersistsOnLogout => false;

        public DamageDealtAdjustmentStatusEffect()
            : this(0, 0, EffectIconType.Invalid, StatusEffectCleanseType.None, ResistanceType.Invalid)
        {
        }

        public DamageDealtAdjustmentStatusEffect(
            int damageDealtPercentAdjustment,
            int nameStrRef,
            EffectIconType icon,
            StatusEffectCleanseType cleanseTypes,
            ResistanceType resistanceType)
        {
            _damageDealtPercentAdjustment = damageDealtPercentAdjustment;
            _nameStrRef = nameStrRef;
            Icon = icon;
            CleanseTypes = cleanseTypes;
            ResistanceType = resistanceType;
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = damageDealtPercentAdjustment;
        }

        public override string CanApply(uint creature)
        {
            if (_damageDealtPercentAdjustment == 0)
                return "Damage Dealt Adjustment requires a non-zero adjustment.";
            if (_nameStrRef <= 0)
                return "Damage Dealt Adjustment requires a configured name strref.";
            return Icon == EffectIconType.Invalid
                ? "Damage Dealt Adjustment requires a configured status icon."
                : string.Empty;
        }

        public override IStatusEffect Clone()
        {
            return new DamageDealtAdjustmentStatusEffect(
                _damageDealtPercentAdjustment,
                _nameStrRef,
                Icon,
                CleanseTypes,
                ResistanceType);
        }
    }
}
