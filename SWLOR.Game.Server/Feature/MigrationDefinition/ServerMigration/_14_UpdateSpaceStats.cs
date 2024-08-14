using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _14_UpdateSpaceStats: ServerMigrationBase, IServerMigration
    {
        public int Version => 14;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

        private uint GetTempStorage()
        {
            return GetObjectByTag("TEMP_ITEM_STORAGE");
        }

        public void Migrate()
        {
            var query = new DBQuery<PlayerShip>();
            var count = (int)DB.SearchCount(query);
            var dbShips = DB.Search(query
                .AddPaging(count, 0));

            foreach (var dbShip in dbShips)
            {
                var shipDetail = Space.GetShipDetailByItemTag(dbShip.Status.ItemTag);
                var shipItem = ObjectPlugin.Deserialize(dbShip.SerializedItem);
                var shipBonuses = Space.GetShipBonuses(shipItem);

                dbShip.Status.Shield = shipDetail.MaxShield + shipBonuses.Shield;
                dbShip.Status.MaxShield = shipDetail.MaxShield + shipBonuses.Shield;
                dbShip.Status.Hull = shipDetail.MaxHull + shipBonuses.Hull;
                dbShip.Status.MaxHull = shipDetail.MaxHull + shipBonuses.Hull;
                dbShip.Status.Capacitor = shipDetail.MaxCapacitor + shipBonuses.Capacitor;
                dbShip.Status.MaxCapacitor = shipDetail.MaxCapacitor + shipBonuses.Capacitor;
                dbShip.Status.EMDamage = shipBonuses.EMDamage;
                dbShip.Status.ExplosiveDamage = shipBonuses.ExplosiveDamage;
                dbShip.Status.ThermalDamage = shipBonuses.ThermalDamage;
                dbShip.Status.EMDefense = shipDetail.EMDefense + shipBonuses.EMDefense;
                dbShip.Status.ExplosiveDefense = shipDetail.ExplosiveDefense + shipBonuses.ExplosiveDefense;
                dbShip.Status.ThermalDefense = shipDetail.ThermalDefense + shipBonuses.ThermalDefense;
                dbShip.Status.Accuracy = shipDetail.Accuracy + shipBonuses.Accuracy;
                dbShip.Status.Evasion = shipDetail.Evasion + shipBonuses.Evasion;
                dbShip.Status.ShieldRechargeRate = shipDetail.ShieldRechargeRate - shipBonuses.ShieldRechargeRate;
                dbShip.Status.CapitalShip = shipDetail.CapitalShip;

                for (var slot = 1; slot <= 10; slot++)
                {
                    if (dbShip.Status.HighPowerModules.ContainsKey(slot))
                    {
                        var tempStorage = GetTempStorage();
                        var oldItem = ObjectPlugin.Deserialize(dbShip.Status.HighPowerModules[slot].SerializedItem);
                        var resref = GetResRef(oldItem);
                        var moduleBonus = Space.GetModuleBonus(oldItem);

                        var newItem = CreateItemOnObject(resref, tempStorage);
                        var newItemTag = GetTag(newItem);
                        var moduleDetails = Space.GetShipModuleDetailByItemTag(newItemTag);

                        if (moduleBonus > 0)
                        {
                            BiowareXP2.IPSafeAddItemProperty(newItem, ItemPropertyCustom(ItemPropertyType.ModuleBonus, -1, moduleBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                        }

                        dbShip.Status.HighPowerModules[slot].ItemInstanceId = GetObjectUUID(newItem);
                        dbShip.Status.HighPowerModules[slot].ItemTag = newItemTag;
                        dbShip.Status.HighPowerModules[slot].SerializedItem = ObjectPlugin.Serialize(newItem);

                        moduleDetails.ModuleEquippedAction?.Invoke(dbShip.Status, moduleBonus);

                        DestroyObject(oldItem);
                        DestroyObject(newItem);
                    }

                    if (dbShip.Status.LowPowerModules.ContainsKey(slot))
                    {
                        var tempStorage = GetTempStorage();
                        var oldItem = ObjectPlugin.Deserialize(dbShip.Status.LowPowerModules[slot].SerializedItem);
                        var resref = GetResRef(oldItem);
                        var moduleBonus = Space.GetModuleBonus(oldItem);

                        var newItem = CreateItemOnObject(resref, tempStorage);
                        var newItemTag = GetTag(newItem);
                        var moduleDetails = Space.GetShipModuleDetailByItemTag(newItemTag);

                        if (moduleBonus > 0)
                        {
                            BiowareXP2.IPSafeAddItemProperty(newItem, ItemPropertyCustom(ItemPropertyType.ModuleBonus, -1, moduleBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                        }

                        dbShip.Status.LowPowerModules[slot].ItemInstanceId = GetObjectUUID(newItem);
                        dbShip.Status.LowPowerModules[slot].ItemTag = newItemTag;
                        dbShip.Status.LowPowerModules[slot].SerializedItem = ObjectPlugin.Serialize(newItem);

                        moduleDetails.ModuleEquippedAction?.Invoke(dbShip.Status, moduleBonus);

                        DestroyObject(oldItem);
                        DestroyObject(newItem);
                    }
                }

                DB.Set(dbShip);

                DestroyObject(shipItem);
            }

        }
    }
}
