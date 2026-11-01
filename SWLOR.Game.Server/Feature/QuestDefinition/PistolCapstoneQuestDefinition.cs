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
            _builder.Create(LastWordFoundationQuestId, "Ten Paces")
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
                    "Jek Talin gave you the Anchorhead Canyon Range Key. On Tatooine, enter the Anchorhead Canyon Range through the sealed door in the Rocky Pass, a short walk south of where the trail from Anchorhead's Southern Entrance comes in. Defeat six Canyon Drifters, the Canyon Gunhands who hold the rooms nearest the entrance, and claim the Last Word Canyon Range Tally. Any Canyon Drifter counts, but a kill only counts while your Disarming Shot weakens them.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_CanyonDrifters, "last_word", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastWordCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    "You claimed the Last Word Canyon Range Tally. Deliver it to Jek Talin, by the west trail at Anchorhead - Southern Entrance on Tatooine.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void LastWordMeasure()
        {
            _builder.Create(LastWordMeasureQuestId, "Called Shot")
                .PrerequisiteQuest(LastWordFoundationQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastWordSightlineCalibrator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastWordSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "Defeat five Canyon Sharpshooters, the Canyon Gunhands who hold the deeper rooms of the Anchorhead Canyon Range on Tatooine, and recover the Last Word Sightline Calibrator. Any Canyon Sharpshooter counts, but a kill only counts while your Disarming Shot weakens them.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_CanyonSharpshooters, "last_word", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastWordSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Last Word Sightline Calibrator. Return it to Jek Talin, by the west trail at Anchorhead - Southern Entrance on Tatooine.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void LastWordBreach()
        {
            _builder.Create(LastWordBreachQuestId, "Dead to Rights")
                .PrerequisiteQuest(LastWordMeasureQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastWordShatteredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastWordShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Range Warden Gruvo at the trial altar in the middle of the Anchorhead Canyon Range on Tatooine and take the Last Word Shattered Range Crest.")
                .AddKillObjective(NPCGroupType.Tatooine_RangeWardenGruvo, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastWordShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "You took the Last Word Shattered Range Crest from Range Warden Gruvo. Bring it to Jek Talin, by the west trail at Anchorhead - Southern Entrance on Tatooine.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void LastWordCircle()
        {
            _builder.Create(LastWordCircleQuestId, "No Second Draw")
                .PrerequisiteQuest(LastWordBreachQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneLastWordMarshalsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneLastWordMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "Defeat four High Table Guns, the Canyon Gunhands who guard the deepest part of the Anchorhead Canyon Range on Tatooine, and secure the Last Word Marshal's Challenge Chit. Any High Table Gun counts, but a kill only counts while your Disarming Shot weakens them.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_HighTableGuns, "last_word", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneLastWordMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "You secured the Last Word Marshal's Challenge Chit. Return it to Jek Talin, by the west trail at Anchorhead - Southern Entrance on Tatooine.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void LastWordMastery()
        {
            _builder.Create(LastWordMasteryQuestId, "The Quiet After")
                .PrerequisiteQuest(LastWordCircleQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Range Marshal Wuun Kalo in the Canyon Dueling Pit, through the door at the north end of the Anchorhead Canyon Range on Tatooine.")
                .AddKillObjective(NPCGroupType.Tatooine_RangeMarshalWuunKalo, 1)

                .AddState()
                .SetStateJournalText(
                    "Range Marshal Wuun Kalo is defeated. Report to Jek Talin, by the west trail at Anchorhead - Southern Entrance on Tatooine.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.LastWord);
                });
        }

        private void DeadMansHandFoundation()
        {
            _builder.Create(DeadMansHandFoundationQuestId, "Table Stakes")
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
                    "Pavo Orrel gave you the Anchorhead Canyon Range Key. On Tatooine, enter the Anchorhead Canyon Range through the sealed door in the Rocky Pass, a short walk south of where the trail from Anchorhead's Southern Entrance comes in. Defeat six Canyon Drifters, the Canyon Gunhands who hold the rooms nearest the entrance, and claim the Dead Man's Hand Canyon Range Tally. Any Canyon Drifter counts, but a kill only counts with Quick Draw, Double Shot or Fan the Hammer.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_CanyonDrifters, "dead_mans_hand", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDeadMansHandCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    "You claimed the Dead Man's Hand Canyon Range Tally. Deliver it to Pavo Orrel in the cantina at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void DeadMansHandMeasure()
        {
            _builder.Create(DeadMansHandMeasureQuestId, "Marked Cards")
                .PrerequisiteQuest(DeadMansHandFoundationQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDeadMansHandSightlineCalibrator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDeadMansHandSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "Defeat five Canyon Sharpshooters, the Canyon Gunhands who hold the deeper rooms of the Anchorhead Canyon Range on Tatooine, and recover the Dead Man's Hand Sightline Calibrator. Any Canyon Sharpshooter counts, but a kill only counts with Quick Draw, Double Shot or Fan the Hammer.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_CanyonSharpshooters, "dead_mans_hand", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDeadMansHandSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Dead Man's Hand Sightline Calibrator. Return it to Pavo Orrel in the cantina at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void DeadMansHandBreach()
        {
            _builder.Create(DeadMansHandBreachQuestId, "Calling the Bluff")
                .PrerequisiteQuest(DeadMansHandMeasureQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDeadMansHandShatteredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDeadMansHandShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Range Warden Silas Crowe at the trial altar on the east side of the Anchorhead Canyon Range on Tatooine and take the Dead Man's Hand Shattered Range Crest.")
                .AddKillObjective(NPCGroupType.Tatooine_RangeWardenSilasCrowe, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDeadMansHandShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "You took the Dead Man's Hand Shattered Range Crest from Range Warden Silas Crowe. Bring it to Pavo Orrel in the cantina at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void DeadMansHandCircle()
        {
            _builder.Create(DeadMansHandCircleQuestId, "House Rules")
                .PrerequisiteQuest(DeadMansHandBreachQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDeadMansHandMarshalsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDeadMansHandMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "Defeat four High Table Guns, the Canyon Gunhands who guard the deepest part of the Anchorhead Canyon Range on Tatooine, and secure the Dead Man's Hand Marshal's Challenge Chit. Any High Table Gun counts, but a kill only counts with Quick Draw, Double Shot or Fan the Hammer.")
                .AddCapstoneProofKillObjective(NPCGroupType.Tatooine_HighTableGuns, "dead_mans_hand", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDeadMansHandMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "You secured the Dead Man's Hand Marshal's Challenge Chit. Return it to Pavo Orrel in the cantina at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void DeadMansHandMastery()
        {
            _builder.Create(DeadMansHandMasteryQuestId, "Aces and Eights")
                .PrerequisiteQuest(DeadMansHandCircleQuestId)
                .PrerequisiteSkill(SkillType.Pistol, 50)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat Range Marshal Jonas Harlan in the Canyon Dueling Pit, through the door at the north end of the Anchorhead Canyon Range on Tatooine.")
                .AddKillObjective(NPCGroupType.Tatooine_RangeMarshalJonasHarlan, 1)

                .AddState()
                .SetStateJournalText(
                    "Range Marshal Jonas Harlan is defeated. Report to Pavo Orrel in the cantina at Anchorhead - Junix's Joint on Tatooine.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.DeadMansHand);
                });
        }

        internal static void RemoveAnchorheadCanyonRangeAccessIfNoLongerNeeded(uint player)
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
                TwinPistolCapstoneQuestDefinition.TwinSunsMasteryQuestId,
                TwinPistolCapstoneQuestDefinition.FinalSalvoMasteryQuestId,
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
