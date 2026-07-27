using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Script;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>Finds text or identifiers across every module script from a script editor.</summary>
    public partial class ScriptSearchViewModel : ObservableObject
    {
        private readonly string _scriptRoot;
        private readonly Action<string, int> _navigate;
        private readonly Func<string, string?>? _sourceOverlay;

        public ScriptSearchViewModel(
            string scriptRoot,
            Action<string, int> navigate,
            Func<string, string?>? sourceOverlay = null)
        {
            _scriptRoot = scriptRoot ?? throw new ArgumentNullException(nameof(scriptRoot));
            _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
            _sourceOverlay = sourceOverlay;
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

            if (string.IsNullOrWhiteSpace(Query))
            {
                RaiseSummary();
                return;
            }

            var mode = IdentifierOnly ? ScriptSearchMode.Identifier : ScriptSearchMode.Substring;
            var search = new ScriptWorkspaceSearch(_scriptRoot, _sourceOverlay);
            foreach (var result in search.Search(Query, mode).Take(500))
                Results.Add(result);

            RaiseSummary();
        }

        [RelayCommand]
        private void Navigate(ScriptSearchResult? result)
        {
            if (result == null)
                return;

            _navigate(result.ResRef, result.Line);
        }

        private void RaiseSummary()
        {
            OnPropertyChanged(nameof(Summary));
        }
    }
}
