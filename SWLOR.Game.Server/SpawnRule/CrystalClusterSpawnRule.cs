using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class CrystalClusterSpawnRule : ISpawnRule
    {
        private readonly IRandomService _random;
        private readonly IDataContext _db;

        public CrystalClusterSpawnRule(IRandomService random, IDataContext db)
        {
            _random = random;
            _db = db;
        }

        public void Run(NWObject target, params object[] args)
        {
            int roll = _random.Random(0, 100);
            ResourceQuality quality = ResourceQuality.Low;
            string qualityName = "Sparse";

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

            roll = _random.Random(0, 100);
            if (roll <= 3)
            {
                tier++;
            }

            if (tier > 10) tier = 10;

            string resref = GetResourceResref(tier);
            int quantity = _random.Random(3, 10);

            roll = _random.Random(0, 100);

            if (roll <= 2)
            {
                string[] coloredResrefs = {"p_crystal_red", "p_crystal_green", "p_crystal_blue", "p_crystal_yellow"};

                roll = _random.Random(0, 3);
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
