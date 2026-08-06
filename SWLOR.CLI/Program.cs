using System;
using System.CommandLine;
using System.Linq;

namespace SWLOR.CLI
{
    internal class Program
    {
        private static readonly HakBuilder _hakBuilder = new();
        private static readonly PlaceableBuilder _placeableBuilder = new();
        private static readonly LanguageBuilder _languageBuilder = new();
        private static readonly ModulePacker _modulePacker = new();
        private static readonly StructureItemCreator _structureItemCreator = new();
        private static readonly EnhancementItemBuilder _enhancementItemBuilder = new();
        private static readonly RecipeCodeBuilder _recipeCodeBuilder = new();
        private static readonly AdHocTool _adHocTool = new();
        private static readonly DroidItemBuilder _droidItemBuilder = new();
        private static readonly DeployBuild _deployBuild = new();
        private static readonly BeastCodeBuilder _beastBuilder = new();
        private static readonly StoreInstanceSync _storeInstanceSync = new();

        static int Main(string[] args)
        {
            var adHocToolOption = CreateFlagOption("--adhoc", "-a", "Ad-hoc code testing.");
            var beastBuilderOption = CreateFlagOption("--beast", "-b", "Beast code generator.");
            var placeableOption = CreateFlagOption(
                "--placeable",
                "-c",
                "Generates utp files in json format for all of the entries found in placeables.2da.");
            var droidItemOption = CreateFlagOption(
                "--droid",
                "-d",
                "Generates uti files in json format for all of the entries found in droid_item_template.tsv.");
            var enhancementOption = CreateFlagOption(
                "--enhancement",
                "-e",
                "Generates uti files in json format for all of the entries found in enhancement_list.tsv.");
            var hakBuilderOption = CreateFlagOption(
                "--hak",
                "-k",
                "Builds hakpak files based on the hakbuilder.json configuration file.");
            var languageBuilderOption = CreateFlagOption(
                "--language",
                "-l",
                "Generates code for use with the language system.");
            var deployOption = CreateFlagOption(
                "--outputDeploy",
                "-o",
                "Deploys DLLs in the bin folder to the NWN dotnet directory.");
            var modulePackerOption = CreateValueOption(
                "--pack",
                "-p",
                "Packs a module at the specified path. Target must be the path to a .mod file.");
            var recipeOption = CreateFlagOption(
                "--recipe",
                "-r",
                "Generates code file for all of the recipes in the recipes.tsv file.");
            var structureOption = CreateFlagOption(
                "--structure",
                "-s",
                "Generates uti files in json format for all of the StructureType.cs enum values.");
            var moduleUnpackOption = CreateValueOption(
                "--unpack",
                "-u",
                "Unpacks a module within the running directory. Target must be the path to a .mod file.");
            var checkStoreInstancesOption = new Option<bool>("--checkStoreInstances")
            {
                Description = "Checks placed store instances against UTM/UTI blueprints without writing changes."
            };
            var syncStoreInstancesOption = new Option<bool>("--syncStoreInstances")
            {
                Description = "Syncs placed store instances against UTM/UTI blueprints."
            };
            var createMissingStoreBlueprintsOption = new Option<bool>("--createMissingStoreBlueprints")
            {
                Description = "Creates UTM blueprints for placed store instances that have no source UTM."
            };
            var storeModuleRootOption = new Option<string>("--storeModuleRoot")
            {
                Description = "Module root containing git, uti, and utm folders. Defaults to ./Module."
            };
            var noPromptOption = new Option<bool>("--no-prompt")
            {
                Description = "Skips the 'Press any key to end' prompt after packing or unpacking a module."
            };

            var rootCommand = new RootCommand("SWLOR build and content tools.");
            rootCommand.Options.Add(adHocToolOption);
            rootCommand.Options.Add(beastBuilderOption);
            rootCommand.Options.Add(placeableOption);
            rootCommand.Options.Add(droidItemOption);
            rootCommand.Options.Add(enhancementOption);
            rootCommand.Options.Add(hakBuilderOption);
            rootCommand.Options.Add(languageBuilderOption);
            rootCommand.Options.Add(deployOption);
            rootCommand.Options.Add(modulePackerOption);
            rootCommand.Options.Add(recipeOption);
            rootCommand.Options.Add(structureOption);
            rootCommand.Options.Add(moduleUnpackOption);
            rootCommand.Options.Add(checkStoreInstancesOption);
            rootCommand.Options.Add(syncStoreInstancesOption);
            rootCommand.Options.Add(createMissingStoreBlueprintsOption);
            rootCommand.Options.Add(storeModuleRootOption);
            rootCommand.Options.Add(noPromptOption);

            rootCommand.SetAction(parseResult =>
            {
                if (parseResult.GetValue(placeableOption))
                {
                    _placeableBuilder.Process();
                }

                if (parseResult.GetValue(enhancementOption))
                {
                    _enhancementItemBuilder.Process();
                }

                if (parseResult.GetValue(droidItemOption))
                {
                    _droidItemBuilder.Process();
                }

                if (parseResult.GetValue(hakBuilderOption))
                {
                    _hakBuilder.Process();
                }

                if (parseResult.GetValue(languageBuilderOption))
                {
                    _languageBuilder.Process();
                }

                var modulePath = parseResult.GetValue(modulePackerOption);
                if (!string.IsNullOrWhiteSpace(modulePath))
                {
                    _modulePacker.PackModule(modulePath, parseResult.GetValue(noPromptOption));
                }

                var unpackPath = parseResult.GetValue(moduleUnpackOption);
                if (!string.IsNullOrWhiteSpace(unpackPath))
                {
                    _modulePacker.UnpackModule(unpackPath, parseResult.GetValue(noPromptOption));
                }

                if (parseResult.GetValue(recipeOption))
                {
                    _recipeCodeBuilder.Process();
                }

                if (parseResult.GetValue(structureOption))
                {
                    _structureItemCreator.Process();
                }

                if (parseResult.GetValue(adHocToolOption))
                {
                    _adHocTool.Process();
                }

                if (parseResult.GetValue(deployOption))
                {
                    _deployBuild.Process();
                }

                if (parseResult.GetValue(beastBuilderOption))
                {
                    _beastBuilder.Process();
                }

                var storeModeCount = new[]
                {
                    parseResult.GetValue(checkStoreInstancesOption),
                    parseResult.GetValue(syncStoreInstancesOption),
                    parseResult.GetValue(createMissingStoreBlueprintsOption),
                }.Count(value => value);

                if (storeModeCount > 1)
                {
                    Console.Error.WriteLine("Use only one store instance mode at a time.");
                    return 1;
                }

                if (storeModeCount > 0)
                {
                    var moduleRoot = parseResult.GetValue(storeModuleRootOption) ?? "./Module";
                    var checkStoreInstances = parseResult.GetValue(checkStoreInstancesOption);
                    var hasDrift = _storeInstanceSync.Process(
                        moduleRoot,
                        checkStoreInstances,
                        parseResult.GetValue(createMissingStoreBlueprintsOption));

                    if (checkStoreInstances && hasDrift)
                        return 1;
                }

                return 0;
            });

            return rootCommand.Parse(args).Invoke();
        }

        private static Option<bool> CreateFlagOption(string name, string alias, string description)
        {
            var option = new Option<bool>(name)
            {
                Description = description
            };
            option.Aliases.Add(alias);
            return option;
        }

        private static Option<string> CreateValueOption(string name, string alias, string description)
        {
            var option = new Option<string>(name)
            {
                Description = description
            };
            option.Aliases.Add(alias);
            return option;
        }
    }
}
