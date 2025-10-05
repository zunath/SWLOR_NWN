using McMaster.Extensions.CommandLineUtils;

namespace SWLOR.App.CLI
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

        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "SWLOR CLI";
            app.Description = "Command line tools for SWLOR development";
            app.HelpOption("-?|-h|--help");

            var adHocOption = app.Option("-a|--adhoc", "Ad-hoc code testing.", CommandOptionType.NoValue);
            var beastOption = app.Option("-b|--beast", "Beast code generator.", CommandOptionType.NoValue);
            var placeableOption = app.Option("-c|--placeable", "Generates utp files in json format for all of the entries found in placeables.2da.", CommandOptionType.NoValue);
            var droidOption = app.Option("-d|--droid", "Generates uti files in json format for all of the entries found in droid_item_template.tsv.", CommandOptionType.NoValue);
            var enhancementOption = app.Option("-e|--enhancement", "Generates uti files in json format for all of the entries found in enhancement_list.csv.", CommandOptionType.NoValue);
            var hakOption = app.Option("-k|--hak", "Builds hakpak files based on the hakbuilder.json configuration file.", CommandOptionType.NoValue);
            var languageOption = app.Option("-l|--language", "Generates code for use with the language system.", CommandOptionType.NoValue);
            var deployOption = app.Option("-o|--outputDeploy", "Deploys DLLs in the bin folder to the NWN dotnet directory.", CommandOptionType.NoValue);
            var packOption = app.Option("-p|--pack <PATH>", "Packs a module at the specified path. Target must be the path to a .mod file.", CommandOptionType.SingleValue);
            var recipeOption = app.Option("-r|--recipe", "Generates code file for all of the recipes in the recipes.tsv file.", CommandOptionType.NoValue);
            var structureOption = app.Option("-s|--structure", "Generates uti files in json format for all of the StructureType.cs enum values.", CommandOptionType.NoValue);
            var unpackOption = app.Option("-u|--unpack <PATH>", "Unpacks a module within the running directory. Target must be the path to a .mod file.", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (placeableOption.HasValue())
                {
                    _placeableBuilder.Process();
                }

                if (enhancementOption.HasValue())
                {
                    _enhancementItemBuilder.Process();
                }

                if (droidOption.HasValue())
                {
                    _droidItemBuilder.Process();
                }

                if (hakOption.HasValue())
                {
                    _hakBuilder.Process();
                }

                if (languageOption.HasValue())
                {
                    _languageBuilder.Process();
                }

                if (packOption.HasValue())
                {
                    _modulePacker.PackModule(packOption.Value());
                }

                if (unpackOption.HasValue())
                {
                    _modulePacker.UnpackModule(unpackOption.Value());
                }

                if (recipeOption.HasValue())
                {
                    _recipeCodeBuilder.Process();
                }

                if (structureOption.HasValue())
                {
                    _structureItemCreator.Process();
                }

                if (adHocOption.HasValue())
                {
                    _adHocTool.Process();
                }

                if (deployOption.HasValue())
                {
                    _deployBuild.Process();
                }

                if (beastOption.HasValue())
                {
                    _beastBuilder.Process();
                }

                return 0;
            });

            return app.Execute(args);
        }
    }
}