using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>A source of chance whose every roll is the same number, clamped into the range asked for.</summary>
internal sealed class FixedRolls : IMinigameRandom
{
    private readonly int _value;

    public FixedRolls(int value)
    {
        _value = value;
    }

    public int Calls { get; private set; }

    public int D100()
    {
        Calls++;
        return _value;
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        Calls++;
        return Math.Clamp(_value, minInclusive, maxExclusive - 1);
    }
}

/// <summary>Small recipes, cooks and rolls the Kitchen tests share.</summary>
internal static class KitchenTestKit
{
    public static readonly FlavorVector Center = new(30, 0, 16);
    public const int StartingFreshness = 80;
    public const int Level = 20;

    public static KitchenIngredientLine Meat(int quantity = 3) => new(
        "mynock_meat", "Mynock Meat", quantity, new KitchenComponentFlavor(KitchenIngredientKind.Flavor, new FlavorVector(13, 0, 2)));

    public static KitchenIngredientLine Herb(int quantity = 2) => new(
        "herb_v", "Viscaran Rosemary", quantity, new KitchenComponentFlavor(KitchenIngredientKind.Flavor, new FlavorVector(3, 0, 10)));

    public static KitchenIngredientLine Water(int quantity = 1) => new(
        "distilled_water", "Distilled Water", quantity, new KitchenComponentFlavor(KitchenIngredientKind.Dilute, FlavorVector.Zero));

    public const int MeatLine = 0;
    public const int HerbLine = 1;
    public const int WaterLine = 2;

    /// <summary>A soup of meat, herb and water whose target an empty pot is 34 away from.</summary>
    public static KitchenRecipe Soup(
        int freshness = StartingFreshness,
        int inner = 10,
        int outer = 30,
        FlavorVector? center = null,
        int level = Level,
        params KitchenIngredientLine[] lines) =>
        new(
            KitchenDish.Soup,
            new KitchenTarget(center ?? Center, inner, outer),
            freshness,
            level,
            lines.Length > 0 ? lines : new[] { Meat(), Herb(), Water() });

    /// <summary>Rank 20 against a level 20 recipe, no Craftsmanship or Control, and 40 gear CP (55 CP in all).</summary>
    public static KitchenStats Cook(int craftsmanship = 0, int control = 0, int gearCp = 40, int rank = 20) =>
        new(rank, craftsmanship, control, gearCp);

    /// <summary>
    /// The smallest Craftsmanship at which an action of this base Doneness gives at least this much at the test level, so a test that
    /// needs a fast cook does not depend on the tuned Craftsmanship rate.
    /// </summary>
    public static int CraftsmanshipFor(int baseDoneness, int atLeast)
    {
        for (var craftsmanship = 0; craftsmanship <= 100000; craftsmanship++)
        {
            if (new KitchenStats(Level, craftsmanship, 0, 0).Doneness(baseDoneness, Level) >= atLeast)
                return craftsmanship;
        }

        throw new InvalidOperationException($"No Craftsmanship gives {atLeast} Doneness from a base of {baseDoneness}.");
    }

    /// <summary>A cook whose Medium simmer gives about a quarter of the Doneness the dish needs: three simmers do not finish it and the fourth does.</summary>
    public static KitchenStats QuarterCook() =>
        Cook(craftsmanship: CraftsmanshipFor(KitchenTuning.SimmerMediumDoneness, (KitchenTuning.DonenessMax + 3) / 4));

    /// <summary>The Doneness of one Medium simmer by <see cref="QuarterCook"/>.</summary>
    public static int QuarterSimmer => QuarterCook().Doneness(KitchenTuning.SimmerMediumDoneness, Level);

    public static KitchenEngine Engine(
        IMinigameRandom random,
        KitchenRecipe recipe = null,
        KitchenStats stats = null) =>
        new(random, recipe ?? Soup(), stats ?? Cook());

    /// <summary>
    /// The rolls for one performed action that does not end the attempt: its outcome roll when it has several results (pass 0 for
    /// none), then the forecast's drift axis (0 Savory, 1 Sweet, 2 Spice), drift direction (0 down, 1 up) and Peak Aroma roll
    /// (25 or less announces it). By default the drift pushes Sweet down, which does nothing while Sweet is 0, and nothing is
    /// announced.
    /// </summary>
    public static int[] Rolls(int outcome = 0, int axis = 1, int direction = 0, int aroma = 100) =>
        outcome > 0 ? new[] { outcome, axis, direction, aroma } : new[] { axis, direction, aroma };

    public static MinigameMeterView Meter(this KitchenEngine engine, string id) =>
        engine.DescribeMeters().Single(meter => meter.Id == id);
}
