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
            _builder.Create(AdamantineGuardFoundationQuestId, "Six Names on the Work Order")
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
                    "Tressa Kade gave you the CZ-220 Breaker Yard Key. A wreck crew is drilling katar guard-work in the CZ-220 Breaker Yard on CZ-220; enter through the Breaker Bay force-field wall in the far north-east corner of the Maintenance Level, below the Offices & Labs. Defeat six Wreck Crew Scrappers, who hold the floor nearest the entrance, and recover the Adamantine Guard Breaker Yard Work Order. Any Scrapper counts, whichever katar line the crew drilled for, but only kills made while you guard an ally with Steel Shoulder or hold Whirling Guard or Iron Wall.")
                .AddCapstoneProofKillObjective(NPCGroupType.CZ220_WreckCrewScrappers, "adamantine_guard", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAdamantineGuardBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Adamantine Guard Breaker Yard Work Order. Deliver it to Tressa Kade in the Offices & Labs on CZ-220.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void AdamantineGuardMeasure()
        {
            _builder.Create(AdamantineGuardMeasureQuestId, "Pull the Junkline Relay")
                .PrerequisiteQuest(AdamantineGuardFoundationQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAdamantineGuardJunklineControlRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAdamantineGuardJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "The wreck crew rerouted the junkline in the CZ-220 Breaker Yard on CZ-220. Defeat five Wreck Crew Riggers deeper inside and recover the Adamantine Guard Junkline Control Relay. Any Rigger counts, whichever katar line the crew drilled for, but only kills made while you guard an ally with Steel Shoulder or hold Whirling Guard or Iron Wall.")
                .AddCapstoneProofKillObjective(NPCGroupType.CZ220_WreckCrewRiggers, "adamantine_guard", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAdamantineGuardJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Adamantine Guard Junkline Control Relay. Deliver it to Tressa Kade in the Offices & Labs on CZ-220.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void AdamantineGuardBreach()
        {
            _builder.Create(AdamantineGuardBreachQuestId, "Two Spotters for the Sheared Bay")
                .PrerequisiteQuest(AdamantineGuardMeasureQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAdamantineGuardShearedBaySigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAdamantineGuardShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "The wreck crew's warden holds the sheared bay in the CZ-220 Breaker Yard on CZ-220. Bring at least two fighters you trust, defeat the warden, Crew Chief Rakkesh, and recover the Adamantine Guard Sheared Bay Sigil.")
                .AddKillObjective(NPCGroupType.CZ220_CrewChiefRakkesh, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAdamantineGuardShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Adamantine Guard Sheared Bay Sigil. Deliver it to Tressa Kade in the Offices & Labs on CZ-220.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void AdamantineGuardCircle()
        {
            _builder.Create(AdamantineGuardCircleQuestId, "Four Names Off the Roster")
                .PrerequisiteQuest(AdamantineGuardBreachQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAdamantineGuardForemansOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAdamantineGuardForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The wreck crew's inner circle holds the deepest part of the yard, the far south-east end of the Breaker Bay in the CZ-220 Breaker Yard on CZ-220. Defeat four Foreman's Hands and recover the Adamantine Guard Foreman's Override Chip. Any Foreman's Hand counts, whichever katar line the crew drilled for, but only kills made while you guard an ally with Steel Shoulder or hold Whirling Guard or Iron Wall.")
                .AddCapstoneProofKillObjective(NPCGroupType.CZ220_ForemansHands, "adamantine_guard", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAdamantineGuardForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Adamantine Guard Foreman's Override Chip. Deliver it to Tressa Kade in the Offices & Labs on CZ-220.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void AdamantineGuardMastery()
        {
            _builder.Create(AdamantineGuardMasteryQuestId, "The Last Order on the Book")
                .PrerequisiteQuest(AdamantineGuardCircleQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)

                .AddState()
                .SetStateJournalText(
                    "The wreck crew's foreman holds Bay One, deep in the CZ-220 Breaker Yard on CZ-220. Bring fighters you trust and defeat Wreck Foreman Ssurgan, the master.")
                .AddKillObjective(NPCGroupType.CZ220_WreckForemanSsurgan, 1)

                .AddState()
                .SetStateJournalText(
                    "You defeated Wreck Foreman Ssurgan. Return to Tressa Kade in the Offices & Labs on CZ-220 to close out the last work order.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.AdamantineGuard);
                });
        }

        private void ScrapheapLockdownFoundation()
        {
            _builder.Create(ScrapheapLockdownFoundationQuestId, "Six Unlisted Names")
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
                    "Borrik Sen gave you the CZ-220 Breaker Yard Key. A crew has sealed off the CZ-220 Breaker Yard on CZ-220 for unauthorized katar drills; enter through the Breaker Bay force-field wall in the far north-east corner of the Maintenance Level, below the Offices & Labs. Defeat six Wreck Crew Scrappers, who hold the floor nearest the entrance, and recover the Scrapheap Lockdown Breaker Yard Work Order. Any Scrapper counts, whichever katar line the crew drilled for, but only kills made after hobbling them with Hooking Strike or dazing them with Joint Lock.")
                .AddCapstoneProofKillObjective(NPCGroupType.CZ220_WreckCrewScrappers, "scrapheap_lockdown", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneScrapheapLockdownBreakerYardWorkOrder)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Scrapheap Lockdown Breaker Yard Work Order. Deliver it to Borrik Sen in the Hangar on CZ-220.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ScrapheapLockdownMeasure()
        {
            _builder.Create(ScrapheapLockdownMeasureQuestId, "Chain of Custody, Broken")
                .PrerequisiteQuest(ScrapheapLockdownFoundationQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneScrapheapLockdownJunklineControlRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneScrapheapLockdownJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "The crew rerouted the junkline into a stand-off grid in the CZ-220 Breaker Yard on CZ-220. Defeat five Wreck Crew Riggers deeper inside and recover the Scrapheap Lockdown Junkline Control Relay. Any Rigger counts, whichever katar line the crew drilled for, but only kills made after hobbling them with Hooking Strike or dazing them with Joint Lock.")
                .AddCapstoneProofKillObjective(NPCGroupType.CZ220_WreckCrewRiggers, "scrapheap_lockdown", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneScrapheapLockdownJunklineControlRelay)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Scrapheap Lockdown Junkline Control Relay. Deliver it to Borrik Sen in the Hangar on CZ-220.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ScrapheapLockdownBreach()
        {
            _builder.Create(ScrapheapLockdownBreachQuestId, "Containment Failure, Sheared Bay")
                .PrerequisiteQuest(ScrapheapLockdownMeasureQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneScrapheapLockdownShearedBaySigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneScrapheapLockdownShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "The crew's warden has fortified the sheared bay in the CZ-220 Breaker Yard on CZ-220. Go three-strong, defeat the warden, Crew Chief Tobin Rask, and recover the Scrapheap Lockdown Sheared Bay Sigil.")
                .AddKillObjective(NPCGroupType.CZ220_CrewChiefTobinRask, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneScrapheapLockdownShearedBaySigil)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Scrapheap Lockdown Sheared Bay Sigil. Deliver it to Borrik Sen in the Hangar on CZ-220.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ScrapheapLockdownCircle()
        {
            _builder.Create(ScrapheapLockdownCircleQuestId, "Conspiracy on the Crane Deck")
                .PrerequisiteQuest(ScrapheapLockdownBreachQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneScrapheapLockdownForemansOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneScrapheapLockdownForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The crew's inner circle holds the deepest part of the yard, the east side of the Breaker Bay in the CZ-220 Breaker Yard on CZ-220. Defeat four Foreman's Hands and recover the Scrapheap Lockdown Foreman's Override Chip. Any Foreman's Hand counts, whichever katar line the crew drilled for, but only kills made after hobbling them with Hooking Strike or dazing them with Joint Lock.")
                .AddCapstoneProofKillObjective(NPCGroupType.CZ220_ForemansHands, "scrapheap_lockdown", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneScrapheapLockdownForemansOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Scrapheap Lockdown Foreman's Override Chip. Deliver it to Borrik Sen in the Hangar on CZ-220.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ScrapheapLockdownMastery()
        {
            _builder.Create(ScrapheapLockdownMasteryQuestId, "Final Incident, Case Closed")
                .PrerequisiteQuest(ScrapheapLockdownCircleQuestId)
                .PrerequisiteSkill(SkillType.Katar, 50)

                .AddState()
                .SetStateJournalText(
                    "The man who wrote the lockdown protocol holds the deepest bay of the CZ-220 Breaker Yard on CZ-220. Go three-strong, defeat Wreck Foreman Lucan Brandt, the master, and end the lockdown.")
                .AddKillObjective(NPCGroupType.CZ220_WreckForemanLucanBrandt, 1)

                .AddState()
                .SetStateJournalText(
                    "You defeated Wreck Foreman Lucan Brandt. Return to Borrik Sen in the Hangar on CZ-220 to close the case.")
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
