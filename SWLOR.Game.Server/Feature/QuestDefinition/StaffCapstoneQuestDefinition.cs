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
    public class StaffCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string UnmovingCenterFoundationQuestId = "unmoving_center_foundation";
        internal const string UnmovingCenterMeasureQuestId = "unmoving_center_measure";
        internal const string UnmovingCenterBreachQuestId = "unmoving_center_breach";
        internal const string UnmovingCenterCircleQuestId = "unmoving_center_circle";
        internal const string UnmovingCenterMasteryQuestId = "unmoving_center_mastery";
        internal const string UnmovingCenterAdeptResref = "cp_unmovctr_ad";
        internal const string UnmovingCenterSpecialistResref = "cp_unmovctr_sp";
        internal const string UnmovingCenterInnerCircleResref = "cp_unmovctr_ic";
        internal const string WorldbreakerFoundationQuestId = "worldbreaker_foundation";
        internal const string WorldbreakerMeasureQuestId = "worldbreaker_measure";
        internal const string WorldbreakerBreachQuestId = "worldbreaker_breach";
        internal const string WorldbreakerCircleQuestId = "worldbreaker_circle";
        internal const string WorldbreakerMasteryQuestId = "worldbreaker_mastery";
        internal const string WorldbreakerAdeptResref = "cp_worldbrk_ad";
        internal const string WorldbreakerSpecialistResref = "cp_worldbrk_sp";
        internal const string WorldbreakerInnerCircleResref = "cp_worldbrk_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            UnmovingCenterFoundation();
            UnmovingCenterMeasure();
            UnmovingCenterBreach();
            UnmovingCenterCircle();
            UnmovingCenterMastery();
            WorldbreakerFoundation();
            WorldbreakerMeasure();
            WorldbreakerBreach();
            WorldbreakerCircle();
            WorldbreakerMastery();

            return _builder.Build();
        }

        private void UnmovingCenterFoundation()
        {
            _builder.Create(UnmovingCenterFoundationQuestId, "Root Before Wind")
                .PrerequisiteSkill(SkillType.Staff, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneAnchorheadCanyonRangeKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveAnchorheadCanyonRangeAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnmovingCenterCanyonRangeTally)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnmovingCenterCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    "Six Unmoving Center adepts train at Anchorhead Canyon Range on Tatooine. Defeat six of them and recover the Unmoving Center Canyon Range Tally.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnmovingCenterCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center Canyon Range Tally has been recovered from Anchorhead Canyon Range. Return it to Marda Voss at the Anchorhead cantina on Tatooine.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void UnmovingCenterMeasure()
        {
            _builder.Create(UnmovingCenterMeasureQuestId, "The Windless Ground")
                .PrerequisiteQuest(UnmovingCenterFoundationQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnmovingCenterSightlineCalibrator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnmovingCenterSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "Five Unmoving Center specialists train at Anchorhead Canyon Range on Tatooine. Defeat five of them and recover the Unmoving Center Sightline Calibrator.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnmovingCenterSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center Sightline Calibrator has been recovered from Anchorhead Canyon Range. Return it to Marda Voss at the Anchorhead cantina on Tatooine.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void UnmovingCenterBreach()
        {
            _builder.Create(UnmovingCenterBreachQuestId, "Where the Warden Stopped")
                .PrerequisiteQuest(UnmovingCenterMeasureQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnmovingCenterShatteredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnmovingCenterShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center warden holds Anchorhead Canyon Range on Tatooine. Defeat the warden and recover the Unmoving Center Shattered Range Crest.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnmovingCenterShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center Shattered Range Crest has been recovered from Anchorhead Canyon Range. Return it to Marda Voss at the Anchorhead cantina on Tatooine.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void UnmovingCenterCircle()
        {
            _builder.Create(UnmovingCenterCircleQuestId, "Four Who Never Moved")
                .PrerequisiteQuest(UnmovingCenterBreachQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnmovingCenterMarshalsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnmovingCenterMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "Four members of the Unmoving Center inner circle hold Anchorhead Canyon Range on Tatooine. Defeat all four and recover the Unmoving Center Marshal's Challenge Chit.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnmovingCenterMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center Marshal's Challenge Chit has been recovered from Anchorhead Canyon Range. Return it to Marda Voss at the Anchorhead cantina on Tatooine.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void UnmovingCenterMastery()
        {
            _builder.Create(UnmovingCenterMasteryQuestId, "The One Who Stopped Moving")
                .PrerequisiteQuest(UnmovingCenterCircleQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center master holds Anchorhead Canyon Range on Tatooine. Defeat the master.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center master has been defeated. Return to Marda Voss at the Anchorhead cantina on Tatooine.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.UnmovingCenter);
                });
        }

        private void WorldbreakerFoundation()
        {
            _builder.Create(WorldbreakerFoundationQuestId, "Full Load Rating")
                .PrerequisiteSkill(SkillType.Staff, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneCZ220BreakerYardKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveCZ220BreakerYardAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneWorldbreakerBreakerYardWorkOrder)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneWorldbreakerBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    "Six Worldbreaker adepts operate at CZ-220 Breaker Yard on CZ-220. Defeat six of them and recover the Worldbreaker Breaker Yard Work Order.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneWorldbreakerBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker Breaker Yard Work Order has been recovered from CZ-220 Breaker Yard. Return it to Unit KX-17 at the CZ-220 maintenance level.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void WorldbreakerMeasure()
        {
            _builder.Create(WorldbreakerMeasureQuestId, "Stress Reading")
                .PrerequisiteQuest(WorldbreakerFoundationQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneWorldbreakerJunklineControlRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneWorldbreakerJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "Five Worldbreaker specialists operate at CZ-220 Breaker Yard on CZ-220. Defeat five of them and recover the Worldbreaker Junkline Control Relay.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneWorldbreakerJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker Junkline Control Relay has been recovered from CZ-220 Breaker Yard. Return it to Unit KX-17 at the CZ-220 maintenance level.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void WorldbreakerBreach()
        {
            _builder.Create(WorldbreakerBreachQuestId, "Single Point of Failure")
                .PrerequisiteQuest(WorldbreakerMeasureQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneWorldbreakerShearedBaySigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneWorldbreakerShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker warden holds CZ-220 Breaker Yard on CZ-220. Defeat the warden and recover the Worldbreaker Sheared Bay Sigil.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneWorldbreakerShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker Sheared Bay Sigil has been recovered from CZ-220 Breaker Yard. Return it to Unit KX-17 at the CZ-220 maintenance level.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void WorldbreakerCircle()
        {
            _builder.Create(WorldbreakerCircleQuestId, "Redundant Units")
                .PrerequisiteQuest(WorldbreakerBreachQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneWorldbreakerForemansOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneWorldbreakerForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "Four members of the Worldbreaker inner circle hold CZ-220 Breaker Yard on CZ-220. Defeat all four and recover the Worldbreaker Foreman's Override Chip.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneWorldbreakerForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker Foreman's Override Chip has been recovered from CZ-220 Breaker Yard. Return it to Unit KX-17 at the CZ-220 maintenance level.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void WorldbreakerMastery()
        {
            _builder.Create(WorldbreakerMasteryQuestId, "Exceeded Rating")
                .PrerequisiteQuest(WorldbreakerCircleQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker master holds CZ-220 Breaker Yard on CZ-220. Defeat the master.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker master has been defeated. Return to Unit KX-17 at the CZ-220 maintenance level.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.Worldbreaker);
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

        private static void RemoveCZ220BreakerYardAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                KatarCapstoneQuestDefinition.AdamantineGuardFoundationQuestId,
                KatarCapstoneQuestDefinition.AdamantineGuardMeasureQuestId,
                KatarCapstoneQuestDefinition.AdamantineGuardBreachQuestId,
                KatarCapstoneQuestDefinition.AdamantineGuardCircleQuestId,
                KatarCapstoneQuestDefinition.AdamantineGuardMasteryQuestId,
                KatarCapstoneQuestDefinition.ScrapheapLockdownFoundationQuestId,
                KatarCapstoneQuestDefinition.ScrapheapLockdownMeasureQuestId,
                KatarCapstoneQuestDefinition.ScrapheapLockdownBreachQuestId,
                KatarCapstoneQuestDefinition.ScrapheapLockdownCircleQuestId,
                KatarCapstoneQuestDefinition.ScrapheapLockdownMasteryQuestId,
                StaffCapstoneQuestDefinition.WorldbreakerFoundationQuestId,
                StaffCapstoneQuestDefinition.WorldbreakerMeasureQuestId,
                StaffCapstoneQuestDefinition.WorldbreakerBreachQuestId,
                StaffCapstoneQuestDefinition.WorldbreakerCircleQuestId,
                StaffCapstoneQuestDefinition.WorldbreakerMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneCZ220BreakerYardKey, questIds);
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
