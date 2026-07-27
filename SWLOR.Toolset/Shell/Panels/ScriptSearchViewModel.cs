using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Script;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>Finds text or identifiers across every module script from a script editor.</summary>
    public partial class ScriptSearchViewModel : ObservableObject
    {
        /// <summary>How long typing has to pause before the script corpus is read.</summary>
        private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);

        private readonly string _scriptRoot;
        private readonly Action<string, int> _navigate;

        /// <summary>
        /// Produces a fresh resref-to-text snapshot of every open script buffer, or null when no
        /// overlay is wired (e.g. in tests). Called once per scan, on the UI thread, before
        /// <c>Task.Run</c> - see <see cref="Search"/>.
        /// </summary>
        private readonly Func<IReadOnlyDictionary<string, string>>? _sourceSnapshot;

        /// <summary>Cancels the scan in flight when the query changes underneath it.</summary>
        private CancellationTokenSource? _pending;

        /// <summary>Set when the last scan failed outright, so <see cref="Summary"/> can say why.</summary>
        private string? _searchError;

        public ScriptSearchViewModel(
            string scriptRoot,
            Action<string, int> navigate,
            Func<IReadOnlyDictionary<string, string>>? sourceSnapshot = null)
        {
            _scriptRoot = scriptRoot ?? throw new ArgumentNullException(nameof(scriptRoot));
            _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
            _sourceSnapshot = sourceSnapshot;
        }

        [ObservableProperty]
        private string _query = string.Empty;

        [ObservableProperty]
        private bool _identifierOnly = true;

        [ObservableProperty]
        private ScriptSearchResult? _selectedResult;

        /// <summary>True while a background scan is in flight, so the panel can say so - and tests can wait it out.</summary>
        [ObservableProperty]
        private bool _isSearching;

        public ObservableCollection<ScriptSearchResult> Results { get; } = new();

        public string Summary => _searchError != null
            ? _searchError
            : Results.Count == 0
                ? "No results"
                : $"{Results.Count} result(s)";

        partial void OnQueryChanged(string value) => Search();

        partial void OnIdentifierOnlyChanged(bool value) => Search();

        /// <summary>
        /// Schedules a debounced, cancellable background scan of the script corpus.
        /// </summary>
        /// <remarks>
        /// This used to enumerate, read, and lex every module script inline on the UI thread for every
        /// keystroke - about 1.1 MiB across 87 files re-read per character typed, which could stall the
        /// whole editor. Now the keystroke only (re)schedules a scan: it cancels whatever scan is still
        /// in flight, waits out the debounce off the UI thread, and only then reads/lexes the corpus -
        /// applying the result set back on the UI thread only if nothing superseded it in the meantime.
        /// <para>
        /// The open-script overlay is snapshotted here too, before <c>Task.Run</c> - not read live by
        /// the worker. A builder opening or closing a script tab while the scan is in flight mutates
        /// the live open-editors map concurrently with the worker's reads, which can fault the scan;
        /// the worker instead reads only the immutable resref-to-text copy captured on this thread.
        /// </para>
        /// </remarks>
        [RelayCommand]
        private void Search()
        {
            _pending?.Cancel();
            _pending?.Dispose();
            _pending = null;

            if (string.IsNullOrWhiteSpace(Query))
            {
                Results.Clear();
                _searchError = null;
                IsSearching = false;
                RaiseSummary();
                return;
            }

            var pending = new CancellationTokenSource();
            _pending = pending;
            var token = pending.Token;
            IsSearching = true;
            _searchError = null;

            // Captured now rather than read from the properties inside the background task: another
            // keystroke replaces both before the debounce elapses, and this scan's token is cancelled
            // when that happens, but a defensive snapshot costs nothing.
            var query = Query;
            var mode = IdentifierOnly ? ScriptSearchMode.Identifier : ScriptSearchMode.Substring;

            // Materialized now, on the UI thread, rather than handed to the worker as a live lookup
            // into the open-editors dictionary - see the remarks above.
            var openScripts = _sourceSnapshot?.Invoke();
            string? Overlay(string resRef) =>
                openScripts != null && openScripts.TryGetValue(resRef, out var text) ? text : null;

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(SearchDebounce, token).ConfigureAwait(false);

                        var search = new ScriptWorkspaceSearch(_scriptRoot, Overlay);
                        var matches = search.Search(query, mode).Take(500).ToList();

                        if (token.IsCancellationRequested)
                            return;

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested)
                                return;

                            Results.Clear();
                            foreach (var result in matches)
                                Results.Add(result);

                            IsSearching = false;
                            RaiseSummary();
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
        /// Surfaces a scan failure the way the sibling dialogue-text search does: stop saying
        /// "searching", clear whatever stale results are on screen, and say why instead of leaving
        /// <see cref="IsSearching"/> stuck true with a result set nobody can trust any more.
        /// </summary>
        private void PublishSearchFailure(Exception ex, CancellationToken token)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                Results.Clear();
                _searchError = $"Search failed: {ex.Message}";
                IsSearching = false;
                RaiseSummary();
            });
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
