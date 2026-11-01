using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>Starts, tracks and cancels each player's quick craft batch.</summary>
public static class QuickCraftService
{
    private static readonly Dictionary<uint, QuickCraftBatch> Batches = new();

    public static bool IsRunning(uint player) => Batches.TryGetValue(player, out var batch) && batch.IsRunning;

    /// <summary>Whether the player may quick craft the recipe now, and why not when they may not.</summary>
    public static QuickCraftEligibility Evaluate(uint player, RecipeType recipeType)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        if (dbPlayer == null)
            return QuickCraftEligibility.Refused("Your character could not be loaded.");

        var recipe = Craft.GetRecipe(recipeType);
        var rank = dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill) ? skill.Rank : 0;
        var eligibility = QuickCraftRules.Evaluate(
            recipe,
            Skill.GetSkillDetails(recipe.Skill).Name,
            rank,
            dbPlayer.CraftedRecipes.ContainsKey(recipeType));
        if (!eligibility.IsEligible)
            return eligibility;

        if (!Craft.CanPlayerCraftRecipe(player, recipeType))
            return QuickCraftEligibility.Refused("Recipe requirements not met.");

        return QuickCraftEligibility.Eligible;
    }

    /// <summary>How many items a batch of <paramref name="requested"/> can make with the materials the player carries.</summary>
    public static int ResolveBatchSize(uint player, RecipeType recipeType, int requested) =>
        QuickCraftRules.ResolveBatchSize(requested, Craft.GetRecipe(recipeType), resref => NwnQuickCraftWorld.CountComponent(player, resref));

    /// <summary>
    /// Starts a batch after checking everything again; the window's enabled state is not trusted. Returns the reason when it
    /// does not start. <paramref name="onEnded"/> runs when the batch ends for any reason.
    /// </summary>
    public static string Start(uint player, RecipeType recipeType, int requested, Action onEnded)
    {
        if (IsRunning(player))
            return "A quick craft is already running.";

        if (CraftSessionService.IsCrafting(player) || CraftMinigameRouting.IsAnyCraftWindowOpen(player))
            return "Finish your current craft first.";

        var eligibility = Evaluate(player, recipeType);
        if (!eligibility.IsEligible)
            return eligibility.Reason;

        var quantity = ResolveBatchSize(player, recipeType, requested);
        if (quantity < 1)
            return "You do not have the materials for this recipe.";

        var batch = new QuickCraftBatch(NwnQuickCraftWorld.Instance, player, recipeType, Craft.GetRecipe(recipeType), quantity);
        batch.OnEnded += finished =>
        {
            Batches.Remove(player);
            Log.Write(LogGroup.Crafting,
                $"{GetName(player)} quick crafted {finished.Completed} of {finished.Requested} '{recipeType}' ({finished.Ended}).");
            onEnded?.Invoke();
        };

        Batches[player] = batch;
        batch.Start();
        return string.Empty;
    }

    public static void Cancel(uint player)
    {
        if (Batches.TryGetValue(player, out var batch))
            batch.Cancel();
    }

    /// <summary>Stops the batch because the player died or left.</summary>
    public static void Interrupt(uint player)
    {
        if (Batches.TryGetValue(player, out var batch))
            batch.Interrupt();
    }
}
