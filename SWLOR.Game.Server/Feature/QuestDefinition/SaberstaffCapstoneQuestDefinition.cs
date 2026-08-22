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
                    "Defeat six Saber Cyclone adepts in the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Saber Cyclone Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "You secured the Saber Cyclone Enclave Trial Slate. Return it to Jora Sel at the colony interior on Dantooine.")
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
                    "Defeat five Saber Cyclone specialists in the deep chambers of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Saber Cyclone Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Saber Cyclone Kyber Focus Shard is in hand. Bring it back to Jora Sel.")
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
                    "Defeat the Saber Cyclone warden in the trial ring of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Saber Cyclone Fractured Trial Sigil. The warden is too strong to face alone; bring at least two allies.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "The warden is defeated and the Saber Cyclone Fractured Trial Sigil secured. Return to Jora Sel at the colony interior on Dantooine.")
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
                    "Defeat the four members of the Saber Cyclone inner circle in the council chamber of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Saber Cyclone Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "The Saber Cyclone Council Trial Chit is recovered. Return it to Jora Sel at the colony interior on Dantooine.")
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
                    "Defeat the Saber Cyclone master in the deepest hall of the Dantooine Jedi Enclave Trial Halls on Dantooine. No proof needs to be carried back; the master's defeat is the proof. She is too strong to face alone; bring at least two allies.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Saber Cyclone master is defeated. Return to Jora Sel at the colony interior on Dantooine.")
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
                    "Defeat six Infinite Conduit adepts in the Dantooine Medical Sublevel on Dantooine and secure the Infinite Conduit Triage Ward Ledger.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Infinite Conduit Triage Ward Ledger. Bring it to Tessa Quell at the Dantooine medical center.")
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
                    "Defeat five Infinite Conduit specialists in the pump galleries of the Dantooine Medical Sublevel on Dantooine and secure the Infinite Conduit Kolto Conduit Coupler.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit Kolto Conduit Coupler is recovered. Return it to Tessa Quell at the Dantooine medical center.")
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
                    "Defeat the Infinite Conduit warden in the flooded ward of the Dantooine Medical Sublevel on Dantooine and secure the Infinite Conduit Fractured Ward Sigil. The warden is too strong to face alone; bring at least two allies.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "The warden is defeated and the Infinite Conduit Fractured Ward Sigil secured. Return to Tessa Quell at the Dantooine medical center.")
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
                    "Defeat the four members of the Infinite Conduit inner circle at the matron's station in the Dantooine Medical Sublevel on Dantooine and secure the Infinite Conduit Matron's Ward Token.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit Matron's Ward Token is recovered. Bring it to Tessa Quell at the Dantooine medical center.")
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
                    "Defeat the Infinite Conduit master in the reservoir chamber at the bottom of the Dantooine Medical Sublevel on Dantooine. No proof needs to be carried back; the master's defeat is the proof. He is too strong to face alone; bring at least two allies.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit master is defeated. Return to Tessa Quell at the Dantooine medical center.")
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
