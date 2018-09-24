using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class TreeSpawnRule : ISpawnRule
    {
        private readonly IRandomService _random;
        private readonly IDataContext _db;

        public TreeSpawnRule(IRandomService random, IDataContext db)
        {
            _random = random;
            _db = db;
        }

        public void Run(NWObject target)
        {
            int roll = _random.Random(0, 100);
            ResourceQuality quality = ResourceQuality.Low;
            ResourceType resourceType = ResourceType.Ore;
            string qualityName = "Low Quality";

            const int NormalQualityChance = 20;
            const int HighQualityChance = 10;
            const int VeryHighQualityChance = 2;

            var dbArea = _db.Areas.Single(x => x.Resref == target.Area.Resref);
            int tier = dbArea.ResourceQuality;
            
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


            roll = _random.Random(0, 100);
            if (roll <= 2)
            {
                tier++;
            }

            if (tier > 10) tier = 10;

            target.SetLocalInt("RESOURCE_QUALITY", (int)quality);
            target.SetLocalInt("RESOURCE_TIER", tier);
            target.SetLocalInt("RESOURCE_COUNT", _random.Random(3, 10));
            target.SetLocalString("RESOURCE_RESREF", GetResourceResref(tier));
            target.SetLocalString("RESOURCE_QUALITY_NAME", qualityName);
        }

        private string GetResourceResref(int tier)
        {
            switch (tier)
            {
                case 1: return "elm_wood";
                case 2: return "ash_wood";
                case 3: return "walnut_wood";
                case 4: return "arrowwood_wood";
                case 5: return "rosewood_wood";
                case 6: return "mahogany_wood";
                case 7: return "maple_wood";
                case 8: return "willow_wood";
                case 9: return "lauan_wood";
                case 10: return "ebony_wood";
                default: return string.Empty;
            }
        }

    }
}
