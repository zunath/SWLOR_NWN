using Avalonia.Media.Imaging;
using Nwn.Toolset.Avalonia.Appearances;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Appearance;

/// <summary>Resolves shared gallery requests through SW's appearance and model thumbnail caches.</summary>
public sealed class AppearanceGalleryPreviewProvider : IAppearanceGalleryPreviewProvider
{
    private readonly ThumbnailService _thumbnails;
    private readonly Func<AppearanceGalleryOptionId, AppearanceOption?> _resolve;

    public AppearanceGalleryPreviewProvider(
        ThumbnailService thumbnails,
        Func<AppearanceGalleryOptionId, AppearanceOption?> resolve)
    {
        _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
        _resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
    }

    public Bitmap? Cached(AppearanceGalleryOption option)
    {
        var hostOption = _resolve(option.Id);
        if (hostOption == null)
            return null;
        if (hostOption.CreatureAppearanceId is { } appearanceId)
            return _thumbnails.CachedAppearance(appearanceId);
        return string.IsNullOrWhiteSpace(hostOption.ModelResRef)
            ? null
            : _thumbnails.CachedTile(
                hostOption.ModelResRef,
                renderDoorTransitionFallback: hostOption.IsDoorTransition);
    }

    public bool Request(
        AppearanceGalleryOption option,
        Action<Bitmap> onReady,
        Action onFailed,
        AppearanceGalleryPreviewPriority priority)
    {
        var hostOption = _resolve(option.Id);
        if (hostOption == null || !_thumbnails.IsAvailable)
        {
            onFailed();
            return false;
        }

        if (hostOption.CreatureAppearanceId is { } appearanceId)
        {
            return _thumbnails.RequestAppearanceAsync(
                appearanceId,
                onReady,
                onFailed,
                priority == AppearanceGalleryPreviewPriority.Selected
                    ? AppearancePreviewPriority.Selected
                    : AppearancePreviewPriority.Visible);
        }

        if (string.IsNullOrWhiteSpace(hostOption.ModelResRef))
        {
            onFailed();
            return false;
        }

        _thumbnails.RequestTileAsync(
            hostOption.ModelResRef,
            onReady,
            renderDoorTransitionFallback: hostOption.IsDoorTransition,
            onFailed: onFailed);
        return true;
    }
}
