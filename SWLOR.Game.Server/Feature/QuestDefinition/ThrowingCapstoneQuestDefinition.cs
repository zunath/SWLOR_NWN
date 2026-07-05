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
            _builder.Create(RainOfSteelFoundationQuestId, "First Principle: Rain of Steel")
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
                    "The Rain of Steel capstone line continues in Czerka Arms Test Range. Defeat Rain of Steel adepts and secure the Rain of Steel Czerka Test Docket.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRainOfSteelCzerkaTestDocket)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Rain of Steel Czerka Test Docket from Czerka Arms Test Range. Return to Varik Dane for the next Rain of Steel lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void RainOfSteelMeasure()
        {
            _builder.Create(RainOfSteelMeasureQuestId, "The Measure of Rain of Steel")
                .PrerequisiteQuest(RainOfSteelFoundationQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRainOfSteelBlastCellRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRainOfSteelBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Rain of Steel capstone line continues in Czerka Arms Test Range. Defeat Rain of Steel specialists and secure the Rain of Steel Blast-Cell Regulator.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRainOfSteelBlastCellRegulator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Rain of Steel Blast-Cell Regulator from Czerka Arms Test Range. Return to Varik Dane for the next Rain of Steel lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void RainOfSteelBreach()
        {
            _builder.Create(RainOfSteelBreachQuestId, "Fault Line: Rain of Steel")
                .PrerequisiteQuest(RainOfSteelMeasureQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRainOfSteelScoredRangeCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRainOfSteelScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    "The Rain of Steel capstone line continues in Czerka Arms Test Range. Defeat the Rain of Steel warden and secure the Rain of Steel Scored Range Crest.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRainOfSteelScoredRangeCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Rain of Steel Scored Range Crest from Czerka Arms Test Range. Return to Varik Dane for the next Rain of Steel lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void RainOfSteelCircle()
        {
            _builder.Create(RainOfSteelCircleQuestId, "Circle of Proof: Rain of Steel")
                .PrerequisiteQuest(RainOfSteelBreachQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneRainOfSteelCzerkaClearanceChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneRainOfSteelCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    "The Rain of Steel capstone line continues in Czerka Arms Test Range. Defeat the Rain of Steel inner circle and secure the Rain of Steel Czerka Clearance Chit.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneRainOfSteelCzerkaClearanceChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Rain of Steel Czerka Clearance Chit from Czerka Arms Test Range. Return to Varik Dane for the next Rain of Steel lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void RainOfSteelMastery()
        {
            _builder.Create(RainOfSteelMasteryQuestId, "Rain of Steel Mastery")
                .PrerequisiteQuest(RainOfSteelCircleQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Rain of Steel master is waiting in Czerka Arms Test Range. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.SmugglersMoon_RainOfSteel_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Rain of Steel master is defeated. Return to Varik Dane and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.RainOfSteel);
                });
        }

        private void PerfectFlurryFoundation()
        {
            _builder.Create(PerfectFlurryFoundationQuestId, "First Principle: Perfect Flurry")
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
                    "The Perfect Flurry capstone line continues in Hutlar Qion Test Site. Defeat Perfect Flurry adepts and secure the Perfect Flurry Qion Test Log.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePerfectFlurryQionTestLog)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Perfect Flurry Qion Test Log from Hutlar Qion Test Site. Return to Selka Vorn for the next Perfect Flurry lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void PerfectFlurryMeasure()
        {
            _builder.Create(PerfectFlurryMeasureQuestId, "The Measure of Perfect Flurry")
                .PrerequisiteQuest(PerfectFlurryFoundationQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePerfectFlurryCryoRangeRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePerfectFlurryCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Perfect Flurry capstone line continues in Hutlar Qion Test Site. Defeat Perfect Flurry specialists and secure the Perfect Flurry Cryo-Range Regulator.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePerfectFlurryCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Perfect Flurry Cryo-Range Regulator from Hutlar Qion Test Site. Return to Selka Vorn for the next Perfect Flurry lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void PerfectFlurryBreach()
        {
            _builder.Create(PerfectFlurryBreachQuestId, "Fault Line: Perfect Flurry")
                .PrerequisiteQuest(PerfectFlurryMeasureQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePerfectFlurryFrostburnedTestCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePerfectFlurryFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "The Perfect Flurry capstone line continues in Hutlar Qion Test Site. Defeat the Perfect Flurry warden and secure the Perfect Flurry Frostburned Test Crest.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePerfectFlurryFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Perfect Flurry Frostburned Test Crest from Hutlar Qion Test Site. Return to Selka Vorn for the next Perfect Flurry lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void PerfectFlurryCircle()
        {
            _builder.Create(PerfectFlurryCircleQuestId, "Circle of Proof: Perfect Flurry")
                .PrerequisiteQuest(PerfectFlurryBreachQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePerfectFlurrySiteChiefsOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePerfectFlurrySiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Perfect Flurry capstone line continues in Hutlar Qion Test Site. Defeat the Perfect Flurry inner circle and secure the Perfect Flurry Site Chief's Override Chip.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePerfectFlurrySiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Perfect Flurry Site Chief's Override Chip from Hutlar Qion Test Site. Return to Selka Vorn for the next Perfect Flurry lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void PerfectFlurryMastery()
        {
            _builder.Create(PerfectFlurryMasteryQuestId, "Perfect Flurry Mastery")
                .PrerequisiteQuest(PerfectFlurryCircleQuestId)
                .PrerequisiteSkill(SkillType.Throwing, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Perfect Flurry master is waiting in Hutlar Qion Test Site. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Hutlar_PerfectFlurry_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Perfect Flurry master is defeated. Return to Selka Vorn and claim the completed lesson.")
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
