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
    public class VibroknifeCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string VitalRuptureFoundationQuestId = "vital_rupture_foundation";
        internal const string VitalRuptureMeasureQuestId = "vital_rupture_measure";
        internal const string VitalRuptureBreachQuestId = "vital_rupture_breach";
        internal const string VitalRuptureCircleQuestId = "vital_rupture_circle";
        internal const string VitalRuptureMasteryQuestId = "vital_rupture_mastery";
        internal const string VitalRuptureAdeptResref = "cp_vitrupt_ad";
        internal const string VitalRuptureSpecialistResref = "cp_vitrupt_sp";
        internal const string VitalRuptureInnerCircleResref = "cp_vitrupt_ic";
        internal const string SystemicShutdownFoundationQuestId = "systemic_shutdown_foundation";
        internal const string SystemicShutdownMeasureQuestId = "systemic_shutdown_measure";
        internal const string SystemicShutdownBreachQuestId = "systemic_shutdown_breach";
        internal const string SystemicShutdownCircleQuestId = "systemic_shutdown_circle";
        internal const string SystemicShutdownMasteryQuestId = "systemic_shutdown_mastery";
        internal const string SystemicShutdownAdeptResref = "cp_sysshut_ad";
        internal const string SystemicShutdownSpecialistResref = "cp_sysshut_sp";
        internal const string SystemicShutdownInnerCircleResref = "cp_sysshut_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            VitalRuptureFoundation();
            VitalRuptureMeasure();
            VitalRuptureBreach();
            VitalRuptureCircle();
            VitalRuptureMastery();
            SystemicShutdownFoundation();
            SystemicShutdownMeasure();
            SystemicShutdownBreach();
            SystemicShutdownCircle();
            SystemicShutdownMastery();

            return _builder.Build();
        }

        private void VitalRuptureFoundation()
        {
            _builder.Create(VitalRuptureFoundationQuestId, "Shallow Incisions")
                .PrerequisiteSkill(SkillType.Vibroknife, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneVelesMilitiaAnnexKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveVelesMilitiaAnnexAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneVitalRuptureVelesDrillLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneVitalRuptureVelesDrillLedger)

                .AddState()
                .SetStateJournalText(
                    "Begin Mikka Varn's trials to unlock Viral Cascade. Mikka Varn gave you the Veles Militia Annex Key. Enter the Veles Militia Annex on Viscara through the sealed door inside the Sheriff's Office on the west side of Veles. Defeat six Holdout Recruits, the Annex Holdouts who hold the rooms nearest the entrance, and secure the Viral Cascade Veles Drill Ledger. Any Holdout Recruit counts, but a kill only counts while your venom festers in them.")
                .AddCapstoneProofKillObjective(NPCGroupType.Viscara_VitalRupture_Adept, "viral_cascade", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneVitalRuptureVelesDrillLedger)

                .AddState()
                .SetStateJournalText(
                    "The Viral Cascade Veles Drill Ledger is in hand. Return to Mikka Varn in Racin' Jims in Veles on Viscara.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void VitalRuptureMeasure()
        {
            _builder.Create(VitalRuptureMeasureQuestId, "Arterial Mapping")
                .PrerequisiteQuest(VitalRuptureFoundationQuestId)
                .PrerequisiteSkill(SkillType.Vibroknife, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneVitalRuptureMilitiaRangeRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneVitalRuptureMilitiaRangeRelay)

                .AddState()
                .SetStateJournalText(
                    "Defeat five Holdout Sergeants, the Annex Holdouts who hold the deeper rooms of the Veles Militia Annex on Viscara, and secure the Viral Cascade Militia Range Relay. Any Holdout Sergeant counts, but a kill only counts while your venom festers in them.")
                .AddCapstoneProofKillObjective(NPCGroupType.Viscara_VitalRupture_Specialist, "viral_cascade", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneVitalRuptureMilitiaRangeRelay)

                .AddState()
                .SetStateJournalText(
                    "The Viral Cascade Militia Range Relay is in hand. Return to Mikka Varn in Racin' Jims in Veles on Viscara.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void VitalRuptureBreach()
        {
            _builder.Create(VitalRuptureBreachQuestId, "Cardiac Access")
                .PrerequisiteQuest(VitalRuptureMeasureQuestId)
                .PrerequisiteSkill(SkillType.Vibroknife, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneVitalRuptureScoredChallengeBadge)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneVitalRuptureScoredChallengeBadge)

                .AddState()
                .SetStateJournalText(
                    "Defeat Holdout Lieutenant Korvan Tull in the Veles Militia Annex on Viscara and secure the Viral Cascade Scored Challenge Badge.")
                .AddKillObjective(NPCGroupType.Viscara_VitalRupture_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneVitalRuptureScoredChallengeBadge)

                .AddState()
                .SetStateJournalText(
                    "Holdout Lieutenant Korvan Tull is defeated and the Viral Cascade Scored Challenge Badge is in hand. Return to Mikka Varn in Racin' Jims in Veles on Viscara.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void VitalRuptureCircle()
        {
            _builder.Create(VitalRuptureCircleQuestId, "Clean Margins")
                .PrerequisiteQuest(VitalRuptureBreachQuestId)
                .PrerequisiteSkill(SkillType.Vibroknife, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneVitalRuptureCaptainsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneVitalRuptureCaptainsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "Defeat four Holdout Honor Guards, the Annex Holdouts who guard the deepest part of the Veles Militia Annex on Viscara, and secure the Viral Cascade Captain's Challenge Chit. Any Holdout Honor Guard counts, but a kill only counts while your venom festers in them.")
                .AddCapstoneProofKillObjective(NPCGroupType.Viscara_VitalRupture_InnerCircle, "viral_cascade", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneVitalRuptureCaptainsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Viral Cascade Captain's Challenge Chit is in hand. Return to Mikka Varn in Racin' Jims in Veles on Viscara.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void VitalRuptureMastery()
        {
            _builder.Create(VitalRuptureMasteryQuestId, "Terminal Bleed")
                .PrerequisiteQuest(VitalRuptureCircleQuestId)
                .PrerequisiteSkill(SkillType.Vibroknife, 50)

                .AddState()
                .SetStateJournalText(
                    "Defeat Holdout Captain Marek Dolan in the Veles Militia Annex on Viscara.")
                .AddKillObjective(NPCGroupType.Viscara_VitalRupture_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "Holdout Captain Marek Dolan is defeated. Return to Mikka Varn in Racin' Jims in Veles on Viscara.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.VitalRupture);
                });
        }

        private void SystemicShutdownFoundation()
        {
            _builder.Create(SystemicShutdownFoundationQuestId, "Nothing on Record")
                .PrerequisiteSkill(SkillType.Vibroknife, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneVelesMilitiaAnnexKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveVelesMilitiaAnnexAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSystemicShutdownVelesDrillLedger)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSystemicShutdownVelesDrillLedger)

                .AddState()
                .SetStateJournalText(
                    "Dalen Orso gave you the Veles Militia Annex Key. Enter the Veles Militia Annex on Viscara through the sealed door inside the Sheriff's Office on the west side of Veles. Bring down six Holdout Recruits, the Annex Holdouts who hold the rooms nearest the entrance, and recover the Systemic Shutdown Veles Drill Ledger. Any Holdout Recruit counts, but a kill only counts after exposing them with Crippling Slice or dropping them with Backstab.")
                .AddCapstoneProofKillObjective(NPCGroupType.Viscara_SystemicShutdown_Adept, "systemic_shutdown", 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSystemicShutdownVelesDrillLedger)

                .AddState()
                .SetStateJournalText(
                    "The Systemic Shutdown Veles Drill Ledger has been recovered. Deliver it to Dalen Orso in the Czerka Tower in Veles on Viscara.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void SystemicShutdownMeasure()
        {
            _builder.Create(SystemicShutdownMeasureQuestId, "Lines of Inquiry")
                .PrerequisiteQuest(SystemicShutdownFoundationQuestId)
                .PrerequisiteSkill(SkillType.Vibroknife, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSystemicShutdownMilitiaRangeRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSystemicShutdownMilitiaRangeRelay)

                .AddState()
                .SetStateJournalText(
                    "Bring down five Holdout Sergeants, the Annex Holdouts who hold the deeper rooms of the Veles Militia Annex on Viscara, and recover the Systemic Shutdown Militia Range Relay. Any Holdout Sergeant counts, but a kill only counts after exposing them with Crippling Slice or dropping them with Backstab.")
                .AddCapstoneProofKillObjective(NPCGroupType.Viscara_SystemicShutdown_Specialist, "systemic_shutdown", 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSystemicShutdownMilitiaRangeRelay)

                .AddState()
                .SetStateJournalText(
                    "The Systemic Shutdown Militia Range Relay has been recovered. Deliver it to Dalen Orso in the Czerka Tower in Veles on Viscara.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void SystemicShutdownBreach()
        {
            _builder.Create(SystemicShutdownBreachQuestId, "Point of Failure")
                .PrerequisiteQuest(SystemicShutdownMeasureQuestId)
                .PrerequisiteSkill(SkillType.Vibroknife, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSystemicShutdownScoredChallengeBadge)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSystemicShutdownScoredChallengeBadge)

                .AddState()
                .SetStateJournalText(
                    "Bring down Holdout Lieutenant Vessik Rane in the Veles Militia Annex on Viscara and recover the Systemic Shutdown Scored Challenge Badge.")
                .AddKillObjective(NPCGroupType.Viscara_SystemicShutdown_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSystemicShutdownScoredChallengeBadge)

                .AddState()
                .SetStateJournalText(
                    "Holdout Lieutenant Vessik Rane is down and the Systemic Shutdown Scored Challenge Badge has been recovered. Deliver it to Dalen Orso in the Czerka Tower in Veles on Viscara.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void SystemicShutdownCircle()
        {
            _builder.Create(SystemicShutdownCircleQuestId, "Housecleaning")
                .PrerequisiteQuest(SystemicShutdownBreachQuestId)
                .PrerequisiteSkill(SkillType.Vibroknife, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneSystemicShutdownCaptainsChallengeChit)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneSystemicShutdownCaptainsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "Bring down four Holdout Honor Guards, the Annex Holdouts who guard the deepest part of the Veles Militia Annex on Viscara, and recover the Systemic Shutdown Captain's Challenge Chit. Any Holdout Honor Guard counts, but a kill only counts after exposing them with Crippling Slice or dropping them with Backstab.")
                .AddCapstoneProofKillObjective(NPCGroupType.Viscara_SystemicShutdown_InnerCircle, "systemic_shutdown", 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSystemicShutdownCaptainsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Systemic Shutdown Captain's Challenge Chit has been recovered. Deliver it to Dalen Orso in the Czerka Tower in Veles on Viscara.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void SystemicShutdownMastery()
        {
            _builder.Create(SystemicShutdownMasteryQuestId, "End of Transmission")
                .PrerequisiteQuest(SystemicShutdownCircleQuestId)
                .PrerequisiteSkill(SkillType.Vibroknife, 50)

                .AddState()
                .SetStateJournalText(
                    "Bring down Holdout Captain Jorrik Vael in the Veles Militia Annex on Viscara.")
                .AddKillObjective(NPCGroupType.Viscara_SystemicShutdown_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "Holdout Captain Jorrik Vael is down. Report back to Dalen Orso in the Czerka Tower in Veles on Viscara.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.SystemicShutdown);
                });
        }

        private static void RemoveVelesMilitiaAnnexAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                VibrobladeCapstoneQuestDefinition.InvincibleFoundationQuestId,
                VibrobladeCapstoneQuestDefinition.InvincibleMeasureQuestId,
                VibrobladeCapstoneQuestDefinition.InvincibleBreachQuestId,
                VibrobladeCapstoneQuestDefinition.InvincibleCircleQuestId,
                VibrobladeCapstoneQuestDefinition.InvincibleMasteryQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureFoundationQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureMeasureQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureBreachQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureCircleQuestId,
                VibroknifeCapstoneQuestDefinition.VitalRuptureMasteryQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownFoundationQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownMeasureQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownBreachQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownCircleQuestId,
                VibroknifeCapstoneQuestDefinition.SystemicShutdownMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneVelesMilitiaAnnexKey, questIds);
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
