using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// Case-insensitive text matching shared by the DM chooser, creator, and player search.
    /// </summary>
    public static class DMSearchText
    {
        private static readonly char[] Separators = { ' ', '\t' };

        /// <summary>
        /// Splits a search into the words that must each be found.
        /// </summary>
        /// <param name="search">The text the DM typed.</param>
        public static IReadOnlyList<string> GetTerms(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return new List<string>();

            return search
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(term => term.Trim())
                .Where(term => term.Length > 0)
                .ToList();
        }

        /// <summary>
        /// Returns true when every term appears in at least one of the fields. No terms matches everything.
        /// </summary>
        /// <param name="terms">The search terms from <see cref="GetTerms"/>.</param>
        /// <param name="fields">The values to search, such as name, tag, and resref.</param>
        public static bool Matches(IReadOnlyList<string> terms, params string[] fields)
        {
            foreach (var term in terms)
            {
                var found = false;
                foreach (var field in fields)
                {
                    if (!string.IsNullOrEmpty(field) &&
                        field.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }
    }
}
