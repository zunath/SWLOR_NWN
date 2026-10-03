using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class VelesSewerEngineTests
{
    [EngineTest("Veles surface sewer entrances can be reached and used", Category = "VelesSewers", TimeoutSeconds = 150f)]
    public static async Task SurfaceEntrances(EngineTestContext ctx)
    {
        await VerifyRoutesAsync(ctx, "veles_exterior", "veles_sewers", "SEWERS_", "SEWERS_VELES_", 11);
    }

    [EngineTest("Veles sewer exits can be reached and used; missing districts are sealed", Category = "VelesSewers", TimeoutSeconds = 150f)]
    public static async Task SewerExits(EngineTestContext ctx)
    {
        var sewers = Area.GetAreaByResref("veles_sewers");
        var sealedLadders = Placeables(sewers)
            .Where(placeable => GetName(placeable) == "Sealed Surface Access")
            .ToArray();
        ctx.AssertEqual(3, sealedLadders.Length, "Ladders to the three absent surface districts are labeled sealed.");
        foreach (var ladder in sealedLadders)
        {
            ctx.Assert(!GetUseableFlag(ladder), "A sealed ladder is not clickable.");
            ctx.AssertEqual(string.Empty, GetEventScript(ladder, EventScript.Placeable_OnUsed),
                "A sealed ladder cannot run a broken teleport.");
        }

        await VerifyRoutesAsync(ctx, "veles_sewers", "veles_exterior", "SEWERS_VELES_", "SEWERS_", 9);
    }

    private static async Task VerifyRoutesAsync(
        EngineTestContext ctx,
        string sourceResref,
        string destinationResref,
        string destinationPrefix,
        string approachPrefix,
        int expectedCount)
    {
        // Use the loaded areas: teleport's native tag lookup must resolve the real destination,
        // rather than an identically tagged waypoint in an extra copy of either area.
        var sourceArea = Area.GetAreaByResref(sourceResref);
        var destinationArea = Area.GetAreaByResref(destinationResref);
        ctx.Assert(GetIsObjectValid(sourceArea) && GetIsObjectValid(destinationArea), "Both Veles areas are loaded.");
        var devices = Placeables(sourceArea)
            .Where(placeable => GetEventScript(placeable, EventScript.Placeable_OnUsed) == ScriptName.OnPlaceableTeleport &&
                                GetLocalString(placeable, "DESTINATION").StartsWith(destinationPrefix, StringComparison.Ordinal))
            .OrderBy(placeable => GetLocalString(placeable, "DESTINATION"))
            .ThenBy(placeable => GetPosition(placeable).X)
            .ThenBy(placeable => GetPosition(placeable).Y)
            .ToArray();
        ctx.AssertEqual(expectedCount, devices.Length, $"Every active sewer route in {sourceResref} is exercised.");

        var mover = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        SetPlotFlag(mover, true);
        SetAILevel(mover, AILevel.VeryLow);
        SetEventScript(mover, EventScript.Creature_OnHeartbeat, string.Empty);
        SetEventScript(mover, EventScript.Creature_OnNotice, string.Empty);
        var failures = new List<string>();
        foreach (var device in devices)
        {
            var destinationTag = GetLocalString(device, "DESTINATION");
            var position = GetPosition(device);
            var route = $"{destinationTag} from ({position.X:F2}, {position.Y:F2})";
            var waypoint = GetWaypointByTag(destinationTag);
            ctx.Assert(GetIsObjectValid(waypoint), $"{route} has a destination waypoint.");
            ctx.AssertEqual(destinationArea, GetArea(waypoint), $"{route} leads to the expected area.");
            ctx.Assert(GetUseableFlag(device), $"{route} is clickable.");
            ctx.AssertEqual(0, GetLocalInt(device, "KEY_ITEM_ID"), $"{route} is an ordinary public sewer connection.");

            var counterpartTag = approachPrefix + destinationTag.Substring(destinationPrefix.Length);
            var approachWaypoint = GetWaypointByTag(counterpartTag);
            Location approach;
            if (GetIsObjectValid(approachWaypoint) && GetArea(approachWaypoint) == sourceArea &&
                System.Numerics.Vector3.Distance(GetPosition(approachWaypoint), position) < 4f)
            {
                approach = GetLocation(approachWaypoint);
            }
            else
            {
                // The two additional apartment hatches do not have their own return waypoint.
                approach = Location(sourceArea, position + new Vector3(-2f, 0f, 0f), 0f);
            }

            await ctx.ExecuteInCreatureContextAsync(mover, () =>
            {
                ClearAllActions();
                JumpToLocation(approach);
            });
            await ctx.WaitUntilAsync(() => GetArea(mover) == sourceArea, 3f, $"the mover to start {route}");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(mover, () =>
            {
                ClearAllActions();
                ActionInteractObject(device);
            });

            try
            {
                await ctx.WaitUntilAsync(
                    () => GetArea(mover) == destinationArea && GetDistanceBetween(mover, waypoint) < 1f,
                    10f,
                    $"the mover to walk to and use {route}");
                ctx.Log($"PASS {route}");
            }
            catch (EngineTestAssertionException ex)
            {
                var finalPosition = GetPosition(mover);
                var failure = $"{route}: {ex.Message}; stopped at ({finalPosition.X:F2}, {finalPosition.Y:F2}) in {GetResRef(GetArea(mover))}";
                failures.Add(failure);
                ctx.Log(failure);
            }
        }

        ctx.Assert(failures.Count == 0, string.Join(Environment.NewLine, failures));
        ctx.SetResultDetail($"Physically approached and used all {devices.Length} placed routes from {sourceResref} through the native action queue and teleport handler; each arrived at its configured waypoint in {destinationResref}.");
    }

    private static IEnumerable<uint> Placeables(uint area)
    {
        for (var placeable = GetFirstObjectInArea(area, ObjectType.Placeable);
             GetIsObjectValid(placeable);
             placeable = GetNextObjectInArea(area, ObjectType.Placeable))
        {
            yield return placeable;
        }
    }
}
