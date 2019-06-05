using System;
using NWN;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;

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
            var config = DataService.Single<ServerConfiguration>(); // There should only ever be one row in the server configuration table.
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
                    PlayerMigrationService.ProcessVersion6RemoveACFromItem(deserialized);
                    item.ItemObject = SerializationService.Serialize(deserialized);
                    DataService.Connection.Update(item);
                    deserialized.Destroy();
                }
                Console.WriteLine("Processed BankItem");

                // PCBaseStructureItem
                foreach (var item in DataService.Connection.GetAll<PCBaseStructureItem>())
                {
                    NWItem deserialized = SerializationService.DeserializeItem(item.ItemObject, storage);
                    PlayerMigrationService.ProcessVersion6RemoveACFromItem(deserialized);
                    item.ItemObject = SerializationService.Serialize(deserialized);
                    DataService.Connection.Update(item);
                    deserialized.Destroy();
                }
                Console.WriteLine("Processed PCBaseStructureItem");

                // PCImpoundedItem
                foreach (var item in DataService.GetAll<PCImpoundedItem>())
                {
                    NWItem deserialized = SerializationService.DeserializeItem(item.ItemObject, storage);
                    PlayerMigrationService.ProcessVersion6RemoveACFromItem(deserialized);
                    item.ItemObject = SerializationService.Serialize(deserialized);
                    DataService.Connection.Update(item);
                    deserialized.Destroy();
                }
                Console.WriteLine("Processed PCImpoundedItem");

                // PCMarketListing
                foreach (var item in DataService.GetAll<PCMarketListing>())
                {
                    NWItem deserialized = SerializationService.DeserializeItem(item.ItemObject, storage);
                    PlayerMigrationService.ProcessVersion6RemoveACFromItem(deserialized);
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
    }
}
