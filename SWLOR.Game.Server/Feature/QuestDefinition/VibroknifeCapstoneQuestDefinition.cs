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
                    "Defeat six Vital Rupture adepts in the Veles Militia Annex on Viscara and secure the Vital Rupture Veles Drill Ledger.")
                .AddKillObjective(NPCGroupType.Viscara_VitalRupture_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneVitalRuptureVelesDrillLedger)

                .AddState()
                .SetStateJournalText(
                    "The Vital Rupture Veles Drill Ledger is in hand. Return to Mikka Varn at Racin' Jims in Veles on Viscara.")
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
                    "Defeat five Vital Rupture specialists in the Veles Militia Annex on Viscara and secure the Vital Rupture Militia Range Relay.")
                .AddKillObjective(NPCGroupType.Viscara_VitalRupture_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneVitalRuptureMilitiaRangeRelay)

                .AddState()
                .SetStateJournalText(
                    "The Vital Rupture Militia Range Relay is in hand. Return to Mikka Varn at Racin' Jims in Veles on Viscara.")
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
                    "Defeat the Vital Rupture warden in the Veles Militia Annex on Viscara and secure the Vital Rupture Scored Challenge Badge.")
                .AddKillObjective(NPCGroupType.Viscara_VitalRupture_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneVitalRuptureScoredChallengeBadge)

                .AddState()
                .SetStateJournalText(
                    "The Vital Rupture warden is defeated and the Vital Rupture Scored Challenge Badge is in hand. Return to Mikka Varn at Racin' Jims in Veles on Viscara.")
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
                    "Defeat four members of the Vital Rupture inner circle in the Veles Militia Annex on Viscara and secure the Vital Rupture Captain's Challenge Chit.")
                .AddKillObjective(NPCGroupType.Viscara_VitalRupture_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneVitalRuptureCaptainsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Vital Rupture Captain's Challenge Chit is in hand. Return to Mikka Varn at Racin' Jims in Veles on Viscara.")
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
                    "Defeat the Vital Rupture master in the Veles Militia Annex on Viscara.")
                .AddKillObjective(NPCGroupType.Viscara_VitalRupture_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Vital Rupture master is defeated. Return to Mikka Varn at Racin' Jims in Veles on Viscara.")
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
                    "Bring down six Systemic Shutdown adepts in the Veles Militia Annex on Viscara and recover the Systemic Shutdown Veles Drill Ledger.")
                .AddKillObjective(NPCGroupType.Viscara_SystemicShutdown_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSystemicShutdownVelesDrillLedger)

                .AddState()
                .SetStateJournalText(
                    "The Systemic Shutdown Veles Drill Ledger has been recovered. Deliver it to Dalen Orso at the Czerka tower in Veles on Viscara.")
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
                    "Bring down five Systemic Shutdown specialists in the Veles Militia Annex on Viscara and recover the Systemic Shutdown Militia Range Relay.")
                .AddKillObjective(NPCGroupType.Viscara_SystemicShutdown_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSystemicShutdownMilitiaRangeRelay)

                .AddState()
                .SetStateJournalText(
                    "The Systemic Shutdown Militia Range Relay has been recovered. Deliver it to Dalen Orso at the Czerka tower in Veles on Viscara.")
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
                    "Bring down the Systemic Shutdown warden in the Veles Militia Annex on Viscara and recover the Systemic Shutdown Scored Challenge Badge.")
                .AddKillObjective(NPCGroupType.Viscara_SystemicShutdown_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSystemicShutdownScoredChallengeBadge)

                .AddState()
                .SetStateJournalText(
                    "The Systemic Shutdown warden is down and the Systemic Shutdown Scored Challenge Badge has been recovered. Deliver it to Dalen Orso at the Czerka tower in Veles on Viscara.")
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
                    "Bring down four members of the Systemic Shutdown inner circle in the Veles Militia Annex on Viscara and recover the Systemic Shutdown Captain's Challenge Chit.")
                .AddKillObjective(NPCGroupType.Viscara_SystemicShutdown_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneSystemicShutdownCaptainsChallengeChit)

                .AddState()
                .SetStateJournalText(
                    "The Systemic Shutdown Captain's Challenge Chit has been recovered. Deliver it to Dalen Orso at the Czerka tower in Veles on Viscara.")
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
                    "Bring down the Systemic Shutdown master in the Veles Militia Annex on Viscara.")
                .AddKillObjective(NPCGroupType.Viscara_SystemicShutdown_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Systemic Shutdown master is down. Report back to Dalen Orso.")
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
