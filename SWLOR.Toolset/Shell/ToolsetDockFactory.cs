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
            ToolsetSettings? settings = null)
        {
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

        /// <summary>
        /// The area document in front, when it can accept a placement - what the Palette's Place button
        /// acts on. Null when the active tab is a blueprint editor or nothing is open.
        /// </summary>
        public IAreaPlacementTarget? ActivePlacementTarget =>
            _documentDock?.ActiveDockable as IAreaPlacementTarget;

        public override IRootDock CreateLayout()
        {
            var explorerDock = new ToolDock
            {
                Id = "ExplorerDock",
                ActiveDockable = _explorer,
                VisibleDockables = CreateList<IDockable>(_explorer),
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

        /// <summary>Docks an editor document into the Documents area and activates it.</summary>
        public void OpenDocument(Document document)
        {
            if (_documentDock == null)
                return;

            AddDockable(_documentDock, document);
            ActivateDocument(document);
        }

        /// <summary>Brings an already-open editor document to the front.</summary>
        public void ActivateDocument(Document document)
        {
            if (_documentDock == null)
                return;

            SetActiveDockable(document);
            SetFocusedDockable(_documentDock, document);
        }

        /// <summary>Brings a tool panel to the front - what the View and Tools menus act on.</summary>
        public void Focus(IDockable dockable)
        {
            SetActiveDockable(dockable);
            if (dockable.Owner is IDock owner)
                SetFocusedDockable(owner, dockable);
        }

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
                [_scriptReference.Id] = () => _scriptReference
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
