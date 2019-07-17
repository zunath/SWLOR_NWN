using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class OreSpawnRule : ISpawnRule
    {
        public void Run(NWObject target, params object[] args)
        {
            int roll = RandomService.Random(0, 100);
            ResourceQuality quality = ResourceQuality.Low;
            string qualityName = "Low Quality";

            var dbArea = DataService.Area.GetByResref(target.Area.Resref);
            int tier = dbArea.ResourceQuality;
            int maxTier = dbArea.MaxResourceQuality;

            if (tier <= 0)
            {
                Console.WriteLine("WARNING: Area '" + target.Area.Name + "' has resources but the RESOURCE_QUALITY variable is not set. Edit the area properties and add this value to set up resources.");
                return;
            }


            int NormalQualityChance;
            int HighQualityChance;
            int VeryHighQualityChance;

            switch (tier)
            {
                case 1:
                    NormalQualityChance = 20;
                    HighQualityChance = 0;
                    VeryHighQualityChance = 0;
                    break;
                case 2:
                    NormalQualityChance = 20;
                    HighQualityChance = 2;
                    VeryHighQualityChance = 0;
                    break;
                case 3:
                case 4:
                case 5:
                    NormalQualityChance = 20;
                    HighQualityChance = 2;
                    VeryHighQualityChance = 1;
                    break;
                case 6:
                case 7:
                case 8:
                    NormalQualityChance = 20;
                    HighQualityChance = 10;
                    VeryHighQualityChance = 3;
                    break;
                case 9:
                case 10:
                    NormalQualityChance = 20;
                    HighQualityChance = 12;
                    VeryHighQualityChance = 5;
                    break;
                default:
                    NormalQualityChance = 0;
                    HighQualityChance = 0;
                    VeryHighQualityChance = 0;
                    break;
            }


            if (roll <= VeryHighQualityChance)
            {
                quality = ResourceQuality.VeryHigh;
                qualityName = "Very High Quality";
            }
            else if (roll <= HighQualityChance)
            {
                quality = ResourceQuality.High;
                qualityName = "High Quality";
            }
            else if (roll <= NormalQualityChance)
            {
                quality = ResourceQuality.Normal;
                qualityName = "Normal Quality";
            }
            
            roll = RandomService.Random(0, 100);
            if (roll <= 3)
            {
                tier++;
            }

            if (tier > 10) tier = 10;

            if (tier > maxTier)
                tier = maxTier;

            target.SetLocalInt("RESOURCE_QUALITY", (int)quality);
            target.SetLocalInt("RESOURCE_TIER", tier);
            target.SetLocalInt("RESOURCE_COUNT", RandomService.Random(3, 10));
            target.SetLocalString("RESOURCE_RESREF", GetResourceResref(tier));
            target.SetLocalString("RESOURCE_QUALITY_NAME", qualityName);
        }

        private string GetResourceResref(int tier)
        {
            switch (tier)
            {
                case 1: return "raw_veldite";
                case 2: return "raw_scordspar";
                case 3: return "raw_plagionite";
                case 4: return "raw_keromber";
                case 5: return "raw_jasioclase";
                case 6: return "raw_hemorgite";
                case 7: return "raw_ochne";
                case 8: return "raw_croknor";
                case 9: return "raw_arkoxit";
                case 10: return "raw_bisteiss";
                default: return string.Empty;
            }
        }

    }
}
