using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Legacy.Event.Player;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class EnmityService
    {
        private static Enmity GetEnmity(NWCreature npc, NWCreature attacker)
        {
            var table = GetEnmityTable(npc);
            if (!table.ContainsKey(attacker.GlobalID))
            {
                table.Add(attacker.GlobalID, new Enmity(attacker));
            }

            return table[attacker.GlobalID];
        }

        private static Dictionary<Guid, EnmityTable> GetAllNPCEnmityTablesForCreature(NWCreature player)
        {
            if (!player.IsPlayer) throw new Exception(nameof(GetAllNPCEnmityTablesForCreature) + " can only be used with players.");

            var npcTables = new Dictionary<Guid, EnmityTable>();

            foreach (var npcTable in AppCache.NPCEnmityTables)
            {
                if (npcTable.Value.ContainsKey(player.GlobalID))
                {
                    npcTables.Add(npcTable.Key, npcTable.Value);
                }
            }

            return npcTables;
        }

        public static void AdjustEnmity(NWCreature npc, NWCreature attacker, int volatileAdjust, int cumulativeAdjust = 0)
        {
            if (!npc.IsNPC) return;
            if (attacker == null || attacker.Area != npc.Area || LineOfSightObject(npc, attacker) == false) return;

            var adjustVolatile = volatileAdjust != 0;
            var adjustCumulative = cumulativeAdjust != 0;

            var effectiveEnmityRate = 1.0f;
            if (attacker.IsPlayer)
            {
                NWPlayer player = (attacker.Object);
                var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
                effectiveEnmityRate = effectiveStats.EnmityRate;
            }

            volatileAdjust = (int)(effectiveEnmityRate * volatileAdjust);
            cumulativeAdjust = (int)(effectiveEnmityRate * cumulativeAdjust);

            var table = GetEnmityTable(npc);
            
            // If this is the first creature to go on the enmity table, immediately attack them so they aren't
            // waiting around to do something the next time their AI runs.
            if (table.Count <= 0)
            {
                npc.AssignCommand(() =>
                {
                    ActionAttack(attacker);
                });
            }

            var enmity = GetEnmity(npc, attacker);

            if (adjustVolatile)
            {
                enmity.VolatileAmount += volatileAdjust;
            }
            if (adjustCumulative)
            {
                enmity.CumulativeAmount += cumulativeAdjust;
            }

        }

        public static void AdjustEnmityOnAllTaggedCreatures(NWCreature attacker, int volatileAdjust, int cumulativeAdjust = 0)
        {
            var tables = GetAllNPCEnmityTablesForCreature(attacker);
            foreach (var table in tables)
            {
                AdjustEnmity(table.Value.NPCObject, attacker, volatileAdjust, cumulativeAdjust);
            }
        }

        public static EnmityTable GetEnmityTable(NWCreature npc)
        {
            if (!npc.IsNPC && !npc.IsDMPossessed) throw new Exception("Only NPCs have enmity tables. Object name = " + npc.Name);

            if (!AppCache.NPCEnmityTables.ContainsKey(npc.GlobalID))
            {
                AppCache.NPCEnmityTables.Add(npc.GlobalID, new EnmityTable(npc));
            }

            return AppCache.NPCEnmityTables[npc.GlobalID];
        }
    }
}
