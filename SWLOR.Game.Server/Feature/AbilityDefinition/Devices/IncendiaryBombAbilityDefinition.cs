﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class IncendiaryBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        private static void ApplyEffect(uint creature, float dmg)
        {
            var defense = Stat.GetDefense(creature, CombatDamageType.Physical) +
                          Stat.GetDefense(creature, CombatDamageType.Fire);
            var vitality = GetAbilityModifier(AbilityType.Vitality, creature);
            var damage = Combat.CalculateDamage(dmg, 0, defense, vitality, 0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), creature);
        }

        [NWNEventHandler("grenade_inc1_en")]
        public static void IncendiaryBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 1.5f);
        }

        [NWNEventHandler("grenade_inc1_hb")]
        public static void IncendiaryBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 1.5f);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler("grenade_inc2_en")]
        public static void IncendiaryBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 5.0f);
        }

        [NWNEventHandler("grenade_inc2_hb")]
        public static void IncendiaryBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 5.0f);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler("grenade_inc3_en")]
        public static void IncendiaryBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 7.5f);
        }

        [NWNEventHandler("grenade_inc3_hb")]
        public static void IncendiaryBomb3Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 7.5f);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            IncendiaryBomb1();
            IncendiaryBomb2();
            IncendiaryBomb3();

            return _builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!HasExplosives(activator))
            {
                return "You have no explosives.";
            }

            return string.Empty;
        }

        private void IncendiaryBomb1()
        {
            _builder.Create(FeatType.IncendiaryBomb1, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb I")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc1_en",
                        "grenade_inc1_hb",
                        20f);

                    Enmity.ModifyEnmityOnAll(activator, 30);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void IncendiaryBomb2()
        {
            _builder.Create(FeatType.IncendiaryBomb2, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb II")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(20f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc2_en",
                        "grenade_inc2_hb",
                        40f);
                });
        }

        private void IncendiaryBomb3()
        {
            _builder.Create(FeatType.IncendiaryBomb3, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb III")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(25f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc3_en",
                        "grenade_inc3_hb",
                        60f);
                });
        }
    }
}
