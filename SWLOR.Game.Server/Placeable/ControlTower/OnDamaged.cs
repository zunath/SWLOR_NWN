using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.ControlTower
{
    public class OnDamaged: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IRandomService _random;
        private readonly IBaseService _base;
        private readonly ISerializationService _serialization;

        public OnDamaged(
            INWScript script,
            IDataContext db,
            IRandomService random,
            IBaseService @base,
            ISerializationService serialization)
        {
            _ = script;
            _db = db;
            _random = random;
            _base = @base;
            _serialization = serialization;
        }

        public bool Run(params object[] args)
        {
            NWCreature attacker = NWCreature.Wrap(_.GetLastDamager(Object.OBJECT_SELF));
            NWPlaceable tower = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWItem weapon = NWItem.Wrap(_.GetLastWeaponUsed(attacker.Object));
            int damage = _.GetTotalDamageDealt();
            int structureID = tower.GetLocalInt("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);
            int maxShieldHP = _base.CalculateMaxShieldHP(structure);
            PCBase pcBase = structure.PCBase;
            pcBase.ShieldHP -= damage;
            if (pcBase.ShieldHP <= 0) pcBase.ShieldHP = 0;
            float hpPercentage = (float)pcBase.ShieldHP / (float)maxShieldHP * 100.0f;

            if (hpPercentage <= 25.0f && pcBase.ReinforcedFuel > 0)
            {
                pcBase.IsInReinforcedMode = true;
                pcBase.ShieldHP = (int)(maxShieldHP * 0.25f);
            }

            attacker.SendMessage("Tower Shields: " + hpPercentage.ToString("0.00") + "%");

            if (pcBase.IsInReinforcedMode)
            {
                attacker.SendMessage("Control tower is in reinforced mode and cannot be damaged. Reinforced mode will be disabled when the tower runs out of fuel.");
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(9999), tower.Object);
            weapon.Durability -= _random.RandomFloat(0.01f, 0.03f);

            if (pcBase.ShieldHP <= 0)
            {
                pcBase.ShieldHP = 0;
                
                structure.Durability -= _random.RandomFloat(0.5f, 2.0f);
                if (structure.Durability < 0.0f) structure.Durability = 0.0f;
                attacker.SendMessage("Structure Durability: " + structure.Durability.ToString("0.00"));

                if (structure.Durability <= 0.0f)
                {
                    structure.Durability = 0.0f;
                    BlowUpBase(structure.PCBase);
                }
            }


            _db.SaveChanges();
            return true;
        }


        private void BlowUpBase(PCBase pcBase)
        {
            NWArea area = NWArea.Wrap(_.GetArea(Object.OBJECT_SELF));
            List<AreaStructure> cache = area.Data["BASE_SERVICE_STRUCTURES"];
            cache = cache.Where(x => x.PCBaseID == pcBase.PCBaseID).ToList();
            
            foreach (var structure in cache)
            {
                // Child structures will be picked up later on in the process.
                // Just destroy the structure and continue on.
                if (structure.ChildStructure != null)
                {
                    structure.Structure.Destroy();
                    continue;
                }

                var dbStructure = pcBase.PCBaseStructures.Single(x => x.PCBaseStructureID == structure.PCBaseStructureID);

                // Explosion effect
                Location location = structure.Structure.Location;
                _.ApplyEffectAtLocation(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_FNF_FIREBALL), location);

                // Boot from instance, if any
                _base.BootPlayersOutOfInstance(structure.PCBaseStructureID);

                // Spawn container for items
                NWPlaceable container = NWPlaceable.Wrap(_.CreateObject(OBJECT_TYPE_PLACEABLE, "structure_rubble", structure.Structure.Location));
                container.Name = dbStructure.BaseStructure.Name + " Rubble";

                // Drop item storage into container
                for (int i = dbStructure.PCBaseStructureItems.Count - 1; i >= 0; i--)
                {
                    var dbItem = dbStructure.PCBaseStructureItems.ElementAt(i);
                    _serialization.DeserializeItem(dbItem.ItemObject, container);
                    _db.PCBaseStructureItems.Remove(dbItem);
                }

                // Convert child placeables to items and drop into container
                var children = dbStructure.ChildStructures;
                if (children != null)
                {
                    
                    for (int f = dbStructure.ChildStructures.Count - 1; f >= 0; f--)
                    {
                        var child = children.ElementAt(f);

                        // Move child items to container
                        for (int i = child.PCBaseStructureItems.Count - 1; i >= 0; i++)
                        {
                            var dbItem = child.PCBaseStructureItems.ElementAt(i);
                            _serialization.DeserializeItem(dbItem.ItemObject, container);
                            _db.PCBaseStructureItems.Remove(dbItem);
                        }

                        // Convert child structure to item
                        _base.ConvertStructureToItem(child, container);
                        _db.PCBaseStructures.Remove(child);
                    }
                }

                // Clear structure permissions
                for (int p = dbStructure.PCBaseStructurePermissions.Count - 1; p >= 0; p--)
                {
                    var permission = dbStructure.PCBaseStructurePermissions.ElementAt(p);
                    _db.PCBaseStructurePermissions.Remove(permission);
                }

                // Destroy structure placeable
                _db.PCBaseStructures.Remove(dbStructure);
                structure.Structure.Destroy();
            }

            // Remove from cache
            foreach (var record in cache)
            {
                ((List<AreaStructure>)area.Data["BASE_SERVICE_STRUCTURES"]).Remove(record);
            }
            
            // Clear base permissions
            for (int p = pcBase.PCBasePermissions.Count - 1; p >= 0; p--)
            {
                var permission = pcBase.PCBasePermissions.ElementAt(p);
                _db.PCBasePermissions.Remove(permission);
            }
            
            _db.PCBases.Remove(pcBase);


            Area dbArea = _db.Areas.Single(x => x.Resref == pcBase.AreaResref);
            if (pcBase.Sector == AreaSector.Northeast) dbArea.NortheastOwner = null;
            else if (pcBase.Sector == AreaSector.Northwest) dbArea.NorthwestOwner = null;
            else if (pcBase.Sector == AreaSector.Southeast) dbArea.SoutheastOwner = null;
            else if (pcBase.Sector == AreaSector.Southwest) dbArea.SouthwestOwner = null;

        }
    }
}
