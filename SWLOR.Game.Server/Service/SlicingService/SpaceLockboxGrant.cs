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
    /// then kept under this id so keeping them again after a restart changes nothing, and the mark is retired.
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
