using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// The item reads the item cache makes (<see cref="IItemCacheInfo"/>, owned by Foundation; see
    /// Readmes/MaintainabilityRefactorPhase5.md): the same three answers <see cref="IItemService"/> gives.
    /// </summary>
    public class ItemCacheInfo : IItemCacheInfo
    {
        private readonly IItemService _item;

        public ItemCacheInfo(IItemService item)
        {
            _item = item;
        }

        public string GetIconResref(uint item) => _item.GetIconResref(item);

        public bool IsLegacyItem(uint item) => _item.IsLegacyItem(item);

        public bool IsEconomyRestricted(uint item) => _item.IsEconomyRestricted(item);
    }
}
