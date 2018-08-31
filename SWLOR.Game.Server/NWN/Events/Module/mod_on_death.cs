using SWLOR.Game.Server.Event.Module;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.NWN.Events.Module
{
#pragma warning disable IDE1006 // Naming Styles
    internal class mod_on_death
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            App.RunEvent<OnModuleDeath>();
        }
    }
}
