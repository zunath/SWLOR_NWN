using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Registers one recipe in the recipe cache that <see cref="Craft.GetRecipe"/> reads and removes it again (restoring any
/// recipe that was registered under the same type) when disposed. <c>Craft.CacheData</c> checks every crafted item with the
/// engine and cannot run outside it, and the research cost calculations only need the recipe's level and cost modifier.
/// The cache is process-wide, so a fixture that uses this scope must be <c>[NonParallelizable]</c>.
/// </summary>
internal sealed class CraftRecipeCacheScope : IDisposable
{
    private readonly Dictionary<RecipeType, RecipeDetail> _cache;
    private readonly RecipeType _type;
    private readonly RecipeDetail? _original;

    public CraftRecipeCacheScope(RecipeType type, RecipeDetail recipe)
    {
        _cache = CraftCalc.GetAllRecipes();
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
