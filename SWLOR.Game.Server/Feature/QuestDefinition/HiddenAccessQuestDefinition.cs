using System.Collections.Generic;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class HiddenAccessQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
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
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "7E2C4B6D9F8A35B1C0E8D7F3A4B5C6E2", VisibilityType.Hidden);
                })
                .OnAbandonAction(player =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "7E2C4B6D9F8A35B1C0E8D7F3A4B5C6E2", VisibilityType.Hidden);
                })

                .OnCompleteAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "7E2C4B6D9F8A35B1C0E8D7F3A4B5C6E2", VisibilityType.Visible);
                });
        }
    }
}

