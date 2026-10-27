using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Ace in the Hole: one energy weapon shot, and a critical hit gives the ship Dead Man's Hand: its next three
    /// weapon cycles have +20% Critical Rate and no extra delay. Ported from the Pistol's Dead Man's Hand.
    /// </summary>
    public class AceInTheHoleAbilityDefinition : IAbilityListDefinition
    {
        private const int BonusDmg = 30;
        private const int Stamina = 15;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            PilotingAbilityConfiguration.ConfigureTargetedShot(
                _builder.Create(FeatType.AceInTheHole, PerkType.AceInTheHole)
                    .Name("Ace in the Hole")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, 90f),
                Stamina,
                SpaceWeaponFilter.Energy,
                (activator, target, _, _) =>
                {
                    var result = SpaceAbilities.FireWeapon(activator, target, SpaceWeaponFilter.Energy, BonusDmg);
                    if (!result.Fired)
                    {
                        PilotingAbilityConfiguration.ReportFailure(activator, new[] { result });
                        return;
                    }

                    if (result.IsCritical)
                        StatusEffect.ApplyStatusEffect(activator, activator, new DeadMansHandStatusEffect(), 0f);
                });

            return _builder.Build();
        }
    }
}
