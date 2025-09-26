using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Service;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;
using SWLOR.Shared.Domain.World.Contracts;

namespace SWLOR.Component.Quest.Feature.QuestDefinition
{
    public class HiddenAccessQuestDefinition : IQuestListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public HiddenAccessQuestDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IQuestBuilderFactory QuestBuilderFactory => _serviceProvider.GetRequiredService<IQuestBuilderFactory>();
        private IObjectVisibilityService ObjectVisibilityService => _serviceProvider.GetRequiredService<IObjectVisibilityService>();
        private IQuestService QuestService => _serviceProvider.GetRequiredService<IQuestService>();

        public Dictionary<string, IQuestDetail> BuildQuests()
        {
            var builder = _questBuilderFactory.Create();
            SithBasementQuest(builder);
            return builder.Build();
        }

        private void SithBasementQuest(IQuestBuilder builder)
        {
            builder.Create("sith_basement", "Viscara Sith Basement")

                .AddState()
                .SetStateJournalText("Talk to SithBasementGiver again to complete quest.")

                .AddKeyItemReward(KeyItemType.SithBasementKey)

                .OnAcceptAction((player, sourceObject) =>
                {
                    _objectVisibilityService.AdjustVisibilityByObjectId(player, "7E2C4B6D9F8A35B1C0E8D7F3A4B5C6E2", VisibilityType.Hidden);
                })
                .OnAbandonAction(player =>
                {
                    _objectVisibilityService.AdjustVisibilityByObjectId(player, "7E2C4B6D9F8A35B1C0E8D7F3A4B5C6E2", VisibilityType.Hidden);
                })

                .OnCompleteAction((player, sourceObject) =>
                {
                    _objectVisibilityService.AdjustVisibilityByObjectId(player, "7E2C4B6D9F8A35B1C0E8D7F3A4B5C6E2", VisibilityType.Visible);
                });
        }
    }
}

