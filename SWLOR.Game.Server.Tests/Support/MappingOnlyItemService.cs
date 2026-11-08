using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The server's item service with the search for item definitions switched off, for a fixture that needs the weapon
/// ability mappings <c>CacheData</c> builds but not every item list of the server. Register it in a
/// <see cref="FacadeContainer"/> with <c>services.Replace(ServiceDescriptor.Singleton&lt;IItemService, MappingOnlyItemService&gt;())</c>
/// and call <c>Item.CacheData()</c> under a fake engine.
/// </summary>
public sealed class MappingOnlyItemService : ItemService
{
    public MappingOnlyItemService(IBeastMasteryService beastMastery, IDBService db)
        : base(beastMastery, db, AmbientServices.Of<IActivityService>(), AmbientServices.Of<ICombatRecorderService>(), AmbientServices.Of<ICraftMinigameRoutingService>(), AmbientServices.Of<IPerkService>(), AmbientServices.Of<IRecastService>(), AmbientServices.LazyOf<ISkillService>(), AmbientServices.LazyOf<ITwinPistolEquipmentService>())
    {
    }

    protected override IEnumerable<Type> GetItemListDefinitionTypes() => Array.Empty<Type>();
}
