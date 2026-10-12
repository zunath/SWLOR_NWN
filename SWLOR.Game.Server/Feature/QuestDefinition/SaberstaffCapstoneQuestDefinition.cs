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
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDantooineJediEnclaveTrialHallsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDantooineJediEnclaveTrialHallsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberCycloneEnclaveTrialSlate)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberCycloneEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "Jora Sel gave you the Dantooine Jedi Enclave Trial Halls Key. Disable six Cyclone Drill Droids in the Dantooine Jedi Enclave Trial Halls, through the sealed passage just inside the Jedi Enclave Library on Dantooine, and recover the Saber Cyclone Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_CycloneDrillDroid, 6)
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
                    "Disable five Cyclone Duelist Droids deeper in the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Saber Cyclone Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_CycloneDuelistDroid, 5)
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
                    "With at least two allies, disable the Cyclone Gatekeeper Droid guarding the approach to the Saber Trial Chamber in the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Saber Cyclone Fractured Trial Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_CycloneGatekeeperDroid, 1)
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
                    "Disable four Cyclone Examiner Droids guarding the control systems of the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Saber Cyclone Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_CycloneExaminerDroid, 4)
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
                    "With at least two allies, disable the Vortex Training Engine in the Saber Trial Chamber at the north end of the Dantooine Jedi Enclave Trial Halls on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_VortexTrainingEngine, 1)

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
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDantooineMedicalSublevelKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDantooineMedicalSublevelAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInfiniteConduitTriageWardLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInfiniteConduitTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "Tessa Quell gave you the Dantooine Medical Sublevel Key. Defeat six Infinite Conduit adepts in the Dantooine Medical Sublevel, behind the sealed door on the far west wall of the Abandoned Warehouse on Dantooine's Jungle Mountain, and recover the Infinite Conduit Triage Ward Ledger.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Adept, 6)
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
                    "Defeat five Infinite Conduit specialists deeper in the Dantooine Medical Sublevel on Dantooine and recover the Infinite Conduit Kolto Conduit Coupler.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Specialist, 5)
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
                    "With at least two allies, defeat the Infinite Conduit warden by the bloodstained altar in the north of the Dantooine Medical Sublevel on Dantooine and recover the Infinite Conduit Fractured Ward Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Warden, 1)
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
                    "Defeat the four members of the Infinite Conduit inner circle in the Dantooine Medical Sublevel on Dantooine and recover the Infinite Conduit Matron's Ward Token.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_InnerCircle, 4)
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
                    "With at least two allies, defeat the Infinite Conduit master in the Protected Ward, through the door in the north-east corner of the Dantooine Medical Sublevel on Dantooine. His defeat is all the proof Tessa Quell needs.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Master, 1)

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

        private static void RemoveDantooineJediEnclaveTrialHallsAccessIfNoLongerNeeded(uint player)
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

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneDantooineJediEnclaveTrialHallsKey, questIds);
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
