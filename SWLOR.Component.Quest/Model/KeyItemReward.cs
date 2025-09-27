using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class KeyItemReward : IQuestReward
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IKeyItemService KeyItemService => _serviceProvider.GetRequiredService<IKeyItemService>();
        public bool IsSelectable { get; }

        public string MenuName
        {
            get
            {
                var detail = KeyItemService.GetKeyItem(KeyItemType);
                return detail.Name;
            }
        }

        public KeyItemType KeyItemType { get; }

        public KeyItemReward(KeyItemType keyItemType, bool isSelectable, IServiceProvider serviceProvider)
        {
            KeyItemType = keyItemType;
            IsSelectable = isSelectable;
            // Services are now lazy-loaded via IServiceProvider
        }

        public void GiveReward(uint player)
        {
            KeyItemService.GiveKeyItem(player, KeyItemType);
        }
    }
}
