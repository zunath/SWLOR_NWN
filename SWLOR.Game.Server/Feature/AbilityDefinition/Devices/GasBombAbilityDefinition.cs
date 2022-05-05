using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;


namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class GasBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        private static void ApplyEffect(uint creature, int dmg)
        {
            var defense = Stat.GetDefense(creature, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(0, dmg, 0, defense, defenderStat, 0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Acid), creature);
        }

        [NWNEventHandler("grenade_gas1_en")]
        public static void GasBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature,  2);
        }

        [NWNEventHandler("grenade_gas1_hb")]
        public static void GasBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature,  2);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler("grenade_gas2_en")]
        public static void GasBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 6);
        }

        [NWNEventHandler("grenade_gas2_hb")]
        public static void GasBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 6);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler("grenade_gas3_en")]
        public static void GasBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 8);
        }

        [NWNEventHandler("grenade_gas3_hb")]
        public static void GasBomb3Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 8);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            GasBomb1();
            GasBomb2();
            GasBomb3();

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

        private void GasBomb1()
        {
            _builder.Create(FeatType.GasBomb1, PerkType.GasBomb)
                .Name("Gas Bomb I")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
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
                        AreaOfEffect.FogAcid,
                        "grenade_gas1_en",
                        "grenade_gas1_hb",
                        18f);

                    Enmity.ModifyEnmityOnAll(activator, 30);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void GasBomb2()
        {
            _builder.Create(FeatType.GasBomb2, PerkType.GasBomb)
                .Name("Gas Bomb II")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
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
                        AreaOfEffect.FogAcid,
                        "grenade_gas2_en",
                        "grenade_gas2_hb",
                        30f);
                });
        }

        private void GasBomb3()
        {
            _builder.Create(FeatType.GasBomb3, PerkType.GasBomb)
                .Name("Gas Bomb III")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(6)
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
                        AreaOfEffect.FogAcid,
                        "grenade_gas3_en",
                        "grenade_gas3_hb",
                        48f);
                });
        }
    }
}
