using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;

namespace SWLOR.Game.Server.Event.Conversation
{
    internal class CheckCredits
    {
        public static int Main()
        {
            NWPlayer oPC = _.GetPCSpeaker();
            NWObject oNPC = NWGameObject.OBJECT_SELF;
            int nGold = _.GetGold(oPC);
            int reqGold = _.GetLocalInt(oNPC, "gold");
            if (nGold > reqGold)
            {
                return 1;
            }

            return 0;
        }
    }
}
