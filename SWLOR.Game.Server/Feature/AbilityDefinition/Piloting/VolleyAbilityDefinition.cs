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
    /// Volley I-IV: one launcher fires at the target ship and the blast reaches every enemy ship near it, setting
    /// them burning. Ported from the Throwing Explosive Toss (damage, STM, cooldown and Burn duration are the
    /// ground's), with its 5 m blast scaled to space (<see cref="SpaceRadii.NearTarget"/>).
    /// </summary>
    public class VolleyAbilityDefinition : IAbilityListDefinition
    {
        private const float BurnSeconds = 30f;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.OrdnanceVolley1, PerkType.OrdnanceVolley).Name("Volley I").Level(1), Spell.OrdnanceVolley1, 7, 3);
            Rank(_builder.Create(FeatType.OrdnanceVolley2, PerkType.OrdnanceVolley).Name("Volley II").Level(2), Spell.OrdnanceVolley2, 15, 5);
            Rank(_builder.Create(FeatType.OrdnanceVolley3, PerkType.OrdnanceVolley).Name("Volley III").Level(3), Spell.OrdnanceVolley3, 24, 8);
            Rank(_builder.Create(FeatType.OrdnanceVolley4, PerkType.OrdnanceVolley).Name("Volley IV").Level(4), Spell.OrdnanceVolley4, 32, 12);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, Spell targetingSpell, int bonusDmg, int stamina)
        {
            PilotingAbilityConfiguration.ConfigureTargetedBurst(
                ability.HasRecastDelay(RecastGroup.OrdnanceVolley, 18f),
                stamina,
                targetingSpell,
                SpaceRadii.NearTarget,
                PilotingAbilityConfiguration.LauncherRange,
                SpaceWeaponFilter.Launcher,
                (activator, target, _, _) =>
                {
                    var results = SpaceAbilities.FireWeaponArea(
                        activator,
                        GetLocation(target),
                        SpaceRadii.NearTarget,
                        int.MaxValue,
                        SpaceWeaponFilter.Launcher,
                        bonusDmg,
                        primaryTarget: target);
                    if (!results.Any(x => x.Fired))
                    {
                        PilotingAbilityConfiguration.ReportFailure(activator, results);
                        return;
                    }

                    foreach (var hit in results.Where(x => x.IsHit))
                        Space.ApplyStatusToShip(activator, hit.Target, typeof(BurnStatusEffect), BurnSeconds);

                    SpaceOrdnance.OnAreaAbilityResolved(activator, results);
                });
        }
    }
}
