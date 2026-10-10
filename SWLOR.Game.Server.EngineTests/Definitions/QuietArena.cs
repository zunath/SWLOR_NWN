using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// An instanced copy of an area with no creatures, scripts, or spawn tables. Combat tests
    /// that need an undisturbed fight use it instead of the shared arena, whose placed town
    /// NPCs attack hostile spawns and stand where creatures need to move.
    /// </summary>
    public sealed class QuietArena
    {
        private const string AreaResref = "hutlar_testsite";
        // Players who get stuck are moved here, so it is known to be walkable.
        private const string AnchorWaypointTag = "STUCK_WAYPOINT";

        private readonly EngineTestContext _ctx;
        private readonly Vector3 _anchor;

        public uint Area { get; }

        private QuietArena(EngineTestContext ctx, uint area, Vector3 anchor)
        {
            _ctx = ctx;
            Area = area;
            _anchor = anchor;
        }

        public static async Task<QuietArena> CreateAsync(EngineTestContext ctx)
        {
            var area = ctx.CreateInstancedArea(AreaResref);
            await ctx.WaitFrameAsync();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) == AnchorWaypointTag)
                    return new QuietArena(ctx, area, GetPosition(obj));
            }

            ctx.Fail($"{AreaResref} has no '{AnchorWaypointTag}' waypoint to anchor spawns.");
            return null;
        }

        /// <summary>
        /// Spawns a Defender-faction creature north of the anchor, tracked for cleanup.
        /// </summary>
        public uint Spawn(string resref, float northMeters, float facing)
        {
            var position = _anchor + new Vector3(0f, northMeters, 0f);
            var creature = CreateObject(ObjectType.Creature, resref, Location(Area, position, facing));
            _ctx.Assert(GetIsObjectValid(creature), $"Failed to spawn '{resref}' in {AreaResref}.");
            ChangeToStandardFaction(creature, StandardFaction.Defender);
            _ctx.Track(creature);
            return creature;
        }
    }
}
