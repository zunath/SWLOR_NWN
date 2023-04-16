using System.Collections.Generic;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Feature.FishingLocationDefinition
{
    public class DantooineFishingLocationDefinition : IFishingLocationDefinition
    {
        private readonly FishingLocationBuilder _builder = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            DantooineLakeFish();
            DantooineMountainJunglesFish();
            DantooineCanyonFish();
            DantooineSouthFieldsFish();
            DantooineForsakenJunglesFish();

            return _builder.Build();
        }

        private void DantooineLakeFish()
        {
            _builder
                .Create(FishingLocationType.DantooineLake)

                .AddFish(FishType.Crayfish, FishingRodType.Clothespole, FishingBaitType.RobberRig)
                .AddFish(FishType.Crayfish, FishingRodType.Clothespole, FishingBaitType.ShellBug)
                .AddFish(FishType.Crayfish, FishingRodType.Clothespole, FishingBaitType.LittleWorm)

                .AddFish(FishType.Pipira, FishingRodType.Willow, FishingBaitType.DrillCalamary)
                .DaytimeOnly()
                .AddFish(FishType.Pipira, FishingRodType.Willow, FishingBaitType.RobberRig)
                .DaytimeOnly()

                .AddFish(FishType.ZafmlugBass, FishingRodType.Butterfly, FishingBaitType.InsectBall)
                .NighttimeOnly()
                .AddFish(FishType.ZafmlugBass, FishingRodType.Butterfly, FishingBaitType.ShrimpLure)
                .NighttimeOnly();
        }

        private void DantooineMountainJunglesFish()
        {
            _builder
                .Create(FishingLocationType.DantooineMountainJungles)

                .AddFish(FishType.TinyGoldfish, FishingRodType.GlassFiber, FishingBaitType.LittleWorm)

                .AddFish(FishType.TricoloredCarp, FishingRodType.Yew, FishingBaitType.InsectBall)
                .AddFish(FishType.TricoloredCarp, FishingRodType.Yew, FishingBaitType.LizardLure)
                .AddFish(FishType.TricoloredCarp, FishingRodType.Willow, FishingBaitType.InsectBall)
                .AddFish(FishType.TricoloredCarp, FishingRodType.Willow, FishingBaitType.LizardLure)

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

        private void DantooineCanyonFish()
        {
            _builder
                .Create(FishingLocationType.DantooineCanyon)

                .AddFish(FishType.CobaltJellyfish, FishingRodType.Carbon, FishingBaitType.DrillCalamary)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Carbon, FishingBaitType.SardineBall)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Carbon, FishingBaitType.SlicedCod)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Clothespole, FishingBaitType.DrillCalamary)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Clothespole, FishingBaitType.SardineBall)
                .AddFish(FishType.CobaltJellyfish, FishingRodType.Clothespole, FishingBaitType.SlicedCod)

                .AddFish(FishType.Bonefish, FishingRodType.MazeMonger, FishingBaitType.SabikiRig)
                .AddFish(FishType.Bonefish, FishingRodType.MazeMonger, FishingBaitType.GiantShellBug)
                .AddFish(FishType.Bonefish, FishingRodType.Yew, FishingBaitType.SabikiRig)
                .AddFish(FishType.Bonefish, FishingRodType.Yew, FishingBaitType.GiantShellBug)

                .AddFish(FishType.Frigorifish, FishingRodType.Tranquility, FishingBaitType.DrillCalamary)
                .NighttimeOnly()
                .AddFish(FishType.Frigorifish, FishingRodType.Tranquility, FishingBaitType.GiantShellBug)
                .NighttimeOnly()
                .AddFish(FishType.Frigorifish, FishingRodType.LuShang, FishingBaitType.DrillCalamary)
                .NighttimeOnly()
                .AddFish(FishType.Frigorifish, FishingRodType.LuShang, FishingBaitType.GiantShellBug)
                .NighttimeOnly();
        }

        private void DantooineSouthFieldsFish()
        {
            _builder
                .Create(FishingLocationType.DantooineSouthFields)

                .AddFish(FishType.MuddySiredon, FishingRodType.Ebisu, FishingBaitType.Lugworm)
                .AddFish(FishType.MuddySiredon, FishingRodType.Ebisu, FishingBaitType.SinkingMinnow)

                .AddFish(FishType.Yorchete, FishingRodType.GlassFiber, FishingBaitType.CallaLure)
                .AddFish(FishType.Yorchete, FishingRodType.GlassFiber, FishingBaitType.SabikiRig)
                .AddFish(FishType.Yorchete, FishingRodType.Judge, FishingBaitType.CallaLure)
                .AddFish(FishType.Yorchete, FishingRodType.Judge, FishingBaitType.SabikiRig)

                .AddFish(FishType.Lungfish, FishingRodType.Tranquility, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.Lungfish, FishingRodType.Tranquility, FishingBaitType.TroutBall)
                .AddFish(FishType.Lungfish, FishingRodType.LuShang, FishingBaitType.PeeledCrayfish)
                .AddFish(FishType.Lungfish, FishingRodType.LuShang, FishingBaitType.TroutBall)

                .AddFish(FishType.Lakerda, FishingRodType.Butterfly, FishingBaitType.SinkingMinnow)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Butterfly, FishingBaitType.Bluetail)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Tranquility, FishingBaitType.SinkingMinnow)
                .DaytimeOnly()
                .AddFish(FishType.Lakerda, FishingRodType.Tranquility, FishingBaitType.Bluetail)
                .DaytimeOnly();
        }

        private void DantooineForsakenJunglesFish()
        {
            _builder
                .Create(FishingLocationType.DantooineForsakenJungles)

                .AddFish(FishType.MoatCarp, FishingRodType.Bamboo, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Bamboo, FishingBaitType.Meatball)
                .AddFish(FishType.MoatCarp, FishingRodType.Carbon, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Carbon, FishingBaitType.Meatball)
                .AddFish(FishType.MoatCarp, FishingRodType.Clothespole, FishingBaitType.LittleWorm)
                .AddFish(FishType.MoatCarp, FishingRodType.Clothespole, FishingBaitType.Meatball)

                .AddFish(FishType.DathomirSardine, FishingRodType.Carbon, FishingBaitType.SinkingMinnow)
                .AddFish(FishType.DathomirSardine, FishingRodType.Carbon, FishingBaitType.TroutBall)

                .AddFish(FishType.Hoptoad, FishingRodType.Judge, FishingBaitType.LufaiseFly)
                .NighttimeOnly()
                .AddFish(FishType.Hoptoad, FishingRodType.Judge, FishingBaitType.FlyLure)
                .NighttimeOnly();
        }

    }
}

