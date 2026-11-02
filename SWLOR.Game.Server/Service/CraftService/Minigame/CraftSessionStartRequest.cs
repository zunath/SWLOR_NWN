using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>What a player commits to when a craft starts.</summary>
public sealed class CraftSessionStartRequest
{
    public CraftSessionStartRequest(RecipeType recipe, uint blueprintItem, IReadOnlyList<uint> enhancementItems)
    {
        Recipe = recipe;
        BlueprintItem = blueprintItem;
        EnhancementItems = enhancementItems ?? Array.Empty<uint>();
    }

    public RecipeType Recipe { get; }

    /// <summary>The blueprint to craft from, or <c>OBJECT_INVALID</c> for none.</summary>
    public uint BlueprintItem { get; }

    /// <summary>The enhancement in each slot, counting from slot 1. A slot with no enhancement holds <c>OBJECT_INVALID</c>.</summary>
    public IReadOnlyList<uint> EnhancementItems { get; }

    public bool HasBlueprint => BlueprintItem != OBJECT_INVALID;
}
