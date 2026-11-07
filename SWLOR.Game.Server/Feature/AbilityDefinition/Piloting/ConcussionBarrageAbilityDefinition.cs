using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Concussion Barrage I-II: one launcher fires at up to five enemy ships near the pilot's ship and dazes them.
    /// Ported from the Throwing Concussive Toss (damage, STM, cooldown, target limit and Dazed duration are the
    /// ground's), with its 5 m burst scaled to space (<see cref="SpaceRadii.NearShip"/>). The status system's hard
    /// crowd-control rules apply to Dazed.
    /// </summary>
    public class ConcussionBarrageAbilityDefinition : IAbilityListDefinition
    {
        private const float DazedSeconds = 15f;
        private const int MaximumTargets = 5;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.OrdnanceConcussionBarrage1, PerkType.OrdnanceConcussionBarrage).Name("Concussion Barrage I").Level(1), Spell.OrdnanceConcussionBarrage1, 16, 6);
            Rank(_builder.Create(FeatType.OrdnanceConcussionBarrage2, PerkType.OrdnanceConcussionBarrage).Name("Concussion Barrage II").Level(2), Spell.OrdnanceConcussionBarrage2, 30, 9);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, Spell targetingSpell, int bonusDmg, int stamina)
        {
            PilotingAbilityConfiguration.ConfigureShipBurst(
                ability.HasRecastDelay(RecastGroup.OrdnanceConcussionBarrage, 30f),
                stamina,
                targetingSpell,
                SpaceRadii.NearShip,
                SpaceWeaponFilter.Launcher,
                (activator, _, _, _) =>
                {
                    var results = SpaceAbilities.FireWeaponArea(
                        activator,
                        GetLocation(activator),
                        SpaceRadii.NearShip,
                        MaximumTargets,
                        SpaceWeaponFilter.Launcher,
                        bonusDmg);
                    if (!results.Any(x => x.Fired))
                    {
                        PilotingAbilityConfiguration.ReportFailure(activator, results);
                        return;
                    }

                    foreach (var hit in results.Where(x => x.IsHit))
                        Space.ApplyStatusToShip(activator, hit.Target, typeof(DazedStatusEffect), DazedSeconds);

                    SpaceOrdnance.OnAreaAbilityResolved(activator, results);
                });
        }
    }
}
