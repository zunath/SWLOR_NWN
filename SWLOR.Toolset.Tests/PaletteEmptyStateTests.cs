using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The Palette's "no area open" state: which conditions raise it, and - the part that was wrong -
    /// which clear it again.
    /// </summary>
    [TestFixture]
    public class PaletteEmptyStateTests
    {
        /// <summary>An area in front, made of <paramref name="tilesetResRef"/>. Nothing is ever placed into it.</summary>
        private sealed class StubPlacementTarget(string tilesetResRef) : IAreaPlacementTarget
        {
            public string? TilesetResRef { get; } = tilesetResRef;

            public bool ArmPlacement(ResourceType type, string resRef, PaletteSource source) => false;

            public bool ArmTilePlacement(TilePaletteEntry entry) => false;
        }

        private static PaletteViewModel BuildPalette(IAreaPlacementTarget? target)
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            return new PaletteViewModel(
                workspace,
                new CategoryService(workspace, log),
                log,
                placementTarget: () => target);
        }

        [Test]
        public void TilesWithNoAreaOpen_RaisesTheEmptyState()
        {
            var palette = BuildPalette(target: null);

            palette.IsTileMode = true;

            palette.NeedsOpenArea.Should().BeTrue();
        }

        /// <summary>
        /// With an area in front the state is down, whatever becomes of the tileset afterwards. The
        /// question this answers is "is there an area to take tiles from", not "did its tileset parse" -
        /// a tileset that fails to load has its own message and is not a reason to tell the builder to
        /// open an area they already have open.
        /// </summary>
        [Test]
        public void TilesWithAnAreaOpen_DoesNot()
        {
            var palette = BuildPalette(new StubPlacementTarget("tin01"));

            palette.IsTileMode = true;

            palette.NeedsOpenArea.Should().BeFalse();
        }

        /// <summary>
        /// Switching to a blueprint type clears it. This is the bug that prompted the change: the text
        /// used to live in StatusMessage, which persists, so "open an area to see the tiles" stayed on
        /// screen under a list of creatures - where it was neither true nor about anything on screen.
        /// </summary>
        [Test]
        public void LeavingTilesForABlueprintType_ClearsTheEmptyState()
        {
            var palette = BuildPalette(target: null);
            palette.IsTileMode = true;
            palette.NeedsOpenArea.Should().BeTrue("the fixture starts with no area open");

            palette.IsTileMode = false;

            palette.NeedsOpenArea.Should().BeFalse();
        }

        /// <summary>
        /// The state carries the explanation, so the status line must not repeat it. Two copies of the
        /// same sentence in one panel is how the footnote came to look like a caption on nothing.
        /// </summary>
        [Test]
        public void TheEmptyStateDoesNotAlsoWriteAStatusLine()
        {
            var palette = BuildPalette(target: null);

            palette.IsTileMode = true;

            palette.NeedsOpenArea.Should().BeTrue();
            palette.StatusMessage.Should().BeEmpty();
        }

        /// <summary>Opening an area after the fact takes the state down without touching the type chips.</summary>
        [Test]
        public void OpeningAnAreaWhileTilesAreShown_TakesTheStateDown()
        {
            IAreaPlacementTarget? target = null;
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var palette = new PaletteViewModel(
                workspace, new CategoryService(workspace, log), log, placementTarget: () => target);

            palette.IsTileMode = true;
            palette.NeedsOpenArea.Should().BeTrue();

            target = new StubPlacementTarget("tin01");
            palette.OnActiveAreaChanged();

            palette.NeedsOpenArea.Should().BeFalse();
        }
    }
}
