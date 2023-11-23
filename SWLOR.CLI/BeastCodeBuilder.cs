using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.CLI
{
    internal class BeastCodeBuilder
    {
        private class BeastCodeDetail
        {
            public string Code { get; set; }
            public Dictionary<int, string> Levels { get; set; }

            public BeastCodeDetail()
            {
                Levels = new Dictionary<int, string>();
            }
        }

        private const string InputData = "./InputFiles/beast_levels.tsv";
        
        private const string Template = "./Templates/beast_builder_template.txt";
        private const string LevelTemplate = "./Templates/beast_level_template.txt";
        private const string OutputFolder = "./OutputBeasts/";

        public void Process()
        {
            ClearOutputDirectory();

            var template = File.ReadAllText(Template);
            var levelTemplate = File.ReadAllText(LevelTemplate);
            var inputLines = File.ReadAllLines(InputData).ToList();
            var beasts = new Dictionary<BeastType, BeastCodeDetail>();

            // Throw away the first three lines.
            inputLines.RemoveAt(2);
            inputLines.RemoveAt(1);
            inputLines.RemoveAt(0);

            foreach (var line in inputLines)
            {
                var data = line.Split('\t');
                if (string.IsNullOrWhiteSpace(data[0]))
                    continue;

                var enumName = Enum.Parse<BeastType>(data[26]);

                if(!beasts.ContainsKey(enumName))
                    beasts.Add(enumName, new BeastCodeDetail());

                var detail = beasts[enumName];

                var className = $"{enumName}BeastDefinition";
                var name = data[0].Trim();
                var beastType = data[26].Trim();
                var accuracyStat = data[12].Trim();
                var damageStat = data[13].Trim();
                var role = data[1].Trim();
                var appearance = data[27].Trim();
                var portraitId = data[28].Trim();
                var soundSetId = data[29].Trim();
                detail.Code = template
                    .Replace("%%BEASTNAME%%", name)
                    .Replace("%%APPEARANCETYPE%%", appearance)
                    .Replace("%%SOUNDSETID%%", soundSetId)
                    .Replace("%%PORTRAITID%%", portraitId)
                    .Replace("%%CLASSNAME%%", className)
                    .Replace("%%BEASTTYPE%%", beastType)
                    .Replace("%%ACCURACYSTAT%%", GetAbilityEnumName(accuracyStat))
                    .Replace("%%DAMAGESTAT%%", GetAbilityEnumName(damageStat))
                    .Replace("%%BEASTROLE%%", role);

                var level = Convert.ToInt32(data[2]);
                if (!detail.Levels.ContainsKey(level))
                    detail.Levels.Add(level, levelTemplate);

                var hp = data[3].Replace(",", "").Trim();
                var stm = data[4].Replace(",", "").Trim();
                var fp = data[5].Replace(",", "").Trim();
                var mgt = data[6].Trim();
                var per = data[7].Trim();
                var vit = data[8].Trim();
                var wil = data[9].Trim();
                var agi = data[10].Trim();
                var soc = data[11].Trim();
                var attackBonusMax = data[14].Trim();
                var accuracyBonusMax = data[15].Trim();
                var evasionBonusMax = data[16].Trim();
                var physicalDefenseMax = data[17].Trim();
                var forceDefenseMax = data[18].Trim();
                var fireDefenseMax = data[19].Trim();
                var poisonDefenseMax = data[21].Trim();
                var electricalDefenseMax = data[22].Trim();
                var iceDefenseMax = data[20].Trim();
                var willMax = data[24].Trim();
                var fortitudeMax = data[23].Trim();
                var reflexMax = data[25].Trim();
                var dmg = data[31].Trim();

                detail.Levels[level] = detail.Levels[level]
                    .Replace("%%LEVEL%%", level.ToString())
                    .Replace("%%HP%%", hp)
                    .Replace("%%STM%%", stm)
                    .Replace("%%FP%%", fp)
                    .Replace("%%DMG%%", dmg)

                    .Replace("%%MGT%%", mgt)
                    .Replace("%%PER%%", per)
                    .Replace("%%VIT%%", vit)
                    .Replace("%%WIL%%", wil)
                    .Replace("%%AGI%%", agi)
                    .Replace("%%SOC%%", soc)

                    .Replace("%%MAXATTACKBONUS%%", attackBonusMax)
                    .Replace("%%MAXACCURACYBONUS%%", accuracyBonusMax)
                    .Replace("%%MAXEVASIONBONUS%%", evasionBonusMax)

                    .Replace("%%MAXPHYSICALDEFENSE%%", physicalDefenseMax)
                    .Replace("%%MAXFORCEDEFENSE%%", forceDefenseMax)
                    .Replace("%%MAXFIREDEFENSE%%", fireDefenseMax)
                    .Replace("%%MAXPOISONDEFENSE%%", poisonDefenseMax)
                    .Replace("%%MAXELECTRICALDEFENSE%%", electricalDefenseMax)
                    .Replace("%%MAXICEDEFENSE%%", iceDefenseMax)

                    .Replace("%%MAXWILL%%", willMax)
                    .Replace("%%MAXFORTITUDE%%", fortitudeMax)
                    .Replace("%%MAXREFLEX%%", reflexMax);
            }

            foreach (var(type, detail) in beasts)
            {
                var levels = detail.Levels.OrderBy(o => o.Key);
                var levelText = string.Empty;
                var levelFunctionCalls = string.Empty;

                foreach (var (levelId, level) in levels)
                {
                    levelText += level;
                    levelFunctionCalls += $"\t\t\tLevel{levelId}();" + Environment.NewLine;
                }

                var output = detail.Code
                    .Replace("%%LEVELLIST%%", levelText)
                    .Replace("%%LEVELCALLS%%", levelFunctionCalls);
                File.WriteAllText($"{OutputFolder}/{type}BeastDefinition.cs", output);
            }
        }

        private string GetAbilityEnumName(string shorthand)
        {
            switch (shorthand)
            {
                case "MGT":
                    return AbilityType.Might.ToString();
                case "PER":
                    return AbilityType.Perception.ToString();
                case "VIT":
                    return AbilityType.Vitality.ToString();
                case "WIL":
                    return AbilityType.Willpower.ToString();
                case "AGI":
                    return AbilityType.Agility.ToString();
                case "SOC":
                    return AbilityType.Social.ToString();
            }

            return AbilityType.Invalid.ToString();
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
