using Nwn.Formats.Mdl;
using Nwn.Preview.Dds;
using Nwn.Preview.Scene;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Viewport;

/// <summary>Adapts SWLOR's ordered resource index to neutral static MDL and DDS readers.</summary>
public sealed class NativeModelPreviewAdapter(ResourceIndex resources)
{
    private const int MaximumModelBytes = 64 * 1024 * 1024;
    private const int MaximumTextureBytes = 64 * 1024 * 1024;

    public NativeModelPreviewData Load(string resRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resRef);
        var modelIdentity = ResourceIdentity.FromFileName($"{resRef}.mdl");
        if (!resources.TryLookup(modelIdentity, out var modelHandle))
            throw new FileNotFoundException($"Model '{resRef}.mdl' was not found in the configured game resource layers.");
        var source = MdlBinaryReader.Read(modelHandle.GetBytes(MaximumModelBytes));
        var scene = MdlScenePreparer.Prepare(source);
        var textures = new Dictionary<string, Nwn.Preview.Pixels.RgbaImage>(StringComparer.OrdinalIgnoreCase);
        var missingTextures = new List<string>();
        foreach (var bitmap in scene.Nodes.Select(node => node.Mesh?.BitmapName).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var identity = ResourceIdentity.FromFileName($"{bitmap}.dds");
            if (!resources.TryLookup(identity, out var handle))
            {
                missingTextures.Add(bitmap!);
                continue;
            }
            textures[bitmap!] = DdsDecoder.Decode(handle.GetBytes(MaximumTextureBytes));
        }

        return new NativeModelPreviewData(scene, textures, missingTextures, modelHandle.Provenance.SourcePath);
    }
}
