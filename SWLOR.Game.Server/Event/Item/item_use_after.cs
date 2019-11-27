using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class item_use_after
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public void Main()
        {
            // Already handled in the item_use_before script. No need for anything else to run at this point.
            NWNXEvents.SkipEvent();
        }
    }
}