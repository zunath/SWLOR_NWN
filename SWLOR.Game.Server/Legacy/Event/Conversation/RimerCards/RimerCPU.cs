using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.Conversation.RimerCards
{
    internal static class RimerCPU
    {
        internal static void ConfigureGameSettings(NWObject npc, RimerDeckType deck, RimerAIDifficulty difficulty)
        {
            npc.SetLocalInt("CARD_AI_DIFFICULTY", (int)difficulty);
            npc.SetLocalInt("CARD_DECK_TYPE", (int)deck);
        }

    }
}
