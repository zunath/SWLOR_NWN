using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class PommelStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            PommelStrike1(builder);
            PommelStrike2(builder);
            PommelStrike3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (Item.LightsaberBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;
            }
            else
                return "A lightsaber must be equipped in your right hand to use this ability.";
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            int dmg;
            int dc;
            const float Duration = 6f;

            var stat = AbilityType.Perception;
            if (Ability.IsAbilityToggled(activator, AbilityToggleType.StrongStyleLightsaber))
            {
                stat = AbilityType.Might;
            }

            switch (level)
            {
                default:
                case 1:
                    dmg = 6;
                    dc = 10;
                    break;
                case 2:
                    dmg = 12;
                    dc = 12;
                    break;
                case 3:
                    dmg = 24;
                    dc = 15;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            CombatPoint.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var attackerStat = Combat.GetPerkAdjustedAbilityScore(activator);
            var attack = Stat.GetAttack(activator, stat, SkillType.OneHanded);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            // Tiered base + full modifier on attack stat (Perception or Might w/ Strong Style); rank III reaches DC 25 at score 30 (+10 mod).
            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Will, dc, stat);
            var checkResult = WillSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectDazed(), target, Duration);
                Ability.ApplyTemporaryImmunity(target, Duration, ImmunityType.Dazed);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.RiotBlade));

            Enmity.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private static void PommelStrike1(AbilityBuilder builder)
        {
            builder.Create(FeatType.PommelStrike1, PerkType.PommelStrike)
                .Name("Pommel Strike I")
                .Level(1)
                .HasRecastDelay(RecastGroup.PommelStrike, 20f)
                .RequirementStamina(4)
                .HasActivationDelay(0.5f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void PommelStrike2(AbilityBuilder builder)
        {
            builder.Create(FeatType.PommelStrike2, PerkType.PommelStrike)
                .Name("Pommel Strike II")
                .Level(2)
                .HasRecastDelay(RecastGroup.PommelStrike, 20f)
                .RequirementStamina(6)
                .HasActivationDelay(0.5f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void PommelStrike3(AbilityBuilder builder)
        {
            builder.Create(FeatType.PommelStrike3, PerkType.PommelStrike)
                .Name("Pommel Strike III")
                .Level(3)
                .HasRecastDelay(RecastGroup.PommelStrike, 20f)
                .RequirementStamina(8)
                .HasActivationDelay(0.5f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
