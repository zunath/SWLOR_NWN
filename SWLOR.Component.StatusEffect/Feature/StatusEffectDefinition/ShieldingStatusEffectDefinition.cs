using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class ShieldingStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Shielding();

            return _builder.Build();
        }

        private void Shielding()
        {
            _builder.Create(StatusEffectType.Shielding1)
                .Name("Shielding I")
                .EffectIcon(EffectIconType.DamageImmunityIncrease)
                .CannotReplace(StatusEffectType.Shielding2, StatusEffectType.Shielding3, StatusEffectType.Shielding4);

            _builder.Create(StatusEffectType.Shielding2)
                .Name("Shielding II")
                .EffectIcon(EffectIconType.DamageImmunityIncrease)
                .Replaces(StatusEffectType.Shielding1)
                .CannotReplace(StatusEffectType.Shielding3, StatusEffectType.Shielding4);

            _builder.Create(StatusEffectType.Shielding3)
                .Name("Shielding III")
                .EffectIcon(EffectIconType.DamageImmunityIncrease)
                .Replaces(StatusEffectType.Shielding1, StatusEffectType.Shielding2)
                .CannotReplace(StatusEffectType.Shielding4);

            _builder.Create(StatusEffectType.Shielding4)
                .Name("Shielding IV")
                .EffectIcon(EffectIconType.DamageImmunityIncrease)
                .Replaces(StatusEffectType.Shielding1, StatusEffectType.Shielding2, StatusEffectType.Shielding3);
        }
    }
}
