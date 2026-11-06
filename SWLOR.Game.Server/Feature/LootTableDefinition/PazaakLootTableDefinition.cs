using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    /// <summary>
    /// Pazaak side card items drop only from these tables. The creatures that carry the song disc
    /// tables carry these beside them, at the same chances.
    /// </summary>
    public class PazaakLootTableDefinition : ILootTableDefinition
    {
        public const string PazaakCards = "PAZAAK_CARDS";
        public const string BossPazaakCards = "PAZAAK_CARDS_BOSS";

        // Creatures roll these tables at 1% (regular) and 5% (boss). A full rare find bonus
        // (Treasure Hunter III) raises either to at most 20%.
        public const int MaxDropChance = 20;
        public const int PazaakCardChance = 1;
        public const int BossPazaakCardChance = 5;

        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            CardTable(PazaakCards, PazaakCardChance);
            CardTable(BossPazaakCards, BossPazaakCardChance);

            return _builder.Build();
        }

        // How often each card is picked once its table drops: plus and minus cards are the most
        // common, plus/minus cards less so, special cards rare, and the Tiebreaker rarest.
        public const int CommonWeight = 6;
        public const int UncommonWeight = 3;
        public const int RareWeight = 2;
        public const int TiebreakerWeight = 1;

        private void CardTable(string name, int chance)
        {
            _builder.Create(name)
                .IsRare()
                .MaxRareChanceBonus(MaxDropChance - chance)
                .AddItem("pzk_plus1", CommonWeight)
                .AddItem("pzk_plus2", CommonWeight)
                .AddItem("pzk_plus3", CommonWeight)
                .AddItem("pzk_plus4", CommonWeight)
                .AddItem("pzk_plus5", CommonWeight)
                .AddItem("pzk_plus6", CommonWeight)
                .AddItem("pzk_minus1", CommonWeight)
                .AddItem("pzk_minus2", CommonWeight)
                .AddItem("pzk_minus3", CommonWeight)
                .AddItem("pzk_minus4", CommonWeight)
                .AddItem("pzk_minus5", CommonWeight)
                .AddItem("pzk_minus6", CommonWeight)
                .AddItem("pzk_pm1", UncommonWeight)
                .AddItem("pzk_pm2", UncommonWeight)
                .AddItem("pzk_pm3", UncommonWeight)
                .AddItem("pzk_pm4", UncommonWeight)
                .AddItem("pzk_pm5", UncommonWeight)
                .AddItem("pzk_pm6", UncommonWeight)
                .AddItem("pzk_flip24", RareWeight)
                .AddItem("pzk_flip36", RareWeight)
                .AddItem("pzk_double", RareWeight)
                .AddItem("pzk_tiebreak", TiebreakerWeight);
        }
    }
}
