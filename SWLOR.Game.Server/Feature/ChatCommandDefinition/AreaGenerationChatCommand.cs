using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Enumeration;
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
                .Description("Generates a procedural test area. Usage: /genarea [width] [height] [seed]")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    var width = args.Length > 0 && int.TryParse(args[0], out var w) ? w : 16;
                    var height = args.Length > 1 && int.TryParse(args[1], out var h) ? h : 16;
                    int? seed = args.Length > 2 && int.TryParse(args[2], out var s) ? s : null;

                    if (width < 8 || width > 32 || height < 8 || height > 32)
                    {
                        SendMessageToPC(user, "Width and height must be between 8 and 32.");
                        return;
                    }

                    var returnLocation = GetLocation(user);
                    SendMessageToPC(user, $"Generating {width}x{height} area" + (seed.HasValue ? $" with seed {seed}..." : "..."));

                    // Small areas cannot fit the default room counts; scale down so requests
                    // below 16x16 still succeed instead of burning every retry.
                    var small = width < 16 || height < 16;
                    AreaGeneration.QueueGeneration(new AreaGenerationRequest
                    {
                        Width = width,
                        Height = height,
                        Seed = seed,
                        MinRooms = small ? 2 : 4,
                        MaxRooms = small ? 4 : 8,
                        DisplayName = "Generated Test Area",
                        Tag = "GEN_TEST_AREA"
                    }, result =>
                    {
                        if (!result.Success)
                        {
                            SendMessageToPC(user, $"Generation FAILED after {result.AttemptsUsed} attempt(s): {result.FailureReason}");
                            return;
                        }

                        if (RuntimeAreaRegistry.TryGetById(result.InstanceId, out var instance))
                        {
                            instance.ExitLocation = returnLocation;
                        }

                        SendMessageToPC(user,
                            $"Generated '{result.InstanceId}' (seed {result.SeedUsed}, {result.AttemptsUsed} attempt(s), {result.Layout.Rooms.Count} rooms). Jumping you to the entrance.");

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
