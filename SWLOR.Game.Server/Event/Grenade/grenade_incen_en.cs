using NWN;
using static SWLOR.Game.Server.NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class grenade_incen_en
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            //MessageHub.Instance.Publish(new OnItemUsed());
            SWLOR.Game.Server.Item.Grenade.grenadeAoe(GetEnteringObject(), "INCENDIARY");
        }
    }
}