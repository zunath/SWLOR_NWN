using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.GameData.GameCode;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Shell.Panels.PaletteHost;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Waypoints
{
    /// <summary>
    /// SWLOR's waypoint editor: the shared waypoint behavior editor bound to SWLOR's module-derived
    /// waypoint catalog, local-variable policy and prompts.
    /// </summary>
    public sealed class WaypointEditorViewModel : WaypointBehaviorEditorViewModel
    {
        public WaypointEditorViewModel(
            JsonGffStruct waypoint,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            WaypointBehaviorCatalog catalog,
            IGameCodeIndex? gameCodeIndex = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            ChoicePreviewService? previews = null,
            Services.IEditorPromptService? prompts = null,
            Func<string, bool>? singletonTagInUse = null,
            OutputLogService? log = null)
            : base(
                waypoint ?? throw new ArgumentNullException(nameof(waypoint)),
                headerOwner,
                isInstance,
                runEdit,
                CreateHost(catalog, gameCodeIndex, resolveChoices, previews, prompts, log),
                singletonTagInUse)
        {
        }

        /// <summary>SWLOR's waypoint data and services for the shared waypoint editor.</summary>
        public static WaypointBehaviorEditorHost CreateHost(
            WaypointBehaviorCatalog catalog,
            IGameCodeIndex? gameCodeIndex,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            ChoicePreviewService? previews,
            Services.IEditorPromptService? prompts,
            OutputLogService? log) => new()
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)),
            ResolveChoices = resolveChoices,
            ChoicePreviews = previews,
            Variables = new SwlorVarTableSectionFactory(gameCodeIndex),
            Prompts = prompts == null ? null : new SwlorPalettePrompts(prompts),
            Log = log == null ? null : new SwlorPaletteLog(log),
        };
    }
}
