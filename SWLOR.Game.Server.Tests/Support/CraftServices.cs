using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A crafting instance for a test that needs only the recipe cache and the item property rules: the database is in
/// memory and the neighbouring services are scripted stand-ins that answer nothing, so a call that reaches one fails.
/// </summary>
public static class CraftServices
{
    public static CraftService Create(IDBService? db = null) => new(
        ScriptedProxy.Create<ICacheService>(out _),
        db ?? new InMemoryDBService(),
        ScriptedProxy.Create<IItemService>(out _),
        ScriptedProxy.Create<IPropertyService>(out _));
}
