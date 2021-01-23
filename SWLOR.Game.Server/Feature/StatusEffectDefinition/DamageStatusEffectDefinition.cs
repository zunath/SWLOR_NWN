using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class DamageStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Bleed(builder);
            Poison(builder);
            Shock(builder);

            return builder.Build();
        }

        private void Bleed(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Bleed)
                .Name("Bleed")
                .EffectIcon(129) // 129 = Wounding
                .TickAction((source, target) =>
                {
                    var damage = EffectDamage(d2());
                    ApplyEffectToObject(DurationType.Instant, damage, target);

                    var location = GetLocation(target);
                    var placeable = CreateObject(ObjectType.Placeable, "plc_bloodstain", location);

                    DestroyObject(placeable, 48.0f);
                });
        }

        private void Poison(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Poison)
                .Name("Poison")
                .EffectIcon(20) // 20 = Poison
                .TickAction((source, target) =>
                {
                    var amount = Random.Next(3, 7);
                    var damage = EffectDamage(amount, DamageType.Acid);
                    var decreasedAC = EffectACDecrease(2);

                    ApplyEffectToObject(DurationType.Instant, damage, target);
                    ApplyEffectToObject(DurationType.Temporary, decreasedAC, target, 1.0f);

                });
        }

        private void Shock(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Bleed)
                .Name("Shock")
                .EffectIcon(129) // 129 = Wounding
                .TickAction((source, target) =>
                {
                    var damage = EffectDamage(d4(), DamageType.Electrical);
                    ApplyEffectToObject(DurationType.Instant, damage, target);

                    var location = GetLocation(target);                                       
                });
        }

    }
}
