using System;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System.Linq;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.FuelBay
{
    public class OnDisturbed : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IItemService _item;
        private readonly ITimeService _time;
        private readonly IColorTokenService _color;

        public OnDisturbed(
            INWScript script,
            IDataContext db,
            IItemService item,
            ITimeService time,
            IColorTokenService color)
        {
            _ = script;
            _db = db;
            _item = item;
            _time = time;
            _color = color;
        }
        public bool Run(params object[] args)
        {
            NWPlayer player = NWPlayer.Wrap(_.GetLastDisturbed());
            NWPlaceable bay = NWPlaceable.Wrap(Object.OBJECT_SELF);
            int disturbType = _.GetInventoryDisturbType();
            NWItem item = NWItem.Wrap(_.GetInventoryDisturbItem());
            bool stronidiumOnly = bay.GetLocalInt("CONTROL_TOWER_FUEL_TYPE") == TRUE;
            string allowedResref = stronidiumOnly ? "stronidium" : "fuel_cell";
            int structureID = bay.GetLocalInt("PC_BASE_STRUCTURE_ID");
            
            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                if (item.Resref != allowedResref)
                {
                    _item.ReturnItem(player, item);
                    player.SendMessage("Only " + (stronidiumOnly ? "Stronidium" : "Fuel Cells") + " may be placed inside this fuel bay.");
                    return false;
                }
            }
            else if (disturbType == INVENTORY_DISTURB_TYPE_REMOVED)
            {
                if (item.Resref != allowedResref)
                {
                    return false;
                }
            }

            var structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);

            int fuelCount = 0;
            foreach (var fuel in bay.InventoryItems)
            {
                fuelCount += fuel.StackSize;
            }
            
            NWItem firstFuel = NWItem.Wrap(_.GetFirstItemInInventory(bay.Object));
            NWItem nextFuel = NWItem.Wrap(_.GetNextItemInInventory(bay.Object));
            while (nextFuel.IsValid)
            {
                nextFuel.Destroy();
                nextFuel = NWItem.Wrap(_.GetNextItemInInventory(bay.Object));
            }

            int maxFuel = GetMaxFuel(structure, stronidiumOnly);
            if (stronidiumOnly)
            {
                if (fuelCount > maxFuel)
                {
                    int returnAmount = fuelCount - maxFuel;
                    _.CreateItemOnObject("stronidium", player.Object, returnAmount);

                    fuelCount = maxFuel;
                }

                firstFuel.StackSize = fuelCount;
                structure.ReinforcedFuel = fuelCount;
            }
            else
            {
                if (fuelCount > maxFuel)
                {
                    int returnAmount = fuelCount - maxFuel;
                    _.CreateItemOnObject("fuel_cell", player.Object, returnAmount);

                    fuelCount = maxFuel;
                }

                firstFuel.StackSize = fuelCount;
                structure.Fuel = fuelCount;
            }

            _db.SaveChanges();

            TimeSpan timeSpan = TimeSpan.FromMinutes(30.0f * fuelCount);
            player.SendMessage(_color.Gray("Fuel will last for " + 
                               _time.GetTimeLongIntervals(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, false) + 
                               " (" + fuelCount + " / " + maxFuel + " units)"));

            return true;
        }

        private int GetMaxFuel(PCBaseStructure structure, bool stronidiumOnly)
        {
            int siloType = stronidiumOnly ? (int)Enumeration.BaseStructureType.StronidiumSilo : (int)Enumeration.BaseStructureType.FuelSilo;

            float siloBonus = _db.PCBaseStructures
                .Where(x => x.PCBaseID == structure.PCBaseID &&
                            x.BaseStructure.BaseStructureTypeID == siloType)
                .DefaultIfEmpty()
                .Sum(x => x == null ? 0 : x.BaseStructure.Storage + x.StructureBonus) * 0.01f;

            var fuelMax = stronidiumOnly ? 
                structure.BaseStructure.ReinforcedStorage : 
                structure.BaseStructure.Storage;

            return (int)(fuelMax + fuelMax * siloBonus);
        }

    }
}
