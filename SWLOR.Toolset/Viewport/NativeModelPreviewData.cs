using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;

namespace SWLOR.Toolset.Viewport;

/// <summary>Prepared shared scene, available DDS maps, and resource provenance for the SWLOR UI.</summary>
public sealed class NativeModelPreviewData(
    PreparedScene scene,
    IReadOnlyDictionary<string, RgbaImage> textures,
    IReadOnlyList<string> missingTextures,
    string modelSourcePath,
    IReadOnlyList<string>? unsupportedMaterials = null)
{
    public PreparedScene Scene { get; } = scene;
    public IReadOnlyDictionary<string, RgbaImage> Textures { get; } = textures;
    public IReadOnlyList<string> MissingTextures { get; } = missingTextures;
    public string ModelSourcePath { get; } = modelSourcePath;
    public IReadOnlyList<string> UnsupportedMaterials { get; } = unsupportedMaterials ?? Array.Empty<string>();
}
