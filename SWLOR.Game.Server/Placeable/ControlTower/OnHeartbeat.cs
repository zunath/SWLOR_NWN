using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.ControlTower
{
    public class OnHeartbeat: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IBaseService _base;

        public OnHeartbeat(
            INWScript script,
            IDataContext db,
            IBaseService @base)
        {
            _ = script;
            _db = db;
            _base = @base;
        }
        public bool Run(params object[] args)
        {
            NWPlaceable tower = NWPlaceable.Wrap(Object.OBJECT_SELF);
            int structureID = tower.GetLocalInt("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);
            int maxShieldHP = _base.CalculateMaxShieldHP(structure);
            var pcBase = structure.PCBase;

            // Regular fuel usage
            if (DateTime.UtcNow >= pcBase.DateFuelEnds && pcBase.Fuel > 0)
            {
                pcBase.Fuel--;
                pcBase.DateFuelEnds = DateTime.UtcNow.AddMinutes(30);

                // If a player is manipulating fuel, look for a fuel item and reduce its stack size or destroy it
                NWPlaceable bay = NWPlaceable.Wrap(tower.GetLocalObject("CONTROL_TOWER_FUEL_BAY"));
                if (bay.IsValid)
                {
                    bool isStronidium = bay.GetLocalInt("CONTROL_TOWER_FUEL_TYPE") == TRUE;
                    if (!isStronidium)
                    {
                        NWItem fuel = NWItem.Wrap(_.GetFirstItemInInventory(bay.Object));

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
                Effect outOfPowerEffect = _.EffectVisualEffect(VFX_DUR_AURA_RED);
                outOfPowerEffect = _.TagEffect(outOfPowerEffect, "CONTROL_TOWER_OUT_OF_POWER");
                _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, outOfPowerEffect, tower.Object);
            }
            else
            {
                foreach (var effect in tower.Effects)
                {
                    if (_.GetEffectTag(effect) == "CONTROL_TOWER_OUT_OF_POWER")
                    {
                        _.RemoveEffect(tower.Object, effect);
                        break;
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
            else
            {
                pcBase.ShieldHP += 12 + (4 * structure.StructureBonus);
            }

            if (pcBase.ShieldHP > maxShieldHP)
                pcBase.ShieldHP = maxShieldHP;

            _db.SaveChanges();
            return true;
        }
    }
}
