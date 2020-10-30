using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class ColoredCrystalSpawnRule : ISpawnRule
    {
        public void Run(NWObject target, params object[] args)
        {
            var roll = RandomService.Random(0, 100);
            var quality = ResourceQuality.Low;
            var qualityName = "Sparse";

            const int NormalQualityChance = 20;
            const int HighQualityChance = 10;
            const int VeryHighQualityChance = 2;

            var dbArea = DataService.Area.GetByResref(target.Area.Resref);
            var tier = dbArea.ResourceQuality;
            var maxTier = dbArea.MaxResourceQuality;

            if (tier <= 0)
            {
                Console.WriteLine("WARNING: Area '" + target.Area.Name + "' has resources but the RESOURCE_QUALITY variable is not set. Edit the area properties and add this value to set up resources.");
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

            var quantity = RandomService.Random(1, 3);

            target.SetLocalInt("RESOURCE_QUALITY", (int)quality);
            target.SetLocalInt("RESOURCE_TIER", tier);
            target.SetLocalInt("RESOURCE_COUNT", quantity);
            target.SetLocalString("RESOURCE_QUALITY_NAME", qualityName);
        }
    }
}
