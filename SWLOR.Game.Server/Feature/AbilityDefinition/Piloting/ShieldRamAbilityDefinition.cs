using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Shield Ram I-IV: the ship's next landed weapon hit also deals an instance of damage equal to a percent of
    /// the ship's Defense. Ported from the Vibroblade Shield Bash (the percents, STM and cooldown are the ground's).
    /// It waits 30 s for the hit, as a queued ability does.
    /// </summary>
    public class ShieldRamAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.BulwarkShieldRam1, PerkType.BulwarkShieldRam).Name("Shield Ram I").Level(1), 4, 3);
            Rank(_builder.Create(FeatType.BulwarkShieldRam2, PerkType.BulwarkShieldRam).Name("Shield Ram II").Level(2), 6, 5);
            Rank(_builder.Create(FeatType.BulwarkShieldRam3, PerkType.BulwarkShieldRam).Name("Shield Ram III").Level(3), 8, 6);
            Rank(_builder.Create(FeatType.BulwarkShieldRam4, PerkType.BulwarkShieldRam).Name("Shield Ram IV").Level(4), 10, 8);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, int defensePercent, int stamina)
        {
            PilotingAbilityConfiguration.ConfigureRider(
                ability.HasRecastDelay(RecastGroup.BulwarkShieldRam, 20f),
                stamina,
                (activator, _, _, _) => SpaceBulwark.ArmShieldRam(activator, defensePercent));
        }
    }
}
