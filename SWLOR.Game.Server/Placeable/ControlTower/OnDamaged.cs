using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;
using Object = NWN.Object;
using System.Globalization;

namespace SWLOR.Game.Server.Placeable.ControlTower
{
    public class OnDamaged: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IRandomService _random;
        private readonly IBaseService _base;
        private readonly ISerializationService _serialization;
        private readonly IDurabilityService _durability;
        private readonly IPlayerService _player;

        public OnDamaged(
            INWScript script,
            IDataService data,
            IRandomService random,
            IBaseService @base,
            ISerializationService serialization,
            IDurabilityService durability,
            IPlayerService player)
        {
            _ = script;
            _data = data;
            _random = random;
            _base = @base;
            _serialization = serialization;
            _durability = durability;
            _player = player;
        }

        public bool Run(params object[] args)
        {
            NWCreature attacker = (_.GetLastDamager(Object.OBJECT_SELF));
            NWPlaceable tower = (Object.OBJECT_SELF);
            NWItem weapon = (_.GetLastWeaponUsed(attacker.Object));
            int damage = _.GetTotalDamageDealt();
            var structureID = tower.GetLocalString("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = _data.Single<PCBaseStructure>(x => x.ID == new Guid(structureID));
            int maxShieldHP = _base.CalculateMaxShieldHP(structure);
            PCBase pcBase = _data.Get<PCBase>(structure.PCBaseID);
            var playerIDs = _data.Where<PCBasePermission>(x => x.PCBaseID == structure.PCBaseID).Select(s => s.PlayerID);
            var toNotify = NWModule.Get().Players.Where(x => playerIDs.Contains(x.GlobalID));
            DateTime timer = DateTime.UtcNow.AddSeconds(30);
            string clock = timer.ToString(CultureInfo.InvariantCulture);
            string sector = _base.GetSectorOfLocation(attacker.Location);
            if (DateTime.UtcNow <= DateTime.Parse(clock))
            {
                foreach(NWPlayer player in toNotify)
                {
                    player.SendMessage("Your base in " + attacker.Area.Name + " " + sector + "Is under Attack!");
                }
            }
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

            var durability = _durability.GetDurability(weapon) - _random.RandomFloat(0.01f, 0.03f);
            _durability.SetDurability(weapon, durability);

            if (pcBase.ShieldHP <= 0)
            {
                pcBase.ShieldHP = 0;
                
                structure.Durability -= _random.RandomFloat(0.5f, 2.0f);
                if (structure.Durability < 0.0f) structure.Durability = 0.0f;
                attacker.SendMessage("Structure Durability: " + structure.Durability.ToString("0.00"));

                if (structure.Durability <= 0.0f)
                {
                    structure.Durability = 0.0f;
                    BlowUpBase(pcBase);
                    return true;
                }
            }

            _data.SubmitDataChange(pcBase, DatabaseActionType.Update);
            _data.SubmitDataChange(structure, DatabaseActionType.Update);
            return true;
        }


        private void BlowUpBase(PCBase pcBase)
        {
            NWArea area = (_.GetArea(Object.OBJECT_SELF));
            List<AreaStructure> cache = area.Data["BASE_SERVICE_STRUCTURES"];
            cache = cache.Where(x => x.PCBaseID == pcBase.ID).ToList();
            
            foreach (var structure in cache)
            {
                // Child structures will be picked up later on in the process.
                // Just destroy the structure and continue on.
                if (structure.ChildStructure != null)
                {
                    structure.Structure.Destroy();
                    continue;
                }

                var dbStructure = _data.Get<PCBaseStructure>(structure.PCBaseStructureID);
                var baseStructure = _data.Get<BaseStructure>(dbStructure.BaseStructureID);
                var items = _data.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == structure.PCBaseStructureID).ToList();
                var children = _data.Where<PCBaseStructure>(x => x.ParentPCBaseStructureID == dbStructure.ParentPCBaseStructureID).ToList();
                
                // Explosion effect
                Location location = structure.Structure.Location;
                _.ApplyEffectAtLocation(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_FNF_FIREBALL), location);

                // Boot from instance, if any
                _base.BootPlayersOutOfInstance(structure.PCBaseStructureID);

                // Spawn container for items
                NWPlaceable container = (_.CreateObject(OBJECT_TYPE_PLACEABLE, "structure_rubble", structure.Structure.Location));
                container.Name = baseStructure.Name + " Rubble";

                // Drop item storage into container
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    var dbItem = items.ElementAt(i);
                    _serialization.DeserializeItem(dbItem.ItemObject, container);
                    _data.SubmitDataChange(dbItem, DatabaseActionType.Delete);
                }

                // Convert child placeables to items and drop into container
                for (int f = children.Count - 1; f >= 0; f--)
                {
                    var child = children.ElementAt(f);
                    var childItems = _data.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == child.ID).ToList();

                    // Move child items to container
                    for (int i = childItems.Count - 1; i >= 0; i++)
                    {
                        var dbItem = childItems.ElementAt(i);
                        _serialization.DeserializeItem(dbItem.ItemObject, container);
                        _data.SubmitDataChange(dbItem, DatabaseActionType.Delete);
                    }

                    // Convert child structure to item
                    _base.ConvertStructureToItem(child, container);
                    _data.SubmitDataChange(child, DatabaseActionType.Delete);
                }
            

                // Clear structure permissions
                var structurePermissions = _data.Where<PCBaseStructurePermission>(x => x.PCBaseStructureID == dbStructure.ID).ToList();
                for (int p = structurePermissions.Count - 1; p >= 0; p--)
                {
                    var permission = structurePermissions.ElementAt(p);
                    _data.SubmitDataChange(permission, DatabaseActionType.Delete);
                }

                // Destroy structure placeable
                _data.SubmitDataChange(dbStructure, DatabaseActionType.Delete);
                structure.Structure.Destroy();
            }

            // Remove from cache
            foreach (var record in cache)
            {
                ((List<AreaStructure>)area.Data["BASE_SERVICE_STRUCTURES"]).Remove(record);
            }
            var basePermissions = _data.Where<PCBasePermission>(x => x.PCBaseID == pcBase.ID).ToList();

            // Clear base permissions
            for (int p = basePermissions.Count - 1; p >= 0; p--)
            {
                var permission = basePermissions.ElementAt(p);
                _data.SubmitDataChange(permission, DatabaseActionType.Delete);
            }
            
            _data.SubmitDataChange(pcBase, DatabaseActionType.Delete);
            
            Area dbArea = _data.Single<Area>(x => x.Resref == pcBase.AreaResref);
            if (pcBase.Sector == AreaSector.Northeast) dbArea.NortheastOwner = null;
            else if (pcBase.Sector == AreaSector.Northwest) dbArea.NorthwestOwner = null;
            else if (pcBase.Sector == AreaSector.Southeast) dbArea.SoutheastOwner = null;
            else if (pcBase.Sector == AreaSector.Southwest) dbArea.SouthwestOwner = null;

        }
    }
}
