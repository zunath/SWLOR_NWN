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
            var attackerStat = GetLocalInt(OBJECT_SELF, "DEVICE_ACC");
            var attack = GetLocalInt(OBJECT_SELF, "DEVICE_ATK");
            dmg += GetLocalInt(OBJECT_SELF, "DEVICE_DMG");

            var defense = Stat.GetDefense(creature, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Acid), creature);
        }

        [NWNEventHandler(ScriptName.OnGrenadeGas1Enable)]
        public static void GasBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature,  4);
        }

        [NWNEventHandler(ScriptName.OnGrenadeGas1Heartbeat)]
        public static void GasBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature,  4);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler(ScriptName.OnGrenadeGas2Enable)]
        public static void GasBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 12);
        }

        [NWNEventHandler(ScriptName.OnGrenadeGas2Heartbeat)]
        public static void GasBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 12);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler(ScriptName.OnGrenadeGas3Enable)]
        public static void GasBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 16);
        }

        [NWNEventHandler(ScriptName.OnGrenadeGas3Heartbeat)]
        public static void GasBomb3Heartbeat()
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
            GasBomb1();
            GasBomb2();
            GasBomb3();

            return _builder.Build();
        }
        
        private void GasBomb1()
        {
            _builder.Create(FeatType.GasBomb1, PerkType.GasBomb)
                .Name("Gas Bomb I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
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
                        AreaOfEffect.FogAcid,
                        "grenade_gas1_en",
                        "grenade_gas1_hb",
                        18f);

                    Enmity.ModifyEnmityOnAll(activator, 250);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void GasBomb2()
        {
            _builder.Create(FeatType.GasBomb2, PerkType.GasBomb)
                .Name("Gas Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
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
                        AreaOfEffect.FogAcid,
                        "grenade_gas2_en",
                        "grenade_gas2_hb",
                        30f);

                    Enmity.ModifyEnmityOnAll(activator, 350);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void GasBomb3()
        {
            _builder.Create(FeatType.GasBomb3, PerkType.GasBomb)
                .Name("Gas Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(6)
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
                        AreaOfEffect.FogAcid,
                        "grenade_gas3_en",
                        "grenade_gas3_hb",
                        48f);

                    Enmity.ModifyEnmityOnAll(activator, 450);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
