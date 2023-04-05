using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class BeastStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Assault();

            return _builder.Build();
        }

        private void Assault()
        {
            _builder.Create(StatusEffectType.Assault)
                .Name("Assault")
                .EffectIcon(EffectIconType.ACIncrease);
        }
    }
}
