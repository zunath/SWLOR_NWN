#nullable enable
using Serilog;
using SWLOR.NWN.Formats;
using SWLOR.NWN.Formats.TwoDA;
using SWLOR.Toolset.Domain.AreaGeneration.Atmosphere;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>Projects one solved draft into a fresh toolset area triplet.</summary>
    public sealed class GeneratedAreaDocumentPopulator
    {
        private const string WaypointList = "WaypointList";
        private const string DoorList = "Door List";
        private const string PlaceableList = "Placeable List";
        private const string CreatureList = "Creature List";
        private const string GeneratedTreasureOpenScript = "proc_loot_open";
        private const int HostileFactionId = 1;
        private const float TreasureAnchorClearance = 3f;
        private const float DefaultCreatureRadius = 1f;
        private const float GeneratedObjectRadius = 1f;
        private static readonly ILogger Logger = Log.ForContext<GeneratedAreaDocumentPopulator>();

        private GeneratedAreaDocumentPopulator()
        {
        }

        public static void Populate(
            AreaGenerationDraft draft,
            ModuleWorkspace workspace,
            AreDocument are,
            GitDocument git,
            GicDocument gic)
        {
            ArgumentNullException.ThrowIfNull(draft);
            ArgumentNullException.ThrowIfNull(workspace);
            ArgumentNullException.ThrowIfNull(are);
            ArgumentNullException.ThrowIfNull(git);
            ArgumentNullException.ThrowIfNull(gic);
            if (!draft.Result.Success || draft.Result.Resolved == null)
                throw new InvalidOperationException("Only a successful generated draft can be written.");

            var layout = draft.Result.Resolved;
            var originalWaypointCount = git.Fields.GetListOrEmpty(WaypointList).Count;
            var originalDoorCount = git.Fields.GetListOrEmpty(DoorList).Count;
            var originalPlaceableCount = git.Fields.GetListOrEmpty(PlaceableList).Count;
            var originalCreatureCount = git.Fields.GetListOrEmpty(CreatureList).Count;
            Logger.Information(
                "Populating generated area documents for a {Width}x{Height} layout with " +
                "{TransitionCount} transitions and {DecorationCount} planned decorations.",
                layout.Width,
                layout.Height,
                layout.Transitions.Count,
                draft.Result.PlannedDecorations.Count);

            WriteTiles(are, layout, draft.Composition.Tileset.Lighting);
            WriteAtmosphere(
                are,
                draft.Composition.Tileset.ResolveAtmosphere(draft.Composition.Content.AtmosphereProfile));
            WriteTransitions(draft, workspace, git, gic);
            WriteTreasure(draft, workspace, git, gic);
            WriteCreatures(draft, workspace, git, gic);
            WriteDecorations(draft, workspace, git, gic);

            Logger.Information(
                "Populated generated area documents for a {Width}x{Height} layout: " +
                "{WaypointCount} waypoints, {DoorCount} doors, {PlaceableCount} placeables, and " +
                "{CreatureCount} creatures added.",
                layout.Width,
                layout.Height,
                git.Fields.GetListOrEmpty(WaypointList).Count - originalWaypointCount,
                git.Fields.GetListOrEmpty(DoorList).Count - originalDoorCount,
                git.Fields.GetListOrEmpty(PlaceableList).Count - originalPlaceableCount,
                git.Fields.GetListOrEmpty(CreatureList).Count - originalCreatureCount);
        }

        private static void WriteTiles(
            AreDocument are,
            ResolvedLayout layout,
            DungeonTileLighting lighting)
        {
            for (var y = 0; y < layout.Height; y++)
            for (var x = 0; x < layout.Width; x++)
            {
                var resolved = layout.Tiles[y * layout.Width + x];
                AreaTiles.SetTile(are, x, y, resolved.TileId, resolved.Orientation);
                AreaTiles.SetHeightLevel(are, x, y, resolved.Height);

                var index = AreaTiles.IndexOf(are, x, y);
                var tile = are.Tiles[index];
                tile.SetInt("Tile_MainLight1", GffFieldType.Byte, lighting.MainLight1);
                tile.SetInt("Tile_MainLight2", GffFieldType.Byte, lighting.MainLight2);
                tile.SetInt("Tile_SrcLight1", GffFieldType.Byte, lighting.SourceLight1);
                tile.SetInt("Tile_SrcLight2", GffFieldType.Byte, lighting.SourceLight2);
            }
        }

        private static void WriteAtmosphere(AreDocument are, DungeonAreaAtmosphere? atmosphere)
        {
            if (atmosphere == null)
                return;

            var root = are.Fields;
            root.SetInt("SkyBox", GffFieldType.Byte, atmosphere.SkyBox);
            root.SetInt("DayNightCycle", GffFieldType.Byte, atmosphere.DayNightCycle ? 1 : 0);
            root.SetInt("IsNight", GffFieldType.Byte, atmosphere.IsNight ? 1 : 0);
            root.SetUInt("SunAmbientColor", GffFieldType.Dword, unchecked((uint)atmosphere.SunAmbientColor));
            root.SetUInt("SunDiffuseColor", GffFieldType.Dword, unchecked((uint)atmosphere.SunDiffuseColor));
            root.SetUInt("MoonAmbientColor", GffFieldType.Dword, unchecked((uint)atmosphere.MoonAmbientColor));
            root.SetUInt("MoonDiffuseColor", GffFieldType.Dword, unchecked((uint)atmosphere.MoonDiffuseColor));
            root.SetInt("SunFogAmount", GffFieldType.Byte, atmosphere.SunFogAmount);
            root.SetUInt("SunFogColor", GffFieldType.Dword, unchecked((uint)atmosphere.SunFogColor));
            root.SetInt("MoonFogAmount", GffFieldType.Byte, atmosphere.MoonFogAmount);
            root.SetUInt("MoonFogColor", GffFieldType.Dword, unchecked((uint)atmosphere.MoonFogColor));
            root.SetInt("SunShadows", GffFieldType.Byte, atmosphere.SunShadows ? 1 : 0);
            root.SetInt("MoonShadows", GffFieldType.Byte, atmosphere.MoonShadows ? 1 : 0);
            root.SetInt("ShadowOpacity", GffFieldType.Byte, atmosphere.ShadowOpacity);
            root.SetInt("WindPower", GffFieldType.Int, atmosphere.WindPower);
            root.SetInt("ChanceRain", GffFieldType.Int, atmosphere.ChanceRain);
            root.SetInt("ChanceSnow", GffFieldType.Int, atmosphere.ChanceSnow);
            root.SetInt("ChanceLightning", GffFieldType.Int, atmosphere.ChanceLightning);
            root.SetInt("LightingScheme", GffFieldType.Byte, atmosphere.LightingScheme);
            root.SetSingle("FogClipDist", atmosphere.FogClipDist);
            if (atmosphere.LoadScreenId.HasValue)
                root.SetInt("LoadScreenID", GffFieldType.Word, atmosphere.LoadScreenId.Value);
        }

        private static void WriteTransitions(
            AreaGenerationDraft draft,
            ModuleWorkspace workspace,
            GitDocument git,
            GicDocument gic)
        {
            var entranceCount = 0;
            var exitCount = 0;
            foreach (var transition in draft.Result.Resolved.Transitions)
            {
                var isEntrance = transition.Kind == TransitionKind.Entrance;
                var index = isEntrance ? ++entranceCount : ++exitCount;
                var label = isEntrance ? "Entrance" : "Exit";
                var tag = (isEntrance ? "PG_ENT_" : "PG_EXIT_") + index;

                var anchorX = transition.Tile.X * 10f + 5f;
                var anchorY = transition.Tile.Y * 10f + 5f;
                var anchorZ = GroundHeightAt(
                    draft.Result.Resolved,
                    draft.Tileset,
                    anchorX,
                    anchorY);
                var waypointX = anchorX;
                var waypointY = anchorY;
                var waypointZ = anchorZ;
                if (transition.Style != TransitionStyle.Placeable)
                {
                    var dx = anchorX - transition.DoorX;
                    var dy = anchorY - transition.DoorY;
                    var length = MathF.Sqrt(dx * dx + dy * dy);
                    if (length > 0.01f)
                    {
                        waypointX = transition.DoorX + dx / length * 2f;
                        waypointY = transition.DoorY + dy / length * 2f;
                    }

                    waypointZ = transition.DoorZ;
                }

                AddWaypoint(git, gic, $"PG {label} {index}", tag, waypointX, waypointY, waypointZ);

                if (transition.Style is TransitionStyle.Door or TransitionStyle.GroupExit)
                {
                    AddDoor(
                        workspace,
                        git,
                        gic,
                        draft.Composition.Content.ExitDoorResref,
                        draft.Composition.Content.ExitDisplayName,
                        transition.DoorType,
                        $"PG_DOOR_{(isEntrance ? "ENT" : "EXIT")}_{index}",
                        transition.DoorX,
                        transition.DoorY,
                        transition.DoorZ,
                        transition.DoorOrientation);
                }
                else
                {
                    AddPlaceable(
                        workspace,
                        git,
                        gic,
                        draft.Composition.Content.ExitPlaceableResref,
                        $"PG_TRANS_{(isEntrance ? "ENT" : "EXIT")}_{index}",
                        anchorX,
                        anchorY,
                        anchorZ,
                        facingDegrees: 0f,
                        visualScale: 1f,
                        displayName: draft.Composition.Content.ExitDisplayName,
                        clearInheritedTransitionBehavior: true);
                }
            }
        }

        private static void AddWaypoint(
            GitDocument git,
            GicDocument gic,
            string name,
            string tag,
            float x,
            float y,
            float z)
        {
            var instance = JsonGffField.CreateStruct(5).Struct!;
            instance.SetInt("Appearance", GffFieldType.Byte, 1);
            instance.GetOrAddLocString("Description");
            instance.SetInt("HasMapNote", GffFieldType.Byte, 0);
            instance.SetString("LinkedTo", GffFieldType.CExoString, string.Empty);
            instance.GetOrAddLocString("LocalizedName").Text = name;
            instance.GetOrAddLocString("MapNote");
            instance.SetInt("MapNoteEnabled", GffFieldType.Byte, 0);
            instance.SetString("Tag", GffFieldType.CExoString, tag);
            instance.SetString("TemplateResRef", GffFieldType.ResRef, "nw_waypoint001");
            instance.SetSingle("XOrientation", 0f);
            instance.SetSingle("XPosition", x);
            instance.SetSingle("YOrientation", 1f);
            instance.SetSingle("YPosition", y);
            instance.SetSingle("ZPosition", z);

            var list = git.Fields.GetOrAddList(WaypointList);
            list.Add(instance);
            gic.InsertBlankComment(WaypointList, ResourceType.Utw, list.Count - 1, list.Count);
        }

        private static void AddDoor(
            ModuleWorkspace workspace,
            GitDocument git,
            GicDocument gic,
            string resref,
            string displayName,
            int doorType,
            string tag,
            float x,
            float y,
            float z,
            float facingDegrees)
        {
            var blueprint = workspace.LoadBlueprint(ResourceType.Utd, resref);
            var radians = facingDegrees * Math.PI / 180.0;
            var instance = InstanceFieldMap.CreateInstance(
                ResourceType.Utd,
                blueprint.Document,
                resref,
                x,
                y,
                z,
                Math.Cos(radians),
                Math.Sin(radians));
            InstanceFieldMap.SetTag(instance, tag);
            if (doorType > 0)
            {
                instance.SetInt("Appearance", GffFieldType.Dword, doorType);
                instance.SetInt("GenericType_New", GffFieldType.Dword, 0);
            }
            if (!string.IsNullOrWhiteSpace(displayName))
                instance.GetOrAddLocString("LocName").Text = displayName;

            var list = git.Fields.GetOrAddList(DoorList);
            list.Add(instance);
            gic.InsertBlankComment(DoorList, ResourceType.Utd, list.Count - 1, list.Count);
        }

        private static void WriteDecorations(
            AreaGenerationDraft draft,
            ModuleWorkspace workspace,
            GitDocument git,
            GicDocument gic)
        {
            var sequence = 0;
            foreach (var planned in draft.Result.PlannedDecorations)
            {
                var groundPoint = planned.GroundAnchor ?? new System.Numerics.Vector2(
                    planned.Position.X,
                    planned.Position.Y);
                var groundZ = GroundHeightAt(
                    draft.Result.Resolved,
                    draft.Tileset,
                    groundPoint.X,
                    groundPoint.Y);
                AddPlaceable(
                    workspace,
                    git,
                    gic,
                    planned.Resref,
                    $"PG_DEC_{++sequence}",
                    planned.Position.X,
                    planned.Position.Y,
                    groundZ + planned.Position.Z,
                    planned.Facing,
                    planned.VisualScale);
            }
        }

        /// <summary>Places the theme's configured treasure container at the Boss room anchor.</summary>
        private static void WriteTreasure(
            AreaGenerationDraft draft,
            ModuleWorkspace workspace,
            GitDocument git,
            GicDocument gic)
        {
            var bossRoom = draft.Result.Resolved.Rooms.FirstOrDefault(room => room.Role == RoomRole.Boss);
            if (bossRoom == null)
                return;

            if (!draft.Composition.Content.Tiers.TryGetValue(draft.Settings.Tier, out var tier))
            {
                throw new InvalidOperationException(
                    $"Theme '{draft.Composition.Content.DisplayName}' does not define tier {draft.Settings.Tier}.");
            }

            var (x, y) = FindTreasureAnchor(draft, bossRoom);
            var z = GroundHeightAt(draft.Result.Resolved, draft.Tileset, x, y);
            AddPlaceable(
                workspace,
                git,
                gic,
                draft.Composition.Content.TreasurePlaceableResref,
                "PG_TREASURE",
                x,
                y,
                z,
                facingDegrees: 0f,
                visualScale: 1f,
                displayName: draft.Composition.Content.TreasureDisplayName,
                configureInstance: instance => ConfigureTreasure(instance, tier));
        }

        private static void ConfigureTreasure(JsonGffStruct instance, DungeonTierDetail tier)
        {
            if (string.IsNullOrWhiteSpace(tier.TreasureLootTableId) || tier.TreasureItemCount < 1)
                throw new InvalidOperationException($"Tier {tier.Tier} has invalid treasure settings.");

            instance.SetInt("Useable", GffFieldType.Byte, 1);
            instance.SetInt("HasInventory", GffFieldType.Byte, 1);
            instance.SetInt("Static", GffFieldType.Byte, 0);
            instance.SetString("OnOpen", GffFieldType.ResRef, GeneratedTreasureOpenScript);
            instance.SetString("OnClosed", GffFieldType.ResRef, string.Empty);
            instance.SetString("OnInvDisturbed", GffFieldType.ResRef, string.Empty);

            var variables = new VarTable(instance);
            variables.Remove("SCAVENGE_POINT_LEVEL");
            variables.Remove("SCAVENGE_POINT_LOOT_TABLE_NAME");
            variables.SetString(
                "LOOT_TABLE_1",
                $"{tier.TreasureLootTableId},100,{tier.TreasureItemCount}");
        }

        /// <summary>Populates Standard rooms and the Boss room from the selected tier.</summary>
        private static void WriteCreatures(
            AreaGenerationDraft draft,
            ModuleWorkspace workspace,
            GitDocument git,
            GicDocument gic)
        {
            foreach (var placement in PlanCreatures(draft, workspace))
            {
                AddCreature(
                    git,
                    gic,
                    placement.Spawn,
                    placement.X,
                    placement.Y,
                    placement.Z,
                    placement.FacingDegrees);
            }
        }

        /// <summary>
        /// Runs the exact deterministic encounter plan used during area writing, without changing
        /// documents. The authoring service calls this before accepting a draft for preview.
        /// </summary>
        public static void ValidateEncounterPlacement(
            AreaGenerationDraft draft,
            ModuleWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(draft);
            ArgumentNullException.ThrowIfNull(workspace);
            _ = PlanCreatures(draft, workspace);
        }

        private static IReadOnlyList<CreaturePlacement> PlanCreatures(
            AreaGenerationDraft draft,
            ModuleWorkspace workspace)
        {
            if (!draft.Composition.Content.Tiers.TryGetValue(draft.Settings.Tier, out var tier))
            {
                throw new InvalidOperationException(
                    $"Theme '{draft.Composition.Content.DisplayName}' does not define tier {draft.Settings.Tier}.");
            }

            var creaturePool = tier.Creatures
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Resref) && entry.Weight > 0)
                .ToList();
            if (creaturePool.Count == 0 ||
                tier.MinCreaturesPerRoom < 0 ||
                tier.MaxCreaturesPerRoom < tier.MinCreaturesPerRoom ||
                string.IsNullOrWhiteSpace(tier.BossResref))
            {
                throw new InvalidOperationException($"Tier {tier.Tier} has invalid creature settings.");
            }

            var random = new Random(unchecked(
                draft.Settings.Seed * 31 ^
                draft.Result.AttemptSeed * 397 ^
                draft.Settings.Tier * 7919 ^
                0x51ED270B));
            var occupied = CreatureOccupiedAnchors(draft);
            var appearanceTable = LoadCreatureAppearanceTable(workspace.ResourceIndex);
            var placements = new List<CreaturePlacement>();

            foreach (var room in draft.Result.Resolved.Rooms
                         .Where(room => room.Role == RoomRole.Standard)
                         .OrderBy(room => room.Id))
            {
                var count = random.Next(tier.MinCreaturesPerRoom, tier.MaxCreaturesPerRoom + 1);
                var spawns = Enumerable.Range(0, count)
                    .Select(_ => LoadCreatureSpawn(
                        workspace,
                        ChooseCreatureResref(creaturePool, random),
                        appearanceTable))
                    .OrderByDescending(spawn => spawn.Radius)
                    .ToList();
                var anchors = SelectCreatureAnchors(
                    draft.Result.Resolved,
                    room,
                    spawns.Select(spawn => spawn.Radius).ToList(),
                    occupied,
                    random);
                for (var index = 0; index < spawns.Count; index++)
                {
                    var spawn = spawns[index];
                    var (x, y) = anchors[index];
                    placements.Add(new CreaturePlacement(
                        spawn,
                        x,
                        y,
                        GroundHeightAt(draft.Result.Resolved, draft.Tileset, x, y),
                        (float)(random.NextDouble() * 360.0)));
                }
            }

            var bossRoom = draft.Result.Resolved.Rooms.FirstOrDefault(room => room.Role == RoomRole.Boss);
            if (bossRoom == null)
                return placements;

            var bossSpawn = LoadCreatureSpawn(workspace, tier.BossResref, appearanceTable);
            var bossAnchor = SelectCreatureAnchors(
                draft.Result.Resolved,
                bossRoom,
                [bossSpawn.Radius],
                occupied,
                random).Single();
            placements.Add(new CreaturePlacement(
                bossSpawn,
                bossAnchor.X,
                bossAnchor.Y,
                GroundHeightAt(draft.Result.Resolved, draft.Tileset, bossAnchor.X, bossAnchor.Y),
                (float)(random.NextDouble() * 360.0)));
            return placements;
        }

        private static List<(float X, float Y, float Radius)> CreatureOccupiedAnchors(
            AreaGenerationDraft draft)
        {
            var occupied = draft.Result.Resolved.Transitions
                .Select(transition => transition.Style == TransitionStyle.Placeable
                    ? (transition.Tile.X * 10f + 5f, transition.Tile.Y * 10f + 5f, GeneratedObjectRadius)
                    : (transition.DoorX, transition.DoorY, GeneratedObjectRadius))
                .ToList();
            occupied.AddRange(draft.Result.PlannedDecorations.Select(decoration =>
                (decoration.Position.X, decoration.Position.Y, GeneratedObjectRadius)));

            var bossRoom = draft.Result.Resolved.Rooms.FirstOrDefault(room => room.Role == RoomRole.Boss);
            if (bossRoom != null)
            {
                var treasure = FindTreasureAnchor(draft, bossRoom);
                occupied.Add((treasure.X, treasure.Y, GeneratedObjectRadius));
            }

            return occupied;
        }

        internal static IReadOnlyList<(float X, float Y)> SelectCreatureAnchors(
            ResolvedLayout resolved,
            LayoutRoom room,
            IReadOnlyList<float> creatureRadii,
            ICollection<(float X, float Y, float Radius)> occupied,
            Random random)
        {
            if (creatureRadii.Count == 0)
                return Array.Empty<(float X, float Y)>();

            var tiles = room.Tiles
                .DefaultIfEmpty(room.CenterTile)
                .Distinct()
                .Where(tile =>
                    !resolved.FeatureTileCells.ContainsKey(tile) &&
                    !resolved.StampedStructureTiles.Contains(tile))
                .ToHashSet();
            if (tiles.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Room {room.Id} has no creature anchor clear of feature geometry and stamped structures.");
            }

            var boundarySegments = CreatureBoundarySegments(tiles);
            var candidatesByCreature = new List<List<(float X, float Y)>>(creatureRadii.Count);
            foreach (var radius in creatureRadii)
            {
                var candidates = new List<(float X, float Y)>();
                foreach (var tile in tiles)
                {
                    for (var offsetY = 0.5f; offsetY < 10f; offsetY += 1f)
                    for (var offsetX = 0.5f; offsetX < 10f; offsetX += 1f)
                    {
                        var candidate = (tile.X * 10f + offsetX, tile.Y * 10f + offsetY);
                        if (HasCreatureBoundaryClearance(candidate, radius, boundarySegments))
                            candidates.Add(candidate);
                    }
                }

                for (var index = candidates.Count - 1; index > 0; index--)
                {
                    var swapIndex = random.Next(index + 1);
                    (candidates[index], candidates[swapIndex]) = (candidates[swapIndex], candidates[index]);
                }

                if (candidates.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Room {room.Id} cannot fit a creature with a {radius:0.##}m collision radius.");
                }

                candidatesByCreature.Add(candidates);
            }

            var selected = new List<(float X, float Y, float Radius)>(creatureRadii.Count);
            bool TryPlace(int creatureIndex)
            {
                if (creatureIndex == creatureRadii.Count)
                    return true;

                var radius = creatureRadii[creatureIndex];
                foreach (var candidate in candidatesByCreature[creatureIndex])
                {
                    if (!occupied.Concat(selected).All(point =>
                            DistanceSquared(candidate, (point.X, point.Y)) + 0.0001f >=
                            (radius + point.Radius) * (radius + point.Radius)))
                    {
                        continue;
                    }

                    selected.Add((candidate.X, candidate.Y, radius));
                    if (TryPlace(creatureIndex + 1))
                        return true;
                    selected.RemoveAt(selected.Count - 1);
                }

                return false;
            }

            if (!TryPlace(0))
            {
                throw new InvalidOperationException(
                    $"Room {room.Id} cannot fit {creatureRadii.Count} creatures without overlapping geometry.");
            }

            foreach (var anchor in selected)
                occupied.Add(anchor);
            return selected.Select(anchor => (anchor.X, anchor.Y)).ToList();
        }

        private static IReadOnlyList<((float X, float Y) Start, (float X, float Y) End)>
            CreatureBoundarySegments(IReadOnlySet<(int X, int Y)> tiles)
        {
            var segments = new List<((float X, float Y), (float X, float Y))>();
            foreach (var tile in tiles)
            {
                var left = tile.X * 10f;
                var right = left + 10f;
                var bottom = tile.Y * 10f;
                var top = bottom + 10f;
                if (!tiles.Contains((tile.X - 1, tile.Y)))
                    segments.Add(((left, bottom), (left, top)));
                if (!tiles.Contains((tile.X + 1, tile.Y)))
                    segments.Add(((right, bottom), (right, top)));
                if (!tiles.Contains((tile.X, tile.Y - 1)))
                    segments.Add(((left, bottom), (right, bottom)));
                if (!tiles.Contains((tile.X, tile.Y + 1)))
                    segments.Add(((left, top), (right, top)));
            }

            return segments;
        }

        private static bool HasCreatureBoundaryClearance(
            (float X, float Y) candidate,
            float radius,
            IReadOnlyList<((float X, float Y) Start, (float X, float Y) End)> boundarySegments)
        {
            var requiredDistanceSquared = radius * radius;
            foreach (var segment in boundarySegments)
            {
                if (DistanceSquaredToSegment(candidate, segment.Start, segment.End) < requiredDistanceSquared)
                    return false;
            }

            return true;
        }

        private static float DistanceSquaredToSegment(
            (float X, float Y) point,
            (float X, float Y) start,
            (float X, float Y) end)
        {
            var segmentX = end.X - start.X;
            var segmentY = end.Y - start.Y;
            var lengthSquared = segmentX * segmentX + segmentY * segmentY;
            var projection = ((point.X - start.X) * segmentX + (point.Y - start.Y) * segmentY) /
                             lengthSquared;
            projection = Math.Clamp(projection, 0f, 1f);
            var closest = (start.X + projection * segmentX, start.Y + projection * segmentY);
            return DistanceSquared(point, closest);
        }

        private static string ChooseCreatureResref(
            IReadOnlyList<DungeonCreatureEntry> creatures,
            Random random)
        {
            var totalWeight = creatures.Sum(entry => entry.Weight);
            var roll = random.Next(totalWeight);
            foreach (var creature in creatures)
            {
                if (roll < creature.Weight)
                    return creature.Resref;
                roll -= creature.Weight;
            }

            return creatures[^1].Resref;
        }

        private sealed record CreatureSpawn(string Resref, JsonGffDocument Blueprint, float Radius);
        private sealed record CreaturePlacement(
            CreatureSpawn Spawn,
            float X,
            float Y,
            float Z,
            float FacingDegrees);

        private static CreatureSpawn LoadCreatureSpawn(
            ModuleWorkspace workspace,
            string resref,
            TwoDaTable? appearanceTable)
        {
            var blueprint = workspace.LoadBlueprint(ResourceType.Utc, resref).Document;
            var radius = DefaultCreatureRadius;
            if (appearanceTable != null)
            {
                var appearanceId = blueprint.Root.GetIntOrNull("Appearance_Type") ?? -1;
                var rawRadius = appearanceTable.GetString(appearanceId, "CREPERSPACE");
                if (!float.TryParse(
                        rawRadius,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out radius) ||
                    radius <= 0f)
                {
                    throw new InvalidOperationException(
                        $"Creature blueprint '{resref}' has no valid CREPERSPACE in appearance.2da row {appearanceId}.");
                }
            }

            return new CreatureSpawn(resref, blueprint, radius);
        }

        private static TwoDaTable? LoadCreatureAppearanceTable(ResourceIndex? resourceIndex)
        {
            if (resourceIndex == null)
                return null;

            var identity = new ResourceIdentity("appearance", ResourceIdentity.TypeFromExtension("2da"));
            if (!resourceIndex.TryLookup(identity, out var resource))
                throw new InvalidOperationException("appearance.2da is unavailable for creature placement.");

            try
            {
                return new TwoDaTable("appearance", TwoDAReader.Read(resource.GetBytes()));
            }
            catch (NwnFormatException ex)
            {
                throw new InvalidOperationException(
                    "appearance.2da could not be read for creature placement.", ex);
            }
        }

        private static void AddCreature(
            GitDocument git,
            GicDocument gic,
            CreatureSpawn spawn,
            float x,
            float y,
            float z,
            float facingDegrees)
        {
            var radians = facingDegrees * Math.PI / 180.0;
            var instance = InstanceFieldMap.CreateInstance(
                ResourceType.Utc,
                spawn.Blueprint,
                spawn.Resref,
                x,
                y,
                z,
                Math.Cos(radians),
                Math.Sin(radians));
            instance.SetInt("FactionID", GffFieldType.Word, HostileFactionId);
            instance.SetInt("IsImmortal", GffFieldType.Byte, 0);
            var variables = new VarTable(instance);
            foreach (var questLocal in variables
                         .Select(entry => entry.Name)
                         .Where(name => name.StartsWith("QUEST_", StringComparison.Ordinal))
                         .ToList())
            {
                variables.Remove(questLocal);
            }

            var list = git.Fields.GetOrAddList(CreatureList);
            list.Add(instance);
            gic.InsertBlankComment(CreatureList, ResourceType.Utc, list.Count - 1, list.Count);
        }

        /// <summary>
        /// Prefers the Boss room center, then other room-tile centers and inset quarter points that
        /// stay clear of generated transitions and decorations. A one-tile Boss room can also host
        /// the required placeable exit, so its center is not unconditionally safe.
        /// </summary>
        internal static (float X, float Y) FindTreasureAnchor(
            AreaGenerationDraft draft,
            LayoutRoom bossRoom)
        {
            var tiles = bossRoom.Tiles
                .DefaultIfEmpty(bossRoom.CenterTile)
                .Distinct()
                .OrderBy(tile => Math.Abs(tile.X - bossRoom.CenterTile.X) + Math.Abs(tile.Y - bossRoom.CenterTile.Y))
                .ThenBy(tile => tile.Y)
                .ThenBy(tile => tile.X)
                .ToList();
            var candidates = new List<(float X, float Y)>
            {
                (bossRoom.CenterTile.X * 10f + 5f, bossRoom.CenterTile.Y * 10f + 5f)
            };
            foreach (var tile in tiles)
                candidates.Add((tile.X * 10f + 5f, tile.Y * 10f + 5f));
            foreach (var tile in tiles)
            {
                candidates.Add((tile.X * 10f + 2.5f, tile.Y * 10f + 2.5f));
                candidates.Add((tile.X * 10f + 7.5f, tile.Y * 10f + 2.5f));
                candidates.Add((tile.X * 10f + 7.5f, tile.Y * 10f + 7.5f));
                candidates.Add((tile.X * 10f + 2.5f, tile.Y * 10f + 7.5f));
            }

            candidates = candidates
                .Distinct()
                .Where(candidate => IsTreasureSurfaceEligible(draft, candidate))
                .ToList();
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "The Boss room has no treasure anchor clear of feature geometry, stamped structures, and roads.");
            }

            var occupied = new List<(float X, float Y)>();
            foreach (var transition in draft.Result.Resolved.Transitions)
            {
                occupied.Add(transition.Style == TransitionStyle.Placeable
                    ? (transition.Tile.X * 10f + 5f, transition.Tile.Y * 10f + 5f)
                    : (transition.DoorX, transition.DoorY));
            }

            occupied.AddRange(draft.Result.PlannedDecorations.Select(decoration =>
                (decoration.Position.X, decoration.Position.Y)));
            if (occupied.Count == 0)
                return candidates[0];

            var requiredDistanceSquared = TreasureAnchorClearance * TreasureAnchorClearance;
            foreach (var candidate in candidates)
            {
                if (occupied.All(point => DistanceSquared(candidate, point) >= requiredDistanceSquared))
                    return candidate;
            }

            // Extremely dense synthetic layouts may leave no candidate with the full clearance.
            // Pick the deterministic candidate with the greatest available separation instead of
            // falling back to an exact overlap.
            return candidates
                .OrderByDescending(candidate => occupied.Min(point => DistanceSquared(candidate, point)))
                .First();
        }

        private static bool IsTreasureSurfaceEligible(
            AreaGenerationDraft draft,
            (float X, float Y) candidate)
        {
            var tile = ((int)MathF.Floor(candidate.X / 10f), (int)MathF.Floor(candidate.Y / 10f));
            var resolved = draft.Result.Resolved;
            if (resolved.FeatureTileCells.ContainsKey(tile) || resolved.StampedStructureTiles.Contains(tile))
                return false;

            return !DungeonDecorationPlanner.TileCarriesRoadEdge(
                tile,
                resolved,
                draft.Composition.Tileset.RoadCrosser);
        }

        private static float DistanceSquared((float X, float Y) left, (float X, float Y) right)
        {
            var dx = left.X - right.X;
            var dy = left.Y - right.Y;
            return dx * dx + dy * dy;
        }

        private static float GroundHeightAt(
            ResolvedLayout layout,
            TilesetModel tileset,
            float worldX,
            float worldY)
        {
            var tileX = Math.Clamp((int)MathF.Floor(worldX / 10f), 0, layout.Width - 1);
            var tileY = Math.Clamp((int)MathF.Floor(worldY / 10f), 0, layout.Height - 1);
            var resolved = layout.GetTile(tileX, tileY);
            var tile = tileset.Tiles[resolved.TileId];
            var localX = Math.Clamp((worldX - tileX * 10f) / 10f, 0f, 1f);
            var localY = Math.Clamp((worldY - tileY * 10f) / 10f, 0f, 1f);

            var topLeft = tile.GetCornerHeightAt(resolved.Orientation, CornerSlot.TopLeft);
            var topRight = tile.GetCornerHeightAt(resolved.Orientation, CornerSlot.TopRight);
            var bottomRight = tile.GetCornerHeightAt(resolved.Orientation, CornerSlot.BottomRight);
            var bottomLeft = tile.GetCornerHeightAt(resolved.Orientation, CornerSlot.BottomLeft);
            var bottom = bottomLeft + (bottomRight - bottomLeft) * localX;
            var top = topLeft + (topRight - topLeft) * localX;
            var cornerOffset = bottom + (top - bottom) * localY;

            return (resolved.Height + cornerOffset) * layout.HeightTransition;
        }

        private static void AddPlaceable(
            ModuleWorkspace workspace,
            GitDocument git,
            GicDocument gic,
            string resref,
            string tag,
            float x,
            float y,
            float z,
            float facingDegrees,
            float visualScale,
            string? displayName = null,
            bool clearInheritedTransitionBehavior = false,
            Action<JsonGffStruct>? configureInstance = null)
        {
            if (string.IsNullOrWhiteSpace(resref))
                throw new InvalidOperationException($"Placeable '{tag}' has no configured blueprint resref.");

            var blueprint = workspace.LoadBlueprint(ResourceType.Utp, resref);
            var radians = facingDegrees * Math.PI / 180.0;
            var instance = InstanceFieldMap.CreateInstance(
                ResourceType.Utp,
                blueprint.Document,
                resref,
                x,
                y,
                z,
                Math.Cos(radians),
                Math.Sin(radians));
            InstanceFieldMap.SetTag(instance, tag);
            if (!string.IsNullOrWhiteSpace(displayName))
                instance.GetOrAddLocString("LocName").Text = displayName;
            if (clearInheritedTransitionBehavior)
            {
                instance.SetString("OnUsed", GffFieldType.ResRef, string.Empty);
                new VarTable(instance).Remove("Destination");
            }
            configureInstance?.Invoke(instance);
            ApplyVisualScale(instance, visualScale);

            var list = git.Fields.GetOrAddList(PlaceableList);
            list.Add(instance);
            gic.InsertBlankComment(PlaceableList, ResourceType.Utp, list.Count - 1, list.Count);
        }

        private static void ApplyVisualScale(JsonGffStruct instance, float scale)
        {
            if (MathF.Abs(scale - 1f) < 0.0001f)
                return;

            instance.Remove("VisualTransform");
            var field = JsonGffField.CreateStruct(6);
            var transform = field.Struct!;
            transform.SetSingle("ScaleX", scale);
            transform.SetSingle("ScaleY", scale);
            transform.SetSingle("ScaleZ", scale);
            instance.Add("VisualTransform", field);
        }
    }
}
