using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>
    /// The arithmetic of giving a player their waiting Search finds, apart from the engine: which quantities to try, and which of them
    /// are delivered and which must stay pending when the inventory is full.
    /// </summary>
    public static class ScavengeReturn
    {
        /// <summary>Adds up the finds of several lists by resref. Empty lists and non-positive quantities add nothing.</summary>
        public static Dictionary<string, int> Merge(params IReadOnlyDictionary<string, int>[] sources)
        {
            var merged = new Dictionary<string, int>();
            foreach (var source in sources)
            {
                if (source == null)
                    continue;

                foreach (var (resref, quantity) in source)
                {
                    if (quantity <= 0)
                        continue;

                    merged[resref] = merged.GetValueOrDefault(resref) + quantity;
                }
            }

            return merged;
        }

        /// <summary>
        /// Tries to deliver every wanted quantity and splits the wanted finds into what was delivered and what was not.
        /// <paramref name="deliver"/> gets a resref and the quantity wanted and returns how many were actually put in the inventory; a
        /// result below zero counts as none, and one above the wanted quantity counts as all of it. Delivered plus remainder is always
        /// exactly the wanted finds, so nothing is lost or made.
        /// </summary>
        public static ScavengeReturnSplit Divide(IReadOnlyDictionary<string, int> wanted, Func<string, int, int> deliver)
        {
            if (wanted == null)
                throw new ArgumentNullException(nameof(wanted));
            if (deliver == null)
                throw new ArgumentNullException(nameof(deliver));

            var delivered = new Dictionary<string, int>();
            var remainder = new Dictionary<string, int>();
            foreach (var (resref, quantity) in wanted.Where(find => find.Value > 0).ToList())
            {
                var given = Math.Clamp(deliver(resref, quantity), 0, quantity);
                if (given > 0)
                    delivered[resref] = given;
                if (given < quantity)
                    remainder[resref] = quantity - given;
            }

            return new ScavengeReturnSplit(delivered, remainder);
        }

        /// <summary>Lists quantities for a message, such as <c>3x Ruined Fiberp, 1x Ruby</c>, using the given names where there are any.</summary>
        public static string Describe(IReadOnlyDictionary<string, int> finds, IReadOnlyDictionary<string, string> names) =>
            string.Join(", ", finds.Select(find => $"{find.Value}x {(names != null && names.TryGetValue(find.Key, out var name) ? name : find.Key)}"));
    }
}
