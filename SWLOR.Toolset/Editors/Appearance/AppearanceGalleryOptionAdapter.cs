using Nwn.Toolset.Avalonia.Appearances;

namespace SWLOR.Toolset.Editors.Appearance;

/// <summary>Maps SW appearance metadata into the shared host-neutral gallery contract.</summary>
public static class AppearanceGalleryOptionAdapter
{
    public static AppearanceGalleryOption ToShared(AppearanceOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        return new AppearanceGalleryOption(
            new AppearanceGalleryOptionId(option.Key),
            option.Caption,
            option.Detail);
    }

    public static IReadOnlyList<AppearanceGalleryOption> ToShared(
        IReadOnlyList<AppearanceOption> options) => options.Select(ToShared).ToArray();
}
