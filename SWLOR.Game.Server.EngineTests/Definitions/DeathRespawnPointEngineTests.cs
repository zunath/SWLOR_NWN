using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class DeathRespawnPointEngineTests
{
    private const string MedicalArea = "dan_repgarrison";
    private const string TerminalTag = "cloning_reg";

    [EngineTest("Respawn uses a registered point and falls back to the default for a missing or unusable one", Category = "Death", TimeoutSeconds = 30f)]
    public static Task RespawnLocation(EngineTestContext ctx)
    {
        var defaultPoint = GetWaypointByTag("DTH_DEFAULT_RESPAWN_POINT");
        ctx.Assert(GetIsObjectValid(defaultPoint), "the packed module has the default respawn waypoint");
        var defaultArea = GetArea(defaultPoint);
        var medicalArea = Area.GetAreaByResref(MedicalArea);
        ctx.Assert(GetIsObjectValid(medicalArea), "the packed module has the Dantooine garrison medical center");

        // The garrison cloning registration terminal stands here; players register beside it.
        var registered = Record(MedicalArea, 72.2f, 17.7f, 0f);
        var location = Death.GetRespawnLocation(registered);
        ctx.AssertEqual(medicalArea, GetAreaFromLocation(location), "a registered point keeps its area");
        ctx.Assert(GetDistanceBetweenLocations(location, Location(medicalArea, Vector3(72.2f, 17.7f, 0f), 0f)) < 0.1f,
            "a registered point keeps its position");

        AssertDefault(null, "no player record");
        AssertDefault(Record(null, 0f, 0f, 0f), "no respawn area saved");
        AssertDefault(Record(string.Empty, 0f, 0f, 0f), "a blank respawn area");
        AssertDefault(Record("no_such_area", 10f, 10f, 0f), "an area that no longer exists");
        AssertDefault(Record(MedicalArea, -500f, -500f, 0f), "a point outside the area's walkmesh");

        var defaultPosition = GetPosition(defaultPoint);
        var defaultRecord = Record(GetResRef(defaultArea), defaultPosition.X, defaultPosition.Y, defaultPosition.Z);
        ctx.Assert(GetDistanceBetweenLocations(Death.GetRespawnLocation(defaultRecord), GetLocation(defaultPoint)) < 0.1f,
            "the default respawn point is itself a usable registered point");

        // Every medical registration terminal must leave ground a registering player can stand on,
        // or registering there would send that player to the default point instead.
        var terminals = 0;
        for (var terminal = GetObjectByTag(TerminalTag, terminals); GetIsObjectValid(terminal); terminal = GetObjectByTag(TerminalTag, ++terminals))
        {
            var area = GetArea(terminal);
            var position = GetPosition(terminal);
            var kept = false;
            for (var step = 0; step < 8 && !kept; step++)
            {
                var angle = step * MathF.PI / 4f;
                var x = position.X + 1.5f * MathF.Cos(angle);
                var y = position.Y + 1.5f * MathF.Sin(angle);
                var resolved = Death.GetRespawnLocation(Record(GetResRef(area), x, y, position.Z));
                kept = GetAreaFromLocation(resolved) == area &&
                       GetDistanceBetweenLocations(resolved, Location(area, Vector3(x, y, position.Z), 0f)) < 0.1f;
            }

            ctx.Assert(kept, $"a player registering at the terminal in {GetResRef(area)} ({position.X:F1}, {position.Y:F1}) keeps that point");
        }

        ctx.Assert(terminals > 0, "the packed module has medical registration terminals");
        ctx.SetResultDetail($"A registered point resolves to itself; a missing record, null or blank area, unknown area and off-walkmesh point resolve to DTH_DEFAULT_RESPAWN_POINT; ground beside all {terminals} registration terminals is kept.");
        return Task.CompletedTask;

        void AssertDefault(Player dbPlayer, string label)
        {
            var resolved = Death.GetRespawnLocation(dbPlayer);
            ctx.AssertEqual(defaultArea, GetAreaFromLocation(resolved), $"{label}: area");
            ctx.Assert(GetDistanceBetweenLocations(resolved, GetLocation(defaultPoint)) < 0.1f, $"{label}: uses the default respawn point");
        }
    }

    private static Player Record(string areaResref, float x, float y, float z)
    {
        return new Player("respawn-engine-test")
        {
            RespawnAreaResref = areaResref,
            RespawnLocationX = x,
            RespawnLocationY = y,
            RespawnLocationZ = z
        };
    }
}
