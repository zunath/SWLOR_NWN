using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.SpawnRule.Contracts;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.SpawnRule
{
    public class CrystalClusterSpawnRule : ISpawnRule
    {
        public void Run(NWObject target, params object[] args)
        {
            var area = GetArea(target);
            var areaName = GetName(area);
            var areaResref = GetResRef(area);
            var roll = RandomService.Random(0, 100);
            var quality = ResourceQuality.Low;
            var qualityName = "Sparse";

            const int NormalQualityChance = 20;
            const int HighQualityChance = 10;
            const int VeryHighQualityChance = 2;

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
                qualityName = "Very Dense";
            }
            else if (roll <= HighQualityChance)
            {
                quality = ResourceQuality.High;
                qualityName = "Dense";
            }
            else if (roll <= NormalQualityChance)
            {
                quality = ResourceQuality.Normal;
                qualityName = "Thick";
            }

            roll = RandomService.Random(0, 100);
            if (roll <= 3)
            {
                tier++;
            }

            if (tier > 10) tier = 10;

            if (tier > maxTier)
                tier = maxTier;

            var resref = GetResourceResref(tier);
            var quantity = RandomService.Random(3, 10);

            roll = RandomService.Random(0, 100);

            if (roll <= 2)
            {
                string[] coloredResrefs = {"p_crystal_red", "p_crystal_green", "p_crystal_blue", "p_crystal_yellow"};

                roll = RandomService.Random(0, 3);
                resref = coloredResrefs[roll];
            }
            
            target.SetLocalInt("RESOURCE_QUALITY", (int)quality);
            target.SetLocalInt("RESOURCE_TIER", tier);
            target.SetLocalInt("RESOURCE_COUNT", quantity);
            target.SetLocalString("RESOURCE_RESREF", resref);
            target.SetLocalString("RESOURCE_QUALITY_NAME", qualityName);
        }

        private string GetResourceResref(int tier)
        {
            switch (tier)
            {
                case 1: return "power_crystal_po";
                case 2: return "power_crystal_fl";
                case 3: return "power_crystal_fa";
                case 4: return "power_crystal_im";
                case 5: return "power_crystal_go";
                case 6: return "power_crystal_qu";
                case 7: return "power_crystal_se";
                case 8: return "power_crystal_pr";
                case 9: return "power_crystal_fl";
                case 10: return "power_crystal_pe";
                default: return string.Empty;
            }
        }

    }
}
