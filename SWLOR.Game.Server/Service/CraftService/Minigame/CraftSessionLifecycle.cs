using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The life of one item craft, from the moment its materials are committed to its success, failure or abandonment. The
/// committed materials are saved on the player, so a logout, death, window close or server restart still settles the attempt
/// as a failure under the loss rule in <see cref="CraftOutcomeRules"/>.
/// </summary>
public sealed class CraftSessionLifecycle
{
    public const string HeldItemsMessage =
        "Your inventory is full. Items from your craft wait for you; make room, then open a crafting station again.";

    private readonly ICraftSessionWorld _world;
    private readonly IMinigameRandom _random;

    public CraftSessionLifecycle(ICraftSessionWorld world, IMinigameRandom random)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>True when the saved craft is the one the session describes.</summary>
    public static bool IsActive(Player dbPlayer, CraftSession session) =>
        session != null && dbPlayer.ActiveCraftSession != null && dbPlayer.ActiveCraftSession.SessionId == session.SessionId;

    /// <summary>
    /// Commits the recipe's components, the selected enhancements and the blueprint, charging the blueprint's credits and one
    /// licensed run as the original crafting window did. Nothing is taken when any step is refused.
    /// </summary>
    public CraftSessionStartResult Start(uint player, Player dbPlayer, CraftSessionStartRequest request)
    {
        if (dbPlayer.ActiveCraftSession != null)
            Fail(player, dbPlayer, CraftSessionEndReason.Replaced);

        if (!_world.CanCraft(player, request.Recipe))
            return CraftSessionStartResult.Refused("Recipe requirements not met!");

        var recipe = _world.GetRecipe(request.Recipe);
        if (request.HasBlueprint)
        {
            var problem = _world.ValidateBlueprint(player, request.BlueprintItem, request.Recipe);
            if (!string.IsNullOrEmpty(problem))
                return CraftSessionStartResult.Refused(problem);
        }

        var components = new List<string>();
        if (!_world.TryTakeComponents(player, recipe, components))
            return CraftSessionStartResult.Refused("Missing components!");

        var enhancements = new List<string>();
        foreach (var item in request.EnhancementItems)
        {
            if (item == OBJECT_INVALID)
            {
                enhancements.Add(string.Empty);
                continue;
            }

            if (!_world.TryTakeEnhancement(player, item, out var serialized))
            {
                foreach (var taken in components.Concat(enhancements.Where(e => !string.IsNullOrEmpty(e))))
                    ReturnOrHold(player, dbPlayer, taken);
                _world.SaveRecord(dbPlayer);
                return CraftSessionStartResult.Refused("An enhancement is no longer in your inventory.");
            }

            enhancements.Add(serialized);
        }

        var blueprint = request.HasBlueprint ? _world.ChargeBlueprint(player, request.BlueprintItem) : null;
        var sessionId = Guid.NewGuid().ToString();
        dbPlayer.ActiveCraftSession = new CraftSessionRecord
        {
            SessionId = sessionId,
            Recipe = request.Recipe,
            Skill = recipe.Skill,
            HasBlueprint = blueprint != null,
            BlueprintLevel = blueprint?.Level ?? 0,
            StartedUtc = DateTime.UtcNow,
            Components = components,
            Enhancements = enhancements
        };
        _world.SaveRecord(dbPlayer);
        _world.BeginStationWork(player);

        return CraftSessionStartResult.Started(new CraftSession(sessionId, request.Recipe, blueprint));
    }

    /// <summary>
    /// Finishes a craft: creates the item, applies the enhancements whose slot thresholds the quality meets, the food and
    /// blueprint bonuses and the vendor value, records the recipe as crafted and grants XP. Returns null when the session is no
    /// longer the player's saved craft, so a repeated request cannot create a second item.
    /// </summary>
    public CraftSessionSuccess Succeed(uint player, Player dbPlayer, CraftSession session, MinigameMeter quality)
    {
        if (!IsActive(dbPlayer, session))
            return null;

        var record = dbPlayer.ActiveCraftSession;
        var recipe = _world.GetRecipe(record.Recipe);
        var firstTime = !dbPlayer.CraftedRecipes.ContainsKey(record.Recipe);
        var blueprintLevel = session.Blueprint?.Level ?? 0;
        var filledSlots = FilledSlots(record);

        var outcome = CraftOutcomeRules.CalculateSuccess(
            quality,
            recipe.Level,
            Craft.GetBaseRecipeXP(recipe, GetRank(dbPlayer, recipe)),
            firstTime,
            blueprintLevel,
            recipe.Category == RecipeCategoryType.Food,
            filledSlots);

        var item = _world.CreateCraftedItem(player, recipe);
        _world.ApplyVendorValue(item, outcome.VendorValueBonus);

        foreach (var transfer in outcome.Enhancements)
        {
            if (transfer.Transfers)
            {
                _world.ApplyEnhancement(item, record.Enhancements[transfer.Slot - 1], recipe);
                _world.Message(player, ColorToken.Green("Enhancement applied successfully."));
            }
            else
            {
                _world.Message(player, ColorToken.Red(
                    $"Enhancement failed to apply: quality did not reach {transfer.ThresholdPercent}%."));
            }
        }

        if (recipe.Category == RecipeCategoryType.Food)
            _world.ApplyFoodBonus(item, outcome.FoodDurationBonusUnits, outcome.FoodBonusCharges);

        if (session.HasBlueprint)
            _world.ApplyBlueprintBonuses(player, item, recipe, session.Blueprint);

        if (firstTime)
            dbPlayer.CraftedRecipes[record.Recipe] = DateTime.UtcNow;
        dbPlayer.ActiveCraftSession = null;
        _world.SaveRecord(dbPlayer);
        _world.EndStationWork(player);

        // XP last: it saves the player record itself.
        _world.GiveXp(player, recipe.Skill, outcome.Xp);
        _world.LogCraft(player, $"successfully crafted '{recipe.Resref}'.");

        return new CraftSessionSuccess(item, outcome);
    }

    /// <summary>
    /// Ends the saved craft as a failure: each committed component stack and enhancement comes back on a roll above the loss
    /// chance, and the attempt still grants 15% of the base XP. Items that do not fit the inventory are held on the player until
    /// there is room. Does nothing when no craft is saved.
    /// </summary>
    public CraftSettlement Fail(uint player, Player dbPlayer, CraftSessionEndReason reason)
    {
        var record = dbPlayer.ActiveCraftSession;
        if (record == null)
            return CraftSettlement.None;

        var recipe = _world.GetRecipe(record.Recipe);
        var enhancements = record.Enhancements.Where(e => !string.IsNullOrEmpty(e)).ToList();
        var failure = CraftOutcomeRules.RollFailure(
            _random,
            enhancements.Count,
            record.Components.Count,
            Craft.GetBaseRecipeXP(recipe, GetRank(dbPlayer, recipe)),
            record.BlueprintLevel);

        var kept = new List<string>();
        for (var index = 0; index < enhancements.Count; index++)
        {
            if (failure.EnhancementsKept[index])
                kept.Add(enhancements[index]);
        }

        for (var index = 0; index < record.Components.Count; index++)
        {
            if (failure.ComponentsKept[index])
                kept.Add(record.Components[index]);
        }

        var total = enhancements.Count + record.Components.Count;
        dbPlayer.HeldCraftItems ??= new List<string>();
        dbPlayer.HeldCraftItems.AddRange(kept);
        dbPlayer.ActiveCraftSession = null;
        _world.SaveRecord(dbPlayer);
        _world.EndStationWork(player);

        ReturnHeldItems(player, dbPlayer);

        if (reason != CraftSessionEndReason.Broken)
        {
            _world.Message(player, ColorToken.Red(
                $"Your unfinished craft failed. {total - kept.Count} of {total} items were lost."));
        }

        // XP last: it saves the player record itself.
        _world.GiveXp(player, recipe.Skill, failure.Xp);
        _world.LogCraft(player, $"failed to craft '{record.Recipe}' ({reason}).");

        return new CraftSettlement(true, total - kept.Count, kept.Count, failure.Xp);
    }

    /// <summary>
    /// Settles a craft left over from a logout, death or restart, then gives back any held items that now fit. Call it at login
    /// and whenever a crafting window opens.
    /// </summary>
    public CraftSettlement SettleLeftover(uint player, Player dbPlayer, CraftSessionEndReason reason)
    {
        var settlement = Fail(player, dbPlayer, reason);
        if (!settlement.Settled)
            ReturnHeldItems(player, dbPlayer);

        return settlement;
    }

    /// <summary>Gives back held items that fit the inventory now. Returns how many are still held.</summary>
    public int ReturnHeldItems(uint player, Player dbPlayer)
    {
        var held = dbPlayer.HeldCraftItems;
        if (held == null || held.Count == 0)
            return 0;

        var remaining = new List<string>();
        foreach (var serialized in held)
        {
            if (!_world.TryReturnItem(player, serialized))
                remaining.Add(serialized);
        }

        if (remaining.Count < held.Count)
        {
            held.Clear();
            held.AddRange(remaining);
            _world.SaveRecord(dbPlayer);
        }

        if (held.Count > 0)
            _world.Message(player, HeldItemsMessage);

        return held.Count;
    }

    private void ReturnOrHold(uint player, Player dbPlayer, string serialized)
    {
        if (_world.TryReturnItem(player, serialized))
            return;

        dbPlayer.HeldCraftItems ??= new List<string>();
        dbPlayer.HeldCraftItems.Add(serialized);
    }

    private static List<int> FilledSlots(CraftSessionRecord record)
    {
        var slots = new List<int>();
        for (var index = 0; index < record.Enhancements.Count; index++)
        {
            if (!string.IsNullOrEmpty(record.Enhancements[index]))
                slots.Add(index + 1);
        }

        return slots;
    }

    private static int GetRank(Player dbPlayer, RecipeDetail recipe) =>
        dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill) ? skill.Rank : 0;
}
