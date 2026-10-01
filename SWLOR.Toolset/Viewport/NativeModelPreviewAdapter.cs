using System.Security.Cryptography;
using Nwn.Preview.Cache;
using Nwn.Formats.Mdl;
using Nwn.Preview.Dds;
using Nwn.Preview.Scene;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Viewport;

/// <summary>Adapts SWLOR's ordered resource index to neutral static MDL and DDS readers.</summary>
public sealed class NativeModelPreviewAdapter(ResourceIndex resources)
{
    private static readonly PreviewAssetCache AssetCache = new(maximumEntries: 256, maximumBytes: 512L * 1024 * 1024);
    private const int MaximumModelBytes = 64 * 1024 * 1024;
    private const int MaximumTextureBytes = 64 * 1024 * 1024;

    public NativeModelPreviewData Load(string resRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resRef);
        var modelIdentity = ResourceIdentity.FromFileName($"{resRef}.mdl");
        if (!resources.TryLookup(modelIdentity, out var modelHandle))
            throw new FileNotFoundException($"Model '{resRef}.mdl' was not found in the configured game resource layers.");
        var modelBytes = modelHandle.GetBytes(MaximumModelBytes);
        var modelCacheKey = $"{modelHandle.Provenance.SourcePath}|{Convert.ToHexStringLower(SHA256.HashData(modelBytes))}";
        var scene = AssetCache.GetOrAddScene(modelCacheKey, () => MdlScenePreparer.Prepare(MdlBinaryReader.Read(modelBytes)));
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
            var textureBytes = handle.GetBytes(MaximumTextureBytes);
            var textureCacheKey = $"{handle.Provenance.SourcePath}|{Convert.ToHexStringLower(SHA256.HashData(textureBytes))}";
            textures[bitmap!] = AssetCache.GetOrAddImage(textureCacheKey,
                () => DdsDecoder.Decode(textureBytes));
        }

        return new NativeModelPreviewData(scene, textures, missingTextures, modelHandle.Provenance.SourcePath);
    }
}
