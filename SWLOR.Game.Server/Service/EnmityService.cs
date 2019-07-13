using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Event.Player;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Service
{
    public static class EnmityService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnPlayerDamaged>(message => OnPlayerDamaged());
        }

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

            bool adjustVolatile = volatileAdjust != 0;
            bool adjustCumulative = cumulativeAdjust != 0;

            float effectiveEnmityRate = 1.0f;
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
                    _.ActionAttack(attacker);
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

        public static void AdjustPercentEnmityOnAllTaggedCreatures(NWCreature attacker, int volatilePercentAdjust, int cumulativePercentAdjust = 0)
        {
            var tables = GetAllNPCEnmityTablesForCreature(attacker);
            foreach (var table in tables)
            {
                var enmity = GetEnmity(table.Value.NPCObject, attacker);
                float volatileAdjust = volatilePercentAdjust * 0.01f;
                float cumulativeAdjust = cumulativePercentAdjust * 0.01f;

                if (volatilePercentAdjust != 0)
                {
                    enmity.VolatileAmount = (int)(enmity.VolatileAmount + (enmity.VolatileAmount * volatileAdjust));
                }

                if (cumulativePercentAdjust != 0)
                {
                    enmity.CumulativeAmount = (int)(enmity.CumulativeAmount + (enmity.CumulativeAmount * cumulativeAdjust));
                }

            }
        }

        public static void OnNPCPhysicallyAttacked()
        {
            NWCreature self = (NWGameObject.OBJECT_SELF);
            NWCreature attacker = (_.GetLastAttacker(NWGameObject.OBJECT_SELF));
            AdjustEnmity(self, attacker, 0, 1);
        }

        public static void OnNPCDamaged()
        {
            NWCreature self = (NWGameObject.OBJECT_SELF);
            NWCreature damager = (_.GetLastDamager(NWGameObject.OBJECT_SELF));
            int enmityAmount = _.GetTotalDamageDealt();
            if (enmityAmount <= 0) enmityAmount = 1;

            AdjustEnmity(self, damager, 0, enmityAmount);
        }

        public static void OnPlayerDamaged()
        {
            NWPlayer player = (NWGameObject.OBJECT_SELF);
            NWCreature npc = (_.GetLastDamager(NWGameObject.OBJECT_SELF));

            if (!player.IsPlayer || !npc.IsNPC) return;

            int damage = _.GetTotalDamageDealt();
            Enmity enmity = GetEnmity(npc, player);
            enmity.CumulativeAmount -= damage;
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

        public static bool IsOnEnmityTable(NWCreature npc, NWCreature target)
        {
            if (!npc.IsNPC && !npc.IsDMPossessed) throw new Exception("Only NPCs have enmity tables. Object name = " + npc.Name);

            EnmityTable table = GetEnmityTable(npc);

            return table.ContainsKey(target.GlobalID);
        }

        public static bool IsEnmityTableEmpty(NWCreature npc)
        {
            if (!npc.IsNPC && !npc.IsDMPossessed) throw new Exception("Only NPCs have enmity tables. Object name = " + npc.Name);

            EnmityTable table = GetEnmityTable(npc);
            return table.Count <= 0;
        }
    }
}
