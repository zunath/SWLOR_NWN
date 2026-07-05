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
    public class BeastMasteryCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        private const string PrimalOverrunFoundationQuestId = "primal_overrun_foundation";
        private const string PrimalOverrunMeasureQuestId = "primal_overrun_measure";
        private const string PrimalOverrunBreachQuestId = "primal_overrun_breach";
        private const string PrimalOverrunCircleQuestId = "primal_overrun_circle";
        internal const string PrimalOverrunMasteryQuestId = "primal_overrun_mastery";
        internal const string PrimalOverrunAdeptResref = "cp_primover_ad";
        internal const string PrimalOverrunSpecialistResref = "cp_primover_sp";
        internal const string PrimalOverrunInnerCircleResref = "cp_primover_ic";
        private const string UntouchableInstinctFoundationQuestId = "untouchable_instinct_foundation";
        private const string UntouchableInstinctMeasureQuestId = "untouchable_instinct_measure";
        private const string UntouchableInstinctBreachQuestId = "untouchable_instinct_breach";
        private const string UntouchableInstinctCircleQuestId = "untouchable_instinct_circle";
        internal const string UntouchableInstinctMasteryQuestId = "untouchable_instinct_mastery";
        internal const string UntouchableInstinctAdeptResref = "cp_untinst_ad";
        internal const string UntouchableInstinctSpecialistResref = "cp_untinst_sp";
        internal const string UntouchableInstinctInnerCircleResref = "cp_untinst_ic";
        private const string ForceBondedBeastFoundationQuestId = "force_bonded_beast_foundation";
        private const string ForceBondedBeastMeasureQuestId = "force_bonded_beast_measure";
        private const string ForceBondedBeastBreachQuestId = "force_bonded_beast_breach";
        private const string ForceBondedBeastCircleQuestId = "force_bonded_beast_circle";
        internal const string ForceBondedBeastMasteryQuestId = "force_bonded_beast_mastery";
        internal const string ForceBondedBeastAdeptResref = "cp_forcebeast_ad";
        internal const string ForceBondedBeastSpecialistResref = "cp_forcebeast_sp";
        internal const string ForceBondedBeastInnerCircleResref = "cp_forcebeast_ic";
        private const string ApexBiteFoundationQuestId = "apex_bite_foundation";
        private const string ApexBiteMeasureQuestId = "apex_bite_measure";
        private const string ApexBiteBreachQuestId = "apex_bite_breach";
        private const string ApexBiteCircleQuestId = "apex_bite_circle";
        internal const string ApexBiteMasteryQuestId = "apex_bite_mastery";
        internal const string ApexBiteAdeptResref = "cp_apexbite_ad";
        internal const string ApexBiteSpecialistResref = "cp_apexbite_sp";
        internal const string ApexBiteInnerCircleResref = "cp_apexbite_ic";
        private const string UnbreakableBeastFoundationQuestId = "unbreakable_beast_foundation";
        private const string UnbreakableBeastMeasureQuestId = "unbreakable_beast_measure";
        private const string UnbreakableBeastBreachQuestId = "unbreakable_beast_breach";
        private const string UnbreakableBeastCircleQuestId = "unbreakable_beast_circle";
        internal const string UnbreakableBeastMasteryQuestId = "unbreakable_beast_mastery";
        internal const string UnbreakableBeastAdeptResref = "cp_unbrbeast_ad";
        internal const string UnbreakableBeastSpecialistResref = "cp_unbrbeast_sp";
        internal const string UnbreakableBeastInnerCircleResref = "cp_unbrbeast_ic";
        private const string AlphaRhythmFoundationQuestId = "alpha_rhythm_foundation";
        private const string AlphaRhythmMeasureQuestId = "alpha_rhythm_measure";
        private const string AlphaRhythmBreachQuestId = "alpha_rhythm_breach";
        private const string AlphaRhythmCircleQuestId = "alpha_rhythm_circle";
        internal const string AlphaRhythmMasteryQuestId = "alpha_rhythm_mastery";
        internal const string AlphaRhythmAdeptResref = "cp_alpharhy_ad";
        internal const string AlphaRhythmSpecialistResref = "cp_alpharhy_sp";
        internal const string AlphaRhythmInnerCircleResref = "cp_alpharhy_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            PrimalOverrunFoundation();
            PrimalOverrunMeasure();
            PrimalOverrunBreach();
            PrimalOverrunCircle();
            PrimalOverrunMastery();
            UntouchableInstinctFoundation();
            UntouchableInstinctMeasure();
            UntouchableInstinctBreach();
            UntouchableInstinctCircle();
            UntouchableInstinctMastery();
            ForceBondedBeastFoundation();
            ForceBondedBeastMeasure();
            ForceBondedBeastBreach();
            ForceBondedBeastCircle();
            ForceBondedBeastMastery();
            ApexBiteFoundation();
            ApexBiteMeasure();
            ApexBiteBreach();
            ApexBiteCircle();
            ApexBiteMastery();
            UnbreakableBeastFoundation();
            UnbreakableBeastMeasure();
            UnbreakableBeastBreach();
            UnbreakableBeastCircle();
            UnbreakableBeastMastery();
            AlphaRhythmFoundation();
            AlphaRhythmMeasure();
            AlphaRhythmBreach();
            AlphaRhythmCircle();
            AlphaRhythmMastery();

            return _builder.Build();
        }

        private void PrimalOverrunFoundation()
        {
            _builder.Create(PrimalOverrunFoundationQuestId, "First Principle: Primal Overrun")
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDathomirGrottoApexDenKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDathomirGrottoApexDenAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePrimalOverrunGrottoTrackSlate)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePrimalOverrunGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "The Primal Overrun capstone line continues in Dathomir Grotto Apex Den. Defeat Primal Overrun adepts and secure the Primal Overrun Grotto Track Slate.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Primal Overrun Grotto Track Slate from Dathomir Grotto Apex Den. Return to Nalka Rinn for the next Primal Overrun lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void PrimalOverrunMeasure()
        {
            _builder.Create(PrimalOverrunMeasureQuestId, "The Measure of Primal Overrun")
                .PrerequisiteQuest(PrimalOverrunFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePrimalOverrunResonantFangCharm)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePrimalOverrunResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "The Primal Overrun capstone line continues in Dathomir Grotto Apex Den. Defeat Primal Overrun specialists and secure the Primal Overrun Resonant Fang Charm.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Primal Overrun Resonant Fang Charm from Dathomir Grotto Apex Den. Return to Nalka Rinn for the next Primal Overrun lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void PrimalOverrunBreach()
        {
            _builder.Create(PrimalOverrunBreachQuestId, "Fault Line: Primal Overrun")
                .PrerequisiteQuest(PrimalOverrunMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePrimalOverrunCrackedApexTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePrimalOverrunCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "The Primal Overrun capstone line continues in Dathomir Grotto Apex Den. Defeat the Primal Overrun warden and secure the Primal Overrun Cracked Apex Totem.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Primal Overrun Cracked Apex Totem from Dathomir Grotto Apex Den. Return to Nalka Rinn for the next Primal Overrun lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void PrimalOverrunCircle()
        {
            _builder.Create(PrimalOverrunCircleQuestId, "Circle of Proof: Primal Overrun")
                .PrerequisiteQuest(PrimalOverrunBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePrimalOverrunDenMothersFangToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePrimalOverrunDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "The Primal Overrun capstone line continues in Dathomir Grotto Apex Den. Defeat the Primal Overrun inner circle and secure the Primal Overrun Den-Mother's Fang Token.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Primal Overrun Den-Mother's Fang Token from Dathomir Grotto Apex Den. Return to Nalka Rinn for the next Primal Overrun lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void PrimalOverrunMastery()
        {
            _builder.Create(PrimalOverrunMasteryQuestId, "Primal Overrun Mastery")
                .PrerequisiteQuest(PrimalOverrunCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Primal Overrun master is waiting in Dathomir Grotto Apex Den. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Primal Overrun master is defeated. Return to Nalka Rinn and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.PrimalOverrun);
                });
        }

        private void UntouchableInstinctFoundation()
        {
            _builder.Create(UntouchableInstinctFoundationQuestId, "First Principle: Untouchable Instinct")
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDathomirGrottoApexDenKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDathomirGrottoApexDenAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUntouchableInstinctGrottoTrackSlate)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUntouchableInstinctGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "The Untouchable Instinct capstone line continues in Dathomir Grotto Apex Den. Defeat Untouchable Instinct adepts and secure the Untouchable Instinct Grotto Track Slate.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Untouchable Instinct Grotto Track Slate from Dathomir Grotto Apex Den. Return to Voro Thane for the next Untouchable Instinct lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void UntouchableInstinctMeasure()
        {
            _builder.Create(UntouchableInstinctMeasureQuestId, "The Measure of Untouchable Instinct")
                .PrerequisiteQuest(UntouchableInstinctFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUntouchableInstinctResonantFangCharm)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUntouchableInstinctResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "The Untouchable Instinct capstone line continues in Dathomir Grotto Apex Den. Defeat Untouchable Instinct specialists and secure the Untouchable Instinct Resonant Fang Charm.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Untouchable Instinct Resonant Fang Charm from Dathomir Grotto Apex Den. Return to Voro Thane for the next Untouchable Instinct lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void UntouchableInstinctBreach()
        {
            _builder.Create(UntouchableInstinctBreachQuestId, "Fault Line: Untouchable Instinct")
                .PrerequisiteQuest(UntouchableInstinctMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUntouchableInstinctCrackedApexTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUntouchableInstinctCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "The Untouchable Instinct capstone line continues in Dathomir Grotto Apex Den. Defeat the Untouchable Instinct warden and secure the Untouchable Instinct Cracked Apex Totem.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Untouchable Instinct Cracked Apex Totem from Dathomir Grotto Apex Den. Return to Voro Thane for the next Untouchable Instinct lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void UntouchableInstinctCircle()
        {
            _builder.Create(UntouchableInstinctCircleQuestId, "Circle of Proof: Untouchable Instinct")
                .PrerequisiteQuest(UntouchableInstinctBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUntouchableInstinctDenMothersFangToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUntouchableInstinctDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "The Untouchable Instinct capstone line continues in Dathomir Grotto Apex Den. Defeat the Untouchable Instinct inner circle and secure the Untouchable Instinct Den-Mother's Fang Token.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Untouchable Instinct Den-Mother's Fang Token from Dathomir Grotto Apex Den. Return to Voro Thane for the next Untouchable Instinct lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void UntouchableInstinctMastery()
        {
            _builder.Create(UntouchableInstinctMasteryQuestId, "Untouchable Instinct Mastery")
                .PrerequisiteQuest(UntouchableInstinctCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Untouchable Instinct master is waiting in Dathomir Grotto Apex Den. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Untouchable Instinct master is defeated. Return to Voro Thane and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.UntouchableInstinct);
                });
        }

        private void ForceBondedBeastFoundation()
        {
            _builder.Create(ForceBondedBeastFoundationQuestId, "First Principle: Force-Bonded Beast")
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDathomirGrottoApexDenKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDathomirGrottoApexDenAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForceBondedBeastGrottoTrackSlate)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForceBondedBeastGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "The Force-Bonded Beast capstone line continues in Dathomir Grotto Apex Den. Defeat Force-Bonded Beast adepts and secure the Force-Bonded Beast Grotto Track Slate.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Force-Bonded Beast Grotto Track Slate from Dathomir Grotto Apex Den. Return to Eshka Korr for the next Force-Bonded Beast lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ForceBondedBeastMeasure()
        {
            _builder.Create(ForceBondedBeastMeasureQuestId, "The Measure of Force-Bonded Beast")
                .PrerequisiteQuest(ForceBondedBeastFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForceBondedBeastResonantFangCharm)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForceBondedBeastResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "The Force-Bonded Beast capstone line continues in Dathomir Grotto Apex Den. Defeat Force-Bonded Beast specialists and secure the Force-Bonded Beast Resonant Fang Charm.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Force-Bonded Beast Resonant Fang Charm from Dathomir Grotto Apex Den. Return to Eshka Korr for the next Force-Bonded Beast lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ForceBondedBeastBreach()
        {
            _builder.Create(ForceBondedBeastBreachQuestId, "Fault Line: Force-Bonded Beast")
                .PrerequisiteQuest(ForceBondedBeastMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForceBondedBeastCrackedApexTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForceBondedBeastCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "The Force-Bonded Beast capstone line continues in Dathomir Grotto Apex Den. Defeat the Force-Bonded Beast warden and secure the Force-Bonded Beast Cracked Apex Totem.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Force-Bonded Beast Cracked Apex Totem from Dathomir Grotto Apex Den. Return to Eshka Korr for the next Force-Bonded Beast lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ForceBondedBeastCircle()
        {
            _builder.Create(ForceBondedBeastCircleQuestId, "Circle of Proof: Force-Bonded Beast")
                .PrerequisiteQuest(ForceBondedBeastBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForceBondedBeastDenMothersFangToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForceBondedBeastDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "The Force-Bonded Beast capstone line continues in Dathomir Grotto Apex Den. Defeat the Force-Bonded Beast inner circle and secure the Force-Bonded Beast Den-Mother's Fang Token.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Force-Bonded Beast Den-Mother's Fang Token from Dathomir Grotto Apex Den. Return to Eshka Korr for the next Force-Bonded Beast lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ForceBondedBeastMastery()
        {
            _builder.Create(ForceBondedBeastMasteryQuestId, "Force-Bonded Beast Mastery")
                .PrerequisiteQuest(ForceBondedBeastCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Force-Bonded Beast master is waiting in Dathomir Grotto Apex Den. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Force-Bonded Beast master is defeated. Return to Eshka Korr and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.ForceBondedBeast);
                });
        }

        private void ApexBiteFoundation()
        {
            _builder.Create(ApexBiteFoundationQuestId, "First Principle: Apex Bite")
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDathomirTarnJunglePreserveKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDathomirTarnJunglePreserveAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneApexBiteTarnHuntTally)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneApexBiteTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "The Apex Bite capstone line continues in Dathomir Tarn Jungle Preserve. Defeat Apex Bite adepts and secure the Apex Bite Tarn Hunt Tally.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Apex Bite Tarn Hunt Tally from Dathomir Tarn Jungle Preserve. Return to Talra Venn for the next Apex Bite lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ApexBiteMeasure()
        {
            _builder.Create(ApexBiteMeasureQuestId, "The Measure of Apex Bite")
                .PrerequisiteQuest(ApexBiteFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneApexBiteBeastPenScentVial)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneApexBiteBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "The Apex Bite capstone line continues in Dathomir Tarn Jungle Preserve. Defeat Apex Bite specialists and secure the Apex Bite Beast-Pen Scent Vial.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Apex Bite Beast-Pen Scent Vial from Dathomir Tarn Jungle Preserve. Return to Talra Venn for the next Apex Bite lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ApexBiteBreach()
        {
            _builder.Create(ApexBiteBreachQuestId, "Fault Line: Apex Bite")
                .PrerequisiteQuest(ApexBiteMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneApexBiteClawedAlphaTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneApexBiteClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "The Apex Bite capstone line continues in Dathomir Tarn Jungle Preserve. Defeat the Apex Bite warden and secure the Apex Bite Clawed Alpha Totem.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Apex Bite Clawed Alpha Totem from Dathomir Tarn Jungle Preserve. Return to Talra Venn for the next Apex Bite lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ApexBiteCircle()
        {
            _builder.Create(ApexBiteCircleQuestId, "Circle of Proof: Apex Bite")
                .PrerequisiteQuest(ApexBiteBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneApexBitePreserveKeepersToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneApexBitePreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "The Apex Bite capstone line continues in Dathomir Tarn Jungle Preserve. Defeat the Apex Bite inner circle and secure the Apex Bite Preserve Keeper's Token.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBitePreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Apex Bite Preserve Keeper's Token from Dathomir Tarn Jungle Preserve. Return to Talra Venn for the next Apex Bite lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ApexBiteMastery()
        {
            _builder.Create(ApexBiteMasteryQuestId, "Apex Bite Mastery")
                .PrerequisiteQuest(ApexBiteCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Apex Bite master is waiting in Dathomir Tarn Jungle Preserve. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Apex Bite master is defeated. Return to Talra Venn and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.ApexBite);
                });
        }

        private void UnbreakableBeastFoundation()
        {
            _builder.Create(UnbreakableBeastFoundationQuestId, "First Principle: Unbreakable Beast")
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDathomirTarnJunglePreserveKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDathomirTarnJunglePreserveAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnbreakableBeastTarnHuntTally)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnbreakableBeastTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "The Unbreakable Beast capstone line continues in Dathomir Tarn Jungle Preserve. Defeat Unbreakable Beast adepts and secure the Unbreakable Beast Tarn Hunt Tally.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Unbreakable Beast Tarn Hunt Tally from Dathomir Tarn Jungle Preserve. Return to Oren Krast for the next Unbreakable Beast lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void UnbreakableBeastMeasure()
        {
            _builder.Create(UnbreakableBeastMeasureQuestId, "The Measure of Unbreakable Beast")
                .PrerequisiteQuest(UnbreakableBeastFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "The Unbreakable Beast capstone line continues in Dathomir Tarn Jungle Preserve. Defeat Unbreakable Beast specialists and secure the Unbreakable Beast Beast-Pen Scent Vial.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Unbreakable Beast Beast-Pen Scent Vial from Dathomir Tarn Jungle Preserve. Return to Oren Krast for the next Unbreakable Beast lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void UnbreakableBeastBreach()
        {
            _builder.Create(UnbreakableBeastBreachQuestId, "Fault Line: Unbreakable Beast")
                .PrerequisiteQuest(UnbreakableBeastMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnbreakableBeastClawedAlphaTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnbreakableBeastClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "The Unbreakable Beast capstone line continues in Dathomir Tarn Jungle Preserve. Defeat the Unbreakable Beast warden and secure the Unbreakable Beast Clawed Alpha Totem.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Unbreakable Beast Clawed Alpha Totem from Dathomir Tarn Jungle Preserve. Return to Oren Krast for the next Unbreakable Beast lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void UnbreakableBeastCircle()
        {
            _builder.Create(UnbreakableBeastCircleQuestId, "Circle of Proof: Unbreakable Beast")
                .PrerequisiteQuest(UnbreakableBeastBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnbreakableBeastPreserveKeepersToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnbreakableBeastPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "The Unbreakable Beast capstone line continues in Dathomir Tarn Jungle Preserve. Defeat the Unbreakable Beast inner circle and secure the Unbreakable Beast Preserve Keeper's Token.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Unbreakable Beast Preserve Keeper's Token from Dathomir Tarn Jungle Preserve. Return to Oren Krast for the next Unbreakable Beast lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void UnbreakableBeastMastery()
        {
            _builder.Create(UnbreakableBeastMasteryQuestId, "Unbreakable Beast Mastery")
                .PrerequisiteQuest(UnbreakableBeastCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Unbreakable Beast master is waiting in Dathomir Tarn Jungle Preserve. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Unbreakable Beast master is defeated. Return to Oren Krast and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.UnbreakableBeast);
                });
        }

        private void AlphaRhythmFoundation()
        {
            _builder.Create(AlphaRhythmFoundationQuestId, "First Principle: Alpha Rhythm")
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDathomirTarnJunglePreserveKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDathomirTarnJunglePreserveAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAlphaRhythmTarnHuntTally)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAlphaRhythmTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "The Alpha Rhythm capstone line continues in Dathomir Tarn Jungle Preserve. Defeat Alpha Rhythm adepts and secure the Alpha Rhythm Tarn Hunt Tally.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Alpha Rhythm Tarn Hunt Tally from Dathomir Tarn Jungle Preserve. Return to Mira Syth for the next Alpha Rhythm lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void AlphaRhythmMeasure()
        {
            _builder.Create(AlphaRhythmMeasureQuestId, "The Measure of Alpha Rhythm")
                .PrerequisiteQuest(AlphaRhythmFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAlphaRhythmBeastPenScentVial)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAlphaRhythmBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "The Alpha Rhythm capstone line continues in Dathomir Tarn Jungle Preserve. Defeat Alpha Rhythm specialists and secure the Alpha Rhythm Beast-Pen Scent Vial.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Alpha Rhythm Beast-Pen Scent Vial from Dathomir Tarn Jungle Preserve. Return to Mira Syth for the next Alpha Rhythm lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void AlphaRhythmBreach()
        {
            _builder.Create(AlphaRhythmBreachQuestId, "Fault Line: Alpha Rhythm")
                .PrerequisiteQuest(AlphaRhythmMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAlphaRhythmClawedAlphaTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAlphaRhythmClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "The Alpha Rhythm capstone line continues in Dathomir Tarn Jungle Preserve. Defeat the Alpha Rhythm warden and secure the Alpha Rhythm Clawed Alpha Totem.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Alpha Rhythm Clawed Alpha Totem from Dathomir Tarn Jungle Preserve. Return to Mira Syth for the next Alpha Rhythm lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void AlphaRhythmCircle()
        {
            _builder.Create(AlphaRhythmCircleQuestId, "Circle of Proof: Alpha Rhythm")
                .PrerequisiteQuest(AlphaRhythmBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAlphaRhythmPreserveKeepersToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAlphaRhythmPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "The Alpha Rhythm capstone line continues in Dathomir Tarn Jungle Preserve. Defeat the Alpha Rhythm inner circle and secure the Alpha Rhythm Preserve Keeper's Token.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Alpha Rhythm Preserve Keeper's Token from Dathomir Tarn Jungle Preserve. Return to Mira Syth for the next Alpha Rhythm lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void AlphaRhythmMastery()
        {
            _builder.Create(AlphaRhythmMasteryQuestId, "Alpha Rhythm Mastery")
                .PrerequisiteQuest(AlphaRhythmCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Alpha Rhythm master is waiting in Dathomir Tarn Jungle Preserve. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Alpha Rhythm master is defeated. Return to Mira Syth and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.AlphaRhythm);
                });
        }

        private static void RemoveDathomirGrottoApexDenAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                BeastMasteryCapstoneQuestDefinition.PrimalOverrunFoundationQuestId,
                BeastMasteryCapstoneQuestDefinition.PrimalOverrunMeasureQuestId,
                BeastMasteryCapstoneQuestDefinition.PrimalOverrunBreachQuestId,
                BeastMasteryCapstoneQuestDefinition.PrimalOverrunCircleQuestId,
                BeastMasteryCapstoneQuestDefinition.PrimalOverrunMasteryQuestId,
                BeastMasteryCapstoneQuestDefinition.UntouchableInstinctFoundationQuestId,
                BeastMasteryCapstoneQuestDefinition.UntouchableInstinctMeasureQuestId,
                BeastMasteryCapstoneQuestDefinition.UntouchableInstinctBreachQuestId,
                BeastMasteryCapstoneQuestDefinition.UntouchableInstinctCircleQuestId,
                BeastMasteryCapstoneQuestDefinition.UntouchableInstinctMasteryQuestId,
                BeastMasteryCapstoneQuestDefinition.ForceBondedBeastFoundationQuestId,
                BeastMasteryCapstoneQuestDefinition.ForceBondedBeastMeasureQuestId,
                BeastMasteryCapstoneQuestDefinition.ForceBondedBeastBreachQuestId,
                BeastMasteryCapstoneQuestDefinition.ForceBondedBeastCircleQuestId,
                BeastMasteryCapstoneQuestDefinition.ForceBondedBeastMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneDathomirGrottoApexDenKey, questIds);
        }

        private static void RemoveDathomirTarnJunglePreserveAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                BeastMasteryCapstoneQuestDefinition.ApexBiteFoundationQuestId,
                BeastMasteryCapstoneQuestDefinition.ApexBiteMeasureQuestId,
                BeastMasteryCapstoneQuestDefinition.ApexBiteBreachQuestId,
                BeastMasteryCapstoneQuestDefinition.ApexBiteCircleQuestId,
                BeastMasteryCapstoneQuestDefinition.ApexBiteMasteryQuestId,
                BeastMasteryCapstoneQuestDefinition.UnbreakableBeastFoundationQuestId,
                BeastMasteryCapstoneQuestDefinition.UnbreakableBeastMeasureQuestId,
                BeastMasteryCapstoneQuestDefinition.UnbreakableBeastBreachQuestId,
                BeastMasteryCapstoneQuestDefinition.UnbreakableBeastCircleQuestId,
                BeastMasteryCapstoneQuestDefinition.UnbreakableBeastMasteryQuestId,
                BeastMasteryCapstoneQuestDefinition.AlphaRhythmFoundationQuestId,
                BeastMasteryCapstoneQuestDefinition.AlphaRhythmMeasureQuestId,
                BeastMasteryCapstoneQuestDefinition.AlphaRhythmBreachQuestId,
                BeastMasteryCapstoneQuestDefinition.AlphaRhythmCircleQuestId,
                BeastMasteryCapstoneQuestDefinition.AlphaRhythmMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneDathomirTarnJunglePreserveKey, questIds);
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
