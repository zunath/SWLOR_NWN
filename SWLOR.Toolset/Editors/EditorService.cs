using System.Collections.Concurrent;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;
using GameItem = SWLOR.Game.Server.Service.Item;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// Opens blueprint editors as document tabs. One editor per file: requesting an already
    /// open blueprint activates its existing tab. Types without a schema yet log a notice
    /// instead of opening until a schema for that type is available.
    /// </summary>
    public sealed class EditorService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly LookupOptionProvider _lookups;
        private readonly Domain.GameData.Tlk.TlkService? _tlkService;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly OutputLogService _log;
        private readonly ToolsetDockFactory _factory;
        private readonly IEditorPromptService _prompts;
        private readonly IExternalLinkService? _externalLinks;
        private readonly TilesetCatalog? _tilesetCatalog;
        private readonly TileModelCache? _tileModelCache;
        private readonly ResourceIndex? _resourceIndex;
        private readonly PlaceableAppearanceService? _placeableAppearances;

        /// <summary>Backs the creature Appearance grid; null degrades it to the schema field alone.</summary>
        private readonly AppearanceService? _appearances;
        private readonly DoorTypeService? _doorTypes;
        private readonly PortraitService? _portraits;
        private readonly WaypointAppearanceService? _waypointAppearances;
        private readonly Domain.GameData.TwoDa.TwoDaService? _twoDaService;
        private Behaviors.ChoicePreviewService? _choicePreviews;

        /// <summary>
        /// The picker option sets, shared by every editor rather than rebuilt per tab. Each one is a
        /// module scan or a 2DA read, and opening a second door used to redo all of them.
        /// </summary>
        private readonly ConcurrentDictionary<string, Lazy<IReadOnlyList<BehaviorChoice>>> _choiceSets =
            new(StringComparer.Ordinal);

        /// <summary>
        /// The placeable behavior pickers' option sets. One provider for the session: the tag source
        /// alone offers five figures of options, and a per-editor copy multiplied both the scan and
        /// the memory by however many tabs were open.
        /// </summary>
        private Placeables.BehaviorValueSourceProvider? _behaviorValues;

        /// <summary>
        /// The creature appearance rows, projected once. appearance.2da is thousands of rows and
        /// every creature editor asks for the same list.
        /// </summary>
        private readonly object _creatureAppearanceOptionsGate = new();
        private IReadOnlyList<Appearance.AppearanceOption>? _creatureAppearanceOptions;

        /// <summary>Backs the placeable Appearance tab's model grid; null degrades it to an empty grid.</summary>
        private readonly PlaceableModelCatalog? _placeableModels;

        /// <summary>Raised while a pack, validation, or Build All is running; handed to script tabs.</summary>
        private readonly Services.ModuleMutationLock? _mutationLock;
        private readonly ThumbnailService? _thumbnails;
        private readonly PlaceableIndexService? _placeableIndexes;
        private readonly Placeables.VfxPreviewService? _vfxPreviews;

        /// <summary>Supplies the area editor its placement-ghost geometry; null degrades the ghost to a marker.</summary>
        private readonly Workspace.BlueprintPreviewRenderer? _previewRenderer;

        /// <summary>Shared engine-symbol database driving script completion; null disables it.</summary>
        private readonly Workspace.ScriptLanguageService? _scriptLanguage;

        /// <summary>Where script diagnostics land; null in a shell without the panel.</summary>
        private readonly Shell.Panels.ProblemsViewModel? _problems;

        /// <summary>Rebuilds a script's .ncs on save; null when no compiler is vendored.</summary>
        private readonly Services.ScriptCompileService? _compileService;

        /// <summary>
        /// Built in the background on first use, then invalidated when a scripted resource changes.
        /// Scanning all script slots is expensive; builders who never request usages still pay
        /// nothing at startup.
        /// </summary>
        private readonly ScriptUsageIndexCache _scriptUsageIndex;
        private readonly TileWalkmeshCache? _tileWalkmeshCache;
        private readonly Dictionary<string, BlueprintEditorViewModel> _openEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AreaEditorViewModel> _openAreaEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _openingAreaEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly AreaInstanceClipboard _areaInstanceClipboard = new();
        private readonly Dictionary<string, ObjectPlacement> _pendingAreaReveals =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly List<WeakReference<Sources.ObjectSourceSectionViewModel>> _objectSources = new();
        private readonly Dictionary<string, Triggers.TriggerDocumentViewModel> _openTriggerEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Waypoints.WaypointDocumentViewModel> _openWaypointEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _openingWaypointEditors = new(StringComparer.OrdinalIgnoreCase);
        private int _waypointCatalogGeneration;
        private readonly Dictionary<string, Doors.DoorDocumentViewModel> _openDoorEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Creatures.CreatureDocumentViewModel> _openCreatureEditors = new(StringComparer.OrdinalIgnoreCase);
        private int _creatureEquipmentChoicesGeneration;
        private readonly Dictionary<string, Creatures.CreatureEquipmentChoice> _creatureEquipmentDetails =
            new(StringComparer.OrdinalIgnoreCase);
        private Creatures.CreatureSoundSetPreviewResolver? _creatureSoundSetPreviews;
        private IReadOnlyList<Domain.Editors.Doors.DoorAppearanceChoice>? _doorAppearances;
        private readonly Dictionary<string, Items.ItemDocumentViewModel> _openItemEditors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Merchants.MerchantDocumentViewModel> _openMerchantEditors =
            new(StringComparer.OrdinalIgnoreCase);
        private Merchants.MerchantInstanceService? _merchantInstances;
        private IReadOnlyList<Merchants.MerchantItemDefinition>? _merchantItemCatalog;
        private Merchants.MerchantItemSearchIndex? _merchantItemSearchIndex;
        private readonly ConcurrentDictionary<string, Merchants.MerchantItemDefinition>
            _merchantItemSummaries = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Merchants.MerchantItemDefinition> _merchantItemDetails =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _merchantItemSearchEligibility =
            new(StringComparer.OrdinalIgnoreCase);
        private BaseItemRowService? _baseItemRowService;
        private BaseItemIconService? _baseItemIconService;
        private Domain.Editors.Items.ItemCostTableRanges? _itemCostTableRanges;
        private ItemObtainabilityIndex? _itemSources;

        /// <summary>The in-flight background index build, so concurrent callers share one scan.</summary>
        private Task? _itemSourcesBuild;

        private static readonly TimeSpan ItemSourcesRetryDelay = TimeSpan.FromMilliseconds(250);
        private const int MaximumAutomaticItemSourceFailureRetries = 1;
        private int _itemSourceFailureRetries;
        private bool _itemSourceRetryBlocked;

        /// <summary>The background scan implementation; injectable to make generation races testable.</summary>
        private readonly Func<Domain.Workspace.ModuleWorkspace, string?, ItemObtainabilityIndex>
            _itemSourcesBuilder;

        /// <summary>The placement lookup implementation; injectable to verify workspace races.</summary>
        private readonly Func<
            Domain.Workspace.ModuleWorkspace,
            ResourceType,
            string,
            Task<IReadOnlyList<ObjectPlacement>>> _objectPlacementsFinder;

        /// <summary>The area file parser; injectable to verify workspace replacement during a slow load.</summary>
        private readonly Func<string, string, string, AreaEditorDocumentLoad> _areaDocumentsLoader;

        /// <summary>
        /// Bumped every time <see cref="_itemSources"/> is invalidated (a module opens, or a store,
        /// item, creature, or placeable is saved). <see cref="BuildItemSourcesAsync"/> captures this at
        /// the start of a scan and compares again when the scan finishes; a mismatch means content
        /// changed mid-scan, so the just-finished result reflects stale, pre-save data and must not be
        /// published - a fresh build is queued instead. Read and written only from the UI thread (the
        /// events that bump it and the completion check that reads it all run there), so no lock is
        /// needed.
        /// </summary>
        private int _itemSourcesGeneration;

        /// <summary>
        /// Optional category sidecar used by the shared blueprint rename transaction. Without it,
        /// file and instance updates still work, but custom palette membership cannot be carried.
        /// </summary>
        private readonly CategoryService? _categories;
        private readonly BlueprintSaveCoordinator _blueprintSaves;
        private Items.ArmorDyeSwatchService? _armorDyeSwatches;
        private Items.ArmorPartCatalog? _armorPartModels;
        private readonly Dictionary<string, Sounds.SoundDocumentViewModel> _openSoundEditors = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<string>? _soundResources;
        private Services.SoundPreviewService? _soundPreviews;
        private readonly Workspace.ModuleCustomContentService? _moduleCustomContent;
        private Module.ModulePropertiesDocumentViewModel? _moduleProperties;

        // Keyed by path like the blueprint map rather than by resref like the area map: a script is
        // one file, so the path is its identity and there is no are/git/gic triplet to name.
        private readonly Dictionary<string, ScriptEditorViewModel> _openScriptEditors = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ConversationEditorViewModel> _openConversations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, NuiConversationEditorViewModel> _openNuiConversations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ConversationOpenIssueViewModel> _openConversationIssues = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Deep, independent snapshots of every open conversation editor's live document, keyed by
        /// resref. Dialogue search consults these before falling back to the saved file so unsaved
        /// edits are searchable.
        /// </summary>
        /// <remarks>
        /// Each value is a private copy, not the editor's live <see cref="DlgDocument"/>: a builder
        /// can add, remove, or reorder nodes and links in an open conversation while a background
        /// scan is walking that same document's <c>Entries</c>/<c>Replies</c> lists, which can throw
        /// or produce inconsistent hits. Round-tripping through <see cref="GffDocumentBase.ToBytes"/>
        /// and <see cref="DlgDocument.Parse"/> here, on the UI thread, gives the worker a document
        /// nothing else can mutate out from under it.
        /// </remarks>
        public IReadOnlyDictionary<string, DlgDocument> SnapshotOpenConversationDocuments()
        {
            var documents = new Dictionary<string, DlgDocument>(StringComparer.OrdinalIgnoreCase);
            foreach (var editor in _openConversations.Values)
                documents[editor.ResRef] = DlgDocument.Parse(editor.LiveDialog.ToBytes());
            return documents;
        }

        /// <summary>
        /// Independent snapshots of open graph-native conversations for the background text search.
        /// The worker never observes a graph while the editor is mutating its ordered collections.
        /// </summary>
        public IReadOnlyDictionary<string, SWLOR.Game.Server.Service.ConversationService.ConversationGraph>
            SnapshotOpenNuiConversationGraphs()
        {
            var graphs = new Dictionary<string, SWLOR.Game.Server.Service.ConversationService.ConversationGraph>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var editor in _openNuiConversations.Values)
                graphs[editor.ResRef] = editor.SnapshotGraph();
            return graphs;
        }

        /// <summary>
        /// Current text of every open script editor buffer, keyed by resref. A background script
        /// search consults this instead of reading the live <c>_openScriptEditors</c> dictionary and
        /// each editor's live buffer off-thread: a builder opening or closing a script tab while the
        /// scan is running mutates that dictionary concurrently, which can fault the search.
        /// </summary>
        /// <remarks>
        /// Copied out on the UI thread, before the scan starts. Script text is an immutable
        /// <see cref="string"/>, so once copied into this dictionary neither the dictionary nor the
        /// strings it holds can change under the worker; only the dictionary reference itself needs
        /// to be captured before <c>Task.Run</c>, not the underlying editors.
        /// </remarks>
        public IReadOnlyDictionary<string, string> SnapshotOpenScriptSources()
        {
            var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var editor in _openScriptEditors.Values)
                sources[editor.ResRef] = editor.TextBinding;
            return sources;
        }

        /// <summary>
        /// The conversation snippet registry, read once from the game code. Built lazily because
        /// reflecting over every <c>ISnippetListDefinition</c> is wasted work in a session that
        /// never opens a dialog.
        /// </summary>
        private SnippetCatalog? _snippets;

        public EditorService(
            WorkspaceContext workspaceContext,
            LookupOptionProvider lookups,
            OutputLogService log,
            ToolsetDockFactory factory,
            IEditorPromptService prompts,
            IGameCodeIndex? gameCodeIndex = null,
            TilesetCatalog? tilesetCatalog = null,
            TileModelCache? tileModelCache = null,
            ResourceIndex? resourceIndex = null,
            PlaceableAppearanceService? placeableAppearances = null,
            DoorTypeService? doorTypes = null,
            TileWalkmeshCache? tileWalkmeshCache = null,
            Domain.GameData.Tlk.TlkService? tlkService = null,
            WaypointAppearanceService? waypointAppearances = null,
            Workspace.BlueprintPreviewRenderer? previewRenderer = null,
            Workspace.ScriptLanguageService? scriptLanguage = null,
            Shell.Panels.ProblemsViewModel? problems = null,
            Services.ScriptCompileService? compileService = null,
            PlaceableModelCatalog? placeableModels = null,
            ThumbnailService? thumbnails = null,
            PlaceableIndexService? placeableIndexes = null,
            Domain.GameData.TwoDa.TwoDaService? twoDaService = null,
            Placeables.VfxPreviewService? vfxPreviews = null,
            PortraitService? portraits = null,
            AppearanceService? appearances = null,
            Services.ModuleMutationLock? mutationLock = null,
            CategoryService? categories = null,
            Func<Domain.Workspace.ModuleWorkspace, string?, ItemObtainabilityIndex>?
                itemSourcesBuilder = null,
            Workspace.ModuleCustomContentService? moduleCustomContent = null,
            IExternalLinkService? externalLinks = null,
            Func<
                Domain.Workspace.ModuleWorkspace,
                ResourceType,
                string,
                Task<IReadOnlyList<ObjectPlacement>>>? objectPlacementsFinder = null,
            Func<string, string, string, AreaEditorDocumentLoad>? areaDocumentsLoader = null)
        {
            _moduleCustomContent = moduleCustomContent;
            _externalLinks = externalLinks;
            _categories = categories;
            _blueprintSaves = new BlueprintSaveCoordinator(
                log,
                categories,
                areaResRef =>
                    _openAreaEditors.TryGetValue(areaResRef, out var area) &&
                    area.HasUnsavedInstanceChanges,
                areaResRef =>
                {
                    if (_openAreaEditors.TryGetValue(areaResRef, out var area))
                        area.ReloadInstancesAfterBlueprintSave();
                },
                FindUnsavedBlueprintReferences);
            _itemSourcesBuilder = itemSourcesBuilder ??
                                  ((workspace, gameSourceRoot) =>
                                      ItemObtainabilityIndex.Build(workspace, gameSourceRoot));
            _objectPlacementsFinder = objectPlacementsFinder ??
                                      ((workspace, type, resRef) =>
                                          workspace.PlacementIndex.FindAsync(type, resRef));
            _areaDocumentsLoader = areaDocumentsLoader ?? AreaEditorDocumentLoad.Load;
            _mutationLock = mutationLock;
            _placeableModels = placeableModels;
            _thumbnails = thumbnails;
            _placeableIndexes = placeableIndexes;
            _vfxPreviews = vfxPreviews;
            _appearances = appearances;
            _workspaceContext = workspaceContext;
            _lookups = lookups;
            _log = log;
            _factory = factory;
            _prompts = prompts;
            _gameCodeIndex = gameCodeIndex;
            _tilesetCatalog = tilesetCatalog;
            _tileModelCache = tileModelCache;
            _resourceIndex = resourceIndex;
            _placeableAppearances = placeableAppearances;
            _doorTypes = doorTypes;
            _portraits = portraits;
            _tileWalkmeshCache = tileWalkmeshCache;
            _tlkService = tlkService;
            _waypointAppearances = waypointAppearances;
            _previewRenderer = previewRenderer;
            _twoDaService = twoDaService;
            _scriptLanguage = scriptLanguage;
            _problems = problems;
            _compileService = compileService;
            _scriptUsageIndex = new ScriptUsageIndexCache(() =>
            {
                var workspace = _workspaceContext.Workspace;
                if (workspace == null)
                    return (Domain.Script.ScriptUsageIndex?)null;

                try
                {
                    return Domain.Script.ScriptUsageIndex.Build(workspace);
                }
                catch (Exception ex)
                {
                    _log.AppendLine($"Could not index script usages: {ex.Message}");
                    return null;
                }
            });
            _workspaceContext.ScriptUsagesInvalidated += _scriptUsageIndex.Invalidate;
            _workspaceContext.TagIndexInvalidated += OnTagIndexInvalidated;
            _workspaceContext.PlacementIndexInvalidated += ReloadPlacementSources;
            _workspaceContext.CatalogBuildCompleted += RefreshObjectSourceAreaNames;

            // Opening another module invalidates every module-derived picker; saving a blueprint
            // invalidates only the ones built out of the module's own content.
            _workspaceContext.WorkspaceOpened += () =>
            {
                _choiceSets.Clear();
                _doorAppearances = null;
                _itemSources = null;
                _itemSourceFailureRetries = 0;
                _itemSourceRetryBlocked = false;
                _merchantItemCatalog = null;
                _merchantItemSearchIndex = null;
                _merchantItemSummaries.Clear();
                _merchantItemDetails.Clear();
                _merchantItemSearchEligibility.Clear();
                InvalidateCreatureEquipmentChoices();
                _itemSourcesGeneration++;
                _behaviorValues?.InvalidateModuleSources();
                OnTagIndexInvalidated();
                ReloadPlacementSources();

                // Pay the obtainability scan's cost here, in the background, rather than on
                // whichever item editor happens to open first.
                _ = WarmItemSourcesAsync();
            };
            _workspaceContext.CatalogEntryRefreshed += (type, refreshedResRef) =>
            {
                _behaviorValues?.InvalidateModuleSources();
                if (type == ResourceType.Area)
                    RefreshObjectSourceAreaNames();

                // A saved store, item, creature, or placeable can change where items are
                // obtainable; the index is cheap to rebuild, so it is dropped rather than patched.
                if (type is ResourceType.Utm or ResourceType.Uti or ResourceType.Utc or ResourceType.Utp)
                {
                    _itemSources = null;
                    _itemSourceFailureRetries = 0;
                    _itemSourceRetryBlocked = false;
                    _itemSourcesGeneration++;
                    _ = WarmItemSourcesAsync();
                }

                if (type == ResourceType.Uti)
                {
                    _merchantItemCatalog = null;
                    _merchantItemSearchIndex = null;
                    _merchantItemSummaries.TryRemove(refreshedResRef, out _);
                    _merchantItemDetails.TryRemove(refreshedResRef, out _);
                    _merchantItemSearchEligibility.TryRemove(refreshedResRef, out _);
                    InvalidateCreatureEquipmentChoices(refreshedResRef);
                    foreach (var merchant in _openMerchantEditors.Values)
                        merchant.Editor.RefreshItemCatalog();
                }
            };
            _workspaceContext.PaletteChoicesInvalidated += InvalidatePaletteChoices;
        }

        private void OnTagIndexInvalidated()
        {
            var generation = ++_waypointCatalogGeneration;
            var workspace = _workspaceContext.Workspace;
            if (workspace == null ||
                (_openWaypointEditors.Count == 0 && _openAreaEditors.Count == 0))
            {
                return;
            }

            _ = RefreshWaypointCatalogsAsync(workspace, generation);
        }

        private async Task RefreshWaypointCatalogsAsync(
            ModuleWorkspace workspace,
            int generation)
        {
            try
            {
                var transitionDestinationTags =
                    await workspace.TagIndex.GetTransitionDestinationTagsAsync().ConfigureAwait(true);
                if (generation != _waypointCatalogGeneration ||
                    !ReferenceEquals(workspace, _workspaceContext.Workspace))
                {
                    return;
                }

                var catalog = new Domain.Editors.Waypoints.WaypointBehaviorCatalog(
                    _gameCodeIndex,
                    transitionDestinationTags);
                foreach (var editor in _openWaypointEditors.Values)
                    editor.RefreshCatalog(catalog);
                foreach (var editor in _openAreaEditors.Values)
                    editor.RefreshWaypointCatalog(catalog);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not refresh open waypoint behavior catalogs: {ex.Message}");
            }
        }

        private async Task<IReadOnlyCollection<string>?> GetCurrentTransitionDestinationTagsAsync(
            ModuleWorkspace workspace)
        {
            while (ReferenceEquals(workspace, _workspaceContext.Workspace))
            {
                var generation = _waypointCatalogGeneration;
                var tags =
                    await workspace.TagIndex.GetTransitionDestinationTagsAsync().ConfigureAwait(true);
                if (generation == _waypointCatalogGeneration)
                    return tags;
            }

            return null;
        }

        /// <summary>Opens Module Properties as the single document tab for module.ifo.</summary>
        public void OpenModuleProperties(Module.ModulePropertiesActions? actions = null)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
            {
                _log.AppendLine("Open a module before using Module Properties.");
                return;
            }

            if (_moduleProperties != null)
            {
                _factory.ActivateDocument(_moduleProperties);
                return;
            }

            var path = Path.Combine(workspace.ModuleRoot, "ifo", "module.ifo.json");
            if (!File.Exists(path))
            {
                _log.AppendLine($"Module properties file not found: {path}");
                return;
            }

            try
            {
                var document = new Module.ModulePropertiesDocumentViewModel(
                    path,
                    workspace.ModuleRoot,
                    workspace,
                    _log,
                    _prompts,
                    _gameCodeIndex,
                    _moduleCustomContent,
                    script => TryOpenEditor(ResourceType.Nss, script),
                    actions);
                document.Closed += _ => _moduleProperties = null;
                document.CloseRequested += _ => _factory.CloseDocument(document);
                _moduleProperties = document;
                _factory.OpenDocument(document);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open Module Properties: {ex.Message}");
            }
        }

        /// <summary>Refreshes open resource-backed content after the module HAK stack changes.</summary>
        public void ReloadOpenGameResources()
        {
            _lookups.Invalidate();
            _choiceSets.Clear();
            lock (_creatureAppearanceOptionsGate)
                _creatureAppearanceOptions = null;
            InvalidateCreatureEquipmentChoices();
            _creatureSoundSetPreviews = null;
            _soundResources = null;
            _doorAppearances = null;
            _merchantItemCatalog = null;
            _merchantItemSearchIndex = null;
            _merchantItemSummaries.Clear();
            _merchantItemDetails.Clear();
            _merchantItemSearchEligibility.Clear();
            _baseItemRowService = null;
            _baseItemIconService = null;
            _itemCostTableRanges = null;
            _armorDyeSwatches = null;
            _armorPartModels = null;

            foreach (var editor in _openAreaEditors.Values)
                editor.ReloadGameResources();
            foreach (var editor in _openEditors.Values)
                editor.PlaceableSections?.Appearance.ReloadGameResources();
            foreach (var editor in _openDoorEditors.Values)
                editor.Editor.ReloadGameResources();
            foreach (var editor in _openCreatureEditors.Values)
                editor.Editor.ReloadGameResources();
            foreach (var editor in _openItemEditors.Values)
                editor.Editor.ReloadGameResources(ItemCostTables());
            foreach (var editor in _openMerchantEditors.Values)
                editor.Editor.RefreshItemCatalog();
        }

        /// <summary>
        /// Invalidates faction choice caches after the modal faction editor saves. Any open editor
        /// which owns a faction picker is clean (the shell saves all documents before opening the
        /// modal workflow), so closing it prevents a stale list or a pre-remap numeric id from being
        /// written back over the grouped faction transaction.
        /// </summary>
        public void RefreshAfterFactionSave(IReadOnlyCollection<string> changedPaths)
        {
            if (changedPaths.Count == 0)
                return;

            _lookups.Invalidate();
            _choiceSets.Clear();

            foreach (var path in changedPaths)
            {
                var extension = Path.GetFileName(Path.GetDirectoryName(path));
                if (string.IsNullOrWhiteSpace(extension) ||
                    !ResourceTypeExtensions.TryFromExtension(extension, out var type) ||
                    type == ResourceType.Area)
                {
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(path);
                var resRef = Path.GetFileNameWithoutExtension(fileName);
                if (!string.IsNullOrWhiteSpace(resRef))
                    _workspaceContext.RefreshCatalogEntry(type, resRef);
            }

            var closed = 0;
            foreach (var editor in _openEditors.Values
                         .Where(editor => editor.BlueprintType is
                             ResourceType.Utc or ResourceType.Utp or ResourceType.Utd or
                             ResourceType.Utt)
                         .ToList())
            {
                _factory.CloseDocument(editor);
                closed++;
            }

            foreach (var editor in _openCreatureEditors.Values.ToList())
            {
                _factory.CloseDocument(editor);
                closed++;
            }

            foreach (var editor in _openDoorEditors.Values.ToList())
            {
                _factory.CloseDocument(editor);
                closed++;
            }

            foreach (var editor in _openTriggerEditors.Values.ToList())
            {
                _factory.CloseDocument(editor);
                closed++;
            }

            if (changedPaths.Any(path =>
                    string.Equals(
                        Path.GetFileName(Path.GetDirectoryName(path)),
                        "git",
                        StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var editor in _openAreaEditors.Values.ToList())
                {
                    _factory.CloseDocument(editor);
                    closed++;
                }
            }

            if (closed > 0)
            {
                _log.AppendLine(
                    $"Closed {closed} clean faction-aware editor tab" +
                    $"{(closed == 1 ? string.Empty : "s")} so it reopens with the saved faction table.");
            }
        }

        /// <summary>
        /// Focuses the script named by a Problems row and puts the caret on the reported line.
        /// </summary>
        public void NavigateToScriptLine(string resRef, int line)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            TryOpenEditor(ResourceType.Nss, resRef);

            if (_openScriptEditors.TryGetValue(workspace.GetResourcePath(ResourceType.Nss, resRef), out var editor))
                Avalonia.Threading.Dispatcher.UIThread.Post(() => editor.GoToLineRequested?.Invoke(line));
        }

        /// <summary>Compiles a script by resref, for callers outside an open tab (the explorer).</summary>
        /// <remarks>
        /// An open tab with unsaved edits is routed through its own compile path, which saves first.
        /// The compiler reads the file from disk, so without this the explorer's Compile produced
        /// bytecode for source the builder had already replaced on screen - and for a dirty include
        /// it rebuilt every dependent entry point from the old version, which is a far larger wrong
        /// answer than one stale script.
        /// </remarks>
        public async Task CompileScriptAsync(string resRef)
        {
            if (_compileService == null || !_compileService.IsAvailable)
            {
                _log.AppendLine("Cannot compile: nwn_script_comp.exe is missing from tools/SWLOR.CLI.");
                return;
            }

            var workspace = _workspaceContext.Workspace;
            if (workspace != null &&
                _openScriptEditors.TryGetValue(
                    workspace.GetResourcePath(ResourceType.Nss, resRef), out var open))
            {
                await open.CompileCommand.ExecuteAsync(null).ConfigureAwait(true);
                return;
            }

            var outcome = await _compileService.CompileAsync(resRef).ConfigureAwait(true);
            if (!outcome.Succeeded)
                _factory.ShowProblems();
        }

        /// <summary>Backs the script slots on one editor, describing its owner for the picker title.</summary>
        private ScriptSlotHost CreateScriptSlotHost(string ownerDescription) =>
            new(
                _workspaceContext,
                () => this,
                _log,
                ownerDescription,
                _scriptUsageIndex.GetAsync,
                CreateNewScriptAsync);

        /// <summary>
        /// Creates an NSS resource for the script picker's "New script..." action and returns the
        /// resref the picker should place in the slot.
        /// </summary>
        private async Task<string?> CreateNewScriptAsync()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            var name = await _prompts.PromptForTextAsync(
                "New script",
                "Name for the new script. Its resref is derived from this.",
                string.Empty,
                "Create").ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var resRef = ModuleResourceTemplateFactory.ToResRef(name);
            if (resRef.Length == 0)
            {
                _log.AppendLine("Could not create script: that name has no letters or digits.");
                return null;
            }

            var path = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (File.Exists(path))
            {
                _log.AppendLine($"Could not create script: '{resRef}' already exists.");
                return null;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                SaveService.WriteNewAtomic(
                    path,
                    ModuleResourceTemplateFactory.CreateFileContent(ResourceType.Nss, resRef, name.Trim()));
                _workspaceContext.RefreshCatalogEntry(ResourceType.Nss, resRef);
                _log.AppendLine($"Created script '{resRef}' from the script-slot picker.");
                return resRef;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not create script '{resRef}': {ex.Message}");
                return null;
            }
        }

        /// <summary>Opens a NWScript source file as a text editor tab, or activates its open tab.</summary>
        private void OpenScriptEditor(ModuleWorkspace workspace, string resRef)
        {
            var filePath = workspace.GetResourcePath(ResourceType.Nss, resRef);
            if (!File.Exists(filePath))
            {
                _log.AppendLine($"File not found: {filePath}");
                return;
            }

            if (_openScriptEditors.TryGetValue(filePath, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            try
            {
                var editor = new ScriptEditorViewModel(
                    filePath,
                    resRef,
                    _log,
                    _prompts,
                    _scriptLanguage,
                    new Shell.Panels.ScriptSearchViewModel(
                        Path.Combine(workspace.ModuleRoot, "nss"),
                        NavigateToScriptLine,
                        SnapshotOpenScriptSources))
                {
                    // The tab's own Compile button writes a .ncs, so it follows the same module-wide
                    // lock that the Build menu does.
                    MutationLock = _mutationLock
                };
                editor.Closed += _ =>
                {
                    _openScriptEditors.Remove(filePath);
                    // A closed tab's findings must not linger in Problems - they refer to a buffer
                    // nobody can see or navigate to any more.
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => _problems?.Clear(resRef));
                };
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CompileOnSave = _compileService != null && _compileService.IsAvailable
                    ? name => CompileOnSaveAsync(workspace, editor, name)
                    : null;

                // Compile belongs to the document, not to a module-wide menu. The tab owns the button
                // and Ctrl+B; this just gives it the service.
                editor.CompileRequested = _compileService != null && _compileService.IsAvailable
                    ? async name => (await _compileService.CompileAsync(name).ConfigureAwait(true)).Succeeded
                    : null;
                editor.ShowProblemsRequested = () => _factory.ShowProblems();
                editor.FindUsages = async name =>
                {
                    var index = await _scriptUsageIndex.GetAsync().ConfigureAwait(true);
                    return index?.UsagesOf(name) ?? Array.Empty<Domain.Script.ScriptUsage>();
                };
                editor.DiagnosticsChanged += diagnostics =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        _problems?.SetDiagnostics(resRef, Domain.Script.Syntax.ScriptDiagnosticSource.Editor, diagnostics));

                // Go-to-definition across an include opens that script and lands on the symbol.
                editor.OpenIncludeRequested += (includeResRef, offset) =>
                {
                    OpenScriptEditor(workspace, includeResRef);
                    if (_openScriptEditors.TryGetValue(
                            workspace.GetResourcePath(ResourceType.Nss, includeResRef), out var opened))
                    {
                        // Posted so the tab's view has attached and wired GoToOffsetRequested first.
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            opened.GoToOffsetRequested?.Invoke(offset));
                    }
                };
                _openScriptEditors[filePath] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not open {filePath}: {ex.Message}");
            }
        }

        public void TryOpenEditor(ResourceType type, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            if (type == ResourceType.Area)
            {
                OpenAreaEditor(workspace, resRef);
                return;
            }

            if (type == ResourceType.Nss)
            {
                OpenScriptEditor(workspace, resRef);
                return;
            }

            if (type == ResourceType.Dlg)
            {
                OpenConversationEditor(workspace, resRef);
                return;
            }

            var schema = GetSchema(type);
            if (schema == null)
            {
                _log.AppendLine($"No editor available yet for {type.DisplayName()}.");
                return;
            }

            var filePath = workspace.GetResourcePath(type, resRef);
            if (!File.Exists(filePath))
            {
                _log.AppendLine($"File not found: {filePath}");
                return;
            }

            if (_openEditors.TryGetValue(filePath, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            if (_openTriggerEditors.TryGetValue(filePath, out var existingTrigger))
            {
                _factory.ActivateDocument(existingTrigger);
                return;
            }

            if (_openWaypointEditors.TryGetValue(filePath, out var existingWaypoint))
            {
                _factory.ActivateDocument(existingWaypoint);
                return;
            }

            if (_openDoorEditors.TryGetValue(filePath, out var existingDoor))
            {
                _factory.ActivateDocument(existingDoor);
                return;
            }

            if (_openCreatureEditors.TryGetValue(filePath, out var existingCreature))
            {
                _factory.ActivateDocument(existingCreature);
                return;
            }

            if (_openSoundEditors.TryGetValue(filePath, out var existingSound))
            {
                _factory.ActivateDocument(existingSound);
                return;
            }

            if (_openItemEditors.TryGetValue(filePath, out var existingItem))
            {
                _factory.ActivateDocument(existingItem);
                return;
            }

            if (_openMerchantEditors.TryGetValue(filePath, out var existingMerchant))
            {
                _factory.ActivateDocument(existingMerchant);
                return;
            }

            try
            {
                if (!CanRepresentEveryValue(filePath, resRef, schema))
                    return;

                // Doors use the behavior editor for both blueprints and area placements.
                if (type == ResourceType.Utd)
                {
                    OpenDoorEditor(filePath, resRef);
                    return;
                }

                if (type == ResourceType.Utc)
                {
                    OpenCreatureEditor(filePath, resRef);
                    return;
                }

                // Sounds use the behavior editor and its dedicated ordered Sounds-list control.
                if (type == ResourceType.Uts)
                {
                    OpenSoundEditor(filePath, resRef);
                    return;
                }

                // Triggers get the behavior editor rather than the generic schema form: what a
                // trigger is for drives which fields it even has.
                if (type == ResourceType.Utt)
                {
                    OpenTriggerEditor(filePath, resRef);
                    return;
                }

                if (type == ResourceType.Utw)
                {
                    OpenWaypointEditor(filePath, resRef);
                    return;
                }

                // Items get the behavior editor: the base type chosen on Basic decides which
                // roles and stat groups the item even has, which a flat schema form cannot say.
                if (type == ResourceType.Uti)
                {
                    OpenItemEditor(filePath, resRef);
                    return;
                }

                if (type == ResourceType.Utm)
                {
                    OpenMerchantEditor(filePath, resRef);
                    return;
                }

                var editor = new BlueprintEditorViewModel(
                    filePath, resRef, type, schema, _lookups, _gameCodeIndex, _log, _prompts,
                    // So a localized field that carries a strref but no language-0 override can show what
                    // that strref says, instead of a blank the builder reads as missing data.
                    _tlkService == null ? null : _tlkService.GetString,
                    CreateScriptSlotHost($"{type.SingularDisplayName()} '{resRef}'"),
                    type == ResourceType.Utp ? CreatePlaceableSections : null,
                    () => _workspaceContext.Workspace,
                    type == ResourceType.Utc ? CreateCreatureAppearanceGallery : null,
                    _blueprintSaves,
                    CreateObjectSource(type, resRef));
                editor.Closed += closed => _openEditors.Remove(closed.FilePath);
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CatalogEntryChanged += () =>
                    _workspaceContext.RefreshCatalogEntry(editor.BlueprintType, editor.ResRef);
                editor.Renamed += (renamed, oldResRef, oldPath) =>
                    OnBlueprintRenamed(
                        _openEditors,
                        renamed,
                        renamed.BlueprintType,
                        oldResRef,
                        oldPath,
                        renamed.FilePath,
                        renamed.ResRef);
                _openEditors[filePath] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open editor for {resRef}: {ex.Message}");
            }
        }

        /// <summary>
        /// Aurora-style Edit Copy for an area instance: create a new module blueprint from its source,
        /// retain the source category when possible, and open the independent copy.
        /// </summary>
        private string? TryEditCopyAndOpenBlueprint(ResourceType type, string sourceResRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || !ModuleWorkspace.BlueprintTypes.Contains(type))
                return null;

            try
            {
                var sourcePath = workspace.GetResourcePath(type, sourceResRef);
                var isCustomSource = File.Exists(sourcePath);
                var source = isCustomSource
                    ? workspace.LoadBlueprint(type, sourceResRef)
                    : workspace.LoadIndexedBlueprint(type, sourceResRef);
                var copyResRef = BlueprintCopyFactory.NextResRef(
                    workspace,
                    type,
                    sourceResRef);
                var copyPath = workspace.GetResourcePath(type, copyResRef);
                var content = BlueprintCopyFactory.CreateFileContent(type, source.Document, copyResRef);

                Directory.CreateDirectory(Path.GetDirectoryName(copyPath)!);
                SaveService.WriteNewAtomic(copyPath, content);
                _workspaceContext.RefreshCatalogEntry(type, copyResRef);

                var categoryNotificationRaised = false;
                if (_categories != null)
                {
                    var sourceSection = isCustomSource
                        ? _categories.Section(type)
                        : _categories.StandardSection(type);
                    var sourceFolder = sourceSection?.FoldersContaining(sourceResRef).FirstOrDefault();
                    var categoryPath = sourceFolder == null || sourceSection == null
                        ? Array.Empty<string>()
                        : sourceSection.PathTo(sourceFolder).ToArray();

                    if (categoryPath.Length > 0 && _categories.Section(type) is { } customSection)
                    {
                        var targetFolder = EnsureBlueprintCopyFolder(customSection, categoryPath);
                        targetFolder.AddMember(copyResRef);
                        var save = _categories.SaveChanges();
                        categoryNotificationRaised = save.Saved;
                        if (!save.Saved)
                        {
                            _log.AppendLine(
                                $"Copied {type.Extension()} blueprint '{sourceResRef}' to '{copyResRef}', " +
                                $"but could not file it under '{categoryPath[^1]}': {save.Problem}");
                        }
                    }

                    // A successful category save raises Changed itself. An unfiled copy, or a failed
                    // category save restored to Unsorted, still needs that notification so an open
                    // palette re-enumerates the newly created module resource immediately.
                    if (!categoryNotificationRaised)
                        _categories.NotifyChanged();
                }

                _log.AppendLine(
                    $"Copied {type.Extension()} blueprint '{sourceResRef}' to '{copyResRef}' ({copyPath}).");
                TryOpenEditor(type, copyResRef);
                return copyResRef;
            }
            catch (Exception ex)
            {
                _log.AppendLine(
                    $"Edit Copy failed for {type.Extension()} blueprint '{sourceResRef}': {ex.Message}");
                return null;
            }
        }

        private static CategoryFolder EnsureBlueprintCopyFolder(
            CategorySection section,
            IReadOnlyList<string> path)
        {
            var current = section.Folders.FirstOrDefault(folder =>
                              string.Equals(folder.Name, path[0], StringComparison.OrdinalIgnoreCase))
                          ?? section.AddFolder(path[0]);
            for (var index = 1; index < path.Count; index++)
            {
                var segment = path[index];
                current = current.Children.FirstOrDefault(child =>
                              string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase))
                          ?? current.AddChild(segment);
            }

            return current;
        }

        private Sources.ObjectSourceSectionViewModel CreateObjectSource(
            ResourceType type,
            string resRef)
        {
            var source = new Sources.ObjectSourceSectionViewModel(
                type,
                resRef,
                FindObjectPlacementsAsync,
                ResolveAreaName,
                GoToObjectPlacement,
                _workspaceContext.InvalidatePlacementIndex,
                _log);
            _objectSources.Add(new WeakReference<Sources.ObjectSourceSectionViewModel>(source));
            return source;
        }

        private void ReloadPlacementSources()
        {
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(ReloadPlacementSources);
                return;
            }

            VisitObjectSources(source => source.Reload());
            foreach (var merchant in _openMerchantEditors.Values)
                merchant.Editor.InvalidatePlacedInstances();
        }

        private void RefreshObjectSourceAreaNames() =>
            VisitObjectSources(source => source.RefreshAreaNames());

        private void VisitObjectSources(Action<Sources.ObjectSourceSectionViewModel> visit)
        {
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => VisitObjectSources(visit));
                return;
            }

            for (var index = _objectSources.Count - 1; index >= 0; index--)
            {
                if (_objectSources[index].TryGetTarget(out var source))
                    visit(source);
                else
                    _objectSources.RemoveAt(index);
            }
        }

        private async Task<IReadOnlyList<ObjectPlacement>> FindObjectPlacementsAsync(
            ResourceType type,
            string resRef)
        {
            while (true)
            {
                var workspace = _workspaceContext.Workspace;
                if (workspace == null)
                    throw new InvalidOperationException(
                        "Cannot scan object placements because no module workspace is open.");

                try
                {
                    var placements = await _objectPlacementsFinder(workspace, type, resRef)
                        .ConfigureAwait(true);
                    if (ReferenceEquals(workspace, _workspaceContext.Workspace))
                        return placements;
                }
                catch when (!ReferenceEquals(workspace, _workspaceContext.Workspace))
                {
                    // The result belongs to an obsolete workspace generation. Retry below against
                    // the replacement rather than surfacing an error from a module that is gone.
                }

                _log.AppendLine(
                    $"Retrying placement scan for '{resRef}' because the module workspace " +
                    "changed during the scan.");
            }
        }

        private string ResolveAreaName(string areaResRef)
        {
            if (_workspaceContext.Catalog?.TryGetEntry(
                    ResourceType.Area, areaResRef, out var entry) == true &&
                !string.IsNullOrWhiteSpace(entry!.Name))
                return entry.Name!;

            return areaResRef;
        }

        private void GoToObjectPlacement(ObjectPlacement placement)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
            {
                _log.AppendLine(
                    $"Cannot navigate to '{placement.BlueprintResRef}' in '{placement.AreaResRef}': " +
                    "no module workspace is open.");
                return;
            }

            _pendingAreaReveals[placement.AreaResRef] = placement;
            OpenAreaEditor(workspace, placement.AreaResRef);
        }

        private void DispatchPendingAreaReveal(AreaEditorViewModel editor)
        {
            if (!_pendingAreaReveals.Remove(editor.AreaResRef, out var placement))
                return;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _factory.ActivateDocument(editor);
                _factory.ShowAreaContents();
                editor.RevealPlacement(placement);
            });
        }

        private async Task<bool> CompileOnSaveAsync(
            ModuleWorkspace workspace,
            ScriptEditorViewModel editor,
            string resRef)
        {
            if (_compileService == null)
                return false;

            var outcome = await _compileService.CompileAsync(resRef).ConfigureAwait(false);

            // Saving an include compile-checks it and rebuilds every transitive entry point, inside
            // CompileAsync. This used to then offer the builder a second, identical build - which
            // for a widely included header is a full compilation pass repeated for nothing, and
            // which read as "your dependents are still stale" when they were not. Said, not offered.
            if (outcome.RebuiltDependents > 0)
            {
                var count = outcome.RebuiltDependents;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    editor.ReportDependentRebuild(count));
            }

            return outcome.Succeeded;
        }

        /// <summary>
        /// Builds the placeable's Appearance and Behavior tabs. The module-wide scans they validate
        /// against are kicked off here rather than at startup, so a session that never opens a
        /// placeable never pays for them.
        /// </summary>
        private Placeables.PlaceableEditorSections? CreatePlaceableSections(
            EditorFieldContext context,
            Func<string, Action, bool> runEdit,
            IScriptSlotHost? scriptSlotHost,
            Func<string?, IReadOnlyList<string>> resourceChoices)
        {
            // No 2DA layer means no model grid, so the placeable opens with the plain tabs rather
            // than an Appearance tab that could only ever be empty.
            if (_placeableModels == null)
                return null;

            _placeableIndexes?.EnsureBuilt();

            var values = _behaviorValues ??= new Placeables.BehaviorValueSourceProvider(
                _gameCodeIndex,
                () => _placeableIndexes?.Tags,
                BlueprintChoices,
                _thumbnails,
                _vfxPreviews);

            var appearance = new Placeables.AppearanceSectionViewModel(
                context,
                _placeableModels,
                _thumbnails,
                () => _placeableIndexes?.Usage ?? Domain.Workspace.PlaceableAppearanceUsageIndex.Empty,
                runEdit,
                _resourceIndex,
                // The same resource/cache layer the area viewport builds through, with the preview's
                // transform tracks and emitter metadata added in its separate one-model cache.
                modelResRef => _tileModelCache?.GetOrBuildPlaceablePreview(modelResRef));

            var behavior = new Placeables.PlaceableBehaviorSectionViewModel(
                context, values, _prompts, runEdit, scriptSlotHost, resourceChoices);

            // The scan kicked off just above finishes after these fields are built, so the first
            // placeable opened in a session built every tag field against an empty list - which is
            // what makes a choice field degrade to plain free text with no suggestions and no
            // resolution check. Nothing put the real options back, so a Teleporter destination
            // stayed bare until the tab was closed and reopened.
            if (_placeableIndexes != null)
            {
                var indexes = _placeableIndexes;
                void OnIndexUpdated()
                {
                    appearance.RefreshUsage();
                    values.InvalidateModuleSources();
                    behavior.RefreshChoiceSources();
                }

                indexes.Updated += OnIndexUpdated;
                behavior.Detach = () => indexes.Updated -= OnIndexUpdated;
            }

            return new Placeables.PlaceableEditorSections(appearance, behavior);
        }

        /// <summary>
        /// The creature editor's appearance grid. Null without the 2DA layer, which leaves the
        /// schema's own appearance field as the only way to set it - the same degradation every
        /// other game-data-backed control makes.
        /// </summary>
        private Appearance.AppearanceGallerySectionViewModel? CreateCreatureAppearanceGallery(
            EditorFieldContext context,
            Func<string, Action, bool> runEdit)
        {
            if (_appearances == null)
                return null;

            return new Appearance.AppearanceGallerySectionViewModel(
                CreatureAppearanceOptions(),
                _thumbnails,
                () => (context.Document.Root.GetOrNull("Appearance_Type")?.GetInteger() ?? 0)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture),
                option => runEdit(
                    $"Change appearance to {option.Caption}",
                    () => WriteCreatureAppearance(context, option)),
                noun: "appearance");
        }

        private static void WriteCreatureAppearance(
            EditorFieldContext context,
            Appearance.AppearanceOption option)
        {
            var id = option.CreatureAppearanceId ?? 0;
            var field = context.Document.Root.GetOrNull("Appearance_Type");
            if (field == null)
            {
                var raw = System.Text.Encoding.ASCII.GetBytes(
                    id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                context.Document.Root.Add(
                    "Appearance_Type",
                    Domain.Gff.JsonGffField.CreateScalar(Domain.Gff.GffFieldType.Word, raw));
                return;
            }

            field.SetInteger(id);
        }

        private IReadOnlyList<CatalogEntry> BlueprintChoices(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<CatalogEntry>();

            var catalog = _workspaceContext.Catalog;
            var choices = new List<CatalogEntry>();
            foreach (var resRef in workspace.EnumerateResRefs(type))
            {
                // The catalog is keyed by type and resref, so this is a lookup rather than a scan of
                // every indexed blueprint per requested type.
                choices.Add(catalog != null && catalog.TryGetEntry(type, resRef, out var entry)
                    ? entry
                    : new CatalogEntry(
                        type, resRef, null, null, workspace.GetResourcePath(type, resRef)));
            }

            return choices;
        }

        /// <summary>
        /// Whether <paramref name="resRef"/> is open in an editor right now.
        /// </summary>
        /// <remarks>
        /// Deleting a file out from under an open editor leaves that editor holding a live
        /// DocumentSession: the next save sees the missing file as an external change, and Overwrite
        /// recreates the blueprint that was supposedly deleted while Reload fails outright. Callers
        /// check this and refuse instead.
        /// </remarks>
        public bool IsOpen(ResourceType type, string resRef)
        {
            if (type == ResourceType.Area)
                return _openAreaEditors.ContainsKey(resRef);

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return false;

            var path = workspace.GetResourcePath(type, resRef);
            return _openEditors.ContainsKey(path)
                   || _openTriggerEditors.ContainsKey(path)
                   || _openWaypointEditors.ContainsKey(path)
                   || _openDoorEditors.ContainsKey(path)
                   || _openCreatureEditors.ContainsKey(path)
                   || _openSoundEditors.ContainsKey(path)
                   || _openItemEditors.ContainsKey(path)
                   || _openMerchantEditors.ContainsKey(path)
                   || _openConversations.ContainsKey(path)
                   || (type == ResourceType.Dlg &&
                       _openNuiConversations.Values.Any(editor =>
                           editor.ResRef.Equals(resRef, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Saves every open editor. Returns false as soon as a save fails or the user cancels an
        /// external-change prompt, allowing validation and packing to abort safely.
        /// </summary>
        public async Task<bool> SaveAllAsync()
        {
            if (_moduleProperties != null &&
                !await _moduleProperties.TrySaveAsync().ConfigureAwait(true))
            {
                return false;
            }

            foreach (var editor in _openEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openTriggerEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openWaypointEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openDoorEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openCreatureEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openSoundEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openItemEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openMerchantEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openAreaEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openConversations.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            foreach (var editor in _openNuiConversations.Values.ToList())
            {
                if (!await editor.TrySaveAsync().ConfigureAwait(true))
                    return false;
            }

            return await SaveScriptsAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// Saves every open script buffer. Explicit bulk compilation suppresses per-file
        /// compile-on-save so each entry point is written exactly once by the subsequent build.
        /// </summary>
        public async Task<bool> SaveScriptsAsync(bool compileOnSave = true)
        {
            foreach (var editor in _openScriptEditors.Values.ToList())
            {
                if (!await editor.TrySaveAsync(compileOnSave).ConfigureAwait(true))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Prompts before application shutdown when any editor is dirty. The caller may close the
        /// window only when this returns true; saving still honors each document's external-change
        /// check and cancels shutdown if any save is declined or fails.
        /// </summary>
        public async Task<bool> TryPrepareApplicationCloseAsync()
        {
            if (_moduleProperties?.IsDirty != true &&
                !_openEditors.Values.Any(editor => editor.IsDirty) &&
                !_openTriggerEditors.Values.Any(editor => editor.IsDirty) &&
                !_openWaypointEditors.Values.Any(editor => editor.IsDirty) &&
                !_openDoorEditors.Values.Any(editor => editor.IsDirty) &&
                !_openCreatureEditors.Values.Any(editor => editor.IsDirty) &&
                !_openSoundEditors.Values.Any(editor => editor.IsDirty) &&
                !_openItemEditors.Values.Any(editor => editor.IsDirty) &&
                !_openMerchantEditors.Values.Any(editor => editor.IsDirty) &&
                !_openAreaEditors.Values.Any(editor => editor.IsDirty) &&
                !_openScriptEditors.Values.Any(editor => editor.IsDirty || editor.HasPendingCompileFailure) &&
                !_openConversations.Values.Any(editor => editor.IsDirty) &&
                !_openNuiConversations.Values.Any(editor => editor.IsDirty))
                return true;

            var choice = await _prompts.ConfirmCloseAsync("all open editors").ConfigureAwait(true);
            if (choice == UnsavedChangesChoice.Save)
                return await SaveAllAsync().ConfigureAwait(true);

            if (choice != UnsavedChangesChoice.Discard)
                return false;

            _moduleProperties?.ApproveApplicationClose();

            foreach (var editor in _openEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openTriggerEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openWaypointEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openDoorEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openCreatureEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openSoundEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openItemEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openMerchantEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openAreaEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openScriptEditors.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openConversations.Values)
                editor.ApproveApplicationClose();
            foreach (var editor in _openNuiConversations.Values)
                editor.ApproveApplicationClose();

            return true;
        }

        /// <summary>Trigger blueprints open in the behavior editor, as a document tab.</summary>
        private void OpenTriggerEditor(string filePath, string resRef)
        {
            var editor = new Triggers.TriggerDocumentViewModel(
                filePath, resRef, _gameCodeIndex, _log, _prompts, ResolveTriggerTagArea,
                ResolveTriggerChoices,
                ChoicePreviews(),
                _blueprintSaves,
                CreateObjectSource(ResourceType.Utt, resRef));
            editor.Closed += closed => _openTriggerEditors.Remove(closed.FilePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Utt, editor.ResRef);
            editor.Renamed += (renamed, oldResRef, oldPath) =>
                OnBlueprintRenamed(
                    _openTriggerEditors, renamed, ResourceType.Utt,
                    oldResRef, oldPath, renamed.FilePath, renamed.ResRef);
            _openTriggerEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        /// <summary>Waypoint blueprints open in the behavior editor, as a document tab.</summary>
        private async void OpenWaypointEditor(string filePath, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || !_openingWaypointEditors.Add(filePath))
                return;

            try
            {
                // The transition classifier needs a module-wide GIT scan. Await its background
                // warm-up instead of parsing hundreds of area files on Avalonia's UI thread.
                var transitionDestinationTags =
                    await GetCurrentTransitionDestinationTagsAsync(workspace).ConfigureAwait(true);
                if (transitionDestinationTags == null)
                    return;

                if (_openWaypointEditors.TryGetValue(filePath, out var existing))
                {
                    _factory.ActivateDocument(existing);
                    return;
                }

                var catalog = new Domain.Editors.Waypoints.WaypointBehaviorCatalog(
                    _gameCodeIndex,
                    transitionDestinationTags);
                var editor = new Waypoints.WaypointDocumentViewModel(
                    filePath,
                    resRef,
                    _gameCodeIndex,
                    _log,
                    _prompts,
                    catalog,
                    ResolveWaypointChoices,
                    ChoicePreviews(),
                    _blueprintSaves,
                    CreateObjectSource(ResourceType.Utw, resRef));
                editor.Closed += closed => _openWaypointEditors.Remove(closed.FilePath);
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CatalogEntryChanged += () =>
                    _workspaceContext.RefreshCatalogEntry(ResourceType.Utw, editor.ResRef);
                editor.Renamed += (renamed, oldResRef, oldPath) =>
                    OnBlueprintRenamed(
                        _openWaypointEditors, renamed, ResourceType.Utw,
                        oldResRef, oldPath, renamed.FilePath, renamed.ResRef);
                _openWaypointEditors[filePath] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open waypoint editor for {resRef}: {ex.Message}");
            }
            finally
            {
                _openingWaypointEditors.Remove(filePath);
            }
        }

        /// <summary>Creature blueprints open with their linked stat/equipment resources.</summary>
        private void OpenCreatureEditor(string filePath, string resRef)
        {
            var editor = new Creatures.CreatureDocumentViewModel(
                filePath,
                resRef,
                _gameCodeIndex,
                _log,
                _prompts,
                ResolveCreatureChoices,
                _resourceIndex,
                _previewRenderer != null
                    ? creature => _previewRenderer.BuildModel(ResourceType.Utc, creature)
                    : null,
                CreatureAppearance,
                ArmorPartModels(),
                null,
                LoadCreatureEquipmentDetails,
                ChoicePreviews(),
                PreviewCreatureAudio,
                OpenLootDefinition,
                null,
                _thumbnails,
                ArmorDyeSwatches(),
                itemResRef => LoadMerchantItem(itemResRef)?.Name ?? itemResRef,
                SearchCreatureEquipmentItems,
                _appearances == null ? null : CreatureAppearanceOptions,
                CreatureAbilityIcon,
                CreateObjectSource(ResourceType.Utc, resRef));
            editor.Closed += closed => _openCreatureEditors.Remove(closed.FilePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Utc, editor.ResRef);
            _openCreatureEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        /// <summary>Door blueprints open in the same behavior editor used by door placements.</summary>
        private void OpenDoorEditor(string filePath, string resRef)
        {
            var editor = new Doors.DoorDocumentViewModel(
                filePath,
                resRef,
                _gameCodeIndex,
                _log,
                _prompts,
                ResolveDoorTag,
                ResolveDoorChoices,
                DoorAppearances(),
                _resourceIndex,
                _previewRenderer != null
                    ? door => _previewRenderer.BuildModelResult(ResourceType.Utd, door)
                    : null,
                _thumbnails,
                ChoicePreviews(),
                _blueprintSaves,
                CreateObjectSource(ResourceType.Utd, resRef));
            editor.Closed += closed => _openDoorEditors.Remove(closed.FilePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Utd, editor.ResRef);
            editor.Renamed += (renamed, oldResRef, oldPath) =>
                OnBlueprintRenamed(
                    _openDoorEditors, renamed, ResourceType.Utd,
                    oldResRef, oldPath, renamed.FilePath, renamed.ResRef);
            _openDoorEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        /// <summary>Item blueprints open in the behavior-shaped item editor, as a document tab.</summary>
        private void OpenItemEditor(string filePath, string resRef)
        {
            var editor = new Items.ItemDocumentViewModel(
                filePath,
                resRef,
                _gameCodeIndex,
                _log,
                _prompts,
                ResolveItemChoices,
                BaseItemRows(),
                _previewRenderer != null ? _previewRenderer.RenderItemIcon : null,
                ChoicePreviews(),
                BaseItemIcons(),
                _resourceIndex == null ? null : ItemTextureExists,
                ItemSourcesFor,
                AreItemSourcesReady,
                ItemCostTables(),
                resolveModel: _previewRenderer != null
                    ? (item, female) => _previewRenderer.BuildModel(
                        ResourceType.Uti, item, armorPreviewFemale: female)
                    : null,
                resourceIndex: _resourceIndex,
                armorDyeSwatches: ArmorDyeSwatches(),
                armorPartModels: ArmorPartModels(),
                findReferences: FindItemReferences,
                canRefileCategories: CanRefileItemCategories,
                refileCategories: RefileItemCategories,
                saveCoordinator: _blueprintSaves,
                placementSource: CreateObjectSource(ResourceType.Uti, resRef));
            editor.Closed += closed => _openItemEditors.Remove(closed.FilePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Uti, editor.ResRef);
            editor.Renamed += OnItemRenamed;
            _openItemEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        /// <summary>Merchant blueprints open in the dedicated inventory and buying-rules editor.</summary>
        private void OpenMerchantEditor(string filePath, string resRef)
        {
            // Share the item editor's filtered base-type catalog. The raw lookup includes deleted,
            // reserved, iconless, and broken-TLK rows (displayed as "Bad Strref"), none of which is
            // a meaningful merchant buying rule.
            var baseItems = ResolveItemChoices(Domain.Editors.Items.ItemChoiceKeys.BaseItems);
            var editor = new Merchants.MerchantDocumentViewModel(
                filePath,
                resRef,
                _log,
                _prompts,
                ResolveMerchantChoices,
                baseItems,
                LoadMerchantItem,
                SearchMerchantItems,
                _merchantInstances ??= new Merchants.MerchantInstanceService(
                    _workspaceContext,
                    _log,
                    areaResRef =>
                        _openAreaEditors.TryGetValue(areaResRef, out var area) &&
                        area.HasUnsavedInstanceChanges,
                    areaResRef =>
                    {
                        if (_openAreaEditors.TryGetValue(areaResRef, out var area))
                            area.ReloadInstancesAfterBlueprintSave();
                    }),
                _blueprintSaves,
                _thumbnails == null
                    ? null
                    : (itemResRef, onReady) =>
                        _thumbnails.RequestAsync(ResourceType.Uti, itemResRef, onReady),
                itemResRef => TryOpenEditor(ResourceType.Uti, itemResRef),
                (merchantResRef, placement) => GoToObjectPlacement(new ObjectPlacement(
                    ResourceType.Utm,
                    merchantResRef,
                    placement.AreaResRef,
                    placement.InstanceIndex,
                    placement.Tag,
                    placement.XPosition,
                    placement.YPosition,
                    placement.ZPosition)));
            editor.Closed += closed => _openMerchantEditors.Remove(closed.FilePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Utm, editor.ResRef);
            editor.Renamed += (renamed, oldResRef, oldPath) =>
                OnBlueprintRenamed(
                    _openMerchantEditors, renamed, ResourceType.Utm,
                    oldResRef, oldPath, renamed.FilePath, renamed.ResRef);
            _openMerchantEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        /// <summary>
        /// Follows a rename-on-save: re-keys the open map so reopening by either path behaves, swaps
        /// the catalog entry so Explorer and Search stop offering the old resref, and carries the
        /// item's custom-category membership (if any) over to the new resref.
        /// </summary>
        private void OnItemRenamed(Items.ItemDocumentViewModel editor, string oldResRef, string oldPath)
        {
            _openItemEditors.Remove(oldPath);
            _openItemEditors[editor.FilePath] = editor;
            _workspaceContext.RemoveCatalogEntry(ResourceType.Uti, oldResRef);
            _workspaceContext.RefreshCatalogEntry(ResourceType.Uti, editor.ResRef);
            _workspaceContext.InvalidatePlacementIndex();

            // The membership already moved during the save itself (see RefileItemCategories),
            // before the original was deleted, so there is nothing left to reconcile here beyond
            // the catalog re-keying above.
        }

        /// <summary>
        /// Moves an item's category membership as part of its rename. Called mid-save, before the
        /// original blueprint is deleted, so a sidecar that cannot be written aborts the whole
        /// rename instead of stranding the category on a resref about to disappear.
        /// </summary>
        private CategorySaveResult RefileItemCategories(string oldResRef, string newResRef)
        {
            var result = _categories?.RefileMember(ResourceType.Uti, oldResRef, newResRef)
                         ?? CategorySaveResult.Ok();
            if (result.Saved)
                return result;

            _log.AppendLine($"Could not move the category of {oldResRef} to {newResRef}: {result.Problem}");
            return result;
        }

        /// <summary>
        /// Whether an item rename's custom-category membership can be carried over, checked before
        /// the file rename writes anything. See <see cref="CategoryService.CanRefileMember"/>.
        /// </summary>
        private bool CanRefileItemCategories(string oldResRef) =>
            _categories == null || _categories.CanRefileMember(ResourceType.Uti, oldResRef);

        private Func<int, BaseItemRow?>? BaseItemRows()
        {
            if (_twoDaService == null)
                return null;

            _baseItemRowService ??= new BaseItemRowService(_twoDaService);
            return _baseItemRowService.GetOrNull;
        }

        private Func<int, BaseItemIconRow?>? BaseItemIcons()
        {
            if (_twoDaService == null)
                return null;

            _baseItemIconService ??= new BaseItemIconService(_twoDaService);
            return _baseItemIconService.GetOrNull;
        }

        /// <summary>The armor Colors panel's dye swatches; null when there is no resource layer to
        /// decode palette textures from - the panel still shows neutral chips.</summary>
        private Items.ArmorDyeSwatchService? ArmorDyeSwatches()
        {
            if (_resourceIndex == null)
                return null;

            _armorDyeSwatches ??= new Items.ArmorDyeSwatchService(_resourceIndex);
            return _armorDyeSwatches;
        }

        /// <summary>Which numbered body-part models exist, so the armor rows list real variants.</summary>
        private Items.ArmorPartCatalog? ArmorPartModels()
        {
            if (_resourceIndex == null)
                return null;

            _armorPartModels ??= new Items.ArmorPartCatalog(_resourceIndex);
            return _armorPartModels;
        }

        /// <summary>A stat/requirement/appearance cell's real engine cap, by CostTableId.</summary>
        private Domain.Editors.Items.ItemCostTableRanges? ItemCostTables()
        {
            if (_twoDaService == null)
                return null;

            _itemCostTableRanges ??= new Domain.Editors.Items.ItemCostTableRanges(_twoDaService);
            return _itemCostTableRanges;
        }

        /// <summary>
        /// Files that still name an item resref, for the rename-on-save refusal. Swept fresh per
        /// call - a rename is rare, and a stale index here would let a just-added reference slip
        /// through the very check that exists to protect it.
        /// </summary>
        private IReadOnlyList<string> FindItemReferences(string resRef, string selfFilePath)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<string>();

            var repoRoot = Path.GetDirectoryName(Path.GetFullPath(workspace.ModuleRoot));
            var gameSourceRoot = repoRoot == null ? null : Path.Combine(repoRoot, "SWLOR.Game.Server");
            var generatorInputRoot = repoRoot == null
                ? null
                : Path.Combine(repoRoot, "SWLOR.CLI", "InputFiles");
            var references = ItemReferenceScanner.FindReferences(
                workspace.ModuleRoot,
                gameSourceRoot,
                resRef,
                selfFilePath,
                generatorInputRoot).ToList();

            // The scan reads disk, but an open script's unsaved buffer can already name this resref
            // - and SaveAll writes item editors before script editors, so a rename that only
            // consulted disk could delete the blueprint and then save a script that references it.
            // Editors whose buffer no longer names it still count from their on-disk copy above:
            // this preflight fails closed, and an unsaved deletion is not a reference removed yet.
            var quoted = $"\"{resRef}\"";
            foreach (var (scriptResRef, text) in SnapshotOpenScriptSources())
            {
                if (text.Contains(quoted, StringComparison.OrdinalIgnoreCase))
                    references.Add($"Module/nss/{scriptResRef}.nss (unsaved editor buffer)");
            }

            foreach (var area in _openAreaEditors.Values)
            {
                var itemSection = area.Sections.FirstOrDefault(section =>
                    section.BlueprintType == ResourceType.Uti);
                if (itemSection?.Rows.Any(row =>
                        string.Equals(row.TemplateResRef, resRef, StringComparison.OrdinalIgnoreCase)) == true)
                {
                    references.Add(
                        $"Module/git/{area.AreaResRef}.git.json (unsaved area editor)");
                }
            }

            return references
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Where a player can obtain an item, from the workspace index. Never builds the index on
        /// the calling thread: an unbuilt index answers "not ready yet" and the background build
        /// (see <see cref="WarmItemSourcesAsync"/>) refreshes every open item editor when it
        /// lands. Building here is what made the first item editor open sit for seconds - the scan
        /// reads every .cs under SWLOR.Game.Server plus every store, creature and container.
        /// </summary>
        private IReadOnlyList<ItemSourceEntry> ItemSourcesFor(string resRef)
        {
            if (_itemSources != null)
                return _itemSources.SourcesFor(resRef);

            // A module opened before this service existed (or an editor opened during the build)
            // still gets an index - the warm is idempotent and returns the running one.
            _ = WarmItemSourcesAsync();
            return Array.Empty<ItemSourceEntry>();
        }

        /// <summary>Whether the obtainability index has finished building, for the Source tab's verdict.</summary>
        private bool AreItemSourcesReady() => _itemSources != null;

        /// <summary>
        /// Builds the obtainability index off the UI thread, once per workspace. Started when a
        /// module opens so the cost is paid during startup rather than on the first item editor,
        /// and awaited by nothing - open editors refresh from the completion instead.
        /// </summary>
        public Task WarmItemSourcesAsync()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || _itemSources != null || _itemSourceRetryBlocked)
                return Task.CompletedTask;

            return _itemSourcesBuild ??= BuildItemSourcesAsync(workspace);
        }

        private async Task BuildItemSourcesAsync(Domain.Workspace.ModuleWorkspace workspace)
        {
            // Captured before the scan starts and compared again after it finishes. A save that
            // lands mid-scan bumps this through WorkspaceOpened/CatalogEntryRefreshed above (and
            // already reset _itemSources to null), so a mismatch here is the signal that the result
            // about to be published reflects pre-save content, even though nothing else distinguishes
            // it from a clean scan.
            var generation = _itemSourcesGeneration;
            var retryNeeded = false;
            try
            {
                var repoRoot = Path.GetDirectoryName(Path.GetFullPath(workspace.ModuleRoot));
                var gameSourceRoot = repoRoot == null
                    ? null
                    : Path.Combine(repoRoot, "SWLOR.Game.Server");

                var index = await Task.Run(
                    () => _itemSourcesBuilder(workspace, gameSourceRoot)).ConfigureAwait(true);

                // A workspace-open event tries to warm immediately, but while this task is still in
                // _itemSourcesBuild that call can only reuse this obsolete scan. Queue the replacement
                // after finally clears the shared task instead of assuming the event started one.
                if (!ReferenceEquals(_workspaceContext.Workspace, workspace))
                {
                    retryNeeded = true;
                }
                else if (_itemSourcesGeneration != generation)
                {
                    // Something changed while the scan was running. Publishing this result would
                    // keep an item's Source tab reporting pre-save obtainability until the next
                    // unrelated save happened to trigger another rebuild - the retry below starts
                    // a fresh one instead of leaving that to chance.
                    retryNeeded = true;
                }
                else
                {
                    _itemSources = index;
                    _itemSourceFailureRetries = 0;
                    _itemSourceRetryBlocked = false;
                    foreach (var editor in _openItemEditors.Values.ToList())
                        editor.Editor.RefreshSource();
                }
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not build the item source index: {ex.Message}");
                var currentWorkspace = _workspaceContext.Workspace;
                var invalidated = !ReferenceEquals(currentWorkspace, workspace) ||
                                  _itemSourcesGeneration != generation;
                if (currentWorkspace != null && invalidated)
                {
                    retryNeeded = true;
                }
                else if (currentWorkspace != null &&
                         _itemSourceFailureRetries < MaximumAutomaticItemSourceFailureRetries)
                {
                    _itemSourceFailureRetries++;
                    retryNeeded = true;
                }
                else
                {
                    // A persistent read failure must not rescan the whole module forever. The next
                    // workspace/catalog invalidation clears this block and permits a fresh attempt.
                    _itemSourceRetryBlocked = true;
                }
            }
            finally
            {
                _itemSourcesBuild = null;
            }

            // The retry has to start after finally clears _itemSourcesBuild, or
            // WarmItemSourcesAsync would hand back this just-finished task instead of starting a
            // new scan. One queued rebuild per invalidation is enough: a retry that is itself
            // invalidated queues its own single follow-up, so a save storm converges rather than
            // looping.
            if (retryNeeded)
            {
                await Task.Delay(ItemSourcesRetryDelay).ConfigureAwait(true);
                _ = WarmItemSourcesAsync();
            }
        }

        /// <summary>
        /// Whether artwork exists for an icon resref, probed in the same tga/dds/plt order
        /// TextureLoader decodes in - a lookup, never a decode.
        /// </summary>
        private bool ItemTextureExists(string resRef)
        {
            var index = _resourceIndex;
            if (index == null || string.IsNullOrWhiteSpace(resRef))
                return false;

            return index.TryLookup(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("tga")), out _)
                || index.TryLookup(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("dds")), out _)
                || index.TryLookup(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("plt")), out _);
        }

        private IReadOnlyList<BehaviorChoice> ResolveItemChoices(string key) =>
            Cached("item", key, BuildItemChoices);

        private IReadOnlyList<BehaviorChoice> ResolveMerchantChoices(string key) =>
            Cached("merchant", key, BuildMerchantChoices);

        private IReadOnlyList<BehaviorChoice> BuildMerchantChoices(string key)
        {
            if (key != Domain.Editors.Merchants.MerchantChoiceKeys.PaletteCategories)
                return Array.Empty<BehaviorChoice>();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "storepalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<BehaviorChoice>();

                return SortByDisplay(PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null));
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the merchant palette categories: {ex.Message}");
                return Array.Empty<BehaviorChoice>();
            }
        }

        private void OnBlueprintRenamed<TEditor>(
            IDictionary<string, TEditor> openEditors,
            TEditor editor,
            ResourceType type,
            string oldResRef,
            string oldPath,
            string newPath,
            string newResRef)
        {
            openEditors.Remove(oldPath);
            openEditors[newPath] = editor;
            _workspaceContext.RemoveCatalogEntry(type, oldResRef);
            _workspaceContext.RefreshCatalogEntry(type, newResRef);
            _workspaceContext.InvalidatePlacementIndex();
        }

        private Merchants.MerchantItemDefinition? LoadMerchantItem(string resRef)
        {
            return LoadMerchantItemDetails(
                resRef,
                BaseItemRows(),
                ItemCostTables(),
                ResolveItemChoices);
        }

        private Merchants.MerchantItemDefinition? LoadMerchantItemSummary(
            string resRef,
            Func<int, BaseItemRow?>? baseItemRows)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || string.IsNullOrWhiteSpace(resRef))
                return null;
            if (_merchantItemSearchEligibility.TryGetValue(resRef, out var eligible))
            {
                if (!eligible)
                    return null;
                if (_merchantItemDetails.TryGetValue(resRef, out var detailed))
                    return detailed;
                if (_merchantItemSummaries.TryGetValue(resRef, out var cached))
                    return cached;
            }

            Merchants.MerchantItemDefinition definition;
            try
            {
                var item = workspace.LoadBlueprint(ResourceType.Uti, resRef).Fields;
                var name = ResolveMerchantItemName(resRef, item);
                var baseItem = item.GetIntOrNull("BaseItem") ?? -1;
                var noEconomy = new VarTable(item).GetInt(GameItem.NoEconomyVariable) == 1;
                var hasInventoryIcon = _previewRenderer?.RenderItemIcon(item) != null;
                if (GameItem.IsEconomyRestricted(
                        (BaseItem)baseItem,
                        name ?? string.Empty,
                        noEconomy,
                        hasInventoryIcon))
                {
                    _merchantItemSearchEligibility[resRef] = false;
                    return null;
                }

                _merchantItemSearchEligibility[resRef] = true;
                definition = BuildMerchantItemDefinition(resRef, item, baseItemRows);
            }
            catch
            {
                return null;
            }

            return _merchantItemSummaries.GetOrAdd(resRef, definition);
        }

        private Merchants.MerchantItemDefinition? LoadMerchantItemDetails(
            string resRef,
            Func<int, BaseItemRow?>? baseItemRows,
            Domain.Editors.Items.ItemCostTableRanges? costTables,
            Func<string, IReadOnlyList<BehaviorChoice>> resolveChoices)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || string.IsNullOrWhiteSpace(resRef))
                return null;
            if (_merchantItemDetails.TryGetValue(resRef, out var cached))
                return cached;

            Merchants.MerchantItemDefinition definition;
            try
            {
                var item = workspace.LoadBlueprint(ResourceType.Uti, resRef).Fields;
                definition = BuildMerchantItemDefinition(
                    resRef,
                    item,
                    baseItemRows,
                    Items.ItemStatSummary.Build(item, costTables, resolveChoices));
            }
            catch
            {
                definition = new Merchants.MerchantItemDefinition(resRef, resRef, 0);
            }

            definition = _merchantItemDetails.GetOrAdd(resRef, definition);
            _merchantItemSummaries[resRef] = definition;
            return definition;
        }

        private Merchants.MerchantItemDefinition BuildMerchantItemDefinition(
            string resRef,
            JsonGffStruct item,
            Func<int, BaseItemRow?>? baseItemRows,
            IReadOnlyList<Items.ItemStatSummaryGroup>? statGroups = null)
        {
            var name = ResolveMerchantItemName(resRef, item);

            var cost = (long)(item.GetUIntOrNull("Cost") ?? 0) +
                       (item.GetUIntOrNull("AddCost") ?? 0);
            var baseItem = item.GetIntOrNull("BaseItem") ?? -1;
            var storePanel = baseItemRows?.Invoke(baseItem)?.StorePanel
                             ?? (int)Domain.Editors.Merchants.MerchantInventoryCategory.Miscellaneous;
            return new Merchants.MerchantItemDefinition(
                resRef,
                string.IsNullOrWhiteSpace(name) ? resRef : name,
                cost,
                storePanel,
                statGroups,
                HasKnownStorePanel: true);
        }

        private string? ResolveMerchantItemName(string resRef, JsonGffStruct item)
        {
            var name = _workspaceContext.Catalog?.TryGetEntry(
                           ResourceType.Uti, resRef, out var entry) == true
                ? entry.Name
                : null;
            if (string.IsNullOrWhiteSpace(name))
                name = item.GetLocStringOrNull("LocalizedName")?.Text;
            return name;
        }

        private async Task<IReadOnlyList<Merchants.MerchantItemDefinition>> SearchMerchantItems(
            string query,
            int storePanel,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            if (_workspaceContext.Workspace == null || take <= 0)
                return Array.Empty<Merchants.MerchantItemDefinition>();

            if (_workspaceContext.Catalog is { } catalog && !catalog.BuildTask.IsCompleted)
                await catalog.BuildTask.WaitAsync(cancellationToken).ConfigureAwait(true);

            cancellationToken.ThrowIfCancellationRequested();

            if (_merchantItemSearchIndex == null)
            {
                var baseItemRows = BaseItemRows();
                var costTables = ItemCostTables();
                var resolveChoices = CacheItemSubtypeChoices();
                _merchantItemSearchIndex = new Merchants.MerchantItemSearchIndex(
                    MerchantItemCatalog(),
                    resRef => LoadMerchantItemSummary(resRef, baseItemRows),
                    resRef => LoadMerchantItemDetails(
                        resRef, baseItemRows, costTables, resolveChoices));
            }

            return await _merchantItemSearchIndex.SearchAsync(
                query,
                storePanel,
                skip,
                take,
                cancellationToken).ConfigureAwait(true);
        }

        private Func<string, IReadOnlyList<BehaviorChoice>> CacheItemSubtypeChoices()
        {
            const string subtypePrefix = "item.subtypes:";
            var choiceSets = Domain.Editors.Items.ItemMultiEntryCatalog.All
                .Select(definition => definition.SubtypeTableResRef)
                .Concat(Domain.Editors.Items.ItemEngineLegacyCatalog.All
                    .Select(definition => definition.SubtypeTableResRef))
                .Where(table => !string.IsNullOrWhiteSpace(table))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    table => subtypePrefix + table,
                    table => ResolveItemChoices(subtypePrefix + table),
                    StringComparer.OrdinalIgnoreCase);
            return key => choiceSets.TryGetValue(key, out var choices)
                ? choices
                : Array.Empty<BehaviorChoice>();
        }

        private IReadOnlyList<string> FindUnsavedBlueprintReferences(string resRef)
        {
            var references = new List<string>();
            var quoted = $"\"{resRef}\"";
            foreach (var (scriptResRef, text) in SnapshotOpenScriptSources())
            {
                if (text.Contains(quoted, StringComparison.OrdinalIgnoreCase))
                {
                    references.Add(
                        $"Module/nss/{scriptResRef}.nss (unsaved editor buffer)");
                }
            }

            foreach (var area in _openAreaEditors.Values)
            {
                if (!area.HasUnsavedInstanceChanges)
                    continue;

                if (area.Sections.Any(section => section.Rows.Any(row =>
                        string.Equals(
                            row.TemplateResRef,
                            resRef,
                            StringComparison.OrdinalIgnoreCase))))
                {
                    references.Add(
                        $"Module/git/{area.AreaResRef}.git.json (unsaved area editor)");
                }
            }

            return references;
        }

        private IReadOnlyList<Merchants.MerchantItemDefinition> MerchantItemCatalog()
        {
            if (_merchantItemCatalog != null)
                return _merchantItemCatalog;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<Merchants.MerchantItemDefinition>();

            var items = new Dictionary<string, Merchants.MerchantItemDefinition>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var resRef in workspace.EnumerateResRefs(ResourceType.Uti))
                items[resRef] = new Merchants.MerchantItemDefinition(resRef, resRef, 0);

            if (_workspaceContext.Catalog is { } catalog)
            {
                var baseItemRows = BaseItemRows();
                foreach (var entry in catalog.EntriesOfType(ResourceType.Uti))
                {
                    var storePanel = entry.BaseItem is { } baseItem
                        ? baseItemRows?.Invoke(baseItem)?.StorePanel
                        : null;
                    items[entry.ResRef] = new Merchants.MerchantItemDefinition(
                        entry.ResRef,
                        string.IsNullOrWhiteSpace(entry.Name) ? entry.ResRef : entry.Name!,
                        0,
                        storePanel ?? (int)Domain.Editors.Merchants.MerchantInventoryCategory.Miscellaneous,
                        HasKnownStorePanel: storePanel.HasValue);
                }
            }

            if (_resourceIndex != null)
            {
                var utiType = ResourceIdentity.TypeFromExtension("uti");
                foreach (var identity in _resourceIndex.EnumerateResources(utiType))
                {
                    items.TryAdd(
                        identity.ResRef,
                        new Merchants.MerchantItemDefinition(
                            identity.ResRef, identity.ResRef, 0));
                }
            }

            _merchantItemCatalog = items.Values
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return _merchantItemCatalog;
        }

        private IReadOnlyList<BehaviorChoice> BuildItemChoices(string key) =>
            SortByDisplay(BuildItemChoicesUnsorted(key));

        private IReadOnlyList<BehaviorChoice> BuildItemChoicesUnsorted(string key)
        {
            if (key == Domain.Editors.Items.ItemChoiceKeys.PaletteCategories)
                return ResolveItemCategories();

            if (key == Domain.Editors.Items.ItemChoiceKeys.Spells)
            {
                var tlk = _tlkService;
                return Domain.Editors.Items.ItemSpellChoiceCatalog.Read(
                    _twoDaService,
                    tlk == null ? null : id => tlk.GetString((uint)id));
            }

            // Subtype pickers name their table in the key ("item.subtypes:iprp_foodtype"), so one
            // case serves every multi-subtype property and each table is still cached separately.
            const string subtypePrefix = "item.subtypes:";
            if (key.StartsWith(subtypePrefix, StringComparison.Ordinal))
            {
                var tlk = _tlkService;
                return Domain.Editors.Items.ItemSubtypeChoiceCatalog.Read(
                    _twoDaService,
                    key[subtypePrefix.Length..],
                    tlk == null ? null : id => tlk.GetString((uint)id));
            }

            if (key == Domain.Editors.Items.ItemChoiceKeys.BaseItems)
            {
                var options = _lookups.GetOptions(LookupKeys.BaseItems);
                var rows = BaseItemRows();
                var icons = BaseItemIcons();

                // The item editor's established rule needs label and ItemClass together. Do not
                // invent a weaker display-only merchant rule when that structural metadata is
                // unavailable; fail closed instead of exposing reserved USER/CEP/Bio rows.
                if (rows == null || icons == null)
                    return Array.Empty<BehaviorChoice>();

                return options
                    .Where(option => Domain.Editors.Items.BaseItemChoicePolicy.IsOffered(
                        rows((int)option.Id)?.Label, icons((int)option.Id)?.ItemClass, option.Display))
                    .Select(option => new BehaviorChoice(option.Id, option.Display))
                    .ToList();
            }

            return Array.Empty<BehaviorChoice>();
        }

        /// <summary>
        /// Every item-editor choice list (Base Types, Palette Categories, Spells, and every
        /// "item.subtypes:&lt;table&gt;" subtype set) is offered alphabetically by its display text -
        /// one central sort here rather than one per source, so a new source never has to remember
        /// to do it itself. Category displays are hierarchical strings ("Armor &gt; Clothing"); a
        /// plain ordinal sort of the whole string is what "alphabetical" means for those too.
        /// </summary>
        private static IReadOnlyList<BehaviorChoice> SortByDisplay(IReadOnlyList<BehaviorChoice> choices) =>
            choices.OrderBy(choice => choice.Display, StringComparer.OrdinalIgnoreCase).ToList();

        private IReadOnlyList<BehaviorChoice> ResolveItemCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "itempalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<BehaviorChoice>();

                return PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the item palette categories: {ex.Message}");
                return Array.Empty<BehaviorChoice>();
            }
        }

        private IReadOnlyList<Domain.Editors.Doors.DoorAppearanceChoice> DoorAppearances() =>
            _doorAppearances ??= Domain.Editors.Doors.DoorAppearanceCatalog.Read(_doorTypes);

        private IReadOnlyList<BehaviorChoice> ResolveCreatureChoices(string key) =>
            Cached("creature", key, BuildCreatureChoices);

        private IReadOnlyList<BehaviorChoice> BuildCreatureChoices(string key)
        {
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.PaletteCategories)
                return ResolveCreatureCategories();

            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.Appearances && _appearances != null)
            {
                return _appearances.GetAll()
                    .Select(row => new BehaviorChoice(
                        row.Id,
                        $"{row.DisplayName} ({row.Id})",
                        modelResRef: string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase)
                            ? null
                            : row.Race))
                    .ToList();
            }

            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.Portraits && _portraits != null)
                return ResolvePortraitChoices();

            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.Races)
                return LookupChoices(LookupKeys.Races);
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.Factions)
                return LookupChoices(LookupKeys.Factions);
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.Genders)
                return LookupChoices(LookupKeys.Gender);
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.Phenotypes)
                return LookupChoices(LookupKeys.Phenotype);
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.SoundSets)
            {
                var table = _twoDaService?.TryGetTable("soundset", out var soundSets) == true
                    ? soundSets
                    : null;
                return _lookups.GetOptions(LookupKeys.SoundSets)
                    .Select(option => new BehaviorChoice(
                        option.Id,
                        option.Display,
                        canPreviewAudio: !string.IsNullOrWhiteSpace(
                            table?.GetString((int)option.Id, "RESREF"))))
                    .ToList();
            }
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.MovementRates)
                return LookupChoices(LookupKeys.CreatureMovementRates);

            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.Dialogs)
                return CreatureBlueprintChoices(ResourceType.Dlg);
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.NpcGroups && _gameCodeIndex != null)
                return _gameCodeIndex.NpcGroups.OrderBy(entry => entry.Value)
                    .Select(entry => new BehaviorChoice(entry.Key, $"{entry.Value} ({entry.Key})")).ToList();
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.DialogDefinitions && _gameCodeIndex != null)
                return _gameCodeIndex.DialogNames.Order(StringComparer.OrdinalIgnoreCase)
                    .Select(name => new BehaviorChoice(name, name)).ToList();
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.Guilds)
            {
                return Enum.GetValues<SWLOR.Game.Server.Enumeration.GuildType>()
                    .Where(value => value != SWLOR.Game.Server.Enumeration.GuildType.Invalid)
                    .Select(value => new BehaviorChoice(
                        (int)value,
                        Humanize(value.ToString())))
                    .ToList();
            }
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.GuildStores)
                return GuildStoreChoices();
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.BeastTypes)
            {
                return Enum.GetValues<SWLOR.Game.Server.Service.BeastMasteryService.BeastType>()
                    .Where(value => value != SWLOR.Game.Server.Service.BeastMasteryService.BeastType.Invalid)
                    .Select(value => new BehaviorChoice((int)value, $"{Humanize(value.ToString())} ({(int)value})"))
                    .ToList();
            }
            if (key == Domain.Editors.Creatures.CreatureChoiceKeys.VisualEffects && _gameCodeIndex != null)
            {
                return Domain.Editors.Creatures.CreatureVisualEffectCatalog.Build(
                    _gameCodeIndex.VisualEffects,
                    _gameCodeIndex.VisualEffectReferences);
            }

            return Array.Empty<BehaviorChoice>();
        }

        private IReadOnlyList<BehaviorChoice> LookupChoices(string key) => _lookups.GetOptions(key)
            .Select(option => new BehaviorChoice(option.Id, option.BehaviorDisplay))
            .ToList();

        private IReadOnlyList<BehaviorChoice> ResolvePortraitChoices() =>
            Cached("shared", LookupKeys.Portraits, _ =>
            {
                if (_portraits == null)
                    return Array.Empty<BehaviorChoice>();

                var genders = _lookups.GetOptions(LookupKeys.Gender)
                    .GroupBy(option => (int)option.Id)
                    .ToDictionary(group => group.Key, group => group.First().Display);
                var races = _lookups.GetOptions(LookupKeys.Races)
                    .GroupBy(option => (int)option.Id)
                    .ToDictionary(group => group.Key, group => group.First().Display);
                return PortraitBehaviorChoiceCatalog.Build(_portraits.GetAll(), genders, races);
            });

        private IReadOnlyList<BehaviorChoice> ResolveCreatureCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<BehaviorChoice>();
            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "creaturepalcus.itp.json");
                return File.Exists(path)
                    ? PaletteCategoryReader.Read(
                        Domain.Documents.ItpDocument.Load(path),
                        _tlkService != null ? _tlkService.GetString : null)
                    : Array.Empty<BehaviorChoice>();
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the creature palette categories: {ex.Message}");
                return Array.Empty<BehaviorChoice>();
            }
        }

        private IReadOnlyList<BehaviorChoice> CreatureBlueprintChoices(ResourceType type)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<BehaviorChoice>();
            var names = _workspaceContext.Catalog?.EntriesOfType(type)
                .ToDictionary(entry => entry.ResRef, entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            return workspace.EnumerateResRefs(type)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(resRef => new BehaviorChoice(
                    resRef,
                    names.TryGetValue(resRef, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? $"{name} ({resRef})"
                        : resRef))
                .ToList();
        }

        private IReadOnlyList<BehaviorChoice> GuildStoreChoices()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<BehaviorChoice>();
            var choices = new Dictionary<string, BehaviorChoice>(StringComparer.OrdinalIgnoreCase);
            foreach (var resRef in workspace.EnumerateResRefs(ResourceType.Utm))
            {
                try
                {
                    var root = JsonGffDocument.Load(workspace.GetResourcePath(ResourceType.Utm, resRef)).Root;
                    var tag = root.GetStringOrNull("Tag");
                    if (string.IsNullOrWhiteSpace(tag))
                        continue;
                    var name = root.GetLocStringOrNull("LocName")?.Text;
                    choices[tag] = new BehaviorChoice(
                        tag,
                        string.IsNullOrWhiteSpace(name) ? tag : $"{name} ({tag})");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
                {
                }
            }
            return choices.Values.OrderBy(choice => choice.Display, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
         /// Supplies one visible equipment page. It follows the merchant editor's established
         /// repository-backed picker: use the lightweight catalog to narrow names first, then parse
         /// only enough matching blueprints to fill this slot's requested page. Stats and preview
         /// renders therefore advance with the builder's search and scroll instead of blocking the
         /// tab on every UTI in the module.
         /// </summary>
        private async Task<IReadOnlyList<Creatures.CreatureEquipmentChoice>> SearchCreatureEquipmentItems(
            string query,
            int slot,
            int skip,
            int take)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || take <= 0)
                return Array.Empty<Creatures.CreatureEquipmentChoice>();

            var trimmed = query.Trim();
            var candidates = MerchantItemCatalog()
                .Where(item => trimmed.Length == 0 ||
                               item.ResRef.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                               item.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var baseItemRows = BaseItemRows();
            var costTables = ItemCostTables();
            const string subtypePrefix = "item.subtypes:";
            var subtypeChoiceSets = Domain.Editors.Items.ItemMultiEntryCatalog.All
                .Select(definition => definition.SubtypeTableResRef)
                .Concat(Domain.Editors.Items.ItemEngineLegacyCatalog.All
                    .Select(definition => definition.SubtypeTableResRef))
                .Where(table => !string.IsNullOrWhiteSpace(table))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    table => subtypePrefix + table,
                    table => ResolveItemChoices(subtypePrefix + table),
                    StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<BehaviorChoice> ResolveCachedItemChoices(string key) =>
                subtypeChoiceSets.TryGetValue(key, out var choices)
                    ? choices
                    : Array.Empty<BehaviorChoice>();
            var knownDetails = new Dictionary<string, Creatures.CreatureEquipmentChoice>(
                _creatureEquipmentDetails,
                StringComparer.OrdinalIgnoreCase);
            var generation = _creatureEquipmentChoicesGeneration;

            var page = await Task.Run(() =>
            {
                var matches = new List<Creatures.CreatureEquipmentChoice>();
                var parsed = new List<Creatures.CreatureEquipmentChoice>();
                var matched = 0;
                foreach (var item in candidates)
                {
                    Creatures.CreatureEquipmentChoice? detailed;
                    if (!knownDetails.TryGetValue(item.ResRef, out detailed))
                    {
                        try
                        {
                            detailed = BuildCreatureEquipmentDetails(
                                item.ResRef,
                                workspace.LoadBlueprint(ResourceType.Uti, item.ResRef).Fields,
                                baseItemRows,
                                costTables,
                                ResolveCachedItemChoices);
                            parsed.Add(detailed);
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
                        {
                            continue;
                        }
                    }

                    if ((detailed.EquipableSlots & slot) == 0)
                        continue;

                    if (matched++ < Math.Max(0, skip))
                        continue;

                    matches.Add(detailed);
                    if (matches.Count == take)
                        break;
                }

                return (Matches: (IReadOnlyList<Creatures.CreatureEquipmentChoice>)matches,
                    Parsed: (IReadOnlyList<Creatures.CreatureEquipmentChoice>)parsed);
            }).ConfigureAwait(true);

            if (generation != _creatureEquipmentChoicesGeneration ||
                !ReferenceEquals(workspace, _workspaceContext.Workspace))
            {
                return await SearchCreatureEquipmentItems(query, slot, skip, take).ConfigureAwait(true);
            }

            foreach (var detailed in page.Parsed)
                _creatureEquipmentDetails[detailed.ResRef] = detailed;
            return page.Matches;
        }

        private void InvalidateCreatureEquipmentChoices(string? resRef = null)
        {
            _creatureEquipmentChoicesGeneration++;
            if (string.IsNullOrWhiteSpace(resRef))
                _creatureEquipmentDetails.Clear();
            else
                _creatureEquipmentDetails.Remove(resRef);
        }

        /// <summary>
        /// Loads one equipped blueprint for the details pane without scanning every UTI. The
        /// progressive equipment gallery fills this same cache as each requested page is published.
        /// </summary>
        private Creatures.CreatureEquipmentChoice? LoadCreatureEquipmentDetails(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return null;
            if (_creatureEquipmentDetails.TryGetValue(resRef, out var cached))
                return cached;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            try
            {
                var root = workspace.LoadBlueprint(ResourceType.Uti, resRef).Fields;
                var details = BuildCreatureEquipmentDetails(resRef, root, BaseItemRows());
                _creatureEquipmentDetails[resRef] = details;
                return details;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
            {
                return null;
            }
        }

        private Creatures.CreatureEquipmentChoice BuildCreatureEquipmentDetails(
            string resRef,
            JsonGffStruct root,
            Func<int, BaseItemRow?>? baseItemRows,
            Domain.Editors.Items.ItemCostTableRanges? costTables = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveItemChoices = null)
        {
            var baseItem = root.GetIntOrNull("BaseItem") ?? -1;
            var name = root.GetLocStringOrNull("LocalizedName")?.Text;
            var stats = Items.ItemStatSummary.Build(
                root,
                costTables ?? ItemCostTables(),
                resolveItemChoices ?? ResolveItemChoices);
            return new Creatures.CreatureEquipmentChoice(
                resRef,
                string.IsNullOrWhiteSpace(name) ? resRef : $"{name} ({resRef})",
                baseItem,
                baseItemRows?.Invoke(baseItem)?.EquipableSlots ?? 0,
                stats);
        }

        /// <summary>
        /// The shared creature appearance option set used by both generic UTC fields and the
        /// specialized creature editor. The projection is cached once per game-data generation.
        /// </summary>
        private IReadOnlyList<Appearance.AppearanceOption> CreatureAppearanceOptions()
        {
            lock (_creatureAppearanceOptionsGate)
            {
                if (_creatureAppearanceOptions != null)
                    return _creatureAppearanceOptions;
                if (_appearances == null)
                    return Array.Empty<Appearance.AppearanceOption>();

                _creatureAppearanceOptions = _appearances.GetAll()
                    .Select(row => new Appearance.AppearanceOption(
                        row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        row.DisplayName,
                        // The label rather than the model column: half of appearance.2da names a
                        // phenotype there, and "H" tells a builder nothing about what they picked.
                        $"row {row.Id} \u00b7 {row.Label}",
                        CreatureAppearanceId: row.Id,
                        IsSegmentedCreatureAppearance:
                            string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                return _creatureAppearanceOptions;
            }
        }

        /// <summary>
        /// Looks up a selected appearance through AppearanceService's cached ID index. Body-part
        /// bindings ask for this repeatedly, so scanning the entire 2DA on every property read made
        /// the Appearance tab cost proportional to the table size.
        /// </summary>
        private AppearanceRow? CreatureAppearance(int id)
        {
            if (_appearances == null)
                return null;

            try
            {
                return _appearances.Get(id);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private static string Humanize(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2")
                .Replace('_', ' ');

        private Behaviors.ChoicePreviewService ChoicePreviews() =>
            _choicePreviews ??= new Behaviors.ChoicePreviewService(_resourceIndex, _thumbnails, _vfxPreviews);

        private string? CreatureAbilityIcon(int featId)
        {
            return _twoDaService?.TryGetTable("feat", out var feats) == true
                ? feats?.GetString(featId, "ICON")?.Trim()
                : null;
        }

        private string? PreviewCreatureAudio(BehaviorChoice choice)
        {
            var resolver = _creatureSoundSetPreviews ??=
                new Creatures.CreatureSoundSetPreviewResolver(_twoDaService, _resourceIndex);
            var sample = resolver.Resolve((int)choice.Value);
            return sample == null
                ? "This sound set has no preview sample."
                : SoundPreviews().Play(sample);
        }

        private void OpenLootDefinition(string typeName)
        {
            var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
            var repositoryRoot = moduleRoot == null ? null : Directory.GetParent(moduleRoot)?.FullName;
            var definitionsRoot = repositoryRoot == null
                ? null
                : Path.Combine(repositoryRoot, "SWLOR.Game.Server", "Feature", "LootTableDefinition");
            var path = definitionsRoot != null && Directory.Exists(definitionsRoot)
                ? Directory.EnumerateFiles(definitionsRoot, typeName + ".cs", SearchOption.AllDirectories)
                    .FirstOrDefault()
                : null;
            if (path == null || _externalLinks == null)
            {
                _log.AppendLine("The loot table source definition is not available in this workspace.");
                return;
            }
            _externalLinks.OpenFile(path);
        }

        /// <summary>
        /// One option set, built on first use and kept. Every one of these is a 2DA read, a palette
        /// file parse, or a module scan; before this, opening a second door of the same kind redid
        /// all of them.
        /// </summary>
        private IReadOnlyList<BehaviorChoice> Cached(
            string scope,
            string key,
            Func<string, IReadOnlyList<BehaviorChoice>> build)
        {
            var cacheKey = scope + ":" + key;
            var cached = _choiceSets.GetOrAdd(
                cacheKey,
                _ => new Lazy<IReadOnlyList<BehaviorChoice>>(
                    () => build(key),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            try
            {
                return cached.Value;
            }
            catch
            {
                // Do not permanently cache a transient parse/indexing failure.
                _choiceSets.TryRemove(cacheKey, out _);
                throw;
            }
        }

        private void InvalidatePaletteChoices(string paletteResRef)
        {
            switch (paletteResRef.ToLowerInvariant())
            {
                case "creaturepalcus":
                    _choiceSets.TryRemove(
                        "creature:" + Domain.Editors.Creatures.CreatureChoiceKeys.PaletteCategories,
                        out _);
                    foreach (var editor in _openCreatureEditors.Values)
                        editor.Editor.RefreshPaletteChoices();
                    break;
                case "doorpalcus":
                    _choiceSets.TryRemove(
                        "door:" + Domain.Editors.Doors.DoorChoiceKeys.DoorPaletteCategories,
                        out _);
                    foreach (var editor in _openDoorEditors.Values)
                        editor.Editor.RefreshPaletteChoices();
                    foreach (var section in _openAreaEditors.Values.SelectMany(editor => editor.Sections)
                                 .Where(section => section.BlueprintType == ResourceType.Utd))
                    {
                        section.RefreshPaletteChoices();
                    }
                    break;
                case "soundpalcus":
                    _choiceSets.TryRemove(
                        "sound:" + Domain.Editors.Sounds.SoundChoiceKeys.PaletteCategories,
                        out _);
                    foreach (var editor in _openSoundEditors.Values)
                        editor.Editor.RefreshPaletteChoices();
                    foreach (var section in _openAreaEditors.Values.SelectMany(editor => editor.Sections)
                                 .Where(section => section.BlueprintType == ResourceType.Uts))
                    {
                        section.RefreshPaletteChoices();
                    }
                    break;
                case "triggerpalcus":
                    _choiceSets.TryRemove(
                        "trigger:" + Domain.Editors.Triggers.TriggerChoiceKeys.PaletteCategories,
                        out _);
                    foreach (var editor in _openTriggerEditors.Values)
                        editor.Editor.RefreshPaletteChoices();
                    break;
                case "waypointpalcus":
                    _choiceSets.TryRemove(
                        "waypoint:" + Domain.Editors.Waypoints.WaypointChoiceKeys.PaletteCategories,
                        out _);
                    foreach (var editor in _openWaypointEditors.Values)
                        editor.Editor.RefreshPaletteChoices();
                    break;
                case "itempalcus":
                    _choiceSets.TryRemove(
                        "item:" + Domain.Editors.Items.ItemChoiceKeys.PaletteCategories,
                        out _);
                    foreach (var editor in _openItemEditors.Values)
                        editor.Editor.RefreshPaletteChoices();
                    break;
                case "storepalcus":
                    _choiceSets.TryRemove(
                        "merchant:" + Domain.Editors.Merchants.MerchantChoiceKeys.PaletteCategories,
                        out _);
                    foreach (var editor in _openMerchantEditors.Values)
                        editor.Editor.RefreshPaletteChoices();
                    break;
            }
        }

        private IReadOnlyList<BehaviorChoice> ResolveDoorChoices(string key) =>
            Cached("door", key, BuildDoorChoices);

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> BuildDoorChoices(string key)
        {
            if (key == Domain.Editors.Doors.DoorChoiceKeys.DoorPaletteCategories)
                return ResolveDoorCategories();

            if (key == Domain.Editors.Doors.DoorChoiceKeys.LoadScreens)
                return Domain.Editors.Triggers.LoadScreenCatalog.Read(_twoDaService);

            if (key == Domain.Editors.Doors.DoorChoiceKeys.TrapTypes)
                return Domain.Editors.Triggers.TrapTypeCatalog.Read(_twoDaService);

            if (key == Domain.Editors.Doors.DoorChoiceKeys.Portraits && _portraits != null)
                return ResolvePortraitChoices();

            return _lookups.GetOptions(key)
                .Select(option => new Domain.Editors.Behaviors.BehaviorChoice(option.Id, option.Display))
                .ToList();
        }

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> ResolveDoorCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "doorpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

                return Domain.Editors.Behaviors.PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the door palette categories: {ex.Message}");
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();
            }
        }

        private string? ResolveDoorTag(Domain.Editors.Behaviors.BehaviorTagScope scope, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return null;

            if (scope == Domain.Editors.Behaviors.BehaviorTagScope.Item)
            {
                var itemResRef = workspace.TagIndex.FindItemBlueprintDefiningTag(tag);
                return itemResRef == null ? null : $"item blueprint {itemResRef}";
            }

            if (scope == Domain.Editors.Behaviors.BehaviorTagScope.Waypoint)
            {
                var area = workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utw);
                return area == null ? null : $"waypoint in {area}";
            }

            if (scope == Domain.Editors.Behaviors.BehaviorTagScope.Door)
            {
                var area = workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utd);
                return area == null ? null : $"door in {area}";
            }

            var doorArea = workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utd);
            if (doorArea != null)
                return $"door in {doorArea}";

            var waypointArea = workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utw);
            return waypointArea == null ? null : $"waypoint in {waypointArea}";
        }

        /// <summary>Ambient-sound blueprints open in the behavior editor.</summary>
        private void OpenSoundEditor(string filePath, string resRef)
        {
            var editor = new Sounds.SoundDocumentViewModel(
                filePath,
                resRef,
                _gameCodeIndex,
                _log,
                _prompts,
                ResolveSoundChoices,
                SoundResources(),
                SoundPreviews(),
                _blueprintSaves,
                CreateObjectSource(ResourceType.Uts, resRef));
            editor.Closed += closed => _openSoundEditors.Remove(closed.FilePath);
            editor.CloseRequested += _ => _factory.CloseDocument(editor);
            editor.CatalogEntryChanged += () =>
                _workspaceContext.RefreshCatalogEntry(ResourceType.Uts, editor.ResRef);
            editor.Renamed += (renamed, oldResRef, oldPath) =>
                OnBlueprintRenamed(
                    _openSoundEditors, renamed, ResourceType.Uts,
                    oldResRef, oldPath, renamed.FilePath, renamed.ResRef);
            _openSoundEditors[filePath] = editor;
            _factory.OpenDocument(editor);
        }

        private IReadOnlyList<string> SoundResources() =>
            _soundResources ??= Domain.Editors.Sounds.SoundResourceCatalog.Read(_resourceIndex);

        /// <summary>
        /// The sound preview, one for the session. It owns an output device and plays one thing at a
        /// time on purpose, so every sound list shares it rather than each opening its own.
        /// </summary>
        private Services.SoundPreviewService SoundPreviews() =>
            _soundPreviews ??= new Services.SoundPreviewService(_resourceIndex);

        private IReadOnlyList<BehaviorChoice> ResolveSoundChoices(string key) =>
            Cached("sound", key, BuildSoundChoices);

        private IReadOnlyList<BehaviorChoice> BuildSoundChoices(string key)
        {
            if (key != Domain.Editors.Sounds.SoundChoiceKeys.PaletteCategories)
                return Array.Empty<BehaviorChoice>();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "soundpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<BehaviorChoice>();

                return Domain.Editors.Sounds.SoundPaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the sound palette categories: {ex.Message}");
                return Array.Empty<BehaviorChoice>();
            }
        }

        /// <summary>
        /// Game-data choice sets a trigger row asks for. Most forward to the shared lookup provider,
        /// whose keys these deliberately match; the palette categories come from the module's own
        /// .itp, which no 2DA lookup covers.
        /// </summary>
        private IReadOnlyList<BehaviorChoice> ResolveTriggerChoices(string key) =>
            Cached("trigger", key, BuildTriggerChoices);

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> BuildTriggerChoices(string key)
        {
            if (key == Domain.Editors.Triggers.TriggerChoiceKeys.PaletteCategories)
                return ResolveTriggerCategories();

            // Not routed through the shared lookup: the picker shows each screen's artwork, and the
            // generic 2DA lookup returns labels only.
            if (key == Domain.Editors.Triggers.TriggerChoiceKeys.LoadScreens)
                return Domain.Editors.Triggers.LoadScreenCatalog.Read(_twoDaService);

            if (key == Domain.Editors.Triggers.TriggerChoiceKeys.TrapTypes)
                return Domain.Editors.Triggers.TrapTypeCatalog.Read(_twoDaService);

            return _lookups.GetOptions(key)
                .Select(option => new Domain.Editors.Behaviors.BehaviorChoice(option.Id, option.Display))
                .ToList();
        }

        /// <summary>
        /// The trigger palette's categories, named rather than numbered. A missing or unreadable
        /// palette yields an empty list, which shows as an empty picker instead of a bare id.
        /// </summary>
        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> ResolveTriggerCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "triggerpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

                return Domain.Editors.Behaviors.PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the trigger palette categories: {ex.Message}");
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();
            }
        }

        private IReadOnlyList<BehaviorChoice> ResolveWaypointChoices(string key) =>
            Cached("waypoint", key, BuildWaypointChoices);

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> BuildWaypointChoices(string key)
        {
            if (key == Domain.Editors.Waypoints.WaypointChoiceKeys.PaletteCategories)
                return ResolveWaypointCategories();

            // Not routed through the shared lookup: the picker draws each marker's model, and the
            // generic 2DA lookup returns labels only.
            if (key == Domain.Editors.Waypoints.WaypointChoiceKeys.Appearances)
                return Domain.Editors.Waypoints.WaypointAppearanceCatalog.Read(_waypointAppearances);

            return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();
        }

        private IReadOnlyList<Domain.Editors.Behaviors.BehaviorChoice> ResolveWaypointCategories()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

            try
            {
                var path = Path.Combine(workspace.ModuleRoot, "itp", "waypointpalcus.itp.json");
                if (!File.Exists(path))
                    return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();

                return Domain.Editors.Behaviors.PaletteCategoryReader.Read(
                    Domain.Documents.ItpDocument.Load(path),
                    _tlkService != null ? _tlkService.GetString : null);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Could not read the waypoint palette categories: {ex.Message}");
                return Array.Empty<Domain.Editors.Behaviors.BehaviorChoice>();
            }
        }

        /// <summary>
        /// Names the area a tag lives in for the destination kind selected by a trigger transition.
        /// Stores and the other destination kind deliberately do not satisfy this lookup.
        /// </summary>
        private string? ResolveTriggerTagArea(
            Domain.Editors.Behaviors.BehaviorTagScope scope,
            string tag)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || string.IsNullOrWhiteSpace(tag))
                return null;

            return scope switch
            {
                Domain.Editors.Behaviors.BehaviorTagScope.Waypoint =>
                    workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utw),
                Domain.Editors.Behaviors.BehaviorTagScope.Door =>
                    workspace.TagIndex.FindAreaDefiningTag(tag, ResourceType.Utd),
                _ => null
            };
        }

        /// <summary>Conversations open graph-first, with legacy DLG limited to explicit exceptions.</summary>
        private void OpenConversationEditor(Domain.Workspace.ModuleWorkspace workspace, string resRef)
        {
            var graphPath = workspace.GetConversationGraphPath(resRef);
            var filePath = workspace.GetResourcePath(ResourceType.Dlg, resRef);
            Domain.Conversations.ConversationEditorRoute route;
            try
            {
                route = Domain.Conversations.ConversationEditorRoute.Resolve(resRef, graphPath, filePath);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to inspect conversation {resRef}: {ex}");
                OpenConversationIssue(
                    filePath,
                    resRef,
                    $"Could not inspect '{resRef}'",
                    "The conversation source could not be read.",
                    filePath,
                    ExceptionDetails(ex));
                return;
            }

            if (route.Kind == Domain.Conversations.ConversationEditorRouteKind.NuiGraph)
            {
                if (_openNuiConversations.TryGetValue(graphPath, out var openGraph))
                {
                    _factory.ActivateDocument(openGraph);
                    return;
                }

                try
                {
                    _snippets ??= SnippetCatalog.Build();
                    var graphEditor = new NuiConversationEditorViewModel(
                        graphPath,
                        resRef,
                        _snippets,
                        _gameCodeIndex,
                        _log,
                        _prompts,
                        extension => TagsFor(extension),
                        ChoicePreviews());
                    graphEditor.Closed += _ => _openNuiConversations.Remove(graphPath);
                    graphEditor.CloseRequested += _ => _factory.CloseDocument(graphEditor);
                    _openNuiConversations[graphPath] = graphEditor;
                    _factory.OpenDocument(graphEditor);
                }
                catch (Exception ex)
                {
                    _log.AppendLine($"Failed to open NUI conversation {resRef}: {ex}");
                    OpenConversationIssue(
                        graphPath,
                        resRef,
                        $"Could not open '{resRef}'",
                        "The NUI conversation graph exists, but the editor could not load it.",
                        graphPath,
                        ExceptionDetails(ex));
                }
                return;
            }

            if (route.Kind == Domain.Conversations.ConversationEditorRouteKind.Missing)
            {
                _log.AppendLine($"{resRef}: {route.Reason}");
                OpenConversationIssue(
                    filePath,
                    resRef,
                    $"Could not find '{resRef}'",
                    route.Reason,
                    filePath,
                    route.Details);
                return;
            }

            if (_openConversations.TryGetValue(filePath, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            try
            {
                _snippets ??= SnippetCatalog.Build();
                var editor = new ConversationEditorViewModel(
                    filePath, resRef, _snippets, _gameCodeIndex, _log, _prompts,
                    extension => TagsFor(extension));
                editor.Closed += _ => _openConversations.Remove(filePath);
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CatalogEntryChanged += () =>
                    _workspaceContext.RefreshCatalogEntry(ResourceType.Dlg, resRef);
                _openConversations[filePath] = editor;
                _factory.OpenDocument(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open legacy conversation {resRef}: {ex}");
                OpenConversationIssue(
                    filePath,
                    resRef,
                    $"Could not open '{resRef}'",
                    "The legacy NWN conversation exists, but the editor could not load it.",
                    filePath,
                    ExceptionDetails(ex));
            }
        }

        private void OpenConversationIssue(
            string identity,
            string resRef,
            string headline,
            string message,
            string filePath,
            IReadOnlyList<string> details)
        {
            if (_openConversationIssues.TryGetValue(identity, out var existing))
            {
                _factory.ActivateDocument(existing);
                return;
            }

            var issue = new ConversationOpenIssueViewModel(
                identity,
                resRef,
                headline,
                message,
                filePath,
                details);
            issue.Closed += _ => _openConversationIssues.Remove(identity);
            _openConversationIssues[identity] = issue;
            _factory.OpenDocument(issue);
        }

        private static IReadOnlyList<string> ExceptionDetails(Exception exception) =>
            exception.ToString()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Tags of placed objects of one kind, so a store or waypoint argument names something the
        /// runtime can actually find. Instance overrides win; blank tags fall back to the blueprint.
        /// </summary>
        private IReadOnlyList<string> TagsFor(string extension)
        {
            if (!ResourceTypeExtensions.TryFromExtension(extension, out var type))
                return Array.Empty<string>();

            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<string>();

            return workspace.TagIndex.TagsFor(type).ToList();
        }

        /// <summary>Areas open in the composite editor (.are properties + .git instance lists).</summary>
        private async void OpenAreaEditor(Domain.Workspace.ModuleWorkspace workspace, string resRef)
        {
            if (_openAreaEditors.TryGetValue(resRef, out var existing))
            {
                _factory.ActivateDocument(existing);
                DispatchPendingAreaReveal(existing);
                return;
            }

            if (!_openingAreaEditors.Add(resRef))
                return;

            try
            {
                var arePath = workspace.GetResourcePath(ResourceType.Area, resRef);
                var gitPath = Path.Combine(workspace.ModuleRoot, "git", resRef + ".git.json");
                var gicPath = Path.Combine(workspace.ModuleRoot, "gic", resRef + ".gic.json");
                if (!File.Exists(arePath) || !File.Exists(gitPath) || !File.Exists(gicPath))
                {
                    _log.AppendLine($"Area files not found for '{resRef}' (.are/.git/.gic set required).");
                    return;
                }

                // Both cold operations start together: the module-wide waypoint scan and this
                // area's file read/JSON parse. Neither owns Avalonia's UI thread, so a large area can
                // load while the builder keeps working in another document.
                var transitionTagsTask = GetCurrentTransitionDestinationTagsAsync(workspace);
                var documentLoadTask = Task.Run(() =>
                    _areaDocumentsLoader(arePath, gitPath, gicPath));
                await Task.WhenAll(transitionTagsTask, documentLoadTask).ConfigureAwait(true);
                var transitionDestinationTags = await transitionTagsTask.ConfigureAwait(true);
                var loadedDocuments = await documentLoadTask.ConfigureAwait(true);
                if (transitionDestinationTags == null ||
                    !ReferenceEquals(workspace, _workspaceContext.Workspace))
                    return;

                if (_openAreaEditors.TryGetValue(resRef, out existing))
                {
                    _factory.ActivateDocument(existing);
                    DispatchPendingAreaReveal(existing);
                    return;
                }

                var editor = new AreaEditorViewModel(
                    resRef, workspace, _lookups, _gameCodeIndex, _log,
                    _tilesetCatalog, _tileModelCache, _resourceIndex,
                    _placeableAppearances, _doorTypes, _tileWalkmeshCache, _prompts,
                    ResolveBlueprintName, TryOpenEditor,
                    _tlkService != null ? _tlkService.GetString : null, _waypointAppearances,
                    _previewRenderer != null
                        ? (type, blueprintResRef, useIndexed) =>
                            _previewRenderer.BuildModelResult(type, blueprintResRef, useIndexed)
                        : null,
                    CreateScriptSlotHost($"Area '{resRef}'"),
                    _previewRenderer != null
                        ? instance => _previewRenderer.BuildModel(ResourceType.Utc, instance)
                        : null,
                    new Doors.DoorEditorServices(
                        resRef,
                        ResolveDoorTag,
                        ResolveDoorChoices,
                        DoorAppearances(),
                        _resourceIndex,
                        _previewRenderer != null
                            ? door => _previewRenderer.BuildModelResult(ResourceType.Utd, door)
                            : null,
                        _thumbnails,
                        ChoicePreviews()),
                    new Waypoints.WaypointEditorServices(
                        resRef,
                        new Domain.Editors.Waypoints.WaypointBehaviorCatalog(
                            _gameCodeIndex,
                            transitionDestinationTags),
                        ResolveWaypointChoices,
                        ChoicePreviews()),
                    ResolveSoundChoices,
                    SoundResources(),
                    SoundPreviews(),
                    loadedDocuments,
                    TryEditCopyAndOpenBlueprint,
                    _mutationLock,
                    _areaInstanceClipboard);
                editor.Closed += _ => _openAreaEditors.Remove(resRef);
                editor.TilesetChanged += () => _factory.NotifyActiveAreaChanged();
                editor.CloseRequested += _ => _factory.CloseDocument(editor);
                editor.CatalogEntryChanged += () =>
                    _workspaceContext.RefreshCatalogEntry(ResourceType.Area, resRef);
                editor.PlacementsChanged += _workspaceContext.InvalidateGitIndexes;
                _openAreaEditors[resRef] = editor;
                _factory.OpenDocument(editor);
                DispatchPendingAreaReveal(editor);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Failed to open area editor for {resRef}: {ex.Message}");
            }
            finally
            {
                _openingAreaEditors.Remove(resRef);
                if (!_openAreaEditors.ContainsKey(resRef))
                    _pendingAreaReveals.Remove(resRef);
            }
        }

        /// <summary>
        /// A blueprint's display name from the background catalog, or null while the catalog is still
        /// building or when the resref isn't indexed (hak-provided blueprints are not). Callers fall
        /// back to the resref, which is why this returns null rather than inventing a label.
        /// </summary>
        private string? ResolveBlueprintName(ResourceType? type, string? resRef)
        {
            if (type == null || string.IsNullOrWhiteSpace(resRef))
                return null;

            // A lookup, not a scan: this answers the area editor's selection bar, which asks on
            // every click, and the snapshot holds ~17,900 records.
            if (_workspaceContext.Catalog?.TryGetEntry(type.Value, resRef, out var entry) != true)
                return null;

            return string.IsNullOrWhiteSpace(entry!.Name) ? null : entry.Name;
        }

        /// <summary>
        /// Refuses to open a blueprint whose dropdown-backed fields hold values their lookup tables
        /// cannot represent, reporting exactly which ones instead.
        ///
        /// A combo box can only select values its lookup knows about, so an unknown id renders as a
        /// blank box - the stored value becomes invisible, and touching that field would overwrite
        /// it sight-unseen. Aborting the open leaves the file completely untouched, which is the
        /// safest outcome: the data is preserved and the problem is stated rather than hidden.
        /// Returns true when the blueprint is safe to open.
        /// </summary>
        private bool CanRepresentEveryValue(string filePath, string resRef, EditorSchema schema)
        {
            IReadOnlyList<UnresolvedFieldValue> unresolved;
            try
            {
                var document = JsonGffDocument.Load(filePath);
                unresolved = DropdownValueValidator.FindUnresolved(
                    document, schema,
                    lookupKey => _lookups.GetOptions(lookupKey).Select(option => option.Id).ToHashSet());
            }
            catch (Exception ex)
            {
                // Never let the safety check itself block an otherwise-openable file.
                _log.AppendLine($"Could not validate lookup values for {resRef}: {ex.Message}");
                return true;
            }

            if (unresolved.Count == 0)
                return true;

            var details = unresolved
                .Select(u => $"{u.Label} ({u.FieldName}) = {u.Value}   [not found in {u.LookupKey}]")
                .ToList();

            foreach (var line in details)
                _log.AppendLine($"{resRef}: {line}");

            Shell.Views.ErrorDialog.Show(
                $"Cannot open '{resRef}'",
                "This blueprint stores values that are not present in the game data tables the editor "
                + "uses to fill its dropdowns. Opening it would show those fields as blank, and editing "
                + "anything else could overwrite them without you seeing the original value.\n\n"
                + "The file has NOT been modified. Fix the referenced rows in the 2DA data (or correct "
                + "the blueprint outside the toolset), then reopen it.",
                details);

            return false;
        }

        /// <summary>
        /// The schema a blueprint type opens with, or null when the type has no editor yet.
        /// </summary>
        /// <remarks>
        /// Public because it is the one fact about this class worth asserting from outside: every
        /// type the module explorer lists has to open as something, and a type that quietly falls
        /// through to "No editor available yet" is a blank double-click a builder has to guess at.
        /// </remarks>
        public static EditorSchema? SchemaFor(ResourceType type) => GetSchema(type);

        private static EditorSchema? GetSchema(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utc => UtcSchema.Build(),
                ResourceType.Uti => UtiSchema.Build(),
                ResourceType.Utp => UtpSchema.Build(),
                ResourceType.Utd => UtdSchema.Build(),
                ResourceType.Utw => UtwSchema.Build(),
                ResourceType.Uts => UtsSchema.Build(),
                ResourceType.Utt => UttSchema.Build(),
                ResourceType.Utm => UtmSchema.Build(),
                _ => null
            };
        }
    }
}
