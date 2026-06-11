using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.PazaakService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class PazaakLootTableDefinition : ILootTableDefinition
    {
        private const string CardResref = "pazaak_card";
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            CommonCards();
            UncommonCards();
            RareCards();
            GoldCards();

            return _builder.Build();
        }

        private void CommonCards()
        {
            _builder.Create("PAZAAK_COMMON")
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Plus1))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Plus2))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Plus3))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Plus4))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Minus1))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Minus2))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Minus3))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Minus4));
        }

        private void UncommonCards()
        {
            _builder.Create("PAZAAK_UNCOMMON")
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Plus5))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Plus6))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Minus5))
                .AddItem(CardResref, 10).AddSpawnAction(item => StampCard(item, PazaakCardType.Minus6))
                .AddItem(CardResref, 7).AddSpawnAction(item => StampCard(item, PazaakCardType.PlusMinus1))
                .AddItem(CardResref, 7).AddSpawnAction(item => StampCard(item, PazaakCardType.PlusMinus2))
                .AddItem(CardResref, 7).AddSpawnAction(item => StampCard(item, PazaakCardType.PlusMinus3));
        }

        private void RareCards()
        {
            _builder.Create("PAZAAK_RARE")
                .IsRare()
                .AddItem(CardResref, 7, 1, true).AddSpawnAction(item => StampCard(item, PazaakCardType.PlusMinus4))
                .AddItem(CardResref, 7, 1, true).AddSpawnAction(item => StampCard(item, PazaakCardType.PlusMinus5))
                .AddItem(CardResref, 7, 1, true).AddSpawnAction(item => StampCard(item, PazaakCardType.PlusMinus6));
        }

        private void GoldCards()
        {
            _builder.Create("PAZAAK_GOLD")
                .IsRare()
                .AddItem(CardResref, 4, 1, true).AddSpawnAction(item => StampCard(item, PazaakCardType.OneOrMinusTwo))
                .AddItem(CardResref, 3, 1, true).AddSpawnAction(item => StampCard(item, PazaakCardType.Double))
                .AddItem(CardResref, 3, 1, true).AddSpawnAction(item => StampCard(item, PazaakCardType.TieBreaker))
                .AddItem(CardResref, 3, 1, true).AddSpawnAction(item => StampCard(item, PazaakCardType.Flip2And4))
                .AddItem(CardResref, 3, 1, true).AddSpawnAction(item => StampCard(item, PazaakCardType.Flip3And6));
        }

        private static void StampCard(uint item, PazaakCardType cardType)
        {
            SetLocalInt(item, Pazaak.CardItemLocalVariable, (int)cardType);
            SetName(item, $"Pazaak Card: {PazaakCardCatalog.GetName(cardType)}");
        }
    }
}
