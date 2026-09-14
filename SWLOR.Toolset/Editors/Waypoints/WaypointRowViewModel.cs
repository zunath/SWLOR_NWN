using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Waypoints
{
    /// <summary>
    /// One row of the waypoint editor. The waypoint form needs nothing beyond the shared row's
    /// shape, so this exists only to name the type its editor builds.
    /// </summary>
    public sealed class WaypointRowViewModel : BehaviorRowViewModel
    {
        public WaypointRowViewModel(
            BehaviorFieldDefinition definition,
            BehaviorValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoice>? choices = null,
            Action? valueChanged = null,
            ChoicePreviewService? previews = null)
            : base(definition, store, runEdit, choices, valueChanged, previews)
        {
            Reload();
        }
    }
}
