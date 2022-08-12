﻿using System;
using System.IO;
using System.Linq;
using SWLOR.Game.Server.Service;

namespace SWLOR.CLI
{
    internal class RecipeCodeBuilder
    {
        private const string InputData = "./InputFiles/recipes.tsv";

        // Note: Adjust the template before running.
        private const string Template = "./Templates/RecipeTemplate.txt";
        private const string OutputFolder = "./OutputRecipes/";

        public void Process()
        {
            ClearOutputDirectory();

            var componentTemplate = $"{Environment.NewLine}\t.Component(\"%%COMPONENTRESREF%%\", %%COMPONENTQUANTITY%%)";
            var enhancementTemplate = $"{Environment.NewLine}\t.EnhancementSlots(RecipeEnhancementType.%%ENHANCEMENTTYPE%%, %%ENHANCEMENTSLOTS%%)";

            var recipeTemplate = File.ReadAllText(Template);
            var inputLines = File.ReadAllLines(InputData);
            var output = string.Empty;

            foreach (var line in inputLines)
            {
                var data = line.Split('\t');
                if (string.IsNullOrWhiteSpace(data[0]))
                    continue;

                var skill = data[0].Trim();
                var recipeName = data[6].Trim();
                var categoryEnumName = data[2].Trim().Replace("-", "").Replace(" ", "");
                var recipeEnumName = data[3].Trim();
                var recipeCategory = data[4].Trim();
                var enhancementCategory = data[10].Trim();
                var resref = data[9].Trim();
                var level = data[7].Trim();
                var perkLevel = data[5].Trim();
                var enhancementSlots = data[11].Trim();
                var component1Resref = data[12].Trim();
                var component1Quantity = data[13].Trim();
                var component2Resref = data[14].Trim();
                var component2Quantity = data[15].Trim();
                var component3Resref = data[16].Trim();
                var component3Quantity = data[17].Trim();
                var component4Resref = data[18].Trim();
                var component4Quantity = data[19].Trim();

                var recipeCode = recipeTemplate
                    .Replace("%%RECIPESKILL%%", skill)
                    .Replace("%%RECIPENAME%%", recipeName)
                    .Replace("%%CATEGORYENUMNAME%%", categoryEnumName)
                    .Replace("%%RECIPEENUMNAME%%", recipeEnumName)
                    .Replace("%%RESREF%%", resref)
                    .Replace("%%LEVEL%%", level)
                    .Replace("%%PERKLEVEL%%", perkLevel)
                    .Replace("%%RECIPECATEGORY%%", recipeCategory);

                var enhancements = string.Empty;
                if (!string.IsNullOrWhiteSpace(enhancementSlots))
                {
                    enhancements = enhancementTemplate
                        .Replace("%%ENHANCEMENTSLOTS%%", enhancementSlots)
                        .Replace("%%ENHANCEMENTTYPE%%", enhancementCategory);
                }

                recipeCode = recipeCode.Replace("%%ENHANCEMENTSLOTS%%", enhancements);

                var components = string.Empty;
                if (!string.IsNullOrWhiteSpace(component1Resref) && !string.IsNullOrWhiteSpace(component1Quantity))
                {
                    components += componentTemplate
                        .Replace("%%COMPONENTRESREF%%", component1Resref)
                        .Replace("%%COMPONENTQUANTITY%%", component1Quantity);
                }
                if (!string.IsNullOrWhiteSpace(component2Resref) && !string.IsNullOrWhiteSpace(component2Quantity))
                {
                    components += componentTemplate
                        .Replace("%%COMPONENTRESREF%%", component2Resref)
                        .Replace("%%COMPONENTQUANTITY%%", component2Quantity);
                }
                if (!string.IsNullOrWhiteSpace(component3Resref) && !string.IsNullOrWhiteSpace(component3Quantity))
                {
                    components += componentTemplate
                        .Replace("%%COMPONENTRESREF%%", component3Resref)
                        .Replace("%%COMPONENTQUANTITY%%", component3Quantity);
                }
                if (!string.IsNullOrWhiteSpace(component4Resref) && !string.IsNullOrWhiteSpace(component4Quantity))
                {
                    components += componentTemplate
                        .Replace("%%COMPONENTRESREF%%", component4Resref)
                        .Replace("%%COMPONENTQUANTITY%%", component4Quantity);
                }

                recipeCode = recipeCode.Replace("%%COMPONENTS%%", components);

                recipeCode += Environment.NewLine;
                recipeCode += Environment.NewLine;

                output += recipeCode;
            }

            File.WriteAllText($"{OutputFolder}/Recipes.txt", output);
        }

        private void ClearOutputDirectory()
        {
            if (Directory.Exists(OutputFolder))
            {
                Directory.Delete(OutputFolder, true);
            }

            Directory.CreateDirectory(OutputFolder);
        }
    }
}
