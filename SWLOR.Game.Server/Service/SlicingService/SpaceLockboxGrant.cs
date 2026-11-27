using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>
    /// The rewards of a sliced space lockbox that the database keeps (agent intel, and items a full inventory refused,
    /// packed into the hold). They are saved in the character file as a mark once the file no longer holds the lockbox,
    /// then kept under this id so keeping them again after a restart changes nothing, and the mark is retired. The mark
    /// holds every grant still waiting for a save (<see cref="Append"/>), so opening another lockbox first loses none.
    /// </summary>
    public sealed class SpaceLockboxGrant
    {
        public string Id { get; init; }
        public int Intel { get; init; }
        public List<(string Resref, ShipComponentTraitType? Trait)> Unplaced { get; init; } = new();

        public bool IsEmpty => Intel <= 0 && Unplaced.Count == 0;

        public string Serialize() =>
            string.Join("|", Id, Intel.ToString(CultureInfo.InvariantCulture),
                string.Join(";", Unplaced.Select(x => x.Trait == null
                    ? x.Resref
                    : $"{x.Resref}~{((int)x.Trait.Value).ToString(CultureInfo.InvariantCulture)}")));

        /// <summary>Separates the grants in a mark; a grant never holds it.</summary>
        private const char GrantSeparator = '#';

        /// <summary>The mark with <paramref name="grant"/> added after the grants it already holds.</summary>
        public static string Append(string mark, SpaceLockboxGrant grant) =>
            string.IsNullOrEmpty(mark) ? grant.Serialize() : mark + GrantSeparator + grant.Serialize();

        /// <summary>The grants a mark holds, in the order they were added; an entry that does not parse is left out.</summary>
        public static List<SpaceLockboxGrant> ParseAll(string mark)
        {
            var grants = new List<SpaceLockboxGrant>();
            foreach (var entry in (mark ?? string.Empty).Split(GrantSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryParse(entry, out var grant))
                    grants.Add(grant);
            }

            return grants;
        }

        public static bool TryParse(string value, out SpaceLockboxGrant grant)
        {
            grant = null;
            var parts = (value ?? string.Empty).Split('|');
            if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[0]) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var intel) || intel < 0)
            {
                return false;
            }

            var unplaced = new List<(string Resref, ShipComponentTraitType? Trait)>();
            foreach (var entry in parts[2].Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var item = entry.Split('~');
                if (string.IsNullOrWhiteSpace(item[0]) || item.Length > 2)
                    return false;

                if (item.Length == 1)
                {
                    unplaced.Add((item[0], null));
                    continue;
                }

                if (!int.TryParse(item[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var trait))
                    return false;

                unplaced.Add((item[0], (ShipComponentTraitType)trait));
            }

            grant = new SpaceLockboxGrant { Id = parts[0], Intel = intel, Unplaced = unplaced };
            return true;
        }
    }
}
