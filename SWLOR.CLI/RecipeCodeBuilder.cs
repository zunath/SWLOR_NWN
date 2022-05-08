using System;
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

            var recipeTemplate = File.ReadAllText(Template);
            var inputLines = File.ReadAllLines(InputData);
            var output = string.Empty;

            foreach (var line in inputLines)
            {
                var data = line.Split('\t');
                if (string.IsNullOrWhiteSpace(data[0]))
                    continue;


                var recipeName = data[6].Trim();
                var categoryEnumName = data[4].Trim();
                var recipeEnumName = data[3].Trim();
                var resref = data[9].Trim();
                var level = data[7].Trim();
                var perkLevel = data[5].Trim();
                var modSlots = data[11].Trim();
                var component1Resref = data[12].Trim();
                var component1Quantity = data[13].Trim();
                var component2Resref = data[14].Trim();
                var component2Quantity = data[15].Trim();
                var component3Resref = data[16].Trim();
                var component3Quantity = data[17].Trim();
                var component4Resref = data[18].Trim();
                var component4Quantity = data[19].Trim();

                var recipeCode = recipeTemplate
                    .Replace("%%RECIPENAME%%", recipeName)
                    .Replace("%%CATEGORYENUMNAME%%", categoryEnumName)
                    .Replace("%%RECIPEENUMNAME%%", recipeEnumName)
                    .Replace("%%RESREF%%", resref)
                    .Replace("%%LEVEL%%", level)
                    .Replace("%%PERKLEVEL%%", perkLevel)
                    .Replace("%%MODSLOTS%%", modSlots)
                    .Replace("%%COMPONENT1RESREF%%", component1Resref)
                    .Replace("%%COMPONENT1QUANTITY%%", component1Quantity)
                    .Replace("%%COMPONENT2RESREF%%", component2Resref)
                    .Replace("%%COMPONENT2QUANTITY%%", component2Quantity)
                    .Replace("%%COMPONENT3RESREF%%", component3Resref)
                    .Replace("%%COMPONENT3QUANTITY%%", component3Quantity)
                    .Replace("%%COMPONENT4RESREF%%", component4Resref)
                    .Replace("%%COMPONENT4QUANTITY%%", component4Quantity);

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
