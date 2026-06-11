using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public static class PazaakCardCatalog
    {
        private static readonly Dictionary<PazaakCardType, PazaakCardDefinition> _cards = BuildCards();

        public static IReadOnlyDictionary<PazaakCardType, PazaakCardDefinition> All => _cards;

        public static IReadOnlyList<PazaakCardType> StarterDeck { get; } = new List<PazaakCardType>
        {
            PazaakCardType.Plus1,
            PazaakCardType.Plus2,
            PazaakCardType.Plus3,
            PazaakCardType.Plus4,
            PazaakCardType.Plus5,
            PazaakCardType.Plus6,
            PazaakCardType.Minus1,
            PazaakCardType.Minus2,
            PazaakCardType.Minus3,
            PazaakCardType.Minus4,
        };

        public static bool IsValidCard(PazaakCardType type)
        {
            return _cards.ContainsKey(type);
        }

        public static PazaakCardDefinition Get(PazaakCardType type)
        {
            return _cards[type];
        }

        public static PazaakCardDefinition Get(int typeId)
        {
            return Get((PazaakCardType)typeId);
        }

        public static IEnumerable<PazaakCardDefinition> GetAllCards()
        {
            return _cards.Values.OrderBy(x => x.Type);
        }

        public static string GetName(PazaakCardType type)
        {
            return IsValidCard(type) ? Get(type).Name : "Unknown Card";
        }

        public static string GetShortName(PazaakCardType type)
        {
            return IsValidCard(type) ? Get(type).ShortName : "?";
        }

        private static Dictionary<PazaakCardType, PazaakCardDefinition> BuildCards()
        {
            var cards = new Dictionary<PazaakCardType, PazaakCardDefinition>();

            void Add(
                PazaakCardType type,
                string name,
                string shortName,
                PazaakCardRule rule,
                IEnumerable<int> values,
                IEnumerable<int> flipValues,
                bool isGold,
                string rarity,
                int price,
                string icon)
            {
                cards[type] = new PazaakCardDefinition(
                    type,
                    name,
                    shortName,
                    rule,
                    values,
                    flipValues,
                    isGold,
                    rarity,
                    price,
                    icon);
            }

            for (var value = 1; value <= 6; value++)
            {
                Add((PazaakCardType)value, $"+{value}", $"+{value}", PazaakCardRule.FixedValue, new[] { value }, new int[0], false, value <= 4 ? "Common" : "Uncommon", 125 * value, $"pz_p{value}");
                Add((PazaakCardType)(10 + value), $"-{value}", $"-{value}", PazaakCardRule.FixedValue, new[] { -value }, new int[0], false, value <= 4 ? "Common" : "Uncommon", 125 * value, $"pz_m{value}");
                Add((PazaakCardType)(20 + value), $"+/-{value}", $"+/-{value}", PazaakCardRule.ChooseValue, new[] { value, -value }, new int[0], false, value <= 3 ? "Uncommon" : "Rare", 350 * value, $"pz_pm{value}");
            }

            Add(PazaakCardType.OneOrMinusTwo, "1 +/- 2", "1+/-2", PazaakCardRule.ChooseValue, new[] { 1, 2, -1, -2 }, new int[0], true, "Gold", 2200, "pz_g12");
            Add(PazaakCardType.Double, "Double", "Double", PazaakCardRule.DoubleLastCard, new int[0], new int[0], true, "Gold", 3000, "pz_gdbl");
            Add(PazaakCardType.TieBreaker, "Tie Breaker", "Tie", PazaakCardRule.TieBreaker, new[] { 1, -1 }, new int[0], true, "Gold", 2600, "pz_gtie");
            Add(PazaakCardType.Flip2And4, "Flip 2 & 4", "Flip 2/4", PazaakCardRule.FlipValues, new[] { 0 }, new[] { 2, 4 }, true, "Gold", 2800, "pz_g24");
            Add(PazaakCardType.Flip3And6, "Flip 3 & 6", "Flip 3/6", PazaakCardRule.FlipValues, new[] { 0 }, new[] { 3, 6 }, true, "Gold", 3200, "pz_g36");

            return cards;
        }
    }
}
