using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class HiddenAccessQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder;
        private readonly IObjectVisibilityService _objectVisibilityService;

        public HiddenAccessQuestDefinition(IObjectVisibilityService objectVisibilityService, IServiceProvider serviceProvider)
        {
            _objectVisibilityService = objectVisibilityService;
            _builder = new QuestBuilder(serviceProvider);
        }

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            SithBasementQuest();
            return _builder.Build();
        }

        private void SithBasementQuest()
        {
            _builder.Create("sith_basement", "Viscara Sith Basement")

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

