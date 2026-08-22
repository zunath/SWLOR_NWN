#nullable enable
using SWLOR.Toolset.Domain.AreaGeneration.Atmosphere;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>Projects one solved draft into a fresh toolset area triplet.</summary>
    public static class GeneratedAreaDocumentPopulator
    {
        private const string WaypointList = "WaypointList";
        private const string DoorList = "Door List";
        private const string PlaceableList = "Placeable List";

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

            WriteTiles(are, draft.Result.Resolved, draft.Composition.Tileset.Lighting);
            WriteAtmosphere(
                are,
                draft.Composition.Tileset.ResolveAtmosphere(draft.Composition.Content.AtmosphereProfile));
            WriteTransitions(draft, workspace, git, gic);
            WriteDecorations(draft, workspace, git, gic);
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
            bool clearInheritedTransitionBehavior = false)
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
