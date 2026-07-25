using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
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
        private readonly ModelPreviewViewModel _modelPreview;
        private readonly PaletteViewModel _palette;
        private IRootDock? _rootDock;
        private DocumentDock? _documentDock;

        public event Action<Document?>? ActiveDocumentChanged;

        public ToolsetDockFactory(
            ModuleExplorerViewModel explorer,
            PropertiesViewModel properties,
            SearchViewModel search,
            OutputViewModel output,
            ValidationViewModel validation,
            ModelPreviewViewModel modelPreview,
            PaletteViewModel palette)
        {
            _explorer = explorer;
            _properties = properties;
            _search = search;
            _output = output;
            _validation = validation;
            _modelPreview = modelPreview;
            _palette = palette;
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
                Proportion = 0.28
            };

            // The Palette leads on the right - it is the panel a builder works out of - with Properties
            // and Model Preview behind it as reference tabs.
            var propertiesDock = new ToolDock
            {
                Id = "PropertiesDock",
                ActiveDockable = _palette,
                VisibleDockables = CreateList<IDockable>(_palette, _properties, _modelPreview),
                Alignment = Alignment.Right,
                Proportion = 0.27
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
                if (e.PropertyName == nameof(DocumentDock.ActiveDockable))
                    ActiveDocumentChanged?.Invoke(_documentDock.ActiveDockable as Document);
            };

            var middleLayout = new ProportionalDock
            {
                Id = "MiddleLayout",
                Orientation = Orientation.Horizontal,
                Proportion = 0.72,
                VisibleDockables = CreateList<IDockable>(
                    explorerDock,
                    new ProportionalDockSplitter(),
                    _documentDock,
                    new ProportionalDockSplitter(),
                    propertiesDock)
            };

            var searchDock = new ToolDock
            {
                Id = "SearchDock",
                ActiveDockable = _search,
                VisibleDockables = CreateList<IDockable>(_search),
                Alignment = Alignment.Top,
                Proportion = 0.08
            };

            var outputDock = new ToolDock
            {
                Id = "OutputDock",
                ActiveDockable = _output,
                VisibleDockables = CreateList<IDockable>(_output, _validation),
                Alignment = Alignment.Bottom,
                Proportion = 0.20
            };

            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>(
                    searchDock,
                    new ProportionalDockSplitter(),
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
            return rootDock;
        }

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

        /// <summary>Requests that Dock close a document after the editor has approved any prompt.</summary>
        public void CloseDocument(Document document)
        {
            CloseDockable(document);
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
                [_modelPreview.Id] = () => _modelPreview,
                [_palette.Id] = () => _palette
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
