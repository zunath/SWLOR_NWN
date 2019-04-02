using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.SpawnRule.Contracts;

namespace SWLOR.Game.Server.SpawnRule
{
    public class DrillSpawnRule: ISpawnRule
    {

        public void Run(NWObject target, params object[] args)
        {
            int retrievalRating = (int) args[0];
            int highQualityChance = 10 * retrievalRating;
            int veryHighQualityChance = 2 * retrievalRating;
            int roll = RandomService.Random(0, 100);

            ResourceQuality quality = ResourceQuality.Normal;

            if (roll <= veryHighQualityChance)
            {
                quality = ResourceQuality.VeryHigh;
            }
            else if (roll <= highQualityChance)
            {
                quality = ResourceQuality.High;
            }

            var ip = ResourceService.GetRandomComponentBonusIP(quality);
            BiowareXP2.IPSafeAddItemProperty(target.Object, ip.Item1, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);

            switch (ip.Item2)
            {
                case 0:
                    target.Name = ColorTokenService.Green(target.Name);
                    break;
                case 1:
                    target.Name = ColorTokenService.Blue(target.Name);
                    break;
                case 2:
                    target.Name = ColorTokenService.Purple(target.Name);
                    break;
                case 3:
                    target.Name = ColorTokenService.Orange(target.Name);
                    break;
                case 4:
                    target.Name = _color.LightPurple(target.Name);
                    break;
                case 5:
                    target.Name = _color.Yellow(target.Name);
                    break;
                case 6:
                    target.Name = _color.Red(target.Name);
                    break;
                case 7:
                    target.Name = _color.Cyan(target.Name);
                    break;
            }
        }
    }
}
