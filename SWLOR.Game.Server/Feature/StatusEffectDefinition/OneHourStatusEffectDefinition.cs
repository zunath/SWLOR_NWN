using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class OneHourStatusEffectDefinition : IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            ElementalSeal(builder);
            Manafont(builder);

            return builder.Build();
        }

        private void ElementalSeal(StatusEffectBuilder builder)
        {
            builder
                .Create(StatusEffectType.ElementalSeal)
                .Name("Elemental Seal")
                .EffectIcon(132);
        }

        private void Manafont(StatusEffectBuilder builder)
        {
            builder
                .Create(StatusEffectType.Manafont)
                .Name("Manafont")
                .EffectIcon(156);
        }
    }
}