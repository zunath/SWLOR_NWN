using Avalonia.Media.Imaging;
using Nwn.Authoring.Areas.Tiles;
using SharedPaletteSource = Nwn.Toolset.Avalonia.Palettes.PaletteSource;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>SWLOR's thumbnail service, adapted from its callbacks to the palette's tasks.</summary>
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

        public Task<Bitmap?> LoadBlueprintAsync(ResourceType type, string resRef, SharedPaletteSource source)
        {
            var completion = new TaskCompletionSource<Bitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _thumbnails.RequestAsync(
                type,
                resRef,
                source == SharedPaletteSource.Standard,
                bitmap => completion.TrySetResult(bitmap));
            return completion.Task;
        }

        public Task<Bitmap?> LoadTileAsync(TilePaletteEntry tile)
        {
            var completion = new TaskCompletionSource<Bitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _thumbnails.RequestTileAsync(
                tile.PreviewModelResRef,
                bitmap => completion.TrySetResult(bitmap),
                tile.FootprintModelResRefs,
                tile.Columns,
                tile.Rows,
                onFailed: () => completion.TrySetResult(null));
            return completion.Task;
        }
    }
}
