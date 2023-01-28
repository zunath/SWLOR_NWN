using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.StatusEffectService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class DamageStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Bleed();
            Disease();
            Poison();
            Shock();
            Burn();
            Freezing();

            return _builder.Build();
        }

        private void Bleed()
        {
            _builder.Create(StatusEffectType.Bleed)
                .Name("Bleed")
                .EffectIcon(EffectIconType.Wounding)
                .TickAction((source, target, effectData) =>
                {
                    var level = effectData == null ? 1 : (int)effectData;
                    if (level < 1)
                        level = 1;

                    var perception = GetAbilityModifier(AbilityType.Perception, source);
                    var damage = EffectDamage(d2() + perception * 2 * level);
                    ApplyEffectToObject(DurationType.Instant, damage, target);

                    var location = GetLocation(target);
                    var placeable = CreateObject(ObjectType.Placeable, "plc_bloodstain", location);

                    DestroyObject(placeable, 48.0f);
                });
        }

        private void Disease()
        {
            _builder.Create(StatusEffectType.Disease)
                .Name("Disease")
                .EffectIcon(EffectIconType.Disease)
                .TickAction((source, target, effectData) =>
                {
                    var level = effectData == null ? 1 : (int)effectData;
                    if (level < 1)
                        level = 1;

                    var perception = GetAbilityModifier(AbilityType.Perception, source);
                    var damage = EffectDamage(d2() + perception * level);
                    ApplyEffectToObject(DurationType.Instant, damage, target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Disease_S), target);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Vitality, 2), target, 5.9f);
                });
        }

        private void Poison()
        {
            _builder.Create(StatusEffectType.Poison)
                .Name("Poison")
                .EffectIcon(EffectIconType.Poison)
                .TickAction((source, target, effectData) =>
                {
                    var level = effectData == null ? 1 : (int)effectData;
                    if (level < 1)
                        level = 1;

                    var agility = GetAbilityModifier(AbilityType.Agility, source);
                    var amount = Random.Next(3, 7) + agility * level;
                    var damage = EffectDamage(amount, DamageType.Acid);
                    var decreasedAC = EffectACDecrease(2);

                    ApplyEffectToObject(DurationType.Instant, damage, target);
                    ApplyEffectToObject(DurationType.Temporary, decreasedAC, target, 6.0f);

                });
        }

        private void Shock()
        {
            _builder.Create(StatusEffectType.Shock)
                .Name("Shock")
                .EffectIcon(EffectIconType.Shocked)
                .TickAction((source, target, effectData) =>
                {
                    var level = effectData == null ? 1 : (int)effectData;
                    if (level < 1)
                        level = 1;

                    var agility = GetAbilityModifier(AbilityType.Agility, source);
                    var damage = EffectDamage(d4() + agility * 2 * level, DamageType.Electrical);
                    ApplyEffectToObject(DurationType.Instant, damage, target);                                  
                });
        }
        
        private void Burn()
        {
            _builder.Create(StatusEffectType.Burn)
                .Name("Burn")
                .EffectIcon(EffectIconType.Burning)
                .TickAction((source, target, effectData) =>
                {
                    var level = effectData == null ? 1 : (int)effectData;
                    if (level < 1)
                        level = 1;

                    var might = GetAbilityModifier(AbilityType.Might, source);
                    var amount = Random.Next(2, 4) + might * 2 * level;
                    var damage = EffectDamage(amount, DamageType.Fire);

                    ApplyEffectToObject(DurationType.Instant, damage, target);
                });
        }

        private void Freezing()
        {
            _builder.Create(StatusEffectType.Freezing)
                .Name("Freezing")
                .EffectIcon(EffectIconType.DamageImmunityColdDecrease)
                .TickAction((source, target, effectData) =>
                {
                    var level = effectData == null ? 1 : (int)effectData;
                    if (level < 1)
                        level = 1;

                    var perception = GetAbilityModifier(AbilityType.Perception, source);
                    var damage = EffectDamage(d2() + perception * level);
                    ApplyEffectToObject(DurationType.Instant, damage, target);
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), target, 5.9f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Might, 2), target, 5.9f);
                });
        }
    }
}
