using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Devices
{
    public class GasBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public GasBombAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
            : base(serviceProvider, statCalculation)
        {
        }

        private void ApplyEffect(uint creature, int dmg)
        {
            var attackerStat = GetLocalInt(OBJECT_SELF, "DEVICE_ACC");
            var attack = GetLocalInt(OBJECT_SELF, "DEVICE_ATK");
            dmg += GetLocalInt(OBJECT_SELF, "DEVICE_DMG");

            var defense = _statCalculation.CalculateDefense(creature);
            var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Acid), creature);
        }

        public void GasBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature,  4);
        }

        public void GasBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature,  4);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public void GasBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 12);
        }

        public void GasBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 12);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public void GasBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 16);
        }

        public void GasBomb3Heartbeat()
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
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(AnimationType.ThrowGrenade)
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
                        AreaOfEffectType.FogAcid,
                        "grenade_gas1_en",
                        "grenade_gas1_hb",
                        18f);

                    EnmityService.ModifyEnmityOnAll(activator, 250);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void GasBomb2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.GasBomb2, PerkType.GasBomb)
                .Name("Gas Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(AnimationType.ThrowGrenade)
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
                        AreaOfEffectType.FogAcid,
                        "grenade_gas2_en",
                        "grenade_gas2_hb",
                        30f);

                    EnmityService.ModifyEnmityOnAll(activator, 350);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void GasBomb3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.GasBomb3, PerkType.GasBomb)
                .Name("Gas Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(6)
                .UsesAnimation(AnimationType.ThrowGrenade)
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
                        AreaOfEffectType.FogAcid,
                        "grenade_gas3_en",
                        "grenade_gas3_hb",
                        48f);

                    EnmityService.ModifyEnmityOnAll(activator, 450);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
