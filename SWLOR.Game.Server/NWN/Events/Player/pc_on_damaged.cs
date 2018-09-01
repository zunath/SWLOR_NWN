using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Player;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class pc_on_damaged
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            App.RunEvent<OnPlayerDamaged>();
        }
    }
}