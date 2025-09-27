using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class ItemReward : IQuestReward
    {
        private readonly IItemCacheService _itemCache;
        public bool IsSelectable { get; }
        public string MenuName { get; }
        private readonly string _resref;
        private readonly int _quantity;

        public ItemReward(IItemCacheService itemCache, string resref, int quantity, bool isSelectable)
        {
            _itemCache = itemCache;
            _resref = resref;
            _quantity = quantity;
            IsSelectable = isSelectable;

            var itemName = _itemCache.GetItemNameByResref(resref);

            if (_quantity > 1)
                MenuName = _quantity + "x " + itemName;
            else
                MenuName = itemName;
        }

        public void GiveReward(uint player)
        {
            CreateItemOnObject(_resref, player, _quantity);
        }
    }
}
