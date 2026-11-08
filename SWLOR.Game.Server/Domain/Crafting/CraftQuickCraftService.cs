using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <inheritdoc cref="ICraftQuickCraftService" />
public class CraftQuickCraftService : ICraftQuickCraftService
{
    private readonly IDBService _db;
    private readonly ICraftService _craft;
    private readonly ICraftSessionsService _sessions;
    private readonly ICraftMinigameRoutingService _routing;
    private readonly ISkillService _skill;

    // Each player's quick craft batch, by player object.
    private readonly Dictionary<uint, QuickCraftBatch> _batches = new();

    public CraftQuickCraftService(
        IDBService db,
        ICraftService craft,
        ICraftSessionsService sessions,
        ICraftMinigameRoutingService routing,
        ISkillService skill)
    {
        _db = db;
        _craft = craft;
        _sessions = sessions;
        _routing = routing;
        _skill = skill;
    }

    /// <inheritdoc />
    public bool IsRunning(uint player) => _batches.TryGetValue(player, out var batch) && batch.IsRunning;

    /// <inheritdoc />
    public QuickCraftEligibility Evaluate(uint player, RecipeType recipeType)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        if (dbPlayer == null)
            return QuickCraftEligibility.Refused("Your character could not be loaded.");

        var recipe = _craft.GetRecipe(recipeType);
        var rank = dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill) ? skill.Rank : 0;
        var eligibility = QuickCraftRules.Evaluate(
            recipe,
            GetSkillName(recipe.Skill),
            rank,
            dbPlayer.CraftedRecipes.ContainsKey(recipeType));
        if (!eligibility.IsEligible)
            return eligibility;

        if (!_craft.CanPlayerCraftRecipe(player, recipeType))
            return QuickCraftEligibility.Refused("Recipe requirements not met.");

        return QuickCraftEligibility.Eligible;
    }

    /// <inheritdoc />
    public int ResolveBatchSize(uint player, RecipeType recipeType, int requested) =>
        QuickCraftRules.ResolveBatchSize(requested, _craft.GetRecipe(recipeType), resref => NwnQuickCraftWorld.CountComponent(player, resref));

    /// <inheritdoc />
    public string Start(uint player, RecipeType recipeType, int requested, Action onEnded)
    {
        if (IsRunning(player))
            return "A quick craft is already running.";

        if (_sessions.IsCrafting(player) || _routing.IsAnyCraftWindowOpen(player))
            return "Finish your current craft first.";

        var eligibility = Evaluate(player, recipeType);
        if (!eligibility.IsEligible)
            return eligibility.Reason;

        var quantity = ResolveBatchSize(player, recipeType, requested);
        if (quantity < 1)
            return "You do not have the materials for this recipe.";

        var batch = CreateBatch(player, _craft.GetRecipe(recipeType), quantity);
        batch.OnEnded += finished =>
        {
            _batches.Remove(player);
            WriteLog(
                $"{GetName(player)} quick crafted {finished.Completed} of {finished.Requested} '{recipeType}' ({finished.Ended}).");
            onEnded?.Invoke();
        };

        _batches[player] = batch;
        batch.Start();
        return string.Empty;
    }

    /// <inheritdoc />
    public void Cancel(uint player)
    {
        if (_batches.TryGetValue(player, out var batch))
            batch.Cancel();
    }

    /// <inheritdoc />
    public void Interrupt(uint player)
    {
        if (_batches.TryGetValue(player, out var batch))
            batch.Interrupt();
    }

    /// <summary>Makes the batch against the engine world of quick crafting.</summary>
    protected virtual QuickCraftBatch CreateBatch(uint player, RecipeDetail recipe, int quantity) =>
        new(_craft.QuickCraftWorld, player, recipe, quantity);

    /// <summary>The display name of a skill. Skill is not converted yet.</summary>
    protected virtual string GetSkillName(SkillType skill) => _skill.GetSkillDetails(skill).Name;

    /// <summary>Writes a crafting log line. Log is not converted yet.</summary>
    protected virtual void WriteLog(string message) => Log.Write(LogGroup.Crafting, message);
}
