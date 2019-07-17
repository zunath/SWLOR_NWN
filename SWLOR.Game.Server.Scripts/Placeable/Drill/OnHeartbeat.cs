using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.Drill
{
    public class OnHeartbeat: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable drill = NWGameObject.OBJECT_SELF;
            string structureID = drill.GetLocalString("PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(structureID))
            {
                string areaName = drill.Area.Name;
                Console.WriteLine("There was an error retrieving the PC_BASE_STRUCTURE_ID variable on drill in area: " + areaName);
                return;
            }

            Guid structureGUID = new Guid(structureID);
            PCBaseStructure pcStructure = DataService.PCBaseStructure.GetByID(structureGUID);
            PCBase pcBase = DataService.PCBase.GetByID(pcStructure.PCBaseID);
            PCBaseStructure tower = BaseService.GetBaseControlTower(pcBase.ID);

            // Check whether there's space in this tower.
            int capacity = BaseService.CalculateResourceCapacity(pcBase.ID);
            int count = DataService.PCBaseStructureItem.GetNumberOfItemsContainedBy(tower.ID) + 1;
            if (count > capacity) return;

            BaseStructure baseStructure = DataService.BaseStructure.GetByID(pcStructure.BaseStructureID);
            DateTime now = DateTime.UtcNow;

            var outOfPowerEffect = drill.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "CONTROL_TOWER_OUT_OF_POWER");
            if (now >= pcBase.DateFuelEnds)
            {
                if (outOfPowerEffect == null)
                {
                    outOfPowerEffect = _.EffectVisualEffect(_.VFX_DUR_AURA_RED);
                    outOfPowerEffect = _.TagEffect(outOfPowerEffect, "CONTROL_TOWER_OUT_OF_POWER");
                    _.ApplyEffectToObject(_.DURATION_TYPE_PERMANENT, outOfPowerEffect, drill);
                }

                return;
            }
            else if (now < pcBase.DateFuelEnds && outOfPowerEffect != null)
            {
                _.RemoveEffect(drill, outOfPowerEffect);
            }

            int minuteReduce = 2 * pcStructure.StructureBonus;
            int increaseMinutes = 60 - minuteReduce;
            int retrievalRating = baseStructure.RetrievalRating;

            if (increaseMinutes <= 20) increaseMinutes = 20;
            if (pcStructure.DateNextActivity == null)
            {
                pcStructure.DateNextActivity = now.AddMinutes(increaseMinutes);
                DataService.SubmitDataChange(pcStructure, DatabaseActionType.Update);
            }

            if (!(now >= pcStructure.DateNextActivity)) return;

            // Time to spawn a new item and reset the timer.
            var dbArea = DataService.Area.GetByResref(pcBase.AreaResref);
            string sector = pcBase.Sector;
            int lootTableID = 0;

            switch (sector)
            {
                case "NE": lootTableID = dbArea.NortheastLootTableID ?? 0; break;
                case "NW": lootTableID = dbArea.NorthwestLootTableID ?? 0; break;
                case "SE": lootTableID = dbArea.SoutheastLootTableID ?? 0; break;
                case "SW": lootTableID = dbArea.SouthwestLootTableID ?? 0; break;
            }

            if (lootTableID <= 0)
            {
                Console.WriteLine("WARNING: Loot table ID not defined for area " + dbArea.Name + ". Drills cannot retrieve items.");
                return;
            }
            
            pcStructure.DateNextActivity = now.AddMinutes(increaseMinutes);

            var controlTower = BaseService.GetBaseControlTower(pcStructure.PCBaseID);
            var itemDetails = LootService.PickRandomItemFromLootTable(lootTableID);

            var tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
            NWItem item = _.CreateItemOnObject(itemDetails.Resref, tempStorage, itemDetails.Quantity);

            // Guard against invalid resrefs and missing items.
            if (!item.IsValid)
            {
                Console.WriteLine("ERROR: Could not create base drill item with resref '" + itemDetails.Resref + "'. Is this item valid?");
                return;
            }

            if (!string.IsNullOrWhiteSpace(itemDetails.SpawnRule))
            {
                var rule = SpawnService.GetSpawnRule(itemDetails.SpawnRule);
                rule.Run(item, retrievalRating);
            }

            var dbItem = new PCBaseStructureItem
            {
                PCBaseStructureID = controlTower.ID,
                ItemGlobalID = item.GlobalID.ToString(),
                ItemName = item.Name,
                ItemResref = item.Resref,
                ItemTag = item.Tag,
                ItemObject = SerializationService.Serialize(item)
            };

            DataService.SubmitDataChange(pcStructure, DatabaseActionType.Update);
            DataService.SubmitDataChange(dbItem, DatabaseActionType.Insert);
            item.Destroy();
        }
    }
}
