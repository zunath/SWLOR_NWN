using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SnippetService;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class TransportationSnippetDefinition: ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();
        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions

            // Actions
            ActionTeleport("action-teleport", false);
            ActionTeleport("action-teleport-out-of-combat", true);

            return _builder.Build();
        }

        private void ActionTeleport(string snippetId, bool requireOutOfCombat)
        {
            _builder.Create(snippetId)
                .Description("Teleports a player to the waypoint with the specified tag.")
                .Phrase("sends the player to {waypointTag}")
                .Argument("waypointTag", SnippetArgumentType.WaypointTag)
                .ActionsTakenAction((player, args) =>
                {
                    if (requireOutOfCombat && (GetIsInCombat(player) || Enmity.HasEnmity(player)))
                    {
                        SendMessageToPC(player, "You are in combat.");
                        return false;
                    }

                    if (args.Length <= 0)
                    {
                        var Error = $"'{snippetId}' requires a waypoint tag argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var waypointTag = args[0];
                    var waypoint = GetWaypointByTag(waypointTag);

                    if (!GetIsObjectValid(waypoint))
                    {
                        var error = $"Could not locate waypoint with tag '{waypointTag}' for snippet '{snippetId}'";
                        SendMessageToPC(player, error);
                        Log.Write(LogGroup.Error, error);
                        return false;
                    }

                    var location = GetLocation(waypoint);
                    AssignCommand(player, () => ActionJumpToLocation(location));
                    return true;
                });
        }
    }
}
