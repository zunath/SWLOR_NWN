using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.CLI
{
    internal class EnhancementItemBuilder
    {
        private const string InputData = "./InputFiles/enhancement_list.tsv";
        private const string Template = "./Templates/enhancement_template.json";
        private const string OutputFolder = "./OutputEnhancements/";

        private readonly int[] _iconIds = { 1, 2, 3, 4, 5, 6, 9, 10, 12, 13, 14, 15, 16, 17, 20, 22, 23, 24, 25, 26, 27, 30, 31, 41, 42, 
            44, 45, 46, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 
            92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 160, 175, 176, 177, 
            178, 179, 180, 181, 182, 183, 187, 188, 189, 190, 191, 194, 196, 197, 198, 217, 218, 219, 220, 227, 228, 229, 230,
            231, 232, 233, 234, 236, 237, 238, 239, 244, 245, 246, 247, 248, 250, 254};

        private readonly Dictionary<string, int> _categoryNameToId = new()
        {
            { "Armor", 101 },
            { "Weapon", 102 },
            { "Structure", 107 },
            { "Cooking", 108 },
            { "Starship", 109 },
            { "Module", 110 },
        };

        private readonly Dictionary<string, int> _subTypeToId = new()
        {
            { "Control - Smithery", 14},
            { "Control - Engineering", 39 },
            { "Control - Fabrication", 41 },
            { "Control - Agriculture", 43 },
            { "Craftsmanship - Smithery", 15 },
            { "Craftsmanship - Engineering", 40 },
            { "Craftsmanship - Fabrication", 42 },
            { "Craftsmanship - Agriculture", 44 },
            { "Defense - Physical", 1 },
            { "Defense - Force", 2 },
            { "Defense - Poison", 4 },
            { "Defense - Fire", 3 },
            { "Defense - Ice", 6 },
            { "Defense - Electrical", 5 },
            { "Evasion", 7 },
            { "FP", 9 },
            { "HP", 8 },
            { "Recast Reduction", 27 },
            { "STM", 10 },
            { "Accuracy", 26 },
            { "DMG - Physical", 18 },
            { "DMG - Force", 19 },
            { "DMG - Poison", 21 },
            { "DMG - Fire", 20 },
            { "DMG - Ice", 23 },
            { "DMG - Electrical", 22 },
            { "Structure Bonus", 28 },
            { "Duration", 35 },
            { "FP Food", 37 },
            { "FP Regen", 30 },
            { "HP Food", 36 },
            { "HP Regen", 29 },
            { "Recast Reduction Food", 34 },
            { "Rest Regen", 32 },
            { "STM Food", 38 },
            { "STM Regen", 31 },
            { "XP Bonus", 33 },
            { "Accuracy", 53 },
            { "Armor", 46 },
            { "Capacitor", 47 },
            { "EM Damage", 50 },
            { "EM Defense", 57 },
            { "Evasion Starship", 54 },
            { "Explosive Damage", 52 },
            { "Explosive Defense", 56 },
            { "Shield", 48 },
            { "Shield Recharge Rate", 49 },
            { "Thermal Damage", 51 },
            { "Thermal Defense", 55 },
            { "Agility", 58 },
            { "Attack", 83 },
            { "Force Attack", 84 }
        };

        private readonly Dictionary<int, int> _levelToTier = new()
        {
            { 5, 1 },
            { 15, 2 },
            { 25, 3 },
            { 35, 4 },
            { 45, 5 },
        };

        public void Process()
        {
            ClearOutputDirectory();

            var input2daText = File.ReadAllLines(InputData).ToList();
            var templateText = File.ReadAllText(Template);

            foreach (var line in input2daText)
            {
                var parsed = line.Split('\t');
                var category = parsed[0].Trim();
                var name = parsed[1].Trim();
                var resref = parsed[2].Trim();
                var level = parsed[3].Trim();
                var progressPenalty = parsed[5].Trim();
                var propertyName = parsed[6].Trim();
                var bonus = parsed[7].Trim();
                var iconId = _iconIds[Random.Next(_iconIds.Length - 1)];
                var itemPropertyId = _categoryNameToId[category];
                var subTypeId = _subTypeToId[propertyName];
                var tier = _levelToTier[Convert.ToInt32(level)];
                var price = CalculatePrice(tier);

                var json = templateText
                    .Replace("%%NAME%%", name)
                    .Replace("%%ICONID%%", iconId.ToString())
                    .Replace("%%ENHANCEMENTLEVEL%%", level)
                    .Replace("%%PROGRESSPENALTY%%", progressPenalty)
                    .Replace("%%ITEMPROPERTYID%%", itemPropertyId.ToString())
                    .Replace("%%SUBTYPEID%%", subTypeId.ToString())
                    .Replace("%%BONUSAMOUNT%%", bonus)
                    .Replace("%%TAG%%", resref)
                    .Replace("%%RESREF%%", resref)
                    .Replace("%%PRICE%%", price.ToString());

                File.WriteAllText($"{OutputFolder}/{resref}.uti.json", json);
            }
        }

        private void ClearOutputDirectory()
        {
            if (Directory.Exists(OutputFolder))
            {
                Directory.Delete(OutputFolder, true);
            }

            Directory.CreateDirectory(OutputFolder);
        }

        private int CalculatePrice(int tier)
        {
            const int BasePrice = 250;

            return BasePrice * tier;
        }
    }
}
