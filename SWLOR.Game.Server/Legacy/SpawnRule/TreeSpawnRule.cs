using System;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.SpawnRule.Contracts;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.SpawnRule
{
    public class TreeSpawnRule : ISpawnRule
    {
        public void Run(NWObject target, params object[] args)
        {
            var roll = SWLOR.Game.Server.Service.Random.Next(0, 100);
            var quality = ResourceQuality.Low;
            var qualityName = "Low Quality";

            const int NormalQualityChance = 20;
            const int HighQualityChance = 10;
            const int VeryHighQualityChance = 2;

            var area = GetArea(target);
            var areaName = GetName(area);
            var areaResref = GetResRef(area);
            var dbArea = DataService.Area.GetByResref(areaResref);
            var tier = dbArea.ResourceQuality;
            var maxTier = dbArea.MaxResourceQuality;
            
            if (tier <= 0)
            {
                Console.WriteLine("WARNING: Area '" + areaName + "' has resources but the RESOURCE_QUALITY variable is not set. Edit the area properties and add this value to set up resources.");
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


            roll = SWLOR.Game.Server.Service.Random.Next(0, 100);
            if (roll <= 2)
            {
                tier++;
            }

            if (tier > 10) tier = 10;

            if (tier > maxTier)
                tier = maxTier;

            target.SetLocalInt("RESOURCE_QUALITY", (int)quality);
            target.SetLocalInt("RESOURCE_TIER", tier);
            target.SetLocalInt("RESOURCE_COUNT", SWLOR.Game.Server.Service.Random.Next(3, 10));
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
