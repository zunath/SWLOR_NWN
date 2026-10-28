using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The signal a Probe Launcher gathers and what the Scanner tab shows for it (plan section 24). Pure rules; numbers
    /// come from <see cref="ScanningTuning"/>.
    /// </summary>
    public static class ScanningRules
    {
        /// <summary>The signal a signature of this type and tier needs to resolve; a Wild Space one at tier 5.5.</summary>
        public static double SignalRequired(SignatureType type, int tier, bool wildSpace = false) =>
            ScanningTuning.SignalPerTier * WildSpaceRules.SignalTier(tier, wildSpace) *
            (ScanningTuning.Difficulty.TryGetValue(type, out var difficulty) ? difficulty : 1.0);

        /// <summary>A signature's tier as the Scanner tab shows it: 5.5 in Wild Space.</summary>
        public static string TierLabel(ScanSignature signature) =>
            WildSpaceRules.TierLabel(signature.Tier, signature.WildSpace);

        /// <summary>The Espionage bonus in percent for a rank: +1% per rank, up to +50% at rank 50.</summary>
        public static int EspionageBonusPercent(int espionageRank) =>
            Math.Clamp(espionageRank, 0, ScanningTuning.EspionageMaximumRank) * ScanningTuning.EspionagePercentPerRank;

        /// <summary>
        /// The hull's scan bonus in percent: the Explorer role, the Chiss manufacturer and the Chiss Hyperion's
        /// signature add together.
        /// </summary>
        public static int HullScanBonusPercent(ShipRole role, ShipManufacturer manufacturer, string hullTag)
        {
            var percent = 0;
            if (role == ShipRole.Explorer)
                percent += ScanningTuning.ExplorerScanPercent;
            if (manufacturer == ShipManufacturer.Chiss)
                percent += ScanningTuning.ChissScanPercent;
            if (hullTag == ScanningTuning.HyperionTag)
                percent += ScanningTuning.HyperionScanPercent;
            return percent;
        }

        /// <summary>Whether the hull shows a signature's tier at the type threshold (the Chiss Hyperion).</summary>
        public static bool ShowsTierEarly(string hullTag) => hullTag == ScanningTuning.HyperionTag;

        /// <summary>
        /// The signal one Probe Launcher cycle adds: the module's scan strength, raised by the operator's Espionage, the
        /// hull, the system's environment (the ship's Space Scan Strength stat) and a droid's Survey Protocol, then
        /// scaled by the station's effectiveness. Never negative.
        /// </summary>
        public static double SignalPerCycle(
            int scanStrength,
            int espionageRank,
            int hullBonusPercent,
            int environmentPercent,
            int surveyBonusPercent,
            double effectiveness)
        {
            var bonus = EspionageBonusPercent(espionageRank) + hullBonusPercent + environmentPercent + surveyBonusPercent;
            return Math.Max(0.0, scanStrength * (1.0 + bonus / 100.0) * Math.Max(0.0, effectiveness));
        }

        /// <summary>A scanner's strength on a signature in percent, 0-100.</summary>
        public static int Percent(ScanSignature signature, string playerId)
        {
            if (signature == null || string.IsNullOrEmpty(playerId) || !signature.Signal.TryGetValue(playerId, out var signal))
                return 0;

            return ToPercent(signal, signature.SignalRequired);
        }

        /// <summary>The best strength any member of a party holds on a signature, in percent.</summary>
        public static int PartyPercent(ScanSignature signature, IEnumerable<string> partyIds)
        {
            return partyIds == null ? 0 : partyIds.Select(id => Percent(signature, id)).DefaultIfEmpty(0).Max();
        }

        private static int ToPercent(double signal, double required)
        {
            if (required <= 0)
                return 100;

            return Math.Clamp((int)Math.Floor(signal / required * 100.0 + 1e-9), 0, 100);
        }

        /// <summary>Adds a cycle's signal for a scanner and returns its percent before and after.</summary>
        public static (int Before, int After) AddSignal(ScanSignature signature, string playerId, double signal)
        {
            var before = Percent(signature, playerId);
            if (signature == null || string.IsNullOrEmpty(playerId) || signal <= 0)
                return (before, before);

            signature.Signal.TryGetValue(playerId, out var current);
            signature.Signal[playerId] = Math.Min(signature.SignalRequired, current + signal);
            return (before, Percent(signature, playerId));
        }

        /// <summary>How much the Scanner tab shows at a strength.</summary>
        public static SignatureInfoLevel InfoLevel(int percent)
        {
            if (percent >= ScanningTuning.InfoLocatedPercent) return SignatureInfoLevel.Located;
            if (percent >= ScanningTuning.InfoThreatPercent) return SignatureInfoLevel.Threat;
            if (percent >= ScanningTuning.InfoTierPercent) return SignatureInfoLevel.Tier;
            if (percent >= ScanningTuning.InfoTypePercent) return SignatureInfoLevel.Type;
            return SignatureInfoLevel.Unknown;
        }

        /// <summary>The highest information level a cycle crossed into, or Unknown when it crossed none.</summary>
        public static SignatureInfoLevel CrossedLevel(int before, int after)
        {
            var from = InfoLevel(before);
            var to = InfoLevel(after);
            return to > from ? to : SignatureInfoLevel.Unknown;
        }

        /// <summary>The signature type's name.</summary>
        public static string TypeName(SignatureType type) =>
            ScanningTuning.Names.TryGetValue(type, out var name) ? name : "Signature";

        /// <summary>
        /// The area's sector a point lies in on a 3 x 3 grid, for the rough location ("north-west", "central", ...).
        /// North is high Y, as on the area map.
        /// </summary>
        public static string Sector(float x, float y, float areaWidth, float areaHeight)
        {
            var grid = ScanningTuning.SectorGrid;
            var column = areaWidth <= 0 ? 1 : Math.Clamp((int)(x / areaWidth * grid), 0, grid - 1);
            var row = areaHeight <= 0 ? 1 : Math.Clamp((int)(y / areaHeight * grid), 0, grid - 1);
            var northSouth = row == grid - 1 ? "north" : row == 0 ? "south" : string.Empty;
            var eastWest = column == grid - 1 ? "east" : column == 0 ? "west" : string.Empty;

            if (northSouth.Length > 0 && eastWest.Length > 0)
                return $"{northSouth}-{eastWest}";
            if (northSouth.Length > 0)
                return northSouth;
            if (eastWest.Length > 0)
                return eastWest;
            return "central";
        }

        /// <summary>The name column of a Scanner tab row.</summary>
        public static string RowName(ScanSignature signature, int percent, bool tierEarly)
        {
            var level = InfoLevel(percent);
            if (level == SignatureInfoLevel.Unknown)
                return "Unknown signal";

            var name = TypeName(signature.Type);
            return level >= SignatureInfoLevel.Tier || tierEarly ? $"{name} (Tier {TierLabel(signature)})" : name;
        }

        /// <summary>The detail column of a Scanner tab row: the rough location, then the threat, then the exact location.</summary>
        public static string RowDetail(ScanSignature signature, int percent, float areaWidth, float areaHeight)
        {
            var level = InfoLevel(percent);
            if (level < SignatureInfoLevel.Tier)
                return "Keep scanning";

            var location = Location(signature, level, areaWidth, areaHeight);
            return level >= SignatureInfoLevel.Threat && ScanningTuning.Threat.TryGetValue(signature.Type, out var threat)
                ? $"{location}, {threat}"
                : location;
        }

        /// <summary>The tooltip of a Scanner tab row: everything known, the loot class included.</summary>
        public static string RowTooltip(ScanSignature signature, int percent, float areaWidth, float areaHeight)
        {
            var level = InfoLevel(percent);
            if (level < SignatureInfoLevel.Tier)
                return level == SignatureInfoLevel.Unknown
                    ? $"Scan to {ScanningTuning.InfoTypePercent}% to learn what it is."
                    : $"Scan to {ScanningTuning.InfoTierPercent}% for its tier and rough location.";

            var parts = new List<string> { Location(signature, level, areaWidth, areaHeight) };
            if (level >= SignatureInfoLevel.Threat)
            {
                if (ScanningTuning.Threat.TryGetValue(signature.Type, out var threat))
                    parts.Add($"Threat: {threat}");
                if (ScanningTuning.LootClass.TryGetValue(signature.Type, out var loot))
                    parts.Add(signature.Locked ? $"Loot: {loot} (locked: needs a Data Analyzer)" : $"Loot: {loot}");
                if (!string.IsNullOrEmpty(signature.RiftDetail))
                    parts.Add(signature.RiftDetail);
            }
            else
            {
                parts.Add($"Scan to {ScanningTuning.InfoThreatPercent}% for its threat and loot.");
            }

            if (level < SignatureInfoLevel.Located)
                parts.Add($"Scan to {ScanningTuning.InfoLocatedPercent}% to jump to it.");

            return string.Join(". ", parts) + ".";
        }

        private static string Location(ScanSignature signature, SignatureInfoLevel level, float areaWidth, float areaHeight) =>
            level == SignatureInfoLevel.Located
                ? $"At {signature.X:0}, {signature.Y:0}"
                : $"{Capitalize(Sector(signature.X, signature.Y, areaWidth, areaHeight))} sector";

        private static string Capitalize(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];

        /// <summary>The message a scanner gets when a cycle crosses a threshold, or null when it crossed none.</summary>
        public static string ThresholdMessage(ScanSignature signature, SignatureInfoLevel crossed, bool tierEarly)
        {
            var name = TypeName(signature.Type);
            return crossed switch
            {
                SignatureInfoLevel.Type => tierEarly ? $"Signature resolved: {name} (Tier {TierLabel(signature)})." : $"Signature resolved: {name}.",
                SignatureInfoLevel.Tier => $"Signature resolved: {name} (Tier {TierLabel(signature)}).",
                SignatureInfoLevel.Threat => $"Signature resolved: {name} (Tier {TierLabel(signature)}), threat: " +
                                             $"{(ScanningTuning.Threat.TryGetValue(signature.Type, out var t) ? t : "unknown")}.",
                SignatureInfoLevel.Located => $"Signature located: {name} (Tier {TierLabel(signature)}). Jump to it from the Scanner tab of the Ship HUD.",
                _ => null,
            };
        }

        /// <summary>
        /// Whether a party may jump to a signature: its site is open, not completed, and some member of the party holds
        /// 100% on it. A party that did not scan it must scan it itself.
        /// </summary>
        public static bool CanJump(ScanSignature signature, IEnumerable<string> partyIds)
        {
            return signature != null && signature.IsRevealed && !signature.Completed &&
                   PartyPercent(signature, partyIds) >= ScanningTuning.InfoLocatedPercent;
        }

        /// <summary>
        /// The rows the Scanner tab shows a viewer: every signature in the area that the viewer's party has any signal
        /// on, strongest first.
        /// </summary>
        public static List<ScannerRow> BuildRows(
            IEnumerable<ScanSignature> signatures,
            IReadOnlyCollection<string> partyIds,
            bool tierEarly,
            float areaWidth,
            float areaHeight)
        {
            var rows = new List<ScannerRow>();
            foreach (var signature in signatures ?? Enumerable.Empty<ScanSignature>())
            {
                if (signature.Completed)
                    continue;

                var percent = PartyPercent(signature, partyIds);
                if (percent <= 0)
                    continue;

                rows.Add(new ScannerRow
                {
                    SignatureId = signature.Id,
                    Name = RowName(signature, percent, tierEarly),
                    Detail = RowDetail(signature, percent, areaWidth, areaHeight),
                    Tooltip = RowTooltip(signature, percent, areaWidth, areaHeight),
                    Percent = percent,
                    CanJump = CanJump(signature, partyIds),
                });
            }

            return rows.OrderByDescending(x => x.Percent).ThenBy(x => x.SignatureId, StringComparer.Ordinal).ToList();
        }
    }
}
