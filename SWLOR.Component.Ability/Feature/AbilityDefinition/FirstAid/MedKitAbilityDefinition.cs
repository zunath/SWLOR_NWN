using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid
{
    public class MedKitAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public MedKitAbilityDefinition(
            IServiceProvider serviceProvider,
            ICombatPointService combatPointService, 
            IEnmityService enmityService, 
            IAbilityService abilityService, 
            IPerkService perkService,
            IStatusEffectService statusEffect) 
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private IDatabaseService DB => _serviceProvider.GetRequiredService<IDatabaseService>();
        private ISkillService SkillService => _serviceProvider.GetRequiredService<ISkillService>();
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();
        
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            MedKit1(builder);
            MedKit2(builder);
            MedKit3(builder);
            MedKit4(builder);
            MedKit5(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!IsWithinRange(activator, target))
            {
                return "Your target is too far away.";
            }

            if (GetCurrentHitPoints(target) >= GetMaxHitPoints(target))
            {
                return "Your target is unharmed.";
            }

            if (!HasMedicalSupplies(activator))
            {
                return "You have no medical supplies.";
            }

            if (BeastMastery.IsPlayerBeast(target))
            {
                return "That ability cannot be used on beasts.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            var willpowerMod = GetAbilityModifier(AbilityType.Willpower, activator);
            var amount = baseAmount + willpowerMod * 20 + Random.D10(1);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Head_Heal), target);
            TakeMedicalSupplies(activator);

            EnmityService.ModifyEnmityOnAll(activator, 150 + amount);
            CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
            if (CombatPointService.GetTaggedCreatureCount(activator) == 0)
            {
                // Scale XP to the thing we just fought -- only give XP if we're not in combat.
                // Retrieve the level of our recent enemy from the CombatPoint service, and use the Skill service 
                // delta function to get base XP based on relative level.
                // If Add_combatPoint... returns 0, but GetRecentEnemyLevel returns > -1, then we are out of combat but recently were in combat.
                var enemyLevel = CombatPointService.GetRecentEnemyLevel(activator);
                var playerId = GetObjectUUID(activator);
                var dbPlayer = DB.Get<Player>(playerId);
                var firstAidLevel = dbPlayer.Skills[SkillType.FirstAid].Rank;
                var nXP = enemyLevel != -1 ? SkillService.GetDeltaXP(enemyLevel - firstAidLevel) : 0;
                SkillService.GiveSkillXP(activator, SkillType.FirstAid, nXP);
                CombatPointService.ClearRecentEnemyLevel(activator);
            }
        }

        private void MedKit1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MedKit1, PerkType.MedKit)
                .Name("Med Kit I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(4)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 30);
                });
        }

        private void MedKit2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MedKit2, PerkType.MedKit)
                .Name("Med Kit II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(5)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 50);
                });
        }

        private void MedKit3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MedKit3, PerkType.MedKit)
                .Name("Med Kit III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(6)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 80);
                });
        }

        private void MedKit4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MedKit4, PerkType.MedKit)
                .Name("Med Kit IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(7)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 110);
                });
        }
        private void MedKit5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MedKit5, PerkType.MedKit)
                .Name("Med Kit V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(8)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 140);
                });
        }
    }
}
