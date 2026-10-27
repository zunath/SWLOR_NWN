using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Hail I-II: the pilot goads an enemy ship (Hail I) or an enemy ship and the enemy ships near it (Hail II)
    /// into attacking them. Ported from the Armor Provoke (the enmity, cooldown and catch-up rule are the
    /// ground's): the enmity is scaled by VIT, the pilot's threat catches up to the highest visible rival once, and
    /// there is no target lock. A goad fires no weapon and costs no STM.
    /// </summary>
    public class HailAbilityDefinition : IAbilityListDefinition
    {
        private const int HailEnmity = 700;
        private const int HailAreaEnmity = 400;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            PilotingAbilityConfiguration.ConfigureGoad(
                _builder.Create(FeatType.BulwarkHail1, PerkType.BulwarkHail)
                    .Name("Hail I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.BulwarkHail, 6f),
                (activator, target, _, _) =>
                    Space.Goad(activator, target, Stat.ScaleEffect(HailEnmity, GetAbilityScore(activator, AbilityType.Vitality))));

            PilotingAbilityConfiguration.ConfigureAreaGoad(
                _builder.Create(FeatType.BulwarkHail2, PerkType.BulwarkHail)
                    .Name("Hail II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.BulwarkHail2, 12f),
                Spell.BulwarkHail2,
                SpaceRadii.NearTarget,
                (activator, target, _, _) =>
                    Space.GoadArea(
                        activator,
                        target,
                        SpaceRadii.NearTarget,
                        int.MaxValue,
                        Stat.ScaleEffect(HailAreaEnmity, GetAbilityScore(activator, AbilityType.Vitality))));

            return _builder.Build();
        }
    }
}
