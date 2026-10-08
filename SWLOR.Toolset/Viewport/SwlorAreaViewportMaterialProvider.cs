using System.Numerics;
using Nwn.Formats.Mtr;
using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;
using Nwn.Toolset.Avalonia.Areas;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Viewport;

/// <summary>Applies SWLOR resource, material, and tint conventions to the shared area viewport.</summary>
public sealed class SwlorAreaViewportMaterialProvider :
    IAreaViewportMaterialProvider,
    IAreaViewportMeshMetadataProvider
{
    private readonly ResourceIndex _resources;
    private readonly object _cacheLock = new();
    private readonly Dictionary<string, MtrDocument?> _parsedMaterialCache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<TextureSurfaceKey, CachedTextureSurface?> _textureCache = new();
    private readonly Dictionary<SurfaceKey, ResolvedSurface?> _surfaceCache = new();
    private long _revision;

    public long Revision => Interlocked.Read(ref _revision);

    public SwlorAreaViewportMaterialProvider(ResourceIndex resources)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public void Invalidate()
    {
        lock (_cacheLock)
        {
            _parsedMaterialCache.Clear();
            _textureCache.Clear();
            _surfaceCache.Clear();
        }
        Interlocked.Increment(ref _revision);
    }

    public AreaViewportMeshMetadata GetMetadata(RenderMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        return new AreaViewportMeshMetadata(mesh.LayerColorIndices, mesh.UsesItemTintOverrides);
    }

    public AreaViewportMaterial Resolve(AreaViewportMaterialRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var surfaceName = !string.IsNullOrWhiteSpace(request.MaterialName)
            ? request.MaterialName
            : request.TextureName;
        if (string.IsNullOrWhiteSpace(surfaceName))
            return new AreaViewportMaterial();

        var hasMaterial = !string.IsNullOrWhiteSpace(request.MaterialName);
        var metadata = request.MeshMetadata ?? (request.Mesh is null ? null : GetMetadata(request.Mesh));
        var colors = SelectLayerColors(metadata, request.Instance, request.Model);
        var surface = ResolveSurface(surfaceName, hasMaterial, colors);
        if (surface is null)
            return new AreaViewportMaterial();

        var tintSurface = surface.IsTint
            ? ResolveTintSurface(surfaceName, surface.Material!, metadata, request.Mesh, request.Instance, request.Purpose)
            : null;

        return new AreaViewportMaterial
        {
            Diffuse = surface.Diffuse,
            Normal = surface.Normal,
            Specular = surface.Specular,
            Roughness = surface.Roughness,
            Environment = surface.Environment,
            Tint = tintSurface,
            AlphaCutoff = surface.AlphaCutoff,
            UseTextureAlpha = surface.Blending == TxiBlendMode.Additive,
            BlendMode = surface.Blending == TxiBlendMode.Additive
                ? AreaViewportBlendMode.Additive
                : AreaViewportBlendMode.None
        };
    }

    // The viewport asks once per mesh and placed instance, but everything here depends only on the
    // surface and its dye set. Resolving it per instance repeated the companion-map lookups and the
    // full-image alpha scan for every copy of a placeable.
    private ResolvedSurface? ResolveSurface(string surfaceName, bool hasMaterial, IReadOnlyDictionary<int, int>? colors)
    {
        var key = new SurfaceKey(surfaceName.Trim().ToLowerInvariant(), hasMaterial, PaletteKey(colors));
        lock (_cacheLock)
        {
            if (_surfaceCache.TryGetValue(key, out var cached))
                return cached;
        }

        var material = hasMaterial
            ? TryParseMaterial(surfaceName)
            : TryTintMaterialFallback(surfaceName);
        MaterialMaps maps;
        try
        {
            maps = material is null
                ? MaterialResolver.ResolveMaterialMaps(_resources, surfaceName, resolveMaterial: false)
                : new MaterialMaps
                {
                    Diffuse = MaterialResolver.GetTexture(material, 0) ?? surfaceName,
                    Normal = MaterialResolver.GetTexture(material, 1),
                    Specular = MaterialResolver.GetTexture(material, 2),
                    Roughness = MaterialResolver.GetTexture(material, 3)
                };
        }
        catch (Exception)
        {
            material = null;
            maps = new MaterialMaps { Diffuse = surfaceName };
        }

        ResolvedSurface? surface = null;
        var diffuse = LoadSurface(maps.Diffuse, colors);
        if (diffuse is not null)
        {
            var hints = TextureRenderPolicy.Resolve(_resources, maps.Diffuse, diffuse.Image);
            var isTint = TintMapTextureRenderer.IsTintMapMaterial(material);
            var environmentName = isTint
                ? hints.EnvironmentMapTexture ?? TextureRenderPolicy.StandaloneEnvironmentMap
                : hints.EnvironmentMapTexture;
            surface = new ResolvedSurface(
                material,
                isTint,
                diffuse.Pixels,
                LoadSurface(maps.Normal)?.Pixels,
                LoadSurface(maps.Specular)?.Pixels,
                LoadSurface(maps.Roughness)?.Pixels,
                LoadSurface(environmentName)?.Pixels,
                hints.AlphaCutoff,
                hints.Blending);
        }

        lock (_cacheLock)
            _surfaceCache[key] = surface;
        return surface;
    }

    private MtrDocument? TryTintMaterialFallback(string surfaceName)
    {
        try
        {
            var candidate = MaterialResolver.TryParseMaterial(_resources, surfaceName);
            return TintMapTextureRenderer.IsTintMapMaterial(candidate) ? candidate : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private MtrDocument? TryParseMaterial(string surfaceName)
    {
        lock (_cacheLock)
        {
            if (_parsedMaterialCache.TryGetValue(surfaceName, out var cached))
                return cached;

            MtrDocument? material;
            try
            {
                material = MaterialResolver.TryParseMaterial(_resources, surfaceName);
            }
            catch (Exception)
            {
                material = null;
            }

            _parsedMaterialCache[surfaceName] = material;
            return material;
        }
    }

    private AreaViewportTintSurface? ResolveTintSurface(
        string materialName,
        MtrDocument material,
        AreaViewportMeshMetadata? metadata,
        RenderMesh? mesh,
        Nwn.Preview.Areas.InstanceMarker? instance,
        AreaViewportDrawPurpose purpose)
    {
        var maskName = MaterialResolver.GetTexture(material, 7);
        var paletteName = MaterialResolver.GetTexture(material, 10);
        var mask = LoadSurface(maskName);
        var palette = LoadSurface(paletteName);
        if (mask is null || palette is null)
            return null;

        var alphaSource = MaterialResolver.GetAlphaSource(material);
        var alpha = LoadSurface(alphaSource?.TextureName);
        var layerColors = SelectLayerColors(metadata, instance, instance?.Model);
        var (overrides, creatureOverrides) = SelectTintOverrides(purpose, mesh, metadata, instance);
        var armorPart = mesh is null
            ? AppearanceArmor.Invalid
            : SwlorRenderMeshMetadataStore.GetArmorPart(mesh);
        var layers = new AreaViewportTintLayer[10];

        for (var layerValue = 0; layerValue < layers.Length; layerValue++)
        {
            var layer = (TintMapLayerType)layerValue;
            var activeOverrides = TintMapVariable.IsCreatureColorLayer(layer) && creatureOverrides is not null
                ? creatureOverrides
                : overrides;
            var savedValue = TintMapOverrides.GetMaterialColor(activeOverrides, materialName, layer, armorPart);
            var hasCustomColor = TintMapColor.TryFromStoredValue(savedValue, out var custom);
            var customColor = hasCustomColor
                ? new Vector4(custom.Red / 255f, custom.Green / 255f, custom.Blue / 255f, 1f)
                : Vector4.Zero;
            var paletteColor = hasCustomColor
                ? TintMapPaletteColors.GetClosestColorId(layer, custom)
                : savedValue > 0 && savedValue <= TintMapMaterialRegistry.PaletteColorCount
                    ? savedValue - 1
                    : layerColors is not null && layerColors.TryGetValue(layerValue, out var standardColor)
                        ? standardColor
                        : 0;
            var paletteRow = TintMapMaterialRegistry.GetPaletteCoordinate(
                layer,
                Math.Clamp(paletteColor, 0, TintMapMaterialRegistry.PaletteColorCount - 1));
            layers[layerValue] = new AreaViewportTintLayer(customColor, paletteRow);
        }

        return new AreaViewportTintSurface(
            mask.Pixels,
            palette.Pixels,
            alpha?.Pixels,
            alphaSource?.UsesRedChannel == true,
            alphaSource?.Cutoff ?? 0f,
            layers);
    }

    private (IReadOnlyDictionary<string, int>? Overrides, IReadOnlyDictionary<string, int>? CreatureOverrides)
        SelectTintOverrides(
            AreaViewportDrawPurpose purpose,
            RenderMesh? mesh,
            AreaViewportMeshMetadata? metadata,
            Nwn.Preview.Areas.InstanceMarker? instance)
    {
        if (instance is null || mesh is null)
        {
            return (null, null);
        }

        if (purpose == AreaViewportDrawPurpose.PlacementGhost && metadata?.UsesItemTintOverrides == true)
        {
            var overrides = mesh.TintMapOverrides.Count > 0 ||
                            instance.Kind != Nwn.Preview.Areas.InstanceMarkerKind.Item
                ? mesh.TintMapOverrides
                : instance.TintMapOverrides;
            return (overrides, instance.Kind == Nwn.Preview.Areas.InstanceMarkerKind.Creature
                ? instance.TintMapOverrides
                : null);
        }

        if (instance.Kind == Nwn.Preview.Areas.InstanceMarkerKind.Creature && metadata?.UsesItemTintOverrides == true)
        {
            return (mesh.TintMapOverrides, instance.TintMapOverrides);
        }

        return (instance.TintMapOverrides, null);
    }

    private static IReadOnlyDictionary<int, int>? SelectLayerColors(
        AreaViewportMeshMetadata? metadata,
        Nwn.Preview.Areas.InstanceMarker? instance,
        RenderModel? model)
    {
        if (metadata is { LayerColorIndices.Count: > 0 })
            return metadata.LayerColorIndices;
        if (instance is { LayerColorIndices.Count: > 0 })
            return instance.LayerColorIndices;
        return model?.LayerColorIndices;
    }

    private CachedTextureSurface? LoadSurface(string? textureName, IReadOnlyDictionary<int, int>? layerColors = null)
    {
        if (string.IsNullOrWhiteSpace(textureName))
            return null;
        var key = new TextureSurfaceKey(
            textureName.Trim().ToLowerInvariant(),
            PaletteKey(layerColors));
        lock (_cacheLock)
        {
            if (_textureCache.TryGetValue(key, out var cached))
                return cached;

            CachedTextureSurface? surface;
            try
            {
                var image = TextureLoader.Load(_resources, textureName, layerColors);
                surface = image is null ? null : new CachedTextureSurface(
                    image,
                    new RgbaImage(image.Width, image.Height, image.Pixels));
            }
            catch (Exception)
            {
                surface = null;
            }

            _textureCache.Add(key, surface);
            return surface;
        }
    }

    private static string PaletteKey(IReadOnlyDictionary<int, int>? colors) =>
        colors is null || colors.Count == 0
            ? string.Empty
            : string.Join(";", colors.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"));

    private sealed record CachedTextureSurface(TextureImage Image, RgbaImage Pixels);

    private readonly record struct TextureSurfaceKey(string Name, string Palette);

    private readonly record struct SurfaceKey(string Name, bool HasMaterial, string Palette);

    private sealed record ResolvedSurface(
        MtrDocument? Material,
        bool IsTint,
        RgbaImage Diffuse,
        RgbaImage? Normal,
        RgbaImage? Specular,
        RgbaImage? Roughness,
        RgbaImage? Environment,
        float AlphaCutoff,
        TxiBlendMode Blending);

}
