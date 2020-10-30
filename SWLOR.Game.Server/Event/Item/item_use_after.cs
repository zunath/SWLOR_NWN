using SWLOR.Game.Server.Core.NWNX;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class item_use_after
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            // Already handled in the item_use_before script. No need for anything else to run at this point.
            Events.SkipEvent();
        }
    }
}