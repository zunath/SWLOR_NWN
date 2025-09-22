using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Service.QuestService
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
