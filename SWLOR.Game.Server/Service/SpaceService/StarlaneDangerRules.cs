using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Dangerous starlanes (plan section 12), pure. A lane is dangerous for the pilot using it when the system it
    /// leads to is a higher tier than the system the pilot is in, is one of the three Small and Medium hull zones, or
    /// is Wild Space. Lanes are named and described for it ahead of the jump, and the jump repeats the warning.
    /// </summary>
    public static class StarlaneDangerRules
    {
        /// <summary>The three Small and Medium hull zones (W6c), each a dead end one lane from its parent system.</summary>
        public static readonly IReadOnlyList<string> ZoneAreaResrefs = new[]
        {
            "space_viscbelt", "space_midrimneb", "space_korrdebris",
        };

        /// <summary>Marks a lane name that is already marked, so a second pass leaves it alone.</summary>
        public const string NameMarker = " [Danger: ";

        public static bool IsZone(string areaResref) =>
            !string.IsNullOrWhiteSpace(areaResref) && ZoneAreaResrefs.Contains(areaResref, StringComparer.OrdinalIgnoreCase);

        /// <summary>A system's tier in tenths (Wild Space 55), or 0 when it has no tier.</summary>
        public static int GetTierTenths(string areaResref)
        {
            if (string.IsNullOrWhiteSpace(areaResref))
                return 0;

            if (WildSpaceRules.IsWildSpaceSystem(areaResref))
                return WildSpaceTuning.TierTenths;

            var orbit = TradeLookup.GetOrbit(areaResref);
            return orbit == null ? 0 : orbit.Tier * 10;
        }

        /// <summary>A tier in tenths as a label: 30 is "3", 55 is "5.5".</summary>
        public static string FormatTier(int tierTenths) =>
            tierTenths % 10 == 0 ? (tierTenths / 10).ToString() : $"{tierTenths / 10}.{tierTenths % 10}";

        /// <summary>
        /// What makes a lane from <paramref name="fromAreaResref"/> to <paramref name="toAreaResref"/> dangerous. A
        /// system with no tier never counts as higher, and a pilot already in Wild Space is not warned of it again.
        /// </summary>
        public static StarlaneDanger Classify(string fromAreaResref, string toAreaResref)
        {
            if (string.IsNullOrWhiteSpace(toAreaResref))
                return StarlaneDanger.None;

            var danger = StarlaneDanger.None;
            var from = GetTierTenths(fromAreaResref);
            var to = GetTierTenths(toAreaResref);
            if (from > 0 && to > from)
                danger |= StarlaneDanger.HigherTier;

            if (IsZone(toAreaResref) && !string.Equals(fromAreaResref, toAreaResref, StringComparison.OrdinalIgnoreCase))
                danger |= StarlaneDanger.Zone;

            if (WildSpaceRules.IsWildSpaceSystem(toAreaResref) && !WildSpaceRules.IsWildSpaceSystem(fromAreaResref))
                danger |= StarlaneDanger.WildSpace;

            return danger;
        }

        /// <summary>The short tag a dangerous lane's name carries, such as " [Danger: Tier 3, Zone]". Empty for a safe lane.</summary>
        public static string GetNameTag(StarlaneDanger danger, string toAreaResref)
        {
            if (danger == StarlaneDanger.None)
                return string.Empty;

            var parts = new List<string>();
            if (danger.HasFlag(StarlaneDanger.HigherTier) || danger.HasFlag(StarlaneDanger.WildSpace))
                parts.Add($"Tier {FormatTier(GetTierTenths(toAreaResref))}");
            if (danger.HasFlag(StarlaneDanger.Zone))
                parts.Add("Zone");
            if (danger.HasFlag(StarlaneDanger.WildSpace))
                parts.Add("Wild Space");

            return $"{NameMarker}{string.Join(", ", parts)}]";
        }

        /// <summary>The sentences that say what a dangerous lane leads to, for its description and the warning on use.</summary>
        public static IReadOnlyList<string> GetReasons(StarlaneDanger danger, string fromAreaResref, string toAreaResref)
        {
            var reasons = new List<string>();
            if (danger.HasFlag(StarlaneDanger.HigherTier))
            {
                reasons.Add($"It leads to tier {FormatTier(GetTierTenths(toAreaResref))} space, above the tier " +
                            $"{FormatTier(GetTierTenths(fromAreaResref))} you are in, so expect tougher ships.");
            }

            if (danger.HasFlag(StarlaneDanger.Zone))
                reasons.Add("It is a dead-end zone that admits only Small and Medium hulls.");

            if (danger.HasFlag(StarlaneDanger.WildSpace))
            {
                reasons.Add("Wild Space is lawless: other players can attack you there, and a ship another player destroys can drop its modules.");
            }

            return reasons;
        }

        /// <summary>The line a pilot sees as they jump a dangerous lane. Empty for a safe lane.</summary>
        public static string GetWarning(StarlaneDanger danger, string fromAreaResref, string toAreaResref, string destinationName)
        {
            if (danger == StarlaneDanger.None)
                return string.Empty;

            return $"Warning: {destinationName} is dangerous. " + string.Join(" ", GetReasons(danger, fromAreaResref, toAreaResref));
        }

        /// <summary>A lane's description with its danger sentences added after the original text.</summary>
        public static string GetDescription(string original, StarlaneDanger danger, string fromAreaResref, string toAreaResref)
        {
            if (danger == StarlaneDanger.None)
                return original ?? string.Empty;

            var warning = "Dangerous lane. " + string.Join(" ", GetReasons(danger, fromAreaResref, toAreaResref));
            return string.IsNullOrWhiteSpace(original) ? warning : $"{original.TrimEnd()}\n\n{warning}";
        }
    }
}
