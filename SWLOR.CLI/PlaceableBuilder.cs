using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SWLOR.CLI
{
    internal class PlaceableBuilder
    {
        private const string Input2da = "./InputFiles/placeables.2da";
        private const string Template = "./Templates/placeable_template.json";
        private const string OutputFolder = "./OutputPlaceables/";

        private readonly Dictionary<string, int> _categoryNameToId = new()
        {
            { "Starships:", 26 },
            { "Structure:", 27 },
            { "Battlefield:", 28 },
            { "Furniture:", 29 },
            { "Celestial:", 30 },
            { "Food:", 31 },
            { "Lighting:", 32 },
            { "Vehicles:", 33 },
            { "WallCovering:", 34 },
            { "Building:", 35 },
            { "Sign:", 36 },
            { "Ceiling:", 37 },
            { "Vegetation:", 38 },
            { "Mech:", 39 },
            { "Wall:", 40 },
            { "GroundCovering:", 41 },
            { "Electronics:", 42 },
            { "Container:", 43 },
        };

        public void Process()
        {
            ClearOutputDirectory();

            var input2daText = File.ReadAllLines(Input2da).ToList();
            var templateText = File.ReadAllText(Template);

            // Throw away the first three lines (no data on them)
            input2daText.RemoveRange(0, 3);

            foreach (var line in input2daText)
            {
                var parsed = ParseString(line);
                var name = parsed[1].Replace("\"", "");
                var firstWord = name.Split(' ').FirstOrDefault();

                if (firstWord != null && _categoryNameToId.ContainsKey(firstWord))
                {
                    var nameWithoutCategory = name.Replace(firstWord, string.Empty);
                    var paletteId = _categoryNameToId[firstWord];
                    var resref = $"xm_plc_{parsed[0]}";
                    var json = templateText
                        .Replace("%%APPEARANCEID%%", parsed[0])
                        .Replace("%%NAME%%", nameWithoutCategory)
                        .Replace("%%TAG%%", resref)
                        .Replace("%%RESREF%%", resref)
                        .Replace("%%PALETTEID%%", paletteId.ToString());

                    File.WriteAllText($"{OutputFolder}/{resref}.utp.json", json);
                }
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

        private List<string> ParseString(string line)
        {
            return Regex.Matches(line, @"[\""].+?[\""]|[^ ]+")
                .Select(m => m.Value)
                .ToList();
        }

    }
}
