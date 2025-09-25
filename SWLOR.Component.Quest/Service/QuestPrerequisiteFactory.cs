using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Communication.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Service
{
    /// <summary>
    /// Factory implementation for creating quest prerequisite instances.
    /// This ensures proper DI management and dependency injection.
    /// </summary>
    public class QuestPrerequisiteFactory : IQuestPrerequisiteFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QuestPrerequisiteFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IQuestPrerequisite CreateRequiredQuestPrerequisite(string prerequisiteQuestId)
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            return new RequiredQuestPrerequisite(db, prerequisiteQuestId);
        }

        public IQuestPrerequisite CreateRequiredKeyItemPrerequisite(KeyItemType keyItemType)
        {
            var keyItemService = _serviceProvider.GetRequiredService<IKeyItemService>();
            return new RequiredKeyItemPrerequisite(keyItemType, keyItemService);
        }

        public IQuestPrerequisite CreateRequiredFactionStandingPrerequisite(FactionType faction, int requiredAmount)
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            return new RequiredFactionStandingPrerequisite(db, faction, requiredAmount);
        }
    }
}
