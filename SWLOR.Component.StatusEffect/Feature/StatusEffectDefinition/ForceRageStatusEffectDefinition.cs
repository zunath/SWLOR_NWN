using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Model;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class ForceRageStatusEffectDefinition : IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            ForceRage1();
            ForceRage2();

            return _builder.Build();
        }

        private void ForceRage1()
        {
            _builder.Create(StatusEffectType.ForceRage1)
                .Name("Force Rage I")
                .EffectIcon(EffectIconType.DamageIncrease)
                .CannotReplace(StatusEffectType.ForceRage2);
        }

        private void ForceRage2()
        {
            _builder.Create(StatusEffectType.ForceRage2)
                .Name("Force Rage II")
                .EffectIcon(EffectIconType.DamageIncrease)
                .Replaces(StatusEffectType.ForceRage1);
        }
    }
}
