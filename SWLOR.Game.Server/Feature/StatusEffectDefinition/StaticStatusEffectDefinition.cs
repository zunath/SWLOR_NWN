using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class StaticStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder()
                .Create(StatusEffectType.Static)
                .Name("Static")
                .EffectIcon(139)
                .TickAction((activator, target) =>
                {
                    var damage = Random.D4(1);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
                });

            return builder.Build();
        }
    }
}
