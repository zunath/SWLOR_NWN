using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Event.Conversation.RimerCards
{
    internal class RimerCardsCPU4
    {
        public static int Main()
        {
            RimerDeckType deck = RandomService.Random(4) <= 3 ? RimerDeckType.FastCreatures : RimerDeckType.BigCreatures;
            RimerCPU.ConfigureGameSettings(NWGameObject.OBJECT_SELF, deck, RimerAIDifficulty.Normal);
            return 0;
        }
    }
}
