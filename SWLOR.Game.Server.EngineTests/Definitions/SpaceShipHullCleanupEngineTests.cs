using System.Numerics;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Destroyed NPC ship hulls leave space shortly after they explode, so repeated capital ship kills
    /// do not pile up fully rendered hulls around players. Their loot stays in the corpse container.
    /// </summary>
    public static class SpaceShipHullCleanupEngineTests
    {
        [EngineTest("A destroyed capital ship's hull is removed and its loot corpse stays", Category = "SpaceLoot", TimeoutSeconds = 60f)]
        public static async Task DestroyedCapitalShipHullIsRemoved(EngineTestContext ctx)
        {
            var area = ctx.CreateInstancedArea("space_korriban");
            await ctx.WaitFrameAsync();

            // The Interdictor Heavy Cruiser named in the report.
            var ship = CreateObject(ObjectType.Creature, "sithcap4", Location(area, new Vector3(43.3f, 290.5f, 3f), 0f));
            ctx.Track(ship);
            ctx.Assert(GetIsObjectValid(ship), "The capital ship spawned.");
            ChangeToStandardFaction(ship, StandardFaction.Commoner);
            ctx.Assert(Space.IsNPCShip(ship), "The capital ship is registered as an NPC ship.");

            await ctx.ExecuteInCreatureContextAsync(ship, () =>
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), ship));

            var corpse = OBJECT_INVALID;
            await ctx.WaitUntilAsync(() =>
            {
                corpse = GetNearestObjectByTag("corpse", ship);
                return GetIsObjectValid(corpse);
            }, 5f, "the ship's loot corpse to be created");
            ctx.Track(corpse);

            await ctx.WaitUntilAsync(() => !GetIsObjectValid(ship),
                Space.DestroyedShipHullRemovalSeconds + 3f, "the destroyed hull to be removed");

            ctx.Assert(GetIsObjectValid(corpse), "The loot corpse outlives the hull.");
            ctx.AssertEqual(area, GetArea(corpse), "Loot corpse area");
            ctx.SetResultDetail($"Hull removed within {Space.DestroyedShipHullRemovalSeconds + 3f:0.0} s; loot corpse kept at {GetPosition(corpse)}.");
        }

        [EngineTest("A ground creature's body still stays with its loot corpse", Category = "SpaceLoot", TimeoutSeconds = 60f)]
        public static async Task GroundCreatureBodyIsKept(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature("nw_rat001");
            ctx.Assert(!Space.IsNPCShip(creature), "A ground creature is not an NPC ship.");

            await ctx.ExecuteInCreatureContextAsync(creature, () =>
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), creature));

            var corpse = OBJECT_INVALID;
            await ctx.WaitUntilAsync(() =>
            {
                corpse = GetNearestObjectByTag("corpse", creature);
                return GetIsObjectValid(corpse);
            }, 5f, "the creature's loot corpse to be created");
            ctx.Track(corpse);

            await ctx.DelaySecondsAsync(Space.DestroyedShipHullRemovalSeconds + 3f);

            ctx.Assert(GetIsObjectValid(creature) && GetIsDead(creature), "The ground creature's body is kept.");
            ctx.Assert(GetIsObjectValid(corpse), "The ground creature's loot corpse is kept.");
        }
    }
}
