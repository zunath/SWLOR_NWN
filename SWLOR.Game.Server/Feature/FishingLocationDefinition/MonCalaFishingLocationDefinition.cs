using System.Collections.Generic;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class MonCalaFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            MonCalaCoralIslesInnerFish();
            MonCalaCoralIslesOuterFish();
            MonCalaDacCitySurfaceFish();
            MonCalaSharptoothJungleSouthFish();
            MonCalaSharptoothJungleCavesFish();
            MonCalaSunkenhedgeSwampsFish();

            return _builder.Build();
        }

        private void MonCalaCoralIslesInnerFish()
        {
            _builder
                .Create(FishingLocationType.MonCalaCoralIslesInner)

                .AddFish(FishType.CalaLobster, FishingRodType.Bamboo, FishingBaitType.CrayfishBall)
                .AddFish(FishType.CalaLobster, FishingRodType.Bamboo, FishingBaitType.InsectBall)
                .AddFish(FishType.CalaLobster, FishingRodType.Bamboo, FishingBaitType.Meatball)
                .AddFish(FishType.CalaLobster, FishingRodType.Bamboo, FishingBaitType.WormLure)

                .AddFish(FishType.SenrohSardine, FishingRodType.Composite, FishingBaitType.RogueRig)
                .AddFish(FishType.SenrohSardine, FishingRodType.Composite, FishingBaitType.Bluetail)
                .AddFish(FishType.SenrohSardine, FishingRodType.Ebisu, FishingBaitType.RogueRig)
                .AddFish(FishType.SenrohSardine, FishingRodType.Ebisu, FishingBaitType.Bluetail)

                .AddFish(FishType.TranslucentSalpa, FishingRodType.GlassFiber, FishingBaitType.CallaLure)
                .AddFish(FishType.TranslucentSalpa, FishingRodType.Halcyon, FishingBaitType.CallaLure)
                .AddFish(FishType.TranslucentSalpa, FishingRodType.Judge, FishingBaitType.CallaLure)

                .AddFish(FishType.ForestCarp, FishingRodType.Halcyon, FishingBaitType.LittleWorm)
                .AddFish(FishType.ForestCarp, FishingRodType.Halcyon, FishingBaitType.ShellBug)
                .AddFish(FishType.ForestCarp, FishingRodType.Judge, FishingBaitType.LittleWorm)
                .AddFish(FishType.ForestCarp, FishingRodType.Judge, FishingBaitType.ShellBug)

                .AddFish(FishType.GiantCatfish, FishingRodType.MazeMonger, FishingBaitType.Bluetail)
                .DaytimeOnly()
                .AddFish(FishType.GiantCatfish, FishingRodType.MazeMonger, FishingBaitType.SardineBall)
                .DaytimeOnly()
                .AddFish(FishType.GiantCatfish, FishingRodType.MazeMonger, FishingBaitType.LittleWorm)
                .DaytimeOnly()

                .AddFish(FishType.Lakerda, FishingRodType.Butterfly, FishingBaitType.SinkingMinnow)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Butterfly, FishingBaitType.Bluetail)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Tranquility, FishingBaitType.SinkingMinnow)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Tranquility, FishingBaitType.Bluetail)
                .DaytimeOnly();
        }

        private void MonCalaCoralIslesOuterFish()
        {
            _builder
                .Create(FishingLocationType.MonCalaCoralIslesOuter)

                .AddFish(FishType.CalaLobster, FishingRodType.Bamboo, FishingBaitType.CrayfishBall)
                .AddFish(FishType.CalaLobster, FishingRodType.Bamboo, FishingBaitType.InsectBall)
                .AddFish(FishType.CalaLobster, FishingRodType.Bamboo, FishingBaitType.Meatball)
                .AddFish(FishType.CalaLobster, FishingRodType.Bamboo, FishingBaitType.WormLure)

                .AddFish(FishType.TranslucentSalpa, FishingRodType.GlassFiber, FishingBaitType.CallaLure)
                .AddFish(FishType.TranslucentSalpa, FishingRodType.Halcyon, FishingBaitType.CallaLure)
                .AddFish(FishType.TranslucentSalpa, FishingRodType.Judge, FishingBaitType.CallaLure)

                .AddFish(FishType.ForestCarp, FishingRodType.Halcyon, FishingBaitType.LittleWorm)
                .AddFish(FishType.ForestCarp, FishingRodType.Halcyon, FishingBaitType.ShellBug)
                .AddFish(FishType.ForestCarp, FishingRodType.Judge, FishingBaitType.LittleWorm)
                .AddFish(FishType.ForestCarp, FishingRodType.Judge, FishingBaitType.ShellBug)

                .AddFish(FishType.GiantCatfish, FishingRodType.MazeMonger, FishingBaitType.Bluetail)
                .DaytimeOnly()
                .AddFish(FishType.GiantCatfish, FishingRodType.MazeMonger, FishingBaitType.SardineBall)
                .DaytimeOnly()
                .AddFish(FishType.GiantCatfish, FishingRodType.MazeMonger, FishingBaitType.LittleWorm)
                .DaytimeOnly()

                .AddFish(FishType.Lakerda, FishingRodType.Butterfly, FishingBaitType.SinkingMinnow)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Butterfly, FishingBaitType.Bluetail)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Tranquility, FishingBaitType.SinkingMinnow)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Tranquility, FishingBaitType.Bluetail)
                .DaytimeOnly();
        }

        private void MonCalaDacCitySurfaceFish()
        {
            _builder
                .Create(FishingLocationType.MonCalaDacCitySurface)

                .AddFish(FishType.MoatCarp, FishingRodType.Bamboo, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Bamboo, FishingBaitType.Meatball)
                .AddFish(FishType.MoatCarp, FishingRodType.Carbon, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Carbon, FishingBaitType.Meatball)
                .AddFish(FishType.MoatCarp, FishingRodType.Clothespole, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Clothespole, FishingBaitType.Meatball)

                .AddFish(FishType.CobaltJellyfish, FishingRodType.Carbon, FishingBaitType.DrillCalamary)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Carbon, FishingBaitType.SardineBall)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Carbon, FishingBaitType.SlicedCod)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Clothespole, FishingBaitType.DrillCalamary)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Clothespole, FishingBaitType.SardineBall)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Clothespole, FishingBaitType.SlicedCod)

                .AddFish(FishType.MoorishIdol, FishingRodType.Halcyon, FishingBaitType.Bluetail)

                .AddFish(FishType.Gurnard, FishingRodType.Yew, FishingBaitType.CallaLure)

                .AddFish(FishType.CrystalBass, FishingRodType.Tranquility, FishingBaitType.RogueRig)
                .AddFish(FishType.CrystalBass, FishingRodType.Butterfly, FishingBaitType.RogueRig)
                .AddFish(FishType.CrystalBass, FishingRodType.LuShang, FishingBaitType.RogueRig);
        }

        private void MonCalaSharptoothJungleSouthFish()
        {
            _builder
                .Create(FishingLocationType.MonCalaSharptoothJungleSouth)

                .AddFish(FishType.Hamsi, FishingRodType.Carbon, FishingBaitType.ShrimpLure)
                .NighttimeOnly()
                .AddFish(FishType.Hamsi, FishingRodType.Carbon, FishingBaitType.ShellBug)
                .NighttimeOnly()
                .AddFish(FishType.Hamsi, FishingRodType.Clothespole, FishingBaitType.ShrimpLure)
                .NighttimeOnly()
                .AddFish(FishType.Hamsi, FishingRodType.Clothespole, FishingBaitType.ShellBug)
                .NighttimeOnly()

                .AddFish(FishType.WhiteLobster, FishingRodType.GlassFiber, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.WhiteLobster, FishingRodType.GlassFiber, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.WhiteLobster, FishingRodType.Halcyon, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.WhiteLobster, FishingRodType.Halcyon, FishingBaitType.PeeledCrayfish)

                .AddFish(FishType.TigerCod, FishingRodType.Willow, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.TigerCod, FishingRodType.Willow, FishingBaitType.RogueRig)
                .AddFish(FishType.TigerCod, FishingRodType.Yew, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.TigerCod, FishingRodType.Yew, FishingBaitType.RogueRig);
        }

        private void MonCalaSharptoothJungleCavesFish()
        {
            _builder
                .Create(FishingLocationType.MonCalaSharptoothJungleCaves)

                .AddFish(FishType.RaKaznarShellfish, FishingRodType.Ebisu, FishingBaitType.GiantShellBug)
                .DaytimeOnly()
                .AddFish(FishType.RaKaznarShellfish, FishingRodType.Fastwater, FishingBaitType.GiantShellBug)
                .DaytimeOnly()

                .AddFish(FishType.Blindfish, FishingRodType.MazeMonger, FishingBaitType.CrayfishBall)
                .AddFish(FishType.Blindfish, FishingRodType.MazeMonger, FishingBaitType.ShrimpLure)
                .AddFish(FishType.Blindfish, FishingRodType.MazeMonger, FishingBaitType.SlicedCod)

                .AddFish(FishType.Bonefish, FishingRodType.MazeMonger, FishingBaitType.SabikiRig)
                .AddFish(FishType.Bonefish, FishingRodType.MazeMonger, FishingBaitType.GiantShellBug)
                .AddFish(FishType.Bonefish, FishingRodType.Yew, FishingBaitType.SabikiRig)
                .AddFish(FishType.Bonefish, FishingRodType.Yew, FishingBaitType.GiantShellBug)

                .AddFish(FishType.RuddySeema, FishingRodType.Butterfly, FishingBaitType.RogueRig)
                .NighttimeOnly()
                .AddFish(FishType.RuddySeema, FishingRodType.Butterfly, FishingBaitType.SabikiRig)
                .NighttimeOnly()
                .AddFish(FishType.RuddySeema, FishingRodType.Tranquility, FishingBaitType.RogueRig)
                .NighttimeOnly()
                .AddFish(FishType.RuddySeema, FishingRodType.Tranquility, FishingBaitType.SabikiRig)
                .NighttimeOnly()
                .AddFish(FishType.RuddySeema, FishingRodType.LuShang, FishingBaitType.RogueRig)
                .NighttimeOnly()
                .AddFish(FishType.RuddySeema, FishingRodType.LuShang, FishingBaitType.SabikiRig)
                .NighttimeOnly();
        }

        private void MonCalaSunkenhedgeSwampsFish()
        {
            _builder
                .Create(FishingLocationType.MonCalaSunkenhedgeSwamps)

                .AddFish(FishType.Crayfish, FishingRodType.Clothespole, FishingBaitType.RobberRig)
                .AddFish(FishType.Crayfish, FishingRodType.Clothespole, FishingBaitType.ShellBug)
                .AddFish(FishType.Crayfish, FishingRodType.Clothespole, FishingBaitType.LittleWorm)

                .AddFish(FishType.Mackerel, FishingRodType.Ebisu, FishingBaitType.SabikiRig)

                .AddFish(FishType.CopperFrog, FishingRodType.Composite, FishingBaitType.LufaiseFly)
                .NighttimeOnly()
                .AddFish(FishType.CopperFrog, FishingRodType.Composite, FishingBaitType.FlyLure)
                .NighttimeOnly()

                .AddFish(FishType.Pipira, FishingRodType.Willow, FishingBaitType.DrillCalamary)
                .DaytimeOnly()
                .AddFish(FishType.Pipira, FishingRodType.Willow, FishingBaitType.RobberRig)
                .DaytimeOnly()

                .AddFish(FishType.Yayinbaligi, FishingRodType.Willow, FishingBaitType.Lugworm)
                .AddFish(FishType.Yayinbaligi, FishingRodType.Willow, FishingBaitType.Meatball);
        }

    }
}