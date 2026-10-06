using Nwn.Toolset.Avalonia.Appearances;
using SWLOR.Toolset.Editors.Appearance;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>Draws the shared door appearance gallery's tiles through SWLOR's model thumbnail cache.</summary>
    internal sealed class SwlorDoorAppearancePreviews : IDoorAppearancePreviewSource
    {
        private readonly ThumbnailService _thumbnails;

        public SwlorDoorAppearancePreviews(ThumbnailService thumbnails)
        {
            _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
        }

        public IAppearanceGalleryPreviewProvider? Create(IReadOnlyList<DoorAppearanceGalleryEntry> entries)
        {
            var choices = new Dictionary<string, DoorAppearanceChoice>(StringComparer.Ordinal);
            foreach (var entry in entries)
                choices.TryAdd(entry.OptionId.Value, entry.Choice);

            return new AppearanceGalleryPreviewProvider(
                _thumbnails,
                id => choices.TryGetValue(id.Value, out var choice)
                    ? new AppearanceOption(
                        id.Value,
                        choice.Display,
                        choice.Model,
                        ModelResRef: choice.Model,
                        IsDoorTransition: choice.IsDoorTransition)
                    : null);
        }
    }
}
