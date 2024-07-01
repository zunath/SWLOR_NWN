using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class HutlarQuestDefinition: IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            BeatTheByysk();
            CullTheTundraThreat();
            HutlarPowerInvestigation();
            StupendousSlugBile();
            BreakTheByysk();

            return _builder.Build();
        }

        private void BeatTheByysk()
        {
            _builder.Create("beat_byysk", "Beat the Byysk")

                .AddState()
                .SetStateJournalText("You've agreed to kill fifteen Byysk out in the Qion Tundra. Kill them all!")
                .AddKillObjective(NPCGroupType.Hutlar_Byysk, 15)

                .AddState()
                .SetStateJournalText("Return to Rorrska Buvvien in the Hutlar Outpost and report your progress.")

                .AddGoldReward(800)
                .AddXPReward(800);
        }

        private void CullTheTundraThreat()
        {
            _builder.Create("tundra_tiger_threat", "Cull the Tundra Tiger Threat")

                .AddState()
                .SetStateJournalText("Kieun Xorxca wants you to head to Qion Tundra and kill ten tigers. Report back when this is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionTigers, 10)

                .AddState()
                .SetStateJournalText("Return to Kieun Xorxca in the Hutlar Outpost and report your progress.")

                .AddGoldReward(550)
                .AddXPReward(800);
        }

        private void HutlarPowerInvestigation()
        {
            _builder.Create("hut_power_invest", "Hutlar Power Investigation")
                .PrerequisiteQuest("beat_byysk")
                .PrerequisiteQuest("tundra_tiger_threat")
                .PrerequisiteQuest("stup_slug_bile")

                // Use object
                .AddState()
                .SetStateJournalText("Investigate the first power terminal in the southeastern section of the Qion Tundra.")

                // Use object
                .AddState()
                .SetStateJournalText("Investigate the second power terminal in the central section of the Qion Tundra.")

                // Use object
                .AddState()
                .SetStateJournalText("Investigate the third power terminal in the northern section of the Qion Tundra.")

                // Use object
                .AddState()
                .SetStateJournalText("Investigate the fourth power terminal in the southwestern section of the Qion Tundra.")

                // Use object
                .AddState()
                .SetStateJournalText("Investigate the fifth power terminal in the northwestern section of the Qion Tundra.")

                // Talk to NPC
                .AddState()
                .SetStateJournalText("Return to Guylan Verruchi in the Hutlar Outpost and report on your findings.")

                // Use object
                .AddState()
                .SetStateJournalText("Replace the actuator on the power terminal in the northwestern section of Qion Tundra.")

                // Talk to NPC
                .AddState()
                .SetStateJournalText("Return to Guylan Verruchi in the Hutlar Outpost and let him know you've replaced the actuator.")

                .AddGoldReward(1200)
                .AddXPReward(1300)

                .OnAcceptAction((player, sourceObject) =>
                {
                    // Southeast 
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "9CD9E7D9-4F10-4A0E-B67D-293CE6EA8EF5", VisibilityType.Visible);
                })
                .OnAbandonAction(player =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "9CD9E7D9-4F10-4A0E-B67D-293CE6EA8EF5", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "989B8C42-B4EE-48B7-8426-9D5C20016AEB", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "4C5721F2-9241-4A6F-9A62-F28CF0525682", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "E9C705B1-2AC9-4F9A-B481-FF3E5E99D8FF", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "83652C7A-7D38-4304-AD4B-92D5783AB279", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "AA0E6798-38E4-4E50-8F0A-C3177FBF2717", VisibilityType.Hidden);
                })
                
                .OnAdvanceAction((player, sourceObject, state) =>
                {
                    string visibilityObject;

                    switch (state)
                    {
                        // Central
                        case 2:
                            visibilityObject = "989B8C42-B4EE-48B7-8426-9D5C20016AEB";
                            break;
                        // Northern
                        case 3:
                            visibilityObject = "4C5721F2-9241-4A6F-9A62-F28CF0525682";
                            break;
                        // Southwestern
                        case 4:
                            visibilityObject = "E9C705B1-2AC9-4F9A-B481-FF3E5E99D8FF";
                            break;
                        // Northwestern
                        case 5:
                            visibilityObject = "83652C7A-7D38-4304-AD4B-92D5783AB279";
                            break;
                        // Northwestern again, Actuator
                        case 7:
                            visibilityObject = "AA0E6798-38E4-4E50-8F0A-C3177FBF2717";
                            break;
                        default: return;
                    }

                    ObjectVisibility.AdjustVisibilityByObjectId(player, visibilityObject, VisibilityType.Visible);
                });
        }

        private void StupendousSlugBile()
        {
            _builder.Create("stup_slug_bile", "Stupendious Slug Bile")

                .AddState()
                .SetStateJournalText("Moricho Deine in the Hutlar Outpost has requested you collect five Slug Biles from the Qion Slugs in Qion Tundra. Collect them and give them to him for a reward.")
                .AddCollectItemObjective("slug_bile", 5)

                .AddState()
                .SetStateJournalText("Speak to Moricho Deine for your reward.")

                .AddGoldReward(742)
                .AddItemReward("slug_shake", 1);
        }

        private void BreakTheByysk()
        {
            _builder.Create("break_the_byysk", "Break the Byysk")

                .AddState()
                .SetStateJournalText("Sharene wants you to kill One Hundred Byysk. Of you go!")
                .AddKillObjective(NPCGroupType.Byysk_Guardian, 100)

                .AddState()
                .SetStateJournalText("Return to Sharene in the Hutlar Outpost and report your progress.")

                .AddState()
                .SetStateJournalText("Sharene wants you to kill another One Hundred Byysk. Better get going")
                .AddKillObjective(NPCGroupType.Byysk_Guardian, 100)

                .AddState()
                .SetStateJournalText("Return to Sharene, once again, at the Hutlar Outpost.")

                .AddState()
                .SetStateJournalText("Sharene wants you to kill ANOTHER One Hundred Byysk...")
                .AddKillObjective(NPCGroupType.Byysk_Guardian, 100)

                .AddState()
                .SetStateJournalText("Return to Sharene, AGAIN.")

                .AddState()
                .SetStateJournalText("Sharene wants you to kill more Byysk... Let's go for one thousand this time!")
                .AddKillObjective(NPCGroupType.Byysk_Guardian, 100)

                .AddState()
                .SetStateJournalText("You know what, another One Hundred will do... That is Four Hundred Guardian's. This is getting stupid!")

                .AddGoldReward(20000)
                .AddXPReward(20000)
                .AddItemReward("recipe_banners01", 1);
        }

    }
}
