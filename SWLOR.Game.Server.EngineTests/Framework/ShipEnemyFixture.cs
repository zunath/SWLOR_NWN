using System.Collections.Generic;
using System.Reflection;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>A real NPC ship target, so disabled player PvP cannot turn a hostile fixture into an ally.</summary>
    public sealed class ShipEnemyFixture : IDisposable
    {
        private static Dictionary<uint, ShipStatus> Ships => (Dictionary<uint, ShipStatus>)typeof(Space)
            .GetField("_shipNPCs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        public uint Creature { get; }
        public ShipEnemyFixture(EngineTestContext context, uint opponent, ShipStatus status, float xOffset = 2f)
        {
            Creature = context.SpawnCreature("civilian", xOffset);
            context.SuppressNPCNaturalRegen(Creature);
            Ships[Creature] = status;
            context.MakeHostile(Creature);
            try { context.Assert(GetIsEnemy(opponent, Creature) || GetIsEnemy(Creature, opponent), "native NPC fixture is hostile without player PvP"); }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            Ships.Remove(Creature);
            if (GetIsObjectValid(Creature)) DestroyObject(Creature);
        }
    }
}
