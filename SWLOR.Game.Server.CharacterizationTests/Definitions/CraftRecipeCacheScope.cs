using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Registers one recipe in the recipe cache of a crafting instance and removes it again (restoring any recipe that was
/// registered under the same type) when disposed. <c>CacheData</c> checks every crafted item with the engine and cannot run
/// outside it, and the research cost calculations only need the recipe's level and cost modifier.
/// </summary>
internal sealed class CraftRecipeCacheScope : IDisposable
{
    private readonly Dictionary<RecipeType, RecipeDetail> _cache;
    private readonly RecipeType _type;
    private readonly RecipeDetail? _original;

    public CraftRecipeCacheScope(CraftService craft, RecipeType type, RecipeDetail recipe)
    {
        _cache = craft.GetAllRecipes();
        _type = type;
        _original = _cache.GetValueOrDefault(type);
        _cache[type] = recipe;
    }

    public void Dispose()
    {
        if (_original == null)
            _cache.Remove(_type);
        else
            _cache[_type] = _original;
    }
}
