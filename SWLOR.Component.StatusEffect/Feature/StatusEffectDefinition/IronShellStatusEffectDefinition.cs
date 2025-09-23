using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Model;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class IronShellStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            IronShell();

            return _builder.Build();
        }

        private void IronShell()
        {
            _builder.Create(StatusEffectType.IronShell)
                .Name("Iron Shell")
                .EffectIcon(EffectIconType.ElementalShield);
        }
    }
}
