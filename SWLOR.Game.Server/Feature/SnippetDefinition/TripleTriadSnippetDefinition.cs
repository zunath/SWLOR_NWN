using System.Collections.Generic;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public static class TripleTriadSnippetDefinition
    {
        [Snippet("action-card-game")]
        public static void StartCardGame(uint player, string[] args)
        {
            var npc = OBJECT_SELF;
            var levels = new List<int>();

            foreach (var arg in args)
            {
                if (int.TryParse(arg, out var level))
                {
                    if (level >= 1 && level <= 10)
                    {
                        levels.Add(level);
                    }
                }
            }

            if (levels.Count <= 0)
            {
                Log.Write(LogGroup.Error, $"{GetName(player)} tried to start card game with {GetName(OBJECT_SELF)} but no levels were defined.", true);
                return;
            }

            // Build a random NPC deck out of the levels assigned.
            var firstLevel = levels[0];
            levels.RemoveAt(0);
            var npcDeck = TripleTriad.BuildRandomDeck(firstLevel, levels.ToArray());
            SetLocalInt(npc, "NPC_DECK_CARD_1", (int)npcDeck.Card1);
            SetLocalInt(npc, "NPC_DECK_CARD_2", (int)npcDeck.Card2);
            SetLocalInt(npc, "NPC_DECK_CARD_3", (int)npcDeck.Card3);
            SetLocalInt(npc, "NPC_DECK_CARD_4", (int)npcDeck.Card4);
            SetLocalInt(npc, "NPC_DECK_CARD_5", (int)npcDeck.Card5);

            DelayCommand(0.1f, () =>
            {
                Dialog.StartConversation(player, npc, nameof(TripleTriadVersusDialog));
            });
        }
    }
}
