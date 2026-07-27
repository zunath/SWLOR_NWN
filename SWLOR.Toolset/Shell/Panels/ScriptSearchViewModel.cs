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
        private readonly Func<string, string?>? _sourceOverlay;

        /// <summary>Cancels the scan in flight when the query changes underneath it.</summary>
        private CancellationTokenSource? _pending;

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

        /// <summary>True while a background scan is in flight, so the panel can say so - and tests can wait it out.</summary>
        [ObservableProperty]
        private bool _isSearching;

        public ObservableCollection<ScriptSearchResult> Results { get; } = new();

        public string Summary => Results.Count == 0
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
                IsSearching = false;
                RaiseSummary();
                return;
            }

            var pending = new CancellationTokenSource();
            _pending = pending;
            var token = pending.Token;
            IsSearching = true;

            // Captured now rather than read from the properties inside the background task: another
            // keystroke replaces both before the debounce elapses, and this scan's token is cancelled
            // when that happens, but a defensive snapshot costs nothing.
            var query = Query;
            var mode = IdentifierOnly ? ScriptSearchMode.Identifier : ScriptSearchMode.Substring;

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(SearchDebounce, token).ConfigureAwait(false);

                        var search = new ScriptWorkspaceSearch(_scriptRoot, _sourceOverlay);
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
                },
                token);
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
