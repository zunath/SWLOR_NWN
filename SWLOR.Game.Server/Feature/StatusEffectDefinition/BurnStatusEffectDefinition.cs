using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class BurnStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder()
                .Create(StatusEffectType.Burn)
                .Name("Burn")
                .EffectIcon(131)
                .TickAction((source, target) =>
                {
                    AssignCommand(source, () =>
                    {
                        var damage = Random.D6(1);
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
                    });
                });

            return builder.Build();
        }
    }
}
