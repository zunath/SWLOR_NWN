using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Staff testing commands for the procedural area generation system.
    /// </summary>
    public class AreaGenerationChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            GenerateArea();
            DestroyGeneratedArea();

            return _builder.Build();
        }

        private void GenerateArea()
        {
            _builder.Create("genarea")
                .Description("Generates a procedural test dungeon. Usage: /genarea [width] [height] [seed] [tier] [theme] [tileset] [layout]")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    var width = args.Length > 0 && int.TryParse(args[0], out var w) ? w : 16;
                    var height = args.Length > 1 && int.TryParse(args[1], out var h) ? h : 16;
                    int? seed = args.Length > 2 && int.TryParse(args[2], out var s) ? s : null;
                    var tier = args.Length > 3 && int.TryParse(args[3], out var t) ? t : 1;
                    var themeKey = args.Length > 4 ? args[4] : MineCaveDungeonDefinition.ThemeKey;
                    var tilesetKey = args.Length > 5 ? args[5] : null;
                    var layoutKey = args.Length > 6 ? args[6] : null;

                    if (width < 8 || width > 32 || height < 8 || height > 32)
                    {
                        SendMessageToPC(user, "Width and height must be between 8 and 32.");
                        return;
                    }

                    if (!DungeonContentPlacer.DungeonThemeExists(themeKey))
                    {
                        SendMessageToPC(user, $"Theme must be one of: {string.Join(", ", DungeonContentPlacer.GetAllDungeonThemes().Keys.OrderBy(k => k))}.");
                        return;
                    }

                    if (tilesetKey != null && !DungeonContentPlacer.TilesetProfileExists(tilesetKey))
                    {
                        SendMessageToPC(user, $"Tileset must be one of: {string.Join(", ", DungeonContentPlacer.GetAllTilesetProfiles().Keys.OrderBy(k => k))}.");
                        return;
                    }

                    if (layoutKey != null && !DungeonContentPlacer.LayoutProfileExists(layoutKey))
                    {
                        SendMessageToPC(user, $"Layout must be one of: {string.Join(", ", DungeonContentPlacer.GetAllLayoutProfiles().Keys.OrderBy(k => k))}.");
                        return;
                    }

                    var composition = DungeonContentPlacer.GetComposition(themeKey, tilesetKey, layoutKey);
                    if (!composition.Content.Tiers.ContainsKey(tier))
                    {
                        SendMessageToPC(user, $"Tier must be one of: {string.Join(", ", composition.Content.Tiers.Keys.OrderBy(k => k))}.");
                        return;
                    }

                    // Sizes below the layout style's measured floor fail generation structurally
                    // (see LayoutStyleSizeFloor) — clamp up and tell the requester.
                    var sizeFloor = LayoutStyleSizeFloor.For(composition.Layout.Template.Style);
                    if (width < sizeFloor || height < sizeFloor)
                    {
                        SendMessageToPC(user,
                            $"{composition.Layout.DisplayName} needs at least {sizeFloor}x{sizeFloor}; clamping.");
                        width = Math.Max(width, sizeFloor);
                        height = Math.Max(height, sizeFloor);
                    }

                    var returnLocation = GetLocation(user);
                    SendMessageToPC(user,
                        $"Generating {width}x{height} tier {tier} '{themeKey}' area " +
                        $"({composition.Tileset.DisplayName} / {composition.Layout.DisplayName})" +
                        (seed.HasValue ? $" with seed {seed}..." : "..."));

                    // Small areas cannot fit the default room counts; scale down so requests
                    // below 16x16 still succeed instead of burning every retry.
                    var small = width < 16 || height < 16;
                    AreaGeneration.QueueGeneration(new AreaGenerationRequest
                    {
                        TilesetResref = composition.Tileset.TilesetResref,
                        TilesetProfileKey = composition.Tileset.Key,
                        PlaceholderResref = composition.Tileset.PlaceholderResref,
                        OpenTerrainOverride = composition.Tileset.PrimaryOpenTerrain,
                        Lighting = composition.Tileset.Lighting,
                        Atmosphere = composition.Tileset.ResolveAtmosphere(composition.Content.AtmosphereProfile),
                        Layout = composition.BuildLayoutParameters(),
                        Width = width,
                        Height = height,
                        Seed = seed,
                        MinRooms = small ? 2 : 4,
                        MaxRooms = small ? 4 : 8,
                        DisplayName = $"Generated Test Area ({composition.Content.DisplayName})",
                        Tag = "GEN_TEST_AREA"
                    }, result =>
                    {
                        if (!result.Success)
                        {
                            SendMessageToPC(user, $"Generation FAILED after {result.AttemptsUsed} attempt(s): {result.FailureReason}");
                            return;
                        }

                        if (!RuntimeAreaRegistry.TryGetById(result.InstanceId, out var instance))
                        {
                            SendMessageToPC(user, $"Generated '{result.InstanceId}' but could not find its runtime instance to populate. Inform a DM.");
                            return;
                        }

                        instance.ExitLocation = returnLocation;

                        var population = DungeonContentPlacer.Populate(instance, themeKey, tier);

                        SendMessageToPC(user,
                            $"Generated '{result.InstanceId}' (seed {result.SeedUsed}, {result.AttemptsUsed} attempt(s), {result.Layout.Rooms.Count} rooms). " +
                            $"Populated {population.RoomsPopulated} room(s) with {population.CreaturesSpawned} creature(s)" +
                            (population.BossSpawned ? $", boss '{population.BossResref}'" : ", no boss") +
                            (population.TreasurePlaced ? ", treasure cache placed" : ", no treasure") +
                            (population.ExitPlaced ? ", exit placed." : ", exit NOT placed.") +
                            " Jumping you to the entrance.");

                        var entrance = RuntimeAreaRegistry.GetRandomWalkableLocation(result.Area);
                        foreach (var room in result.Layout.Rooms)
                        {
                            if (room.Role != RoomRole.Entrance)
                                continue;

                            var x = room.CenterTile.X * 10f + 5f;
                            var y = room.CenterTile.Y * 10f + 5f;
                            var z = GetGroundHeight(Location(result.Area, new Vector3(x, y, 0f), 0f));
                            entrance = Location(result.Area, new Vector3(x, y, z), 0f);
                            break;
                        }

                        AssignCommand(user, () => ActionJumpToLocation(entrance));
                    });
                });
        }

        private void DestroyGeneratedArea()
        {
            _builder.Create("genareadestroy")
                .Description("Destroys the generated area you are standing in (after jumping you back out).")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    var area = GetArea(user);
                    if (!RuntimeAreaRegistry.TryGetByArea(area, out var instance))
                    {
                        SendMessageToPC(user, "You are not standing in a generated area.");
                        return;
                    }

                    var exit = instance.ExitLocation ?? GetLocation(GetFirstObjectInArea(area));
                    AssignCommand(user, () => ActionJumpToLocation(exit));

                    // DestroyArea refuses while anyone is inside, so run after the jump lands.
                    Core.Scheduler.Schedule(() =>
                    {
                        if (AreaGeneration.DestroyGeneratedArea(instance.InstanceId, out var failure))
                            SendMessageToPC(user, $"Destroyed '{instance.InstanceId}'.");
                        else
                            SendMessageToPC(user, $"Destroy failed: {failure}");
                    }, TimeSpan.FromSeconds(6));
                });
        }
    }
}
