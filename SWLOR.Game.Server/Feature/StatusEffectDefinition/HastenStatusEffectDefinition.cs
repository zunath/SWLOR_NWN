using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class HastenStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Hasten1(builder);
            Hasten2(builder);
            Hasten3(builder);

            return builder.Build();
        }

        private void Hasten1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Hasten1)
                .Name("Hasten I")
                .EffectIcon(EffectIconType.Haste);
        }

        private void Hasten2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Hasten2)
                .Name("Hasten II")
                .EffectIcon(EffectIconType.Haste);
        }

        private void Hasten3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Hasten3)
                .Name("Hasten III")
                .EffectIcon(EffectIconType.Haste);
        }
    }
}
