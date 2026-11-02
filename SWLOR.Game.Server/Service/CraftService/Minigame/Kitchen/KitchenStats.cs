namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// What the cook brings to the pot: skill rank, gear and Chef perks. Craftsmanship powers Doneness, Control narrows the spread of a
/// push, and CP is gear CP plus 0.75 per rank, with the rank passive on top. The numbers a perk changes are read here, so the engine
/// asks this record and never a perk.
/// </summary>
/// <param name="Rank">The cook's Agriculture rank.</param>
/// <param name="Craftsmanship">Gear Craftsmanship, which powers Doneness.</param>
/// <param name="Control">Gear Control, which narrows the spread of a push.</param>
/// <param name="GearCp">The CP from the cook's gear.</param>
/// <param name="Perks">The numbers the cook's Chef perks change; null cooks the Kitchen as tuned.</param>
public sealed record KitchenStats(int Rank, int Craftsmanship, int Control, int GearCp, KitchenPerkStats Perks = null)
{
    /// <summary>The numbers the cook's Chef perks change.</summary>
    public KitchenPerkStats Perks { get; init; } = Perks ?? KitchenPerkStats.None;

    /// <summary>The CP the cook starts with.</summary>
    public int MaxCp
    {
        get
        {
            var cp = (int)(GearCp + Rank * KitchenTuning.CpPerRank);
            return Rank >= KitchenTuning.CpPassiveRank ? cp + KitchenTuning.CpPassiveBonus : cp;
        }
    }

    /// <summary>
    /// How far, in percent, a push lands from its listed strength with this Control: the light push is this far below it and the
    /// heavy push this far above. More Control narrows it but never removes it. Sharp Palate takes points off the base spread before
    /// Control narrows it, and a switched-on Seasoned Palate narrows the result by a share.
    /// </summary>
    public int PushSpreadPercent
    {
        get
        {
            var baseSpread = Math.Max(0, KitchenTuning.BasePushSpreadPercent - Math.Max(0, Perks.PushSpreadReduction));
            var spread = baseSpread * 100.0 / (100 + KitchenTuning.ControlPerSpreadStep * Math.Max(0, Control));
            if (Perks.SeasonedPalateOn)
                spread *= (100 - Math.Clamp(Perks.SeasonedPalateSpreadPercent, 0, 100)) / 100.0;

            return (int)Math.Round(spread, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>The push of each free pantry seasoning.</summary>
    public int PantryPush => KitchenTuning.PantryPush + Math.Max(0, Perks.PantryPushBonus);

    /// <summary>The Doneness a Simmer gives before the cook's Craftsmanship and the recipe's level: Low heat simmers gain from Slow Cooking.</summary>
    public double SimmerBase(KitchenHeat heat) => heat switch
    {
        KitchenHeat.Low => KitchenTuning.SimmerLowDoneness * (100 + Math.Max(0, Perks.LowHeatDonenessPercent)) / 100.0,
        KitchenHeat.Medium => KitchenTuning.SimmerMediumDoneness,
        _ => 0
    };

    /// <summary>The Doneness a Stir gives before the cook's Craftsmanship and the recipe's level: a share of a Simmer's at the heat, none at High.</summary>
    public double StirBase(KitchenHeat heat) =>
        heat == KitchenHeat.High ? 0 : SimmerBase(heat) * Math.Max(0, Perks.StirDonenessPercent) / 100.0;

    /// <summary>The CP a Stir costs: never less than 1.</summary>
    public int StirCp => Math.Max(1, KitchenTuning.StirCp - Math.Max(0, Perks.StirCpReduction));

    /// <summary>The CP a Reduce costs: never less than 1.</summary>
    public int ReduceCp => Math.Max(1, KitchenTuning.ReduceCp - Math.Max(0, Perks.ReduceCpReduction));

    /// <summary>The chance, in percent, that a turn announces Peak Aroma.</summary>
    public int PeakAromaChancePercent => Math.Clamp(KitchenTuning.PeakAromaChancePercent + Perks.PeakAromaChancePoints, 0, 100);

    /// <summary>The share, in percent of the announced drift, that the heat lets through once the cook's perks have weakened it.</summary>
    public double DriftPercent(KitchenHeat heat)
    {
        var reduction = heat switch
        {
            KitchenHeat.Medium => Perks.MediumDriftReductionPercent,
            KitchenHeat.High => Perks.HighDriftReductionPercent,
            _ => 0
        };

        return KitchenTuning.DriftPercent(heat) * (100 - Math.Clamp(reduction, 0, 100)) / 100.0;
    }

    /// <summary>The drift that moves the flavor at the end of a turn when the heat is <paramref name="heat"/> and <paramref name="announced"/> was announced.</summary>
    public FlavorVector Drift(FlavorVector announced, KitchenHeat heat)
    {
        // The share is a multiple of 0.5 percent, so scale by hundredths to keep the arithmetic in whole numbers.
        var share = (int)Math.Round(DriftPercent(heat) * 100, MidpointRounding.AwayFromZero);
        return announced.ScaledBy(share, 10000);
    }

    /// <summary>How much the recipe's level against the cook's rank scales Doneness.</summary>
    public double LevelFactor(int recipeLevel) => Math.Clamp(
        1 + KitchenTuning.LevelFactorPerRank * (Rank - recipeLevel),
        KitchenTuning.LevelFactorMin,
        KitchenTuning.LevelFactorMax);

    /// <summary>The Doneness an action of the given base strength gives this cook on this recipe.</summary>
    public int Doneness(double baseAmount, int recipeLevel) => (int)Math.Round(
        baseAmount * (1 + KitchenTuning.CraftsmanshipDonenessPerPoint * Math.Max(0, Craftsmanship)) * LevelFactor(recipeLevel),
        MidpointRounding.AwayFromZero);
}
