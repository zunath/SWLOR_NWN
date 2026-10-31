using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public static class PazaakCards
    {
        public const int SideDeckSize = 10;
        public const int HandSize = 4;

        private static readonly Dictionary<PazaakCardType, PazaakCardDetail> _cards = BuildCards()
            .ToDictionary(x => x.Type);

        /// <summary>
        /// Every side card a player may put in a side deck, in display order.
        /// </summary>
        public static IReadOnlyList<PazaakCardDetail> All { get; } = _cards.Values.ToList();

        /// <summary>
        /// The deck a character starts with until they build their own.
        /// </summary>
        public static IReadOnlyList<PazaakCardType> DefaultSideDeck { get; } = new[]
        {
            PazaakCardType.Plus1, PazaakCardType.Plus2, PazaakCardType.Plus3, PazaakCardType.Plus4, PazaakCardType.Plus5,
            PazaakCardType.Minus1, PazaakCardType.Minus2, PazaakCardType.Minus3, PazaakCardType.Minus4, PazaakCardType.Minus5,
        };

        public static PazaakCardDetail Get(PazaakCardType type) => _cards[type];

        public static bool IsValid(PazaakCardType type) => _cards.ContainsKey(type);

        public static bool IsValidSideDeck(IReadOnlyCollection<PazaakCardType> deck) =>
            deck != null && deck.Count == SideDeckSize && deck.All(IsValid);

        private static IEnumerable<PazaakCardDetail> BuildCards()
        {
            for (var value = 1; value <= 6; value++)
                yield return new PazaakCardDetail(PazaakCardType.Plus1 + value - 1, PazaakCardKind.Plus, value,
                    $"+{value}", $"Adds {value} to your total.");

            for (var value = 1; value <= 6; value++)
                yield return new PazaakCardDetail(PazaakCardType.Minus1 + value - 1, PazaakCardKind.Minus, value,
                    $"-{value}", $"Subtracts {value} from your total.");

            for (var value = 1; value <= 6; value++)
                yield return new PazaakCardDetail(PazaakCardType.PlusMinus1 + value - 1, PazaakCardKind.PlusMinus, value,
                    $"+/-{value}", $"Adds or subtracts {value}. Pick the sign before you play it.");

            yield return new PazaakCardDetail(PazaakCardType.FlipTwoFour, PazaakCardKind.FlipTwoFour, 0,
                "2&4", "Flips the sign of every 2 and 4 on your table.");
            yield return new PazaakCardDetail(PazaakCardType.FlipThreeSix, PazaakCardKind.FlipThreeSix, 0,
                "3&6", "Flips the sign of every 3 and 6 on your table.");
            yield return new PazaakCardDetail(PazaakCardType.Double, PazaakCardKind.Double, 0,
                "D", "Doubles the value of the last card on your table.");
            yield return new PazaakCardDetail(PazaakCardType.Tiebreaker, PazaakCardKind.Tiebreaker, 1,
                "+/-1T", "Adds or subtracts 1. If the set ends in a tie, you win it.");
        }
    }
}
