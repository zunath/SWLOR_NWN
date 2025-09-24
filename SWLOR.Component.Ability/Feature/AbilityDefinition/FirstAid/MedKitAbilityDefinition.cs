using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid
{
    public class MedKitAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        private readonly IRandomService _random;
        private readonly IDatabaseService _db;
        private readonly ISkillService _skillService;
        private readonly IBeastMasteryService _beastMastery;

        public MedKitAbilityDefinition(
            IRandomService random, 
            IDatabaseService db, 
            ISkillService skillService, 
            ICombatPointService combatPointService, 
            IEnmityService enmityService, 
            IBeastMasteryService beastMastery, 
            IAbilityService abilityService, 
            IPerkService perkService,
            IStatusEffectService statusEffect) 
            : base(
                random, 
                perkService, 
                combatPointService, 
                enmityService, 
                abilityService,
                statusEffect)
        {
            _random = random;
            _db = db;
            _skillService = skillService;
            _beastMastery = beastMastery;
        }
        
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

            if (_beastMastery.IsPlayerBeast(target))
            {
                return "That ability cannot be used on beasts.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            var willpowerMod = GetAbilityModifier(AbilityType.Willpower, activator);
            var amount = baseAmount + willpowerMod * 20 + _random.D10(1);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), target);
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
                var dbPlayer = _db.Get<Player>(playerId);
                var firstAidLevel = dbPlayer.Skills[SkillType.FirstAid].Rank;
                var nXP = enemyLevel != -1 ? _skillService.GetDeltaXP(enemyLevel - firstAidLevel) : 0;
                _skillService.GiveSkillXP(activator, SkillType.FirstAid, nXP);
                CombatPointService.ClearRecentEnemyLevel(activator);
            }
        }

        private void MedKit1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.MedKit1, PerkType.MedKit)
                .Name("Med Kit I")
                .Level(1)
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.LoopingGetMid)
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
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.LoopingGetMid)
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
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(6)
                .UsesAnimation(Animation.LoopingGetMid)
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
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.LoopingGetMid)
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
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(8)
                .UsesAnimation(Animation.LoopingGetMid)
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
