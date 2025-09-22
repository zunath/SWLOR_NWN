using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class SmokeBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public SmokeBombAbilityDefinition(IRandomService random, IItemService itemService, IPerkService perkService, IStatService statService, ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService) 
            : base(random, itemService, perkService, statService, combatService, combatPointService, enmityService, statusEffectService)
        {
        }

        private static void ApplyEffect(uint creature)
        {
            ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), creature, 6f);
        }

        [ScriptHandler(ScriptName.OnGrenadeSmokeEnable)]
        public static void SmokeBombEnter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature);
        }

        [ScriptHandler(ScriptName.OnGrenadeSmokeHeartbeat)]
        public static void SmokeBombHeartbeat()
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
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator, 
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        20f);

                    _enmityService.ModifyEnmityOnAll(activator, 90);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void SmokeBomb2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SmokeBomb2, PerkType.SmokeBomb)
                .Name("Smoke Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(20f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        40f);

                    _enmityService.ModifyEnmityOnAll(activator, 180);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void SmokeBomb3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SmokeBomb3, PerkType.SmokeBomb)
                .Name("Smoke Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(25f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        60f);

                    _enmityService.ModifyEnmityOnAll(activator, 360);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
