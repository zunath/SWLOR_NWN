using SWLOR.Game.Server.Service.FishingService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class HutlarFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            HutlarCloningTestSiteFish();
            HutlarQionFoothillsFish();
            HutlarQionTundraFish();
            HutlarQionValleyFish();

            return _builder.Build();
        }

        private void HutlarCloningTestSiteFish()
        {
            _builder
                .Create(FishingLocationType.HutlarCloningTestSite)

                .AddFish(FishType.LampMarimo, FishingRodType.Bamboo, FishingBaitType.FlyLure)
                .AddFish(FishType.LampMarimo, FishingRodType.Bamboo, FishingBaitType.LufaiseFly)

                .AddFish(FishType.Hamsi, FishingRodType.Carbon, FishingBaitType.ShrimpLure)
                .NighttimeOnly()
                .AddFish(FishType.Hamsi, FishingRodType.Carbon, FishingBaitType.ShellBug)
                .NighttimeOnly()
                .AddFish(FishType.Hamsi, FishingRodType.Clothespole, FishingBaitType.ShrimpLure)
                .NighttimeOnly()
                .AddFish(FishType.Hamsi, FishingRodType.Clothespole, FishingBaitType.ShellBug)
                .NighttimeOnly()

                .AddFish(FishType.NosteauHerring, FishingRodType.LuShang, FishingBaitType.GiantShellBug)
                .DaytimeOnly();
        }

        private void HutlarQionFoothillsFish()
        {
            _builder
                .Create(FishingLocationType.HutlarQionFoothills)

                .AddFish(FishType.Bibikibo, FishingRodType.Bamboo, FishingBaitType.CallaLure)
                .AddFish(FishType.Bibikibo, FishingRodType.Clothespole, FishingBaitType.CallaLure)

                .AddFish(FishType.SenrohSardine, FishingRodType.Composite, FishingBaitType.RogueRig)
                .AddFish(FishType.SenrohSardine, FishingRodType.Composite, FishingBaitType.Bluetail)
                .AddFish(FishType.SenrohSardine, FishingRodType.Ebisu, FishingBaitType.RogueRig)
                .AddFish(FishType.SenrohSardine, FishingRodType.Ebisu, FishingBaitType.Bluetail)

                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.LittleWorm)
                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.CrayfishBall)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.LittleWorm)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.CrayfishBall)

                .AddFish(FishType.ChevalSalmon, FishingRodType.Halcyon, FishingBaitType.WormLure)
                .AddFish(FishType.ChevalSalmon, FishingRodType.Halcyon, FishingBaitType.Meatball)

                .AddFish(FishType.FatGreedie, FishingRodType.GlassFiber, FishingBaitType.SlicedCod)
                .AddFish(FishType.FatGreedie, FishingRodType.GlassFiber, FishingBaitType.DrillCalamary)
                .AddFish(FishType.FatGreedie, FishingRodType.Halcyon, FishingBaitType.SlicedCod)
                .AddFish(FishType.FatGreedie, FishingRodType.Halcyon, FishingBaitType.DrillCalamary)
                .AddFish(FishType.FatGreedie, FishingRodType.Judge, FishingBaitType.SlicedCod)
                .AddFish(FishType.FatGreedie, FishingRodType.Judge, FishingBaitType.DrillCalamary)

                .AddFish(FishType.Deadmoiselle, FishingRodType.Willow, FishingBaitType.ShellBug)
                .AddFish(FishType.Deadmoiselle, FishingRodType.Willow, FishingBaitType.RobberRig)
                .AddFish(FishType.Deadmoiselle, FishingRodType.MazeMonger, FishingBaitType.ShellBug)
                .AddFish(FishType.Deadmoiselle, FishingRodType.MazeMonger, FishingBaitType.RobberRig);
        }

        private void HutlarQionTundraFish()
        {
            _builder
                .Create(FishingLocationType.HutlarQionTundra)

                .AddFish(FishType.YellowGlobe, FishingRodType.Composite, FishingBaitType.SardineBall)
                .AddFish(FishType.YellowGlobe, FishingRodType.Composite, FishingBaitType.LittleWorm)
                .AddFish(FishType.YellowGlobe, FishingRodType.Composite, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.YellowGlobe, FishingRodType.Ebisu, FishingBaitType.SardineBall)
                .AddFish(FishType.YellowGlobe, FishingRodType.Ebisu, FishingBaitType.LittleWorm)
                .AddFish(FishType.YellowGlobe, FishingRodType.Ebisu, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.YellowGlobe, FishingRodType.Fastwater, FishingBaitType.SardineBall)
                .AddFish(FishType.YellowGlobe, FishingRodType.Fastwater, FishingBaitType.LittleWorm)
                .AddFish(FishType.YellowGlobe, FishingRodType.Fastwater, FishingBaitType.PeeledCrayfish)

                .AddFish(FishType.MuddySiredon, FishingRodType.Ebisu, FishingBaitType.Lugworm)
                .AddFish(FishType.MuddySiredon, FishingRodType.Ebisu, FishingBaitType.SinkingMinnow)

                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.LittleWorm)
                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.CrayfishBall)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.LittleWorm)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.CrayfishBall)

                .AddFish(FishType.Yayinbaligi, FishingRodType.Willow, FishingBaitType.Lugworm)
                .AddFish(FishType.Yayinbaligi, FishingRodType.Willow, FishingBaitType.Meatball)

                .AddFish(FishType.Blowfish, FishingRodType.Butterfly, FishingBaitType.ShrimpLure);
        }

        private void HutlarQionValleyFish()
        {
            _builder
                .Create(FishingLocationType.HutlarQionValley)

                .AddFish(FishType.BastionSweeper, FishingRodType.Composite, FishingBaitType.LizardLure)
                .AddFish(FishType.BastionSweeper, FishingRodType.Composite, FishingBaitType.WormLure)
                .AddFish(FishType.BastionSweeper, FishingRodType.Composite, FishingBaitType.CallaLure)

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
                .NighttimeOnly()

                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.LittleWorm)
                .AddFish(FishType.Quus, FishingRodType.GlassFiber, FishingBaitType.CrayfishBall)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.LittleWorm)
                .AddFish(FishType.Quus, FishingRodType.Judge, FishingBaitType.CrayfishBall);
        }

    }
}
