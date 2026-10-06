using Nwn.Authoring.Editing;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.AreaPropertiesHost;
using SWLOR.Toolset.Editors.Waypoints;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels.PaletteHost;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// One placed-instance section of SWLOR's area Properties page: the shared section bound to the
    /// open module's blueprints and palettes, SWLOR's door/waypoint/sound editors, its local-variable
    /// policy and its singleton waypoint destinations.
    /// </summary>
    public sealed class InstanceListSectionViewModel : AreaInstanceSectionViewModel
    {
        private readonly SwlorAreaInstanceEditorFactory _editors;

        public InstanceListSectionViewModel(
            string title,
            string listFieldName,
            ResourceType blueprintType,
            DocumentSession gitSession,
            DocumentSession gicSession,
            ModuleWorkspace workspace,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            Func<uint, string?>? resolveStrRef = null,
            Doors.DoorEditorServices? doorEditorServices = null,
            WaypointEditorServices? waypointEditorServices = null,
            string? soundHeaderOwner = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveSoundChoices = null,
            IReadOnlyList<string>? audioResources = null,
            Services.SoundPreviewService? soundPreview = null)
            : this(
                title,
                listFieldName,
                blueprintType,
                gitSession,
                gicSession,
                workspace,
                runEdit,
                log,
                resolveStrRef,
                waypointEditorServices,
                new SwlorAreaInstanceEditorFactory(
                    gameCodeIndex,
                    log,
                    prompts,
                    doorEditorServices,
                    soundHeaderOwner,
                    resolveSoundChoices,
                    audioResources,
                    soundPreview)
                {
                    Waypoints = waypointEditorServices,
                })
        {
        }

        private InstanceListSectionViewModel(
            string title,
            string listFieldName,
            ResourceType blueprintType,
            DocumentSession gitSession,
            DocumentSession gicSession,
            ModuleWorkspace workspace,
            Func<string, Action, bool> runEdit,
            OutputLogService log,
            Func<uint, string?>? resolveStrRef,
            WaypointEditorServices? waypointEditorServices,
            SwlorAreaInstanceEditorFactory editors)
            : base(title, listFieldName, blueprintType, new AreaInstanceSectionHost
            {
                Instances = gitSession,
                Comments = gicSession,
                RunEdit = runEdit,
                Blueprints = new SwlorAreaInstanceBlueprintSource(workspace),
                Palettes = new SwlorAreaInstancePaletteSource(workspace),
                ResolveStrRef = resolveStrRef,
                Editors = editors,
                WaypointTags = waypointEditorServices == null
                    ? null
                    : new SwlorAreaWaypointTagPolicy(
                        workspace,
                        waypointEditorServices.HeaderOwner,
                        () => editors.Waypoints!.Catalog),
                Log = new SwlorPaletteLog(log),
            })
        {
            _editors = editors;
        }

        /// <summary>
        /// Rebinds both the currently selected waypoint and future selections to the latest
        /// module transition-destination catalog.
        /// </summary>
        public void RefreshWaypointCatalog(WaypointBehaviorCatalog catalog)
        {
            if (_editors.Waypoints == null)
                return;

            _editors.Waypoints = _editors.Waypoints with { Catalog = catalog };
            WaypointEditor?.RefreshCatalog(catalog);
        }
    }
}
