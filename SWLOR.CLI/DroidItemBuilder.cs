using System;
using System.IO;
using System.Linq;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.CLI
{
    internal class DroidItemBuilder
    {
        private const string InputData = "./InputFiles/droid_item_list.tsv";
        private const string CPUTemplate = "./Templates/droid_cpu_template.json";
        private const string PartTemplate = "./Templates/droid_part_template.json";
        private const string OutputFolder = "./OutputDroidItems/";

        private readonly int[] _iconIds = { 1, 2, 3, 4, 5, 6, 9, 10, 12, 13, 14, 15, 16, 17, 20, 22, 23, 24, 25, 26, 27, 30, 31, 41, 42,
            44, 45, 46, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91,
            92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 160, 175, 176, 177,
            178, 179, 180, 181, 182, 183, 187, 188, 189, 190, 191, 194, 196, 197, 198, 217, 218, 219, 220, 227, 228, 229, 230,
            231, 232, 233, 234, 236, 237, 238, 239, 244, 245, 246, 247, 248, 250, 254};

        public void Process()
        {
            ClearOutputDirectory();
            ProcessPart();
        }

        private const string StatTemplate = @",{
        ""__struct_id"": 0,
        ""ChanceAppear"": {
          ""type"": ""byte"",
          ""value"": 100
        },
        ""CostTable"": {
          ""type"": ""byte"",
          ""value"": 45
        },
        ""CostValue"": {
          ""type"": ""word"",
          ""value"": %%STATVALUE3%%
        },
        ""Param1"": {
          ""type"": ""byte"",
          ""value"": 255
        },
        ""Param1Value"": {
          ""type"": ""byte"",
          ""value"": 0
        },
        ""PropertyName"": {
          ""type"": ""word"",
          ""value"": 121
        },
        ""Subtype"": {
          ""type"": ""word"",
          ""value"": %%STATTYPE3%%
        }
      }";
        private const string AISlotsTemplate = @",{
        ""__struct_id"": 0,
        ""ChanceAppear"": {
          ""type"": ""byte"",
          ""value"": 100
        },
        ""CostTable"": {
          ""type"": ""byte"",
          ""value"": 45
        },
        ""CostValue"": {
          ""type"": ""word"",
          ""value"": %%AISLOTS%%
        },
        ""Param1"": {
          ""type"": ""byte"",
          ""value"": 255
        },
        ""Param1Value"": {
          ""type"": ""byte"",
          ""value"": 0
        },
        ""PropertyName"": {
          ""type"": ""word"",
          ""value"": 121
        },
        ""Subtype"": {
          ""type"": ""word"",
          ""value"": 3
        }
      }";

        private void ProcessCPU()
        {
            var input2daText = File.ReadAllLines(InputData).ToList();
            var templateText = File.ReadAllText(CPUTemplate);

            foreach (var line in input2daText)
            {
                var parsed = line.Split('\t');
                var name = parsed[0].Trim();
                var resref = parsed[1].Trim();
                var (slotType, _) = GetSlotType(parsed[2].Trim());
                var tier = string.IsNullOrWhiteSpace(parsed[3]) ? 0.ToString() : parsed[3].Trim();
                var aiSlots = string.IsNullOrWhiteSpace(parsed[4]) ? 0.ToString() : parsed[4].Trim();
                var hp = string.IsNullOrWhiteSpace(parsed[6]) ? 0.ToString() : parsed[6].Trim();
                var stm = string.IsNullOrWhiteSpace(parsed[7]) ? 0.ToString() : parsed[7].Trim();
                var mgt = string.IsNullOrWhiteSpace(parsed[8]) ? 0.ToString() : parsed[8].Trim();
                var per = string.IsNullOrWhiteSpace(parsed[9]) ? 0.ToString() : parsed[9].Trim();
                var vit = string.IsNullOrWhiteSpace(parsed[10]) ? 0.ToString() : parsed[10].Trim();
                var wil = string.IsNullOrWhiteSpace(parsed[11]) ? 0.ToString() : parsed[11].Trim();
                var agi = string.IsNullOrWhiteSpace(parsed[12]) ? 0.ToString() : parsed[12].Trim();
                var soc = string.IsNullOrWhiteSpace(parsed[13]) ? 0.ToString() : parsed[13].Trim();
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

        private void ProcessPart()
        {
            var input2daText = File.ReadAllLines(InputData).ToList();
            var templateText = File.ReadAllText(PartTemplate);

            foreach (var line in input2daText)
            {
                var parsed = line.Split('\t');
                var name = parsed[0].Trim();
                var resref = parsed[1].Trim();
                var (slotType, iconId) = GetSlotType(parsed[2].Trim());
                var tier = string.IsNullOrWhiteSpace(parsed[3]) ? 0.ToString() : parsed[3].Trim();
                var aiSlots = string.IsNullOrWhiteSpace(parsed[4]) ? 0 : Convert.ToInt32(parsed[4].Trim());
                //var hp = string.IsNullOrWhiteSpace(parsed[6]) ? 0 : parsed[6].Trim();
                //var stm = string.IsNullOrWhiteSpace(parsed[7]) ? 0 : parsed[7].Trim();
                var mgt = string.IsNullOrWhiteSpace(parsed[8]) ? 0 : Convert.ToInt32(parsed[8].Trim());
                var per = string.IsNullOrWhiteSpace(parsed[9]) ? 0 : Convert.ToInt32(parsed[9].Trim());
                var vit = string.IsNullOrWhiteSpace(parsed[10]) ? 0 : Convert.ToInt32(parsed[10].Trim());
                var wil = string.IsNullOrWhiteSpace(parsed[11]) ? 0 : Convert.ToInt32(parsed[11].Trim());
                var agi = string.IsNullOrWhiteSpace(parsed[12]) ? 0 : Convert.ToInt32(parsed[12].Trim());
                var soc = string.IsNullOrWhiteSpace(parsed[13]) ? 0 : Convert.ToInt32(parsed[13].Trim());
                //var oneHanded = string.IsNullOrWhiteSpace(parsed[14]) ? 0 : Convert.ToInt32(parsed[14].Trim());
                //var twoHanded = string.IsNullOrWhiteSpace(parsed[15]) ? 0 : Convert.ToInt32(parsed[15].Trim());
                //var martialArts = string.IsNullOrWhiteSpace(parsed[16]) ? 0 : Convert.ToInt32(parsed[16].Trim());
                //var ranged = string.IsNullOrWhiteSpace(parsed[17]) ? 0 : Convert.ToInt32(parsed[17].Trim());
                var stat3Template = string.Empty;
                var aiSlotsTemplate = string.Empty;
                var (stat1Type, stat1Value, stat2Type, stat2Value, stat3Type, stat3Value) = 
                    GetStatPoints(mgt, per, agi, vit, wil, soc);

                if (stat3Type > -1 && stat3Value > -1)
                {
                    stat3Template = StatTemplate
                        .Replace("%%STATTYPE3%%", stat3Type.ToString())
                        .Replace("%%STATVALUE3%%", stat3Value.ToString());
                }

                if (aiSlots > -1)
                {
                    aiSlotsTemplate = AISlotsTemplate
                        .Replace("%%AISLOTS%%", aiSlots.ToString());
                }

                var json = templateText
                    .Replace("%%NAME%%", name)
                    .Replace("%%ICONID%%", iconId.ToString())
                    .Replace("%%DROIDPARTTYPE%%", slotType.ToString())
                    .Replace("%%AISLOTS%%", aiSlotsTemplate)
                    .Replace("%%STATTYPE1%%", stat1Type.ToString())
                    .Replace("%%STATVALUE1%%", stat1Value.ToString())
                    .Replace("%%STATTYPE2%%", stat2Type.ToString())
                    .Replace("%%STATVALUE2%%", stat2Value.ToString())
                    .Replace("%%STAT3%%", stat3Template)
                    .Replace("%%TIER%%", tier)
                    .Replace("%%AISLOTS%%", aiSlotsTemplate)

                    .Replace("%%TAG%%", resref)
                    .Replace("%%RESREF%%", resref);

                File.WriteAllText($"{OutputFolder}/{resref}.uti.json", json);
            }
        }

        private (int, int) GetSlotType(string type)
        {
            switch (type)
            {
                case "CPU":
                    return (1, -1);
                case "Head":
                    return (2, 110);
                case "Body":
                    return (3, 120);
                case "Arms":
                    return (4, 37);
                case "Legs":
                    return (5, 90);
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

        private (int, int, int, int, int, int) GetStatPoints(int mgt, int per, int agi, int vit, int wil, int soc)
        {
            var stat1Type = -1;
            var stat1Value = -1;
            var stat2Type = -1;
            var stat2Value = -1;
            var stat3Type = -1;
            var stat3Value = -1;

            if (mgt > 0)
            {
                if (stat1Type == -1)
                {
                    stat1Type = 6;
                    stat1Value = mgt;
                }
                else if (stat2Type == -1)
                {
                    stat2Type = 6;
                    stat2Value = mgt;
                }
                else if (stat3Type == -1)
                {
                    stat3Type = 6;
                    stat3Value = mgt;
                }
            }
            if (per > 0)
            {
                if (stat1Type == -1)
                {
                    stat1Type = 7;
                    stat1Value = per;
                }
                else if (stat2Type == -1)
                {
                    stat2Type = 7;
                    stat2Value = per;
                }
                else if (stat3Type == -1)
                {
                    stat3Type = 7;
                    stat3Value = per;
                }
            }
            if (vit > 0)
            {
                if (stat1Type == -1)
                {
                    stat1Type = 8;
                    stat1Value = vit;
                }
                else if (stat2Type == -1)
                {
                    stat2Type = 8;
                    stat2Value = vit;
                }
                else if (stat3Type == -1)
                {
                    stat3Type = 8;
                    stat3Value = vit;
                }
            }
            if (wil > 0)
            {
                if (stat1Type == -1)
                {
                    stat1Type = 9;
                    stat1Value = wil;
                }
                else if (stat2Type == -1)
                {
                    stat2Type = 9;
                    stat2Value = wil;
                }
                else if (stat3Type == -1)
                {
                    stat3Type = 9;
                    stat3Value = wil;
                }
            }

            if (agi > 0)
            {
                if (stat1Type == -1)
                {
                    stat1Type = 10;
                    stat1Value = agi;
                }
                else if (stat2Type == -1)
                {
                    stat2Type = 10;
                    stat2Value = agi;
                }
                else if (stat3Type == -1)
                {
                    stat3Type = 10;
                    stat3Value = agi;
                }
            }
            if (soc > 0)
            {
                if (stat1Type == -1)
                {
                    stat1Type = 11;
                    stat1Value = soc;
                }
                else if (stat2Type == -1)
                {
                    stat2Type = 11;
                    stat2Value = soc;
                }
                else if (stat3Type == -1)
                {
                    stat3Type = 11;
                    stat3Value = soc;
                }
            }

            return (stat1Type, stat1Value, stat2Type, stat2Value, stat3Type, stat3Value);
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
