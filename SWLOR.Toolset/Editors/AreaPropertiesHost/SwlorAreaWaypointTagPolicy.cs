using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Editors.AreaPropertiesHost
{
    /// <summary>
    /// SWLOR's singleton destination tags: the current waypoint catalog decides which tags are
    /// singletons, and the module tag index counts placements in the module's other areas.
    /// </summary>
    internal sealed class SwlorAreaWaypointTagPolicy : IAreaWaypointTagPolicy
    {
        private readonly ModuleWorkspace _workspace;
        private readonly string _areaResRef;
        private readonly Func<WaypointBehaviorCatalog> _catalog;

        public SwlorAreaWaypointTagPolicy(ModuleWorkspace workspace, string areaResRef, Func<WaypointBehaviorCatalog> catalog)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _areaResRef = areaResRef;
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public bool IsSingletonTag(string tag) => _catalog().IsSingletonDestinationTag(tag);

        public string? ResolveTag(JsonGffStruct waypoint) => _workspace.TagIndex.ResolveWaypointTag(waypoint);

        public int CountPlacementsOutsideArea(string tag) =>
            _workspace.TagIndex.CountWaypointPlacementsOutsideArea(tag, _areaResRef);
    }
}
