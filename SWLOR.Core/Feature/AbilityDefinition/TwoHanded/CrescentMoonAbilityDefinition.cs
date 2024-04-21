//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.CombatService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.SkillService;

namespace SWLOR.Core.Feature.AbilityDefinition.TwoHanded
{
    public class CrescentMoonAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CrescentMoon1(builder);
            CrescentMoon2(builder);
            CrescentMoon3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!Item.HeavyVibrobladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a heavy vibroblade ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth) == true)
                SetActionMode(activator, ActionMode.Stealth, false);
            int dmg;
            int dc;
            const float Duration = 3f;

            switch (level)
            {
                default:
                case 1:
                    dmg = 12;
                    dc = 10;
                    break;
                case 2:
                    dmg = 21;
                    dc = 15;
                    break;
                case 3:
                    dmg = 34;
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

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, Duration);
                Ability.ApplyTemporaryImmunity(target, Duration, ImmunityType.Stun);
            }
            
            CombatPoint.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            Enmity.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private static void CrescentMoon1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon1, PerkType.CrescentMoon)
                .Name("Crescent Moon I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CrescentMoon2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon2, PerkType.CrescentMoon)
                .Name("Crescent Moon II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void CrescentMoon3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CrescentMoon3, PerkType.CrescentMoon)
                .Name("Crescent Moon III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CrescentMoon, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}