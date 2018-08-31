using SWLOR.Game.Server.Event.Player;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.NWN.Events.Player
{
#pragma warning disable IDE1006 // Naming Styles
    internal class @default
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            App.RunEvent<OnPlayerHeartbeat>();
        }
    }
}