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
                    "Nalka Rinn gave you the Dathomir Grotto Apex Den Key. Put down six Young Shell-Backs in the rooms nearest the entrance of the Grotto Apex Den on Dathomir (only kills made with your beast's Crushing Slam or Rampage count; any Young Shell-Back counts, as they serve the whole den), and recover the Primal Overrun Grotto Track Slate from their nest. From the Tribe Village, head north-east along the canyon to the Grottos' east entrance, which opens on the south end of the Grotto Caverns; the den is a short walk north-west of there, on the west side of the Caverns by the cave water: enter through the dark cave mouth in the moss-topped rock mound.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_YoungGrottoBeasts, "primal_overrun", 6)
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
                    "Put down five Goad-Scarred Shell-Backs deeper in the Grotto Apex Den on Dathomir (only kills made with your beast's Crushing Slam or Rampage count; any Goad-Scarred Shell-Back counts, as they serve the whole den), and recover the Primal Overrun Resonant Fang Charm one of them wears.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_GoadScarredGrottoBeasts, "primal_overrun", 5)
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
                    "With two allies you trust, put down Handler's Rancor in the Grotto Apex Den on Dathomir, and recover the Primal Overrun Cracked Apex Totem from his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_HandlersRancor, 1)
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
                    "With your allies, put down four Den Elder Shell-Backs in the deepest part of the Grotto Apex Den on Dathomir (only kills made with your beast's Crushing Slam or Rampage count; any Den Elder Shell-Back counts, as they serve the whole den), and recover the Primal Overrun Den-Mother's Fang Token from their deep nest.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_DenElderGrottoBeasts, "primal_overrun", 4)
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
                    "With your allies, put down Old Rancor, the old rancor bull who sired the warden, in the Grotto Apex Den on Dathomir. His fall is all the proof Nalka Rinn needs.")
                .AddKillObjective(NPCGroupType.Dathomir_OldRancor, 1)

                .AddState()
                .SetStateJournalText(
                    "Old Rancor is dead. Return to Nalka Rinn at the Jungle Landing on Dathomir.")
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
                    "Voro Thane gave you the Dathomir Grotto Apex Den Key. Put down six Young Crystal Spiders in the rooms nearest the entrance of the Grotto Apex Den on Dathomir (only kills made with your beast's Assault or Distracting Feint count; any Young Crystal Spider counts, as they serve the whole den), and recover the Untouchable Instinct Grotto Track Slate from their webs. From the Tribe Village, head north-east along the canyon to the Grottos' east entrance, which opens on the south end of the Grotto Caverns; the den is a short walk north-west of there, on the west side of the Caverns by the cave water: enter through the dark cave mouth in the moss-topped rock mound.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_YoungGrottoBeasts, "untouchable_instinct", 6)
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
                    "Put down five Goad-Scarred Crystal Spiders deeper in the Grotto Apex Den on Dathomir (only kills made with your beast's Assault or Distracting Feint count; any Goad-Scarred Crystal Spider counts, as they serve the whole den), and recover the Untouchable Instinct Resonant Fang Charm one of them wears.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_GoadScarredGrottoBeasts, "untouchable_instinct", 5)
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
                    "With two allies you trust, put down Handler's Giant Brown Lizard in the Grotto Apex Den on Dathomir, and recover the Untouchable Instinct Cracked Apex Totem from his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_HandlersGiantBrownLizard, 1)
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
                    "With your allies, put down four Den Elder Crystal Spiders in the deepest part of the Grotto Apex Den on Dathomir (only kills made with your beast's Assault or Distracting Feint count; any Den Elder Crystal Spider counts, as they serve the whole den), and recover the Untouchable Instinct Den-Mother's Fang Token from their nest.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_DenElderGrottoBeasts, "untouchable_instinct", 4)
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
                    "With your allies, put down Old Giant Brown Lizard, the giant lizard who sired the warden, in the Grotto Apex Den on Dathomir. His fall is all the proof Voro Thane needs.")
                .AddKillObjective(NPCGroupType.Dathomir_OldGiantBrownLizard, 1)

                .AddState()
                .SetStateJournalText(
                    "Old Giant Brown Lizard is dead. Return to Voro Thane at the Czerka Base on Dathomir.")
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
                    "Eshka Korr gave you the Dathomir Grotto Apex Den Key. Put down six Young Wyverns in the rooms nearest the entrance of the Grotto Apex Den on Dathomir (keep your beast's Warding Howl shielding you while your party fights, or land kills with its Force Touch; any Young Wyvern the party kills while the Howl is fresh counts, and so does any Young Wyvern your beast's Force Touch hit, as they serve the whole den), and recover the Force-Bonded Beast Grotto Track Slate from their roost. From the Tribe Village, head north-east along the canyon to the Grottos' east entrance, which opens on the south end of the Grotto Caverns; the den is a short walk north-west of there, on the west side of the Caverns by the cave water: enter through the dark cave mouth in the moss-topped rock mound.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_YoungGrottoBeasts, "force_bonded_beast", 6)
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
                    "Put down five Goad-Scarred Wyverns deeper in the Grotto Apex Den on Dathomir (keep your beast's Warding Howl shielding you while your party fights, or land kills with its Force Touch; any Goad-Scarred Wyvern the party kills while the Howl is fresh counts, and so does any Goad-Scarred Wyvern your beast's Force Touch hit, as they serve the whole den), and recover the Force-Bonded Beast Resonant Fang Charm one of them wears.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_GoadScarredGrottoBeasts, "force_bonded_beast", 5)
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
                    "With two allies you trust, put down Handler's Wraid in the Grotto Apex Den on Dathomir, and recover the Force-Bonded Beast Cracked Apex Totem bound to his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_HandlersWraid, 1)
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
                    "With your allies, put down four Den Elder Wyverns in the deepest part of the Grotto Apex Den on Dathomir (keep your beast's Warding Howl shielding you while your party fights, or land kills with its Force Touch; any Den Elder Wyvern the party kills while the Howl is fresh counts, and so does any Den Elder Wyvern your beast's Force Touch hit, as they serve the whole den), and recover the Force-Bonded Beast Den-Mother's Fang Token from their roost.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_DenElderGrottoBeasts, "force_bonded_beast", 4)
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
                    "With your allies, put down Old Wraid, the eldest wraid, in the Grotto Apex Den on Dathomir. His fall is all the proof Eshka Korr needs.")
                .AddKillObjective(NPCGroupType.Dathomir_OldWraid, 1)

                .AddState()
                .SetStateJournalText(
                    "Old Wraid is dead. Return to Eshka Korr at the Waterfall Ruins on Dathomir.")
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
                    "Talra Venn gave you the Dathomir Tarn Jungle Preserve Key. Put down six Young Scaled Brutes in the rooms nearest the entrance of the Tarn Jungle Preserve on Dathomir (only kills made with your beast's Bite, Rending Claw, Pounce or prey strikes count; any Young Scaled Brute counts, as they serve the whole preserve), and recover the Apex Bite Tarn Hunt Tally from their pen. The preserve's gate is in the north-east part of the Tarnished Jungles, reached by the trail out the north-west side of the Jungle Landing.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_YoungPreserveBeasts, "apex_bite", 6)
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
                    "Put down five Pen-Scarred Scaled Brutes deeper in the Tarn Jungle Preserve on Dathomir (only kills made with your beast's Bite, Rending Claw, Pounce or prey strikes count; any Pen-Scarred Scaled Brute counts, as they serve the whole preserve), and recover the Apex Bite Beast-Pen Scent Vial from their pen.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_PenScarredPreserveBeasts, "apex_bite", 5)
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
                    "With two allies you trust, put down Breeder's Tukata in the Tarn Jungle Preserve on Dathomir, and recover the Apex Bite Clawed Alpha Totem from his collar.")
                .AddKillObjective(NPCGroupType.Dathomir_BreedersTukata, 1)
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
                    "With your allies, put down four Elder Scaled Brutes in the deepest part of the Tarn Jungle Preserve on Dathomir (only kills made with your beast's Bite, Rending Claw, Pounce or prey strikes count; any Elder Scaled Brute counts, as they serve the whole preserve), and recover the Apex Bite Preserve Keeper's Token from the den they hold.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_ElderPreserveBeasts, "apex_bite", 4)
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
                    "With your allies, put down Old Tukata, the old tukata who sired the warden, in the Tarn Jungle Preserve on Dathomir. His fall is all the proof Talra Venn needs.")
                .AddKillObjective(NPCGroupType.Dathomir_OldTukata, 1)

                .AddState()
                .SetStateJournalText(
                    "Old Tukata is dead. Return to Talra Venn at the Jungle Landing on Dathomir.")
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
                    "Oren Krast gave you the Dathomir Tarn Jungle Preserve Key. Put down six Young Plated Burrowers in the rooms nearest the entrance of the Tarn Jungle Preserve on Dathomir (keep your beast intercepting for you or holding Guarding Roar while your party fights; any Young Plated Burrower the party kills while that guard is fresh counts, as they serve the whole preserve), and recover the Unbreakable Beast Tarn Hunt Tally from their pen. The preserve's gate is in the north-east part of the Tarnished Jungles, reached by the trail out the north-west side of the Jungle Landing.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_YoungPreserveBeasts, "unbreakable_beast", 6)
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
                    "Put down five Pen-Scarred Plated Burrowers deeper in the Tarn Jungle Preserve on Dathomir (keep your beast intercepting for you or holding Guarding Roar while your party fights; any Pen-Scarred Plated Burrower the party kills while that guard is fresh counts, as they serve the whole preserve), and recover the Unbreakable Beast Beast-Pen Scent Vial from their pen.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_PenScarredPreserveBeasts, "unbreakable_beast", 5)
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
                    "With two allies you trust, put down Breeder's Shyrack in the Tarn Jungle Preserve on Dathomir, and recover the Unbreakable Beast Clawed Alpha Totem from his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_BreedersShyrack, 1)
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
                    "With your allies, put down four Elder Plated Burrowers in the deepest part of the Tarn Jungle Preserve on Dathomir (keep your beast intercepting for you or holding Guarding Roar while your party fights; any Elder Plated Burrower the party kills while that guard is fresh counts, as they serve the whole preserve), and recover the Unbreakable Beast Preserve Keeper's Token from their den.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_ElderPreserveBeasts, "unbreakable_beast", 4)
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
                    "With your allies, put down Old Shyrack, the old shyrack who sired the warden, in the Tarn Jungle Preserve on Dathomir. His fall is all the proof Oren Krast needs.")
                .AddKillObjective(NPCGroupType.Dathomir_OldShyrack, 1)

                .AddState()
                .SetStateJournalText(
                    "Old Shyrack is dead. Return to Oren Krast at the Czerka Base on Dathomir.")
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
                    "Mira Syth gave you the Dathomir Tarn Jungle Preserve Key. Put down six Young Kinrath in the rooms nearest the entrance of the Tarn Jungle Preserve on Dathomir (only kills made with your beast's Claw, Guarded Bite or Coordinated Strike count; any Young Kinrath counts, as they serve the whole preserve), and recover the Alpha Rhythm Tarn Hunt Tally from their nest. The preserve's gate is in the north-east part of the Tarnished Jungles, reached by heading south from the Waterfall Ruins through the Tarnished Jungles North.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_YoungPreserveBeasts, "alpha_rhythm", 6)
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
                    "Put down five Pen-Scarred Kinrath deeper in the Tarn Jungle Preserve on Dathomir (only kills made with your beast's Claw, Guarded Bite or Coordinated Strike count; any Pen-Scarred Kinrath counts, as they serve the whole preserve), and recover the Alpha Rhythm Beast-Pen Scent Vial from their pen.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_PenScarredPreserveBeasts, "alpha_rhythm", 5)
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
                    "With two allies you trust, put down Breeder's Iriaz Bull in the Tarn Jungle Preserve on Dathomir, and recover the Alpha Rhythm Clawed Alpha Totem from his harness.")
                .AddKillObjective(NPCGroupType.Dathomir_BreedersIriazBull, 1)
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
                    "With your allies, put down four Elder Kinrath in the deepest part of the Tarn Jungle Preserve on Dathomir (only kills made with your beast's Claw, Guarded Bite or Coordinated Strike count; any Elder Kinrath counts, as they serve the whole preserve), and recover the Alpha Rhythm Preserve Keeper's Token from their nest.")
                .AddCapstoneProofKillObjective(NPCGroupType.Dathomir_ElderPreserveBeasts, "alpha_rhythm", 4)
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
                    "With your allies, put down Old Iriaz Bull, the old iriaz bull the warden learned from, in the Tarn Jungle Preserve on Dathomir. His fall is all the proof Mira Syth needs.")
                .AddKillObjective(NPCGroupType.Dathomir_OldIriazBull, 1)

                .AddState()
                .SetStateJournalText(
                    "Old Iriaz Bull is dead. Return to Mira Syth at the Waterfall Ruins on Dathomir.")
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
