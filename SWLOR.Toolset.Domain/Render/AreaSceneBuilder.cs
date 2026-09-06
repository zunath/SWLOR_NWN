using System.Numerics;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Assembles a render-ready <see cref="AreaScene"/> from an area's .are/.git documents: tile
    /// grid placements (resolved against the area's tileset and shared render geometry via
    /// <see cref="TileModelCache"/>) plus placed-instance markers. Headless/Domain-level - no GL or
    /// app dependency; consumed later by the area view.
    /// </summary>
    public static class AreaSceneBuilder
    {
        /// <summary>NWN area tiles sit on a 10-meter grid.</summary>
        public const float TileSize = 10f;

        /// <summary>
        /// Builds the scene for one area. Never throws for missing/unresolvable tile models or
        /// tilesets - those degrade to fallback placements with a diagnostic note on the returned
        /// scene's <see cref="AreaScene.Diagnostics"/>. When <paramref name="walkmeshes"/> is
        /// supplied, every non-fallback tile also gets its <see cref="TilePlacement.Walkmesh"/>
        /// resolved; omitting it (the default) leaves every tile's Walkmesh null,
        /// matching prior behavior exactly.
        /// </summary>
        public static AreaScene Build(
            AreDocument are,
            GitDocument git,
            TilesetCatalog tilesetCatalog,
            TileModelCache modelCache,
            PlaceableAppearanceService? placeableAppearances = null,
            DoorTypeService? doorTypes = null,
            TileWalkmeshCache? walkmeshes = null,
            WaypointAppearanceService? waypointAppearances = null,
            Func<JsonGffStruct, RenderModel?>? resolveCreatureModel = null)
        {
            ArgumentNullException.ThrowIfNull(are);
            ArgumentNullException.ThrowIfNull(git);
            ArgumentNullException.ThrowIfNull(tilesetCatalog);
            ArgumentNullException.ThrowIfNull(modelCache);

            var diagnostics = new AreaSceneDiagnostics();
            var tilesetResRef = are.Tileset ?? string.Empty;
            var width = are.Width ?? 0;
            var height = are.Height ?? 0;

            TilesetDefinition? tileset = null;
            if (!string.IsNullOrWhiteSpace(tilesetResRef) && tilesetCatalog.TryGetTileset(tilesetResRef, out var resolved))
            {
                tileset = resolved;
            }
            else
            {
                diagnostics.AddMissingModel(
                    $"tileset '{tilesetResRef}' could not be resolved/parsed; every tile in this area falls back.");
            }

            var tiles = BuildTiles(are, tileset, tilesetResRef, width, modelCache, diagnostics, walkmeshes);
            var instances = BuildInstances(
                git, modelCache, placeableAppearances, doorTypes, waypointAppearances, resolveCreatureModel, tiles);
            var doorAnchors = BuildDoorAnchors(tiles, tileset);

            return new AreaScene
            {
                Tileset = tilesetResRef,
                Width = width,
                Height = height,
                Tiles = tiles,
                Instances = instances,
                DoorAnchors = doorAnchors,
                IsInteriorTileset = tileset?.Interior ?? false,
                Diagnostics = diagnostics,
                Lighting = ComputeLighting(are)
            };
        }

        /// <summary>
        /// Decodes the area's ambient/diffuse lighting from the .are: moon colors when the
        /// area is flagged night, sun colors otherwise. A missing color field falls back to the
        /// neutral default's component so a partially-authored area still lights sanely.
        /// </summary>
        private static AreaLighting ComputeLighting(AreDocument are)
        {
            var isNight = are.IsNight ?? false;
            var ambientPacked = isNight ? are.MoonAmbientColor : are.SunAmbientColor;
            var diffusePacked = isNight ? are.MoonDiffuseColor : are.SunDiffuseColor;

            return new AreaLighting
            {
                AmbientColor = ambientPacked is { } a ? AreaLighting.DecodeColor(a) : AreaLighting.Default.AmbientColor,
                DiffuseColor = diffusePacked is { } d ? AreaLighting.DecodeColor(d) : AreaLighting.Default.DiffuseColor,
                IsNight = isNight,
                FogColor = (isNight ? are.MoonFogColor : are.SunFogColor) is { } f
                    ? AreaLighting.DecodeColor(f)
                    : Vector3.Zero,
                FogDensity = AreaLighting.DecodeFogDensity(
                    (isNight ? are.MoonFogAmount : are.SunFogAmount) ?? 0)
            };
        }

        private static List<TilePlacement> BuildTiles(
            AreDocument are,
            TilesetDefinition? tileset,
            string tilesetResRef,
            int width,
            TileModelCache modelCache,
            AreaSceneDiagnostics diagnostics,
            TileWalkmeshCache? walkmeshes)
        {
            var tileStructs = are.Tiles;
            var placements = new List<TilePlacement>(tileStructs.Count);

            // A missing/zero Width would make column/row math divide-by-zero; this is corrupt
            // input the corpus never actually has (WORKLOG confirms real Tile_List length ==
            // Width*Height), but assembly must still not throw for it - treat every tile as a
            // single column rather than crash.
            var effectiveWidth = width > 0 ? width : 1;

            for (var i = 0; i < tileStructs.Count; i++)
            {
                var tileStruct = tileStructs[i];
                var tileId = tileStruct.GetIntOrNull("Tile_ID") ?? -1;
                var orientation = tileStruct.GetIntOrNull("Tile_Orientation") ?? 0;
                var tileHeight = tileStruct.GetIntOrNull("Tile_Height") ?? 0;

                var col = i % effectiveWidth;
                var row = i / effectiveWidth;

                var centerX = col * TileSize + TileSize / 2f;
                var centerY = row * TileSize + TileSize / 2f;
                var heightOffset = tileset != null ? tileHeight * tileset.Transition : 0;

                // NWN tile models are ORIGIN-CENTRED - their geometry spans -TileSize/2..+TileSize/2
                // on both axes - so a tile is placed by rotating it about its own centre and then
                // moving that centre to the cell centre. There is deliberately no corner-to-centre
                // pre-translation: adding one rotates the tile about a corner instead, which lands
                // rotated tiles a full cell away from where they belong (orientation 0 merely shifts
                // the whole grid by half a tile, which is why that read as "fine" until painting
                // started producing rotated tiles next to unrotated ones).
                var angle = (float)(orientation * (Math.PI / 2.0));
                var transform =
                    Matrix4x4.CreateRotationZ(angle) *
                    Matrix4x4.CreateTranslation(centerX, centerY, heightOffset);

                string? modelResRef = null;
                RenderModel? model = null;
                var isFallback = false;

                if (tileset == null)
                {
                    isFallback = true; // Already noted once for the whole area above.
                }
                else if (tileId < 0 || tileId >= tileset.Tiles.Count)
                {
                    isFallback = true;
                    diagnostics.AddMissingModel(
                        $"tile #{i} (col {col}, row {row}): Tile_ID {tileId} is out of range for tileset '{tilesetResRef}' ({tileset.Tiles.Count} tiles).");
                }
                else
                {
                    var tileDef = tileset.Tiles[tileId];
                    modelResRef = tileDef.Model;

                    if (string.IsNullOrWhiteSpace(modelResRef))
                    {
                        isFallback = true;
                        diagnostics.AddMissingModel(
                            $"tile #{i} (col {col}, row {row}): Tile_ID {tileId} in tileset '{tilesetResRef}' has no Model ResRef.");
                    }
                    else
                    {
                        model = modelCache.GetOrBuild(modelResRef);
                        if (model == null)
                        {
                            isFallback = true;
                            diagnostics.AddMissingModel(
                                $"tile #{i} (col {col}, row {row}): model '{modelResRef}' (Tile_ID {tileId}, tileset '{tilesetResRef}') could not be resolved/parsed.");
                        }
                    }
                }

                // Walkmesh resolution mirrors the model resolution above: only attempt it once a
                // real modelResRef resolved to a real model (i.e. this placement isn't a
                // fallback) - a tile with no usable model has no meaningful floor to snap to
                // either. A missing/unparseable .wok degrades to Walkmesh == null, same as a
                // missing/unparseable .mdl degrades to Model == null.
                WalkMesh? walkmesh = null;
                if (walkmeshes != null && !isFallback && modelResRef != null)
                    walkmesh = walkmeshes.GetOrBuild(modelResRef);

                placements.Add(new TilePlacement
                {
                    TileIndex = i,
                    Column = col,
                    Row = row,
                    TileId = tileId,
                    Orientation = orientation,
                    HeightLevel = tileHeight,
                    CenterX = centerX,
                    CenterY = centerY,
                    HeightOffset = heightOffset,
                    Transform = transform,
                    ModelResRef = modelResRef,
                    Model = model,
                    IsFallback = isFallback,
                    Walkmesh = walkmesh
                });
            }

            return placements;
        }

        private static List<InstanceMarker> BuildInstances(
            GitDocument git,
            TileModelCache modelCache,
            PlaceableAppearanceService? placeableAppearances,
            DoorTypeService? doorTypes,
            WaypointAppearanceService? waypointAppearances,
            Func<JsonGffStruct, RenderModel?>? resolveCreatureModel,
            IReadOnlyList<TilePlacement>? tiles = null)
        {
            var markers = new List<InstanceMarker>();

            // A creature's model cannot be resolved here: a humanoid body is assembled from a skeleton
            // and a dozen part models, which needs the composer that lives in the app layer. The caller
            // supplies it, and without one a creature falls back to its marker as before.
            AddMarkers(markers, git.Creatures, InstanceMarkerKind.Creature, ResourceType.Utc,
                resolveModel: resolveCreatureModel,
                modelCorrection: CreatureModelFacing.ForwardCorrection);
            AddMarkers(markers, git.Doors, InstanceMarkerKind.Door, ResourceType.Utd,
                resolveModel: instance => ResolveDoorModel(instance, doorTypes, modelCache),
                isDoorTransition: instance => IsDoorTransition(instance, doorTypes));
            AddMarkers(markers, git.Items, InstanceMarkerKind.Item, ResourceType.Uti);
            AddMarkers(markers, git.Placeables, InstanceMarkerKind.Placeable, ResourceType.Utp,
                resolveModel: instance => ResolvePlaceableModel(instance, placeableAppearances, modelCache));
            AddMarkers(markers, git.Sounds, InstanceMarkerKind.Sound, ResourceType.Uts);
            // Aurora gives stores a fixed yellow waypoint flag. A store carries no Appearance field,
            // so leaving this unresolved falls through to the generic pyramid: its apex is at the
            // store while its broad face hangs beside the merchant, and its symmetry loses facing.
            AddMarkers(markers, git.Stores, InstanceMarkerKind.Store, ResourceType.Utm,
                resolveModel: _ => modelCache.GetOrBuild(WaypointMarkerModel.MerchantModelResRef),
                modelCorrection: WaypointMarkerModel.ForwardCorrection);
            AddMarkers(markers, git.Triggers, InstanceMarkerKind.Trigger, ResourceType.Utt, includeGeometry: true,
                tiles: tiles);
            AddMarkers(markers, git.Waypoints, InstanceMarkerKind.Waypoint, ResourceType.Utw,
                resolveModel: instance => ResolveWaypointModel(instance, waypointAppearances, modelCache),
                modelCorrection: WaypointMarkerModel.ForwardCorrection);

            return markers;
        }

        /// <summary>
        /// Builds the render marker for one newly placed GIT instance through the same resolution path
        /// used by a complete scene build.
        /// </summary>
        /// <remarks>
        /// Object placement does not change the tile grid or any other instance. Exposing the single
        /// marker operation lets an editor publish that one addition immediately instead of serializing
        /// and parsing both area documents and rebuilding every tile and existing marker.
        /// </remarks>
        public static InstanceMarker BuildInstanceMarker(
            ResourceType type,
            JsonGffStruct instance,
            TileModelCache modelCache,
            PlaceableAppearanceService? placeableAppearances = null,
            DoorTypeService? doorTypes = null,
            WaypointAppearanceService? waypointAppearances = null,
            Func<JsonGffStruct, RenderModel?>? resolveCreatureModel = null,
            IReadOnlyList<TilePlacement>? tiles = null)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(modelCache);

            return type switch
            {
                ResourceType.Utc => BuildMarker(
                    instance, InstanceMarkerKind.Creature, type,
                    resolveModel: resolveCreatureModel,
                    modelCorrection: CreatureModelFacing.ForwardCorrection),
                ResourceType.Utd => BuildMarker(
                    instance, InstanceMarkerKind.Door, type,
                    resolveModel: placed => ResolveDoorModel(placed, doorTypes, modelCache),
                    isDoorTransition: placed => IsDoorTransition(placed, doorTypes)),
                ResourceType.Uti => BuildMarker(instance, InstanceMarkerKind.Item, type),
                ResourceType.Utp => BuildMarker(
                    instance, InstanceMarkerKind.Placeable, type,
                    resolveModel: placed => ResolvePlaceableModel(placed, placeableAppearances, modelCache)),
                ResourceType.Uts => BuildMarker(instance, InstanceMarkerKind.Sound, type),
                ResourceType.Utm => BuildMarker(
                    instance, InstanceMarkerKind.Store, type,
                    resolveModel: _ => modelCache.GetOrBuild(WaypointMarkerModel.MerchantModelResRef),
                    modelCorrection: WaypointMarkerModel.ForwardCorrection),
                ResourceType.Utt => BuildMarker(
                    instance, InstanceMarkerKind.Trigger, type,
                    includeGeometry: true,
                    tiles: tiles),
                ResourceType.Utw => BuildMarker(
                    instance, InstanceMarkerKind.Waypoint, type,
                    resolveModel: placed => ResolveWaypointModel(placed, waypointAppearances, modelCache),
                    modelCorrection: WaypointMarkerModel.ForwardCorrection),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(type), type, "This resource type is not an area instance.")
            };
        }

        private static void AddMarkers(
            List<InstanceMarker> markers,
            IReadOnlyList<JsonGffStruct> instances,
            InstanceMarkerKind kind,
            ResourceType type,
            bool includeGeometry = false,
            Func<JsonGffStruct, RenderModel?>? resolveModel = null,
            Matrix4x4? modelCorrection = null,
            IReadOnlyList<TilePlacement>? tiles = null,
            Func<JsonGffStruct, bool>? isDoorTransition = null)
        {
            foreach (var instance in instances)
                markers.Add(BuildMarker(
                    instance, kind, type, includeGeometry, resolveModel, modelCorrection, tiles,
                    isDoorTransition));
        }

        private static InstanceMarker BuildMarker(
            JsonGffStruct instance,
            InstanceMarkerKind kind,
            ResourceType type,
            bool includeGeometry = false,
            Func<JsonGffStruct, RenderModel?>? resolveModel = null,
            Matrix4x4? modelCorrection = null,
            IReadOnlyList<TilePlacement>? tiles = null,
            Func<JsonGffStruct, bool>? isDoorTransition = null)
        {
            var (x, y, z) = InstanceFieldMap.GetPosition(type, instance);
            var (xo, yo) = InstanceFieldMap.GetOrientation(type, instance);
            var templateResRef = InstanceFieldMap.GetTemplateResRef(type, instance);
            var tag = InstanceFieldMap.GetTag(instance);
            var position = new Vector3(x, y, z);
            var geometry = includeGeometry ? ReadGeometry(instance) : null;

            // Trigger Geometry is stored as offsets from X/Y/ZPosition. The stored PointZ is
            // whatever height each vertex happened to be clicked at and the corpus mixes floor
            // heights inside one flat polygon, so each vertex is draped onto the walkmesh under
            // it - the same thing Aurora does - keeping the stored Z only when no floor covers
            // that point (off-grid vertices, tiles with no resolvable .wok).
            if (geometry != null)
            {
                geometry = geometry.Select(point =>
                {
                    var world = point + position;
                    if (tiles != null && AreaWalkmesh.GroundHeightAt(tiles, world.X, world.Y) is { } floor)
                        world = new Vector3(world.X, world.Y, floor);
                    return world;
                }).ToArray();
            }

            return new InstanceMarker
            {
                Kind = kind,
                TemplateResRef = templateResRef,
                Tag = tag,
                Position = position,
                Orientation = new Vector2(xo, yo),
                // A model-space correction composes before the instance's own EE visual
                // transform, which is itself instance-local - see WaypointMarkerModel.
                VisualTransform = modelCorrection is { } correction
                    ? correction * InstanceFieldMap.GetVisualTransform(instance)
                    : InstanceFieldMap.GetVisualTransform(instance),
                Geometry = geometry,
                Model = resolveModel?.Invoke(instance),
                IsDoorTransition = isDoorTransition?.Invoke(instance) ?? false,
                TintMapOverrides = TintMapOverrides.Read(new VarTable(instance)),
                SoundMinDistance = kind == InstanceMarkerKind.Sound ? instance.GetSingleOrNull("MinDistance") : null,
                SoundMaxDistance = kind == InstanceMarkerKind.Sound ? instance.GetSingleOrNull("MaxDistance") : null,
                IsPositionalSound = kind == InstanceMarkerKind.Sound && (instance.GetIntOrNull("Positional") ?? 0) != 0
            };
        }

        /// <summary>Placeable instances carry their appearance directly: Appearance → placeables.2da ModelName.</summary>
        private static RenderModel? ResolvePlaceableModel(
            JsonGffStruct instance, PlaceableAppearanceService? placeableAppearances, TileModelCache modelCache)
        {
            if (placeableAppearances == null)
                return null;

            var appearanceId = instance.GetIntOrNull("Appearance") ?? -1;
            if (!placeableAppearances.TryGet(appearanceId, out var row))
                return null;

            return string.IsNullOrWhiteSpace(row.ModelName) ? null : modelCache.GetOrBuild(row.ModelName);
        }

        /// <summary>
        /// Resolves every placed tile's door nodes into world space.
        /// </summary>
        /// <remarks>
        /// The .set stores a door node in tile-local coordinates about the tile's own centre, which is
        /// exactly the space <see cref="TilePlacement.Transform"/> maps into the world - so the node
        /// rides through the same rotation and cell offset its tile did, and a doorway on a tile turned
        /// three times lands on the turned wall rather than the original one. The node's own
        /// orientation is in degrees and adds to the tile's quarter turns.
        /// <para>
        /// A tile whose model would not resolve still contributes its doorways. The nodes come from the
        /// .set, not from the geometry, so they are known whether or not the artwork loaded - and a
        /// corpus door proved the point: ar_scor_kvalinte's CryptExit hangs in a zdc01 tile that draws
        /// as a fallback here, and skipping it stranded a door Aurora had placed correctly.
        /// </para>
        /// </remarks>
        private static IReadOnlyList<TileDoorAnchor> BuildDoorAnchors(
            IReadOnlyList<TilePlacement> tiles, TilesetDefinition? tileset)
        {
            if (tileset == null)
                return Array.Empty<TileDoorAnchor>();

            var anchors = new List<TileDoorAnchor>();

            foreach (var placement in tiles)
            {
                if (placement.TileId < 0 || placement.TileId >= tileset.Tiles.Count)
                    continue;

                var doors = tileset.Tiles[placement.TileId].Doors;
                for (var doorIndex = 0; doorIndex < doors.Count; doorIndex++)
                {
                    var door = doors[doorIndex];
                    var local = new Vector3((float)door.X, (float)door.Y, (float)door.Z);
                    var heading =
                        (float)(door.Orientation * Math.PI / 180.0) + placement.Orientation * (float)(Math.PI / 2.0);

                    anchors.Add(new TileDoorAnchor
                    {
                        TileIndex = placement.TileIndex,
                        DoorIndex = doorIndex,
                        Type = door.Type,
                        Position = Vector3.Transform(local, placement.Transform),
                        Orientation = new Vector2(MathF.Cos(heading), MathF.Sin(heading))
                    });
                }
            }

            return anchors;
        }

        /// <summary>
        /// A waypoint instance's marker model, from waypoint.2da.
        /// </summary>
        /// <remarks>
        /// Waypoints are invisible in game, so it is easy to assume they have no artwork - but
        /// waypoint.2da holds 76 marker models (coloured flags, letters, treasure, mapnote, ...) and
        /// every placed waypoint names one. Drawing the real marker is what tells two waypoints apart
        /// on a map that may hold dozens.
        /// </remarks>
        private static RenderModel? ResolveWaypointModel(
            JsonGffStruct instance, WaypointAppearanceService? waypointAppearances, TileModelCache modelCache)
        {
            if (waypointAppearances == null)
                return null;

            var appearanceId = instance.GetIntOrNull("Appearance") ?? -1;
            if (!waypointAppearances.TryGet(appearanceId, out var row) ||
                string.IsNullOrWhiteSpace(row.ModelName))
                return null;

            return modelCache.GetOrBuild(row.ModelName);
        }

        /// <summary>
        /// A non-zero Appearance indexes doortypes.2da. Otherwise GenericType_New, with legacy
        /// GenericType as fallback, indexes genericdoors.2da.
        /// </summary>
        private static RenderModel? ResolveDoorModel(
            JsonGffStruct instance, DoorTypeService? doorTypes, TileModelCache modelCache)
        {
            if (doorTypes == null)
                return null;

            var appearance = instance.GetIntOrNull("Appearance") ?? 0;
            var specific = appearance > 0
                ? doorTypes.GetAll().FirstOrDefault(row => row.Id == appearance)
                : null;
            var generic = appearance == 0
                ? doorTypes.GetGenericAll().FirstOrDefault(row =>
                    row.Id == (instance.GetIntOrNull("GenericType_New")
                               ?? instance.GetIntOrNull("GenericType")
                               ?? 0))
                : null;
            var modelResRef = specific?.Model ?? generic?.Model;
            var visibleModel = specific?.VisibleModel ?? generic?.VisibleModel ?? true;

            return string.IsNullOrWhiteSpace(modelResRef)
                ? null
                : visibleModel
                    ? modelCache.GetOrBuild(modelResRef)
                    : modelCache.GetOrBuildDoorTransition(modelResRef);
        }

        /// <summary>
        /// <c>VisibleModel=0</c> is how both genericdoors.2da and doortypes.2da identify the
        /// invisible area-transition planes that Aurora still shows while editing.
        /// </summary>
        private static bool IsDoorTransition(JsonGffStruct instance, DoorTypeService? doorTypes)
        {
            if (doorTypes == null)
                return false;

            var appearance = instance.GetIntOrNull("Appearance") ?? 0;
            if (appearance > 0)
                return doorTypes.GetAll().FirstOrDefault(row => row.Id == appearance) is { VisibleModel: false };

            var genericType = instance.GetIntOrNull("GenericType_New")
                              ?? instance.GetIntOrNull("GenericType")
                              ?? 0;
            return doorTypes.GetGenericAll().FirstOrDefault(row => row.Id == genericType) is { VisibleModel: false };
        }

        private static IReadOnlyList<Vector3>? ReadGeometry(JsonGffStruct instance)
        {
            var points = instance.GetListOrEmpty("Geometry");
            if (points.Count == 0)
                return null;

            var result = new List<Vector3>(points.Count);
            foreach (var point in points)
            {
                var x = point.GetSingleOrNull("PointX") ?? 0f;
                var y = point.GetSingleOrNull("PointY") ?? 0f;
                var z = point.GetSingleOrNull("PointZ") ?? 0f;
                result.Add(new Vector3(x, y, z));
            }

            return result;
        }

    }
}
