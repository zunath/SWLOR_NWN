using System.Collections.Generic;
using SWLOR.NWN.API.NWNX;
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

    public void EndBatch(uint player)
    {
        NwnCraftSessionWorld.Instance.EndStationWork(player);
        if (GetIsObjectValid(player))
            AssignCommand(player, () => ClearAllActions());
    }

    public void PlayItemAnimation(uint player, float seconds)
    {
        AssignCommand(player, () => ActionPlayAnimation(Animation.LoopingGetMid, 1.0f, seconds));
        PlayerPlugin.StartGuiTimingBar(player, seconds);
    }

    public bool TryConsumeComponents(uint player, RecipeDetail recipe) =>
        NwnCraftSessionWorld.Instance.TryTakeComponents(player, recipe, new List<string>());

    public void CreateItem(uint player, RecipeDetail recipe) =>
        NwnCraftSessionWorld.Instance.CreateCraftedItem(player, recipe);

    public void Schedule(float seconds, Action action) => DelayCommand(seconds, action);

    public void Message(uint player, string text) => SendMessageToPC(player, text);
}
