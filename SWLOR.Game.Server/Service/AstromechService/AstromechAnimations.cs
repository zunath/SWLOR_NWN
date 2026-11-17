using System.Collections.Generic;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// The scripted animations an astromech chassis has a clip for. Everything else is a humanoid motion
    /// (sitting, bowing, dancing and so on) that the chassis cannot play.
    /// </summary>
    public static class AstromechAnimations
    {
        public const string EmoteRefusal = "An astromech chassis cannot perform that emote.";
        public const string SitRefusal = "An astromech cannot sit there.";

        /// <summary>The pose an astromech holds while resting: powered down on the floor.</summary>
        public const Animation Rest = Animation.LoopingDeadBack;

        /// <summary>
        /// The pose the stand-in of an astromech holds at a ship's controls, a crew station or the astromech socket:
        /// plugged in with its tool arm out, never seated.
        /// </summary>
        public const Animation Station = Animation.LoopingConjure1;

        /// <summary>
        /// The motion an astromech makes while it works at something (crafting, gathering, fishing, using an item):
        /// its tool arm out, probing. It stands in for the authored humanoid working animations. While it fishes, the
        /// same animation plays the rod clips instead (<see cref="AstromechWorkAnimation"/>).
        /// </summary>
        public const Animation Work = Animation.LoopingGetMid;

        private static readonly HashSet<Animation> Supported = new()
        {
            Animation.LoopingPause,
            Animation.LoopingPause2,
            Animation.LoopingDeadFront,
            Animation.LoopingDeadBack,
            Animation.LoopingConjure1,
            Animation.LoopingConjure2,
            Animation.LoopingGetLow,
            Animation.LoopingGetMid,
            Animation.LoopingSpasm,
            Animation.FireForgetHeadTurnLeft,
            Animation.FireForgetHeadTurnRight,
            Animation.FireForgetTaunt,
            Animation.FireForgetVictory1,
            Animation.FireForgetVictory2,
            Animation.FireForgetVictory3,
            Animation.FireForgetDodgeSide,
            Animation.FireForgetDodgeDuck,
            Animation.FireForgetSpasm
        };

        /// <summary>
        /// The chassis clips (entry, then loop) that stand in for an authored fishing clip. The chassis carries a rod that
        /// only these clips bring out: the cast and the wait, the three line actions, and landing the fish.
        /// </summary>
        private static readonly Dictionary<string, (string Entry, string Loop)> RodClips = new()
        {
            [AuthoredAnimation.Fishing6.Name] = ("cfish", "cfishlp"),
            [AuthoredAnimation.Fishing7.Name] = ("cfish", "cfishlp"),
            [AuthoredAnimation.Fishing8.Name] = ("cfish", "cfishlp"),
            [AuthoredAnimation.Reel.Name] = ("cfishin", "cfishreel"),
            [AuthoredAnimation.GiveLine.Name] = ("cfishin", "cfishgive"),
            [AuthoredAnimation.HoldRod.Name] = ("cfishin", "cfishhold"),
            [AuthoredAnimation.LandFish.Name] = ("cfishland", "cfishlandlp")
        };

        public static bool CanPlay(Animation animation)
        {
            return Supported.Contains(animation);
        }

        /// <summary>True when the authored clip is a fishing clip, with the chassis rod clips that replace it.</summary>
        public static bool TryGetRodClips(AnimationClip clip, out (string Entry, string Loop) clips)
        {
            clips = default;
            return clip != null && RodClips.TryGetValue(clip.Name, out clips);
        }
    }
}
