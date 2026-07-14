using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Permanent guard for DungeonTilesetProfile.ExcludedTiles (confirmed placeholder/stub art -- see
/// BaseGameTilesetProfiles.FortInterior/FortInteriorLegacy's own doc comments for twc03's 15 "xyz"-
/// family tiles, the first and so far only declared exclusion). Three things must hold for every
/// profile that declares ExcludedTiles, forever:
///
///   1. Every excluded ID must genuinely exist in the tileset's own .set data (catches a typo'd ID
///      that would otherwise silently exclude nothing).
///   2. No excluded ID may be a member of any group this SAME profile still wires as a SetPiece or
///      ExitGroup -- LayoutGroupStamper's pinned-tile path bypasses TileResolver's candidate lookup
///      entirely (see TileResolver's class doc comment), so an excluded tile that is ALSO a wired
///      group member would still get placed via stamping regardless of the TileResolver-level
///      exclusion. If this ever fails, the fix is to also drop the group from that profile's
///      SetPieces/ExitGroups (see BaseGameTilesetProfiles.FortInteriorLegacy, which removed its
///      "OLD_"-prefixed furnished-room family entirely for exactly this reason), not to add a
///      workaround here.
///   3. A real generation sweep (Complex/Tunnel -- the layout style most likely to reach every
///      structural tile, and specifically how twc03's "xyz" family was originally observed placing as
///      tunnel-corridor terminators) across many seeds must never emit an excluded tile ID.
/// </summary>
public class ExcludedTileRegressionTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static readonly Dictionary<string, DungeonTilesetProfile> TilesetProfiles =
        new BaseGameTilesetProfiles().BuildTilesetProfiles();

    private static readonly Dictionary<string, DungeonLayoutProfile> LayoutProfiles =
        new StandardLayoutProfiles().BuildLayoutProfiles();

    /// <summary>Every profile key that currently declares ExcludedTiles -- discovered, not
    /// hand-maintained, so a future exclusion on any tileset automatically gets this same coverage.</summary>
    public static IEnumerable<string> ProfilesWithExcludedTiles => TilesetProfiles
        .Where(kv => kv.Value.ExcludedTiles.Count > 0)
        .Select(kv => kv.Key)
        .ToList();

    [OneTimeSetUp]
    public void EnsureAnyExclusionsAreActuallyDeclared()
    {
        // If this ever fires, either a future change removed the last ExcludedTiles declaration
        // (fine -- delete this whole file) or ProfilesWithExcludedTiles itself is broken. Either way,
        // a completely empty TestCaseSource silently no-ops every [TestCaseSource]-driven test below
        // instead of failing, so assert the precondition explicitly rather than trusting NUnit to
        // notice zero test cases ran.
        ProfilesWithExcludedTiles.Should().NotBeEmpty(
            "at least twc03/fortinterior and fortinterior_legacy are expected to declare ExcludedTiles; " +
            "if every declaration was genuinely removed, delete this test file instead of leaving it inert");
    }

    [TestCaseSource(nameof(ProfilesWithExcludedTiles))]
    public void ExcludedTiles_ExistInTheSet(string profileKey)
    {
        var profile = TilesetProfiles[profileKey];
        var model = LoadTileset(profile.TilesetResref);

        foreach (var tileId in profile.ExcludedTiles)
        {
            (tileId >= 0 && tileId < model.Tiles.Count).Should().BeTrue(
                $"{profileKey}'s ExcludedTiles entry {tileId} must be a real tile index in " +
                $"{profile.TilesetResref}'s .set data (0..{model.Tiles.Count - 1}) -- catches a typo'd ID " +
                "that would otherwise silently exclude nothing");
        }
    }

    [TestCaseSource(nameof(ProfilesWithExcludedTiles))]
    public void ExcludedTiles_AreNotWiredSetPieceOrExitGroupMembers(string profileKey)
    {
        var profile = TilesetProfiles[profileKey];
        var model = LoadTileset(profile.TilesetResref);

        var wiredGroupNames = profile.SetPieces.Keys.Concat(profile.ExitGroups).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var offendingMembers = new List<string>();
        foreach (var group in model.Groups)
        {
            if (!wiredGroupNames.Contains(group.Name)) continue;

            foreach (var tileId in group.TileIds)
            {
                if (tileId >= 0 && profile.ExcludedTiles.Contains(tileId))
                    offendingMembers.Add($"TILE{tileId} (group '{group.Name}')");
            }
        }

        offendingMembers.Should().BeEmpty(
            $"{profileKey} declares these tile IDs excluded (placeholder art) but STILL wires a " +
            "SetPiece/ExitGroup group containing one -- LayoutGroupStamper's pinned-tile path bypasses " +
            "TileResolver's candidate-lookup exclusion entirely, so the group must also be dropped from " +
            "this profile (see BaseGameTilesetProfiles.FortInteriorLegacy for the precedent), not left " +
            "wired alongside the exclusion");
    }

    /// <summary>
    /// Real generation sweep: Complex/Tunnel is the layout style twc03's "xyz" family was originally
    /// observed placing under (tunnel-corridor terminators), so it is the sweep most likely to still
    /// emit an excluded tile if the exclusion mechanism ever regresses. 40 seeds x every profile that
    /// declares ExcludedTiles.
    /// </summary>
    [TestCaseSource(nameof(ProfilesWithExcludedTiles))]
    public void ExcludedTiles_NeverPlacedAcrossComplexSeedSweep(string profileKey)
    {
        var tilesetProfile = TilesetProfiles[profileKey];
        var layoutProfile = LayoutProfiles[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        const int size = 20;
        var seedCount = 0;
        var successCount = 0;
        var offendingTileIds = new HashSet<int>();

        for (var seed = 6000; seed < 6040; seed++)
        {
            seedCount++;
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;
            successCount++;

            foreach (var tile in solved.Resolved.Tiles)
            {
                if (tilesetProfile.ExcludedTiles.Contains(tile.TileId))
                    offendingTileIds.Add(tile.TileId);
            }
        }

        seedCount.Should().Be(40, "the seed loop must actually have run");
        successCount.Should().BeGreaterThan(0, $"{profileKey}/Complex must succeed at least once across the seed range for this sweep to mean anything");
        offendingTileIds.Should().BeEmpty(
            $"{profileKey}/Complex placed excluded (confirmed placeholder-art) tile ID(s) " +
            $"[{string.Join(",", offendingTileIds)}] somewhere across {seedCount} seeds -- the exclusion mechanism regressed");
    }
}
