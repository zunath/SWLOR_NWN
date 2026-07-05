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
            _builder.Create(SaberStormFoundationQuestId, "First Principle: Saber Storm")
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
                    "The Saber Storm capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat Saber Storm adepts and secure the Saber Storm Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Saber Storm Enclave Trial Slate from Dantooine Jedi Enclave Trial Halls. Return to Talan Rees for the next Saber Storm lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void SaberStormMeasure()
        {
            _builder.Create(SaberStormMeasureQuestId, "The Measure of Saber Storm")
                .PrerequisiteQuest(SaberStormFoundationQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberStormKyberFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberStormKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Saber Storm capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat Saber Storm specialists and secure the Saber Storm Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Saber Storm Kyber Focus Shard from Dantooine Jedi Enclave Trial Halls. Return to Talan Rees for the next Saber Storm lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void SaberStormBreach()
        {
            _builder.Create(SaberStormBreachQuestId, "Fault Line: Saber Storm")
                .PrerequisiteQuest(SaberStormMeasureQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberStormFracturedTrialSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberStormFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "The Saber Storm capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat the Saber Storm warden and secure the Saber Storm Fractured Trial Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Saber Storm Fractured Trial Sigil from Dantooine Jedi Enclave Trial Halls. Return to Talan Rees for the next Saber Storm lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void SaberStormCircle()
        {
            _builder.Create(SaberStormCircleQuestId, "Circle of Proof: Saber Storm")
                .PrerequisiteQuest(SaberStormBreachQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSaberStormCouncilTrialChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSaberStormCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "The Saber Storm capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat the Saber Storm inner circle and secure the Saber Storm Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Saber Storm Council Trial Chit from Dantooine Jedi Enclave Trial Halls. Return to Talan Rees for the next Saber Storm lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void SaberStormMastery()
        {
            _builder.Create(SaberStormMasteryQuestId, "Saber Storm Mastery")
                .PrerequisiteQuest(SaberStormCircleQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Saber Storm master is waiting in Dantooine Jedi Enclave Trial Halls. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dantooine_SaberStorm_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Saber Storm master is defeated. Return to Talan Rees and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.SaberStorm);
                });
        }

        private void GuardianMasterFoundation()
        {
            _builder.Create(GuardianMasterFoundationQuestId, "First Principle: Guardian Master")
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
                    "The Guardian Master capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat Guardian Master adepts and secure the Guardian Master Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Guardian Master Enclave Trial Slate from Dantooine Jedi Enclave Trial Halls. Return to Miris Aven for the next Guardian Master lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void GuardianMasterMeasure()
        {
            _builder.Create(GuardianMasterMeasureQuestId, "The Measure of Guardian Master")
                .PrerequisiteQuest(GuardianMasterFoundationQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneGuardianMasterKyberFocusShard)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneGuardianMasterKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "The Guardian Master capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat Guardian Master specialists and secure the Guardian Master Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Guardian Master Kyber Focus Shard from Dantooine Jedi Enclave Trial Halls. Return to Miris Aven for the next Guardian Master lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void GuardianMasterBreach()
        {
            _builder.Create(GuardianMasterBreachQuestId, "Fault Line: Guardian Master")
                .PrerequisiteQuest(GuardianMasterMeasureQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneGuardianMasterFracturedTrialSigil)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneGuardianMasterFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "The Guardian Master capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat the Guardian Master warden and secure the Guardian Master Fractured Trial Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Guardian Master Fractured Trial Sigil from Dantooine Jedi Enclave Trial Halls. Return to Miris Aven for the next Guardian Master lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void GuardianMasterCircle()
        {
            _builder.Create(GuardianMasterCircleQuestId, "Circle of Proof: Guardian Master")
                .PrerequisiteQuest(GuardianMasterBreachQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneGuardianMasterCouncilTrialChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneGuardianMasterCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "The Guardian Master capstone line continues in Dantooine Jedi Enclave Trial Halls. Defeat the Guardian Master inner circle and secure the Guardian Master Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Guardian Master Council Trial Chit from Dantooine Jedi Enclave Trial Halls. Return to Miris Aven for the next Guardian Master lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void GuardianMasterMastery()
        {
            _builder.Create(GuardianMasterMasteryQuestId, "Guardian Master Mastery")
                .PrerequisiteQuest(GuardianMasterCircleQuestId)
                .PrerequisiteSkill(SkillType.Lightsaber, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Guardian Master master is waiting in Dantooine Jedi Enclave Trial Halls. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianMaster_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Guardian Master master is defeated. Return to Miris Aven and claim the completed lesson.")
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
