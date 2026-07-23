using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The Search panel: a query box over <see cref="BlueprintCatalog.Search"/>, with virtualized
    /// ranked results. Selecting a result shows it in the Properties panel. Safe to search while
    /// the catalog is still being built in the background - it just searches whatever has been
    /// indexed so far, and results improve as the build progresses.
    /// </summary>
    public partial class SearchViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly PropertiesViewModel _properties;

        [ObservableProperty]
        private string _query = string.Empty;

        [ObservableProperty]
        private CatalogSearchResult? _selectedResult;

        public ObservableCollection<CatalogSearchResult> Results { get; } = new();

        public SearchViewModel(WorkspaceContext workspaceContext, PropertiesViewModel properties)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Id = "Search";
            Title = "Search";
            _workspaceContext.CatalogEntryRefreshed += (_, _) => Refresh();
        }

        partial void OnQueryChanged(string value)
        {
            Results.Clear();

            var catalog = _workspaceContext.Catalog;
            if (catalog == null || string.IsNullOrWhiteSpace(value))
                return;

            foreach (var result in catalog.Search(value).Take(200))
                Results.Add(result);
        }

        /// <summary>Re-runs the current query after the background catalog publishes more entries.</summary>
        public void Refresh() => OnQueryChanged(Query);

        partial void OnSelectedResultChanged(CatalogSearchResult? value)
        {
            if (value != null)
                _properties.ShowEntry(value.Entry);
        }
    }
}
