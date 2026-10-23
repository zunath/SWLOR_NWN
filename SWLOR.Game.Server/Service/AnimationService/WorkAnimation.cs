using System;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.AnimationService;

/// <summary>The gathering actions that have an authored clip. The Prospecting and Search engines map their actions onto these.</summary>
public enum GatheringAction
{
    /// <summary>Prospecting: Survey reveals a seam.</summary>
    Survey = 1,

    /// <summary>Prospecting: Strike.</summary>
    Strike = 2,

    /// <summary>Prospecting: Heavy Strike.</summary>
    HeavyStrike = 3,

    /// <summary>Prospecting: Extract claims a revealed seam.</summary>
    Extract = 4,

    /// <summary>Search: Search a spot of the board.</summary>
    Search = 5,

    /// <summary>Search: Dig Deeper into the same spot.</summary>
    Dig = 6
}

/// <summary>
/// Which authored clip each crafting-revamp action plays. Stations get one looping work clip per craft, gathering gets one clip
/// per action, and the Line's fight gets one per move plus the landing. The clips are installed for the playable species on the
/// normal phenotype (<c>design/animations/crafting-stations</c>, <c>gathering</c> and <c>fishing</c>). A lookup returns null for
/// an action that has no clip, so callers skip the animation instead of guessing.
/// </summary>
public static class WorkAnimation
{
    /// <summary>
    /// The longest a station loop is asked to run. A looping clip repeats natively for the playback's duration, which
    /// <c>NamedAnimation</c> caps at ten minutes; a longer session simply stops animating.
    /// </summary>
    public const float StationLoopSeconds = 600f;

    /// <summary>
    /// The looping work clip of a station craft: Smithery, Engineering, Fabrication, cooking (Agriculture) and Espionage. Null for any
    /// other skill. Quick craft plays the same clip.
    /// </summary>
    public static AnimationClip ForStation(SkillType skill) => skill switch
    {
        SkillType.Smithery => AuthoredAnimation.ForgeWork,
        SkillType.Engineering => AuthoredAnimation.Circuitry,
        SkillType.Fabrication => AuthoredAnimation.Fabricate,
        SkillType.Agriculture => AuthoredAnimation.Cooking,
        SkillType.Espionage => AuthoredAnimation.Compound,
        _ => null
    };

    /// <summary>
    /// The one-shot clip of a gathering action. Play it once when the action resolves:
    /// <c>NamedAnimation.Play(player, WorkAnimation.ForGathering(action))</c>.
    /// </summary>
    public static AnimationClip ForGathering(GatheringAction action) => action switch
    {
        GatheringAction.Survey => AuthoredAnimation.Survey,
        GatheringAction.Strike => AuthoredAnimation.Strike,
        GatheringAction.HeavyStrike => AuthoredAnimation.HvyStrike,
        GatheringAction.Extract => AuthoredAnimation.Extract,
        GatheringAction.Search => AuthoredAnimation.Search,
        GatheringAction.Dig => AuthoredAnimation.Dig,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "No clip is authored for this gathering action.")
    };

    /// <summary>
    /// The clip of a Line action, by its id in <see cref="LineActionIds"/>: Reel, Give Line and Hold. Set Hook has none, so it
    /// returns null. A fish landed by the last action plays <see cref="FishLanded"/> instead.
    /// </summary>
    public static AnimationClip ForFishing(string lineActionId) => lineActionId switch
    {
        LineActionIds.Reel => AuthoredAnimation.Reel,
        LineActionIds.GiveLine => AuthoredAnimation.GiveLine,
        LineActionIds.Hold => AuthoredAnimation.HoldRod,
        _ => null
    };

    /// <summary>The clip of landing the fish.</summary>
    public static AnimationClip FishLanded => AuthoredAnimation.LandFish;
}
