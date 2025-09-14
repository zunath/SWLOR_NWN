//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class SlamAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            Slam1(builder);
            Slam2(builder);
            Slam3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.StaffBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a staff ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {

            int dmg;
            int dc;
            const float Duration = 12f;

            switch (level)
            {
                default:
                case 1:
                    dmg = 6;
                    dc = 10;
                    break;
                case 2:
                    dmg = 15;
                    dc = 15;
                    break;
                case 3:
                    dmg = 22;
                    dc = 20;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            Enmity.ModifyEnmityOnAll(activator, 100 * level);
            CombatPoint.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            var attackerStat = Combat.GetPerkAdjustedAbilityScore(activator);
            int attack;

            if (GetHasFeat(FeatType.FlurryStyle, activator))
            {
                attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.MartialArts);
            }
            else
            {
                attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.MartialArts);
            }

            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectBlindness(), target, Duration);
            }
        }

        private static void Slam1(AbilityBuilder builder)
        {
            builder.Create(FeatType.Slam1, PerkType.Slam)
                .Name("Slam I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Slam, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void Slam2(AbilityBuilder builder)
        {
            builder.Create(FeatType.Slam2, PerkType.Slam)
                .Name("Slam II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Slam, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void Slam3(AbilityBuilder builder)
        {
            builder.Create(FeatType.Slam3, PerkType.Slam)
                .Name("Slam III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Slam, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}