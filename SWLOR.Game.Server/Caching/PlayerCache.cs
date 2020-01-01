using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PlayerCache: CacheBase<Player>
    {
        public PlayerCache() 
            : base("Player")
        {
        }

        private const string ByPrimaryResidencePCBaseIDIndex = "ByPrimaryResidencePCBaseID";
        private const string ByPrimaryResidencePCBaseStructureIDIndex = "ByPrimaryResidencePCBaseStructureID";

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
                SetIntoIndex(ByPrimaryResidencePCBaseIDIndex, entity.PrimaryResidencePCBaseID.ToString(), entity);
            }
            // Entity doesn't have a primary residence. Look for any entries which do and remove them.
            else
            {
                RemoveByPrimaryResidencePCBaseID(entity);
            }
        }

        private void RemoveByPrimaryResidencePCBaseID(Player entity)
        {
            if (entity.PrimaryResidencePCBaseID != null &&
                ExistsByIndex(ByPrimaryResidencePCBaseIDIndex, entity.PrimaryResidencePCBaseID.ToString()))
            {
                RemoveFromIndex(ByPrimaryResidencePCBaseIDIndex, entity.PrimaryResidencePCBaseID.ToString());
            }
        }

        private void SetByPrimaryResidencePCBaseStructureID(Player entity)
        {
            // Entity has a primary residence. Set it into the cache.
            if (entity.PrimaryResidencePCBaseStructureID != null)
            {
                SetIntoIndex(ByPrimaryResidencePCBaseStructureIDIndex, entity.PrimaryResidencePCBaseStructureID.ToString(), entity);
            }
            // Entity doesn't have a primary residence. Look for any entries which do and remove them.
            else
            {
                RemoveByPrimaryResidencePCBaseStructureID(entity);
            }
        }

        private void RemoveByPrimaryResidencePCBaseStructureID(Player entity)
        {
            if (entity.PrimaryResidencePCBaseStructureID != null &&
                ExistsByIndex(ByPrimaryResidencePCBaseStructureIDIndex, entity.PrimaryResidencePCBaseStructureID.ToString()))
            {
                RemoveFromIndex(ByPrimaryResidencePCBaseStructureIDIndex, entity.PrimaryResidencePCBaseStructureID.ToString());
            }
        }

        public Player GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<Player> GetAllByIDs(IEnumerable<Guid> playerIDs)
        {
            var list = new List<Player>();
            foreach (var id in playerIDs)
            {
                list.Add(ByID(id));
            }

            return list;
        }

        public bool ExistsByID(Guid id)
        {
            return Exists(id);
        }

        /// <summary>
        /// Returns the player which has the given pcBaseID marked as their primary residence.
        /// </summary>
        /// <param name="pcBaseID">The ID of the PC Base</param>
        /// <returns></returns>
        public Player GetByPrimaryResidencePCBaseIDOrDefault(Guid pcBaseID)
        {
            if (!ExistsByIndex(ByPrimaryResidencePCBaseIDIndex, pcBaseID.ToString()))
            {
                return default;
            }
            else
            {
                var playerID = GetFromIndex(ByPrimaryResidencePCBaseIDIndex, pcBaseID.ToString());
                return ByID(playerID);
            }
        }

        /// <summary>
        /// Returns the player which has the given pcStructureID marked as their primary residence.
        /// </summary>
        /// <param name="pcStructureID">The ID of the PC Base Structure</param>
        /// <returns></returns>
        public Player GetByPrimaryResidencePCBaseStructureIDOrDefault(Guid pcStructureID)
        {
            if (!ExistsByIndex(ByPrimaryResidencePCBaseStructureIDIndex, pcStructureID.ToString()))
            {
                return default;
            }
            else
            {
                var playerID = GetFromIndex(ByPrimaryResidencePCBaseStructureIDIndex, pcStructureID.ToString());
                return ByID(playerID);
            }
        }

    }
}
