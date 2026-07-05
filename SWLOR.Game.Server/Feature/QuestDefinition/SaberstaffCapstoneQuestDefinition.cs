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
            _builder.Create(SaberCycloneFoundationQuestId, "First Principle: Saber Cyclone")
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
                    "The Saber Cyclone capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat Saber Cyclone adepts and secure the Saber Cyclone Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Saber Cyclone Enclave Trial Slate from Dantooine Jedi Enclave Trial Halls. Return to Jora Sel for the next Saber Cyclone lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void SaberCycloneMeasure()
        {
            _builder.Create(SaberCycloneMeasureQuestId, "The Measure of Saber Cyclone")
                .PrerequisiteQuest(SaberCycloneFoundationQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberCycloneKyberFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberCycloneKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Saber Cyclone capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat Saber Cyclone specialists and secure the Saber Cyclone Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Saber Cyclone Kyber Focus Shard from Dantooine Jedi Enclave Trial Halls. Return to Jora Sel for the next Saber Cyclone lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void SaberCycloneBreach()
        {
            _builder.Create(SaberCycloneBreachQuestId, "Fault Line: Saber Cyclone")
                .PrerequisiteQuest(SaberCycloneMeasureQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberCycloneFracturedTrialSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberCycloneFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "The Saber Cyclone capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat the Saber Cyclone warden and secure the Saber Cyclone Fractured Trial Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Saber Cyclone Fractured Trial Sigil from Dantooine Jedi Enclave Trial Halls. Return to Jora Sel for the next Saber Cyclone lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void SaberCycloneCircle()
        {
            _builder.Create(SaberCycloneCircleQuestId, "Circle of Proof: Saber Cyclone")
                .PrerequisiteQuest(SaberCycloneBreachQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberCycloneCouncilTrialChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberCycloneCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "The Saber Cyclone capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat the Saber Cyclone inner circle and secure the Saber Cyclone Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberCycloneCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Saber Cyclone Council Trial Chit from Dantooine Jedi Enclave Trial Halls. Return to Jora Sel for the next Saber Cyclone lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void SaberCycloneMastery()
        {
            _builder.Create(SaberCycloneMasteryQuestId, "Saber Cyclone Mastery")
                .PrerequisiteQuest(SaberCycloneCircleQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Saber Cyclone master is waiting in Dantooine Jedi Enclave Trial Halls. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberCyclone_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Saber Cyclone master is defeated. Return to Jora Sel and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.SaberCyclone);
                });
        }

        private void InfiniteConduitFoundation()
        {
            _builder.Create(InfiniteConduitFoundationQuestId, "First Principle: Infinite Conduit")
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
                    "The Infinite Conduit capstone line continues in Dantooine Medical Sublevel. Defeat Infinite Conduit adepts and secure the Infinite Conduit Triage Ward Ledger.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitTriageWardLedger)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Infinite Conduit Triage Ward Ledger from Dantooine Medical Sublevel. Return to Tessa Quell for the next Infinite Conduit lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void InfiniteConduitMeasure()
        {
            _builder.Create(InfiniteConduitMeasureQuestId, "The Measure of Infinite Conduit")
                .PrerequisiteQuest(InfiniteConduitFoundationQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInfiniteConduitKoltoConduitCoupler)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInfiniteConduitKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit capstone line continues in Dantooine Medical Sublevel. Defeat Infinite Conduit specialists and secure the Infinite Conduit Kolto Conduit Coupler.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitKoltoConduitCoupler)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Infinite Conduit Kolto Conduit Coupler from Dantooine Medical Sublevel. Return to Tessa Quell for the next Infinite Conduit lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void InfiniteConduitBreach()
        {
            _builder.Create(InfiniteConduitBreachQuestId, "Fault Line: Infinite Conduit")
                .PrerequisiteQuest(InfiniteConduitMeasureQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInfiniteConduitFracturedWardSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInfiniteConduitFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit capstone line continues in Dantooine Medical Sublevel. Defeat the Infinite Conduit warden and secure the Infinite Conduit Fractured Ward Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitFracturedWardSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Infinite Conduit Fractured Ward Sigil from Dantooine Medical Sublevel. Return to Tessa Quell for the next Infinite Conduit lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void InfiniteConduitCircle()
        {
            _builder.Create(InfiniteConduitCircleQuestId, "Circle of Proof: Infinite Conduit")
                .PrerequisiteQuest(InfiniteConduitBreachQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneInfiniteConduitMatronsWardToken)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneInfiniteConduitMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    "The Infinite Conduit capstone line continues in Dantooine Medical Sublevel. Defeat the Infinite Conduit inner circle and secure the Infinite Conduit Matron's Ward Token.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneInfiniteConduitMatronsWardToken)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Infinite Conduit Matron's Ward Token from Dantooine Medical Sublevel. Return to Tessa Quell for the next Infinite Conduit lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void InfiniteConduitMastery()
        {
            _builder.Create(InfiniteConduitMasteryQuestId, "Infinite Conduit Mastery")
                .PrerequisiteQuest(InfiniteConduitCircleQuestId)
                .PrerequisiteSkill(SkillType.Saberstaff, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Infinite Conduit master is waiting in Dantooine Medical Sublevel. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dantooine_InfiniteConduit_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Infinite Conduit master is defeated. Return to Tessa Quell and claim the completed lesson.")
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
