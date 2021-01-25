using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class TransportationSnippetDefinition
    {
        /// <summary>
        /// Snippet which teleports a player to the waypoint with the specified tag.
        /// </summary>
        /// <param name="player">The player to teleport</param>
        /// <param name="args">Arguments provided by the conversation builder.</param>
        [Snippet("action-teleport")]
        public static void ActionTeleport(uint player, string[] args)
        {
            if (args.Length <= 0)
            {
                const string Error = "'action-teleport' requires a waypoint tag argument.";
                SendMessageToPC(player, Error);
                Log.Write(LogGroup.Error, Error);
                return;
            }

            var waypointTag = args[0];
            var waypoint = GetWaypointByTag(waypointTag);

            if (!GetIsObjectValid(waypoint))
            {
                var error = $"Could not locate waypoint with tag '{waypointTag}' for snippet 'action-teleport'";
                SendMessageToPC(player, error);
                Log.Write(LogGroup.Error, error);
                return;
            }

            var location = GetLocation(waypoint);
            AssignCommand(player, () => ActionJumpToLocation(location));
        }
    }
}
