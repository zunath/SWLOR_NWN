using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Appearance
{
    /// <summary>
    /// A searchable, paged grid of appearance choices with a picture on every tile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One section for every editor that picks an appearance. The door editor and the placeable
    /// editor had arrived at the same design independently — search box, debounce, 48 tiles a page,
    /// thumbnails requested per published tile — and the creature editor had nothing at all, which
    /// left the most visual choice in the module as a drop-down of names.
    /// </para>
    /// <para>
    /// Two things keep it fast on a table with thousands of rows. Filtering waits for typing to
    /// stop, because each rebuild throws away every published tile and realizes a fresh page. And
    /// the grid is never handed the whole result set: previews are requested only for tiles that
    /// have actually been published, so a builder who does not scroll never pays to render what
    /// they did not look at.
    /// </para>
    /// </remarks>
    public sealed partial class AppearanceGallerySectionViewModel : ObservableObject, IDisposable
    {
        /// <summary>
        /// Tiles added per page. Small on purpose: each one is a control realized and a render
        /// queued, so a large page is a visible stall mid-scroll.
        /// </summary>
        private const int PageSize = 48;

        /// <summary>How long typing has to pause before the grid re-filters.</summary>
        private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);
        private const int MaxPreviewAttempts = 5;

        private IReadOnlyList<AppearanceOption> _options;
        private readonly ThumbnailService? _thumbnails;
        private readonly Func<string> _currentKey;

        /// <summary>Applies a pick; false when the edit was refused, which puts the grid back.</summary>
        private readonly Func<AppearanceOption, bool> _apply;

        private readonly string _noun;
        private List<AppearanceOption> _matches = new();
        private readonly HashSet<AppearanceTileViewModel> _realizedTiles = new();
        private readonly Dictionary<AppearanceTileViewModel, int> _previewAttempts = new();
        private CancellationTokenSource? _searchDebounce;
        private int _published;
        private bool _loading;
        private bool _disposed;

        [ObservableProperty]
        private ObservableCollection<AppearanceTileViewModel> _tiles = new();

        [ObservableProperty]
        private string _query = string.Empty;

        [ObservableProperty]
        private AppearanceTileViewModel? _highlighted;

        /// <summary>What the object's stored appearance is called right now.</summary>
        public string CurrentDescription =>
            _options.FirstOrDefault(option => option.Key == _currentKey()) is { } current
                ? current.Detail is { Length: > 0 } detail
                    ? $"{current.Caption}  ·  {detail}"
                    : current.Caption
                : $"{_currentKey()} — not in the table";

        /// <summary>
        /// True when what is stored is not one of the offered rows. Said rather than hidden: the
        /// value is kept exactly as it was, and a blank picker reads as data loss.
        /// </summary>
        public bool CurrentIsUnknown => _options.All(option => option.Key != _currentKey());

        public string MatchSummary
        {
            get
            {
                if (_matches.Count == 0)
                    return $"No {_noun}s match";

                return _published >= _matches.Count
                    ? $"{_matches.Count} {_noun}{(_matches.Count == 1 ? string.Empty : "s")}"
                    : $"{_published} of {_matches.Count} {_noun}s";
            }
        }

        public bool CanLoadMore => _published < _matches.Count;

        public string SearchWatermark => $"Search {_noun}s by name or ResRef";

        /// <summary>
        /// Tile edge in pixels. The placeable grid packs 24,000 models and wants them small; the
        /// door and creature grids have hundreds and can afford a picture worth judging.
        /// </summary>
        public double TileSize { get; }

        /// <summary>Picture height inside a tile, kept proportional to <see cref="TileSize"/>.</summary>
        public double TileImageHeight => TileSize * 0.73;

        public AppearanceGallerySectionViewModel(
            IReadOnlyList<AppearanceOption> options,
            ThumbnailService? thumbnails,
            Func<string> currentKey,
            Func<AppearanceOption, bool> apply,
            string noun = "model",
            double tileSize = 112)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _thumbnails = thumbnails;
            _currentKey = currentKey ?? throw new ArgumentNullException(nameof(currentKey));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _noun = noun;
            TileSize = tileSize;

            Rebuild();
        }

        [RelayCommand]
        private void LoadMore() => PublishPage();

        /// <summary>
        /// Replaces the rows the grid offers, keeping whatever is typed in the search box.
        /// </summary>
        /// <remarks>
        /// For the filters that change the set rather than the query — the placeable tab's "used in
        /// module" and "named only". A filter that narrowed the search text instead would hide the
        /// builder's own words from them.
        /// </remarks>
        public void SetOptions(IReadOnlyList<AppearanceOption> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _options = options;
            Rebuild();
            NotifyCurrentChanged();
        }

        /// <summary>Re-reads the stored appearance after a save, an undo, or an external reload.</summary>
        public void ReloadFromDocument()
        {
            _loading = true;
            try
            {
                Highlighted = null;
                var current = _currentKey();
                foreach (var tile in Tiles)
                    tile.IsCurrent = tile.Option.Key == current;
            }
            finally
            {
                _loading = false;
            }

            NotifyCurrentChanged();
        }

        /// <summary>
        /// Re-requests pictures for the published page after game resources become available or
        /// the HAK stack changes. A gallery can be constructed while resources are still loading;
        /// those initial requests are deliberately no-ops and therefore need this retry.
        /// </summary>
        public void ReloadPreviews()
        {
            if (_disposed)
                return;

            foreach (var tile in Tiles)
            {
                var wasRealized = _realizedTiles.Contains(tile);
                tile.Preview = null;
                tile.PreviewRequested = false;

                // Retry only cells the view had already realized. Re-rendering the entire published
                // page here would undo the same progressive-loading rule used by the palette.
                if (wasRealized)
                    EnsurePreview(tile);
            }
        }

        /// <summary>
        /// Fetches a tile's preview unless it already has one or has already asked. The view calls
        /// this when the shared virtualizing panel realizes the cell, just as the palette does.
        /// </summary>
        public void EnsurePreview(AppearanceTileViewModel? tile)
        {
            if (_disposed || tile == null || _thumbnails == null)
                return;

            _realizedTiles.Add(tile);
            if (tile.PreviewRequested)
                return;

            if (tile.Option.CreatureAppearanceId is { } appearanceId)
            {
                _previewAttempts.TryGetValue(tile, out var attempts);
                _previewAttempts[tile] = attempts + 1;
                tile.PreviewRequested = _thumbnails.RequestAppearanceAsync(
                    appearanceId,
                    bitmap =>
                    {
                        if (!_disposed && Tiles.Contains(tile))
                        {
                            tile.Preview = bitmap;
                            _previewAttempts.Remove(tile);
                        }
                    },
                    () =>
                    {
                        // A render can race resource-index initialization or fail on one malformed
                        // model. Keep the realized cell retryable instead of pinning its letter glyph
                        // for the lifetime of the editor.
                        if (!_disposed && Tiles.Contains(tile) && tile.Preview == null)
                        {
                            tile.PreviewRequested = false;
                            QueuePreviewRetry(tile);
                        }
                    },
                    // A realized segmented row is a visible generic creature choice, not background
                    // prefetch. Queue it with every other visible tile so the dynamic race models do
                    // not remain behind placeholders while newer non-segmented work overtakes them.
                    tile.IsCurrent
                        ? AppearancePreviewPriority.Selected
                        : AppearancePreviewPriority.Visible);
                if (!tile.PreviewRequested)
                    QueuePreviewRetry(tile);
                return;
            }

            if (!string.IsNullOrWhiteSpace(tile.Option.ModelResRef))
            {
                tile.PreviewRequested = true;
                _thumbnails.RequestTileAsync(
                    tile.Option.ModelResRef,
                    bitmap => tile.Preview = bitmap,
                    renderDoorTransitionFallback: tile.Option.IsDoorTransition);
            }
        }

        partial void OnQueryChanged(string value)
        {
            _searchDebounce?.Cancel();
            _searchDebounce?.Dispose();
            _searchDebounce = null;

            // Clearing the box is a search being abandoned, not one being typed. Waiting out the
            // debounce for it leaves the old results sitting there looking like the filter stuck.
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
                        Dispatcher.UIThread.Post(Rebuild);
                },
                TaskScheduler.Default);
        }

        partial void OnHighlightedChanged(AppearanceTileViewModel? value)
        {
            // Highlighting a tile IS the pick. A confirm button in between only asks a builder to
            // say twice what they already said once, and undo is the real safety net either way.
            if (_loading || value == null || value.Option.Key == _currentKey())
                return;

            if (!_apply(value.Option))
            {
                ReloadFromDocument();
                return;
            }

            foreach (var tile in Tiles)
                tile.IsCurrent = tile.Option.Key == value.Option.Key;

            NotifyCurrentChanged();
        }

        private void Rebuild()
        {
            if (_disposed)
                return;

            var words = (Query ?? string.Empty)
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            _matches = _options
                .Where(option => words.All(word =>
                    option.SearchText.Contains(word, StringComparison.Ordinal)))
                .ToList();

            _realizedTiles.Clear();
            _previewAttempts.Clear();

            // Replace the first page as one collection notification. Adding 48 cells one at a time
            // makes Avalonia repeat measure/arrange while the tab is becoming visible and was the
            // largest UI-thread cost of opening the creature Appearance tab.
            _published = Math.Min(PageSize, _matches.Count);
            var current = _currentKey();
            var firstPage = new List<AppearanceTileViewModel>(_published);
            for (var index = 0; index < _published; index++)
            {
                var option = _matches[index];
                var tile = new AppearanceTileViewModel(option, option.Key == current, TileSize);
                firstPage.Add(tile);
                ApplyCachedPreview(tile);
            }

            Tiles = new ObservableCollection<AppearanceTileViewModel>(firstPage);

            // The selected appearance is the one preview the builder needs immediately. Request it
            // before Avalonia realizes the rest of the viewport so it cannot sit behind the costly
            // dynamic rows that lead appearance.2da.
            EnsurePreview(firstPage.FirstOrDefault(tile => tile.IsCurrent));
            OnPropertyChanged(nameof(MatchSummary));
            OnPropertyChanged(nameof(CanLoadMore));
        }

        private void PublishPage()
        {
            if (_disposed)
                return;

            var current = _currentKey();
            var end = Math.Min(_published + PageSize, _matches.Count);
            for (var index = _published; index < end; index++)
            {
                var option = _matches[index];
                var tile = new AppearanceTileViewModel(option, option.Key == current, TileSize);
                Tiles.Add(tile);
                ApplyCachedPreview(tile);
            }

            _published = end;
            OnPropertyChanged(nameof(MatchSummary));
            OnPropertyChanged(nameof(CanLoadMore));
        }

        /// <summary>
        /// Applies only previews that are already decoded in memory. Disk reads and model rendering
        /// wait until <see cref="EnsurePreview"/> is called for a realized cell.
        /// </summary>
        private void ApplyCachedPreview(AppearanceTileViewModel tile)
        {
            if (_thumbnails == null)
                return;

            if (tile.Option.CreatureAppearanceId is { } appearanceId)
            {
                tile.Preview = _thumbnails.CachedAppearance(appearanceId);
            }
            else if (!string.IsNullOrWhiteSpace(tile.Option.ModelResRef))
            {
                tile.Preview = _thumbnails.CachedTile(
                    tile.Option.ModelResRef,
                    renderDoorTransitionFallback: tile.Option.IsDoorTransition);
            }

            if (tile.Preview != null)
                tile.PreviewRequested = true;
        }

        private void NotifyCurrentChanged()
        {
            OnPropertyChanged(nameof(CurrentDescription));
            OnPropertyChanged(nameof(CurrentIsUnknown));
        }

        /// <summary>
        /// Resource replacement can finish just after its single reload notification while an older
        /// render is still unwinding. Retry a realized cell a few times with backoff so that race heals
        /// itself; unpublished cells still do no work at all.
        /// </summary>
        private void QueuePreviewRetry(AppearanceTileViewModel tile)
        {
            if (_disposed || !_previewAttempts.TryGetValue(tile, out var attempts) ||
                attempts >= MaxPreviewAttempts)
            {
                return;
            }

            var delay = TimeSpan.FromMilliseconds(250 * (1 << Math.Min(attempts - 1, 4)));
            _ = Task.Delay(delay).ContinueWith(
                _ => Dispatcher.UIThread.Post(() =>
                {
                    if (!_disposed && Tiles.Contains(tile) && tile.Preview == null)
                        EnsurePreview(tile);
                }),
                TaskScheduler.Default);
        }

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
