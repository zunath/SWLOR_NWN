using System;
using System.IO;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Shared tileset loading for the AreaGeneration test suite: locates the repository root and
/// resolves a tileset resref's .set file via the same SWLOR_Haks-then-basegame_sets resolution
/// TilesetSetSource gives SWLOR.ProcgenReview/SWLOR.ContentBuilder, instead of each test file keeping
/// its own duplicated TilesetHakDirectories dictionary + LoadTileset/FindRepositoryRoot pair.
/// </summary>
internal static class TilesetTestSource
{
    /// <summary>
    /// Loads and parses <paramref name="tilesetResref"/>'s .set file, searching SWLOR_Haks (hak
    /// copies win) then basegame_sets. Behavior-identical to every test file's former hardcoded
    /// SWLOR_Haks-directory lookup for the four original generation tilesets (tdt01/zsf01/tds01/vmr01),
    /// and additionally resolves any base-game tileset onboarded only via basegame_sets (e.g. the
    /// tdc01/tde01/tin01 pilots).
    /// </summary>
    public static TilesetModel LoadTileset(string tilesetResref)
    {
        return TilesetSetSource.Load(FindRepositoryRoot().FullName, tilesetResref);
    }

    public static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SWLOR.Game.Server.sln")))
                return current;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (SWLOR.Game.Server.sln).");
    }
}
