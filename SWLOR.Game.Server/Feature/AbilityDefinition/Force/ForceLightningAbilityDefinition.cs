//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceLightningAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceLightning1(builder);
            ForceLightning2(builder);
            ForceLightning3(builder);
            ForceLightning4(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);

            switch (level)
            {
                case 1:
                    dmg = 10 + (willBonus * 2);
                    break;
                case 2:
                    dmg = 16 + (willBonus * 4);
                    break;
                case 3:
                    dmg = 22 + (willBonus * 6);
                    break;
                case 4:
                    dmg = 30 + (willBonus * 8);
                    break;
            }
            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);
            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Huge, GetLocation(target), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count <= 5)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    var defense = Stat.GetDefense(creature, CombatDamageType.Force, AbilityType.Willpower);
                    var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
                    var defenderStat = GetAbilityScore(creature, AbilityType.Willpower);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var elecBeam = EffectBeam(VisualEffect.Vfx_Beam_Lightning, activator, BodyNode.Hand);
                    var elecChain = EffectBeam(VisualEffect.Vfx_Beam_Lightning, creature, BodyNode.Chest);
                    var dTarget = creature;

                    AssignCommand(activator, () =>
                    {
                        PlaySound("frc_lghtning");
                        ActionPlayAnimation(Animation.CastOutAnimation, 1.0f, 3.0f);
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), dTarget);
                        ApplyEffectToObject(DurationType.Temporary, elecBeam, dTarget, 3.0f);
                    });

                    CombatPoint.AddCombatPoint(activator, creature, SkillType.Force, 3);
                    Enmity.ModifyEnmity(activator, creature, 100 * level + damage);
                    count++;
                }
                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Huge, GetLocation(target), true, ObjectType.Creature);
            }
            if (Stat.GetCurrentFP(activator) < 1 + (level * 2))
            {
                var darkBargain = 7 * ((5 + (level * 2) - Stat.GetCurrentFP(activator)));
                Stat.ReduceFP(activator, Stat.GetCurrentFP(activator));
                ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
            }
            else { Stat.ReduceFP(activator, 5 + (level * 2)); }
        }

        private static void ForceLightning1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
                .Name("Force Lightning I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning2, PerkType.ForceLightning)
                .Name("Force Lightning II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning3, PerkType.ForceLightning)
                .Name("Force Lightning III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning4, PerkType.ForceLightning)
                .Name("Force Lightning IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
    }
}