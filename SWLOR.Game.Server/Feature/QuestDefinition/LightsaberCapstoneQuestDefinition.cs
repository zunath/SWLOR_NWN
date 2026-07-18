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
                    "Defeat 6 Saber Storm adepts in the outer ring of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Saber Storm Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "You secured the Saber Storm Enclave Trial Slate. Return to Talan Rees at the Dantooine Jedi Enclave.")
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
                    "Defeat 5 Saber Storm specialists in the middle ring of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Saber Storm Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "You secured the Saber Storm Kyber Focus Shard. Return to Talan Rees at the Dantooine Jedi Enclave.")
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
                    "Defeat the Saber Storm warden beyond the third door of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Saber Storm Fractured Trial Sigil. The warden is a match for several blades; bring two companions.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "You secured the Saber Storm Fractured Trial Sigil from the warden. Return to Talan Rees at the Dantooine Jedi Enclave.")
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
                    "Defeat the 4 members of the Saber Storm inner circle in the high ring of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Saber Storm Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "You secured the Saber Storm Council Trial Chit. Return to Talan Rees at the Dantooine Jedi Enclave.")
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
                    "Defeat the Saber Storm master behind the last door of the Dantooine Jedi Enclave Trial Halls on Dantooine. The master is beyond any lone blade; bring two companions. His defeat is the only proof required.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Saber Storm master is defeated. Return to Talan Rees at the Dantooine Jedi Enclave.")
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
                    "Defeat 6 Guardian Master adepts in the outer ward of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Guardian Master Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "You secured the Guardian Master Enclave Trial Slate. Return to Miris Aven at the Dantooine Jedi Library.")
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
                    "Defeat 5 Guardian Master specialists in the middle ward of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Guardian Master Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "You secured the Guardian Master Kyber Focus Shard. Return to Miris Aven at the Dantooine Jedi Library.")
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
                    "Defeat the Guardian Master warden in the third ward of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Guardian Master Fractured Trial Sigil. The warden is a trial for several blades; bring two companions.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "You secured the Guardian Master Fractured Trial Sigil from the warden. Return to Miris Aven at the Dantooine Jedi Library.")
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
                    "Defeat the 4 ward-captains of the Guardian Master inner circle in the deep ward of the Dantooine Jedi Enclave Trial Halls on Dantooine and secure the Guardian Master Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "You secured the Guardian Master Council Trial Chit. Return to Miris Aven at the Dantooine Jedi Library.")
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
                    "Defeat the Guardian Master Paragon behind the last seal of the Dantooine Jedi Enclave Trial Halls on Dantooine. The Paragon is beyond any lone blade; bring two companions. The Paragon's defeat is the only proof required.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Paragon, 1)

                .AddState()
                .SetStateJournalText(
                    "The Guardian Master Paragon is defeated. Return to Miris Aven at the Dantooine Jedi Library.")
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
