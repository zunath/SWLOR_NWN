using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class warp_to_wp
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            App.RunEvent<WarpToWaypoint>();
        }
    }
}
