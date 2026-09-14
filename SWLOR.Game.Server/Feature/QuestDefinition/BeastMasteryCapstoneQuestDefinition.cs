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
            _builder.Create(PrimalOverrunFoundationQuestId, "Blood on the Old Trail")
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
                    "Nalka Rinn wants six Primal Overrun adepts cleared out of the Dathomir Grotto Apex Den on Dathomir. Secure the Primal Overrun Grotto Track Slate they carry.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "The six adepts are down and the Primal Overrun Grotto Track Slate is secured. Return to Nalka Rinn at the Dathomir Jungle Landing.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void PrimalOverrunMeasure()
        {
            _builder.Create(PrimalOverrunMeasureQuestId, "The Fang Remembers")
                .PrerequisiteQuest(PrimalOverrunFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePrimalOverrunResonantFangCharm)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePrimalOverrunResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "Track down five Primal Overrun specialists in the Dathomir Grotto Apex Den on Dathomir and defeat them. Secure the Primal Overrun Resonant Fang Charm one of them carries.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "The five specialists are defeated and the Primal Overrun Resonant Fang Charm is secured. Return to Nalka Rinn at the Dathomir Jungle Landing.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void PrimalOverrunBreach()
        {
            _builder.Create(PrimalOverrunBreachQuestId, "Where the Warden Waits")
                .PrerequisiteQuest(PrimalOverrunMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePrimalOverrunCrackedApexTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePrimalOverrunCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Primal Overrun warden in the Dathomir Grotto Apex Den on Dathomir. Secure the Primal Overrun Cracked Apex Totem it carries.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "The Primal Overrun warden is defeated and the Primal Overrun Cracked Apex Totem is secured. Return to Nalka Rinn at the Dathomir Jungle Landing.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void PrimalOverrunCircle()
        {
            _builder.Create(PrimalOverrunCircleQuestId, "The Ring Closes")
                .PrerequisiteQuest(PrimalOverrunBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstonePrimalOverrunDenMothersFangToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstonePrimalOverrunDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Primal Overrun inner circle in the Dathomir Grotto Apex Den on Dathomir. Secure the Primal Overrun Den-Mother's Fang Token they hold.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "The Primal Overrun inner circle is defeated and the Primal Overrun Den-Mother's Fang Token is secured. Return to Nalka Rinn at the Dathomir Jungle Landing.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void PrimalOverrunMastery()
        {
            _builder.Create(PrimalOverrunMasteryQuestId, "The Overrun Answers")
                .PrerequisiteQuest(PrimalOverrunCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Primal Overrun master in the Dathomir Grotto Apex Den on Dathomir. There is no item to recover; the master's defeat is the proof.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Primal Overrun master is defeated. Return to Nalka Rinn at the Dathomir Jungle Landing to complete the trial.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.PrimalOverrun);
                });
        }

        private void UntouchableInstinctFoundation()
        {
            _builder.Create(UntouchableInstinctFoundationQuestId, "First Frost")
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
                    "Voro Thane wants six Untouchable Instinct adepts cleared out of the Dathomir Grotto Apex Den on Dathomir. Secure the Untouchable Instinct Grotto Track Slate they carry.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "The six adepts are down and the Untouchable Instinct Grotto Track Slate is secured. Return to Voro Thane at the Czerka base on Dathomir.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void UntouchableInstinctMeasure()
        {
            _builder.Create(UntouchableInstinctMeasureQuestId, "Held Breath")
                .PrerequisiteQuest(UntouchableInstinctFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUntouchableInstinctResonantFangCharm)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUntouchableInstinctResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "Track down five Untouchable Instinct specialists in the Dathomir Grotto Apex Den on Dathomir and defeat them. Secure the Untouchable Instinct Resonant Fang Charm one of them carries.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "The five specialists are defeated and the Untouchable Instinct Resonant Fang Charm is secured. Return to Voro Thane at the Czerka base on Dathomir.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void UntouchableInstinctBreach()
        {
            _builder.Create(UntouchableInstinctBreachQuestId, "Smoke and Distance")
                .PrerequisiteQuest(UntouchableInstinctMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUntouchableInstinctCrackedApexTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUntouchableInstinctCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Untouchable Instinct warden, a fast and elusive single foe, in the Dathomir Grotto Apex Den on Dathomir. Secure the Untouchable Instinct Cracked Apex Totem it carries.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "The Untouchable Instinct warden is defeated and the Untouchable Instinct Cracked Apex Totem is secured. Return to Voro Thane at the Czerka base on Dathomir.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void UntouchableInstinctCircle()
        {
            _builder.Create(UntouchableInstinctCircleQuestId, "The Long Exhale")
                .PrerequisiteQuest(UntouchableInstinctBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUntouchableInstinctDenMothersFangToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUntouchableInstinctDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Untouchable Instinct inner circle in the Dathomir Grotto Apex Den on Dathomir. Secure the Untouchable Instinct Den-Mother's Fang Token they hold.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "The Untouchable Instinct inner circle is defeated and the Untouchable Instinct Den-Mother's Fang Token is secured. Return to Voro Thane at the Czerka base on Dathomir.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void UntouchableInstinctMastery()
        {
            _builder.Create(UntouchableInstinctMasteryQuestId, "Where the Wind Turns")
                .PrerequisiteQuest(UntouchableInstinctCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Untouchable Instinct master, a fast and elusive single foe, in the Dathomir Grotto Apex Den on Dathomir. There is no item to recover; the master's defeat is the proof.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Untouchable Instinct master is defeated. Return to Voro Thane at the Czerka base on Dathomir to complete the trial.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.UntouchableInstinct);
                });
        }

        private void ForceBondedBeastFoundation()
        {
            _builder.Create(ForceBondedBeastFoundationQuestId, "Rite of First Thread")
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
                    "Eshka Korr wants six Force-Bonded Beast adepts cleared out of the Dathomir Grotto Apex Den on Dathomir. Secure the Force-Bonded Beast Grotto Track Slate they carry.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "The six adepts are down and the Force-Bonded Beast Grotto Track Slate is secured. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ForceBondedBeastMeasure()
        {
            _builder.Create(ForceBondedBeastMeasureQuestId, "Rite of the Second Knot")
                .PrerequisiteQuest(ForceBondedBeastFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForceBondedBeastResonantFangCharm)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForceBondedBeastResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "Track down five Force-Bonded Beast specialists in the Dathomir Grotto Apex Den on Dathomir and defeat them. Secure the Force-Bonded Beast Resonant Fang Charm one of them carries.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "The five specialists are defeated and the Force-Bonded Beast Resonant Fang Charm is secured. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ForceBondedBeastBreach()
        {
            _builder.Create(ForceBondedBeastBreachQuestId, "Rite of the Broken Weave")
                .PrerequisiteQuest(ForceBondedBeastMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForceBondedBeastCrackedApexTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForceBondedBeastCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Force-Bonded Beast warden in the Dathomir Grotto Apex Den on Dathomir. Secure the Force-Bonded Beast Cracked Apex Totem it carries.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "The Force-Bonded Beast warden is defeated and the Force-Bonded Beast Cracked Apex Totem is secured. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ForceBondedBeastCircle()
        {
            _builder.Create(ForceBondedBeastCircleQuestId, "Rite of the Fourfold Cord")
                .PrerequisiteQuest(ForceBondedBeastBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneForceBondedBeastDenMothersFangToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneForceBondedBeastDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Force-Bonded Beast inner circle in the Dathomir Grotto Apex Den on Dathomir. Secure the Force-Bonded Beast Den-Mother's Fang Token they hold.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "The Force-Bonded Beast inner circle is defeated and the Force-Bonded Beast Den-Mother's Fang Token is secured. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ForceBondedBeastMastery()
        {
            _builder.Create(ForceBondedBeastMasteryQuestId, "Rite of the Last Thread")
                .PrerequisiteQuest(ForceBondedBeastCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Force-Bonded Beast master in the Dathomir Grotto Apex Den on Dathomir. There is no item to recover; the master's defeat is the proof.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Force-Bonded Beast master is defeated. Return to Eshka Korr at the Waterfall Ruins on Dathomir to complete the trial.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.ForceBondedBeast);
                });
        }

        private void ApexBiteFoundation()
        {
            _builder.Create(ApexBiteFoundationQuestId, "Pen Count: Six Short")
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
                    "Talra Venn wants six Apex Bite adepts cleared out of the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Apex Bite Tarn Hunt Tally they carry.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "The six adepts are down and the Apex Bite Tarn Hunt Tally is secured. Return to Talra Venn at the Dathomir Jungle Landing.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ApexBiteMeasure()
        {
            _builder.Create(ApexBiteMeasureQuestId, "Feed Log Discrepancy")
                .PrerequisiteQuest(ApexBiteFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneApexBiteBeastPenScentVial)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneApexBiteBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "Track down five Apex Bite specialists in the Dathomir Tarn Jungle Preserve on Dathomir and defeat them. Secure the Apex Bite Beast-Pen Scent Vial they are using.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "The five specialists are defeated and the Apex Bite Beast-Pen Scent Vial is secured. Return to Talra Venn at the Dathomir Jungle Landing.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ApexBiteBreach()
        {
            _builder.Create(ApexBiteBreachQuestId, "Warden's Notice: Bay Sealed")
                .PrerequisiteQuest(ApexBiteMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneApexBiteClawedAlphaTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneApexBiteClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Apex Bite warden in the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Apex Bite Clawed Alpha Totem it carries.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "The Apex Bite warden is defeated and the Apex Bite Clawed Alpha Totem is secured. Return to Talra Venn at the Dathomir Jungle Landing.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ApexBiteCircle()
        {
            _builder.Create(ApexBiteCircleQuestId, "Audit Pending: Four Names")
                .PrerequisiteQuest(ApexBiteBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneApexBitePreserveKeepersToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneApexBitePreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Apex Bite inner circle in the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Apex Bite Preserve Keeper's Token they hold.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBitePreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "The Apex Bite inner circle is defeated and the Apex Bite Preserve Keeper's Token is secured. Return to Talra Venn at the Dathomir Jungle Landing.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ApexBiteMastery()
        {
            _builder.Create(ApexBiteMasteryQuestId, "Final Entry: Preserve Reclaimed")
                .PrerequisiteQuest(ApexBiteCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Apex Bite master in the Dathomir Tarn Jungle Preserve on Dathomir. There is no item to recover; the master's defeat is the proof.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Apex Bite master is defeated. Return to Talra Venn at the Dathomir Jungle Landing to complete the trial.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.ApexBite);
                });
        }

        private void UnbreakableBeastFoundation()
        {
            _builder.Create(UnbreakableBeastFoundationQuestId, "Take It And Stand")
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
                    "Oren Krast wants six Unbreakable Beast adepts cleared out of the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Unbreakable Beast Tarn Hunt Tally they carry.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "The six adepts are down and the Unbreakable Beast Tarn Hunt Tally is secured. Return to Oren Krast at the Czerka base on Dathomir.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void UnbreakableBeastMeasure()
        {
            _builder.Create(UnbreakableBeastMeasureQuestId, "Six Scars, Still Standing")
                .PrerequisiteQuest(UnbreakableBeastFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "Track down five Unbreakable Beast specialists in the Dathomir Tarn Jungle Preserve on Dathomir and defeat them. Secure the Unbreakable Beast Beast-Pen Scent Vial they are using.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "The five specialists are defeated and the Unbreakable Beast Beast-Pen Scent Vial is secured. Return to Oren Krast at the Czerka base on Dathomir.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void UnbreakableBeastBreach()
        {
            _builder.Create(UnbreakableBeastBreachQuestId, "The Hide That Doesn't Break")
                .PrerequisiteQuest(UnbreakableBeastMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnbreakableBeastClawedAlphaTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnbreakableBeastClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Unbreakable Beast warden in the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Unbreakable Beast Clawed Alpha Totem it carries.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "The Unbreakable Beast warden is defeated and the Unbreakable Beast Clawed Alpha Totem is secured. Return to Oren Krast at the Czerka base on Dathomir.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void UnbreakableBeastCircle()
        {
            _builder.Create(UnbreakableBeastCircleQuestId, "Four More Than You Can Take")
                .PrerequisiteQuest(UnbreakableBeastBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnbreakableBeastPreserveKeepersToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnbreakableBeastPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Unbreakable Beast inner circle in the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Unbreakable Beast Preserve Keeper's Token they hold.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "The Unbreakable Beast inner circle is defeated and the Unbreakable Beast Preserve Keeper's Token is secured. Return to Oren Krast at the Czerka base on Dathomir.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void UnbreakableBeastMastery()
        {
            _builder.Create(UnbreakableBeastMasteryQuestId, "What Doesn't Break, Holds")
                .PrerequisiteQuest(UnbreakableBeastCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Unbreakable Beast master in the Dathomir Tarn Jungle Preserve on Dathomir. There is no item to recover; the master's defeat is the proof.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Unbreakable Beast master is defeated. Return to Oren Krast at the Czerka base on Dathomir to complete the trial.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.UnbreakableBeast);
                });
        }

        private void AlphaRhythmFoundation()
        {
            _builder.Create(AlphaRhythmFoundationQuestId, "Off the Beat")
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
                    "Mira Syth wants six Alpha Rhythm adepts cleared out of the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Alpha Rhythm Tarn Hunt Tally they carry.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "The six adepts are down and the Alpha Rhythm Tarn Hunt Tally is secured. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void AlphaRhythmMeasure()
        {
            _builder.Create(AlphaRhythmMeasureQuestId, "Counting Five Wrong")
                .PrerequisiteQuest(AlphaRhythmFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAlphaRhythmBeastPenScentVial)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAlphaRhythmBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "Track down five Alpha Rhythm specialists in the Dathomir Tarn Jungle Preserve on Dathomir and defeat them. Secure the Alpha Rhythm Beast-Pen Scent Vial they are using.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "The five specialists are defeated and the Alpha Rhythm Beast-Pen Scent Vial is secured. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void AlphaRhythmBreach()
        {
            _builder.Create(AlphaRhythmBreachQuestId, "Broken Time Signature")
                .PrerequisiteQuest(AlphaRhythmMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAlphaRhythmClawedAlphaTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAlphaRhythmClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Alpha Rhythm warden in the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Alpha Rhythm Clawed Alpha Totem it carries.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "The Alpha Rhythm warden is defeated and the Alpha Rhythm Clawed Alpha Totem is secured. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void AlphaRhythmCircle()
        {
            _builder.Create(AlphaRhythmCircleQuestId, "Four-Part Discord")
                .PrerequisiteQuest(AlphaRhythmBreachQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneAlphaRhythmPreserveKeepersToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneAlphaRhythmPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Alpha Rhythm inner circle in the Dathomir Tarn Jungle Preserve on Dathomir. Secure the Alpha Rhythm Preserve Keeper's Token they hold.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "The Alpha Rhythm inner circle is defeated and the Alpha Rhythm Preserve Keeper's Token is secured. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void AlphaRhythmMastery()
        {
            _builder.Create(AlphaRhythmMasteryQuestId, "The Rhythm Returns")
                .PrerequisiteQuest(AlphaRhythmCircleQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat the Alpha Rhythm master in the Dathomir Tarn Jungle Preserve on Dathomir. There is no item to recover; the master's defeat is the proof.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Alpha Rhythm master is defeated. Return to Mira Syth at the Waterfall Ruins on Dathomir to complete the trial.")
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
