using System;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Service.QuestService
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
