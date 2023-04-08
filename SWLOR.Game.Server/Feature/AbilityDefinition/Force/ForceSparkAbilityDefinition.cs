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
    public class ForceSparkAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private const string Tier1Tag = "ABILITY_FORCE_SPARK_1";
        private const string Tier2Tag = "ABILITY_FORCE_SPARK_2";
        private const string Tier3Tag = "ABILITY_FORCE_SPARK_3";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ForceSpark1();
            ForceSpark2();
            ForceSpark3();

            return _builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var willBonus = GetAbilityScore(activator, AbilityType.Willpower);
            var dmg = 0;
            string effectTag = "";
            int dc = 0;
            int evaDecrease = 0;

            switch (level)
            {
                case 1:
                    dmg = willBonus;
                    effectTag = "Tier1Tag";
                    dc = 8;
                    evaDecrease = 2;
                    break;
                case 2:
                    dmg = 10 + (willBonus * 3 / 2);
                    effectTag = "Tier2Tag";
                    dc = 12;
                    evaDecrease = 4;
                    break;
                case 3:
                    dmg = 20 + (willBonus * 2);
                    effectTag = "Tier3Tag";
                    dc = 14;
                    evaDecrease = 6;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);

            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var damage = Combat.CalculateDamage(
                attack,
                dmg,
                attackerStat,
                defense,
                defenderStat,
                0);

            if (HasMorePowerfulEffect(target, level,
                    new(Tier1Tag, 1),
                    new(Tier2Tag, 2),
                    new(Tier3Tag, 3)))
            {
                SendMessageToPC(activator, "Your target is already afflicted by a more powerful effect.");
            }
            else
            {
                RemoveEffectByTag(target, Tier1Tag, Tier2Tag, Tier3Tag);

                dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Will, dc);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

                if (checkResult == SavingThrowResultType.Failed)
                {
                    var breach = TagEffect(EffectACDecrease(evaDecrease), effectTag);
                    ApplyEffectToObject(DurationType.Temporary, breach, target, 60f);
                    Messaging.SendMessageNearbyToPlayers(target, $"{GetName(target)} receives the effect of evasion down.");
                }
            }
            var elecBeam = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Hand);
            var elecImpact = EffectBeam(VisualEffect.Vfx_Com_Hit_Electrical, activator, BodyNode.Hand);

            AssignCommand(activator, () =>
            {
                PlaySound("frc_lghtning");
                ActionPlayAnimation(Animation.CastOutAnimation, 1.0f, 4.0f);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Temporary, elecBeam, target, 1.0f);
            });

            Enmity.ModifyEnmity(activator, target, level * 150 + damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);

            if (Stat.GetCurrentFP(activator) < 2 + (level))
            {
                var darkBargain = 7 * ((2 + level - Stat.GetCurrentFP(activator)));
                Stat.ReduceFP(activator, Stat.GetCurrentFP(activator));
                ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
            }
            else { Stat.ReduceFP(activator, 2 + level); }
        }

        private void ForceSpark1()
        {
            _builder.Create(FeatType.ForceSpark1, PerkType.ForceSpark)
                .Name("Force Spark I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceSpark, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private void ForceSpark2()
        {
            _builder.Create(FeatType.ForceSpark2, PerkType.ForceSpark)
                .Name("Force Spark II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceSpark, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private void ForceSpark3()
        {
            _builder.Create(FeatType.ForceSpark3, PerkType.ForceSpark)
                .Name("Force Spark III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceSpark, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
    }
}