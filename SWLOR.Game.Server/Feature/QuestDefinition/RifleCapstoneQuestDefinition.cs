using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class RifleCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string KillBoxFoundationQuestId = "kill_box_foundation";
        internal const string KillBoxMeasureQuestId = "kill_box_measure";
        internal const string KillBoxBreachQuestId = "kill_box_breach";
        internal const string KillBoxCircleQuestId = "kill_box_circle";
        internal const string KillBoxMasteryQuestId = "kill_box_mastery";
        internal const string KillBoxAdeptResref = "cp_killbox_ad";
        internal const string KillBoxSpecialistResref = "cp_killbox_sp";
        internal const string KillBoxInnerCircleResref = "cp_killbox_ic";
        internal const string OneShotFoundationQuestId = "one_shot_foundation";
        internal const string OneShotMeasureQuestId = "one_shot_measure";
        internal const string OneShotBreachQuestId = "one_shot_breach";
        internal const string OneShotCircleQuestId = "one_shot_circle";
        internal const string OneShotMasteryQuestId = "one_shot_mastery";
        internal const string OneShotAdeptResref = "cp_oneshot_ad";
        internal const string OneShotSpecialistResref = "cp_oneshot_sp";
        internal const string OneShotInnerCircleResref = "cp_oneshot_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            KillBoxFoundation();
            KillBoxMeasure();
            KillBoxBreach();
            KillBoxCircle();
            KillBoxMastery();
            OneShotFoundation();
            OneShotMeasure();
            OneShotBreach();
            OneShotCircle();
            OneShotMastery();

            return _builder.Build();
        }

        private void KillBoxFoundation()
        {
            _builder.Create(KillBoxFoundationQuestId, "Scope of Work")
                .PrerequisiteSkill(SkillType.Rifle, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneCzerkaArmsTestRangeKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveCzerkaArmsTestRangeAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillBoxCzerkaTestDocket)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillBoxCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    "Kill six Kill Box adepts in the Czerka Arms Test Range on Nar Shaddaa and secure the Kill Box Czerka Test Docket.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_KillBox_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillBoxCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box Czerka Test Docket is secured. Deliver it to Ressa Vale at the Czerka Arms store on Nar Shaddaa.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void KillBoxMeasure()
        {
            _builder.Create(KillBoxMeasureQuestId, "Terms of Engagement")
                .PrerequisiteQuest(KillBoxFoundationQuestId)
                .PrerequisiteSkill(SkillType.Rifle, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillBoxBlastCellRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillBoxBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "Kill five Kill Box specialists in the Czerka Arms Test Range on Nar Shaddaa and recover the Kill Box Blast-Cell Regulator.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_KillBox_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillBoxBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box Blast-Cell Regulator is recovered. Deliver it to Ressa Vale at the Czerka Arms store on Nar Shaddaa.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void KillBoxBreach()
        {
            _builder.Create(KillBoxBreachQuestId, "Liability Clause")
                .PrerequisiteQuest(KillBoxMeasureQuestId)
                .PrerequisiteSkill(SkillType.Rifle, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillBoxScoredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillBoxScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Kill Box warden in the deep lanes of the Czerka Arms Test Range on Nar Shaddaa and take the Kill Box Scored Range Crest. The warden is too strong to face alone; bring allies.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_KillBox_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillBoxScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box Scored Range Crest is taken from the warden. Return it to Ressa Vale at the Czerka Arms store on Nar Shaddaa.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void KillBoxCircle()
        {
            _builder.Create(KillBoxCircleQuestId, "Succession Dispute")
                .PrerequisiteQuest(KillBoxBreachQuestId)
                .PrerequisiteSkill(SkillType.Rifle, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillBoxCzerkaClearanceChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillBoxCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "Kill the four members of the Kill Box inner circle in the Czerka Arms Test Range on Nar Shaddaa and secure the Kill Box Czerka Clearance Chit.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_KillBox_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillBoxCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box Czerka Clearance Chit is secured. Deliver it to Ressa Vale at the Czerka Arms store on Nar Shaddaa.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void KillBoxMastery()
        {
            _builder.Create(KillBoxMasteryQuestId, "Termination Clause")
                .PrerequisiteQuest(KillBoxCircleQuestId)
                .PrerequisiteSkill(SkillType.Rifle, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Kill Box master in the last lane of the Czerka Arms Test Range on Nar Shaddaa. He is too strong to face alone; bring allies.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_KillBox_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box master is defeated. Return to Ressa Vale at the Czerka Arms store on Nar Shaddaa.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.KillBox);
                });
        }

        private void OneShotFoundation()
        {
            _builder.Create(OneShotFoundationQuestId, "Dry Fire")
                .PrerequisiteSkill(SkillType.Rifle, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneCzerkaArmsTestRangeKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveCzerkaArmsTestRangeAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOneShotCzerkaTestDocket)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOneShotCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    "Kill six One Shot adepts in the Czerka Arms Test Range on Nar Shaddaa and secure the One Shot Czerka Test Docket.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_OneShot_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOneShotCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    "The One Shot Czerka Test Docket is secured. Return it to Orin Tal at the Czerka shipyard office on Nar Shaddaa.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void OneShotMeasure()
        {
            _builder.Create(OneShotMeasureQuestId, "Wind Call")
                .PrerequisiteQuest(OneShotFoundationQuestId)
                .PrerequisiteSkill(SkillType.Rifle, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOneShotBlastCellRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOneShotBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "Kill five One Shot specialists in the Czerka Arms Test Range on Nar Shaddaa and recover the One Shot Blast-Cell Regulator.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_OneShot_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOneShotBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "The One Shot Blast-Cell Regulator is recovered. Return it to Orin Tal at the Czerka shipyard office on Nar Shaddaa.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void OneShotBreach()
        {
            _builder.Create(OneShotBreachQuestId, "Send It")
                .PrerequisiteQuest(OneShotMeasureQuestId)
                .PrerequisiteSkill(SkillType.Rifle, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOneShotScoredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOneShotScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "Defeat the One Shot warden on the long lane of the Czerka Arms Test Range on Nar Shaddaa and take the One Shot Scored Range Crest. The warden is too strong to face alone; bring allies.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_OneShot_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOneShotScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The One Shot Scored Range Crest is taken from the warden. Return it to Orin Tal at the Czerka shipyard office on Nar Shaddaa.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void OneShotCircle()
        {
            _builder.Create(OneShotCircleQuestId, "Reacquire")
                .PrerequisiteQuest(OneShotBreachQuestId)
                .PrerequisiteSkill(SkillType.Rifle, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOneShotCzerkaClearanceChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOneShotCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "Kill the four members of the One Shot inner circle in the Czerka Arms Test Range on Nar Shaddaa and secure the One Shot Czerka Clearance Chit.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_OneShot_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOneShotCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "The One Shot Czerka Clearance Chit is secured. Return it to Orin Tal at the Czerka shipyard office on Nar Shaddaa.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void OneShotMastery()
        {
            _builder.Create(OneShotMasteryQuestId, "One Answer")
                .PrerequisiteQuest(OneShotCircleQuestId)
                .PrerequisiteSkill(SkillType.Rifle, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the One Shot master on the far lane of the Czerka Arms Test Range on Nar Shaddaa. He is too strong to face alone; bring allies.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_OneShot_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The One Shot master is defeated. Return to Orin Tal at the Czerka shipyard office on Nar Shaddaa.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.OneShot);
                });
        }

        private static void RemoveCzerkaArmsTestRangeAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                RifleCapstoneQuestDefinition.KillBoxFoundationQuestId,
                RifleCapstoneQuestDefinition.KillBoxMeasureQuestId,
                RifleCapstoneQuestDefinition.KillBoxBreachQuestId,
                RifleCapstoneQuestDefinition.KillBoxCircleQuestId,
                RifleCapstoneQuestDefinition.KillBoxMasteryQuestId,
                RifleCapstoneQuestDefinition.OneShotFoundationQuestId,
                RifleCapstoneQuestDefinition.OneShotMeasureQuestId,
                RifleCapstoneQuestDefinition.OneShotBreachQuestId,
                RifleCapstoneQuestDefinition.OneShotCircleQuestId,
                RifleCapstoneQuestDefinition.OneShotMasteryQuestId,
                ThrowingCapstoneQuestDefinition.RainOfSteelFoundationQuestId,
                ThrowingCapstoneQuestDefinition.RainOfSteelMeasureQuestId,
                ThrowingCapstoneQuestDefinition.RainOfSteelBreachQuestId,
                ThrowingCapstoneQuestDefinition.RainOfSteelCircleQuestId,
                ThrowingCapstoneQuestDefinition.RainOfSteelMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneCzerkaArmsTestRangeKey, questIds);
        }

        private static void RemoveAreaAccessIfNoLongerNeeded(
            uint player,
            KeyItemType accessKeyItem,
            IEnumerable<string> questIds)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));

            foreach (var questId in questIds)
            {
                if (!dbPlayer.Quests.TryGetValue(questId, out var quest))
                    continue;

                if (quest.TimesCompleted > 0 || quest.CurrentState > 0)
                    return;
            }

            KeyItem.RemoveKeyItem(player, accessKeyItem);
        }
    }
}
