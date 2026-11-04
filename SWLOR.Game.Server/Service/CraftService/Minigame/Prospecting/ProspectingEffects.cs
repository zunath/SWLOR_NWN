using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// What a harvester's perks and active style change in a Prospecting session, as the stat totals they grant. The session reads them once
/// when it begins (and again while the harvester has not acted, if the style changes) and the engine reads only this record, never a
/// perk, so previews and results stay pure. Zero (or false) changes nothing.
/// </summary>
/// <param name="StrikeStabilityReduction">Stability a Strike costs less, to a minimum of 1. A style may make it negative.</param>
/// <param name="StrikeTimeReductionTenths">Tenths of a second a Strike takes less.</param>
/// <param name="SeamWorkTimeReductionTenths">Tenths of a second a Survey and an Extract take less. A style may make it negative.</param>
/// <param name="HeavyStrikeTimeReductionTenths">Tenths of a second a Heavy Strike takes less.</param>
/// <param name="RichExtractTimeReductionTenths">Tenths of a second an Extract of a rich seam takes less.</param>
/// <param name="SeesSeamTypes">Whether the window shows how many seams the node holds and what each is, before they are found.</param>
/// <param name="MotherLode">Whether the first Strike or Heavy Strike of each node takes <see cref="ProspectingPerkRules.MotherLodeTimePercent"/> percent of its time.</param>
/// <param name="ShatterReduction">Percentage points off a Heavy Strike's chance to shatter a gem pocket that has not been surveyed.</param>
/// <param name="SurveyReveals">Seams a Survey reveals beyond its rank's, never more than a node holds.</param>
/// <param name="PocketsFirst">Whether a Survey reveals gem pockets before rich seams.</param>
public sealed record ProspectingEffects(
    int StrikeStabilityReduction = 0,
    int StrikeTimeReductionTenths = 0,
    int SeamWorkTimeReductionTenths = 0,
    int HeavyStrikeTimeReductionTenths = 0,
    int RichExtractTimeReductionTenths = 0,
    bool SeesSeamTypes = false,
    bool MotherLode = false,
    int ShatterReduction = 0,
    int SurveyReveals = 0,
    bool PocketsFirst = false)
{
    /// <summary>A harvester with no perks and no style.</summary>
    public static readonly ProspectingEffects None = new();

    /// <summary>The effects of a harvester whose stat totals <paramref name="read"/> returns.</summary>
    public static ProspectingEffects FromStats(Func<StatType, int> read)
    {
        if (read == null)
            throw new ArgumentNullException(nameof(read));

        return new ProspectingEffects(
            read(StatType.ProspectingStrikeStabilityReduction),
            Math.Max(0, read(StatType.ProspectingStrikeTimeReduction)),
            read(StatType.ProspectingSeamWorkTimeReduction),
            Math.Max(0, read(StatType.ProspectingHeavyStrikeTimeReduction)),
            Math.Max(0, read(StatType.ProspectingRichExtractTimeReduction)),
            read(StatType.ProspectingSeamSightUnlocked) > 0,
            read(StatType.ProspectingMotherLodeUnlocked) > 0,
            Math.Max(0, read(StatType.ProspectingShatterReduction)),
            Math.Max(0, read(StatType.ProspectingSurveyReveals)),
            read(StatType.ProspectingPocketsFirstUnlocked) > 0);
    }
}
