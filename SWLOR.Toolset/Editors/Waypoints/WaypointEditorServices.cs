using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Waypoints
{
    /// <summary>Module services needed when a waypoint editor is embedded in an area.</summary>
    public sealed record WaypointEditorServices(
        string HeaderOwner,
        WaypointBehaviorCatalog Catalog,
        Func<string, IReadOnlyList<BehaviorChoice>>? ResolveChoices = null,
        ChoicePreviewService? ChoicePreviews = null);
}
