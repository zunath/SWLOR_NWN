using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.StatusEffectService;
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
            Tranquilize(builder);
            Burn(builder);

            return builder.Build();
        }

        private void Bleed(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Bleed)
                .Name("Bleed")
                .EffectIcon(EffectIconType.Wounding)
                .TickAction((source, target, effectData) =>
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
                .EffectIcon(EffectIconType.Poison)
                .TickAction((source, target, effectData) =>
                {
                    var amount = Random.Next(3, 7);
                    var damage = EffectDamage(amount, DamageType.Acid);
                    var decreasedAC = EffectACDecrease(2);

                    ApplyEffectToObject(DurationType.Instant, damage, target);
                    ApplyEffectToObject(DurationType.Temporary, decreasedAC, target, 6.0f);

                });
        }

        private void Shock(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Shock)
                .Name("Shock")
                .EffectIcon(EffectIconType.Shocked)
                .TickAction((source, target, effectData) =>
                {
                    var damage = EffectDamage(d4(), DamageType.Electrical);
                    ApplyEffectToObject(DurationType.Instant, damage, target);

                    var location = GetLocation(target);                                       
                });
        }

        private void Tranquilize(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Tranquilize)
                .Name("Tranquilize")
                .EffectIcon(EffectIconType.Sleep)
                .GrantAction((source, target, length, effectData) =>
                {
                    var effect = EffectSleep();
                    effect = EffectLinkEffects(effect, EffectVisualEffect(Core.NWScript.Enum.VisualEffect.VisualEffect.Vfx_Dur_Iounstone_Blue));
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.Tranquilize);

                    ApplyEffectToObject(DurationType.Permanent, effect, target, length);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.Tranquilize);
                });
        }

        private void Burn(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Burn)
                .Name("Burn")
                .EffectIcon(EffectIconType.Burning)
                .TickAction((source, target, effectData) =>
                {
                    var amount = Random.Next(2, 4);
                    var damage = EffectDamage(amount, DamageType.Fire);

                    ApplyEffectToObject(DurationType.Instant, damage, target);
                });
        }

    }
}
