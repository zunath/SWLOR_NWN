using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PlayerCache: CacheBase<Player>
    {
        // Links PrimaryResidencePCBaseID to PlayerID
        private Dictionary<Guid, Guid> ByPrimaryResidencePCBaseID { get; } = new Dictionary<Guid, Guid>();

        // Links PrimaryResidencePCBaseStructureID to PlayerID
        private Dictionary<Guid, Guid> ByPrimaryResidencePCBaseStructureID { get; }  = new Dictionary<Guid, Guid>();


        protected override void OnCacheObjectSet(Player entity)
        {
            SetByPrimaryResidencePCBaseID(entity);
            SetByPrimaryResidencePCBaseStructureID(entity);
        }

        protected override void OnCacheObjectRemoved(Player entity)
        {
            RemoveByPrimaryResidencePCBaseID(entity);
            RemoveByPrimaryResidencePCBaseStructureID(entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByPrimaryResidencePCBaseID(Player entity)
        {
            // Entity has a primary residence. Set it into the cache.
            if (entity.PrimaryResidencePCBaseID != null)
            {
                ByPrimaryResidencePCBaseID[(Guid)entity.PrimaryResidencePCBaseID] = entity.ID;
            }
            // Entity doesn't have a primary residence. Look for any entries which do and remove them.
            else
            {
                RemoveByPrimaryResidencePCBaseID(entity);
            }
        }

        private void RemoveByPrimaryResidencePCBaseID(Player entity)
        {
            var existingPlayers = ByPrimaryResidencePCBaseID.Where(x => x.Value == entity.ID).ToList();
            for(int x = existingPlayers.Count - 1; x >= 0; x--)
            {
                var existing = existingPlayers.ElementAt(x);
                ByPrimaryResidencePCBaseID.Remove(existing.Key);
            }
        }

        private void SetByPrimaryResidencePCBaseStructureID(Player entity)
        {
            // Entity has a primary residence. Set it into the cache.
            if (entity.PrimaryResidencePCBaseStructureID != null)
            {
                ByPrimaryResidencePCBaseStructureID[(Guid) entity.PrimaryResidencePCBaseStructureID] = entity.ID;
            }
            // Entity doesn't have a primary residence. Look for any entries which do and remove them.
            else
            {
                RemoveByPrimaryResidencePCBaseStructureID(entity);
            }
        }

        private void RemoveByPrimaryResidencePCBaseStructureID(Player entity)
        {
            var existingPlayers = ByPrimaryResidencePCBaseStructureID.Where(x => x.Value == entity.ID).ToList();
            for(int x = existingPlayers.Count - 1; x >= 0; x--)
            {
                var existing = existingPlayers.ElementAt(x);
                ByPrimaryResidencePCBaseStructureID.Remove(existing.Key);
            }
        }

        public Player GetByID(Guid id)
        {
            return (Player)ByID[id].Clone();
        }

        public IEnumerable<Player> GetAllByIDs(IEnumerable<Guid> playerIDs)
        {
            var list = new List<Player>();
            foreach (var id in playerIDs)
            {
                list.Add((Player)ByID[id].Clone());
            }

            return list;
        }

        public bool ExistsByID(Guid id)
        {
            return ByID.ContainsKey(id);
        }

        /// <summary>
        /// Returns the player which has the given pcBaseID marked as their primary residence.
        /// </summary>
        /// <param name="pcBaseID">The ID of the PC Base</param>
        /// <returns></returns>
        public Player GetByPrimaryResidencePCBaseIDOrDefault(Guid pcBaseID)
        {
            if (!ByPrimaryResidencePCBaseID.ContainsKey(pcBaseID))
            {
                return default;
            }
            else
            {
                var playerID = ByPrimaryResidencePCBaseID[pcBaseID];
                return (Player)ByID[playerID].Clone();
            }
        }

        /// <summary>
        /// Returns the player which has the given pcStructureID marked as their primary residence.
        /// </summary>
        /// <param name="pcStructureID">The ID of the PC Base Structure</param>
        /// <returns></returns>
        public Player GetByPrimaryResidencePCBaseStructureIDOrDefault(Guid pcStructureID)
        {
            if (!ByPrimaryResidencePCBaseStructureID.ContainsKey(pcStructureID))
            {
                return default;
            }
            else
            {
                var playerID = ByPrimaryResidencePCBaseStructureID[pcStructureID];
                return (Player)ByID[playerID].Clone();
            }
        }

    }
}
