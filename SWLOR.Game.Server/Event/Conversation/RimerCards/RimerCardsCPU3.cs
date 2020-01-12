using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.RimerCards
{
    internal class RimerCardsCPU3
    {
        public static int Main()
        {
            using (new Profiler(nameof(RimerCardsCPU3)))
            {
                RimerDeckType deck = RandomService.Random(4) <= 3 ? RimerDeckType.Goblins : RimerDeckType.Random;
                RimerCPU.ConfigureGameSettings(NWGameObject.OBJECT_SELF, deck, RimerAIDifficulty.Easy);
                return 0;
            }
        }
    }
}
