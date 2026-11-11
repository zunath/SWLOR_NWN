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
                    "Ressa Vale gave you the Czerka Arms Test Range Key. Kill six Range Washouts, the Range Washouts who hold the rooms nearest the entrance of the Czerka Arms Test Range, whose gate stands near the GSI Tower in the Corporate District on Smuggler's Moon, and secure the Kill Box Czerka Test Docket. Any Range Washout counts, but a kill only counts while your Suppression is on them.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_RangeWashouts, "kill_box", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillBoxCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box Czerka Test Docket is secured. Deliver it to Ressa Vale at the Czerka Arms store off the Hub on Smuggler's Moon.")
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
                    "Kill five Range Instructors, the Range Washouts who hold the deeper rooms of the Czerka Arms Test Range on Smuggler's Moon, and recover the Kill Box Blast-Cell Regulator. Any Range Instructor counts, but a kill only counts while your Suppression is on them.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_RangeInstructors, "kill_box", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillBoxBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box Blast-Cell Regulator is recovered. Deliver it to Ressa Vale at the Czerka Arms store off the Hub on Smuggler's Moon.")
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
                    "With at least two allies, defeat Range Overseer Sskeer in the deep range of the Czerka Arms Test Range on Smuggler's Moon and take the Kill Box Scored Range Crest.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RangeOverseerSskeer, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillBoxScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box Scored Range Crest is taken from Range Overseer Sskeer. Return it to Ressa Vale at the Czerka Arms store off the Hub on Smuggler's Moon.")
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
                    "Kill four Black Cell Shooters, the Range Washouts who guard the deepest part of the Czerka Arms Test Range on Smuggler's Moon, and secure the Kill Box Czerka Clearance Chit. Any Black Cell Shooter counts, but a kill only counts while your Suppression is on them.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_BlackCellShooters, "kill_box", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillBoxCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "The Kill Box Czerka Clearance Chit is secured. Deliver it to Ressa Vale at the Czerka Arms store off the Hub on Smuggler's Moon.")
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
                    "With at least two allies, defeat Range Director Krassek in the Blast-Safe Cell at the far end of the Czerka Arms Test Range on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RangeDirectorKrassek, 1)

                .AddState()
                .SetStateJournalText(
                    "Range Director Krassek is defeated. Return to Ressa Vale at the Czerka Arms store off the Hub on Smuggler's Moon.")
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
                    "Orin Tal gave you the Czerka Arms Test Range Key. Kill six Range Washouts, the Range Washouts who hold the rooms nearest the entrance of the Czerka Arms Test Range, whose gate stands near the GSI Tower in the Corporate District on Smuggler's Moon, and secure the One Shot Czerka Test Docket. Any Range Washout counts, but a kill only counts with Aimed Shot, Piercing Round or Headshot.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_RangeWashouts, "one_shot", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOneShotCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    "The One Shot Czerka Test Docket is secured. Return it to Orin Tal, outside the Czerka shipyard compound on the Landing Pads on Smuggler's Moon.")
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
                    "Kill five Range Instructors, the Range Washouts who hold the deeper rooms of the Czerka Arms Test Range on Smuggler's Moon, and recover the One Shot Blast-Cell Regulator. Any Range Instructor counts, but a kill only counts with Aimed Shot, Piercing Round or Headshot.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_RangeInstructors, "one_shot", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOneShotBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "The One Shot Blast-Cell Regulator is recovered. Return it to Orin Tal, outside the Czerka shipyard compound on the Landing Pads on Smuggler's Moon.")
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
                    "With at least two allies, defeat Range Overseer Callum Breck on the long lane of the Czerka Arms Test Range on Smuggler's Moon and take the One Shot Scored Range Crest.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RangeOverseerCallumBreck, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOneShotScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The One Shot Scored Range Crest is taken from Range Overseer Callum Breck. Return it to Orin Tal, outside the Czerka shipyard compound on the Landing Pads on Smuggler's Moon.")
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
                    "Kill four Black Cell Shooters, the Range Washouts who guard the deepest part of the Czerka Arms Test Range on Smuggler's Moon, and secure the One Shot Czerka Clearance Chit. Any Black Cell Shooter counts, but a kill only counts with Aimed Shot, Piercing Round or Headshot.")
                .AddCapstoneProofKillObjective(NPCGroupType.SmugglersMoon_BlackCellShooters, "one_shot", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOneShotCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "The One Shot Czerka Clearance Chit is secured. Return it to Orin Tal, outside the Czerka shipyard compound on the Landing Pads on Smuggler's Moon.")
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
                    "With at least two allies, defeat Range Director Zhessk in the Blast-Safe Cell at the far end of the Czerka Arms Test Range on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RangeDirectorZhessk, 1)

                .AddState()
                .SetStateJournalText(
                    "Range Director Zhessk is defeated. Return to Orin Tal, outside the Czerka shipyard compound on the Landing Pads on Smuggler's Moon.")
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
