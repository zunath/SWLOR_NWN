using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.General
{
    public class DashAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Dash();

            return _builder.Build();
        }

        private void Dash()
        {
            _builder.Create(FeatType.Dash, PerkType.Dash)
                .Name("Dash")
                .HasImpactAction((activator, target, level, location) =>
                {
                    if (!GetIsPC(target) || GetIsDM(target))
                        return;

                    // Flip the database toggle.
                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);

                    if (!dbPlayer.AbilityToggles.ContainsKey(AbilityToggleType.Dash))
                        dbPlayer.AbilityToggles[AbilityToggleType.Dash] = false;

                    dbPlayer.AbilityToggles[AbilityToggleType.Dash] = !dbPlayer.AbilityToggles[AbilityToggleType.Dash];
                    DB.Set(dbPlayer);

                    // Determine movement rate adjustment
                    float rate;
                    switch (level)
                    {
                        case 1:
                            rate = 0.10f; // 10%
                            break;
                        case 2:
                            rate = 0.25f; // 25%
                            break;
                        default:
                            rate = 0f; // 0%
                            break;
                    }

                    // Apply the movement rate change.
                    var movementRate = CreaturePlugin.GetMovementRateFactor(target);
                    string message;
                    if (dbPlayer.AbilityToggles[AbilityToggleType.Dash])
                    {
                        movementRate += rate;
                        message = ColorToken.Green("Dash enabled");
                    }
                    else
                    {
                        movementRate -= rate;
                        message = ColorToken.Red("Dash disabled");
                    }

                    CreaturePlugin.SetMovementRateFactor(target, movementRate);
                    
                    SendMessageToPC(target, message);
                });
        }
    }
}
