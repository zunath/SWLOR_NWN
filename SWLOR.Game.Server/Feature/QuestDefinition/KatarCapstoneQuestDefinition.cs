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
    public class KatarCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string AdamantineGuardFoundationQuestId = "adamantine_guard_foundation";
        internal const string AdamantineGuardMeasureQuestId = "adamantine_guard_measure";
        internal const string AdamantineGuardBreachQuestId = "adamantine_guard_breach";
        internal const string AdamantineGuardCircleQuestId = "adamantine_guard_circle";
        internal const string AdamantineGuardMasteryQuestId = "adamantine_guard_mastery";
        internal const string AdamantineGuardAdeptResref = "cp_adamguard_ad";
        internal const string AdamantineGuardSpecialistResref = "cp_adamguard_sp";
        internal const string AdamantineGuardInnerCircleResref = "cp_adamguard_ic";
        internal const string ScrapheapLockdownFoundationQuestId = "scrapheap_lockdown_foundation";
        internal const string ScrapheapLockdownMeasureQuestId = "scrapheap_lockdown_measure";
        internal const string ScrapheapLockdownBreachQuestId = "scrapheap_lockdown_breach";
        internal const string ScrapheapLockdownCircleQuestId = "scrapheap_lockdown_circle";
        internal const string ScrapheapLockdownMasteryQuestId = "scrapheap_lockdown_mastery";
        internal const string ScrapheapLockdownAdeptResref = "cp_scraplock_ad";
        internal const string ScrapheapLockdownSpecialistResref = "cp_scraplock_sp";
        internal const string ScrapheapLockdownInnerCircleResref = "cp_scraplock_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            AdamantineGuardFoundation();
            AdamantineGuardMeasure();
            AdamantineGuardBreach();
            AdamantineGuardCircle();
            AdamantineGuardMastery();
            ScrapheapLockdownFoundation();
            ScrapheapLockdownMeasure();
            ScrapheapLockdownBreach();
            ScrapheapLockdownCircle();
            ScrapheapLockdownMastery();

            return _builder.Build();
        }

        private void AdamantineGuardFoundation()
        {
            _builder.Create(AdamantineGuardFoundationQuestId, "First Principle: Adamantine Guard")
                .PrerequisiteSkill(SkillType.Katar, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneCZ220BreakerYardKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveCZ220BreakerYardAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAdamantineGuardBreakerYardWorkOrder)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAdamantineGuardBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    "The Adamantine Guard capstone line continues in CZ-220 Breaker Yard. Defeat Adamantine Guard adepts and secure the Adamantine Guard Breaker Yard Work Order.")
                .AddKillObjective(NPCGroupType.CZ220_AdamantineGuard_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAdamantineGuardBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Adamantine Guard Breaker Yard Work Order from CZ-220 Breaker Yard. Return to Tressa Kade for the next Adamantine Guard lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void AdamantineGuardMeasure()
        {
            _builder.Create(AdamantineGuardMeasureQuestId, "The Measure of Adamantine Guard")
                .PrerequisiteQuest(AdamantineGuardFoundationQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAdamantineGuardJunklineControlRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAdamantineGuardJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "The Adamantine Guard capstone line continues in CZ-220 Breaker Yard. Defeat Adamantine Guard specialists and secure the Adamantine Guard Junkline Control Relay.")
                .AddKillObjective(NPCGroupType.CZ220_AdamantineGuard_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAdamantineGuardJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Adamantine Guard Junkline Control Relay from CZ-220 Breaker Yard. Return to Tressa Kade for the next Adamantine Guard lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void AdamantineGuardBreach()
        {
            _builder.Create(AdamantineGuardBreachQuestId, "Fault Line: Adamantine Guard")
                .PrerequisiteQuest(AdamantineGuardMeasureQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAdamantineGuardShearedBaySigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAdamantineGuardShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "The Adamantine Guard capstone line continues in CZ-220 Breaker Yard. Defeat the Adamantine Guard warden and secure the Adamantine Guard Sheared Bay Sigil.")
                .AddKillObjective(NPCGroupType.CZ220_AdamantineGuard_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAdamantineGuardShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Adamantine Guard Sheared Bay Sigil from CZ-220 Breaker Yard. Return to Tressa Kade for the next Adamantine Guard lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void AdamantineGuardCircle()
        {
            _builder.Create(AdamantineGuardCircleQuestId, "Circle of Proof: Adamantine Guard")
                .PrerequisiteQuest(AdamantineGuardBreachQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAdamantineGuardForemansOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAdamantineGuardForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Adamantine Guard capstone line continues in CZ-220 Breaker Yard. Defeat the Adamantine Guard inner circle and secure the Adamantine Guard Foreman's Override Chip.")
                .AddKillObjective(NPCGroupType.CZ220_AdamantineGuard_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAdamantineGuardForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Adamantine Guard Foreman's Override Chip from CZ-220 Breaker Yard. Return to Tressa Kade for the next Adamantine Guard lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void AdamantineGuardMastery()
        {
            _builder.Create(AdamantineGuardMasteryQuestId, "Adamantine Guard Mastery")
                .PrerequisiteQuest(AdamantineGuardCircleQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Adamantine Guard master is waiting in CZ-220 Breaker Yard. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.CZ220_AdamantineGuard_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Adamantine Guard master is defeated. Return to Tressa Kade and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.AdamantineGuard);
                });
        }

        private void ScrapheapLockdownFoundation()
        {
            _builder.Create(ScrapheapLockdownFoundationQuestId, "First Principle: Scrapheap Lockdown")
                .PrerequisiteSkill(SkillType.Katar, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneCZ220BreakerYardKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveCZ220BreakerYardAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneScrapheapLockdownBreakerYardWorkOrder)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneScrapheapLockdownBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    "The Scrapheap Lockdown capstone line continues in CZ-220 Breaker Yard. Defeat Scrapheap Lockdown adepts and secure the Scrapheap Lockdown Breaker Yard Work Order.")
                .AddKillObjective(NPCGroupType.CZ220_ScrapheapLockdown_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneScrapheapLockdownBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Scrapheap Lockdown Breaker Yard Work Order from CZ-220 Breaker Yard. Return to Borrik Sen for the next Scrapheap Lockdown lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ScrapheapLockdownMeasure()
        {
            _builder.Create(ScrapheapLockdownMeasureQuestId, "The Measure of Scrapheap Lockdown")
                .PrerequisiteQuest(ScrapheapLockdownFoundationQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneScrapheapLockdownJunklineControlRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneScrapheapLockdownJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "The Scrapheap Lockdown capstone line continues in CZ-220 Breaker Yard. Defeat Scrapheap Lockdown specialists and secure the Scrapheap Lockdown Junkline Control Relay.")
                .AddKillObjective(NPCGroupType.CZ220_ScrapheapLockdown_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneScrapheapLockdownJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Scrapheap Lockdown Junkline Control Relay from CZ-220 Breaker Yard. Return to Borrik Sen for the next Scrapheap Lockdown lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ScrapheapLockdownBreach()
        {
            _builder.Create(ScrapheapLockdownBreachQuestId, "Fault Line: Scrapheap Lockdown")
                .PrerequisiteQuest(ScrapheapLockdownMeasureQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneScrapheapLockdownShearedBaySigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneScrapheapLockdownShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "The Scrapheap Lockdown capstone line continues in CZ-220 Breaker Yard. Defeat the Scrapheap Lockdown warden and secure the Scrapheap Lockdown Sheared Bay Sigil.")
                .AddKillObjective(NPCGroupType.CZ220_ScrapheapLockdown_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneScrapheapLockdownShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Scrapheap Lockdown Sheared Bay Sigil from CZ-220 Breaker Yard. Return to Borrik Sen for the next Scrapheap Lockdown lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ScrapheapLockdownCircle()
        {
            _builder.Create(ScrapheapLockdownCircleQuestId, "Circle of Proof: Scrapheap Lockdown")
                .PrerequisiteQuest(ScrapheapLockdownBreachQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneScrapheapLockdownForemansOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneScrapheapLockdownForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Scrapheap Lockdown capstone line continues in CZ-220 Breaker Yard. Defeat the Scrapheap Lockdown inner circle and secure the Scrapheap Lockdown Foreman's Override Chip.")
                .AddKillObjective(NPCGroupType.CZ220_ScrapheapLockdown_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneScrapheapLockdownForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Scrapheap Lockdown Foreman's Override Chip from CZ-220 Breaker Yard. Return to Borrik Sen for the next Scrapheap Lockdown lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ScrapheapLockdownMastery()
        {
            _builder.Create(ScrapheapLockdownMasteryQuestId, "Scrapheap Lockdown Mastery")
                .PrerequisiteQuest(ScrapheapLockdownCircleQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Scrapheap Lockdown master is waiting in CZ-220 Breaker Yard. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.CZ220_ScrapheapLockdown_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Scrapheap Lockdown master is defeated. Return to Borrik Sen and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.ScrapheapLockdown);
                });
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
