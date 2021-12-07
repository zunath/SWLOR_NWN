using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using System;

namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class item_inventory_disturb_after
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            MessageHub.Instance.Publish(new OnItemDisturbed());
        }
    }
}