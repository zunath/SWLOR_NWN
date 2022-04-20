﻿using Microsoft.Extensions.CommandLineUtils;

namespace SWLOR.CLI
{
    internal class Program
    {
        private static readonly HakBuilder _hakBuilder = new();
        private static readonly PlaceableBuilder _placeableBuilder = new();
        private static readonly LanguageBuilder _languageBuilder = new();
        private static readonly ModulePacker _modulePacker = new();
        private static readonly StructureItemCreator _structureItemCreator = new();

        static void Main(string[] args)
        {
            var app = new CommandLineApplication();

            // Set up the options.
            var placeableOption = app.Option(
                "-$|-c |--placeable",
                "Generates uti files in json format for all of the entries found in placeables.2da.",
                CommandOptionType.NoValue
            );

            var hakBuilderOption = app.Option(
                "-$|-k |--hak",
                "Builds hakpak files based on the hakbuilder.json configuration file.",
                CommandOptionType.NoValue
            );

            var languageBuilderOption = app.Option(
                "-$|-l |--language",
                "Generates code for use with the language system.",
                CommandOptionType.NoValue
            );

            var modulePackerOption = app.Option(
                "-$|-p |--pack",
                "Packs a module at the specified path. Target must be the path to a .mod file.",
                CommandOptionType.SingleValue
            );

            var moduleUnpackOption = app.Option(
                "-$|-u |--unpack",
                "Unpacks a module within the running directory. Target must be the path to a .mod file.",
                CommandOptionType.SingleValue
            );

            var structureOption = app.Option(
                "-$|-s |--structure",
                "Generates uti files in json format for all of the StructureType.cs enum values.",
                CommandOptionType.NoValue);

            app.HelpOption("-? | -h | --help");

            app.OnExecute(() =>
            {
                if (placeableOption.HasValue())
                {
                    _placeableBuilder.Process();
                }

                if (hakBuilderOption.HasValue())
                {
                    _hakBuilder.Process();
                }

                if (languageBuilderOption.HasValue())
                {
                    _languageBuilder.Process();
                }

                if (modulePackerOption.HasValue())
                {
                    _modulePacker.PackModule(modulePackerOption.Value());
                }

                if (moduleUnpackOption.HasValue())
                {
                    _modulePacker.UnpackModule(moduleUnpackOption.Value());
                }

                if (structureOption.HasValue())
                {
                    _structureItemCreator.Process();
                }

                return 0;
            });

            app.Execute(args);
        }
    }
}