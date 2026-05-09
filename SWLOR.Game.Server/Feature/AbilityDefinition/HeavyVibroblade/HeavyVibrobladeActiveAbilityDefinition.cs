using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class HeavyVibrobladeActiveAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AbsoluteDefense(builder);
            AngerStrike(builder);
            BastionStance(builder);
            BlazingSpikes(builder);
            Bloodlust(builder);
            EssenceHunter(builder);
            Flash(builder);
            GuardiansResolve(builder);
            LifeSiphon(builder);
            Rampart(builder);
            SoulAscension(builder);
            SoulDevourer(builder);
            SoulSacrifice(builder);
            SoulStorm(builder);
            SoulStrike(builder);

            return builder.Build();
        }

        private static void AbsoluteDefense(AbilityBuilder builder)
        {
            builder.Create(FeatType.AbsoluteDefense1, PerkType.AbsoluteDefense)
                .Name("Absolute Defense")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.AbsoluteDefense, 1800f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ApplyStatusToNearbyParty(activator, typeof(AbsoluteDefenseStatusEffect), 15f, false);
                    ApplyImmunityToNearbyParty(activator, ImmunityType.Knockdown, 15f, false);
                    ApplyImmunityToNearbyParty(activator, ImmunityType.Dazed, 15f, false);

                    var healAmount = (int)Math.Ceiling(GetMaxHitPoints(activator) * 0.25f);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(healAmount), activator);
                    Stat.RestoreStamina(activator, (int)Math.Ceiling(Stat.GetMaxStamina(activator) * 0.25f));
                    Stat.RestoreFP(activator, (int)Math.Ceiling(Stat.GetMaxFP(activator) * 0.25f));
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), activator);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void AngerStrike(AbilityBuilder builder)
        {
            builder.Create(FeatType.AngerStrike1, PerkType.AngerStrike)
                .Name("Anger Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.AngerStrike, 45f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 12, 0, 0, SavingThrow.Will, null, false);
                    Enmity.ModifyEnmity(activator, target, 450 + damage);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void BastionStance(AbilityBuilder builder)
        {
            builder.Create(FeatType.BastionStance1, PerkType.BastionStance)
                .Name("Bastion Stance")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.BastionStance, 180f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(BastionStanceStatusEffect)))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(BastionStanceStatusEffect)))
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void BlazingSpikes(AbilityBuilder builder)
        {
            builder.Create(FeatType.BlazingSpikes1, PerkType.BlazingSpikes)
                .Name("Blazing Spikes")
                .Level(1)
                .HasActivationDelay(0f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(BlazingSpikesStatusEffect)))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(BlazingSpikesStatusEffect)))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void Bloodlust(AbilityBuilder builder)
        {
            builder.Create(FeatType.Bloodlust1, PerkType.Bloodlust)
                .Name("Bloodlust")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    SacrificeHitPoints(activator, 40, 10);
                    var restorePercent = Math.Min(80, 20 + Math.Max(0, GetAbilityModifier(AbilityType.Might, activator)));
                    var amount = (int)Math.Ceiling(Stat.GetMaxStamina(activator) * (restorePercent / 100f));
                    Stat.RestoreStamina(activator, amount);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), activator);
                })
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void EssenceHunter(AbilityBuilder builder)
        {
            builder.Create(FeatType.EssenceHunter1, PerkType.EssenceHunter)
                .Name("Essence Hunter")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.EssenceHunter, 45f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 18, 12, 15, SavingThrow.Fortitude, typeof(EssenceDrainStatusEffect), false);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void Flash(AbilityBuilder builder)
        {
            builder.Create(FeatType.Flash1, PerkType.Flash)
                .Name("Flash")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 0, 30, 0, SavingThrow.Will, typeof(FlashStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f, centerOnActivator: true);
                    Enmity.ModifyEnmityOnAll(activator, 650);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();
        }

        private static void GuardiansResolve(AbilityBuilder builder)
        {
            builder.Create(FeatType.GuardiansResolve1, PerkType.GuardiansResolve)
                .Name("Guardian's Resolve")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardiansResolve, 90f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var shield = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(activator) * 0.3f));
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(shield), activator, 30f);
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(GuardiansResolveStatusEffect), 30f);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void LifeSiphon(AbilityBuilder builder)
        {
            builder.Create(FeatType.LifeSiphon1, PerkType.LifeSiphon)
                .Name("Life Siphon")
                .Level(1)
                .HasActivationDelay(0f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(LifeSiphonStatusEffect)))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(LifeSiphonStatusEffect)))
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void Rampart(AbilityBuilder builder)
        {
            builder.Create(FeatType.Rampart1, PerkType.Rampart)
                .Name("Rampart")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Rampart, 180f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ApplyStatusToNearbyParty(activator, typeof(RampartStatusEffect), 60f, true);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void SoulAscension(AbilityBuilder builder)
        {
            builder.Create(FeatType.SoulAscension1, PerkType.SoulAscension)
                .Name("Soul Ascension")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulAscension, 1800f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(SoulAscensionStatusEffect), 20f);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void SoulDevourer(AbilityBuilder builder)
        {
            builder.Create(FeatType.SoulDevourer1, PerkType.SoulDevourer)
                .Name("Soul Devourer")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.SoulDevourer, 180f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(SoulDevourerStatusEffect)))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(SoulDevourerStatusEffect)))
                .IsCastedAbility()
                .BreaksStealth();
        }

        private static void SoulSacrifice(AbilityBuilder builder)
        {
            builder.Create(FeatType.SoulSacrifice1, PerkType.SoulSacrifice)
                .Name("Soul Sacrifice")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulSacrifice, 180f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    SacrificeHitPoints(activator, 50, 20);
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(SoulSacrificeStatusEffect), 30f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Odd), activator);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void SoulStorm(AbilityBuilder builder)
        {
            builder.Create(FeatType.SoulStorm1, PerkType.SoulStorm)
                .Name("Soul Storm")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulStorm, 300f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    SacrificeHitPoints(activator, 40, 10);
                    ApplyStatusToNearbyParty(activator, typeof(SoulStormStatusEffect), 30f, true);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Mind), activator);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(18);
        }

        private static void SoulStrike(AbilityBuilder builder)
        {
            builder.Create(FeatType.SoulStrike1, PerkType.SoulStrike)
                .Name("Soul Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulStrike, 45f)
                .HasImpactAction((activator, target, level, targetLocation) => SoulStrikeImpact(activator, target, targetLocation, 15, 25))
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);

            builder.Create(FeatType.SoulStrike2, PerkType.SoulStrike)
                .Name("Soul Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulStrike, 45f)
                .HasImpactAction((activator, target, level, targetLocation) => SoulStrikeImpact(activator, target, targetLocation, 30, 40))
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);

            builder.Create(FeatType.SoulStrike3, PerkType.SoulStrike)
                .Name("Soul Strike III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulStrike, 45f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var percent = Math.Min(90, 60 + Math.Max(0, GetAbilityModifier(AbilityType.Might, activator)));
                    SoulStrikeImpact(activator, target, targetLocation, 45, percent);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(15);
        }

        private static bool ToggleSelfStatus(uint activator, Type type)
        {
            if (!StatusEffect.HasStatusEffect(activator, type))
                return true;

            StatusEffect.RemoveStatusEffect(activator, type, false);
            SendMessageToPC(activator, $"{StatusEffect.GetStatusEffectName(type)} deactivated.");
            return false;
        }

        private static void ApplySelfStatus(uint activator, Type type)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, type, 0f);
        }

        private static void SoulStrikeImpact(uint activator, uint target, Location targetLocation, int damageBonus, int healingPercent)
        {
            var damage = Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, damageBonus, 0, 0, SavingThrow.Will, null, false);
            HealFromDamage(activator, damage, healingPercent);
        }

        private static void HealFromDamage(uint target, int damage, int healingPercent)
        {
            if (damage <= 0 || healingPercent <= 0)
                return;

            var amount = Math.Max(1, (int)Math.Ceiling(damage * (healingPercent / 100f)));
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        private static void SacrificeHitPoints(uint activator, int basePercent, int minimumPercent)
        {
            var percent = Math.Max(minimumPercent, basePercent - Math.Max(0, GetAbilityModifier(AbilityType.Might, activator)));
            var amount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(activator) * (percent / 100f)));
            var currentHp = GetCurrentHitPoints(activator);

            if (currentHp <= 1)
                return;

            amount = Math.Min(currentHp - 1, amount);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(amount), activator);
        }

        private static void ApplyStatusToNearbyParty(uint activator, Type type, float duration, bool includeSelf)
        {
            if (includeSelf)
            {
                StatusEffect.ApplyStatusEffect(activator, activator, type, duration);
            }

            var location = GetLocation(activator);
            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    StatusEffect.ApplyStatusEffect(activator, creature, type, duration);
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
            }
        }

        private static void ApplyImmunityToNearbyParty(uint activator, ImmunityType immunity, float duration, bool includeSelf)
        {
            if (includeSelf)
            {
                Ability.ApplyTemporaryImmunity(activator, duration, immunity);
            }

            var location = GetLocation(activator);
            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    Ability.ApplyTemporaryImmunity(creature, duration, immunity);
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
            }
        }
    }
}
