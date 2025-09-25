using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class RequiredKeyItemPrerequisite : IQuestPrerequisite
    {
        private readonly KeyItemType _keyItemType;
        private readonly IKeyItemService _keyItemService;

        public RequiredKeyItemPrerequisite(KeyItemType keyItemType, IKeyItemService keyItemService)
        {
            _keyItemType = keyItemType;
            _keyItemService = keyItemService;
        }

        public bool MeetsPrerequisite(uint player)
        {
            return _keyItemService.HasKeyItem(player, _keyItemType);
        }
    }
}
