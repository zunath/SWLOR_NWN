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
        // The same test TryTakeComponents applies: the stacks of each component add up to the amount one craft needs.
        foreach (var (resref, quantity) in recipe.Components)
        {
            if (CountComponent(player, resref) < quantity)
                return false;
        }

        return true;
    }

    public bool CreateItem(uint player, RecipeDetail recipe)
    {
        var before = CountComponent(player, recipe.Resref);
        var item = NwnCraftSessionWorld.Instance.CreateCraftedItem(player, recipe);
        if (GetIsObjectValid(item) && GetItemPossessor(item) == player)
            return true;

        // Created outside the inventory (on the ground, for a full inventory): it is not a made item, so it is removed.
        if (GetIsObjectValid(item))
            DestroyObject(item);

        // An item the engine merged into an existing stack may not be returned as a valid object; the count tells.
        var gained = CountComponent(player, recipe.Resref) - before;
        if (gained >= recipe.Quantity)
            return true;

        // Only part of the output fit: take that part back too, so a refunded craft leaves no output behind.
        if (gained > 0)
            RemoveUnits(player, recipe.Resref, gained);
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
        NwnCraftSessionWorld.Instance.TryTakeComponents(player, recipe, new List<string>());

    public void RemoveCreatedItem(uint player, RecipeDetail recipe) => RemoveUnits(player, recipe.Resref, recipe.Quantity);

    public void LogCraft(uint player, string text) => NwnCraftSessionWorld.Instance.LogCraft(player, text);

    public void Schedule(float seconds, Action action) => DelayCommand(seconds, action);

    public void Message(uint player, string text) => SendMessageToPC(player, text);
}
