using System;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class ModuleMigrationService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => MigrateModuleVersion());
        }

        private static void MigrateModuleVersion()
        {
            var config = DataService.ServerConfiguration.Get();
            NWPlaceable storage = _.GetObjectByTag("MIGRATION_STORAGE");

            // VERSION 1: Apply new AC rules to all items in persistent storage.
            if (config.ModuleVersion < 1)
            {
                // Loop through all persistent storage sources and run the process which converts AC.
                // Re-serialize it and submit a data change. Then delete the temporary item from temp storage.
                Console.WriteLine("Processing module migration #1. This may take a while.");

                // This is one rare scenario where we want to get data directly from the database, change it, and then write it synchronously.
                // This data is not loaded at boot time, but rather loaded and cached as players log in. So if we were to pull from the DataService's
                // cache, we wouldn't get any of the data as it hasn't loaded yet.
                // This procedure is slow but it only happens one time during the lifespan of the server and then it's finished.

                // BankItem
                foreach (var item in DataService.Connection.GetAll<BankItem>())
                {
                    NWItem deserialized = SerializationService.DeserializeItem(item.ItemObject, storage);
                    PlayerMigrationService.ProcessVersion6_DeflateItemStats(deserialized);
                    ProcessVersion1LightsaberItem(deserialized);
                    item.ItemObject = SerializationService.Serialize(deserialized);
                    DataService.Connection.Update(item);
                    deserialized.Destroy();
                }
                Console.WriteLine("Processed BankItem");

                // PCBaseStructureItem
                foreach (var item in DataService.Connection.GetAll<PCBaseStructureItem>())
                {
                    NWItem deserialized = SerializationService.DeserializeItem(item.ItemObject, storage);
                    PlayerMigrationService.ProcessVersion6_DeflateItemStats(deserialized);
                    ProcessVersion1LightsaberItem(deserialized);
                    item.ItemObject = SerializationService.Serialize(deserialized);
                    DataService.Connection.Update(item);
                    deserialized.Destroy();
                }
                Console.WriteLine("Processed PCBaseStructureItem");

                // PCImpoundedItem
                foreach (var item in DataService.PCImpoundedItem.GetAll())
                {
                    NWItem deserialized = SerializationService.DeserializeItem(item.ItemObject, storage);
                    PlayerMigrationService.ProcessVersion6_DeflateItemStats(deserialized);
                    ProcessVersion1LightsaberItem(deserialized);
                    item.ItemObject = SerializationService.Serialize(deserialized);
                    DataService.Connection.Update(item);
                    deserialized.Destroy();
                }
                Console.WriteLine("Processed PCImpoundedItem");

                // PCMarketListing
                foreach (var item in DataService.PCMarketListing.GetAll())
                {
                    NWItem deserialized = SerializationService.DeserializeItem(item.ItemObject, storage);
                    PlayerMigrationService.ProcessVersion6_DeflateItemStats(deserialized);
                    ProcessVersion1LightsaberItem(deserialized);
                    item.ItemObject = SerializationService.Serialize(deserialized);
                    DataService.Connection.Update(item);
                    deserialized.Destroy();
                }

                Console.WriteLine("Processed PCMarketListing");

                config.ModuleVersion = 1;
                Console.WriteLine("Module migration #1 complete.");
            }

            DataService.SubmitDataChange(config, DatabaseActionType.Update);
        }

        private static void ProcessVersion1LightsaberItem(NWItem item)
        {
            // Lightsaber appearances have changed because they're now considered training foils. 
            // Depending on the type of lightsaber, modify the name, appearance and other details of the item.
            int baseItemType;

            // Appearance strings were built by running NWNXItem.GetEntireItemAppearance on the new design.
            // Then the result was copied over to here. Unfortunately, setting each individual part's graphic didn't work
            // so I used this instead.
            string appearanceString;

            switch (item.Resref)
            {
                // Blue lightsabers
                case "lightsaber_b":
                case "lightsaber_1":
                case "lightsaber_2":
                case "lightsaber_3":
                case "lightsaber_4":
                    baseItemType = 1; // 1 = Longsword
                    appearanceString = "000000000000FBFBD300000000000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
                    break;

                // Red Lightsabers
                case "lightsaber_r_b":
                case "lightsaber_r_1":
                case "lightsaber_r_2":
                case "lightsaber_r_3":
                case "lightsaber_r_4":
                    baseItemType = 1; // 1 = Longsword
                    appearanceString = "000000000000FBFBD500000000000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
                    break;

                // Green Lightsabers
                case "lightsaber_g_b":
                case "lightsaber_g_1":
                case "lightsaber_g_2":
                case "lightsaber_g_3":
                case "lightsaber_g_4":
                    baseItemType = 1; // 1 = Longsword
                    appearanceString = "000000000000FBFBD400000000000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
                    break;

                // Yellow Lightsabers
                case "lightsaber_y_b":
                case "lightsaber_y_1":
                case "lightsaber_y_2":
                case "lightsaber_y_3":
                case "lightsaber_y_4":
                    baseItemType = 1; // 1 = Longsword
                    appearanceString = "000000000000FBFBB500000000000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
                    // Also give a VFX item property for yellow sabers.
                    var ip = _.ItemPropertyVisualEffect(_.ITEM_VISUAL_HOLY);
                    BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                    break;

                default: return;
            }


            NWNXItem.SetBaseItemType(item, baseItemType);
            NWNXItem.RestoreItemAppearance(item, appearanceString);

            item.SetLocalInt("LIGHTSABER", _.TRUE);
            item.Name = item.Name.Replace("Lightsaber", "Training Foil").Replace("Saberstaff", "Training Foil Staff");
        }

    }
}
