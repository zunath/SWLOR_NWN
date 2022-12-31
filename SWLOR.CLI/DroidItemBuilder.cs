using System;
using System.IO;
using System.Linq;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.CLI
{
    internal class DroidItemBuilder
    {
        private const string InputData = "./InputFiles/droid_item_list.tsv";
        private const string Template = "./Templates/droid_item_template.json";
        private const string OutputFolder = "./OutputDroidItems/";

        private readonly int[] _iconIds = { 1, 2, 3, 4, 5, 6, 9, 10, 12, 13, 14, 15, 16, 17, 20, 22, 23, 24, 25, 26, 27, 30, 31, 41, 42,
            44, 45, 46, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91,
            92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 160, 175, 176, 177,
            178, 179, 180, 181, 182, 183, 187, 188, 189, 190, 191, 194, 196, 197, 198, 217, 218, 219, 220, 227, 228, 229, 230,
            231, 232, 233, 234, 236, 237, 238, 239, 244, 245, 246, 247, 248, 250, 254};

        public void Process()
        {
            ClearOutputDirectory();

            var input2daText = File.ReadAllLines(InputData).ToList();
            var templateText = File.ReadAllText(Template);

            foreach (var line in input2daText)
            {
                var parsed = line.Split('\t');
                var name = parsed[0].Trim();
                var resref = parsed[1].Trim();
                var slotType = GetSlotType(parsed[2].Trim());
                var tier = parsed[3].Trim();
                var aiSlots = parsed[4].Trim();
                var hp = parsed[6].Trim();
                var stm = parsed[7].Trim();
                var mgt = parsed[8].Trim();
                var per = parsed[9].Trim();
                var vit = parsed[10].Trim();
                var wil = parsed[11].Trim();
                var agi = parsed[12].Trim();
                var soc = parsed[13].Trim();
                var oneHanded = string.IsNullOrWhiteSpace(parsed[14]) ? 0 : Convert.ToInt32(parsed[14].Trim());
                var twoHanded = string.IsNullOrWhiteSpace(parsed[15]) ? 0 : Convert.ToInt32(parsed[15].Trim());
                var martialArts = string.IsNullOrWhiteSpace(parsed[16]) ? 0 : Convert.ToInt32(parsed[16].Trim());
                var ranged = string.IsNullOrWhiteSpace(parsed[17]) ? 0 : Convert.ToInt32(parsed[17].Trim());
                var iconId = _iconIds[Random.Next(_iconIds.Length - 1)];

                var (skill1Id, skill1Value, skill2Id, skill2Value) = GetSkillLevels(oneHanded, twoHanded, martialArts, ranged);

                var json = templateText
                    .Replace("%%NAME%%", name)
                    .Replace("%%ICONID%%", iconId.ToString())
                    .Replace("%%DROIDPARTTYPE%%", slotType.ToString())
                    .Replace("%%AGI%%", agi)
                    .Replace("%%VIT%%", vit)
                    .Replace("%%MGT%%", mgt)
                    .Replace("%%WIL%%", wil)
                    .Replace("%%SOC%%", soc)
                    .Replace("%%PER%%", per)
                    .Replace("%%AISLOTS%%", aiSlots)
                    .Replace("%%HP%%", hp)
                    .Replace("%%SKILL1VALUE%%", skill1Value.ToString())
                    .Replace("%%SKILL1%%", skill1Id.ToString())
                    .Replace("%%SKILL2VALUE%%", skill2Value.ToString())
                    .Replace("%%SKILL2%%", skill2Id.ToString())
                    .Replace("%%STM%%", stm)
                    .Replace("%%TIER%%", tier)

                    .Replace("%%TAG%%", resref)
                    .Replace("%%RESREF%%", resref);

                File.WriteAllText($"{OutputFolder}/{resref}.uti.json", json);
            }
        }

        private int GetSlotType(string type)
        {
            switch (type)
            {
                case "CPU":
                    return 1;
                case "Head":
                    return 2;
                case "Body":
                    return 3;
                case "Arms":
                    return 4;
                case "Legs":
                    return 5;
            }

            throw new ArgumentOutOfRangeException();
        }

        private (int, int, int, int) GetSkillLevels(int oneHanded, int twoHanded, int martialArts, int ranged)
        {
            var skill1Id = -1;
            var skill1Value = -1;
            var skill2Id = -1;
            var skill2Value = -1;

            if (oneHanded > 0)
            {
                if (skill1Id == -1)
                {
                    skill1Id = 12;
                    skill1Value = oneHanded;
                }
                else if (skill2Id == -1)
                {
                    skill2Id = 12;
                    skill2Value = oneHanded;
                }
            }

            if (twoHanded > 0)
            {
                if (skill1Id == -1)
                {
                    skill1Id = 13;
                    skill1Value = twoHanded;

                }
                else if (skill2Id == -1)
                {
                    skill2Id = 13;
                    skill2Value = twoHanded;
                }

            }

            if (martialArts > 0)
            {
                if (skill1Id == -1)
                {
                    skill1Id = 14;
                    skill1Value = martialArts;
                }
                else if (skill2Id == -1)
                {
                    skill2Id = 14;
                    skill2Value = martialArts;
                }
            }

            if (ranged > 0)
            {
                if (skill1Id == -1)
                {
                    skill1Id = 15;
                    skill1Value = ranged;
                }
                else if (skill2Id == -1)
                {
                    skill2Id = 15;
                    skill2Value = ranged;
                }
            }

            return (skill1Id, skill1Value, skill2Id, skill2Value);
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
