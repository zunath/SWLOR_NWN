using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Caching
{
    public class PCCraftedBlueprintCache: CacheBase<PCCraftedBlueprint>
    {
        public PCCraftedBlueprintCache() 
            : base("PCCraftedBlueprint")
        {
        }

        private const string ByPlayerIDAndCraftBlueprintIDIndex = "ByPlayerIDAndCraftBlueprintID";

        protected override void OnCacheObjectSet(PCCraftedBlueprint entity)
        {
            SetIntoIndex($"{ByPlayerIDAndCraftBlueprintIDIndex}:{entity.PlayerID.ToString()}", entity.CraftBlueprintID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCCraftedBlueprint entity)
        {
            RemoveFromIndex($"{ByPlayerIDAndCraftBlueprintIDIndex}:{entity.PlayerID.ToString()}", entity.CraftBlueprintID.ToString());
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCraftedBlueprint GetByID(Guid id)
        {
            return ByID(id);
        }

        public bool ExistsByPlayerIDAndCraftedBlueprintID(Guid playerID, CraftBlueprint craftBlueprintID)
        {
            return ExistsByIndex($"{ByPlayerIDAndCraftBlueprintIDIndex}:{playerID.ToString()}", craftBlueprintID.ToString());
        }
    }
}
