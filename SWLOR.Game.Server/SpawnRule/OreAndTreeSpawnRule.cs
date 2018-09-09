using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class OreAndTreeSpawnRule: ISpawnRule
    {
        private readonly IRandomService _random;
        private readonly IDataContext _db;

        public OreAndTreeSpawnRule(IRandomService random, IDataContext db)
        {
            _random = random;
            _db = db;
        }

        public void Run(NWObject target)
        {
            int roll = _random.Random(0, 100);
            ResourceQuality quality = ResourceQuality.Low;
            ResourceType resourceType = ResourceType.Ore;

            const int NormalQualityChance = 20;
            const int HighQualityChance = 10;
            const int VeryHighQualityChance = 2;

            var dbArea = _db.Areas.Single(x => x.Resref == target.Area.Resref);
            int tier = dbArea.ResourceSpawnTableID;

            if (roll <= VeryHighQualityChance)
            {
                quality = ResourceQuality.VeryHigh;
            }
            else if (roll <= HighQualityChance)
            {
                quality = ResourceQuality.High;
            }
            else if (roll <= NormalQualityChance)
            {
                quality = ResourceQuality.Normal;
            }

            roll = _random.Random(0, 100);
            if (roll <= 30)
            {
                resourceType = ResourceType.Organic;
            }

            roll = _random.Random(0, 100);
            if (roll <= 3)
            {
                tier++;
            }

            if (tier > 10) tier = 10;

            target.SetLocalInt("RESOURCE_QUALITY", (int)quality);
            target.SetLocalInt("RESOURCE_TIER", tier);
            target.SetLocalInt("RESOURCE_TYPE", (int)resourceType);
            target.SetLocalInt("RESOURCE_COUNT", _random.Random(3, 10));
        }
    }
}
