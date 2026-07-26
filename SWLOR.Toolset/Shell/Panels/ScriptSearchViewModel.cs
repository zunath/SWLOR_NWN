using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>Finds text or identifiers across every module script.</summary>
    public partial class ScriptSearchViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly Func<EditorService> _editorService;

        public ScriptSearchViewModel(WorkspaceContext workspaceContext, Func<EditorService> editorService)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
            Id = "ScriptSearch";
            Title = "Find Scripts";
        }

        [ObservableProperty]
        private string _query = string.Empty;

        [ObservableProperty]
        private bool _identifierOnly = true;

        [ObservableProperty]
        private ScriptSearchResult? _selectedResult;

        public ObservableCollection<ScriptSearchResult> Results { get; } = new();

        public string Summary => Results.Count == 0
            ? "No results"
            : $"{Results.Count} result(s)";

        partial void OnQueryChanged(string value) => Search();

        partial void OnIdentifierOnlyChanged(bool value) => Search();

        [RelayCommand]
        private void Search()
        {
            Results.Clear();

            var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
            if (moduleRoot == null || string.IsNullOrWhiteSpace(Query))
            {
                RaiseSummary();
                return;
            }

            var mode = IdentifierOnly ? ScriptSearchMode.Identifier : ScriptSearchMode.Substring;
            var search = new ScriptWorkspaceSearch(Path.Combine(moduleRoot, "nss"));
            foreach (var result in search.Search(Query, mode).Take(500))
                Results.Add(result);

            RaiseSummary();
        }

        [RelayCommand]
        private void Navigate(ScriptSearchResult? result)
        {
            if (result == null)
                return;

            _editorService().NavigateToScriptLine(result.ResRef, result.Line);
        }

        private void RaiseSummary()
        {
            OnPropertyChanged(nameof(Summary));
            Title = Results.Count == 0 ? "Find Scripts" : $"Find Scripts {Results.Count}";
        }
    }
}
