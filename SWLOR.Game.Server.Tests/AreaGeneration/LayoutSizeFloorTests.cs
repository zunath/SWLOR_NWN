using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Pins the empirically measured per-style minimum area sizes (LayoutStyleSizeFloor): every
/// shipped layout profile, composed with a representative tileset, must generate and resolve
/// reliably at exactly its floor size under the standard 6-attempt retry. UI sliders, review
/// specs, and /genarea clamp to these floors, so a floor that stops holding must fail here
/// rather than in a user's hands.
/// </summary>
public class LayoutSizeFloorTests
{
    // Each shipped layout profile paired with the tileset profile its production themes use
    // (Streets is vmr01-only by vocabulary).
    private static readonly (string LayoutKey, string TilesetKey)[] ShippedPairings =
    {
        (StandardLayoutProfiles.Organic, StandardTilesetProfiles.Cavern),
        (StandardLayoutProfiles.Warren, StandardTilesetProfiles.Sewers),
        (StandardLayoutProfiles.Packed, StandardTilesetProfiles.Facility),
        (StandardLayoutProfiles.Halls, StandardTilesetProfiles.AncientRuin),
        (StandardLayoutProfiles.Complex, StandardTilesetProfiles.Facility),
        (StandardLayoutProfiles.Labyrinth, StandardTilesetProfiles.Cavern),
        (StandardLayoutProfiles.Streets, StandardTilesetProfiles.AncientRuin),
    };

    [Test]
    public void EveryShippedProfile_GeneratesReliablyAtItsSizeFloor()
    {
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        var failures = new List<string>();

        foreach (var (layoutKey, tilesetKey) in ShippedPairings)
        {
            var layout = layoutProfiles[layoutKey];
            var tileset = tilesetProfiles[tilesetKey];
            var model = LoadTileset(tileset.TilesetResref);
            var floor = LayoutStyleSizeFloor.For(layout.Template.Style);

            for (var baseSeed = 95000; baseSeed < 95015; baseSeed++)
            {
                var succeeded = false;
                string lastReason = null;

                // Mirror the production retry: up to 6 attempts with derived seeds.
                for (var attempt = 0; attempt < 6 && !succeeded; attempt++)
                {
                    var composition = new DungeonComposition { Content = null, Tileset = tileset, Layout = layout };
                    var parameters = composition.BuildLayoutParameters();
                    parameters.Width = floor;
                    parameters.Height = floor;
                    parameters.SolidTerrain = model.DefaultTerrain;
                    parameters.OpenTerrain = string.IsNullOrEmpty(tileset.PrimaryOpenTerrain)
                        ? model.FloorTerrain
                        : tileset.PrimaryOpenTerrain;

                    var rng = new Random(baseSeed + attempt);
                    try
                    {
                        var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
                        succeeded = TileResolver.TryResolve(model, macro, rng, out _, out lastReason);
                    }
                    catch (InvalidOperationException ex)
                    {
                        lastReason = ex.Message;
                    }
                }

                if (!succeeded)
                    failures.Add($"{layoutKey}/{tilesetKey} at {floor}x{floor}, seed {baseSeed}: {lastReason}");
            }
        }

        failures.Should().BeEmpty();
    }

    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);
}
