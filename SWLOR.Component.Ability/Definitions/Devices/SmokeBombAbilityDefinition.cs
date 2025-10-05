using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Devices
{
    public class SmokeBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public SmokeBombAbilityDefinition(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {
        }

        private void ApplyEffect(uint creature)
        {
            ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), creature, 6f);
        }

        public void SmokeBombEnter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature);
        }

        public void SmokeBombHeartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SmokeBomb1(builder);
            SmokeBomb2(builder);
            SmokeBomb3(builder);

            return builder.Build();
        }
        
        private void SmokeBomb1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SmokeBomb1, PerkType.SmokeBomb)
                .Name("Smoke Bomb I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator, 
                        targetLocation,
                        AreaOfEffectType.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        20f);

                    EnmityService.ModifyEnmityOnAll(activator, 90);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void SmokeBomb2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SmokeBomb2, PerkType.SmokeBomb)
                .Name("Smoke Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(20f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffectType.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        40f);

                    EnmityService.ModifyEnmityOnAll(activator, 180);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void SmokeBomb3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SmokeBomb3, PerkType.SmokeBomb)
                .Name("Smoke Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(25f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffectType.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        60f);

                    EnmityService.ModifyEnmityOnAll(activator, 360);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
