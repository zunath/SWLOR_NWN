using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class DrillSpawnRule: ISpawnRule
    {
        private readonly IRandomService _random;
        private readonly IResourceService _resource;
        private readonly IBiowareXP2 _biowareXP2;
        private readonly IColorTokenService _color;

        public DrillSpawnRule(
            IRandomService random,
            IResourceService resource,
            IBiowareXP2 biowareXP2,
            IColorTokenService color)
        {
            _random = random;
            _resource = resource;
            _biowareXP2 = biowareXP2;
            _color = color;
        }

        public void Run(NWObject target, params object[] args)
        {
            int retrievalRating = (int) args[0];
            int highQualityChance = 10 * retrievalRating;
            int veryHighQualityChance = 2 * retrievalRating;
            int roll = _random.Random(0, 100);

            ResourceQuality quality = ResourceQuality.Normal;

            if (roll <= veryHighQualityChance)
            {
                quality = ResourceQuality.VeryHigh;
            }
            else if (roll <= highQualityChance)
            {
                quality = ResourceQuality.High;
            }

            var ip = _resource.GetRandomComponentBonusIP(quality);
            _biowareXP2.IPSafeAddItemProperty(target.Object, ip.Item1, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);

            switch (ip.Item2)
            {
                case 0:
                    target.Name = _color.Green(target.Name);
                    break;
                case 1:
                    target.Name = _color.Blue(target.Name);
                    break;
                case 2:
                    target.Name = _color.Purple(target.Name);
                    break;
                case 3:
                    target.Name = _color.Orange(target.Name);
                    break;
            }
        }
    }
}
