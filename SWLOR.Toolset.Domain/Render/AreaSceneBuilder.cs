using System.Numerics;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;
using Nwn.Preview.Areas;
using Nwn.Preview.Scene;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using Nwn.Formats.Tilesets;

namespace SWLOR.Toolset.Domain.Render;

/// <summary>SWLOR resource and appearance adapter for shared area-scene composition.</summary>
public static class AreaSceneBuilder
{
    public const float TileSize = Nwn.Preview.Areas.AreaSceneComposer.TileSize;

    public static AreaScene Build(AreDocument are, GitDocument git, TilesetCatalog tilesetCatalog,
        TileModelCache modelCache, PlaceableAppearanceService? placeableAppearances = null,
        DoorTypeService? doorTypes = null, TileWalkmeshCache? walkmeshes = null,
        WaypointAppearanceService? waypointAppearances = null,
        Func<JsonGffStruct, RenderModel?>? resolveCreatureModel = null)
    {
        ArgumentNullException.ThrowIfNull(are);
        ArgumentNullException.ThrowIfNull(git);
        ArgumentNullException.ThrowIfNull(tilesetCatalog);
        ArgumentNullException.ThrowIfNull(modelCache);

        TilesetDefinition? tileset = null;
        var resRef = are.Tileset ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(resRef) && tilesetCatalog.TryGetTileset(resRef, out var resolved))
            tileset = resolved;

        return Nwn.Preview.Areas.AreaSceneComposer.Build(are, git, tileset,
            CreateResolvers(modelCache, placeableAppearances, doorTypes, walkmeshes, waypointAppearances,
                resolveCreatureModel));
    }

    public static InstanceMarker BuildInstanceMarker(ResourceType type, JsonGffStruct instance,
        TileModelCache modelCache, PlaceableAppearanceService? placeableAppearances = null,
        DoorTypeService? doorTypes = null, WaypointAppearanceService? waypointAppearances = null,
        Func<JsonGffStruct, RenderModel?>? resolveCreatureModel = null,
        IReadOnlyList<TilePlacement>? tiles = null, int listIndex = -1)
    {
        ArgumentNullException.ThrowIfNull(modelCache);
        return Nwn.Preview.Areas.AreaSceneComposer.BuildInstanceMarker(type, instance,
            CreateResolvers(modelCache, placeableAppearances, doorTypes, null, waypointAppearances,
                resolveCreatureModel), listIndex: listIndex, tiles: tiles);
    }

    private static AreaSceneResolvers CreateResolvers(TileModelCache modelCache,
        PlaceableAppearanceService? placeableAppearances, DoorTypeService? doorTypes,
        TileWalkmeshCache? walkmeshes, WaypointAppearanceService? waypointAppearances,
        Func<JsonGffStruct, RenderModel?>? resolveCreatureModel)
    {
        return new AreaSceneResolvers
        {
            ResolveTileModel = modelCache.GetOrBuild,
            ResolveTileWalkmesh = walkmeshes == null ? null : walkmeshes.GetOrBuild,
            ResolveInstanceAppearance = (type, instance) => ResolveAppearance(type, instance,
                modelCache, placeableAppearances, doorTypes, waypointAppearances, resolveCreatureModel)
        };
    }

    private static ResolvedInstanceAppearance ResolveAppearance(ResourceType type, JsonGffStruct instance,
        TileModelCache modelCache, PlaceableAppearanceService? placeableAppearances,
        DoorTypeService? doorTypes, WaypointAppearanceService? waypointAppearances,
        Func<JsonGffStruct, RenderModel?>? resolveCreatureModel)
    {
        var appearance = new ResolvedInstanceAppearance
        {
            TintMapOverrides = TintMapOverrides.Read(new VarTable(instance))
        };
        return type switch
        {
            ResourceType.Utc => appearance with
            {
                Model = resolveCreatureModel?.Invoke(instance),
                ModelCorrection = CreatureModelFacing.ForwardCorrection
            },
            ResourceType.Utd => ResolveDoorAppearance(instance, appearance, doorTypes, modelCache),
            ResourceType.Utp => appearance with { Model = ResolvePlaceableModel(instance, placeableAppearances, modelCache) },
            ResourceType.Utm => appearance with
            {
                Model = modelCache.GetOrBuild(WaypointMarkerModel.MerchantModelResRef),
                ModelCorrection = WaypointMarkerModel.ForwardCorrection
            },
            ResourceType.Utw => appearance with
            {
                Model = ResolveWaypointModel(instance, waypointAppearances, modelCache),
                ModelCorrection = WaypointMarkerModel.ForwardCorrection
            },
            _ => appearance
        };
    }

    private static ResolvedInstanceAppearance ResolveDoorAppearance(JsonGffStruct instance,
        ResolvedInstanceAppearance appearance, DoorTypeService? doorTypes, TileModelCache modelCache)
    {
        var model = ResolveDoorModel(instance, doorTypes, modelCache);
        return appearance with { Model = model, IsDoorTransition = IsDoorTransition(instance, doorTypes) };
    }

    private static RenderModel? ResolvePlaceableModel(JsonGffStruct instance,
        PlaceableAppearanceService? placeableAppearances, TileModelCache modelCache)
    {
        if (placeableAppearances == null)
            return null;
        var appearanceId = instance.GetIntOrNull("Appearance") ?? -1;
        return placeableAppearances.TryGet(appearanceId, out var row) && !string.IsNullOrWhiteSpace(row.ModelName)
            ? modelCache.GetOrBuildPlaceableEditor(row.ModelName)
            : null;
    }

    private static RenderModel? ResolveWaypointModel(JsonGffStruct instance,
        WaypointAppearanceService? waypointAppearances, TileModelCache modelCache)
    {
        if (waypointAppearances == null)
            return null;
        var appearanceId = instance.GetIntOrNull("Appearance") ?? -1;
        return waypointAppearances.TryGet(appearanceId, out var row) && !string.IsNullOrWhiteSpace(row.ModelName)
            ? modelCache.GetOrBuild(row.ModelName)
            : null;
    }

    private static RenderModel? ResolveDoorModel(JsonGffStruct instance, DoorTypeService? doorTypes,
        TileModelCache modelCache)
    {
        if (doorTypes == null)
            return null;
        var appearance = instance.GetIntOrNull("Appearance") ?? 0;
        var specific = appearance > 0 ? doorTypes.GetAll().FirstOrDefault(row => row.Id == appearance) : null;
        var generic = appearance == 0 ? doorTypes.GetGenericAll().FirstOrDefault(row =>
            row.Id == (instance.GetIntOrNull("GenericType_New") ?? instance.GetIntOrNull("GenericType") ?? 0)) : null;
        var modelResRef = specific?.Model ?? generic?.Model;
        var visibleModel = specific?.VisibleModel ?? generic?.VisibleModel ?? true;
        return string.IsNullOrWhiteSpace(modelResRef) ? null : visibleModel
            ? modelCache.GetOrBuild(modelResRef)
            : modelCache.GetOrBuildDoorTransition(modelResRef);
    }

    private static bool IsDoorTransition(JsonGffStruct instance, DoorTypeService? doorTypes)
    {
        if (doorTypes == null)
            return false;
        var appearance = instance.GetIntOrNull("Appearance") ?? 0;
        if (appearance > 0)
            return doorTypes.GetAll().FirstOrDefault(row => row.Id == appearance) is { VisibleModel: false };
        var genericType = instance.GetIntOrNull("GenericType_New") ?? instance.GetIntOrNull("GenericType") ?? 0;
        return doorTypes.GetGenericAll().FirstOrDefault(row => row.Id == genericType) is { VisibleModel: false };
    }
}
