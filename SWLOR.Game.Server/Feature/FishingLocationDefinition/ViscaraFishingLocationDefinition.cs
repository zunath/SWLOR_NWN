using System.Collections.Generic;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class ViscaraFishingLocationDefinition: IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            ViscaraCavernFish();
            ViscaraDeepwoodsFish();
            ViscaraEasternSwamplandsFish();
            ViscaraLakeFish();
            ViscaraLakeGroundsFish();
            ViscaraMountainValleyFish();
            ViscaraWesternSwamplandsFish();
            ViscaraWildlandsFish();
            ViscaraWildwoodsFish();


            return _builder.Build();
        }

        private void ViscaraCavernFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraCavern)

                .AddFish(FishType.Nebimonite, FishingRodType.Yew, FishingBaitType.TroutBall)
                .AddFish(FishType.Nebimonite, FishingRodType.Yew, FishingBaitType.WormLure)
                .AddFish(FishType.Nebimonite, FishingRodType.Yew, FishingBaitType.LittleWorm)
                .AddFish(FishType.Nebimonite, FishingRodType.Willow, FishingBaitType.TroutBall)
                .AddFish(FishType.Nebimonite, FishingRodType.Willow, FishingBaitType.WormLure)
                .AddFish(FishType.Nebimonite, FishingRodType.Willow, FishingBaitType.LittleWorm)
                .AddFish(FishType.Nebimonite, FishingRodType.MazeMonger, FishingBaitType.TroutBall)
                .AddFish(FishType.Nebimonite, FishingRodType.MazeMonger, FishingBaitType.WormLure)
                .AddFish(FishType.Nebimonite, FishingRodType.MazeMonger, FishingBaitType.LittleWorm)

                .AddFish(FishType.Blindfish, FishingRodType.MazeMonger, FishingBaitType.CrayfishBall)
                .AddFish(FishType.Blindfish, FishingRodType.MazeMonger, FishingBaitType.ShrimpLure)
                .AddFish(FishType.Blindfish, FishingRodType.MazeMonger, FishingBaitType.SlicedCod)

                .AddFish(FishType.NosteauHerring, FishingRodType.LuShang, FishingBaitType.GiantShellBug)
                .DaytimeOnly();
        }

        private void ViscaraDeepwoodsFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraDeepwoods);
        }

        private void ViscaraEasternSwamplandsFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraEasternSwamplands)

                .AddFish(FishType.Denizanasi, FishingRodType.Bamboo, FishingBaitType.TroutBall)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Bamboo, FishingBaitType.Lugworm)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Carbon, FishingBaitType.TroutBall)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Carbon, FishingBaitType.Lugworm)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Clothespole, FishingBaitType.TroutBall)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Clothespole, FishingBaitType.Lugworm)
                .DaytimeOnly()

                .AddFish(FishType.ShiningTrout, FishingRodType.Tranquility, FishingBaitType.LufaiseFly)
                .AddFish(FishType.ShiningTrout, FishingRodType.Tranquility, FishingBaitType.SabikiRig);
        }

        private void ViscaraLakeFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraLake)

                .AddFish(FishType.MoatCarp, FishingRodType.Bamboo, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Bamboo, FishingBaitType.Meatball)
                .AddFish(FishType.MoatCarp, FishingRodType.Carbon, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Carbon, FishingBaitType.Meatball)
                .AddFish(FishType.MoatCarp, FishingRodType.Clothespole, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Clothespole, FishingBaitType.Meatball)

                .AddFish(FishType.LampMarimo, FishingRodType.Bamboo, FishingBaitType.FlyLure)
                .AddFish(FishType.LampMarimo, FishingRodType.Bamboo, FishingBaitType.LufaiseFly)

                .AddFish(FishType.ViscaraUrchin, FishingRodType.Bamboo, FishingBaitType.InsectBall)
                .AddFish(FishType.ViscaraUrchin, FishingRodType.Bamboo, FishingBaitType.CrayfishBall)
                .AddFish(FishType.ViscaraUrchin, FishingRodType.Carbon, FishingBaitType.InsectBall)
                .AddFish(FishType.ViscaraUrchin, FishingRodType.Carbon, FishingBaitType.CrayfishBall)

                .AddFish(FishType.Greedie, FishingRodType.Composite, FishingBaitType.SardineBall)
                .AddFish(FishType.Greedie, FishingRodType.Composite, FishingBaitType.RobberRig)
                .AddFish(FishType.Greedie, FishingRodType.Ebisu, FishingBaitType.SardineBall)
                .AddFish(FishType.Greedie, FishingRodType.Ebisu, FishingBaitType.RobberRig)
                .AddFish(FishType.Greedie, FishingRodType.Fastwater, FishingBaitType.SardineBall)
                .AddFish(FishType.Greedie, FishingRodType.Fastwater, FishingBaitType.RobberRig);
        }

        private void ViscaraLakeGroundsFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraLakeGrounds)

                .AddFish(FishType.MoatCarp, FishingRodType.Bamboo, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Bamboo, FishingBaitType.Meatball)
                .AddFish(FishType.MoatCarp, FishingRodType.Carbon, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Carbon, FishingBaitType.Meatball)
                .AddFish(FishType.MoatCarp, FishingRodType.Clothespole, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Clothespole, FishingBaitType.Meatball)

                .AddFish(FishType.LampMarimo, FishingRodType.Bamboo, FishingBaitType.FlyLure)
                .AddFish(FishType.LampMarimo, FishingRodType.Bamboo, FishingBaitType.LufaiseFly)

                .AddFish(FishType.ViscaraUrchin, FishingRodType.Bamboo, FishingBaitType.InsectBall)
                .AddFish(FishType.ViscaraUrchin, FishingRodType.Bamboo, FishingBaitType.CrayfishBall)
                .AddFish(FishType.ViscaraUrchin, FishingRodType.Carbon, FishingBaitType.InsectBall)
                .AddFish(FishType.ViscaraUrchin, FishingRodType.Carbon, FishingBaitType.CrayfishBall)

                .AddFish(FishType.Greedie, FishingRodType.Composite, FishingBaitType.SardineBall)
                .AddFish(FishType.Greedie, FishingRodType.Composite, FishingBaitType.RobberRig)
                .AddFish(FishType.Greedie, FishingRodType.Ebisu, FishingBaitType.SardineBall)
                .AddFish(FishType.Greedie, FishingRodType.Ebisu, FishingBaitType.RobberRig)
                .AddFish(FishType.Greedie, FishingRodType.Fastwater, FishingBaitType.SardineBall)
                .AddFish(FishType.Greedie, FishingRodType.Fastwater, FishingBaitType.RobberRig);
        }

        private void ViscaraMountainValleyFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraMountainValley)

                .AddFish(FishType.PhanauetNewt, FishingRodType.Clothespole, FishingBaitType.LittleWorm)
                .NighttimeOnly()
                .AddFish(FishType.PhanauetNewt, FishingRodType.Clothespole, FishingBaitType.WormLure)
                .NighttimeOnly()

                .AddFish(FishType.CrystalBass, FishingRodType.Tranquility, FishingBaitType.RogueRig)
                .AddFish(FishType.CrystalBass, FishingRodType.Butterfly, FishingBaitType.RogueRig)
                .AddFish(FishType.CrystalBass, FishingRodType.LuShang, FishingBaitType.RogueRig)

                .AddFish(FishType.FatGreedie, FishingRodType.GlassFiber, FishingBaitType.SlicedCod)
                .AddFish(FishType.FatGreedie, FishingRodType.GlassFiber, FishingBaitType.DrillCalamary)
                .AddFish(FishType.FatGreedie, FishingRodType.Halcyon, FishingBaitType.SlicedCod)
                .AddFish(FishType.FatGreedie, FishingRodType.Halcyon, FishingBaitType.DrillCalamary)
                .AddFish(FishType.FatGreedie, FishingRodType.Judge, FishingBaitType.SlicedCod)
                .AddFish(FishType.FatGreedie, FishingRodType.Judge, FishingBaitType.DrillCalamary);
        }

        private void ViscaraWesternSwamplandsFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraWesternSwamplands)

                .AddFish(FishType.ShiningTrout, FishingRodType.Tranquility, FishingBaitType.LufaiseFly)
                .AddFish(FishType.ShiningTrout, FishingRodType.Tranquility, FishingBaitType.SabikiRig)

                .AddFish(FishType.ZafmlugBass, FishingRodType.Butterfly, FishingBaitType.InsectBall)
                .NighttimeOnly()
                .AddFish(FishType.ZafmlugBass, FishingRodType.Butterfly, FishingBaitType.ShrimpLure)
                .NighttimeOnly()

                .AddFish(FishType.YellowGlobe, FishingRodType.Composite, FishingBaitType.SardineBall)
                .AddFish(FishType.YellowGlobe, FishingRodType.Composite, FishingBaitType.LittleWorm)
                .AddFish(FishType.YellowGlobe, FishingRodType.Composite, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.YellowGlobe, FishingRodType.Ebisu, FishingBaitType.SardineBall)
                .AddFish(FishType.YellowGlobe, FishingRodType.Ebisu, FishingBaitType.LittleWorm)
                .AddFish(FishType.YellowGlobe, FishingRodType.Ebisu, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.YellowGlobe, FishingRodType.Fastwater, FishingBaitType.SardineBall)
                .AddFish(FishType.YellowGlobe, FishingRodType.Fastwater, FishingBaitType.LittleWorm)
                .AddFish(FishType.YellowGlobe, FishingRodType.Fastwater, FishingBaitType.PeeledCrayfish)

                .AddFish(FishType.Hoptoad, FishingRodType.Judge, FishingBaitType.LufaiseFly)
                .NighttimeOnly()
                .AddFish(FishType.Hoptoad, FishingRodType.Judge, FishingBaitType.FlyLure)
                .NighttimeOnly();
        }

        private void ViscaraWildlandsFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraWildlands)

                .AddFish(FishType.PhanauetNewt, FishingRodType.Clothespole, FishingBaitType.LittleWorm)
                .NighttimeOnly()
                .AddFish(FishType.PhanauetNewt, FishingRodType.Clothespole, FishingBaitType.WormLure)
                .NighttimeOnly()

                .AddFish(FishType.Istavrit, FishingRodType.Composite, FishingBaitType.LizardLure)
                .NighttimeOnly()
                .AddFish(FishType.Istavrit, FishingRodType.Composite, FishingBaitType.SlicedCod)
                .NighttimeOnly()
                .AddFish(FishType.Istavrit, FishingRodType.Ebisu, FishingBaitType.LizardLure)
                .NighttimeOnly()
                .AddFish(FishType.Istavrit, FishingRodType.Ebisu, FishingBaitType.SlicedCod)
                .NighttimeOnly()
                .AddFish(FishType.Istavrit, FishingRodType.Fastwater, FishingBaitType.LizardLure)
                .NighttimeOnly()
                .AddFish(FishType.Istavrit, FishingRodType.Fastwater, FishingBaitType.SlicedCod)
                .NighttimeOnly();
        }

        private void ViscaraWildwoodsFish()
        {
            _builder
                .Create(FishingLocationType.ViscaraWildwoods)

                .AddFish(FishType.Denizanasi, FishingRodType.Bamboo, FishingBaitType.TroutBall)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Bamboo, FishingBaitType.Lugworm)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Carbon, FishingBaitType.TroutBall)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Carbon, FishingBaitType.Lugworm)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Clothespole, FishingBaitType.TroutBall)
                .DaytimeOnly()
                .AddFish(FishType.Denizanasi, FishingRodType.Clothespole, FishingBaitType.Lugworm)
                .DaytimeOnly()

                .AddFish(FishType.CopperFrog, FishingRodType.Composite, FishingBaitType.LufaiseFly)
                .NighttimeOnly()
                .AddFish(FishType.CopperFrog, FishingRodType.Composite, FishingBaitType.FlyLure)
                .NighttimeOnly()

                .AddFish(FishType.Nebimonite, FishingRodType.Yew, FishingBaitType.TroutBall)
                .AddFish(FishType.Nebimonite, FishingRodType.Yew, FishingBaitType.WormLure)
                .AddFish(FishType.Nebimonite, FishingRodType.Yew, FishingBaitType.LittleWorm)
                .AddFish(FishType.Nebimonite, FishingRodType.Willow, FishingBaitType.TroutBall)
                .AddFish(FishType.Nebimonite, FishingRodType.Willow, FishingBaitType.WormLure)
                .AddFish(FishType.Nebimonite, FishingRodType.Willow, FishingBaitType.LittleWorm)
                .AddFish(FishType.Nebimonite, FishingRodType.MazeMonger, FishingBaitType.TroutBall)
                .AddFish(FishType.Nebimonite, FishingRodType.MazeMonger, FishingBaitType.WormLure)
                .AddFish(FishType.Nebimonite, FishingRodType.MazeMonger, FishingBaitType.LittleWorm)

                .AddFish(FishType.Lungfish, FishingRodType.Tranquility, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.Lungfish, FishingRodType.Tranquility, FishingBaitType.TroutBall)
                .AddFish(FishType.Lungfish, FishingRodType.LuShang, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.Lungfish, FishingRodType.LuShang, FishingBaitType.TroutBall)

                .AddFish(FishType.OgreEel, FishingRodType.LuShang, FishingBaitType.SlicedCod)
                .AddFish(FishType.OgreEel, FishingRodType.LuShang, FishingBaitType.Bluetail);
        }

    }
}
