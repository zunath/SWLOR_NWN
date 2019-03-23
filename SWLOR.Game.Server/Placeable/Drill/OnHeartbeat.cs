using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.Drill
{
    public class OnHeartbeat: IRegisteredEvent
    {
        
        
        private readonly IBaseService _base;
        private readonly ILootService _loot;
        private readonly ISerializationService _serialization;

        public OnHeartbeat(
            
            
            IBaseService @base,
            ILootService loot,
            ISerializationService serialization)
        {
            
            
            _base = @base;
            _loot = loot;
            _serialization = serialization;
        }
        public bool Run(params object[] args)
        {
            NWPlaceable drill = Object.OBJECT_SELF;
            string structureID = drill.GetLocalString("PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(structureID))
            {
                string areaName = drill.Area.Name;
                Console.WriteLine("There was an error retrieving the PC_BASE_STRUCTURE_ID variable on drill in area: " + areaName);
                return false;
            }

            Guid structureGUID = new Guid(structureID);
            PCBaseStructure pcStructure = DataService.Get<PCBaseStructure>(structureGUID);
            PCBase pcBase = DataService.Get<PCBase>(pcStructure.PCBaseID);
            PCBaseStructure tower = _base.GetBaseControlTower(pcBase.ID);

            // Check whether there's space in this tower.
            int capacity = _base.CalculateResourceCapacity(pcBase.ID);
            int count = DataService.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == tower.ID).Count() + 1;
            if (count > capacity) return false;

            BaseStructure baseStructure = DataService.Get<BaseStructure>(pcStructure.BaseStructureID);
            DateTime now = DateTime.UtcNow;

            var outOfPowerEffect = drill.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "CONTROL_TOWER_OUT_OF_POWER");
            if (now >= pcBase.DateFuelEnds)
            {
                if (outOfPowerEffect == null)
                {
                    outOfPowerEffect = _.EffectVisualEffect(VFX_DUR_AURA_RED);
                    outOfPowerEffect = _.TagEffect(outOfPowerEffect, "CONTROL_TOWER_OUT_OF_POWER");
                    _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, outOfPowerEffect, drill);
                }

                return true;
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

            if (!(now >= pcStructure.DateNextActivity)) return true;

            // Time to spawn a new item and reset the timer.
            var dbArea = DataService.Single<Area>(x => x.Resref == pcBase.AreaResref);
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
                return false;
            }
            
            pcStructure.DateNextActivity = now.AddMinutes(increaseMinutes);

            var controlTower = _base.GetBaseControlTower(pcStructure.PCBaseID);
            var itemDetails = _loot.PickRandomItemFromLootTable(lootTableID);

            var tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
            NWItem item = _.CreateItemOnObject(itemDetails.Resref, tempStorage, itemDetails.Quantity);

            // Guard against invalid resrefs and missing items.
            if (!item.IsValid)
            {
                Console.WriteLine("ERROR: Could not create base drill item with resref '" + itemDetails.Resref + "'. Is this item valid?");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(itemDetails.SpawnRule))
            {
                App.ResolveByInterface<ISpawnRule>("SpawnRule." + itemDetails.SpawnRule, action =>
                {
                    action.Run(item, retrievalRating);
                });
            }

            var dbItem = new PCBaseStructureItem
            {
                PCBaseStructureID = controlTower.ID,
                ItemGlobalID = item.GlobalID.ToString(),
                ItemName = item.Name,
                ItemResref = item.Resref,
                ItemTag = item.Tag,
                ItemObject = _serialization.Serialize(item)
            };

            DataService.SubmitDataChange(pcStructure, DatabaseActionType.Update);
            DataService.SubmitDataChange(dbItem, DatabaseActionType.Insert);
            item.Destroy();
            return true;
        }
    }
}
