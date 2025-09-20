using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Shared.Core.Log;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class TransportationSnippetDefinition: ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();
        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions

            // Actions
            ActionTeleport();

            return _builder.Build();
        }

        private void ActionTeleport()
        {
            _builder.Create("action-teleport")
                .Description("Teleports a player to the waypoint with the specified tag.")
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'action-teleport' requires a waypoint tag argument.";
                        SendMessageToPC(player, Error);
                        LogLegacy.Write(LogGroupType.Error, Error);
                        return;
                    }

                    var waypointTag = args[0];
                    var waypoint = GetWaypointByTag(waypointTag);

                    if (!GetIsObjectValid(waypoint))
                    {
                        var error = $"Could not locate waypoint with tag '{waypointTag}' for snippet 'action-teleport'";
                        SendMessageToPC(player, error);
                        LogLegacy.Write(LogGroupType.Error, error);
                        return;
                    }

                    var location = GetLocation(waypoint);
                    AssignCommand(player, () => ActionJumpToLocation(location));
                });
        }
    }
}
