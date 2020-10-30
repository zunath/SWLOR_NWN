using SWLOR.Game.Server.Core.NWScript;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using SWLOR.Game.Server.GameObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class grenade_bbomb_hb
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            NWObject oTarget;
            oTarget = GetFirstInPersistentObject(NWScript.OBJECT_SELF);
            while (GetIsObjectValid(oTarget) == true)
            {
                SWLOR.Game.Server.Item.Grenade.grenadeAoe(oTarget, "BACTABOMB");
                //Get the next target in the AOE
                oTarget = GetNextInPersistentObject(NWScript.OBJECT_SELF);
            }
        }
    }
}