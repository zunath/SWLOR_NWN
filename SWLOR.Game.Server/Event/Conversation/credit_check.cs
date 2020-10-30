
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class credit_check
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            using (new Profiler(nameof(credit_check)))
            {
                NWPlayer oPC = NWScript.GetPCSpeaker();
                NWObject oNPC = NWScript.OBJECT_SELF;
                var nGold = NWScript.GetGold(oPC);
                var reqGold = NWScript.GetLocalInt(oNPC, "gold");
                if (nGold > reqGold)
                {
                    return 1;
                }

                return 0;
            }
        }
    }
}
