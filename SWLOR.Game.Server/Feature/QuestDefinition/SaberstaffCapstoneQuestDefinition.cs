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
    public class SaberstaffCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string SaberCycloneFoundationQuestId = "saber_cyclone_foundation";
        internal const string SaberCycloneMeasureQuestId = "saber_cyclone_measure";
        internal const string SaberCycloneBreachQuestId = "saber_cyclone_breach";
        internal const string SaberCycloneCircleQuestId = "saber_cyclone_circle";
        internal const string SaberCycloneMasteryQuestId = "saber_cyclone_mastery";
        internal const string SaberCycloneAdeptResref = "cp_sabcycl_ad";
        internal const string SaberCycloneSpecialistResref = "cp_sabcycl_sp";
        internal const string SaberCycloneInnerCircleResref = "cp_sabcycl_ic";
        internal const string InfiniteConduitFoundationQuestId = "infinite_conduit_foundation";
        internal const string InfiniteConduitMeasureQuestId = "infinite_conduit_measure";
        internal const string InfiniteConduitBreachQuestId = "infinite_conduit_breach";
        internal const string InfiniteConduitCircleQuestId = "infinite_conduit_circle";
        internal const string InfiniteConduitMasteryQuestId = "infinite_conduit_mastery";
        internal const string InfiniteConduitAdeptResref = "cp_infconduit_ad";
        internal const string InfiniteConduitSpecialistResref = "cp_infconduit_sp";
        internal const string InfiniteConduitInnerCircleResref = "cp_infconduit_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            SaberCycloneFoundation();
            SaberCycloneMeasure();
            SaberCycloneBreach();
            SaberCycloneCircle();
            SaberCycloneMastery();
            InfiniteConduitFoundation();
            InfiniteConduitMeasure();
            InfiniteConduitBreach();
            InfiniteConduitCircle();
            InfiniteConduitMastery();

            return _builder.Build();
        }

        private void SaberCycloneFoundation()
        {
            _builder.Create(SaberCycloneFoundationQuestId, "Step Into the Turn")
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneHutlarJediTrialHallsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveHutlarJediTrialHallsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberCycloneEnclaveTrialSlate)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberCycloneEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "Jora Sel gave you the Hutlar Jedi Trial Halls Key. From the Hutlar Outpost, take the north exit onto the Qion Tundra, go north-west into the Qion Valley, then west into the Cloning Test Site. At the site's north end, follow the big pipe east from the concrete ruin to an ornate stone pillar; through the sealed door in the pillar's east face, disable six Cyclone Drill Droids in the Jedi Trial Halls and recover the Saber Cyclone Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Hutlar_CycloneDrillDroid, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Saber Cyclone Enclave Trial Slate. Return to Jora Sel in Tochee on Tatooine.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void SaberCycloneMeasure()
        {
            _builder.Create(SaberCycloneMeasureQuestId, "Carry the Momentum")
                .PrerequisiteQuest(SaberCycloneFoundationQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberCycloneKyberFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberCycloneKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "Disable five Cyclone Duelist Droids deeper in the Jedi Trial Halls on Hutlar and recover the Saber Cyclone Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Hutlar_CycloneDuelistDroid, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Saber Cyclone Kyber Focus Shard. Return to Jora Sel in Tochee on Tatooine.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void SaberCycloneBreach()
        {
            _builder.Create(SaberCycloneBreachQuestId, "The Warden's Orbit")
                .PrerequisiteQuest(SaberCycloneMeasureQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberCycloneFracturedTrialSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberCycloneFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, disable the Cyclone Gatekeeper Droid guarding the approach to the Saber Trial Chamber in the Jedi Trial Halls on Hutlar and recover the Saber Cyclone Fractured Trial Sigil.")
                .AddKillObjective(NPCGroupType.Hutlar_CycloneGatekeeperDroid, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Saber Cyclone Fractured Trial Sigil. Return to Jora Sel in Tochee on Tatooine.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void SaberCycloneCircle()
        {
            _builder.Create(SaberCycloneCircleQuestId, "Four Turns, One Center")
                .PrerequisiteQuest(SaberCycloneBreachQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberCycloneCouncilTrialChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberCycloneCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "Disable four Cyclone Examiner Droids guarding the control systems of the Jedi Trial Halls on Hutlar and recover the Saber Cyclone Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Hutlar_CycloneExaminerDroid, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Saber Cyclone Council Trial Chit. Return to Jora Sel in Tochee on Tatooine.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void SaberCycloneMastery()
        {
            _builder.Create(SaberCycloneMasteryQuestId, "The Eye of the Cyclone")
                .PrerequisiteQuest(SaberCycloneCircleQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, disable the Vortex Training Engine in the Saber Trial Chamber at the north end of the Jedi Trial Halls on Hutlar.")
                .AddKillObjective(NPCGroupType.Hutlar_VortexTrainingEngine, 1)

                .AddState()
                .SetStateJournalText(
                    "The Vortex Training Engine is disabled. Return to Jora Sel in Tochee on Tatooine.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.SaberCyclone);
                });
        }

        private void InfiniteConduitFoundation()
        {
            _builder.Create(InfiniteConduitFoundationQuestId, "Clearing the Channel")
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneMonCalaMedicalSublevelKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveMonCalaMedicalSublevelAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInfiniteConduitTriageWardLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInfiniteConduitTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "Tessa Quell gave you the Mon Cala Medical Sublevel Key. On Mon Cala, travel east from Dac City through the Coral Isles and enter the Coral Isles Facility on the south-west side of the outer isles, held by eco-terrorists. Inside, go through the sliding door off the entry hall; the sealed door into the Medical Sublevel is in the south wall of the office block just past it. Defeat six Infinite Conduit adepts there and recover the Infinite Conduit Triage Ward Ledger.")
                .AddKillObjective(NPCGroupType.MonCala_InfiniteConduit_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Infinite Conduit Triage Ward Ledger. Bring it to Tessa Quell at the Anchorhead - Medical Center on Tatooine.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void InfiniteConduitMeasure()
        {
            _builder.Create(InfiniteConduitMeasureQuestId, "The Throttled Line")
                .PrerequisiteQuest(InfiniteConduitFoundationQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInfiniteConduitKoltoConduitCoupler)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInfiniteConduitKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "Defeat five Infinite Conduit specialists deeper in the Medical Sublevel on Mon Cala and recover the Infinite Conduit Kolto Conduit Coupler.")
                .AddKillObjective(NPCGroupType.MonCala_InfiniteConduit_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit Kolto Conduit Coupler is recovered. Return it to Tessa Quell at the Anchorhead - Medical Center on Tatooine.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void InfiniteConduitBreach()
        {
            _builder.Create(InfiniteConduitBreachQuestId, "Breaking the Dam")
                .PrerequisiteQuest(InfiniteConduitMeasureQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInfiniteConduitFracturedWardSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInfiniteConduitFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat the Infinite Conduit warden by the bloodstained altar in the north of the Medical Sublevel on Mon Cala and recover the Infinite Conduit Fractured Ward Sigil.")
                .AddKillObjective(NPCGroupType.MonCala_InfiniteConduit_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "You defeated the warden and recovered the Infinite Conduit Fractured Ward Sigil. Return to Tessa Quell at the Anchorhead - Medical Center on Tatooine.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void InfiniteConduitCircle()
        {
            _builder.Create(InfiniteConduitCircleQuestId, "Where the Waters Meet")
                .PrerequisiteQuest(InfiniteConduitBreachQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInfiniteConduitMatronsWardToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInfiniteConduitMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "Defeat the four members of the Infinite Conduit inner circle in the Medical Sublevel on Mon Cala and recover the Infinite Conduit Matron's Ward Token.")
                .AddKillObjective(NPCGroupType.MonCala_InfiniteConduit_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit Matron's Ward Token is recovered. Bring it to Tessa Quell at the Anchorhead - Medical Center on Tatooine.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void InfiniteConduitMastery()
        {
            _builder.Create(InfiniteConduitMasteryQuestId, "The River Without End")
                .PrerequisiteQuest(InfiniteConduitCircleQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)

                .AddState()
                .SetStateJournalText(
                    "With at least two allies, defeat the Infinite Conduit master in the Protected Ward, through the door in the north-east corner of the Medical Sublevel on Mon Cala. His defeat is all the proof Tessa Quell needs.")
                .AddKillObjective(NPCGroupType.MonCala_InfiniteConduit_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit master is defeated. Return to Tessa Quell at the Anchorhead - Medical Center on Tatooine.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.InfiniteConduit);
                });
        }

        private static void RemoveHutlarJediTrialHallsAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                LightsaberCapstoneQuestDefinition.SaberStormFoundationQuestId,
                LightsaberCapstoneQuestDefinition.SaberStormMeasureQuestId,
                LightsaberCapstoneQuestDefinition.SaberStormBreachQuestId,
                LightsaberCapstoneQuestDefinition.SaberStormCircleQuestId,
                LightsaberCapstoneQuestDefinition.SaberStormMasteryQuestId,
                LightsaberCapstoneQuestDefinition.GuardianMasterFoundationQuestId,
                LightsaberCapstoneQuestDefinition.GuardianMasterMeasureQuestId,
                LightsaberCapstoneQuestDefinition.GuardianMasterBreachQuestId,
                LightsaberCapstoneQuestDefinition.GuardianMasterCircleQuestId,
                LightsaberCapstoneQuestDefinition.GuardianMasterMasteryQuestId,
                SaberstaffCapstoneQuestDefinition.SaberCycloneFoundationQuestId,
                SaberstaffCapstoneQuestDefinition.SaberCycloneMeasureQuestId,
                SaberstaffCapstoneQuestDefinition.SaberCycloneBreachQuestId,
                SaberstaffCapstoneQuestDefinition.SaberCycloneCircleQuestId,
                SaberstaffCapstoneQuestDefinition.SaberCycloneMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneHutlarJediTrialHallsKey, questIds);
        }

        private static void RemoveMonCalaMedicalSublevelAccessIfNoLongerNeeded(uint player)
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

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneMonCalaMedicalSublevelKey, questIds);
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
