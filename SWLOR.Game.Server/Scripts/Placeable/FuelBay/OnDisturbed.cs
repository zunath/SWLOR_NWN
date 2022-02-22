﻿using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Item;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.NWN._;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;

namespace SWLOR.Game.Server.Scripts.Placeable.FuelBay
{
    public class OnDisturbed : IScript
    {
        public void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnItemDisturbed>(message => OnItemDisturbed());

        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
        }

        public void OnItemDisturbed()
        {
            NWPlaceable bay = OBJECT_SELF;

            // Filer to only events created by a fuel bay, and ignore this event if it was triggered by clearing
            // the inventory when the bay is being destroyed.
            if (bay.Resref != "fuel_bay" || bay.GetLocalBool("SETUP") == true)
            {
                return;
            }

            NWItem item = StringToObject(NWNXEvents.GetEventData("ITEM"));
            NWPlayer player = bay.GetLocalObject("BAY_ACCESSOR");
            var disturbType = NWNXEvents.GetCurrentEvent();
            bool stronidiumOnly = GetLocalBool(bay, "CONTROL_TOWER_FUEL_TYPE");
            string allowedResref = stronidiumOnly ? "stronidium" : "fuel_cell";
            string structureID = bay.GetLocalString("PC_BASE_STRUCTURE_ID");
            
            // Check for either fuel cells or stronidium when adding an item to the container.
            if (disturbType == EventType.ItemInventoryAddItemAfter)
            {
                if (item.Resref != allowedResref)
                {
                    ItemService.ReturnItem(player, item);
                    player.SendMessage("Only " + (stronidiumOnly ? "Stronidium" : "Fuel Cells") + " may be placed inside this fuel bay.");
                    return;
                }
            }
            // If the item removed wasn't fuel cells or stronidium, exit early. We don't need to do anything else.
            else if (disturbType == EventType.ItemInventoryRemoveItemAfter)
            {
                if (item.Resref != allowedResref)
                {
                    return;
                }
            }

            var structure = DataService.PCBaseStructure.GetByID(new Guid(structureID));
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

            // Calculate how much fuel exists in the bay's inventory.
            int fuelCount = 0;
            foreach (var fuel in bay.InventoryItems)
            {
                fuelCount += fuel.StackSize;
            }
            
            // If there are extra units of fuel, destroy them. We will set the stack size of the first fuel later on.
            NWItem firstFuel = GetFirstItemInInventory(bay.Object);
            NWItem nextFuel = GetNextItemInInventory(bay.Object);
            while (nextFuel.IsValid)
            {
                nextFuel.Destroy();
                nextFuel = GetNextItemInInventory(bay.Object);
            }

            int maxFuel;

            // Handle Stronidium fuel process
            if (stronidiumOnly)
            {
                maxFuel = BaseService.CalculateMaxReinforcedFuel(pcBase.ID);

                // For starships only: Add the ship's cargo bonus to the max stronidium amount.
                if (bay.Area.GetLocalInt("BUILDING_TYPE") == (int)Enumeration.BuildingType.Starship)
                {
                    maxFuel += 25 * SpaceService.GetCargoBonus(SpaceService.GetCargoBay(player.Area, player), ItemPropertyType.StarshipStronidiumBonus);
                }

                // Did the player put too much fuel inside? Return the excess to their inventory.
                if (fuelCount > maxFuel)
                {
                    int returnAmount = fuelCount - maxFuel;
                    CreateItemOnObject("stronidium", player.Object, returnAmount);

                    fuelCount = maxFuel;
                }

                firstFuel.StackSize = fuelCount;
                pcBase.ReinforcedFuel = fuelCount;

                if (bay.Area.GetLocalInt("BUILDING_TYPE") == (int)Enumeration.BuildingType.Starship)
                {
                    // This is a starship. Update the creature object, if any, with the new fuel count. 
                    NWCreature ship = bay.Area.GetLocalObject("CREATURE");

                    if (ship.IsValid)
                    {
                        ship.SetLocalInt("STRONIDIUM", fuelCount);
                    }
                }
            }
            // Handle Fuel Cell process
            else
            {
                maxFuel = BaseService.CalculateMaxFuel(pcBase.ID);

                // For starships only: Add the ship's cargo bonus to the max fuel amount.
                if (bay.Area.GetLocalInt("BUILDING_TYPE") == (int)Enumeration.BuildingType.Starship)
                {
                    maxFuel += 25 * SpaceService.GetCargoBonus(SpaceService.GetCargoBay(player.Area, player), ItemPropertyType.StarshipFuelBonus);
                }

                // Did the player put too much fuel inside? Return the excess to their inventory.
                if (fuelCount > maxFuel)
                {
                    int returnAmount = fuelCount - maxFuel;
                    CreateItemOnObject("fuel_cell", player.Object, returnAmount);

                    fuelCount = maxFuel;
                }

                firstFuel.StackSize = fuelCount;
                pcBase.Fuel = fuelCount;
            }

            // Submit a DB data change for the fuel or stronidium amount adjustment.
            DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);

            var tower = BaseService.GetBaseControlTower(structure.PCBaseID);

            if (tower == null)
            {
                Console.WriteLine("Could not locate tower in Fuel Bay. PCBaseID = " + structure.PCBaseID);
                return;
            }

            var towerStructure = DataService.BaseStructure.GetByID(tower.BaseStructureID);

            if (towerStructure.BaseStructureTypeID == (int)BaseStructureType.Starship)
            {
                // This is a spaceship, so don't give the feedback message.
                return;
            }

            int fuelRating = towerStructure.FuelRating;
            

            // Stronidium - Every unit lasts for 6 seconds during reinforcement mode.
            if (stronidiumOnly)
            {
                int seconds = 6 * fuelCount;
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                player.SendMessage(ColorTokenService.Gray("Reinforcement mode will last for " +
                                               TimeService.GetTimeLongIntervals(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, false) +
                                               " (" + fuelCount + " / " + maxFuel + " units"));
            }
            // Regular fuel cells - Every unit lasts for 45, 15, or 5 minutes depending on the size of the tower.
            else
            {
                int minutes;
                switch (fuelRating)
                {
                    case 1: // Small
                        minutes = 45;
                        break;
                    case 2: // Medium
                        minutes = 15;
                        break;
                    case 3: // Large
                        minutes = 5;
                        break;
                    default:
                        throw new Exception("Invalid fuel rating value: " + fuelRating);
                }

                TimeSpan timeSpan = TimeSpan.FromMinutes(minutes * fuelCount);
                player.SendMessage(ColorTokenService.Gray("Fuel will last for " +
                                               TimeService.GetTimeLongIntervals(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, false) +
                                               " (" + fuelCount + " / " + maxFuel + " units)"));
            }


            return;
        }


    }
}
