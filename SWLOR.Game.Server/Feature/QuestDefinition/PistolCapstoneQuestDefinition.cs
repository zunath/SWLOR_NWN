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
    public class PistolCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string LastWordFoundationQuestId = "last_word_foundation";
        internal const string LastWordMeasureQuestId = "last_word_measure";
        internal const string LastWordBreachQuestId = "last_word_breach";
        internal const string LastWordCircleQuestId = "last_word_circle";
        internal const string LastWordMasteryQuestId = "last_word_mastery";
        internal const string LastWordAdeptResref = "cp_lastword_ad";
        internal const string LastWordSpecialistResref = "cp_lastword_sp";
        internal const string LastWordInnerCircleResref = "cp_lastword_ic";
        internal const string DeadMansHandFoundationQuestId = "dead_mans_hand_foundation";
        internal const string DeadMansHandMeasureQuestId = "dead_mans_hand_measure";
        internal const string DeadMansHandBreachQuestId = "dead_mans_hand_breach";
        internal const string DeadMansHandCircleQuestId = "dead_mans_hand_circle";
        internal const string DeadMansHandMasteryQuestId = "dead_mans_hand_mastery";
        internal const string DeadMansHandAdeptResref = "cp_deadhand_ad";
        internal const string DeadMansHandSpecialistResref = "cp_deadhand_sp";
        internal const string DeadMansHandInnerCircleResref = "cp_deadhand_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            LastWordFoundation();
            LastWordMeasure();
            LastWordBreach();
            LastWordCircle();
            LastWordMastery();
            DeadMansHandFoundation();
            DeadMansHandMeasure();
            DeadMansHandBreach();
            DeadMansHandCircle();
            DeadMansHandMastery();

            return _builder.Build();
        }

        private void LastWordFoundation()
        {
            _builder.Create(LastWordFoundationQuestId, "First Principle: Last Word")
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneAnchorheadCanyonRangeKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveAnchorheadCanyonRangeAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastWordCanyonRangeTally)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastWordCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    "The Last Word capstone line continues in Anchorhead Canyon Range. Defeat Last Word adepts and secure the Last Word Canyon Range Tally.")
                .AddKillObjective(NPCGroupType.Tatooine_LastWord_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastWordCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Last Word Canyon Range Tally from Anchorhead Canyon Range. Return to Jek Talin for the next Last Word lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void LastWordMeasure()
        {
            _builder.Create(LastWordMeasureQuestId, "The Measure of Last Word")
                .PrerequisiteQuest(LastWordFoundationQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastWordSightlineCalibrator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastWordSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "The Last Word capstone line continues in Anchorhead Canyon Range. Defeat Last Word specialists and secure the Last Word Sightline Calibrator.")
                .AddKillObjective(NPCGroupType.Tatooine_LastWord_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastWordSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Last Word Sightline Calibrator from Anchorhead Canyon Range. Return to Jek Talin for the next Last Word lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void LastWordBreach()
        {
            _builder.Create(LastWordBreachQuestId, "Fault Line: Last Word")
                .PrerequisiteQuest(LastWordMeasureQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastWordShatteredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastWordShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The Last Word capstone line continues in Anchorhead Canyon Range. Defeat the Last Word warden and secure the Last Word Shattered Range Crest.")
                .AddKillObjective(NPCGroupType.Tatooine_LastWord_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastWordShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Last Word Shattered Range Crest from Anchorhead Canyon Range. Return to Jek Talin for the next Last Word lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void LastWordCircle()
        {
            _builder.Create(LastWordCircleQuestId, "Circle of Proof: Last Word")
                .PrerequisiteQuest(LastWordBreachQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastWordMarshalsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastWordMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Last Word capstone line continues in Anchorhead Canyon Range. Defeat the Last Word inner circle and secure the Last Word Marshal's Challenge Chit.")
                .AddKillObjective(NPCGroupType.Tatooine_LastWord_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastWordMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Last Word Marshal's Challenge Chit from Anchorhead Canyon Range. Return to Jek Talin for the next Last Word lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void LastWordMastery()
        {
            _builder.Create(LastWordMasteryQuestId, "Last Word Mastery")
                .PrerequisiteQuest(LastWordCircleQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Last Word master is waiting in Anchorhead Canyon Range. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Tatooine_LastWord_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Last Word master is defeated. Return to Jek Talin and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.LastWord);
                });
        }

        private void DeadMansHandFoundation()
        {
            _builder.Create(DeadMansHandFoundationQuestId, "First Principle: Dead Man's Hand")
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneAnchorheadCanyonRangeKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveAnchorheadCanyonRangeAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDeadMansHandCanyonRangeTally)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDeadMansHandCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    "The Dead Man's Hand capstone line continues in Anchorhead Canyon Range. Defeat Dead Man's Hand adepts and secure the Dead Man's Hand Canyon Range Tally.")
                .AddKillObjective(NPCGroupType.Tatooine_DeadMansHand_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDeadMansHandCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Dead Man's Hand Canyon Range Tally from Anchorhead Canyon Range. Return to Pavo Orrel for the next Dead Man's Hand lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void DeadMansHandMeasure()
        {
            _builder.Create(DeadMansHandMeasureQuestId, "The Measure of Dead Man's Hand")
                .PrerequisiteQuest(DeadMansHandFoundationQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDeadMansHandSightlineCalibrator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDeadMansHandSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "The Dead Man's Hand capstone line continues in Anchorhead Canyon Range. Defeat Dead Man's Hand specialists and secure the Dead Man's Hand Sightline Calibrator.")
                .AddKillObjective(NPCGroupType.Tatooine_DeadMansHand_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDeadMansHandSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Dead Man's Hand Sightline Calibrator from Anchorhead Canyon Range. Return to Pavo Orrel for the next Dead Man's Hand lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void DeadMansHandBreach()
        {
            _builder.Create(DeadMansHandBreachQuestId, "Fault Line: Dead Man's Hand")
                .PrerequisiteQuest(DeadMansHandMeasureQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDeadMansHandShatteredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDeadMansHandShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The Dead Man's Hand capstone line continues in Anchorhead Canyon Range. Defeat the Dead Man's Hand warden and secure the Dead Man's Hand Shattered Range Crest.")
                .AddKillObjective(NPCGroupType.Tatooine_DeadMansHand_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDeadMansHandShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Dead Man's Hand Shattered Range Crest from Anchorhead Canyon Range. Return to Pavo Orrel for the next Dead Man's Hand lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void DeadMansHandCircle()
        {
            _builder.Create(DeadMansHandCircleQuestId, "Circle of Proof: Dead Man's Hand")
                .PrerequisiteQuest(DeadMansHandBreachQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDeadMansHandMarshalsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDeadMansHandMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Dead Man's Hand capstone line continues in Anchorhead Canyon Range. Defeat the Dead Man's Hand inner circle and secure the Dead Man's Hand Marshal's Challenge Chit.")
                .AddKillObjective(NPCGroupType.Tatooine_DeadMansHand_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDeadMansHandMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Dead Man's Hand Marshal's Challenge Chit from Anchorhead Canyon Range. Return to Pavo Orrel for the next Dead Man's Hand lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void DeadMansHandMastery()
        {
            _builder.Create(DeadMansHandMasteryQuestId, "Dead Man's Hand Mastery")
                .PrerequisiteQuest(DeadMansHandCircleQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Dead Man's Hand master is waiting in Anchorhead Canyon Range. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Tatooine_DeadMansHand_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Dead Man's Hand master is defeated. Return to Pavo Orrel and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.DeadMansHand);
                });
        }

        private static void RemoveAnchorheadCanyonRangeAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                PistolCapstoneQuestDefinition.LastWordFoundationQuestId,
                PistolCapstoneQuestDefinition.LastWordMeasureQuestId,
                PistolCapstoneQuestDefinition.LastWordBreachQuestId,
                PistolCapstoneQuestDefinition.LastWordCircleQuestId,
                PistolCapstoneQuestDefinition.LastWordMasteryQuestId,
                PistolCapstoneQuestDefinition.DeadMansHandFoundationQuestId,
                PistolCapstoneQuestDefinition.DeadMansHandMeasureQuestId,
                PistolCapstoneQuestDefinition.DeadMansHandBreachQuestId,
                PistolCapstoneQuestDefinition.DeadMansHandCircleQuestId,
                PistolCapstoneQuestDefinition.DeadMansHandMasteryQuestId,
                StaffCapstoneQuestDefinition.UnmovingCenterFoundationQuestId,
                StaffCapstoneQuestDefinition.UnmovingCenterMeasureQuestId,
                StaffCapstoneQuestDefinition.UnmovingCenterBreachQuestId,
                StaffCapstoneQuestDefinition.UnmovingCenterCircleQuestId,
                StaffCapstoneQuestDefinition.UnmovingCenterMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneAnchorheadCanyonRangeKey, questIds);
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
