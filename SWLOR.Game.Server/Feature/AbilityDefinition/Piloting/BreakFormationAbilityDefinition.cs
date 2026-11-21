using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Break Formation I-II: enemy ships within command range suffer an Accuracy penalty (and, at II, Weakened)
    /// for 30 s. Ported from the Leadership Break Morale (the numbers, STM and cooldown are the ground's). A ship
    /// weapon rolls Accuracy, not ability hit chance, so the penalty is the Flash status's ship Accuracy stat.
    /// Social raises the penalties, as it does on the ground, and the captain's Leadership rank raises them further
    /// (<see cref="SpaceCommand.ApplyLeadershipScaling"/>).
    /// </summary>
    public class BreakFormationAbilityDefinition : IAbilityListDefinition
    {
        private const float DebuffSeconds = 30f;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.CommandBreakFormation1, PerkType.CommandBreakFormation).Name("Break Formation I").Level(1),
                Spell.CommandBreakFormation1, 7, 10, 12, 0, 0);
            Rank(_builder.Create(FeatType.CommandBreakFormation2, PerkType.CommandBreakFormation).Name("Break Formation II").Level(2),
                Spell.CommandBreakFormation2, 9, 15, 18, 12, 15);

            return _builder.Build();
        }

        private static void Rank(
            AbilityBuilder ability,
            Spell targetingSpell,
            int stamina,
            int accuracyBase,
            int accuracyMaximum,
            int weakenedBase,
            int weakenedMaximum)
        {
            PilotingAbilityConfiguration.ConfigureCommandDebuff(
                ability.HasRecastDelay(RecastGroup.CommandBreakFormation, 45f),
                stamina,
                targetingSpell,
                (activator, _, _, _) => Apply(activator, accuracyBase, accuracyMaximum, weakenedBase, weakenedMaximum));
        }

        private static void Apply(uint activator, int accuracyBase, int accuracyMaximum, int weakenedBase, int weakenedMaximum)
        {
            var affected = new List<uint>();
            foreach (var ship in SpaceCommand.GetEnemyShipsInRange(activator))
            {
                var applied = Space.ApplyStatusToShip(
                    activator,
                    ship,
                    new FlashStatusEffect(SpaceCommand.ApplyLeadershipScaling(activator,
                        AbilityEffectScaling.ScaleValueBySourceSocial(activator, accuracyBase, accuracyMaximum))),
                    DebuffSeconds);

                if (weakenedBase > 0)
                {
                    applied |= Space.ApplyStatusToShip(
                        activator,
                        ship,
                        new WeakenedStatusEffect(SpaceCommand.ApplyLeadershipScaling(activator,
                            AbilityEffectScaling.ScaleValueBySourceSocial(activator, weakenedBase, weakenedMaximum))),
                        DebuffSeconds);
                }

                if (!applied)
                    continue;

                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), ship);
                Ability.PlaySuccessfulImpactVisualEffect(activator, ship);
                affected.Add(ship);
            }

            if (affected.Count == 0)
                return;

            SpaceCommand.TryMarkTarget(activator);

            // Every enemy it debuffs pays the pilot Piloting XP, whether or not the pilot has hit it.
            foreach (var ship in affected)
                CombatPoint.AddCombatPoint(SpaceCrew.GetAbilityUser(activator), ship, SkillType.Piloting, 2);
        }
    }
}
