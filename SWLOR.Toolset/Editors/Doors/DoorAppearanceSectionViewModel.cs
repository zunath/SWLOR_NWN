using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>
    /// Searchable, paged door appearance browser. The Door editor continues to own the retained 3D
    /// preview; this section supplies the fast thumbnail grid used to choose what that preview shows.
    /// </summary>
    public sealed partial class DoorAppearanceSectionViewModel : ObservableObject, IDisposable
    {
        private const int PageSize = 48;
        private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);

        private readonly DoorValueStore _store;
        private readonly IReadOnlyList<DoorAppearanceChoice> _choices;
        private readonly ThumbnailService? _thumbnails;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action _changed;
        private List<DoorAppearanceChoice> _matches = new();
        private CancellationTokenSource? _searchDebounce;
        private int _published;
        private bool _loading;
        private bool _disposed;

        public ObservableCollection<DoorAppearanceTileViewModel> Tiles { get; } = new();

        [ObservableProperty]
        private string _query = string.Empty;

        [ObservableProperty]
        private DoorAppearanceTileViewModel? _highlighted;

        public DoorAppearanceChoice? Current => _store.GetAppearance(_choices);

        public string CurrentDescription => Current?.Display ?? "Unknown appearance";

        public bool CurrentIsUnknown => Current == null;

        public string MatchSummary
        {
            get
            {
                if (_matches.Count == 0)
                    return "No models match";

                return _published >= _matches.Count
                    ? $"{_matches.Count} model{(_matches.Count == 1 ? string.Empty : "s")}"
                    : $"{_published} of {_matches.Count} models";
            }
        }

        public bool CanLoadMore => _published < _matches.Count;

        public DoorAppearanceSectionViewModel(
            DoorValueStore store,
            IReadOnlyList<DoorAppearanceChoice> choices,
            ThumbnailService? thumbnails,
            Func<string, Action, bool> runEdit,
            Action changed)
        {
            _store = store;
            _choices = choices;
            _thumbnails = thumbnails;
            _runEdit = runEdit;
            _changed = changed;
            Rebuild();
        }

        [RelayCommand]
        private void LoadMore() => PublishPage();

        public void ReloadFromDocument()
        {
            _loading = true;
            try
            {
                Highlighted = null;
                var current = Current;
                foreach (var tile in Tiles)
                    tile.IsCurrent = SameAppearance(tile.Choice, current);
            }
            finally
            {
                _loading = false;
            }

            NotifyCurrentChanged();
        }

        partial void OnQueryChanged(string value)
        {
            _searchDebounce?.Cancel();
            _searchDebounce?.Dispose();
            _searchDebounce = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                Rebuild();
                return;
            }

            var pending = new CancellationTokenSource();
            _searchDebounce = pending;
            Task.Delay(SearchDebounce, pending.Token).ContinueWith(
                task =>
                {
                    if (!task.IsCanceled)
                        Avalonia.Threading.Dispatcher.UIThread.Post(Rebuild);
                },
                TaskScheduler.Default);
        }

        partial void OnHighlightedChanged(DoorAppearanceTileViewModel? value)
        {
            if (_loading || value == null || SameAppearance(value.Choice, Current))
                return;

            if (!_runEdit(
                    $"Change appearance to {value.Caption}",
                    () => _store.SetAppearance(value.Choice)))
            {
                ReloadFromDocument();
                return;
            }

            foreach (var tile in Tiles)
                tile.IsCurrent = SameAppearance(tile.Choice, value.Choice);

            NotifyCurrentChanged();
            _changed();
        }

        private void Rebuild()
        {
            if (_disposed)
                return;

            var words = (Query ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _matches = _choices
                .Where(choice => words.All(word =>
                    choice.Display.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    (choice.Model?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false)))
                .ToList();

            _published = 0;
            Tiles.Clear();
            PublishPage();
        }

        private void PublishPage()
        {
            if (_disposed)
                return;

            var current = Current;
            var end = Math.Min(_published + PageSize, _matches.Count);
            for (var index = _published; index < end; index++)
            {
                var choice = _matches[index];
                var tile = new DoorAppearanceTileViewModel(
                    choice,
                    SameAppearance(choice, current));
                Tiles.Add(tile);
                RequestPreview(tile);
            }

            _published = end;
            OnPropertyChanged(nameof(MatchSummary));
            OnPropertyChanged(nameof(CanLoadMore));
        }

        private void RequestPreview(DoorAppearanceTileViewModel tile)
        {
            if (_thumbnails == null || string.IsNullOrWhiteSpace(tile.ModelName))
                return;

            var cached = _thumbnails.CachedTile(tile.ModelName);
            if (cached != null)
            {
                tile.Preview = cached;
                return;
            }

            _thumbnails.RequestTileAsync(tile.ModelName, bitmap => tile.Preview = bitmap);
        }

        private void NotifyCurrentChanged()
        {
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(CurrentDescription));
            OnPropertyChanged(nameof(CurrentIsUnknown));
        }

        private static bool SameAppearance(
            DoorAppearanceChoice left,
            DoorAppearanceChoice? right) =>
            right != null && left.Kind == right.Kind && left.Id == right.Id;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _searchDebounce?.Cancel();
            _searchDebounce?.Dispose();
            _searchDebounce = null;
        }
    }
}
