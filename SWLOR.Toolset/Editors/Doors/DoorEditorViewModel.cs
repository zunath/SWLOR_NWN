using System.Numerics;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Resources;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Shell.Panels.PaletteHost;
using SWLOR.Toolset.Viewport;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>
    /// SWLOR's door editor: the shared door behavior editor bound to SWLOR's behavior catalog, key
    /// items, local-variable policy and prompts, plus the 3D model preview beside its tabs.
    /// </summary>
    public sealed class DoorEditorViewModel : DoorBehaviorEditorViewModel, IModelPreviewSource
    {
        private readonly Func<JsonGffStruct, BlueprintModelRenderResult>? _resolveModel;
        private ModelPreviewControl? _previewView;

        public ResourceIndex? ResourceIndex { get; }

        public AreaScene? PreviewScene { get; private set; }

        public string? PreviewAnimationName => null;

        public bool IsAnimationPlaying => false;

        public override Avalonia.Controls.Control PreviewView
        {
            get
            {
                if (_previewView != null)
                    return _previewView;

                _previewView = new ModelPreviewControl { DataContext = this };
                _previewView.SetHostVisible(true);
                return _previewView;
            }
        }

        public DoorEditorViewModel(
            JsonGffStruct door,
            string headerOwner,
            bool isInstance,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex = null,
            Func<BehaviorTagScope, string, string?>? resolveTag = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            IReadOnlyList<DoorAppearanceChoice>? appearances = null,
            ResourceIndex? resourceIndex = null,
            Func<JsonGffStruct, BlueprintModelRenderResult>? resolveModel = null,
            bool isDirty = false,
            ThumbnailService? thumbnails = null,
            ChoicePreviewService? choicePreviews = null,
            Services.IEditorPromptService? prompts = null,
            OutputLogService? log = null,
            TransitionDestinationResolver? resolveDestination = null)
            : base(
                door,
                headerOwner,
                isInstance,
                runEdit,
                CreateHost(
                    gameCodeIndex,
                    resolveDestination ?? (resolveTag == null ? null : SwlorTransitionDestinations.FromLocations(resolveTag)),
                    resolveChoices, appearances, thumbnails, choicePreviews, prompts, log),
                isDirty)
        {
            _resolveModel = resolveModel;
            ResourceIndex = resourceIndex;
            UpdatePreviewScene();
        }

        /// <summary>SWLOR's door data and services for the shared door editor.</summary>
        public static DoorBehaviorEditorHost CreateHost(
            IGameCodeIndex? gameCodeIndex,
            TransitionDestinationResolver? resolveDestination,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            IReadOnlyList<DoorAppearanceChoice>? appearances,
            ThumbnailService? thumbnails,
            ChoicePreviewService? choicePreviews,
            Services.IEditorPromptService? prompts,
            OutputLogService? log) => new()
        {
            Catalog = SwlorDoorBehaviorCatalog.Instance,
            ResolveDestination = resolveDestination,
            ResolveChoices = resolveChoices,
            Appearances = appearances ?? Array.Empty<DoorAppearanceChoice>(),
            AppearancePreviews = thumbnails == null ? null : new SwlorDoorAppearancePreviews(thumbnails),
            ChoicePreviews = choicePreviews,
            KeyItems = gameCodeIndex?.KeyItems,
            Variables = new SwlorVarTableSectionFactory(gameCodeIndex),
            Prompts = prompts == null ? null : new SwlorPalettePrompts(prompts),
            Log = log == null ? null : new SwlorPaletteLog(log),
        };

        protected override void OnPresentationChanged() => UpdatePreviewScene();

        private void UpdatePreviewScene()
        {
            if (IsDisposed)
                return;

            var preview = _resolveModel?.Invoke(Door) ?? default;
            PreviewScene = preview.Model == null && !preview.IsDoorTransition
                ? null
                : new AreaScene
                {
                    Tileset = string.Empty,
                    Width = 1,
                    Height = 1,
                    Tiles = Array.Empty<TilePlacement>(),
                    Instances = new[]
                    {
                        new InstanceMarker
                        {
                            Kind = InstanceMarkerKind.Door,
                            TemplateResRef = TemplateResRef,
                            Tag = DoorTag,
                            Position = new Vector3(
                                AreaSceneBuilder.TileSize / 2f,
                                AreaSceneBuilder.TileSize / 2f,
                                0f),
                            Orientation = new Vector2(1f, 0f),
                            Model = preview.Model,
                            IsDoorTransition = preview.IsDoorTransition
                        }
                    },
                    Diagnostics = new AreaSceneDiagnostics()
                };

            OnPropertyChanged(nameof(PreviewScene));
        }

        public override void Dispose()
        {
            if (IsDisposed)
                return;

            base.Dispose();
            _previewView?.Dispose();
            _previewView = null;
            PreviewScene = null;
        }
    }
}
