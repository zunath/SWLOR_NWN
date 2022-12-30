using SWLOR.Game.Server.Service.FishingService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class TatooineFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            TatooineBabySarlaccCaveFish();
            TatooineTuskenRaiderCaveMainFloorFish();

            return _builder.Build();
        }

        private void TatooineBabySarlaccCaveFish()
        {
            _builder
                .Create(FishingLocationType.TatooineBabySarlaccCave);
        }

        private void TatooineTuskenRaiderCaveMainFloorFish()
        {
            _builder
                .Create(FishingLocationType.TatooineTuskenRaiderCaveMainFloor)

                .AddFish(FishType.BastionSweeper, FishingRodType.Composite, FishingBaitType.LizardLure)
                .AddFish(FishType.BastionSweeper, FishingRodType.Composite, FishingBaitType.WormLure)
                .AddFish(FishType.BastionSweeper, FishingRodType.Composite, FishingBaitType.CallaLure)

                .AddFish(FishType.ChevalSalmon, FishingRodType.Halcyon, FishingBaitType.WormLure)
                .AddFish(FishType.ChevalSalmon, FishingRodType.Halcyon, FishingBaitType.Meatball)

                .AddFish(FishType.DarkBass, FishingRodType.LuShang, FishingBaitType.CallaLure)
                .AddFish(FishType.DarkBass, FishingRodType.LuShang, FishingBaitType.FlyLure)

                .AddFish(FishType.Frigorifish, FishingRodType.Tranquility, FishingBaitType.DrillCalamary)
                .NighttimeOnly()
                .AddFish(FishType.Frigorifish, FishingRodType.Tranquility, FishingBaitType.GiantShellBug)
                .NighttimeOnly()
                .AddFish(FishType.Frigorifish, FishingRodType.LuShang, FishingBaitType.DrillCalamary)
                .NighttimeOnly()
                .AddFish(FishType.Frigorifish, FishingRodType.LuShang, FishingBaitType.GiantShellBug)
                .NighttimeOnly()

                .AddFish(FishType.ShiningTrout, FishingRodType.Tranquility, FishingBaitType.LufaiseFly)
                .AddFish(FishType.ShiningTrout, FishingRodType.Tranquility, FishingBaitType.SabikiRig);
        }
    }
}
