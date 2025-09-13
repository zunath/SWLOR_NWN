using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;


namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class DeflectorShieldAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private const string Tier1Tag = "ABILITY_DEFLECTOR_SHIELD_1";
        private const string Tier2Tag = "ABILITY_DEFLECTOR_SHIELD_2";
        private const string Tier3Tag = "ABILITY_DEFLECTOR_SHIELD_3";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            DeflectorShield1();
            DeflectorShield2();
            DeflectorShield3();

            return _builder.Build();
        }

        private string Validation(uint target, int tier)
        {
            if (HasMorePowerfulEffect(target, tier,
                    new(Tier1Tag, 1),
                    new(Tier2Tag, 2),
                    new(Tier3Tag, 3)))
            {
                return "Your target is already enhanced by a more powerful effect.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, float percent, bool affectsParty, float duration, string tag)
        {
            ApplyEffect(activator, percent, duration, tag);
            if (affectsParty)
            {
                var member = GetFirstFactionMember(activator);
                while (GetIsObjectValid(member))
                {
                    if (member != activator &&
                        GetDistanceBetween(activator, member) <= 10f)
                    {
                        ApplyEffect(member, percent, duration, tag);
                    }

                    member = GetNextFactionMember(activator);
                }
            }

            Enmity.ModifyEnmityOnAll(activator, 220);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
        }

        private void ApplyEffect(uint target, float percent, float duration, string tag)
        {
            RemoveEffectByTag(target, Tier1Tag, Tier2Tag, Tier3Tag);

            var maxHP = (int)(GetMaxHitPoints(target) * percent);
            var effect = EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue);
            effect = EffectLinkEffects(effect, EffectTemporaryHitpoints(maxHP));
            effect = TagEffect(effect, tag);

            ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);
        }

        private void DeflectorShield1()
        {
            _builder.Create(FeatType.DeflectorShield1, PerkType.DeflectorShield)
                .Name("Deflector Shield I")
                .Level(1)
                .HasRecastDelay(RecastGroup.DeflectorShield, 600f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.Kneel)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 1))
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, 0.05f, false, 180f, Tier1Tag);
                });
        }

        private void DeflectorShield2()
        {
            _builder.Create(FeatType.DeflectorShield2, PerkType.DeflectorShield)
                .Name("Deflector Shield II")
                .Level(2)
                .HasRecastDelay(RecastGroup.DeflectorShield, 600f)
                .HasActivationDelay(3f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.Kneel)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 2))
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, 0.10f, false, 300f, Tier2Tag);
                });
        }

        private void DeflectorShield3()
        {
            _builder.Create(FeatType.DeflectorShield3, PerkType.DeflectorShield)
                .Name("Deflector Shield III")
                .Level(3)
                .HasRecastDelay(RecastGroup.DeflectorShield, 600f)
                .HasActivationDelay(3f)
                .RequirementStamina(9)
                .UsesAnimation(Animation.Kneel)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 3))
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, 0.15f, true, 300f, Tier3Tag);
                });
        }
    }
}
