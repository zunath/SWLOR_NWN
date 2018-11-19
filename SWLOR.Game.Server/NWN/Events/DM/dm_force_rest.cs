using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Feat;
using SWLOR.Game.Server.Event.Module;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class dm_force_rest
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            App.RunEvent<OnDMAction>(15);
        }
    }
}