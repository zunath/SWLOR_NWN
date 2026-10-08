using Avalonia.Media.Imaging;
using Nwn.Authoring.Areas.Tiles;
using SharedPaletteSource = Nwn.Toolset.Avalonia.Palettes.PaletteSource;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>SWLOR's thumbnail service, adapted from its callbacks to the palette's tasks.</summary>
    /// <remarks>
    /// <see cref="ThumbnailService"/> joins concurrent requests for the same preview into one render and
    /// caches the result, so a render already running is left to finish for whoever else wants it (and for
    /// the next request). Cancellation therefore stops what it can: a request cancelled before it starts
    /// never queues a render, and a cancelled request's task completes at once instead of waiting on it.
    /// </remarks>
    internal sealed class SwlorPalettePreviewSource : IPalettePreviewSource
    {
        private readonly ThumbnailService _thumbnails;

        public SwlorPalettePreviewSource(ThumbnailService thumbnails)
        {
            _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
        }

        public event Action<ResourceType, string>? Invalidated
        {
            add => _thumbnails.InvalidatedForResRef += value;
            remove => _thumbnails.InvalidatedForResRef -= value;
        }

        public bool IsAvailable => _thumbnails.IsAvailable;

        public Bitmap? TypeIcon(ResourceType? type) =>
            type is { } resourceType ? _thumbnails.TypeChipIcon(resourceType) : _thumbnails.TileChipIcon();

        public Task<Bitmap?> LoadBlueprintAsync(
            ResourceType type,
            string resRef,
            SharedPaletteSource source,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<Bitmap?>(cancellationToken);

            var completion = new TaskCompletionSource<Bitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            _thumbnails.RequestAsync(
                type,
                resRef,
                source == SharedPaletteSource.Standard,
                bitmap => completion.TrySetResult(bitmap));
            return Release(completion.Task, registration);
        }

        public Task<Bitmap?> LoadTileAsync(TilePaletteEntry tile, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<Bitmap?>(cancellationToken);

            var completion = new TaskCompletionSource<Bitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            _thumbnails.RequestTileAsync(
                tile.PreviewModelResRef,
                bitmap => completion.TrySetResult(bitmap),
                tile.FootprintModelResRefs,
                tile.Columns,
                tile.Rows,
                onFailed: () => completion.TrySetResult(null));
            return Release(completion.Task, registration);
        }

        /// <summary>Drops the cancellation callback once the request settles, whichever way it went.</summary>
        private static Task<Bitmap?> Release(Task<Bitmap?> request, CancellationTokenRegistration registration)
        {
            request.ContinueWith(
                _ => registration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return request;
        }
    }
}
