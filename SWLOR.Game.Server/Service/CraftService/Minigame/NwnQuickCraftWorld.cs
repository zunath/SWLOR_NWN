using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWNX;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>The game-engine side of a quick craft batch.</summary>
public sealed class NwnQuickCraftWorld : IQuickCraftWorld
{
    public static readonly NwnQuickCraftWorld Instance = new();

    /// <summary>Counts the player's inventory items of a resref, stack sizes included.</summary>
    public static int CountComponent(uint player, string resref)
    {
        var count = 0;
        for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
        {
            if (GetResRef(item) == resref)
                count += GetItemStackSize(item);
        }

        return count;
    }

    public bool IsPlayerAvailable(uint player) => GetIsObjectValid(player) && !GetIsDead(player);

    public void BeginBatch(uint player) => NwnCraftSessionWorld.Instance.BeginStationWork(player);

    public void PlayStationWork(uint player, SkillType skill) => NwnCraftSessionWorld.Instance.PlayStationWork(player, skill);

    public void EndBatch(uint player)
    {
        NwnCraftSessionWorld.Instance.EndStationWork(player);
        if (!GetIsObjectValid(player))
            return;

        AssignCommand(player, () => ClearAllActions());
        PlayerPlugin.StopGuiTimingBar(player);
    }

    public void PlayItemAnimation(uint player, float seconds)
    {
        // The station clip loops through the whole batch, so only a player without one needs the native pose per item.
        if (!NwnCraftSessionWorld.Instance.IsStationWorkPlaying(player))
            AssignCommand(player, () => ActionPlayAnimation(Animation.LoopingGetMid, 1.0f, seconds));

        PlayerPlugin.StartGuiTimingBar(player, seconds);
    }

    public bool HasComponents(uint player, RecipeDetail recipe)
    {
        // A station's fee (a Shipyard's, on a Capital hull) is paid per item, so a batch stops when the credits run out.
        var fee = Craft.GetStationFee(player, recipe);
        if (fee > 0 && GetGold(player) < fee)
            return false;

        // Ship recipes plan rare-trait slots and stand-ins as a manual craft does (space revamp).
        if (recipe.Category == RecipeCategoryType.Starship)
            return NwnCraftSessionWorld.Instance.HasComponents(player, recipe);

        // The same test TryTakeComponents applies: the stacks of each component add up to the amount one craft needs.
        foreach (var (resref, quantity) in recipe.Components)
        {
            if (CountComponent(player, resref) < quantity)
                return false;
        }

        return true;
    }

    /// <summary>The units each player's last <see cref="CreateItem"/> made, so a rollback removes those and never an item the player had.</summary>
    private readonly Dictionary<uint, (List<uint> Items, int Units)> _lastCreated = new();

    public bool CreateItem(uint player, RecipeDetail recipe)
    {
        var before = CountComponent(player, recipe.Resref);
        var created = new List<uint>();
        var first = NwnCraftSessionWorld.Instance.CreateCraftedItem(player, recipe);
        var placed = KeepIfInInventory(player, first);
        if (placed)
            created.Add(first);

        // The count tells what arrived: an item merged into an existing stack may not come back as a valid object, and a template
        // that does not stack creates one item whatever quantity is asked, so the rest are made one at a time.
        var gained = CountComponent(player, recipe.Resref) - before;
        while (placed && gained > 0 && gained < recipe.Quantity)
        {
            var extra = CreateItemOnObject(recipe.Resref, player, 1);
            SetLocalBool(extra, Item.PlayerProducedItemVariable, true);
            placed = KeepIfInInventory(player, extra);
            if (placed)
                created.Add(extra);
            var now = CountComponent(player, recipe.Resref) - before;
            if (now <= gained)
                break;
            gained = now;
        }

        _lastCreated[player] = (created, Math.Max(0, gained));
        if (gained >= recipe.Quantity)
            return true;

        // Only part of the output fit: take that part back too, so a refunded craft leaves no output behind.
        RemoveCreatedItem(player, recipe);
        return false;
    }

    /// <summary>An item created outside the inventory (on the ground, for a full inventory) is not a made item, so it is removed.</summary>
    private static bool KeepIfInInventory(uint player, uint item)
    {
        if (!GetIsObjectValid(item))
            return false;
        if (GetItemPossessor(item) == player)
            return true;

        DestroyObject(item);
        return false;
    }

    private static void RemoveUnits(uint player, string resref, int units)
    {
        for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item) && units > 0; item = GetNextItemInInventory(player))
        {
            if (GetResRef(item) != resref)
                continue;

            var size = GetItemStackSize(item);
            if (size <= units)
            {
                units -= size;
                DestroyObject(item);
            }
            else
            {
                SetItemStackSize(item, size - units);
                units = 0;
            }
        }
    }

    public bool TryConsumeComponents(uint player, RecipeDetail recipe) =>
        TryConsumeComponents(player, RecipeType.Invalid, recipe);

    public bool TryConsumeComponents(uint player, RecipeType recipeType, RecipeDetail recipe)
    {
        var fee = Craft.GetStationFee(player, recipe);
        if (fee > 0 && GetGold(player) < fee)
            return false;

        var taken = new List<string>();
        if (!NwnCraftSessionWorld.Instance.TryTakeComponents(player, recipe, taken))
            return false;

        Craft.PayStationFee(player, recipe);

        // The item made just before carries what the components pass on, as a manual craft's does (ship component traits).
        if (_lastCreated.TryGetValue(player, out var last))
        {
            foreach (var item in last.Items)
            {
                if (GetIsObjectValid(item))
                    NwnCraftSessionWorld.Instance.ApplyComponentTraits(item, recipeType, recipe, taken);
            }
        }

        return true;
    }

    /// <summary>
    /// Removes the units the player's last <see cref="CreateItem"/> made: the items it created, and for units that merged into a stack the
    /// player already had, that many units of the stack. An item the player had before, such as an enhanced one of the same resref, is never touched.
    /// </summary>
    public void RemoveCreatedItem(uint player, RecipeDetail recipe)
    {
        if (!_lastCreated.Remove(player, out var last))
            return;

        var units = last.Units;
        foreach (var item in last.Items)
        {
            if (units <= 0)
                break;
            if (!GetIsObjectValid(item) || GetItemPossessor(item) != player)
                continue;

            // A created handle can be a stack the new units merged into, so only the new units come off it.
            var size = GetItemStackSize(item);
            var take = Math.Min(size, units);
            if (take >= size)
                DestroyObject(item);
            else
                SetItemStackSize(item, size - take);
            units -= take;
        }

        // Units that merged without a handle are plain stack units, the same as any other of the resref's stacks.
        if (units > 0)
            RemoveUnits(player, recipe.Resref, units);
    }

    public void LogCraft(uint player, string text) => NwnCraftSessionWorld.Instance.LogCraft(player, text);

    public void Schedule(float seconds, Action action) => DelayCommand(seconds, action);

    public void Message(uint player, string text) => SendMessageToPC(player, text);
}
