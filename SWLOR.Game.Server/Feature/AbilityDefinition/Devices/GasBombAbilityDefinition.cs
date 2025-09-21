using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;


namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class GasBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public GasBombAbilityDefinition(IRandomService random, IItemService itemService, IPerkService perkService, IStatService statService, ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService) 
            : base(random, itemService, perkService, statService, combatService, combatPoint, enmityService)
        {
        }

        private void ApplyEffect(uint creature, int dmg)
        {
            var attackerStat = GetLocalInt(OBJECT_SELF, "DEVICE_ACC");
            var attack = GetLocalInt(OBJECT_SELF, "DEVICE_ATK");
            dmg += GetLocalInt(OBJECT_SELF, "DEVICE_DMG");

            var defense = _statService.GetDefense(creature, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Acid), creature);
        }

        [ScriptHandler(ScriptName.OnGrenadeGas1Enable)]
        public static void GasBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature,  4);
        }

        [ScriptHandler(ScriptName.OnGrenadeGas1Heartbeat)]
        public static void GasBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature,  4);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [ScriptHandler(ScriptName.OnGrenadeGas2Enable)]
        public static void GasBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 12);
        }

        [ScriptHandler(ScriptName.OnGrenadeGas2Heartbeat)]
        public static void GasBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 12);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [ScriptHandler(ScriptName.OnGrenadeGas3Enable)]
        public static void GasBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 16);
        }

        [ScriptHandler(ScriptName.OnGrenadeGas3Heartbeat)]
        public static void GasBomb3Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 16);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            GasBomb1(builder);
            GasBomb2(builder);
            GasBomb3(builder);

            return builder.Build();
        }
        
        private void GasBomb1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.GasBomb1, PerkType.GasBomb)
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

                    _enmityService.ModifyEnmityOnAll(activator, 250);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void GasBomb2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.GasBomb2, PerkType.GasBomb)
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

                    _enmityService.ModifyEnmityOnAll(activator, 350);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void GasBomb3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.GasBomb3, PerkType.GasBomb)
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

                    _enmityService.ModifyEnmityOnAll(activator, 450);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
