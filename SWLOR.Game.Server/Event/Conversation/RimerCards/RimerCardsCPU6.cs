using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.RimerCards
{
    internal class RimerCardsCPU6
    {
        public static int Main()
        {
            RimerDeckType deck = RandomService.Random(4) <= 3 ? RimerDeckType.Spells : RimerDeckType.Angels;
            RimerCPU.ConfigureGameSettings(NWGameObject.OBJECT_SELF, deck, RimerAIDifficulty.Normal);
            return 0;
        }
    }
}
