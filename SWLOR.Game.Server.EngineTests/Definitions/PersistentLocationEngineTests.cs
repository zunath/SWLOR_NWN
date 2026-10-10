using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PersistentLocationEngineTests
{
    [EngineTest("Saved Veles Culinary School location restores onto an accessible street", Category = "PersistentLocation", TimeoutSeconds = 40f)]
    public static async Task CulinarySchoolArrival(EngineTestContext ctx)
    {
        var veles = Area.GetAreaByResref("veles_exterior");
        ctx.Assert(GetIsObjectValid(veles), "Veles is loaded.");
        var mover = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        SetPlotFlag(mover, true);
        SetAILevel(mover, AILevel.High);
        SetEventScript(mover, EventScript.Creature_OnHeartbeat, string.Empty);
        SetEventScript(mover, EventScript.Creature_OnNotice, string.Empty);
        ctx.Assert(GetArea(mover) != veles, "Restoration starts in another area, as it does at login.");

        var reportedPosition = new Vector3(156.23833f, 129.15797f, .2f);
        PersistentLocation.RestoreLocation(mover, Location(veles, reportedPosition, 180f));
        var arrived = false;
        AssignCommand(mover, () => ActionDoCommand(() => arrived = true));
        await ctx.WaitUntilAsync(() => arrived, 10f, "the saved-location jump and collision search to complete");
        ctx.AssertEqual(veles, GetArea(mover), "The player remains in the saved area.");
        var arrivalPosition = GetPosition(mover);
        ctx.Assert(System.Numerics.Vector3.Distance(arrivalPosition, reportedPosition) <= 20f,
            $"Recovery stays near the saved point; arrived at {arrivalPosition}.");

        // A location inside the closed building shell cannot walk west onto this
        // street. Exercise native pathfinding instead of checking a chosen coordinate.
        var street = new Vector3(150f, 129.15797f, .2f);
        await ctx.ExecuteInCreatureContextAsync(mover, () =>
            ActionMoveToLocation(Location(veles, street, 180f)));
        await ctx.WaitUntilAsync(() => System.Numerics.Vector3.Distance(GetPosition(mover), street) < .75f,
            15f, $"the restored creature to walk out onto the street from {arrivalPosition}");
        ctx.SetResultDetail($"Restored across areas from the reported point to {arrivalPosition} and walked onto the street.");
    }

    [EngineTest("Valid Veles street and rooftop saved locations retain position and facing", Category = "PersistentLocation", TimeoutSeconds = 40f)]
    public static async Task ValidArrivals(EngineTestContext ctx)
    {
        var veles = Area.GetAreaByResref("veles_exterior");
        ctx.Assert(GetIsObjectValid(veles), "Veles is loaded.");
        var mover = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        SetAILevel(mover, AILevel.High);
        SetEventScript(mover, EventScript.Creature_OnHeartbeat, string.Empty);
        SetEventScript(mover, EventScript.Creature_OnNotice, string.Empty);

        foreach (var point in new[] { new Vector3(150f, 129.15797f, .2f), new Vector3(57.38f, 75f, 15.2f) })
        {
            PersistentLocation.RestoreLocation(mover, Location(veles, point, 180f));
            var arrived = false;
            AssignCommand(mover, () => ActionDoCommand(() => arrived = true));
            await ctx.WaitUntilAsync(() => arrived, 10f, $"restoration to {point} to complete");
            ctx.AssertEqual(veles, GetArea(mover), "The saved area is preserved.");
            ctx.Assert(System.Numerics.Vector3.Distance(GetPosition(mover), point) < .1f,
                $"A valid saved location stays unchanged: {point} became {GetPosition(mover)}.");
            ctx.Assert(Math.Abs(GetFacing(mover) - 180f) < 1f, "The saved facing is preserved.");
        }
    }
}
