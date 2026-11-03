using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public static class PazaakCards
    {
        public const int SideDeckSize = 10;
        public const int HandSize = 4;

        /// <summary>
        /// Tag of every Pazaak card item. The card type is stored in <see cref="CardTypeVariable"/>.
        /// </summary>
        public const string CardItemTag = "PAZAAK_CARD";
        public const string CardTypeVariable = "PAZAAK_CARD_TYPE";

        private static readonly Dictionary<PazaakCardType, PazaakCardDetail> _cards = BuildCards()
            .ToDictionary(x => x.Type);

        /// <summary>
        /// Every side card, in display order.
        /// </summary>
        public static IReadOnlyList<PazaakCardDetail> All { get; } = _cards.Values.ToList();

        /// <summary>
        /// The deck every character can play without collecting cards. Each card in it is one free copy.
        /// </summary>
        public static IReadOnlyList<PazaakCardType> DefaultSideDeck { get; } = new[]
        {
            PazaakCardType.Plus1, PazaakCardType.Plus2, PazaakCardType.Plus3, PazaakCardType.Plus4, PazaakCardType.Plus5,
            PazaakCardType.Minus1, PazaakCardType.Minus2, PazaakCardType.Minus3, PazaakCardType.Minus4, PazaakCardType.Minus5,
        };

        public static PazaakCardDetail Get(PazaakCardType type) => _cards[type];

        /// <summary>
        /// Image of an empty table, hand, or side deck slot in the Pazaak window.
        /// </summary>
        public const string EmptySlotImage = "pzk_slot";

        /// <summary>
        /// Image of a side card: the card item's inventory icon.
        /// </summary>
        public static string GetImage(PazaakCardType type) => $"iit_card_{Get(type).Model:000}";

        /// <summary>
        /// Image of a main deck card worth 1 to 10.
        /// </summary>
        public static string GetMainCardImage(int value) => $"pzk_main{value}";

        /// <summary>
        /// Image of a plus or minus card with this value, from 1 to 6 either way.
        /// </summary>
        public static string GetSignedCardImage(int value) =>
            GetImage(value < 0 ? PazaakCardType.Minus1 + (-value - 1) : PazaakCardType.Plus1 + (value - 1));

        public static bool IsValid(PazaakCardType type) => _cards.ContainsKey(type);

        public static int GetFreeCopies(PazaakCardType type) => DefaultSideDeck.Count(x => x == type);

        /// <summary>
        /// Copies of a card a player may put in a side deck: the free copies plus the collected ones.
        /// </summary>
        public static int GetOwnedCopies(PazaakCardType type, IReadOnlyDictionary<PazaakCardType, int> collection)
        {
            var collected = collection != null && collection.TryGetValue(type, out var count) ? count : 0;
            return GetFreeCopies(type) + Math.Max(0, collected);
        }

        /// <summary>
        /// A side deck is valid when it has exactly ten cards and no card appears more often than the player owns it.
        /// </summary>
        public static bool IsValidSideDeck(
            IReadOnlyCollection<PazaakCardType> deck,
            IReadOnlyDictionary<PazaakCardType, int> collection)
        {
            if (deck == null || deck.Count != SideDeckSize || !deck.All(IsValid))
                return false;

            return deck.GroupBy(x => x).All(group => group.Count() <= GetOwnedCopies(group.Key, collection));
        }

        private static IEnumerable<PazaakCardDetail> BuildCards()
        {
            for (var value = 1; value <= 6; value++)
                yield return new PazaakCardDetail(PazaakCardType.Plus1 + value - 1, PazaakCardKind.Plus, value,
                    $"+{value}", $"+{value}", $"Adds {value} to your total.",
                    $"pzk_plus{value}", 97 + value);

            for (var value = 1; value <= 6; value++)
                yield return new PazaakCardDetail(PazaakCardType.Minus1 + value - 1, PazaakCardKind.Minus, value,
                    $"-{value}", $"-{value}", $"Subtracts {value} from your total.",
                    $"pzk_minus{value}", 103 + value);

            for (var value = 1; value <= 6; value++)
                yield return new PazaakCardDetail(PazaakCardType.PlusMinus1 + value - 1, PazaakCardKind.PlusMinus, value,
                    $"+/-{value}", $"+/-{value}", $"Adds or subtracts {value}. Pick the sign before you play it.",
                    $"pzk_pm{value}", 109 + value);

            yield return new PazaakCardDetail(PazaakCardType.FlipTwoFour, PazaakCardKind.FlipTwoFour, 0,
                "2&4", "Flip 2&4", "Flips the sign of every 2 and 4 on your table.",
                "pzk_flip24", 116);
            yield return new PazaakCardDetail(PazaakCardType.FlipThreeSix, PazaakCardKind.FlipThreeSix, 0,
                "3&6", "Flip 3&6", "Flips the sign of every 3 and 6 on your table.",
                "pzk_flip36", 117);
            yield return new PazaakCardDetail(PazaakCardType.Double, PazaakCardKind.Double, 0,
                "D", "Double", "Doubles the value of the last card on your table.",
                "pzk_double", 118);
            yield return new PazaakCardDetail(PazaakCardType.Tiebreaker, PazaakCardKind.Tiebreaker, 1,
                "+/-1T", "Tiebreaker", "Adds or subtracts 1. If the set ends in a tie, you win it.",
                "pzk_tiebreak", 119);
        }
    }
}
