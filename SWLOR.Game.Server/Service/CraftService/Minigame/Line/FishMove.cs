namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// What the hooked fish does on a turn. The window announces it before the player acts, and every action answers it differently.
/// </summary>
public enum FishMove
{
    /// <summary>The fish bolts: it takes line unless the player reels against it, and then the strain is heavy.</summary>
    Run = 1,

    /// <summary>The fish plunges: Tension rises whatever the player does, except that giving line absorbs half of it.</summary>
    Dive = 2,

    /// <summary>The fish thrashes: a spike of Tension that Hold shrugs off and Give Line halves.</summary>
    Thrash = 3,

    /// <summary>The fish rests and loses Stamina; a Reel pulls it in further.</summary>
    Tire = 4
}
