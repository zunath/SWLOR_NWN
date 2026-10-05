using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>The area document in front, resolved lazily through the dock factory.</summary>
    internal sealed class SwlorPalettePlacementTargets : IPalettePlacementTargetProvider
    {
        private readonly Func<IAreaPlacementTarget?> _placementTarget;

        public SwlorPalettePlacementTargets(Func<IAreaPlacementTarget?> placementTarget)
        {
            _placementTarget = placementTarget ?? throw new ArgumentNullException(nameof(placementTarget));
        }

        public IPalettePlacementTarget? ActiveTarget =>
            _placementTarget() is { } target ? new SwlorPalettePlacementTarget(target) : null;
    }
}
