using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.ControlTower
{
    public class OnHeartbeat: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable tower = Object.OBJECT_SELF;
            Guid structureID = new Guid(tower.GetLocalString("PC_BASE_STRUCTURE_ID"));
            PCBaseStructure structure = DataService.Single<PCBaseStructure>(x => x.ID == structureID);
            int maxShieldHP = BaseService.CalculateMaxShieldHP(structure);
            var pcBase = DataService.Get<PCBase>(structure.PCBaseID);

            // Regular fuel usage
            if (DateTime.UtcNow >= pcBase.DateFuelEnds && pcBase.Fuel > 0)
            {
                pcBase.Fuel--;
                BaseStructure towerStructure = DataService.Single<BaseStructure>(x => x.ID == structure.BaseStructureID);
                int fuelRating = towerStructure.FuelRating;
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

                pcBase.DateFuelEnds = DateTime.UtcNow.AddMinutes(minutes);

                // If a player is manipulating fuel, look for a fuel item and reduce its stack size or destroy it
                NWPlaceable bay = tower.GetLocalObject("CONTROL_TOWER_FUEL_BAY");
                if (bay.IsValid)
                {
                    bool isStronidium = bay.GetLocalInt("CONTROL_TOWER_FUEL_TYPE") == TRUE;
                    if (!isStronidium)
                    {
                        NWItem fuel = _.GetFirstItemInInventory(bay.Object);

                        if (fuel.IsValid)
                        {
                            fuel.ReduceItemStack();
                        }
                    }
                }
            }

            // If completely out of power, show tower in red.
            if (pcBase.Fuel <= 0 && DateTime.UtcNow > pcBase.DateFuelEnds)
            {
                bool outOfPowerHasBeenApplied = false;
                foreach (var effect in tower.Effects)
                {
                    if (_.GetEffectTag(effect) == "CONTROL_TOWER_OUT_OF_POWER")
                    {
                        outOfPowerHasBeenApplied = true;
                        break;
                    }
                }

                if (!outOfPowerHasBeenApplied)
                {
                    Effect outOfPowerEffect = _.EffectVisualEffect(VFX_DUR_AURA_RED);
                    outOfPowerEffect = _.TagEffect(outOfPowerEffect, "CONTROL_TOWER_OUT_OF_POWER");
                    _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, outOfPowerEffect, tower.Object);

                    var instances = NWModule.Get().Areas.Where(x => x.GetLocalString("PC_BASE_STRUCTURE_ID") == structureID.ToString());

                    foreach (var instance in instances)
                    {
                        BaseService.ToggleInstanceObjectPower(instance, false);
                    }
                }
            }
            else
            {
                bool outOfPowerWasRemoved = false;
                foreach (var effect in tower.Effects)
                {
                    if (_.GetEffectTag(effect) == "CONTROL_TOWER_OUT_OF_POWER")
                    {
                        _.RemoveEffect(tower.Object, effect);
                        outOfPowerWasRemoved = true;
                        break;
                    }
                }

                if (outOfPowerWasRemoved)
                {
                    var instances = NWModule.Get().Areas.Where(x => x.GetLocalString("PC_BASE_STRUCTURE_ID") == structureID.ToString());
                    foreach (var instance in instances)
                    {
                        BaseService.ToggleInstanceObjectPower(instance, true);
                    }
                }
            }


            // Reinforced mode fuel usage
            if (pcBase.IsInReinforcedMode)
            {
                pcBase.ReinforcedFuel--;

                if (pcBase.ReinforcedFuel <= 0)
                {
                    pcBase.ReinforcedFuel = 0;
                    pcBase.IsInReinforcedMode = false;
                }
            }
            // Tower regeneration only happens if fueled.
            else if(pcBase.DateFuelEnds > DateTime.UtcNow)
            {
                pcBase.ShieldHP += 12 + 4 * structure.StructureBonus;
            }

            if (pcBase.ShieldHP > maxShieldHP)
                pcBase.ShieldHP = maxShieldHP;

            DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
            return true;
        }
    }
}
