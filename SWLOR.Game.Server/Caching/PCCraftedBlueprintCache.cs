using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCraftedBlueprintCache: CacheBase<PCCraftedBlueprint>
    {
        private Dictionary<Guid, Dictionary<int, PCCraftedBlueprint>> ByPlayerIDAndCraftBlueprintID { get; } = new Dictionary<Guid, Dictionary<int, PCCraftedBlueprint>>();

        protected override void OnCacheObjectSet(PCCraftedBlueprint entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.CraftBlueprintID, entity, ByPlayerIDAndCraftBlueprintID);
        }

        protected override void OnCacheObjectRemoved(PCCraftedBlueprint entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.CraftBlueprintID, ByPlayerIDAndCraftBlueprintID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCraftedBlueprint GetByID(Guid id)
        {
            return (PCCraftedBlueprint)ByID[id].Clone();
        }

        public bool ExistsByPlayerIDAndCraftedBlueprintID(Guid playerID, int craftBlueprintID)
        {
            return ByPlayerIDAndCraftBlueprintID.ContainsKey(playerID) && 
                   ByPlayerIDAndCraftBlueprintID[playerID].ContainsKey(craftBlueprintID);
        }
    }
}
