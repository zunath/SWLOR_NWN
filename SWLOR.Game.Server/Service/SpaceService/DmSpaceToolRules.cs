using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The rules behind the DM Tools Space tab's derelict, signature, Rift Pocket and cloak tools (plan sections 24, 28,
    /// 31 and 6), pure. The tools reuse the shipped signature, derelict and Rift Run code; this holds only what they
    /// ask of it and how they report.
    /// </summary>
    public static class DmSpaceToolRules
    {
        /// <summary>The kinds of signature the picker offers. Rifts belong to the rift network and are not offered.</summary>
        public static readonly IReadOnlyList<DmSignatureChoice> SignatureChoices = new[]
        {
            new DmSignatureChoice { Label = "Combat Site", Type = SignatureType.CombatSite },
            new DmSignatureChoice { Label = "Data Site", Type = SignatureType.DataSite },
            new DmSignatureChoice { Label = "Cache Site", Type = SignatureType.CacheSite },
            new DmSignatureChoice { Label = "Gas Site", Type = SignatureType.GasSite },
            new DmSignatureChoice { Label = "Derelict", Type = SignatureType.Derelict },
        };

        /// <summary>The Start Capital Battle picker: a random orbit first, then each orbit that hosts battles.</summary>
        public static IReadOnlyList<string> CapitalBattleLabels =>
            new[] { "Random orbit" }.Concat(IncursionTuning.CapitalBattleAreas.Select(x => $"{x.Name} (tier {x.Tier})")).ToList();

        /// <summary>The orbit picked in the Start Capital Battle picker, or empty for a random one.</summary>
        public static string GetCapitalBattleArea(int index) =>
            index >= 1 && index <= IncursionTuning.CapitalBattleAreas.Count ? IncursionTuning.CapitalBattleAreas[index - 1].Resref : string.Empty;

        /// <summary>The Rift Key tier labels the picker offers: "Tier 1" to "Tier 5.5".</summary>
        public static IReadOnlyList<string> RiftTierLabels => RiftRunTuning.Keys.Select(x => $"Tier {x.TierLabel}").ToList();

        /// <summary>The signature picked at an index, or the first when the index is out of range.</summary>
        public static DmSignatureChoice GetSignatureChoice(int index) =>
            SignatureChoices[Math.Clamp(index, 0, SignatureChoices.Count - 1)];

        /// <summary>The Rift Key picked at an index, or the first when the index is out of range.</summary>
        public static RiftKeyDetail GetRiftKey(int index) =>
            RiftRunTuning.Keys[Math.Clamp(index, 0, RiftRunTuning.Keys.Count - 1)];

        /// <summary>
        /// Why a signature or derelict cannot be placed in an area, or empty. Only the areas the scanning system tends
        /// (<see cref="ScanningTuning.Areas"/>) end a site and check it, so a site placed anywhere else would never end.
        /// </summary>
        public static string GetPlaceRefusal(bool isSpaceArea, string areaResref)
        {
            if (!isSpaceArea)
                return "You must be in a space area.";

            return SignatureSiteRules.GetArea(areaResref) == null
                ? "Signatures and derelicts can only be placed in the orbits, zones and Wild Space systems that hold them."
                : string.Empty;
        }

        /// <summary>The DM's confirmation of a placed signature or derelict.</summary>
        public static string DescribePlaced(ScanSignature signature, bool revealed)
        {
            var kind = signature.Type == SignatureType.Derelict && signature.Boardable
                ? "boardable derelict"
                : signature.Type.ToString();
            var tier = signature.WildSpace ? WildSpaceTuning.TierLabel : signature.Tier.ToString();
            return revealed
                ? $"Placed a tier {tier} {kind} where you pointed. Its site is open."
                : $"Placed a hidden tier {tier} {kind} where you pointed. A Probe Launcher can find it.";
        }

        /// <summary>One line for a Rift Run in the DM's list: tier, ships, chamber, phase and time left.</summary>
        public static string DescribeRiftRun(RiftRun run, DateTime now)
        {
            var room = RiftRunTuning.Rooms[Math.Clamp(run.Room, 0, RiftRunTuning.Rooms.Count - 1)].Name;
            var left = RiftRunRules.FormatTimer(RiftRunRules.SecondsLeft(run.StartedAt, now));
            return $"Tier {run.Key.TierLabel}, {run.Ships} ship(s), chamber {run.Room + 1} of {RiftRunTuning.Rooms.Count} ({room}), " +
                   $"{run.Phase.ToString().ToLowerInvariant()}, {left} left";
        }

        /// <summary>The DM's one-line summary of the open pockets.</summary>
        public static string DescribeRiftRuns(IReadOnlyCollection<RiftRun> runs) =>
            runs.Count == 0
                ? "No Rift Pockets are open."
                : $"{runs.Count} of {RiftRunTuning.MaxRuns} Rift Pockets open.";
    }
}
