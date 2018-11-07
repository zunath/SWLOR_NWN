using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.Drill
{
    public class OnHeartbeat: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IBaseService _base;
        private readonly ILootService _loot;
        private readonly ISerializationService _serialization;

        public OnHeartbeat(
            INWScript script,
            IDataService data,
            IBaseService @base,
            ILootService loot,
            ISerializationService serialization)
        {
            _ = script;
            _data = data;
            _base = @base;
            _loot = loot;
            _serialization = serialization;
        }
        public bool Run(params object[] args)
        {
            NWPlaceable drill = Object.OBJECT_SELF;
            int structureID = drill.GetLocalInt("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = _data.Get<PCBaseStructure>(structureID);
            PCBase pcBase = _data.Get<PCBase>(structure.PCBaseID);
            BaseStructure baseStructure = _data.Get<BaseStructure>(structure.BaseStructureID);
            DateTime now = DateTime.UtcNow;

            if (now >= pcBase.DateFuelEnds)
            {
                var outOfPowerEffect = drill.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "CONTROL_TOWER_OUT_OF_POWER");
                if (outOfPowerEffect == null)
                {
                    outOfPowerEffect = _.EffectVisualEffect(VFX_DUR_AURA_RED);
                    outOfPowerEffect = _.TagEffect(outOfPowerEffect, "CONTROL_TOWER_OUT_OF_POWER");
                    _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, outOfPowerEffect, drill);
                }

                return true;
            }

            int minuteReduce = 2 * structure.StructureBonus;
            int increaseMinutes = 60 - minuteReduce;
            int retrievalRating = baseStructure.RetrievalRating;

            if (increaseMinutes <= 20) increaseMinutes = 20;
            if (structure.DateNextActivity == null)
            {
                structure.DateNextActivity = now.AddMinutes(increaseMinutes);
                _data.SubmitDataChange(structure, DatabaseActionType.Update);
            }

            if (!(now >= structure.DateNextActivity)) return true;

            // Time to spawn a new item and reset the timer.
            var dbArea = _data.Single<Area>(x => x.Resref == pcBase.AreaResref);
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
            
            structure.DateNextActivity = now.AddMinutes(increaseMinutes);

            var controlTower = _base.GetBaseControlTower(structure.PCBaseID);
            var itemDetails = _loot.PickRandomItemFromLootTable(lootTableID);

            var tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
            NWItem item = _.CreateItemOnObject(itemDetails.Resref, tempStorage, itemDetails.Quantity);

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

            _data.SubmitDataChange(structure, DatabaseActionType.Update);
            _data.SubmitDataChange(dbItem, DatabaseActionType.Insert);
            item.Destroy();
            return true;
        }
    }
}
