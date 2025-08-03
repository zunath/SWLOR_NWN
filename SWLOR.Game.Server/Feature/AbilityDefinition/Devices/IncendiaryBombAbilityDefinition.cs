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
    public class IncendiaryBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        private static void ApplyEffect(uint creature, int dmg)
        {
            var attackerStat = GetLocalInt(OBJECT_SELF, "DEVICE_ACC");
            var attack = GetLocalInt(OBJECT_SELF, "DEVICE_ATK");
            dmg += GetLocalInt(OBJECT_SELF, "DEVICE_DMG");

            var defense = Stat.GetDefense(creature, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), creature);
        }

        [NWNEventHandler(ScriptName.OnGrenadeIncendiary1Enable)]
        public static void IncendiaryBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 4);
        }

        [NWNEventHandler(ScriptName.OnGrenadeIncendiary1Heartbeat)]
        public static void IncendiaryBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 4);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler(ScriptName.OnGrenadeIncendiary2Enable)]
        public static void IncendiaryBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 10);
        }

        [NWNEventHandler(ScriptName.OnGrenadeIncendiary2Heartbeat)]
        public static void IncendiaryBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 10);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler(ScriptName.OnGrenadeIncendiary3Enable)]
        public static void IncendiaryBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 16);
        }

        [NWNEventHandler(ScriptName.OnGrenadeIncendiary3Heartbeat)]
        public static void IncendiaryBomb3Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 16);
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
        
        private void IncendiaryBomb1()
        {
            _builder.Create(FeatType.IncendiaryBomb1, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc1_en",
                        "grenade_inc1_hb",
                        20f);

                    Enmity.ModifyEnmityOnAll(activator, 250);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void IncendiaryBomb2()
        {
            _builder.Create(FeatType.IncendiaryBomb2, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(20f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc2_en",
                        "grenade_inc2_hb",
                        40f);

                    Enmity.ModifyEnmityOnAll(activator, 350);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void IncendiaryBomb3()
        {
            _builder.Create(FeatType.IncendiaryBomb3, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(25f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc3_en",
                        "grenade_inc3_hb",
                        60f);

                    Enmity.ModifyEnmityOnAll(activator, 450);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
