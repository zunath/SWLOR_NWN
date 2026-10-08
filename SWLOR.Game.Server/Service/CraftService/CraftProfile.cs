using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService
{
    public enum CraftProfile { Legacy, Sturdy, Delicate, Calibrated }
    [Flags]
    public enum CraftTechnique { None = 0, PoisonMixing = 1, TrapAssembly = 2 }
    public enum CraftCondition { Normal, Workable, Fine, Economical, Reinforced }
    public enum CraftWorkKind { None, Synthesis, Touch }
    public enum CraftBuffType { SteadyHand, MuscleMemory, Veneration, WasteNot, TouchQuality, BasicTouchDiscount, RapidDurabilityDiscount }
    public sealed record CraftBuff(int Charges, int ActionsRemaining, int Magnitude);

    public static class CraftConditionDeck
    {
        // A committed sequence is long enough for any affordable craft. No action rerolls it.
        public static IReadOnlyList<CraftCondition> Create(Func<int, int> randomIndex, int decks = 32)
        {
            if (randomIndex == null) throw new ArgumentNullException(nameof(randomIndex));
            if (decks < 1) throw new ArgumentOutOfRangeException(nameof(decks));
            var result = new List<CraftCondition> { CraftCondition.Normal };
            var normals = 1;
            for (var deck = 0; deck < decks; deck++)
            {
                var remaining = new List<CraftCondition> { CraftCondition.Normal, CraftCondition.Normal,
                    CraftCondition.Normal, CraftCondition.Normal, CraftCondition.Workable, CraftCondition.Workable,
                    CraftCondition.Fine, CraftCondition.Fine, CraftCondition.Economical, CraftCondition.Reinforced };
                while (remaining.Count > 0)
                {
                    // Keep enough opportunity cards to separate all remaining Normal cards.
                    var eligible = remaining.Select((card, index) => (card, index)).Where(candidate =>
                    {
                        var nextRun = candidate.card == CraftCondition.Normal ? normals + 1 : 0;
                        var leftNormal = remaining.Count(card => card == CraftCondition.Normal) - (candidate.card == CraftCondition.Normal ? 1 : 0);
                        var leftOther = remaining.Count - 1 - leftNormal;
                        return nextRun <= 3 && leftNormal <= 3 - nextRun + leftOther * 3;
                    }).ToArray();
                    var pick = randomIndex(eligible.Length);
                    if (pick < 0 || pick >= eligible.Length) return KnownValid(decks);
                    var chosen = eligible[pick];
                    result.Add(chosen.card);
                    normals = chosen.card == CraftCondition.Normal ? normals + 1 : 0;
                    remaining.RemoveAt(chosen.index);
                }
            }
            if (!IsValid(result)) return KnownValid(decks);
            return result.AsReadOnly();
        }

        private static IReadOnlyList<CraftCondition> KnownValid(int decks)
        {
            var block = new[] { CraftCondition.Workable, CraftCondition.Normal, CraftCondition.Fine, CraftCondition.Normal,
                CraftCondition.Economical, CraftCondition.Normal, CraftCondition.Workable, CraftCondition.Fine,
                CraftCondition.Normal, CraftCondition.Reinforced };
            return Array.AsReadOnly(new[] { CraftCondition.Normal }.Concat(Enumerable.Range(0, decks).SelectMany(_ => block)).ToArray());
        }

        public static bool IsValid(IReadOnlyList<CraftCondition> cards)
        {
            if (cards == null || cards.Count == 0 || cards[0] != CraftCondition.Normal || (cards.Count - 1) % 10 != 0) return false;
            var run = 0;
            foreach (var card in cards)
            {
                if (!Enum.IsDefined(typeof(CraftCondition), card)) return false;
                run = card == CraftCondition.Normal ? run + 1 : 0;
                if (run > 3) return false;
            }
            for (var offset = 1; offset < cards.Count; offset += 10)
            {
                var block = cards.Skip(offset).Take(10).ToArray();
                if (block.Count(card => card == CraftCondition.Normal) != 4 ||
                    block.Count(card => card == CraftCondition.Workable) != 2 ||
                    block.Count(card => card == CraftCondition.Fine) != 2 ||
                    block.Count(card => card == CraftCondition.Economical) != 1 ||
                    block.Count(card => card == CraftCondition.Reinforced) != 1) return false;
            }
            return true;
        }
    }
}
