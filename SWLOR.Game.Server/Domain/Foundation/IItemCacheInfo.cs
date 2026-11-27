namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// What the item cache asks about a spawned item while it caches a resref: the icon, whether it is a legacy item and
    /// whether the economy restricts it. Foundation owns the interface and Items answers it (<c>ItemCacheInfo</c>; supply
    /// rule 2 of MaintainabilityRefactorDomainOrder.md), so the cache needs nothing from the item service.
    /// </summary>
    public interface IItemCacheInfo
    {
        /// <summary>The icon resref the UIs use for an item (<c>IItemService.GetIconResref</c>).</summary>
        /// <param name="item">The item to retrieve the icon for.</param>
        string GetIconResref(uint item);

        /// <summary>Whether an item is a legacy item (<c>IItemService.IsLegacyItem</c>).</summary>
        /// <param name="item">The item to check.</param>
        bool IsLegacyItem(uint item);

        /// <summary>Whether an item is an NPC, creature or internal item players never see (<c>IItemService.IsEconomyRestricted</c>).</summary>
        /// <param name="item">The item to classify.</param>
        bool IsEconomyRestricted(uint item);
    }
}
