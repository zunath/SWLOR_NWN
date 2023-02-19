using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class MedKitAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            MedKit1();
            MedKit2();
            MedKit3();
            MedKit4();
            MedKit5();

            return Builder.Build();
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
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), target);
            TakeMedicalSupplies(activator);

            Enmity.ModifyEnmityOnAll(activator, 250 + amount);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
            if (CombatPoint.GetTaggedCreatureCount(activator) == 0)
            {
                // Scale XP to the thing we just fought -- only give XP if we're not in combat.
                // Retrieve the level of our recent enemy from the CombatPoint service, and use the Skill service 
                // delta function to get base XP based on relative level.
                // If AddCombatPoint... returns 0, but GetRecentEnemyLevel returns > -1, then we are out of combat but recently were in combat.
                var enemyLevel = CombatPoint.GetRecentEnemyLevel(activator);
                var playerId = GetObjectUUID(activator);
                var dbPlayer = DB.Get<Player>(playerId);
                var firstAidLevel = dbPlayer.Skills[SkillType.FirstAid].Rank;
                var nXP = enemyLevel != -1 ? Skill.GetDeltaXP(enemyLevel - firstAidLevel) : 0;
                Skill.GiveSkillXP(activator, SkillType.FirstAid, nXP);
                CombatPoint.ClearRecentEnemyLevel(activator);
            }
        }

        private void MedKit1()
        {
            Builder.Create(FeatType.MedKit1, PerkType.MedKit)
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

        private void MedKit2()
        {
            Builder.Create(FeatType.MedKit2, PerkType.MedKit)
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

        private void MedKit3()
        {
            Builder.Create(FeatType.MedKit3, PerkType.MedKit)
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

        private void MedKit4()
        {
            Builder.Create(FeatType.MedKit4, PerkType.MedKit)
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
        private void MedKit5()
        {
            Builder.Create(FeatType.MedKit5, PerkType.MedKit)
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
