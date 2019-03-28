using FluentValidation.Results;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Processor;
using SWLOR.Game.Server.Extension;

using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;

namespace SWLOR.Game.Server.Service
{
    public static class DataPackageService
    {
        const string PackagesPath = "./DataPackages/";
        private static readonly Queue<DatabaseAction> _queuedDBChanges;

        static DataPackageService()
        {
            _queuedDBChanges = new Queue<DatabaseAction>();
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        private static void OnModuleLoad()
        {
            // Look for an existing DataPackages folder. If it's missing, create it.
            if (!Directory.Exists(PackagesPath))
            {
                Directory.CreateDirectory(PackagesPath);
            }

            // Enumerate all packages in the directory.
            // We will process these later in order by their export date.
            // In other words, processing occurs from oldest to newest.
            var packages = BuildPackageList();

            foreach (var package in packages)
            {
                try
                {
                    package.Content = File.ReadAllText(PackagesPath + package.FileName);
                    DataPackageFile dpf = JsonConvert.DeserializeObject<DataPackageFile>(package.Content);
                    package.DateExported = dpf.ExportDate;
                    package.PackageName = dpf.PackageName;

                    string processingErrors = ProcessDataPackageFile(dpf);

                    if (!string.IsNullOrWhiteSpace(processingErrors))
                    {
                        package.ErrorMessage = processingErrors;
                    }
                    else
                    {
                        package.ImportedSuccessfully = true;
                    }
                }
                catch (Exception ex)
                {
                    package.ErrorMessage = ex.ToMessageAndCompleteStacktrace();
                    package.ImportedSuccessfully = false;
                }

                DataService.SubmitDataChange(package, DatabaseActionType.Insert);
                
                if (package.ImportedSuccessfully)
                {
                    Console.WriteLine("Processed package " + package.PackageName + " successfully.");
                }
                else
                {
                    File.WriteAllText(PackagesPath + "IMPORT_FAILURE_" + package.FileName + "_" + DateTime.UtcNow.ToString("yyyy-dd-M--HH-mm-ss") + ".log", package.ErrorMessage);
                    Console.WriteLine("FAILURE: Package " + package.PackageName + " failed to import. Check the logs for errors.");
                }
            }
        }

        private static List<DataPackage> BuildPackageList()
        {
            // Pull back all of the packages we've already attempted to import.
            var importedPackages = DataService.GetAll<DataPackage>();

            List<DataPackage> packages = new List<DataPackage>();
            string[] files = Directory.GetFiles(PackagesPath, "*.json");
            foreach (var file in files)
            {
                string checksum;
                using (var sha1 = SHA1.Create())
                {
                    using (var stream = File.OpenRead(file))
                    {
                        var hash = sha1.ComputeHash(stream);
                        checksum = BitConverter.ToString(hash).Replace("-", "").ToLower();
                    }
                }

                // If a file with this checksum has already been imported, move to the next one.
                if (importedPackages.FirstOrDefault(x => x.Checksum == checksum) != null)
                    continue;

                DataPackage package = new DataPackage
                {
                    Checksum = checksum,
                    DateFound = DateTime.UtcNow,
                    FileName = Path.GetFileName(file),
                    ErrorMessage = string.Empty, 
                    PackageName = string.Empty, 
                    Content = string.Empty
                };

                packages.Add(package);
            }

            return packages.OrderBy(o => o.DateExported).ToList();
        }

        private static string ProcessDataPackageFile(DataPackageFile dpf)
        {
            string errors = string.Empty;

            foreach (var obj in dpf.ApartmentBuildings)
                errors += ValidateAndProcess(new ApartmentBuildingProcessor(), obj) + "\n";
            foreach (var obj in dpf.BaseStructures)
                errors += ValidateAndProcess(new BaseStructureProcessor(), obj) + "\n";
            foreach (var obj in dpf.BuildingStyles)
                errors += ValidateAndProcess(new BuildingStyleProcessor(), obj) + "\n";
            foreach (var obj in dpf.CooldownCategories)
                errors += ValidateAndProcess(new CooldownCategoryProcessor(), obj) + "\n";
            foreach (var obj in dpf.CraftBlueprintCategories)
                errors += ValidateAndProcess(new CraftBlueprintCategoryProcessor(), obj) + "\n";
            foreach (var obj in dpf.CraftBlueprints)
                errors += ValidateAndProcess(new CraftBlueprintProcessor(), obj) + "\n";
            foreach (var obj in dpf.CraftDevices)
                errors += ValidateAndProcess(new CraftDeviceProcessor(), obj) + "\n";
            foreach (var obj in dpf.CustomEffects)
                errors += ValidateAndProcess(new CustomEffectProcessor(), obj) + "\n";
            foreach (var obj in dpf.Downloads)
                errors += ValidateAndProcess(new DownloadProcessor(), obj) + "\n";
            foreach (var obj in dpf.FameRegions)
                errors += ValidateAndProcess(new FameRegionProcessor(), obj) + "\n";
            foreach (var obj in dpf.GameTopicCategories)
                errors += ValidateAndProcess(new GameTopicCategoryProcessor(), obj) + "\n";
            foreach (var obj in dpf.GameTopics)
                errors += ValidateAndProcess(new GameTopicProcessor(), obj) + "\n";
            foreach (var obj in dpf.KeyItemCategories)
                errors += ValidateAndProcess(new KeyItemCategoryProcessor(), obj) + "\n";
            foreach (var obj in dpf.KeyItems)
                errors += ValidateAndProcess(new KeyItemProcessor(), obj) + "\n";
            //foreach (var obj in dpf.LootTableItems)
            //    errors += ValidateAndProcess(new LootTableItemProcessor(), obj) + "\n";
            //foreach (var obj in dpf.LootTables)
            //    errors += ValidateAndProcess(new LootTableProcessor(), obj) + "\n";
            foreach (var obj in dpf.NPCGroups)
                errors += ValidateAndProcess(new NPCGroupProcessor(), obj) + "\n";
            foreach (var obj in dpf.PerkCategories)
                errors += ValidateAndProcess(new PerkCategoryProcessor(), obj) + "\n";
            foreach (var obj in dpf.Plants)
                errors += ValidateAndProcess(new PlantProcessor(), obj) + "\n";
            foreach (var obj in dpf.Quests)
                errors += ValidateAndProcess(new QuestProcessor(), obj) + "\n";
            foreach (var obj in dpf.SkillCategories)
                errors += ValidateAndProcess(new SkillCategoryProcessor(), obj) + "\n";
            foreach (var obj in dpf.Skills)
                errors += ValidateAndProcess(new SkillProcessor(), obj) + "\n";
            foreach (var obj in dpf.Spawns)
                errors += ValidateAndProcess(new SpawnProcessor(), obj) + "\n";

            // Nothing in the package gets committed to the database if any error occurs.
            if (string.IsNullOrWhiteSpace(errors))
            {
                while (_queuedDBChanges.Count > 0)
                {
                    var change = _queuedDBChanges.Dequeue();
                    DataService.SubmitDataChange(change);
                }
            }
            return errors;
        }

        private static string ValidateAndProcess<T>(IDataProcessor<T> processor, JObject dataObject)
        {
            string errors = string.Empty;

            ValidationResult validationResult = processor.Validator.Validate(dataObject);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    errors += error.ErrorMessage + "\n";
                }
            }
            else
            {
                try
                {
                    var result = processor.Process(dataObject);

                    if (result == null)
                    {
                        errors += "Failed to process object of type: " + dataObject.GetType() + " Reason: Processor failed to return valid DatabaseAction object";
                    }
                    else
                    {
                        _queuedDBChanges.Enqueue(result);
                    }
                }
                catch (Exception ex)
                {
                    errors += "Failed to process object of type: " + dataObject.GetType() + " Reason: " + ex.ToMessageAndCompleteStacktrace();
                }
            };

            return errors;
        }

    }
}
