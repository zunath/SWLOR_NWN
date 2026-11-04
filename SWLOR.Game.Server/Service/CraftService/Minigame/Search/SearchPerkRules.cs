namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>
/// The magnitudes of the Treasure Seeker perks and style that act on the Search board. Every value is a starting value for the
/// simulator to tune; the perk definitions, the style status effect and the Design Bible script read them and nothing else, so a tuning
/// pass changes this file only. A ranked perk's higher rank restates what the rank below it gave, because only the purchased rank's stat
/// bonuses apply.
/// </summary>
public static class SearchPerkRules
{
    // Fine Trowel (Gathering 5 and 40): added to the multiplier a Dig Deeper find puts on the weight of the rare entries.
    public const int FineTrowelRank1 = 1;
    public const int FineTrowelRank2 = 2;

    // Steady Dig (Gathering 15): percent off the risk that a Dig Deeper ruins the spot.
    public const int SteadyDigRiskReductionPercent = 15;

    // Keen Eye (Gathering 25): points added to the chance a Search finds.
    public const int KeenEyePoints = 1;

    // Careful Hands (style), Search half: fewer finds, rarer digs.
    public const int CarefulHandsSearchChancePoints = -1;
    public const int CarefulHandsDigRareWeight = 3;

    // Rich Pickings (Gathering 45): added to the multiplier an ordinary Search find puts on the weight of the rare entries.
    public const int RichPickingsRareWeight = 1;

    // Jackpot (Gathering 50): added to the rare-entry multiplier of the first Dig Deeper find of each board.
    public const int JackpotRareWeight = 4;
}
