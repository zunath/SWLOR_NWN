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
    public class LightsaberCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string SaberStormFoundationQuestId = "saber_storm_foundation";
        internal const string SaberStormMeasureQuestId = "saber_storm_measure";
        internal const string SaberStormBreachQuestId = "saber_storm_breach";
        internal const string SaberStormCircleQuestId = "saber_storm_circle";
        internal const string SaberStormMasteryQuestId = "saber_storm_mastery";
        internal const string SaberStormAdeptResref = "cp_sabstorm_ad";
        internal const string SaberStormSpecialistResref = "cp_sabstorm_sp";
        internal const string SaberStormInnerCircleResref = "cp_sabstorm_ic";
        internal const string GuardianMasterFoundationQuestId = "guardian_master_foundation";
        internal const string GuardianMasterMeasureQuestId = "guardian_master_measure";
        internal const string GuardianMasterBreachQuestId = "guardian_master_breach";
        internal const string GuardianMasterCircleQuestId = "guardian_master_circle";
        internal const string GuardianMasterMasteryQuestId = "guardian_master_mastery";
        internal const string GuardianMasterAdeptResref = "cp_guardmst_ad";
        internal const string GuardianMasterSpecialistResref = "cp_guardmst_sp";
        internal const string GuardianMasterInnerCircleResref = "cp_guardmst_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            SaberStormFoundation();
            SaberStormMeasure();
            SaberStormBreach();
            SaberStormCircle();
            SaberStormMastery();
            GuardianMasterFoundation();
            GuardianMasterMeasure();
            GuardianMasterBreach();
            GuardianMasterCircle();
            GuardianMasterMastery();

            return _builder.Build();
        }

        private void SaberStormFoundation()
        {
            _builder.Create(SaberStormFoundationQuestId, "Footwork Before Fury")
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDantooineJediEnclaveTrialHallsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDantooineJediEnclaveTrialHallsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberStormEnclaveTrialSlate)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberStormEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "Talan Rees gave you the Dantooine Jedi Enclave Trial Halls Key. Contain the malfunctioning training network. Disable 6 Storm Drill Droid units in the outer ring of the Dantooine Jedi Enclave Trial Halls. Recover the Saber Storm Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "The Saber Storm Enclave Trial Slate is recovered. Return to Talan Rees at the Jedi Enclave on Dantooine.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void SaberStormMeasure()
        {
            _builder.Create(SaberStormMeasureQuestId, "Tempo Against the Wall")
                .PrerequisiteQuest(SaberStormFoundationQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberStormKyberFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberStormKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "Disable 5 Storm Duelist Droid units in the middle ring of the Dantooine Jedi Enclave Trial Halls. Recover the Saber Storm Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Saber Storm Kyber Focus Shard is recovered. Return to Talan Rees at the Jedi Enclave on Dantooine.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void SaberStormBreach()
        {
            _builder.Create(SaberStormBreachQuestId, "The Third Door Drill")
                .PrerequisiteQuest(SaberStormMeasureQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberStormFracturedTrialSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberStormFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "Disable 1 Storm Gatekeeper Droid in the third trial door of the Dantooine Jedi Enclave Trial Halls. Recover the Saber Storm Fractured Trial Sigil. Bring at least two allies.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "The Saber Storm Fractured Trial Sigil is recovered. Return to Talan Rees at the Jedi Enclave on Dantooine.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void SaberStormCircle()
        {
            _builder.Create(SaberStormCircleQuestId, "The High Ring Examination")
                .PrerequisiteQuest(SaberStormBreachQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberStormCouncilTrialChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberStormCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "Disable 4 Storm Examiner Droid units in the high ring of the Dantooine Jedi Enclave Trial Halls. Recover the Saber Storm Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "The Saber Storm Council Trial Chit is recovered. Return to Talan Rees at the Jedi Enclave on Dantooine.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void SaberStormMastery()
        {
            _builder.Create(SaberStormMasteryQuestId, "The Storm Without an Eye")
                .PrerequisiteQuest(SaberStormCircleQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)

                .AddState()
                .SetStateJournalText(
                    "Disable 1 Tempest Training Engine in the deepest trial hall of the Dantooine Jedi Enclave Trial Halls. Bring at least two allies.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Tempest Training Engine is defeated. Return to Talan Rees at the Jedi Enclave on Dantooine.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.SaberStorm);
                });
        }

        private void GuardianMasterFoundation()
        {
            _builder.Create(GuardianMasterFoundationQuestId, "While I Stand")
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneDantooineJediEnclaveTrialHallsKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveDantooineJediEnclaveTrialHallsAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneGuardianMasterEnclaveTrialSlate)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneGuardianMasterEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "Miris Aven gave you the Dantooine Jedi Enclave Trial Halls Key. Contain the malfunctioning training network. Disable 6 Guardian Patrol Droid units in the outer ward of the Dantooine Jedi Enclave Trial Halls. Recover the Guardian Master Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "The Guardian Master Enclave Trial Slate is recovered. Return to Miris Aven at the Jedi Library on Dantooine.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void GuardianMasterMeasure()
        {
            _builder.Create(GuardianMasterMeasureQuestId, "Not for My Own Hand")
                .PrerequisiteQuest(GuardianMasterFoundationQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneGuardianMasterKyberFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneGuardianMasterKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "Disable 5 Guardian Bulwark Droid units in the shrine chambers of the Dantooine Jedi Enclave Trial Halls. Recover the Guardian Master Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Guardian Master Kyber Focus Shard is recovered. Return to Miris Aven at the Jedi Library on Dantooine.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void GuardianMasterBreach()
        {
            _builder.Create(GuardianMasterBreachQuestId, "Between Harm and the Helpless")
                .PrerequisiteQuest(GuardianMasterMeasureQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneGuardianMasterFracturedTrialSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneGuardianMasterFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "Disable 1 Guardian Gatekeeper Droid in the third ward of the Dantooine Jedi Enclave Trial Halls. Recover the Guardian Master Fractured Trial Sigil. Bring at least two allies.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "The Guardian Master Fractured Trial Sigil is recovered. Return to Miris Aven at the Jedi Library on Dantooine.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void GuardianMasterCircle()
        {
            _builder.Create(GuardianMasterCircleQuestId, "Until the Last Is Safe")
                .PrerequisiteQuest(GuardianMasterBreachQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneGuardianMasterCouncilTrialChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneGuardianMasterCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "Disable 4 Guardian Shield Droid units in the deep ward of the Dantooine Jedi Enclave Trial Halls. Recover the Guardian Master Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "The Guardian Master Council Trial Chit is recovered. Return to Miris Aven at the Jedi Library on Dantooine.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void GuardianMasterMastery()
        {
            _builder.Create(GuardianMasterMasteryQuestId, "Every Oath but One")
                .PrerequisiteQuest(GuardianMasterCircleQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)

                .AddState()
                .SetStateJournalText(
                    "Disable 1 Bastion Training Engine in the final ward of the Dantooine Jedi Enclave Trial Halls. Bring at least two allies.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Paragon, 1)

                .AddState()
                .SetStateJournalText(
                    "The Bastion Training Engine is defeated. Return to Miris Aven at the Jedi Library on Dantooine.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.GuardianMaster);
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
