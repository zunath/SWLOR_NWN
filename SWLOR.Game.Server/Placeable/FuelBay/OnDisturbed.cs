using System;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.FuelBay
{
    public class OnDisturbed : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IItemService _item;
        private readonly ITimeService _time;
        private readonly IColorTokenService _color;
        private readonly IBaseService _base;

        public OnDisturbed(
            INWScript script,
            IDataService data,
            IItemService item,
            ITimeService time,
            IColorTokenService color,
            IBaseService @base)
        {
            _ = script;
            _data = data;
            _item = item;
            _time = time;
            _color = color;
            _base = @base;
        }
        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetLastDisturbed());
            NWPlaceable bay = (Object.OBJECT_SELF);
            int disturbType = _.GetInventoryDisturbType();
            NWItem item = (_.GetInventoryDisturbItem());
            bool stronidiumOnly = bay.GetLocalInt("CONTROL_TOWER_FUEL_TYPE") == TRUE;
            string allowedResref = stronidiumOnly ? "stronidium" : "fuel_cell";
            string structureID = bay.GetLocalString("PC_BASE_STRUCTURE_ID");
            
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

            var structure = _data.Single<PCBaseStructure>(x => x.ID == new Guid(structureID));
            var pcBase = _data.Get<PCBase>(structure.PCBaseID);

            int fuelCount = 0;
            foreach (var fuel in bay.InventoryItems)
            {
                fuelCount += fuel.StackSize;
            }
            
            NWItem firstFuel = (_.GetFirstItemInInventory(bay.Object));
            NWItem nextFuel = (_.GetNextItemInInventory(bay.Object));
            while (nextFuel.IsValid)
            {
                nextFuel.Destroy();
                nextFuel = (_.GetNextItemInInventory(bay.Object));
            }

            int maxFuel;
            if (stronidiumOnly)
            {
                maxFuel = _base.CalculateMaxReinforcedFuel(pcBase.ID);
                if (fuelCount > maxFuel)
                {
                    int returnAmount = fuelCount - maxFuel;
                    _.CreateItemOnObject("stronidium", player.Object, returnAmount);

                    fuelCount = maxFuel;
                }

                firstFuel.StackSize = fuelCount;
                pcBase.ReinforcedFuel = fuelCount;
            }
            else
            {
                maxFuel = _base.CalculateMaxFuel(pcBase.ID);
                if (fuelCount > maxFuel)
                {
                    int returnAmount = fuelCount - maxFuel;
                    _.CreateItemOnObject("fuel_cell", player.Object, returnAmount);

                    fuelCount = maxFuel;
                }

                firstFuel.StackSize = fuelCount;
                pcBase.Fuel = fuelCount;
            }

            _data.SubmitDataChange(pcBase, DatabaseActionType.Update);

            TimeSpan timeSpan = TimeSpan.FromMinutes(30.0f * fuelCount);
            player.SendMessage(_color.Gray("Fuel will last for " + 
                               _time.GetTimeLongIntervals(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, false) + 
                               " (" + fuelCount + " / " + maxFuel + " units)"));

            return true;
        }


    }
}
