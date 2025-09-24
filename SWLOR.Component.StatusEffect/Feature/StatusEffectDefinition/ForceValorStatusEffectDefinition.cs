using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class ForceValorStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            ForceValor1();
            ForceValor2();

            return _builder.Build();
        }

        private void ForceValor1()
        {
            _builder.Create(StatusEffectType.ForceValor1)
                .Name("Force Valor I")
                .EffectIcon(EffectIconType.DamageImmunityIncrease)
                .CannotReplace(StatusEffectType.ForceValor2);
        }

        private void ForceValor2()
        {
            _builder.Create(StatusEffectType.ForceValor2)
                .Name("Force Valor II")
                .EffectIcon(EffectIconType.DamageImmunityIncrease)
                .Replaces(StatusEffectType.ForceValor1);
        }
    }
}
