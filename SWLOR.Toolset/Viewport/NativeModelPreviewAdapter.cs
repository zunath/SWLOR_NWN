using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Nwn.Preview.Cache;
using Nwn.Formats.Mdl;
using Nwn.Formats.TwoDa;
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
        // Shared geometry is published as a .lod redirect, which the client resolves before the MDL.
        if (!ModelResourceResolver.TryResolve(resources, resRef, out var modelHandle))
            throw new FileNotFoundException($"Model '{resRef}' was not found in the configured game resource layers.");
        var modelBytes = modelHandle.GetBytes(MaximumModelBytes);
        var modelCacheKey = $"{modelHandle.Provenance.SourcePath}|{Convert.ToHexStringLower(SHA256.HashData(modelBytes))}";
        var scene = AssetCache.GetOrAddScene(modelCacheKey, () => MdlScenePreparer.Prepare(
            modelBytes.Length >= 4 && modelBytes.AsSpan(0, 4).SequenceEqual(new byte[4])
                ? MdlBinaryReader.Read(modelBytes) : MdlAsciiReader.Read(modelBytes)));
        var textures = new Dictionary<string, RgbaImage>(StringComparer.OrdinalIgnoreCase);
        var missingTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unsupportedMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cloakTexture = CloakTexture(resources, resRef);
        foreach (var mesh in scene.Nodes.Where(node => node.RenderEnabled && node.Mesh is not null).Select(node => node.Mesh!))
        {
            var materialName = HasName(mesh.MaterialName) ? mesh.MaterialName : mesh.BitmapName;
            if (HasName(materialName) && !textures.ContainsKey(materialName!))
            {
                var material = MaterialResolver.TryParseMaterial(resources, materialName!);
                if (material is not null)
                {
                    var tinted = TintMapTextureRenderer.Render(resources, materialName!, material, null, null,
                        textureName: cloakTexture ?? mesh.BitmapName);
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

    /// <summary>
    /// The client draws every cloak mesh with a cloakmodel.2da TEXTURE, whatever its bitmap names
    /// (model 007's visible mesh names cala_cloak). Preview the first appearance using this geometry.
    /// </summary>
    internal static string? CloakTexture(ResourceIndex resources, string resRef)
    {
        var match = CloakModel.Match(resRef);
        if (!match.Success)
            return null;
        var model = int.Parse(match.Groups[1].Value);
        var texture = model;
        if (resources.TryLookup(new ResourceIdentity("cloakmodel", ResourceIdentity.TypeFromExtension("2da")), out var handle))
        {
            var table = TwoDaReader.Read(handle.GetBytes(), TwoDaReadOptions.EngineCompatible);
            for (var row = 0; row < table.Rows.Count; row++)
            {
                if (int.TryParse(table.GetValue(row, "MODEL"), out var rowModel) && rowModel == model &&
                    int.TryParse(table.GetValue(row, "TEXTURE"), out var rowTexture) && rowTexture > 0)
                {
                    texture = rowTexture;
                    break;
                }
            }
        }
        return $"cloak_{texture:000}";
    }

    private static readonly Regex CloakModel = new(@"^p[fm][a-z]\d+_cloak_(\d{3})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
