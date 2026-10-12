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
                    "Nalka Rinn gave you the Dathomir Grotto Apex Den Key. Put down six Primal Overrun Adepts, young shell-backs in the Grotto Apex Den on Dathomir, and recover the Primal Overrun Grotto Track Slate from their nest. From the Tribe Village, head north-east along the canyon to the Grottos' east entrance, which opens on the south end of the Grotto Caverns; the den's gate is a short walk north-west of there, on the west side of the Caverns by the cave water.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "You have the Primal Overrun Grotto Track Slate. Return to Nalka Rinn at the Jungle Landing on Dathomir.")
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
                    "Put down five Primal Overrun Specialists, the older shell-backs in the Grotto Apex Den on Dathomir, and recover the Primal Overrun Resonant Fang Charm one of them wears.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "You have the Primal Overrun Resonant Fang Charm. Return to Nalka Rinn at the Jungle Landing on Dathomir.")
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
                    "With two allies you trust, put down the Primal Overrun Warden, a bull rancor in the Grotto Apex Den on Dathomir, and recover the Primal Overrun Cracked Apex Totem from his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "You have the Primal Overrun Cracked Apex Totem. Return to Nalka Rinn at the Jungle Landing on Dathomir.")
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
                    "With your allies, put down the four shell-backs of the Primal Overrun Inner Circle in the Grotto Apex Den on Dathomir and recover the Primal Overrun Den-Mother's Fang Token from their deep nest.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstonePrimalOverrunDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "You have the Primal Overrun Den-Mother's Fang Token. Return to Nalka Rinn at the Jungle Landing on Dathomir.")
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
                    "With your allies, put down the Primal Overrun Master, the old rancor bull who sired the warden, in the Grotto Apex Den on Dathomir. His fall is all the proof Nalka Rinn needs.")
                .AddKillObjective(NPCGroupType.Dathomir_PrimalOverrun_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Primal Overrun Master is dead. Return to Nalka Rinn at the Jungle Landing on Dathomir.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.PrimalOverrun);
                });
        }

        private void UntouchableInstinctFoundation()
        {
            _builder.Create(UntouchableInstinctFoundationQuestId, "First Silent Track")
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
                    "Voro Thane gave you the Dathomir Grotto Apex Den Key. Put down six Untouchable Instinct Adepts, young crystal spiders in the Grotto Apex Den on Dathomir, and recover the Untouchable Instinct Grotto Track Slate from their webs. From the Tribe Village, head north-east along the canyon to the Grottos' east entrance, which opens on the south end of the Grotto Caverns; the den's gate is a short walk north-west of there, on the west side of the Caverns by the cave water.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "You have the Untouchable Instinct Grotto Track Slate. Return to Voro Thane at the Czerka Base on Dathomir.")
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
                    "Put down five Untouchable Instinct Specialists, the older crystal spiders in the Grotto Apex Den on Dathomir, and recover the Untouchable Instinct Resonant Fang Charm one of them wears.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "You have the Untouchable Instinct Resonant Fang Charm. Return to Voro Thane at the Czerka Base on Dathomir.")
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
                    "With two allies you trust, put down the Untouchable Instinct Warden, a giant brown lizard in the Grotto Apex Den on Dathomir, and recover the Untouchable Instinct Cracked Apex Totem from his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "You have the Untouchable Instinct Cracked Apex Totem. Return to Voro Thane at the Czerka Base on Dathomir.")
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
                    "With your allies, put down the four crystal spiders of the Untouchable Instinct Inner Circle in the Grotto Apex Den on Dathomir and recover the Untouchable Instinct Den-Mother's Fang Token from their nest.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUntouchableInstinctDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "You have the Untouchable Instinct Den-Mother's Fang Token. Return to Voro Thane at the Czerka Base on Dathomir.")
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
                    "With your allies, put down the Untouchable Instinct Master, the giant lizard who sired the warden, in the Grotto Apex Den on Dathomir. His fall is all the proof Voro Thane needs.")
                .AddKillObjective(NPCGroupType.Dathomir_UntouchableInstinct_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Untouchable Instinct Master is dead. Return to Voro Thane at the Czerka Base on Dathomir.")
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
                    "Eshka Korr gave you the Dathomir Grotto Apex Den Key. Put down six Force-Bonded Beast Adepts, young wyverns in the Grotto Apex Den on Dathomir, and recover the Force-Bonded Beast Grotto Track Slate from their roost. From the Tribe Village, head north-east along the canyon to the Grottos' east entrance, which opens on the south end of the Grotto Caverns; the den's gate is a short walk north-west of there, on the west side of the Caverns by the cave water.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastGrottoTrackSlate)

                .AddState()
                .SetStateJournalText(
                    "You have the Force-Bonded Beast Grotto Track Slate. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
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
                    "Put down five Force-Bonded Beast Specialists, the older wyverns in the Grotto Apex Den on Dathomir, and recover the Force-Bonded Beast Resonant Fang Charm one of them wears.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastResonantFangCharm)

                .AddState()
                .SetStateJournalText(
                    "You have the Force-Bonded Beast Resonant Fang Charm. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
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
                    "With two allies you trust, put down the Force-Bonded Beast Warden, a wraid in the Grotto Apex Den on Dathomir, and recover the Force-Bonded Beast Cracked Apex Totem bound to his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastCrackedApexTotem)

                .AddState()
                .SetStateJournalText(
                    "You have the Force-Bonded Beast Cracked Apex Totem. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
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
                    "With your allies, put down the four wyverns of the Force-Bonded Beast Inner Circle in the Grotto Apex Den on Dathomir and recover the Force-Bonded Beast Den-Mother's Fang Token from their roost.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneForceBondedBeastDenMothersFangToken)

                .AddState()
                .SetStateJournalText(
                    "You have the Force-Bonded Beast Den-Mother's Fang Token. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
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
                    "With your allies, put down the Force-Bonded Beast Master, the eldest wraid, in the Grotto Apex Den on Dathomir. His fall is all the proof Eshka Korr needs.")
                .AddKillObjective(NPCGroupType.Dathomir_ForceBondedBeast_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Force-Bonded Beast Master is dead. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
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
                    "Talra Venn gave you the Dathomir Tarn Jungle Preserve Key. Put down six Apex Bite Adepts, young scaled brutes in the Tarn Jungle Preserve on Dathomir, and recover the Apex Bite Tarn Hunt Tally from their pen. The preserve's gate is in the north-east part of the Tarnished Jungles, reached by the trail out the north-west side of the Jungle Landing.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "You have the Apex Bite Tarn Hunt Tally. Return to Talra Venn at the Jungle Landing on Dathomir.")
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
                    "Put down five Apex Bite Specialists, the older scaled brutes in the Tarn Jungle Preserve on Dathomir, and recover the Apex Bite Beast-Pen Scent Vial from their pen.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "You have the Apex Bite Beast-Pen Scent Vial. Return to Talra Venn at the Jungle Landing on Dathomir.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ApexBiteBreach()
        {
            _builder.Create(ApexBiteBreachQuestId, "Warden's Notice: Pen Sealed")
                .PrerequisiteQuest(ApexBiteMeasureQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneApexBiteClawedAlphaTotem)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneApexBiteClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "With two allies you trust, put down the Apex Bite Warden, a tukata in the Tarn Jungle Preserve on Dathomir, and recover the Apex Bite Clawed Alpha Totem from his collar.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBiteClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "You have the Apex Bite Clawed Alpha Totem. Return to Talra Venn at the Jungle Landing on Dathomir.")
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
                    "With your allies, put down the four scaled brutes of the Apex Bite Inner Circle in the Tarn Jungle Preserve on Dathomir and recover the Apex Bite Preserve Keeper's Token from the den they hold.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneApexBitePreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "You have the Apex Bite Preserve Keeper's Token. Return to Talra Venn at the Jungle Landing on Dathomir.")
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
                    "With your allies, put down the Apex Bite Master, the old tukata who sired the warden, in the Tarn Jungle Preserve on Dathomir. His fall is all the proof Talra Venn needs.")
                .AddKillObjective(NPCGroupType.Dathomir_ApexBite_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Apex Bite Master is dead. Return to Talra Venn at the Jungle Landing on Dathomir.")
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
                    "Oren Krast gave you the Dathomir Tarn Jungle Preserve Key. Put down six Unbreakable Beast Adepts, young plated burrowers in the Tarn Jungle Preserve on Dathomir, and recover the Unbreakable Beast Tarn Hunt Tally from their pen. The preserve's gate is in the north-east part of the Tarnished Jungles, reached by the trail out the north-west side of the Jungle Landing.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "You have the Unbreakable Beast Tarn Hunt Tally. Return to Oren Krast at the Czerka Base on Dathomir.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void UnbreakableBeastMeasure()
        {
            _builder.Create(UnbreakableBeastMeasureQuestId, "Five Scars, Still Standing")
                .PrerequisiteQuest(UnbreakableBeastFoundationQuestId)
                .PrerequisiteSkill(SkillType.BeastMastery, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "Put down five Unbreakable Beast Specialists, the older plated burrowers in the Tarn Jungle Preserve on Dathomir, and recover the Unbreakable Beast Beast-Pen Scent Vial from their pen.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "You have the Unbreakable Beast Beast-Pen Scent Vial. Return to Oren Krast at the Czerka Base on Dathomir.")
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
                    "With two allies you trust, put down the Unbreakable Beast Warden, a shyrack in the Tarn Jungle Preserve on Dathomir, and recover the Unbreakable Beast Clawed Alpha Totem from his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "You have the Unbreakable Beast Clawed Alpha Totem. Return to Oren Krast at the Czerka Base on Dathomir.")
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
                    "With your allies, put down the four plated burrowers of the Unbreakable Beast Inner Circle in the Tarn Jungle Preserve on Dathomir and recover the Unbreakable Beast Preserve Keeper's Token from their den.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneUnbreakableBeastPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "You have the Unbreakable Beast Preserve Keeper's Token. Return to Oren Krast at the Czerka Base on Dathomir.")
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
                    "With your allies, put down the Unbreakable Beast Master, the old shyrack who sired the warden, in the Tarn Jungle Preserve on Dathomir. His fall is all the proof Oren Krast needs.")
                .AddKillObjective(NPCGroupType.Dathomir_UnbreakableBeast_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Unbreakable Beast Master is dead. Return to Oren Krast at the Czerka Base on Dathomir.")
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
                    "Mira Syth gave you the Dathomir Tarn Jungle Preserve Key. Put down six Alpha Rhythm Adepts, young kinrath in the Tarn Jungle Preserve on Dathomir, and recover the Alpha Rhythm Tarn Hunt Tally from their nest. The preserve's gate is in the north-east part of the Tarnished Jungles, reached by heading south from the Waterfall Ruins through the Tarnished Jungles North.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmTarnHuntTally)

                .AddState()
                .SetStateJournalText(
                    "You have the Alpha Rhythm Tarn Hunt Tally. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
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
                    "Put down five Alpha Rhythm Specialists, the older kinrath in the Tarn Jungle Preserve on Dathomir, and recover the Alpha Rhythm Beast-Pen Scent Vial from their pen.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmBeastPenScentVial)

                .AddState()
                .SetStateJournalText(
                    "You have the Alpha Rhythm Beast-Pen Scent Vial. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
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
                    "With two allies you trust, put down the Alpha Rhythm Warden, an iriaz bull in the Tarn Jungle Preserve on Dathomir, and recover the Alpha Rhythm Clawed Alpha Totem from his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmClawedAlphaTotem)

                .AddState()
                .SetStateJournalText(
                    "You have the Alpha Rhythm Clawed Alpha Totem. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
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
                    "With your allies, put down the four kinrath of the Alpha Rhythm Inner Circle in the Tarn Jungle Preserve on Dathomir and recover the Alpha Rhythm Preserve Keeper's Token from their nest.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneAlphaRhythmPreserveKeepersToken)

                .AddState()
                .SetStateJournalText(
                    "You have the Alpha Rhythm Preserve Keeper's Token. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
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
                    "With your allies, put down the Alpha Rhythm Master, the old iriaz bull the warden learned from, in the Tarn Jungle Preserve on Dathomir. His fall is all the proof Mira Syth needs.")
                .AddKillObjective(NPCGroupType.Dathomir_AlphaRhythm_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Alpha Rhythm Master is dead. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
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
