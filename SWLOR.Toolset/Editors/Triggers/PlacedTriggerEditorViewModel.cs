using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Triggers;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Shell.Panels.PaletteHost;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// SWLOR's placed-trigger editor: the shared trigger behavior editor bound to SWLOR's trigger
    /// behaviors, destination resolution, choices, local-variable policy and prompts. Trigger
    /// blueprints keep <see cref="TriggerEditorViewModel"/>.
    /// </summary>
    public sealed class PlacedTriggerEditorViewModel : TriggerBehaviorEditorViewModel
    {
        public PlacedTriggerEditorViewModel(
            JsonGffStruct trigger,
            string headerOwner,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            TransitionDestinationResolver? resolveDestination = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            ChoicePreviewService? previews = null,
            Services.IEditorPromptService? prompts = null,
            OutputLogService? log = null)
            : base(
                trigger ?? throw new ArgumentNullException(nameof(trigger)),
                headerOwner,
                isInstance: true,
                runEdit,
                CreateHost(gameCodeIndex, resolveDestination, resolveChoices, previews, prompts, log))
        {
        }

        /// <summary>SWLOR's trigger data and services for the shared trigger editor.</summary>
        public static TriggerBehaviorEditorHost CreateHost(
            IGameCodeIndex? gameCodeIndex,
            TransitionDestinationResolver? resolveDestination,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            ChoicePreviewService? previews,
            Services.IEditorPromptService? prompts,
            OutputLogService? log) => new()
        {
            Catalog = SwlorTriggerBehaviorCatalog.Instance,
            ResolveDestination = resolveDestination,
            ResolveChoices = resolveChoices,
            ChoicePreviews = previews,
            Variables = new SwlorVarTableSectionFactory(gameCodeIndex),
            Prompts = prompts == null ? null : new SwlorPalettePrompts(prompts),
            Log = log == null ? null : new SwlorPaletteLog(log),
        };
    }
}
