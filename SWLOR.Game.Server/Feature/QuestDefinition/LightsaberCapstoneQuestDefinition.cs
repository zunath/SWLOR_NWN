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
                    "Talan Rees gave you the Dantooine Jedi Enclave Trial Halls Key. Disable six Storm Drill Droids in the Dantooine Jedi Enclave Trial Halls, through the sealed passage just inside the Jedi Enclave Library on Dantooine, and recover the Saber Storm Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_StormDrillDroid, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Saber Storm Enclave Trial Slate. Return to Talan Rees at the Anchorhead - Spaceport on Tatooine.")
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
                    "Disable five Storm Duelist Droids deeper in the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Saber Storm Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_StormDuelistDroid, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Saber Storm Kyber Focus Shard. Return to Talan Rees at the Anchorhead - Spaceport on Tatooine.")
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
                    "With at least two allies, disable the Storm Gatekeeper Droid in the western halls of the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Saber Storm Fractured Trial Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_StormGatekeeperDroid, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Saber Storm Fractured Trial Sigil. Return to Talan Rees at the Anchorhead - Spaceport on Tatooine.")
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
                    "Disable four Storm Examiner Droids guarding the control systems of the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Saber Storm Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_StormExaminerDroid, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSaberStormCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Saber Storm Council Trial Chit. Return to Talan Rees at the Anchorhead - Spaceport on Tatooine.")
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
                    "With at least two allies, disable the Tempest Training Engine in the Saber Trial Chamber at the north end of the Dantooine Jedi Enclave Trial Halls on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_TempestTrainingEngine, 1)

                .AddState()
                .SetStateJournalText(
                    "The Tempest Training Engine is disabled. Return to Talan Rees at the Anchorhead - Spaceport on Tatooine.")
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
                    "Miris Aven gave you the Dantooine Jedi Enclave Trial Halls Key. Disable six Guardian Patrol Droids in the Dantooine Jedi Enclave Trial Halls, through the sealed passage just inside the Jedi Enclave Library on Dantooine, and recover the Guardian Master Enclave Trial Slate.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianPatrolDroid, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterEnclaveTrialSlate)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Guardian Master Enclave Trial Slate. Return to Miris Aven at the Anchorhead - South District on Tatooine.")
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
                    "Disable five Guardian Bulwark Droids deeper in the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Guardian Master Kyber Focus Shard.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianBulwarkDroid, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterKyberFocusShard)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Guardian Master Kyber Focus Shard. Return to Miris Aven at the Anchorhead - South District on Tatooine.")
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
                    "With at least two allies, disable the Guardian Gatekeeper Droid in the eastern halls of the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Guardian Master Fractured Trial Sigil.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianGatekeeperDroid, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterFracturedTrialSigil)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Guardian Master Fractured Trial Sigil. Return to Miris Aven at the Anchorhead - South District on Tatooine.")
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
                    "Disable four Guardian Shield Droids guarding the control systems of the Dantooine Jedi Enclave Trial Halls on Dantooine and recover the Guardian Master Council Trial Chit.")
                .AddKillObjective(NPCGroupType.Dantooine_GuardianShieldDroid, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneGuardianMasterCouncilTrialChit)

                .AddState()
                .SetStateJournalText(
                    "You recovered the Guardian Master Council Trial Chit. Return to Miris Aven at the Anchorhead - South District on Tatooine.")
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
                    "With at least two allies, disable the Bastion Training Engine in the Saber Trial Chamber at the north end of the Dantooine Jedi Enclave Trial Halls on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_BastionTrainingEngine, 1)

                .AddState()
                .SetStateJournalText(
                    "The Bastion Training Engine is disabled. Return to Miris Aven at the Anchorhead - South District on Tatooine.")
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
