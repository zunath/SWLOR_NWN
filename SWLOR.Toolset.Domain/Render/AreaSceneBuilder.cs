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
            TileWalkmeshCache? walkmeshes = null)
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
            var instances = BuildInstances(git, modelCache, placeableAppearances, doorTypes);

            return new AreaScene
            {
                Tileset = tilesetResRef,
                Width = width,
                Height = height,
                Tiles = tiles,
                Instances = instances,
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
                IsNight = isNight
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
                            $"tile #{i} (col {col}, row {row}): Tile_ID {tileId} in tileset '{tilesetResRef}' has no Model resref.");
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
            DoorTypeService? doorTypes)
        {
            var markers = new List<InstanceMarker>();

            AddMarkers(markers, git.Creatures, InstanceMarkerKind.Creature, ResourceType.Utc);
            AddMarkers(markers, git.Doors, InstanceMarkerKind.Door, ResourceType.Utd,
                resolveModel: instance => ResolveDoorModel(instance, doorTypes, modelCache));
            AddMarkers(markers, git.Items, InstanceMarkerKind.Item, ResourceType.Uti);
            AddMarkers(markers, git.Placeables, InstanceMarkerKind.Placeable, ResourceType.Utp,
                resolveModel: instance => ResolvePlaceableModel(instance, placeableAppearances, modelCache));
            AddMarkers(markers, git.Sounds, InstanceMarkerKind.Sound, ResourceType.Uts);
            AddMarkers(markers, git.Stores, InstanceMarkerKind.Store, ResourceType.Utm);
            AddMarkers(markers, git.Triggers, InstanceMarkerKind.Trigger, ResourceType.Utt, includeGeometry: true);
            AddMarkers(markers, git.Waypoints, InstanceMarkerKind.Waypoint, ResourceType.Utw);
            AddEncounterMarkers(markers, git.Encounters);

            return markers;
        }

        private static void AddMarkers(
            List<InstanceMarker> markers,
            IReadOnlyList<JsonGffStruct> instances,
            InstanceMarkerKind kind,
            ResourceType type,
            bool includeGeometry = false,
            Func<JsonGffStruct, RenderModel?>? resolveModel = null)
        {
            foreach (var instance in instances)
            {
                var (x, y, z) = InstanceFieldMap.GetPosition(type, instance);
                var (xo, yo) = InstanceFieldMap.GetOrientation(type, instance);
                var templateResRef = InstanceFieldMap.GetTemplateResRef(type, instance);
                var tag = InstanceFieldMap.GetTag(instance);
                var position = new Vector3(x, y, z);
                var geometry = includeGeometry ? ReadGeometry(instance) : null;

                // Trigger Geometry is stored as offsets from X/Y/ZPosition. Encounters are built
                // separately below because their Geometry is already world-space and they carry
                // no standalone position fields.
                if (geometry != null)
                    geometry = geometry.Select(point => point + position).ToArray();

                markers.Add(new InstanceMarker
                {
                    Kind = kind,
                    TemplateResRef = templateResRef,
                    Tag = tag,
                    Position = position,
                    Orientation = new Vector2(xo, yo),
                    VisualTransform = InstanceFieldMap.GetVisualTransform(instance),
                    Geometry = geometry,
                    Model = resolveModel?.Invoke(instance)
                });
            }
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
        /// Door instances carry two candidate doortypes.2da indices — GenericType_New and Appearance —
        /// and the corpus uses either (e.g. GenericType_New=0 with Appearance=24). Take the first
        /// whose row yields a real model.
        /// </summary>
        private static RenderModel? ResolveDoorModel(
            JsonGffStruct instance, DoorTypeService? doorTypes, TileModelCache modelCache)
        {
            if (doorTypes == null)
                return null;

            foreach (var field in new[] { "GenericType_New", "Appearance" })
            {
                var id = instance.GetIntOrNull(field) ?? -1;
                if (id <= 0)
                    continue;

                var row = doorTypes.GetAll().FirstOrDefault(r => r.Id == id);
                if (string.IsNullOrWhiteSpace(row?.Model))
                    continue;

                var model = modelCache.GetOrBuild(row.Model);
                if (model != null)
                    return model;
            }

            return null;
        }

        /// <summary>
        /// Encounters carry no single X/Y/Z field in the Aurora GIT format - unlike every other
        /// supported instance list, an encounter is defined by a Geometry polygon (same shape as
        /// TriggerList's) plus a separate spawn-point list. No corpus area in this repo actually
        /// has an Encounter List entry (corpus verification confirms zero), so this path is
        /// unverified against real data; it is written defensively (every field read is
        /// null-tolerant) so a future authored encounter still assembles instead of throwing. The
        /// reported Position is the Geometry polygon's centroid when present, else the origin.
        /// </summary>
        private static void AddEncounterMarkers(List<InstanceMarker> markers, IReadOnlyList<JsonGffStruct> instances)
        {
            foreach (var instance in instances)
            {
                var tag = InstanceFieldMap.GetTag(instance);
                var geometry = ReadGeometry(instance);
                var position = geometry is { Count: > 0 } ? Centroid(geometry) : Vector3.Zero;

                markers.Add(new InstanceMarker
                {
                    Kind = InstanceMarkerKind.Encounter,
                    TemplateResRef = null,
                    Tag = tag,
                    Position = position,
                    Orientation = new Vector2(1f, 0f),
                    Geometry = geometry
                });
            }
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

        private static Vector3 Centroid(IReadOnlyList<Vector3> points)
        {
            var sum = Vector3.Zero;
            foreach (var point in points)
                sum += point;

            return sum / points.Count;
        }
    }
}
