using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell
{
    /// <summary>
    /// Builds the fixed dock layout: Search across the top, Module Explorer / Properties
    /// split left-right in the middle, Output and Validation tabbed together across the bottom.
    /// All tool view models are DI-resolved singletons handed in rather than constructed here, so
    /// the same instances the rest of the app (startup orchestration, the file watcher log) talk
    /// to are the ones docked.
    /// </summary>
    public sealed class ToolsetDockFactory : Factory
    {
        private readonly ModuleExplorerViewModel _explorer;
        private readonly PropertiesViewModel _properties;
        private readonly SearchViewModel _search;
        private readonly OutputViewModel _output;
        private readonly ValidationViewModel _validation;
        private readonly PaletteViewModel _palette;
        private readonly ProblemsViewModel _problems;
        private readonly ScriptReferenceViewModel _scriptReference;
        private readonly AreaContentsViewModel _areaContents;

        /// <summary>Where remembered divider positions come from and go, or null in a test with none.</summary>
        private readonly ToolsetSettings? _settings;

        private IRootDock? _rootDock;
        private DocumentDock? _documentDock;

        public event Action<Document?>? ActiveDocumentChanged;

        /// <summary>Raised when a divider has moved, so the shell can persist the new layout.</summary>
        public event Action? ProportionsChanged;

        public ToolsetDockFactory(
            ModuleExplorerViewModel explorer,
            PropertiesViewModel properties,
            SearchViewModel search,
            OutputViewModel output,
            ValidationViewModel validation,
            PaletteViewModel palette,
            ProblemsViewModel problems,
            ScriptReferenceViewModel scriptReference,
            AreaContentsViewModel areaContents,
            ToolsetSettings? settings = null)
        {
            _areaContents = areaContents;
            _explorer = explorer;
            _properties = properties;
            _search = search;
            _output = output;
            _validation = validation;
            _palette = palette;
            _problems = problems;
            _scriptReference = scriptReference;
            _settings = settings;
        }

        // Dock uses these virtual constructors while splitting, floating, pinning, and otherwise
        // reshaping a layout after startup. Keep those newly-created models under the same non-null
        // theme contract as the fixed layout assembled below.
        public override IRootDock CreateRootDock() => PrepareForThemeBindings(base.CreateRootDock());
        public override IProportionalDock CreateProportionalDock() => PrepareForThemeBindings(base.CreateProportionalDock());
        public override IDockDock CreateDockDock() => PrepareForThemeBindings(base.CreateDockDock());
        public override IStackDock CreateStackDock() => PrepareForThemeBindings(base.CreateStackDock());
        public override IGridDock CreateGridDock() => PrepareForThemeBindings(base.CreateGridDock());
        public override IWrapDock CreateWrapDock() => PrepareForThemeBindings(base.CreateWrapDock());
        public override IUniformGridDock CreateUniformGridDock() => PrepareForThemeBindings(base.CreateUniformGridDock());
        public override IProportionalDockSplitter CreateProportionalDockSplitter() =>
            PrepareForThemeBindings(base.CreateProportionalDockSplitter());
        public override IGridDockSplitter CreateGridDockSplitter() =>
            PrepareForThemeBindings(base.CreateGridDockSplitter());
        public override IToolDock CreateToolDock() => PrepareForThemeBindings(base.CreateToolDock());
        public override IDocumentDock CreateDocumentDock() => PrepareForThemeBindings(base.CreateDocumentDock());
        public override ISplitViewDock CreateSplitViewDock() => PrepareForThemeBindings(base.CreateSplitViewDock());
        public override IDocument CreateDocument() => PrepareForThemeBindings(base.CreateDocument());
        public override ITool CreateTool() => PrepareForThemeBindings(base.CreateTool());

        /// <summary>
        /// The area document in front, when it can accept a placement - what the Palette's Place button
        /// acts on. Null when the active tab is a blueprint editor or nothing is open.
        /// </summary>
        public IAreaPlacementTarget? ActivePlacementTarget =>
            _documentDock?.ActiveDockable as IAreaPlacementTarget;

        public override IRootDock CreateLayout()
        {
            // Area Contents tabs beside Module Contents rather than nesting inside its tree. Aurora
            // put an area's objects under the area itself, in the same tree as the other 442 areas,
            // and that is the thing this layout is deliberately not doing: the two lists answer
            // different questions and neither should be able to bury the other.
            var explorerDock = new ToolDock
            {
                Id = "ExplorerDock",
                ActiveDockable = _explorer,
                VisibleDockables = CreateList<IDockable>(_explorer, _areaContents),
                Alignment = Alignment.Left,
                Proportion = 0.26,
                // Each panel draws its own title; Dock's chrome adds a dotted drag grip and window
                // buttons that appear nowhere in the design.
                GripMode = GripMode.Hidden
            };

            // The right side is the Palette and nothing else. Properties is gone: for an instance the
            // selection bar under the map says what is selected, and for a blueprint you open the thing
            // itself. A read-only field dump docked permanently was the panel it replaced.
            var paletteDock = new ToolDock
            {
                Id = "PaletteDock",
                ActiveDockable = _palette,
                // Script Reference tabs beside the Palette, exactly as Output and Validation share
                // the bottom dock. The Palette lists the front AREA's tileset, so it has nothing to
                // offer while a script is in front — the shell activates whichever fits the tab.
                VisibleDockables = CreateList<IDockable>(_palette, _scriptReference),
                Alignment = Alignment.Right,
                Proportion = 0.25,
                // Each panel draws its own title; Dock's chrome adds a dotted drag grip and window
                // buttons that appear nowhere in the design.
                GripMode = GripMode.Hidden
            };

            _documentDock = new DocumentDock
            {
                Id = "Documents",
                IsCollapsable = false,
                CanCreateDocument = false,
                VisibleDockables = CreateList<IDockable>(),
                Proportion = 0.47
            };
            _documentDock.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(DocumentDock.ActiveDockable))
                    return;

                ActiveDocumentChanged?.Invoke(_documentDock.ActiveDockable as Document);

                // The Tiles palette lists the front area's tileset, so which tab is active is part of
                // its content - two areas on different tilesets offer different tiles.
                _palette.OnActiveAreaChanged();
            };

            var middleLayout = new ProportionalDock
            {
                Id = "MiddleLayout",
                Orientation = Orientation.Horizontal,
                // Proportions must leave slack rather than summing to 1: the splitters between children
                // take real width, and if the children claim all of it the last one is squeezed to
                // nothing - which reads as a missing panel, not a narrow one. These summed to 1.02
                // despite the comment claiming otherwise; 0.26 + 0.47 + 0.25 = 0.98 leaves the room.
                Proportion = 0.72,
                VisibleDockables = CreateList<IDockable>(
                    explorerDock,
                    new ProportionalDockSplitter(),
                    _documentDock,
                    new ProportionalDockSplitter(),
                    paletteDock)
            };

            var outputDock = new ToolDock
            {
                Id = "OutputDock",
                ActiveDockable = _output,
                VisibleDockables = CreateList<IDockable>(_output, _validation, _problems),
                Alignment = Alignment.Bottom,
                Proportion = 0.20,
                // Each panel draws its own title; Dock's chrome adds a dotted drag grip and window
                // buttons that appear nowhere in the design.
                GripMode = GripMode.Hidden
            };

            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Orientation = Orientation.Vertical,
                // No Search dock across the top: it spanned the whole window to serve one panel, and
                // both panels that needed it now have their own box. That row of height goes to the map.
                VisibleDockables = CreateList<IDockable>(
                    middleLayout,
                    new ProportionalDockSplitter(),
                    outputDock)
            };

            var rootDock = CreateRootDock();
            rootDock.Id = "Root";
            rootDock.IsCollapsable = false;
            rootDock.ActiveDockable = mainLayout;
            rootDock.DefaultDockable = mainLayout;
            rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

            _rootDock = rootDock;

            PrepareForThemeBindings(rootDock);

            // The proportions set above are the designed layout; anything the builder dragged last
            // session replaces them before the layout is ever shown, so the window does not open on the
            // defaults and then jump.
            DockProportions.Apply(rootDock, _settings?.DockProportions);
            DockProportions.Watch(rootDock, () => ProportionsChanged?.Invoke());

            return rootDock;
        }

        /// <summary>Where every divider is now, keyed by dock Id.</summary>
        public IReadOnlyDictionary<string, double> CaptureProportions() => DockProportions.Capture(_rootDock);

        /// <summary>Re-reads the front area in the tile-facing panels, after its tileset changed.</summary>
        public void NotifyActiveAreaChanged() => _palette.OnActiveAreaChanged();

        /// <summary>Rebuilds materialized tool-panel values that resolve through the TLK.</summary>
        public void RefreshTlkLabels() => _properties.RefreshTlkLabels();

        /// <summary>Docks an editor document into the Documents area and activates it.</summary>
        public void OpenDocument(Document document)
        {
            if (_documentDock == null)
                return;

            PrepareForThemeBindings(document);
            AddDockable(_documentDock, document);
            ActivateDocument(document);

            // Opening an area shows what is in it. On open only, not on every re-activation: which
            // of the two left-hand panels you want while switching between tabs you already have
            // open is your business, and taking the rail off Module Contents each time would fight
            // whatever you were doing there.
            if (document is Editors.AreaEditorViewModel)
                Focus(_areaContents);
        }

        /// <summary>Brings an already-open editor document to the front.</summary>
        public void ActivateDocument(Document document)
        {
            if (_documentDock == null)
                return;

            SetActiveDockable(document);
            SetFocusedDockable(_documentDock, document);
        }

        /// <summary>Brings a tool panel to the front.</summary>
        public void Focus(IDockable dockable)
        {
            SetActiveDockable(dockable);
            if (dockable.Owner is IDock owner)
                SetFocusedDockable(owner, dockable);
        }

        /// <summary>Brings Area Contents to the front for an explicit Source-tab Go To.</summary>
        public void ShowAreaContents() => Focus(_areaContents);

        /// <summary>Requests that Dock close a document after the editor has approved any prompt.</summary>
        public void CloseDocument(Document document)
        {
            CloseDockable(document);
        }

        /// <summary>
        /// Brings the right dock's Script Reference or Palette to the front, whichever suits the tab
        /// that just became active. Called from the same ActiveDocumentChanged hook that already tells
        /// the Palette which area is in front.
        /// </summary>
        public void ShowRightTool(bool scriptReference)
        {
            var target = scriptReference ? (IDockable)_scriptReference : _palette;
            if (target.Owner is IDock dock && dock.ActiveDockable != target)
                SetActiveDockable(target);
        }

        /// <summary>Brings the Problems panel to the front of the bottom dock.</summary>
        public void ShowProblems() => SetActiveDockable(_problems);

        /// <summary>
        /// Supplies Dock's optional capability objects before its Fluent theme observes the model.
        /// Empty objects preserve Dock's normal inheritance semantics: every nullable capability inside
        /// them still falls through to the dock, root, and legacy flags. The theme nevertheless walks
        /// through the objects directly, so leaving either container null produces a binding warning for
        /// every drag, drop, pin, and close check on every visible pane or tab.
        /// </summary>
        private static T PrepareForThemeBindings<T>(T root) where T : IDockable
        {
            foreach (var dockable in DockProportions.Walk(root))
            {
                dockable.DockCapabilityOverrides ??= new DockCapabilityOverrides();

                if (dockable is IDock dock)
                    dock.DockCapabilityPolicy ??= new DockCapabilityPolicy();

                if (dockable is IRootDock rootDock)
                    rootDock.RootDockCapabilityPolicy ??= new DockCapabilityPolicy();
            }

            return root;
        }

        public override void InitLayout(IDockable layout)
        {
            ContextLocator = new Dictionary<string, Func<object?>>
            {
                [_explorer.Id] = () => _explorer,
                [_properties.Id] = () => _properties,
                [_search.Id] = () => _search,
                [_output.Id] = () => _output,
                [_validation.Id] = () => _validation,
                [_palette.Id] = () => _palette,
                [_problems.Id] = () => _problems,
                [_scriptReference.Id] = () => _scriptReference,
                [_areaContents.Id] = () => _areaContents
            };

            DockableLocator = new Dictionary<string, Func<IDockable?>>
            {
                ["Root"] = () => _rootDock
            };

            HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
            {
                [nameof(IDockWindow)] = () => new HostWindow()
            };

            base.InitLayout(layout);
        }
    }
}
