using Nwn.Authoring.Areas.Tiles;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SharedPaletteSource = Nwn.Toolset.Avalonia.Palettes.PaletteSource;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>One SWLOR area editor as a shared palette placement target.</summary>
    internal sealed class SwlorPalettePlacementTarget : IPalettePlacementTarget
    {
        private readonly IAreaPlacementTarget _target;

        public SwlorPalettePlacementTarget(IAreaPlacementTarget target)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public string? TilesetResRef => _target.TilesetResRef;

        public bool ArmPlacement(ResourceType type, string resRef, SharedPaletteSource source) =>
            _target.ArmPlacement(
                type,
                resRef,
                source == SharedPaletteSource.Standard ? PaletteSource.Standard : PaletteSource.Custom);

        public bool ArmTilePlacement(TilePaletteEntry entry) => _target.ArmTilePlacement(entry);
    }
}
