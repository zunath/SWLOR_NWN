using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// A casual player who reads the window but never looks past the next move: Tire is a free reel, Thrash is for Hold, and against a
    /// Run or a Dive the player reels while the line has room to spare and gives line when it has not. It uses only what the window shows
    /// (the announced move and the previews), so it marks how much of the catch rate comes from planning ahead.
    /// </summary>
    public sealed class RuleOfThumbPolicy : ICraftPolicy
    {
        /// <summary>The line strength left after a Reel that the player will accept, as a share of the cap.</summary>
        public const double SpareShare = 0.2;

        public string Name => "Rule of thumb";

        public string Choose(ICraftMinigameEngine engine)
        {
            if (engine is not LineEngine line)
                throw new ArgumentException("The rule-of-thumb player fishes with the Line.", nameof(engine));

            switch (line.Move)
            {
                case FishMove.Tire:
                    return LineActionIds.Reel;
                case FishMove.Thrash:
                    return LineActionIds.Hold;
            }

            var reel = line.Preview(LineActionIds.Reel).Outcomes[0];
            var strengthLeft = reel.Change(MinigameMeterIds.Integrity).After;
            return reel.FinishesItem || strengthLeft >= SpareShare * line.TensionCap ? LineActionIds.Reel : LineActionIds.GiveLine;
        }
    }
}
