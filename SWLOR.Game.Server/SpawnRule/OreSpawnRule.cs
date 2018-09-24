using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class OreSpawnRule : ISpawnRule
    {
        private readonly IRandomService _random;
        private readonly IDataContext _db;

        public OreSpawnRule(IRandomService random, IDataContext db)
        {
            _random = random;
            _db = db;
        }

        public void Run(NWObject target)
        {
            int roll = _random.Random(0, 100);
            ResourceQuality quality = ResourceQuality.Low;
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
            if (roll <= 3)
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
