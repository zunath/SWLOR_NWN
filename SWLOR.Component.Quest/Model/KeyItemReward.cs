using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class KeyItemReward : IQuestReward
    {
        private readonly IKeyItemService _keyItemService;
        public bool IsSelectable { get; }

        public string MenuName
        {
            get
            {
                var detail = _keyItemService.GetKeyItem(KeyItemType);
                return detail.Name;
            }
        }

        public KeyItemType KeyItemType { get; }

        public KeyItemReward(KeyItemType keyItemType, bool isSelectable, IKeyItemService keyItemService)
        {
            KeyItemType = keyItemType;
            IsSelectable = isSelectable;
            _keyItemService = keyItemService;
        }

        public void GiveReward(uint player)
        {
            _keyItemService.GiveKeyItem(player, KeyItemType);
        }
    }
}
