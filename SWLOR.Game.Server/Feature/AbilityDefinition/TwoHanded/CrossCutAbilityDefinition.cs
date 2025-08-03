//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class CrossCutAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CrossCut1(builder);
            CrossCut2(builder);
            CrossCut3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.TwinBladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a twin-blade ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            int dc;
            int acLoss;
            const float Duration = 60f;

            switch (level)
            {
                default:
                case 1:
                    dmg = 8;
                    acLoss = 2;
                    dc = 10;
                    break;
                case 2:
                    dmg = 17;
                    acLoss = 4;
                    dc = 15;
                    break;
                case 3:
                    dmg = 25;
                    acLoss = 6;
                    dc = 20;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense,
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                RemoveEffectByTag(target, "CROSS_CUT");
                var breach = TagEffect(EffectACDecrease(acLoss), "CROSS_CUT");
                ApplyEffectToObject(DurationType.Temporary, breach, target, Duration);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.CrossCut));
            DelayCommand(0.2f, () =>
            {
                AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleStrike));
            });

            CombatPoint.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            Enmity.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private static void CrossCut1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut1, PerkType.CrossCut)
                .Name("Cross Cut I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private static void CrossCut2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut2, PerkType.CrossCut)
                .Name("Cross Cut II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private static void CrossCut3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrossCut3, PerkType.CrossCut)
                .Name("Cross Cut III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CrossCut, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
    }
}