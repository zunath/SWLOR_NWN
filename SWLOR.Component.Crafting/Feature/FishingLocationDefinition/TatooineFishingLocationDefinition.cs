using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Enums;
using SWLOR.Component.Crafting.Model;

namespace SWLOR.Component.Crafting.Feature.FishingLocationDefinition
{
    public class TatooineFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly IFishingLocationBuilder _builder = new();

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
