using System;
using System.Collections.Generic;
using System.IO;

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
            var recipes = new Dictionary<int, List<string>>();

            foreach (var line in inputLines)
            {
                var data = line.Split('\t');
                if (string.IsNullOrWhiteSpace(data[0]))
                    continue;

                var skill = data[0].Trim();
                var requiresRecipe = data[1].Trim();
                var recipeName = data[6].Trim();
                var categoryEnumName = data[2].Trim().Replace("-", "").Replace(" ", "");
                var recipeEnumName = data[3].Trim();
                var recipeCategory = data[4].Trim();
                var enhancementCategory = data[10].Trim();
                var resref = data[9].Trim();
                var level = data[7].Trim();
                var quantity = data[8].Trim();
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
                var component5Resref = data[20].Trim();
                var component5Quantity = data[21].Trim();
                var component6Resref = data[22].Trim();
                var component6Quantity = data[23].Trim();
                var component7Resref = data[24].Trim();
                var component7Quantity = data[25].Trim();
                var component8Resref = data[26].Trim();
                var component8Quantity = data[27].Trim();

                var recipeCode = recipeTemplate
                    .Replace("%%RECIPESKILL%%", skill)
                    .Replace("%%RECIPENAME%%", recipeName)
                    .Replace("%%CATEGORYENUMNAME%%", categoryEnumName)
                    .Replace("%%RECIPEENUMNAME%%", recipeEnumName)
                    .Replace("%%RESREF%%", resref)
                    .Replace("%%LEVEL%%", level)
                    .Replace("%%PERKLEVEL%%", perkLevel)
                    .Replace("%%RECIPECATEGORY%%", recipeCategory)
                    .Replace("%%QUANTITY%%", quantity);

                var recipeRequirement = string.Empty;
                if (!string.IsNullOrWhiteSpace(requiresRecipe))
                {
                    recipeRequirement = $"{Environment.NewLine}\t.RequirementUnlocked()";
                }

                recipeCode = recipeCode.Replace("%%REQUIRESRECIPE%%", recipeRequirement);

                var enhancements = string.Empty;
                if (!string.IsNullOrWhiteSpace(enhancementSlots) && 
                    !string.IsNullOrWhiteSpace(enhancementCategory) &&
                    enhancementCategory != "N/A")
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
                if (!string.IsNullOrWhiteSpace(component5Resref) && !string.IsNullOrWhiteSpace(component5Quantity))
                {
                    components += componentTemplate
                        .Replace("%%COMPONENTRESREF%%", component5Resref)
                        .Replace("%%COMPONENTQUANTITY%%", component5Quantity);
                }
                if (!string.IsNullOrWhiteSpace(component6Resref) && !string.IsNullOrWhiteSpace(component6Quantity))
                {
                    components += componentTemplate
                        .Replace("%%COMPONENTRESREF%%", component6Resref)
                        .Replace("%%COMPONENTQUANTITY%%", component6Quantity);
                }
                if (!string.IsNullOrWhiteSpace(component7Resref) && !string.IsNullOrWhiteSpace(component7Quantity))
                {
                    components += componentTemplate
                        .Replace("%%COMPONENTRESREF%%", component7Resref)
                        .Replace("%%COMPONENTQUANTITY%%", component7Quantity);
                }
                if (!string.IsNullOrWhiteSpace(component8Resref) && !string.IsNullOrWhiteSpace(component8Quantity))
                {
                    components += componentTemplate
                        .Replace("%%COMPONENTRESREF%%", component8Resref)
                        .Replace("%%COMPONENTQUANTITY%%", component8Quantity);
                }

                recipeCode = recipeCode.Replace("%%COMPONENTS%%", components);

                recipeCode += Environment.NewLine;
                recipeCode += Environment.NewLine;

                var tier = Convert.ToInt32(perkLevel);
                if (!recipes.ContainsKey(tier))
                    recipes[tier] = new List<string>();

                recipes[tier].Add(recipeCode);
            }

            var output = string.Empty;
            foreach (var (tier, textList) in recipes)
            {
                output += $"private void Tier{tier}()" + Environment.NewLine + "{" + Environment.NewLine + "\t";

                foreach (var text in textList)
                {
                    output += text;
                }

                output += Environment.NewLine + "}";
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
