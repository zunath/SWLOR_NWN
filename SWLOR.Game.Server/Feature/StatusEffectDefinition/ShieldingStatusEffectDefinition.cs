using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ShieldingStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new StatusEffectBuilder();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Shielding();

            return _builder.Build();
        }

        private void Shielding()
        {
            _builder.Create(StatusEffectType.Shielding1)
                .Name("Shielding I")
                .EffectIcon(35);

            _builder.Create(StatusEffectType.Shielding2)
                .Name("Shielding II")
                .EffectIcon(35);

            _builder.Create(StatusEffectType.Shielding3)
                .Name("Shielding III")
                .EffectIcon(35);

            _builder.Create(StatusEffectType.Shielding4)
                .Name("Shielding IV")
                .EffectIcon(35);
        }
    }
}
