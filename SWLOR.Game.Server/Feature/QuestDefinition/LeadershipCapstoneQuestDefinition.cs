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
            _builder.Create(HoldTheLineFoundationQuestId, "Intake Stays Open")
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
                    "Use the Dantooine Medical Sublevel Key to enter the Dantooine Medical Sublevel on Dantooine. Defeat six Hold the Line adepts in the triage hall and recover the Hold the Line Triage Ward Ledger.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHoldTheLineTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line Triage Ward Ledger has been recovered. Return it to Edda Maln at the Dantooine Republic Garrison.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void HoldTheLineMeasure()
        {
            _builder.Create(HoldTheLineMeasureQuestId, "Kolto at Full Pressure")
                .PrerequisiteQuest(HoldTheLineFoundationQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHoldTheLineKoltoConduitCoupler)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHoldTheLineKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "Return to the Dantooine Medical Sublevel on Dantooine. Defeat five Hold the Line specialists at the conduit junctions and recover the Hold the Line Kolto Conduit Coupler.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHoldTheLineKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line Kolto Conduit Coupler has been recovered. Bring it to Edda Maln at the Dantooine Republic Garrison.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void HoldTheLineBreach()
        {
            _builder.Create(HoldTheLineBreachQuestId, "Breach in Recovery")
                .PrerequisiteQuest(HoldTheLineMeasureQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHoldTheLineFracturedWardSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHoldTheLineFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Hold the Line warden at the recovery ward doors in the Dantooine Medical Sublevel on Dantooine and recover the Hold the Line Fractured Ward Sigil. The warden is too strong to face alone; bring companions.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHoldTheLineFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line Fractured Ward Sigil has been recovered from the warden. Return it to Edda Maln at the Dantooine Republic Garrison.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void HoldTheLineCircle()
        {
            _builder.Create(HoldTheLineCircleQuestId, "Every Name Answers")
                .PrerequisiteQuest(HoldTheLineBreachQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneHoldTheLineMatronsWardToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneHoldTheLineMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Hold the Line inner circle in the deep wards of the Dantooine Medical Sublevel on Dantooine and recover the Hold the Line Matron's Ward Token.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneHoldTheLineMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line Matron's Ward Token has been recovered. Return it to Edda Maln at the Dantooine Republic Garrison.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void HoldTheLineMastery()
        {
            _builder.Create(HoldTheLineMasteryQuestId, "The Last Bed Held")
                .PrerequisiteQuest(HoldTheLineCircleQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Hold the Line master in the last ward of the Dantooine Medical Sublevel on Dantooine. The master is too strong to face alone; bring companions.")
                .AddKillObjective(NPCGroupType.Dantooine_HoldTheLine_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Hold the Line master is defeated. Return to Edda Maln at the Dantooine Republic Garrison.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.HoldTheLine);
                });
        }

        private void DecisiveCommandFoundation()
        {
            _builder.Create(DecisiveCommandFoundationQuestId, "Orders Are Not Requests")
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
                    "Use the Viscara Republic Engineering Bunker Key to enter the Viscara Republic Engineering Bunker on Viscara. Defeat six Decisive Command adepts in the outer galleries and recover the Decisive Command Republic Bunker Docket.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDecisiveCommandRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command Republic Bunker Docket has been recovered. Return it to Varen Kell at the Republic Base combat deck on Viscara.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void DecisiveCommandMeasure()
        {
            _builder.Create(DecisiveCommandMeasureQuestId, "Faster Than Fear")
                .PrerequisiteQuest(DecisiveCommandFoundationQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDecisiveCommandShieldGridRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDecisiveCommandShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "Return to the Viscara Republic Engineering Bunker on Viscara. Defeat five Decisive Command specialists in the shield grid chambers and recover the Decisive Command Shield Grid Relay.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDecisiveCommandShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command Shield Grid Relay has been recovered. Bring it to Varen Kell at the Republic Base combat deck on Viscara.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void DecisiveCommandBreach()
        {
            _builder.Create(DecisiveCommandBreachQuestId, "One Voice in the Breach")
                .PrerequisiteQuest(DecisiveCommandMeasureQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDecisiveCommandCrackedCommandCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDecisiveCommandCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Decisive Command warden at the inner blast doors of the Viscara Republic Engineering Bunker on Viscara and recover the Decisive Command Cracked Command Crest. The warden is too strong to face alone; bring companions.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDecisiveCommandCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command Cracked Command Crest has been recovered from the warden. Return it to Varen Kell at the Republic Base combat deck on Viscara.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void DecisiveCommandCircle()
        {
            _builder.Create(DecisiveCommandCircleQuestId, "A Chain Has No Spare Links")
                .PrerequisiteQuest(DecisiveCommandBreachQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneDecisiveCommandQuartermasterOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneDecisiveCommandQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Decisive Command inner circle in the deep stores of the Viscara Republic Engineering Bunker on Viscara and recover the Decisive Command Quartermaster Override Chip.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneDecisiveCommandQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command Quartermaster Override Chip has been recovered. Return it to Varen Kell at the Republic Base combat deck on Viscara.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void DecisiveCommandMastery()
        {
            _builder.Create(DecisiveCommandMasteryQuestId, "The Last Order Stands")
                .PrerequisiteQuest(DecisiveCommandCircleQuestId)
                .PrerequisiteSkill(SkillType.Leadership, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Decisive Command master in the command room of the Viscara Republic Engineering Bunker on Viscara. The master is too strong to face alone; bring companions.")
                .AddKillObjective(NPCGroupType.Viscara_DecisiveCommand_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Decisive Command master is defeated. Return to Varen Kell at the Republic Base combat deck on Viscara.")
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
