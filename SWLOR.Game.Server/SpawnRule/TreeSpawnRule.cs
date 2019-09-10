using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class TreeSpawnRule : ISpawnRule
    {
        public void Run(NWObject target, params object[] args)
        {
            int roll = RandomService.Random(0, 100);
            ResourceQuality quality = ResourceQuality.Low;
            string qualityName = "Low Quality";

            const int NormalQualityChance = 20;
            const int HighQualityChance = 10;
            const int VeryHighQualityChance = 2;

            var dbArea = DataService.Area.GetByResref(target.Area.Resref);
            int tier = dbArea.ResourceQuality;
            int maxTier = dbArea.MaxResourceQuality;
            
            if (tier <= 0)
            {
                Console.WriteLine("WARNING: Area '" + target.Area.Name + "' has resources but the RESOURCE_QUALITY variable is not set. Edit the area properties and add this value to set up resources.");
                return;
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
            if (roll <= 2)
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
                case 1: return "greel_wood";
                case 2: return "borl_wood";
                case 3: return "cosian_wood";
                case 4: return "homogoni_wood";
                case 5: return "japor_wood";
                case 6: return "laroon_wood";
                case 7: return "quasiwood_wood";
                case 8: return "resinwood_wood";
                case 9: return "scentwood_wood";
                case 10: return "ebony_wood";
                default: return string.Empty;
            }
        }

    }
}
