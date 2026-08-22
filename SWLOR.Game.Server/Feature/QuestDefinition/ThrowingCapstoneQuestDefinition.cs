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
    public class ThrowingCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string RainOfSteelFoundationQuestId = "rain_of_steel_foundation";
        internal const string RainOfSteelMeasureQuestId = "rain_of_steel_measure";
        internal const string RainOfSteelBreachQuestId = "rain_of_steel_breach";
        internal const string RainOfSteelCircleQuestId = "rain_of_steel_circle";
        internal const string RainOfSteelMasteryQuestId = "rain_of_steel_mastery";
        internal const string RainOfSteelAdeptResref = "cp_rainsteel_ad";
        internal const string RainOfSteelSpecialistResref = "cp_rainsteel_sp";
        internal const string RainOfSteelInnerCircleResref = "cp_rainsteel_ic";
        internal const string PerfectFlurryFoundationQuestId = "perfect_flurry_foundation";
        internal const string PerfectFlurryMeasureQuestId = "perfect_flurry_measure";
        internal const string PerfectFlurryBreachQuestId = "perfect_flurry_breach";
        internal const string PerfectFlurryCircleQuestId = "perfect_flurry_circle";
        internal const string PerfectFlurryMasteryQuestId = "perfect_flurry_mastery";
        internal const string PerfectFlurryAdeptResref = "cp_perflurry_ad";
        internal const string PerfectFlurrySpecialistResref = "cp_perflurry_sp";
        internal const string PerfectFlurryInnerCircleResref = "cp_perflurry_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            RainOfSteelFoundation();
            RainOfSteelMeasure();
            RainOfSteelBreach();
            RainOfSteelCircle();
            RainOfSteelMastery();
            PerfectFlurryFoundation();
            PerfectFlurryMeasure();
            PerfectFlurryBreach();
            PerfectFlurryCircle();
            PerfectFlurryMastery();

            return _builder.Build();
        }

        private void RainOfSteelFoundation()
        {
            _builder.Create(RainOfSteelFoundationQuestId, "Shortage on the Manifest")
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneCzerkaArmsTestRangeKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveCzerkaArmsTestRangeAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRainOfSteelCzerkaTestDocket)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRainOfSteelCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    "Enter the Czerka Arms Test Range on Smuggler's Moon and defeat 6 Rain of Steel adepts, then recover the Rain of Steel Czerka Test Docket from their trial.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRainOfSteelCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Rain of Steel Czerka Test Docket. Deliver it to Varik Dane at the Nar Shaddaa fabrication facility.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void RainOfSteelMeasure()
        {
            _builder.Create(RainOfSteelMeasureQuestId, "Unreturned Issue")
                .PrerequisiteQuest(RainOfSteelFoundationQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRainOfSteelBlastCellRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRainOfSteelBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "Defeat 5 Rain of Steel specialists in the calibration lanes of the Czerka Arms Test Range on Smuggler's Moon and recover the Rain of Steel Blast-Cell Regulator.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRainOfSteelBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Rain of Steel Blast-Cell Regulator. Deliver it to Varik Dane at the Nar Shaddaa fabrication facility.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void RainOfSteelBreach()
        {
            _builder.Create(RainOfSteelBreachQuestId, "Write-Off: One Warden")
                .PrerequisiteQuest(RainOfSteelMeasureQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRainOfSteelScoredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRainOfSteelScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Rain of Steel warden in the deep range of the Czerka Arms Test Range on Smuggler's Moon and take the Rain of Steel Scored Range Crest from him.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRainOfSteelScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "You took the Rain of Steel Scored Range Crest from the warden. Deliver it to Varik Dane at the Nar Shaddaa fabrication facility.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void RainOfSteelCircle()
        {
            _builder.Create(RainOfSteelCircleQuestId, "Four Names in Red Ink")
                .PrerequisiteQuest(RainOfSteelBreachQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRainOfSteelCzerkaClearanceChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRainOfSteelCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "Defeat the 4 members of the Rain of Steel inner circle in the stockrooms of the Czerka Arms Test Range on Smuggler's Moon and recover the Rain of Steel Czerka Clearance Chit.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRainOfSteelCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Rain of Steel Czerka Clearance Chit. Deliver it to Varik Dane at the Nar Shaddaa fabrication facility.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void RainOfSteelMastery()
        {
            _builder.Create(RainOfSteelMasteryQuestId, "Paid in Full")
                .PrerequisiteQuest(RainOfSteelCircleQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Rain of Steel master on the proving floor at the back of the Czerka Arms Test Range on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Rain of Steel master is defeated. Report to Varik Dane at the Nar Shaddaa fabrication facility.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.RainOfSteel);
                });
        }

        private void PerfectFlurryFoundation()
        {
            _builder.Create(PerfectFlurryFoundationQuestId, "The Warm-Up Act")
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneHutlarQionTestSiteKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveHutlarQionTestSiteAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePerfectFlurryQionTestLog)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePerfectFlurryQionTestLog)

                .AddState()
                .SetStateJournalText(
                    "Enter the Hutlar Qion Test Site on Hutlar and defeat 6 Perfect Flurry adepts on the open range, then recover the Perfect Flurry Qion Test Log.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePerfectFlurryQionTestLog)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Perfect Flurry Qion Test Log. Deliver it to Selka Vorn at the Qion Box Canyon on Hutlar.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void PerfectFlurryMeasure()
        {
            _builder.Create(PerfectFlurryMeasureQuestId, "The Five-Blade Wager")
                .PrerequisiteQuest(PerfectFlurryFoundationQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePerfectFlurryCryoRangeRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePerfectFlurryCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "Defeat 5 Perfect Flurry specialists on the cryo range of the Hutlar Qion Test Site on Hutlar and recover the Perfect Flurry Cryo-Range Regulator.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePerfectFlurryCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Perfect Flurry Cryo-Range Regulator. Deliver it to Selka Vorn at the Qion Box Canyon on Hutlar.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void PerfectFlurryBreach()
        {
            _builder.Create(PerfectFlurryBreachQuestId, "The Warden Takes the Stage")
                .PrerequisiteQuest(PerfectFlurryMeasureQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePerfectFlurryFrostburnedTestCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePerfectFlurryFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Perfect Flurry warden in the deep range of the Hutlar Qion Test Site on Hutlar and take the Perfect Flurry Frostburned Test Crest from him.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePerfectFlurryFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "You took the Perfect Flurry Frostburned Test Crest from the warden. Deliver it to Selka Vorn at the Qion Box Canyon on Hutlar.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void PerfectFlurryCircle()
        {
            _builder.Create(PerfectFlurryCircleQuestId, "Four Rivals, One Spotlight")
                .PrerequisiteQuest(PerfectFlurryBreachQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePerfectFlurrySiteChiefsOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePerfectFlurrySiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "Defeat the 4 members of the Perfect Flurry inner circle in the control block of the Hutlar Qion Test Site on Hutlar and recover the Perfect Flurry Site Chief's Override Chip.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePerfectFlurrySiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Perfect Flurry Site Chief's Override Chip. Deliver it to Selka Vorn at the Qion Box Canyon on Hutlar.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void PerfectFlurryMastery()
        {
            _builder.Create(PerfectFlurryMasteryQuestId, "The Final Bow")
                .PrerequisiteQuest(PerfectFlurryCircleQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Perfect Flurry master on the far proving floor of the Hutlar Qion Test Site on Hutlar.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Perfect Flurry master is defeated. Report to Selka Vorn at the Qion Box Canyon on Hutlar.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.PerfectFlurry);
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

        private static void RemoveHutlarQionTestSiteAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                DevicesCapstoneQuestDefinition.ThermalDetonatorFoundationQuestId,
                DevicesCapstoneQuestDefinition.ThermalDetonatorMeasureQuestId,
                DevicesCapstoneQuestDefinition.ThermalDetonatorBreachQuestId,
                DevicesCapstoneQuestDefinition.ThermalDetonatorCircleQuestId,
                DevicesCapstoneQuestDefinition.ThermalDetonatorMasteryQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageFoundationQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageMeasureQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageBreachQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageCircleQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageMasteryQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryFoundationQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryMeasureQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryBreachQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryCircleQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneHutlarQionTestSiteKey, questIds);
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
