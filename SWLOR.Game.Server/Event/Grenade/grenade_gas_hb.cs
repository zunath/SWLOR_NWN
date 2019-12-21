using NWN;
using static NWN._;
using SWLOR.Game.Server.GameObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class grenade_gas_hb
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public void Main()
        {
            NWObject oTarget;
            oTarget = GetFirstInPersistentObject(NWGameObject.OBJECT_SELF);
            while (GetIsObjectValid(oTarget) == true)
            {
                SWLOR.Game.Server.Item.Grenade.grenadeAoe(oTarget, "GAS");
                //Get the next target in the AOE
                oTarget = GetNextInPersistentObject(NWGameObject.OBJECT_SELF);
            }
        }
    }
}