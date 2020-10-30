using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.Legacy;

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
            NWPlaceable drill = NWScript.OBJECT_SELF;
            var structureID = drill.GetLocalString("PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(structureID))
            {
                var areaName = drill.Area.Name;
                Console.WriteLine("There was an error retrieving the PC_BASE_STRUCTURE_ID variable on drill in area: " + areaName);
                return;
            }

            var structureGUID = new Guid(structureID);
            var pcStructure = DataService.PCBaseStructure.GetByID(structureGUID);
            var pcBase = DataService.PCBase.GetByID(pcStructure.PCBaseID);
            var tower = BaseService.GetBaseControlTower(pcBase.ID);

            if (tower == null)
            {
                Console.WriteLine("Could not locate valid tower in Drill OnHeartbeat. PCBaseID = " + pcBase.ID);
                return;
            }

            // Check whether there's space in this tower.
            var capacity = BaseService.CalculateResourceCapacity(pcBase.ID);
            var count = DataService.PCBaseStructureItem.GetNumberOfItemsContainedBy(tower.ID) + 1;
            if (count > capacity) return;

            var baseStructure = DataService.BaseStructure.GetByID(pcStructure.BaseStructureID);
            var now = DateTime.UtcNow;

            var outOfPowerEffect = drill.Effects.SingleOrDefault(x => NWScript.GetEffectTag(x) == "CONTROL_TOWER_OUT_OF_POWER");
            if (now >= pcBase.DateFuelEnds)
            {
                if (outOfPowerEffect == null)
                {
                    outOfPowerEffect = NWScript.EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Red);
                    outOfPowerEffect = NWScript.TagEffect(outOfPowerEffect, "CONTROL_TOWER_OUT_OF_POWER");
                    NWScript.ApplyEffectToObject(DurationType.Permanent, outOfPowerEffect, drill);
                }

                return;
            }
            else if (now < pcBase.DateFuelEnds && outOfPowerEffect != null)
            {
                NWScript.RemoveEffect(drill, outOfPowerEffect);
            }

            var minuteReduce = 2 * pcStructure.StructureBonus;
            var increaseMinutes = 60 - minuteReduce;
            var retrievalRating = baseStructure.RetrievalRating;

            if (increaseMinutes <= 20) increaseMinutes = 20;
            if (pcStructure.DateNextActivity == null)
            {
                pcStructure.DateNextActivity = now.AddMinutes(increaseMinutes);
                DataService.SubmitDataChange(pcStructure, DatabaseActionType.Update);
            }

            if (!(now >= pcStructure.DateNextActivity)) return;

            // Time to spawn a new item and reset the timer.
            var dbArea = DataService.Area.GetByResref(pcBase.AreaResref);
            var sector = pcBase.Sector;
            var lootTableID = 0;

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

            if (controlTower == null)
            {
                Console.WriteLine("Could not locate control tower in drill heartbeat. PCBaseID = " + pcStructure.PCBaseID);
                return;
            }

            var itemDetails = LootService.PickRandomItemFromLootTable(lootTableID);

            var tempStorage = NWScript.GetObjectByTag("TEMP_ITEM_STORAGE");
            NWItem item = NWScript.CreateItemOnObject(itemDetails.Resref, tempStorage, itemDetails.Quantity);

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
