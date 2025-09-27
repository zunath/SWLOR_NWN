using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Model
{
    public class RequiredKeyItemPrerequisite : IQuestPrerequisite
    {
        private readonly KeyItemType _keyItemType;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IKeyItemService KeyItemService => _serviceProvider.GetRequiredService<IKeyItemService>();

        public RequiredKeyItemPrerequisite(KeyItemType keyItemType, IServiceProvider serviceProvider)
        {
            _keyItemType = keyItemType;
            // Services are now lazy-loaded via IServiceProvider
        }

        public bool MeetsPrerequisite(uint player)
        {
            return KeyItemService.HasKeyItem(player, _keyItemType);
        }
    }
}
