using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.RimerCards
{
    internal class RimerCardsCPU7
    {
        public static int Main()
        {
            RimerDeckType deck = RandomService.Random(4) <= 3 ? RimerDeckType.Undead : RimerDeckType.Random;
            RimerCPU.ConfigureGameSettings(NWGameObject.OBJECT_SELF, deck, RimerAIDifficulty.Hard);
            return 0;
        }
    }
}
