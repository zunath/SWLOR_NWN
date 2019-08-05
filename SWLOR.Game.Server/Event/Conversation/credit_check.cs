
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class credit_check
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            using (new Profiler(nameof(credit_check)))
            {
                NWPlayer oPC = _.GetPCSpeaker();
                NWObject oNPC = NWGameObject.OBJECT_SELF;
                int nGold = _.GetGold(oPC);
                int reqGold = _.GetLocalInt(oNPC, "gold");
                if (nGold > reqGold)
                {
                    return _.TRUE;
                }

                return _.FALSE;
            }
        }
    }
}
