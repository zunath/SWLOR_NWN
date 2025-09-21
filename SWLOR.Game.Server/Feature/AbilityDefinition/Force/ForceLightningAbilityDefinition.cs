using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceLightningAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public ForceLightningAbilityDefinition(ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceLightning1(builder);
            ForceLightning2(builder);
            ForceLightning3(builder);
            ForceLightning4(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var willBonus = GetAbilityScore(activator, AbilityType.Willpower);

            switch (level)
            {
                case 1:
                    dmg = willBonus;
                    break;
                case 2:
                    dmg = 10 + (willBonus * 3 / 2);
                    break;
                case 3:
                    dmg = 20 + (willBonus * 2);
                    break;
                case 4:
                    dmg = 30 + (willBonus * 3);
                    break;
            }

            var combatService = App.Resolve<ICombatService>();
            var statService = App.Resolve<IStatService>();
            var combatPointService = App.Resolve<ICombatPointService>();
            var enmityService = App.Resolve<IEnmityService>();

            dmg += combatService.GetAbilityDamageBonus(activator, SkillType.Force);
            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Huge, GetLocation(target), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count <= 5)
            {
                if (GetIsReactionTypeHostile(creature, activator) && GetIsDead(creature) == false)
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    var defense = statService.GetDefense(creature, CombatDamageType.Force, AbilityType.Willpower);
                    var attack = statService.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
                    var defenderStat = GetAbilityScore(creature, AbilityType.Willpower);
                    var damage = combatService.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var elecBeam = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Hand, true);
                    var elecBurst = EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_S);
                    var dTarget = creature;

                    AssignCommand(activator, () =>
                    {
                        PlaySound("frc_lghtning");
                        ActionPlayAnimation(Animation.CastOutAnimation, 1.0f, 3.0f);
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), dTarget);
                        ApplyEffectToObject(DurationType.Temporary, elecBeam, dTarget, 2.5f);
                        ApplyEffectToObject(DurationType.Instant, elecBurst, dTarget);
                    });

                    combatPointService.AddCombatPoint(activator, creature, SkillType.Force, 3);
                    enmityService.ModifyEnmity(activator, creature, 100 * level + damage);
                    count++;
                }
                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Huge, GetLocation(target), true, ObjectType.Creature);
            }
            if (statService.GetCurrentFP(activator) < 1 + (level * 2))
            {
                var darkBargain = 7 * ((5 + (level * 2) - statService.GetCurrentFP(activator)));
                statService.ReduceFP(activator, statService.GetCurrentFP(activator));
                ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
            }
            else { statService.ReduceFP(activator, 5 + (level * 2)); }
        }

        private static void ForceLightning1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
                .Name("Force Lightning I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning2, PerkType.ForceLightning)
                .Name("Force Lightning II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning3, PerkType.ForceLightning)
                .Name("Force Lightning III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }

        private static void ForceLightning4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning4, PerkType.ForceLightning)
                .Name("Force Lightning IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .HasImpactAction(ImpactAction);
        }
    }
}
