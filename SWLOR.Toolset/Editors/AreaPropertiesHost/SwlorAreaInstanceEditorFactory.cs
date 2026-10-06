using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Editors.Sounds;
using SWLOR.Toolset.Editors.Waypoints;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.AreaPropertiesHost
{
    /// <summary>
    /// Builds SWLOR's door, waypoint and sound editors for the shared instance sections. Doors and
    /// sounds always use their typed editor; waypoints do when the area has waypoint services.
    /// </summary>
    internal sealed class SwlorAreaInstanceEditorFactory : IAreaInstanceEditorFactory
    {
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly DoorEditorServices? _doors;
        private readonly string _soundHeaderOwner;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveSoundChoices;
        private readonly IReadOnlyList<string> _audioResources;
        private readonly SoundPreviewService? _soundPreview;

        public SwlorAreaInstanceEditorFactory(
            IGameCodeIndex? gameCodeIndex,
            OutputLogService log,
            IEditorPromptService prompts,
            DoorEditorServices? doors,
            string? soundHeaderOwner,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveSoundChoices,
            IReadOnlyList<string>? audioResources,
            SoundPreviewService? soundPreview)
        {
            _gameCodeIndex = gameCodeIndex;
            _log = log;
            _prompts = prompts;
            _doors = doors;
            _soundHeaderOwner = soundHeaderOwner ?? string.Empty;
            _resolveSoundChoices = resolveSoundChoices;
            _audioResources = audioResources ?? Array.Empty<string>();
            _soundPreview = soundPreview;
        }

        /// <summary>The waypoint services; replaced when the module's transition destinations change.</summary>
        public WaypointEditorServices? Waypoints { get; set; }

        public DoorBehaviorEditorViewModel? CreateDoor(
            JsonGffStruct door, Func<string, Action, bool> runEdit, bool isDocumentDirty) =>
            new DoorEditorViewModel(
                door,
                _doors?.HeaderOwner ?? "area",
                isInstance: true,
                runEdit,
                _gameCodeIndex,
                _doors?.ResolveTag,
                _doors?.ResolveChoices,
                _doors?.Appearances,
                _doors?.ResourceIndex,
                _doors?.ResolveModel,
                isDocumentDirty,
                _doors?.Thumbnails,
                _doors?.ChoicePreviews,
                _prompts,
                log: _log);

        public WaypointBehaviorEditorViewModel? CreateWaypoint(
            JsonGffStruct waypoint, Func<string, Action, bool> runEdit, Func<string, bool> singletonTagInUse) =>
            Waypoints is not { } services
                ? null
                : new WaypointEditorViewModel(
                    waypoint,
                    services.HeaderOwner,
                    isInstance: true,
                    runEdit,
                    services.Catalog,
                    _gameCodeIndex,
                    services.ResolveChoices,
                    services.ChoicePreviews,
                    _prompts,
                    singletonTagInUse,
                    log: _log);

        public SoundBehaviorEditorViewModel? CreateSound(JsonGffStruct sound, Func<string, Action, bool> runEdit) =>
            new SoundEditorViewModel(
                sound,
                _soundHeaderOwner,
                isInstance: true,
                runEdit,
                _gameCodeIndex,
                _resolveSoundChoices,
                _audioResources,
                _soundPreview,
                _prompts,
                _log);

        public VarTableSectionViewModel CreateVariables(Func<string, Action, bool> runEdit, VarTable table) =>
            SwlorVarTablePolicy.Create(runEdit, table, _gameCodeIndex);
    }
}
