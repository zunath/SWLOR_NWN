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
            _builder.Create(UnmovingCenterFoundationQuestId, "First Principle: Unmoving Center")
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
                    "The Unmoving Center capstone line continues in Anchorhead Canyon Range. Defeat Unmoving Center adepts and secure the Unmoving Center Canyon Range Tally.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnmovingCenterCanyonRangeTally)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Unmoving Center Canyon Range Tally from Anchorhead Canyon Range. Return to Marda Voss for the next Unmoving Center lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void UnmovingCenterMeasure()
        {
            _builder.Create(UnmovingCenterMeasureQuestId, "The Measure of Unmoving Center")
                .PrerequisiteQuest(UnmovingCenterFoundationQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnmovingCenterSightlineCalibrator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnmovingCenterSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center capstone line continues in Anchorhead Canyon Range. Defeat Unmoving Center specialists and secure the Unmoving Center Sightline Calibrator.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnmovingCenterSightlineCalibrator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Unmoving Center Sightline Calibrator from Anchorhead Canyon Range. Return to Marda Voss for the next Unmoving Center lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void UnmovingCenterBreach()
        {
            _builder.Create(UnmovingCenterBreachQuestId, "Fault Line: Unmoving Center")
                .PrerequisiteQuest(UnmovingCenterMeasureQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnmovingCenterShatteredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnmovingCenterShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center capstone line continues in Anchorhead Canyon Range. Defeat the Unmoving Center warden and secure the Unmoving Center Shattered Range Crest.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnmovingCenterShatteredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Unmoving Center Shattered Range Crest from Anchorhead Canyon Range. Return to Marda Voss for the next Unmoving Center lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void UnmovingCenterCircle()
        {
            _builder.Create(UnmovingCenterCircleQuestId, "Circle of Proof: Unmoving Center")
                .PrerequisiteQuest(UnmovingCenterBreachQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnmovingCenterMarshalsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnmovingCenterMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Unmoving Center capstone line continues in Anchorhead Canyon Range. Defeat the Unmoving Center inner circle and secure the Unmoving Center Marshal's Challenge Chit.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnmovingCenterMarshalsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Unmoving Center Marshal's Challenge Chit from Anchorhead Canyon Range. Return to Marda Voss for the next Unmoving Center lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void UnmovingCenterMastery()
        {
            _builder.Create(UnmovingCenterMasteryQuestId, "Unmoving Center Mastery")
                .PrerequisiteQuest(UnmovingCenterCircleQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Unmoving Center master is waiting in Anchorhead Canyon Range. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Tatooine_UnmovingCenter_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Unmoving Center master is defeated. Return to Marda Voss and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.UnmovingCenter);
                });
        }

        private void WorldbreakerFoundation()
        {
            _builder.Create(WorldbreakerFoundationQuestId, "First Principle: Worldbreaker")
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
                    "The Worldbreaker capstone line continues in CZ-220 Breaker Yard. Defeat Worldbreaker adepts and secure the Worldbreaker Breaker Yard Work Order.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneWorldbreakerBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Worldbreaker Breaker Yard Work Order from CZ-220 Breaker Yard. Return to Unit KX-17 for the next Worldbreaker lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void WorldbreakerMeasure()
        {
            _builder.Create(WorldbreakerMeasureQuestId, "The Measure of Worldbreaker")
                .PrerequisiteQuest(WorldbreakerFoundationQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneWorldbreakerJunklineControlRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneWorldbreakerJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker capstone line continues in CZ-220 Breaker Yard. Defeat Worldbreaker specialists and secure the Worldbreaker Junkline Control Relay.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneWorldbreakerJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Worldbreaker Junkline Control Relay from CZ-220 Breaker Yard. Return to Unit KX-17 for the next Worldbreaker lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void WorldbreakerBreach()
        {
            _builder.Create(WorldbreakerBreachQuestId, "Fault Line: Worldbreaker")
                .PrerequisiteQuest(WorldbreakerMeasureQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneWorldbreakerShearedBaySigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneWorldbreakerShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker capstone line continues in CZ-220 Breaker Yard. Defeat the Worldbreaker warden and secure the Worldbreaker Sheared Bay Sigil.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneWorldbreakerShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Worldbreaker Sheared Bay Sigil from CZ-220 Breaker Yard. Return to Unit KX-17 for the next Worldbreaker lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void WorldbreakerCircle()
        {
            _builder.Create(WorldbreakerCircleQuestId, "Circle of Proof: Worldbreaker")
                .PrerequisiteQuest(WorldbreakerBreachQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneWorldbreakerForemansOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneWorldbreakerForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Worldbreaker capstone line continues in CZ-220 Breaker Yard. Defeat the Worldbreaker inner circle and secure the Worldbreaker Foreman's Override Chip.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneWorldbreakerForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Worldbreaker Foreman's Override Chip from CZ-220 Breaker Yard. Return to Unit KX-17 for the next Worldbreaker lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void WorldbreakerMastery()
        {
            _builder.Create(WorldbreakerMasteryQuestId, "Worldbreaker Mastery")
                .PrerequisiteQuest(WorldbreakerCircleQuestId)
                .PrerequisiteSkill(SkillType.Staff, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Worldbreaker master is waiting in CZ-220 Breaker Yard. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.CZ220_Worldbreaker_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Worldbreaker master is defeated. Return to Unit KX-17 and claim the completed lesson.")
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
