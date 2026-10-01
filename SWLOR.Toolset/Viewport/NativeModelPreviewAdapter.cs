using System.Security.Cryptography;
using Nwn.Preview.Cache;
using Nwn.Formats.Mdl;
using Nwn.Preview.Dds;
using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;
using Nwn.Preview.Tga;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

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
        var scene = AssetCache.GetOrAddScene(modelCacheKey, () => MdlScenePreparer.Prepare(
            modelBytes.Length >= 4 && modelBytes.AsSpan(0, 4).SequenceEqual(new byte[4])
                ? MdlBinaryReader.Read(modelBytes) : MdlAsciiReader.Read(modelBytes)));
        var textures = new Dictionary<string, RgbaImage>(StringComparer.OrdinalIgnoreCase);
        var missingTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unsupportedMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mesh in scene.Nodes.Where(node => node.RenderEnabled && node.Mesh is not null).Select(node => node.Mesh!))
        {
            var materialName = HasName(mesh.MaterialName) ? mesh.MaterialName : mesh.BitmapName;
            if (HasName(materialName) && !textures.ContainsKey(materialName!))
            {
                var material = MaterialResolver.TryParseMaterial(resources, materialName!);
                if (material is not null)
                {
                    var tinted = TintMapTextureRenderer.Render(resources, materialName!, material, null, null);
                    if (tinted is not null)
                    {
                        textures.Add(materialName!, new RgbaImage(tinted.Width, tinted.Height, tinted.Pixels));
                        continue;
                    }
                    unsupportedMaterials.Add(materialName!);
                    var diffuseName = MaterialResolver.GetTexture(material, 0);
                    if (HasName(diffuseName) && TryTexture(diffuseName!, out var diffuse))
                    {
                        textures.Add(materialName!, diffuse!);
                        continue;
                    }
                }
                else if (HasName(mesh.MaterialName)) unsupportedMaterials.Add(materialName!);
            }
            else if (HasName(materialName) && textures.ContainsKey(materialName!)) continue;
            if (HasName(mesh.BitmapName) && !textures.ContainsKey(mesh.BitmapName!))
            {
                if (TryTexture(mesh.BitmapName!, out var bitmap)) textures.Add(mesh.BitmapName!, bitmap!);
                else missingTextures.Add(mesh.BitmapName!);
            }
        }

        return new NativeModelPreviewData(scene, textures, missingTextures.ToArray(), modelHandle.Provenance.SourcePath, unsupportedMaterials.ToArray());

        bool TryTexture(string name, out RgbaImage? image)
        {
            // SWLOR's authored TGA takes precedence over its DDS companion.
            var tga = resources.TryLookup(ResourceIdentity.FromFileName($"{name}.tga"), out var handle);
            if (!tga && !resources.TryLookup(ResourceIdentity.FromFileName($"{name}.dds"), out handle))
            { image = null; return false; }
            var textureBytes = handle.GetBytes(MaximumTextureBytes);
            var key = $"{handle.Provenance.SourcePath}|{Convert.ToHexStringLower(SHA256.HashData(textureBytes))}|{(tga ? "tga" : "dds")}";
            image = AssetCache.GetOrAddImage(key, () => tga ? TgaDecoder.Decode(textureBytes) :
                DdsDecoder.Decode(textureBytes, new DdsDecodeOptions { StoredRowOrder = DdsStoredRowOrder.BottomUp }));
            return true;
        }
    }

    private static bool HasName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && !name.Equals("NULL", StringComparison.OrdinalIgnoreCase);
}
