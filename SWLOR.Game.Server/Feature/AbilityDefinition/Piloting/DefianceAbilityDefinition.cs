using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Defiance I-III: the ship's next landed weapon hit deals bonus DMG, generates threat, and gives the ship
    /// Defense for 30 s. Ported from the Heavy Vibroblade Fortress Strike (the numbers, STM and cooldown are the
    /// ground's) with the same Fortress Strike status. The threat is flat threat plus the damage dealt, through
    /// the pilot's threat stats.
    /// </summary>
    public class DefianceAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.BulwarkDefiance1, PerkType.BulwarkDefiance).Name("Defiance I").Level(1), 10, 350, 10, 4);
            Rank(_builder.Create(FeatType.BulwarkDefiance2, PerkType.BulwarkDefiance).Name("Defiance II").Level(2), 20, 450, 20, 7);
            Rank(_builder.Create(FeatType.BulwarkDefiance3, PerkType.BulwarkDefiance).Name("Defiance III").Level(3), 30, 550, 30, 10);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, int bonusDmg, int enmity, int defensePercent, int stamina)
        {
            PilotingAbilityConfiguration.ConfigureRider(
                ability.HasRecastDelay(RecastGroup.BulwarkDefiance, 18f),
                stamina,
                (activator, _, _, _) => SpaceBulwark.ArmDefiance(activator, bonusDmg, enmity, defensePercent));
        }
    }
}
