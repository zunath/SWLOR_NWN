using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;

namespace SWLOR.Component.World.Feature.SnippetDefinition
{
    public class TransportationSnippetDefinition: ISnippetListDefinition
    {
        private readonly ILogger _logger;
        private readonly SnippetBuilder _builder = new();

        public TransportationSnippetDefinition(ILogger logger)
        {
            _logger = logger;
        }
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
                        _logger.Write<ErrorLogGroup>(Error);
                        return;
                    }

                    var waypointTag = args[0];
                    var waypoint = GetWaypointByTag(waypointTag);

                    if (!GetIsObjectValid(waypoint))
                    {
                        var error = $"Could not locate waypoint with tag '{waypointTag}' for snippet 'action-teleport'";
                        SendMessageToPC(player, error);
                        _logger.Write<ErrorLogGroup>(error);
                        return;
                    }

                    var location = GetLocation(waypoint);
                    AssignCommand(player, () => ActionJumpToLocation(location));
                });
        }
    }
}
