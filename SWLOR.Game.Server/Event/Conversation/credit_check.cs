
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class credit_check
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            using (new Profiler(nameof(credit_check)))
            {
                NWPlayer oPC = SWLOR.Game.Server.NWScript._.GetPCSpeaker();
                NWObject oNPC = NWGameObject.OBJECT_SELF;
                int nGold = SWLOR.Game.Server.NWScript._.GetGold(oPC);
                int reqGold = SWLOR.Game.Server.NWScript._.GetLocalInt(oNPC, "gold");
                if (nGold > reqGold)
                {
                    return 1;
                }

                return 0;
            }
        }
    }
}
