using System;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.SpawnRule.Contracts;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.SpawnRule
{
    public class ColoredCrystalSpawnRule : ISpawnRule
    {
        public void Run(NWObject target, params object[] args)
        {
            var roll = SWLOR.Game.Server.Service.Random.Next(0, 100);
            var quality = ResourceQuality.Low;
            var qualityName = "Sparse";

            var area = GetArea(target);
            var areaName = GetName(area);
            var areaResref = GetResRef(area);
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

            roll = SWLOR.Game.Server.Service.Random.Next(0, 100);
            if (roll <= 3)
            {
                tier++;
            }

            if (tier > 10) tier = 10;

            if (tier > maxTier)
                tier = maxTier;

            var quantity = SWLOR.Game.Server.Service.Random.Next(1, 3);

            target.SetLocalInt("RESOURCE_QUALITY", (int)quality);
            target.SetLocalInt("RESOURCE_TIER", tier);
            target.SetLocalInt("RESOURCE_COUNT", quantity);
            target.SetLocalString("RESOURCE_QUALITY_NAME", qualityName);
        }
    }
}
