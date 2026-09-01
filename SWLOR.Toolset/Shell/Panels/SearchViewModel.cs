using System.Collections.ObjectModel;
using Avalonia.Threading;
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
        /// <summary>Results published. Nobody reads past the first screen of a ranked list.</summary>
        public const int MaxResults = 200;

        /// <summary>How long typing has to pause before the catalog is searched.</summary>
        private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(200);

        private readonly WorkspaceContext _workspaceContext;
        private readonly PropertiesViewModel _properties;
        private readonly OutputLogService _log;
        private CancellationTokenSource? _pending;

        [ObservableProperty]
        private string _query = string.Empty;

        [ObservableProperty]
        private CatalogSearchResult? _selectedResult;

        public ObservableCollection<CatalogSearchResult> Results { get; } = new();

        public SearchViewModel(WorkspaceContext workspaceContext, PropertiesViewModel properties, OutputLogService log)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            Id = "Search";
            Title = "Search";
            _workspaceContext.CatalogEntriesChanged += (_, _) => Refresh();
            _workspaceContext.CatalogLabelsChanged += Refresh;
        }

        /// <summary>
        /// Searches after typing stops, off the UI thread.
        /// </summary>
        /// <remarks>
        /// Each search walks the whole indexed corpus - ~17,900 records for this module - and a
        /// one-letter query matches most of it. Doing that inline on every keystroke put tens of
        /// milliseconds of string comparison between the key and the character appearing.
        /// </remarks>
        partial void OnQueryChanged(string value)
        {
            _pending?.Cancel();
            _pending?.Dispose();
            _pending = null;

            var catalog = _workspaceContext.Catalog;
            if (catalog == null || string.IsNullOrWhiteSpace(value))
            {
                Results.Clear();
                return;
            }

            var pending = new CancellationTokenSource();
            _pending = pending;
            var token = pending.Token;

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(SearchDebounce, token).ConfigureAwait(false);
                        var matches = catalog.Search(value, MaxResults);
                        if (token.IsCancellationRequested)
                            return;

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested)
                                return;

                            Results.Clear();
                            foreach (var result in matches)
                                Results.Add(result);
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        // Another keystroke arrived; its search is the one that matters.
                    }
                    catch (Exception ex)
                    {
                        PublishSearchFailure(ex, token);
                    }
                },
                token);
        }

        /// <summary>
        /// Surfaces a scan failure the way the sibling script search does: clear whatever stale
        /// results are on screen and log why, rather than leaving a result set nobody can trust.
        /// </summary>
        private void PublishSearchFailure(Exception ex, CancellationToken token)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                Results.Clear();
                _log.AppendLine($"Search failed: {ex.Message}");
            });
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
