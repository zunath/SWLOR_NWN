using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;

using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    public abstract class ServerMigrationBase
    {
        private static ILogger _logger = ServiceContainer.GetService<ILogger>();
        protected void GrantRebuildTokenToAllPlayers()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var dbPlayers = DB.Search(query
                .AddPaging(count, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                if (!dbPlayer.Currencies.ContainsKey(CurrencyType.RebuildToken))
                    dbPlayer.Currencies[CurrencyType.RebuildToken] = 0;

                dbPlayer.Currencies[CurrencyType.RebuildToken]++;

                DB.Set(dbPlayer);
            }
        }

        protected void RefundPerksByMapping(Dictionary<(PerkType, int), int> refundMap)
        {
            var dbQuery = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(dbQuery);

            var dbPlayers = DB.Search(dbQuery
                .AddPaging(playerCount, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                var refundAmount = 0;

                // Calculate the refund amount first.
                foreach (var ((type, level), sp) in refundMap)
                {
                    if (dbPlayer.Perks.ContainsKey(type) && dbPlayer.Perks[type] >= level)
                    {
                        refundAmount += sp;
                    }
                }

                // Then remove the perks being refunded.
                foreach (var ((type, _), _) in refundMap)
                {
                    if (dbPlayer.Perks.ContainsKey(type))
                    {
                        dbPlayer.Perks.Remove(type);
                    }
                }

                if (refundAmount > 0)
                {
                    dbPlayer.UnallocatedSP += refundAmount;

                    _logger.Write<MigrationLogGroup>($"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP.");

                    DB.Set(dbPlayer);
                }
            }
        }

        protected void RecalculateAllShipStats()
        {
            uint GetTempStorage()
            {
                return GetObjectByTag("TEMP_ITEM_STORAGE");
            }

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

                    if (dbShip.Status.ConfigurationModules.ContainsKey(slot))
                    {
                        var tempStorage = GetTempStorage();
                        var oldItem = ObjectPlugin.Deserialize(dbShip.Status.ConfigurationModules[slot].SerializedItem);
                        var resref = GetResRef(oldItem);
                        var moduleBonus = Space.GetModuleBonus(oldItem);

                        var newItem = CreateItemOnObject(resref, tempStorage);
                        var newItemTag = GetTag(newItem);
                        var moduleDetails = Space.GetShipModuleDetailByItemTag(newItemTag);

                        if (moduleBonus > 0)
                        {
                            BiowareXP2.IPSafeAddItemProperty(newItem, ItemPropertyCustom(ItemPropertyType.ModuleBonus, -1, moduleBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                        }

                        dbShip.Status.ConfigurationModules[slot].ItemInstanceId = GetObjectUUID(newItem);
                        dbShip.Status.ConfigurationModules[slot].ItemTag = newItemTag;
                        dbShip.Status.ConfigurationModules[slot].SerializedItem = ObjectPlugin.Serialize(newItem);

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
