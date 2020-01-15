using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.RimerCards
{
    internal class RimerCardsCPU9
    {
        public static int Main()
        {
            RimerCPU.ConfigureGameSettings(NWGameObject.OBJECT_SELF, RimerDeckType.Random, RimerAIDifficulty.Hard);
            return 0;
        }
    }
}
