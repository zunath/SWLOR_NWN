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
    public class LeadershipCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string HoldTheLineFoundationQuestId = "hold_the_line_foundation";
        internal const string HoldTheLineMeasureQuestId = "hold_the_line_measure";
        internal const string HoldTheLineBreachQuestId = "hold_the_line_breach";
        internal const string HoldTheLineCircleQuestId = "hold_the_line_circle";
        internal const string HoldTheLineMasteryQuestId = "hold_the_line_mastery";
        internal const string HoldTheLineAdeptResref = "cp_holdline_ad";
        internal const string HoldTheLineSpecialistResref = "cp_holdline_sp";
        internal const string HoldTheLineInnerCircleResref = "cp_holdline_ic";
        internal const string DecisiveCommandFoundationQuestId = "decisive_command_foundation";
        internal const string DecisiveCommandMeasureQuestId = "decisive_command_measure";
        internal const string DecisiveCommandBreachQuestId = "decisive_command_breach";
        internal const string DecisiveCommandCircleQuestId = "decisive_command_circle";
        internal const string DecisiveCommandMasteryQuestId = "decisive_command_mastery";
        internal const string DecisiveCommandAdeptResref = "cp_deccommand_ad";
        internal const string DecisiveCommandSpecialistResref = "cp_deccommand_sp";
        internal const string DecisiveCommandInnerCircleResref = "cp_deccommand_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            HoldTheLineFoundation();
            HoldTheLineMeasure();
            HoldTheLineBreach();
            HoldTheLineCircle();
            HoldTheLineMastery();
            DecisiveCommandFoundation();
            DecisiveCommandMeasure();
            DecisiveCommandBreach();
            DecisiveCommandCircle();
            DecisiveCommandMastery();

            return _builder.Build();
        }

        private void HoldTheLineFoundation()
        {
            _builder.Create(HoldTheLineFoundationQuestId, "First Principle: Hold the Line")
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDantooineMedicalSublevelKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDantooineMedicalSublevelAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHoldTheLineTriageWardLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHoldTheLineTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line capstone line continues in Dantooine Medical Sublevel. Defeat Hold the Line adepts and secure the Hold the Line Triage Ward Ledger.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHoldTheLineTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Hold the Line Triage Ward Ledger from Dantooine Medical Sublevel. Return to Edda Maln for the next Hold the Line lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void HoldTheLineMeasure()
        {
            _builder.Create(HoldTheLineMeasureQuestId, "The Measure of Hold the Line")
                .PrerequisiteQuest(HoldTheLineFoundationQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHoldTheLineKoltoConduitCoupler)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHoldTheLineKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line capstone line continues in Dantooine Medical Sublevel. Defeat Hold the Line specialists and secure the Hold the Line Kolto Conduit Coupler.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHoldTheLineKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Hold the Line Kolto Conduit Coupler from Dantooine Medical Sublevel. Return to Edda Maln for the next Hold the Line lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void HoldTheLineBreach()
        {
            _builder.Create(HoldTheLineBreachQuestId, "Fault Line: Hold the Line")
                .PrerequisiteQuest(HoldTheLineMeasureQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHoldTheLineFracturedWardSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHoldTheLineFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line capstone line continues in Dantooine Medical Sublevel. Defeat the Hold the Line warden and secure the Hold the Line Fractured Ward Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHoldTheLineFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Hold the Line Fractured Ward Sigil from Dantooine Medical Sublevel. Return to Edda Maln for the next Hold the Line lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void HoldTheLineCircle()
        {
            _builder.Create(HoldTheLineCircleQuestId, "Circle of Proof: Hold the Line")
                .PrerequisiteQuest(HoldTheLineBreachQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHoldTheLineMatronsWardToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHoldTheLineMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line capstone line continues in Dantooine Medical Sublevel. Defeat the Hold the Line inner circle and secure the Hold the Line Matron's Ward Token.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHoldTheLineMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Hold the Line Matron's Ward Token from Dantooine Medical Sublevel. Return to Edda Maln for the next Hold the Line lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void HoldTheLineMastery()
        {
            _builder.Create(HoldTheLineMasteryQuestId, "Hold the Line Mastery")
                .PrerequisiteQuest(HoldTheLineCircleQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Hold the Line master is waiting in Dantooine Medical Sublevel. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Hold the Line master is defeated. Return to Edda Maln and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.HoldTheLine);
                });
        }

        private void DecisiveCommandFoundation()
        {
            _builder.Create(DecisiveCommandFoundationQuestId, "First Principle: Decisive Command")
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneViscaraRepublicEngineeringBunkerKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveViscaraRepublicEngineeringBunkerAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDecisiveCommandRepublicBunkerDocket)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDecisiveCommandRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command capstone line continues in Viscara Republic Engineering Bunker. Defeat Decisive Command adepts and secure the Decisive Command Republic Bunker Docket.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDecisiveCommandRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Decisive Command Republic Bunker Docket from Viscara Republic Engineering Bunker. Return to Varen Kell for the next Decisive Command lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void DecisiveCommandMeasure()
        {
            _builder.Create(DecisiveCommandMeasureQuestId, "The Measure of Decisive Command")
                .PrerequisiteQuest(DecisiveCommandFoundationQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDecisiveCommandShieldGridRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDecisiveCommandShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command capstone line continues in Viscara Republic Engineering Bunker. Defeat Decisive Command specialists and secure the Decisive Command Shield Grid Relay.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDecisiveCommandShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Decisive Command Shield Grid Relay from Viscara Republic Engineering Bunker. Return to Varen Kell for the next Decisive Command lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void DecisiveCommandBreach()
        {
            _builder.Create(DecisiveCommandBreachQuestId, "Fault Line: Decisive Command")
                .PrerequisiteQuest(DecisiveCommandMeasureQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDecisiveCommandCrackedCommandCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDecisiveCommandCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command capstone line continues in Viscara Republic Engineering Bunker. Defeat the Decisive Command warden and secure the Decisive Command Cracked Command Crest.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDecisiveCommandCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Decisive Command Cracked Command Crest from Viscara Republic Engineering Bunker. Return to Varen Kell for the next Decisive Command lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void DecisiveCommandCircle()
        {
            _builder.Create(DecisiveCommandCircleQuestId, "Circle of Proof: Decisive Command")
                .PrerequisiteQuest(DecisiveCommandBreachQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDecisiveCommandQuartermasterOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDecisiveCommandQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command capstone line continues in Viscara Republic Engineering Bunker. Defeat the Decisive Command inner circle and secure the Decisive Command Quartermaster Override Chip.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDecisiveCommandQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Decisive Command Quartermaster Override Chip from Viscara Republic Engineering Bunker. Return to Varen Kell for the next Decisive Command lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void DecisiveCommandMastery()
        {
            _builder.Create(DecisiveCommandMasteryQuestId, "Decisive Command Mastery")
                .PrerequisiteQuest(DecisiveCommandCircleQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Decisive Command master is waiting in Viscara Republic Engineering Bunker. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Decisive Command master is defeated. Return to Varen Kell and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.DecisiveCommand);
                });
        }

        private static void RemoveDantooineMedicalSublevelAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                FirstAidCapstoneQuestDefinition.EmergencyCocktailFoundationQuestId,
                FirstAidCapstoneQuestDefinition.EmergencyCocktailMeasureQuestId,
                FirstAidCapstoneQuestDefinition.EmergencyCocktailBreachQuestId,
                FirstAidCapstoneQuestDefinition.EmergencyCocktailCircleQuestId,
                FirstAidCapstoneQuestDefinition.EmergencyCocktailMasteryQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineFoundationQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineMeasureQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineBreachQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineCircleQuestId,
                LeadershipCapstoneQuestDefinition.HoldTheLineMasteryQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitFoundationQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitMeasureQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitBreachQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitCircleQuestId,
                SaberstaffCapstoneQuestDefinition.InfiniteConduitMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneDantooineMedicalSublevelKey, questIds);
        }

        private static void RemoveViscaraRepublicEngineeringBunkerAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                DevicesCapstoneQuestDefinition.KillzoneBeaconFoundationQuestId,
                DevicesCapstoneQuestDefinition.KillzoneBeaconMeasureQuestId,
                DevicesCapstoneQuestDefinition.KillzoneBeaconBreachQuestId,
                DevicesCapstoneQuestDefinition.KillzoneBeaconCircleQuestId,
                DevicesCapstoneQuestDefinition.KillzoneBeaconMasteryQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerFoundationQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerMeasureQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerBreachQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerCircleQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerMasteryQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandFoundationQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandMeasureQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandBreachQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandCircleQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneViscaraRepublicEngineeringBunkerKey, questIds);
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
