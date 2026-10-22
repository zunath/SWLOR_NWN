using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// An in-memory stand-in for the game engine. Items are strings: a component stack is "resref xN", an enhancement is the
/// name it was given. Every engine call is also written to <see cref="Events"/> so a test can check order.
/// </summary>
internal sealed class FakeCraftSessionWorld : ICraftSessionWorld
{
    public const uint PlayerHandle = 1;
    public const uint FirstEnhancement = 100;
    public const uint SecondEnhancement = 101;
    public const uint Blueprint = 200;
    private uint _nextItem = 1000;

    public Dictionary<RecipeType, RecipeDetail> Recipes { get; } = new();
    public Dictionary<string, int> Inventory { get; } = new();
    public Dictionary<uint, string> Enhancements { get; } = new();
    public bool MeetsRequirements { get; set; } = true;
    public string BlueprintProblem { get; set; } = string.Empty;
    public BlueprintDetail BlueprintAfterCharge { get; set; } = new() { Recipe = RecipeType.Invalid, Level = 2, LicensedRuns = 4 };

    /// <summary>The number of returned items the inventory can still take.</summary>
    public int Room { get; set; } = int.MaxValue;

    public List<string> Events { get; } = new();
    public List<string> Returned { get; } = new();
    public List<string> Messages { get; } = new();
    public List<(SkillType Skill, int Xp)> XpGiven { get; } = new();
    public List<string> AppliedEnhancements { get; } = new();
    public List<(int Seconds, int Charges)> FoodBonuses { get; } = new();
    public List<int> VendorValues { get; } = new();
    public int BlueprintBonusCalls { get; private set; }
    public int SaveCount { get; private set; }

    public RecipeDetail GetRecipe(RecipeType recipe) => Recipes[recipe];

    /// <summary>The factor on the base XP of every craft. 1 unless a test scales it.</summary>
    public double XpMultiplier { get; set; } = 1.0;

    public double GetXpMultiplier(SkillType skill) => XpMultiplier;

    public bool CanCraft(uint player, RecipeType recipe) => MeetsRequirements;

    public string ValidateBlueprint(uint player, uint blueprintItem, RecipeType recipe) => BlueprintProblem;

    public BlueprintDetail ChargeBlueprint(uint player, uint blueprintItem)
    {
        Events.Add("charge-blueprint");
        return BlueprintAfterCharge;
    }

    public bool TryTakeComponents(uint player, RecipeDetail recipe, List<string> taken)
    {
        foreach (var (resref, quantity) in recipe.Components)
        {
            if (!Inventory.TryGetValue(resref, out var have) || have < quantity)
                return false;
        }

        foreach (var (resref, quantity) in recipe.Components)
        {
            Inventory[resref] -= quantity;
            taken.Add($"{resref} x{quantity}");
        }

        Events.Add("take-components");
        return true;
    }

    public bool TryTakeEnhancement(uint player, uint item, out string serialized)
    {
        if (!Enhancements.Remove(item, out serialized))
        {
            serialized = string.Empty;
            return false;
        }

        Events.Add($"take-enhancement:{serialized}");
        return true;
    }

    public bool TryReturnItem(uint player, string serialized)
    {
        if (Returned.Count >= Room)
            return false;

        Returned.Add(serialized);
        return true;
    }

    public void BeginStationWork(uint player) => Events.Add("begin-work");

    public void EndStationWork(uint player) => Events.Add("end-work");

    public uint CreateCraftedItem(uint player, RecipeDetail recipe)
    {
        Events.Add($"create:{recipe.Resref}");
        return _nextItem++;
    }

    public void ApplyVendorValue(uint item, int addGoldPiece) => VendorValues.Add(addGoldPiece);

    public void ApplyEnhancement(uint item, string serializedEnhancement, RecipeDetail recipe) =>
        AppliedEnhancements.Add(serializedEnhancement);

    public void ApplyFoodBonus(uint item, int durationSeconds, int bonusCharges) =>
        FoodBonuses.Add((durationSeconds, bonusCharges));

    public void ApplyBlueprintBonuses(uint player, uint item, RecipeDetail recipe, BlueprintDetail blueprint) =>
        BlueprintBonusCalls++;

    public void GiveXp(uint player, SkillType skill, int xp)
    {
        Events.Add("xp");
        XpGiven.Add((skill, xp));
    }

    public void SaveRecord(Player dbPlayer)
    {
        SaveCount++;
        Events.Add("save");
    }

    public void Message(uint player, string text) => Messages.Add(text);

    public void LogCraft(uint player, string text)
    {
    }
}
