using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.GameData.GameCode;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Shell.Panels.PaletteHost;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Sounds
{
    /// <summary>
    /// SWLOR's ambient-sound editor: the shared sound behavior editor bound to SWLOR's behavior
    /// catalog, audio resources, preview playback, local-variable policy and prompts.
    /// </summary>
    public sealed class SoundEditorViewModel : SoundBehaviorEditorViewModel
    {
        public SoundEditorViewModel(
            JsonGffStruct sound,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<string>? audioResources = null,
            Services.SoundPreviewService? preview = null,
            Services.IEditorPromptService? prompts = null,
            OutputLogService? log = null)
            : base(
                sound ?? throw new ArgumentNullException(nameof(sound)),
                headerOwner,
                isInstance,
                runEdit,
                CreateHost(gameCodeIndex, resolveChoices, audioResources, preview, prompts, log))
        {
        }

        /// <summary>SWLOR's sound data and services for the shared sound editor.</summary>
        public static SoundBehaviorEditorHost CreateHost(
            IGameCodeIndex? gameCodeIndex,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            IReadOnlyList<string>? audioResources,
            Services.SoundPreviewService? preview,
            Services.IEditorPromptService? prompts,
            OutputLogService? log) => new()
        {
            Catalog = SwlorSoundBehaviorCatalog.Instance,
            ResolveChoices = resolveChoices,
            AudioResources = audioResources ?? Array.Empty<string>(),
            Preview = preview,
            Variables = new SwlorVarTableSectionFactory(gameCodeIndex),
            Prompts = prompts == null ? null : new SwlorPalettePrompts(prompts),
            Log = log == null ? null : new SwlorPaletteLog(log),
        };
    }
}
