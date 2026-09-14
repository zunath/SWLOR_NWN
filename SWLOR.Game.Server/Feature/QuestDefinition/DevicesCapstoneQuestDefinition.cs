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
    public class DevicesCapstoneQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        internal const string ThermalDetonatorFoundationQuestId = "thermal_detonator_foundation";
        internal const string ThermalDetonatorMeasureQuestId = "thermal_detonator_measure";
        internal const string ThermalDetonatorBreachQuestId = "thermal_detonator_breach";
        internal const string ThermalDetonatorCircleQuestId = "thermal_detonator_circle";
        internal const string ThermalDetonatorMasteryQuestId = "thermal_detonator_mastery";
        internal const string ThermalDetonatorAdeptResref = "cp_thermdet_ad";
        internal const string ThermalDetonatorSpecialistResref = "cp_thermdet_sp";
        internal const string ThermalDetonatorInnerCircleResref = "cp_thermdet_ic";
        internal const string OverloadBarrageFoundationQuestId = "overload_barrage_foundation";
        internal const string OverloadBarrageMeasureQuestId = "overload_barrage_measure";
        internal const string OverloadBarrageBreachQuestId = "overload_barrage_breach";
        internal const string OverloadBarrageCircleQuestId = "overload_barrage_circle";
        internal const string OverloadBarrageMasteryQuestId = "overload_barrage_mastery";
        internal const string OverloadBarrageAdeptResref = "cp_overbarr_ad";
        internal const string OverloadBarrageSpecialistResref = "cp_overbarr_sp";
        internal const string OverloadBarrageInnerCircleResref = "cp_overbarr_ic";
        internal const string KillzoneBeaconFoundationQuestId = "killzone_beacon_foundation";
        internal const string KillzoneBeaconMeasureQuestId = "killzone_beacon_measure";
        internal const string KillzoneBeaconBreachQuestId = "killzone_beacon_breach";
        internal const string KillzoneBeaconCircleQuestId = "killzone_beacon_circle";
        internal const string KillzoneBeaconMasteryQuestId = "killzone_beacon_mastery";
        internal const string KillzoneBeaconAdeptResref = "cp_killbeacon_ad";
        internal const string KillzoneBeaconSpecialistResref = "cp_killbeacon_sp";
        internal const string KillzoneBeaconInnerCircleResref = "cp_killbeacon_ic";
        internal const string EmergencyBunkerFoundationQuestId = "emergency_bunker_foundation";
        internal const string EmergencyBunkerMeasureQuestId = "emergency_bunker_measure";
        internal const string EmergencyBunkerBreachQuestId = "emergency_bunker_breach";
        internal const string EmergencyBunkerCircleQuestId = "emergency_bunker_circle";
        internal const string EmergencyBunkerMasteryQuestId = "emergency_bunker_mastery";
        internal const string EmergencyBunkerAdeptResref = "cp_embunker_ad";
        internal const string EmergencyBunkerSpecialistResref = "cp_embunker_sp";
        internal const string EmergencyBunkerInnerCircleResref = "cp_embunker_ic";

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            ThermalDetonatorFoundation();
            ThermalDetonatorMeasure();
            ThermalDetonatorBreach();
            ThermalDetonatorCircle();
            ThermalDetonatorMastery();
            OverloadBarrageFoundation();
            OverloadBarrageMeasure();
            OverloadBarrageBreach();
            OverloadBarrageCircle();
            OverloadBarrageMastery();
            KillzoneBeaconFoundation();
            KillzoneBeaconMeasure();
            KillzoneBeaconBreach();
            KillzoneBeaconCircle();
            KillzoneBeaconMastery();
            EmergencyBunkerFoundation();
            EmergencyBunkerMeasure();
            EmergencyBunkerBreach();
            EmergencyBunkerCircle();
            EmergencyBunkerMastery();

            return _builder.Build();
        }

        private void ThermalDetonatorFoundation()
        {
            _builder.Create(ThermalDetonatorFoundationQuestId, "Unlogged Charge")
                .PrerequisiteSkill(SkillType.Devices, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneHutlarQionTestSiteKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveHutlarQionTestSiteAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneThermalDetonatorQionTestLog)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneThermalDetonatorQionTestLog)

                .AddState()
                .SetStateJournalText(
                    "Six Thermal Detonator adepts are stationed in Hutlar Qion Test Site on Hutlar. Defeat them and recover the Thermal Detonator Qion Test Log.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneThermalDetonatorQionTestLog)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator Qion Test Log has been recovered from Hutlar Qion Test Site. Return it to Ruk Halven at the Hutlar Outpost.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ThermalDetonatorMeasure()
        {
            _builder.Create(ThermalDetonatorMeasureQuestId, "Short Fuze Report")
                .PrerequisiteQuest(ThermalDetonatorFoundationQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneThermalDetonatorCryoRangeRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneThermalDetonatorCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "Five Thermal Detonator specialists are stationed in Hutlar Qion Test Site on Hutlar. Defeat them and recover the Thermal Detonator Cryo-Range Regulator.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneThermalDetonatorCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator Cryo-Range Regulator has been recovered from Hutlar Qion Test Site. Return it to Ruk Halven at the Hutlar Outpost.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ThermalDetonatorBreach()
        {
            _builder.Create(ThermalDetonatorBreachQuestId, "Full-Yield Breach")
                .PrerequisiteQuest(ThermalDetonatorMeasureQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneThermalDetonatorFrostburnedTestCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneThermalDetonatorFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator warden is stationed in Hutlar Qion Test Site on Hutlar. Defeat the warden and recover the Thermal Detonator Frostburned Test Crest.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneThermalDetonatorFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator Frostburned Test Crest has been recovered from Hutlar Qion Test Site. Return it to Ruk Halven at the Hutlar Outpost.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ThermalDetonatorCircle()
        {
            _builder.Create(ThermalDetonatorCircleQuestId, "Site Purge Order")
                .PrerequisiteQuest(ThermalDetonatorBreachQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneThermalDetonatorSiteChiefsOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneThermalDetonatorSiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator inner circle, four in number, holds Hutlar Qion Test Site on Hutlar. Defeat them and recover the Thermal Detonator Site Chief's Override Chip.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneThermalDetonatorSiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator Site Chief's Override Chip has been recovered from Hutlar Qion Test Site. Return it to Ruk Halven at the Hutlar Outpost.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ThermalDetonatorMastery()
        {
            _builder.Create(ThermalDetonatorMasteryQuestId, "Zero-Margin Incident")
                .PrerequisiteQuest(ThermalDetonatorCircleQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator master is stationed in Hutlar Qion Test Site on Hutlar. Defeat the master to complete the trial.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator master has been defeated. Return to Ruk Halven at the Hutlar Outpost to complete the trial.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.ThermalDetonator);
                });
        }

        private void OverloadBarrageFoundation()
        {
            _builder.Create(OverloadBarrageFoundationQuestId, "Overdraw, Unsigned")
                .PrerequisiteSkill(SkillType.Devices, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneHutlarQionTestSiteKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveHutlarQionTestSiteAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOverloadBarrageQionTestLog)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOverloadBarrageQionTestLog)

                .AddState()
                .SetStateJournalText(
                    "Six Overload Barrage adepts are stationed in Hutlar Qion Test Site on Hutlar. Defeat them and recover the Overload Barrage Qion Test Log.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOverloadBarrageQionTestLog)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage Qion Test Log has been recovered from Hutlar Qion Test Site. Return it to Miri Koss at Fort Ka'ra on Hutlar.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void OverloadBarrageMeasure()
        {
            _builder.Create(OverloadBarrageMeasureQuestId, "Cells Don't Lie")
                .PrerequisiteQuest(OverloadBarrageFoundationQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOverloadBarrageCryoRangeRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOverloadBarrageCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "Five Overload Barrage specialists are stationed in Hutlar Qion Test Site on Hutlar. Defeat them and recover the Overload Barrage Cryo-Range Regulator.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOverloadBarrageCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage Cryo-Range Regulator has been recovered from Hutlar Qion Test Site. Return it to Miri Koss at Fort Ka'ra on Hutlar.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void OverloadBarrageBreach()
        {
            _builder.Create(OverloadBarrageBreachQuestId, "Full Dump, No Slag")
                .PrerequisiteQuest(OverloadBarrageMeasureQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOverloadBarrageFrostburnedTestCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOverloadBarrageFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage warden is stationed in Hutlar Qion Test Site on Hutlar. Defeat the warden and recover the Overload Barrage Frostburned Test Crest.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOverloadBarrageFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage Frostburned Test Crest has been recovered from Hutlar Qion Test Site. Return it to Miri Koss at Fort Ka'ra on Hutlar.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void OverloadBarrageCircle()
        {
            _builder.Create(OverloadBarrageCircleQuestId, "Charge Table Recovered")
                .PrerequisiteQuest(OverloadBarrageBreachQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOverloadBarrageSiteChiefsOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOverloadBarrageSiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage inner circle, four in number, holds Hutlar Qion Test Site on Hutlar. Defeat them and recover the Overload Barrage Site Chief's Override Chip.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOverloadBarrageSiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage Site Chief's Override Chip has been recovered from Hutlar Qion Test Site. Return it to Miri Koss at Fort Ka'ra on Hutlar.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void OverloadBarrageMastery()
        {
            _builder.Create(OverloadBarrageMasteryQuestId, "One Fighter Down, Three Don't")
                .PrerequisiteQuest(OverloadBarrageCircleQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage master is stationed in Hutlar Qion Test Site on Hutlar. Defeat the master to complete the lesson.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage master has been defeated. Return to Miri Koss at Fort Ka'ra on Hutlar to complete the lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.OverloadBarrage);
                });
        }

        private void KillzoneBeaconFoundation()
        {
            _builder.Create(KillzoneBeaconFoundationQuestId, "Operation Quiet Grid")
                .PrerequisiteSkill(SkillType.Devices, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneViscaraRepublicEngineeringBunkerKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveViscaraRepublicEngineeringBunkerAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillzoneBeaconRepublicBunkerDocket)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillzoneBeaconRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    "Six Killzone Beacon adepts are stationed in Viscara Republic Engineering Bunker on Viscara. Defeat them and recover the Killzone Beacon Republic Bunker Docket.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillzoneBeaconRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon Republic Bunker Docket has been recovered from Viscara Republic Engineering Bunker. Return it to Aric Jorr at the Republic Base on Viscara.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void KillzoneBeaconMeasure()
        {
            _builder.Create(KillzoneBeaconMeasureQuestId, "Operation Dead Reckoning")
                .PrerequisiteQuest(KillzoneBeaconFoundationQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillzoneBeaconShieldGridRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillzoneBeaconShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "Five Killzone Beacon specialists are stationed in Viscara Republic Engineering Bunker on Viscara. Defeat them and recover the Killzone Beacon Shield Grid Relay.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillzoneBeaconShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon Shield Grid Relay has been recovered from Viscara Republic Engineering Bunker. Return it to Aric Jorr at the Republic Base on Viscara.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void KillzoneBeaconBreach()
        {
            _builder.Create(KillzoneBeaconBreachQuestId, "Operation Cracked Command")
                .PrerequisiteQuest(KillzoneBeaconMeasureQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillzoneBeaconCrackedCommandCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillzoneBeaconCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon warden is stationed in Viscara Republic Engineering Bunker on Viscara. Defeat the warden and recover the Killzone Beacon Cracked Command Crest.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillzoneBeaconCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon Cracked Command Crest has been recovered from Viscara Republic Engineering Bunker. Return it to Aric Jorr at the Republic Base on Viscara.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void KillzoneBeaconCircle()
        {
            _builder.Create(KillzoneBeaconCircleQuestId, "Operation Clean Ledger")
                .PrerequisiteQuest(KillzoneBeaconBreachQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillzoneBeaconQuartermasterOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillzoneBeaconQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon inner circle, four in number, holds Viscara Republic Engineering Bunker on Viscara. Defeat them and recover the Killzone Beacon Quartermaster Override Chip.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillzoneBeaconQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon Quartermaster Override Chip has been recovered from Viscara Republic Engineering Bunker. Return it to Aric Jorr at the Republic Base on Viscara.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void KillzoneBeaconMastery()
        {
            _builder.Create(KillzoneBeaconMasteryQuestId, "Operation Last Painted Target")
                .PrerequisiteQuest(KillzoneBeaconCircleQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon master is stationed in Viscara Republic Engineering Bunker on Viscara. Defeat the master to complete the briefing.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon master has been defeated. Return to Aric Jorr at the Republic Base on Viscara to complete the briefing.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.KillzoneBeacon);
                });
        }

        private void EmergencyBunkerFoundation()
        {
            _builder.Create(EmergencyBunkerFoundationQuestId, "Certify: Sublevel Access")
                .PrerequisiteSkill(SkillType.Devices, 50)
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CapstoneViscaraRepublicEngineeringBunkerKey);
                })
                .OnAbandonAction(player =>
                {
                    RemoveViscaraRepublicEngineeringBunkerAccessIfNoLongerNeeded(player);
                })
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyBunkerRepublicBunkerDocket)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyBunkerRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    "Six Emergency Bunker adepts are stationed in Viscara Republic Engineering Bunker on Viscara. Defeat them and recover the Emergency Bunker Republic Bunker Docket.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyBunkerRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker Republic Bunker Docket has been recovered from Viscara Republic Engineering Bunker. Return it to Nella Voss at the Republic Base grounds on Viscara.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void EmergencyBunkerMeasure()
        {
            _builder.Create(EmergencyBunkerMeasureQuestId, "Certify: Load-Bearing Trust")
                .PrerequisiteQuest(EmergencyBunkerFoundationQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyBunkerShieldGridRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyBunkerShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "Five Emergency Bunker specialists are stationed in Viscara Republic Engineering Bunker on Viscara. Defeat them and recover the Emergency Bunker Shield Grid Relay.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyBunkerShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker Shield Grid Relay has been recovered from Viscara Republic Engineering Bunker. Return it to Nella Voss at the Republic Base grounds on Viscara.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void EmergencyBunkerBreach()
        {
            _builder.Create(EmergencyBunkerBreachQuestId, "Certify: Breach Protocol")
                .PrerequisiteQuest(EmergencyBunkerMeasureQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyBunkerCrackedCommandCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyBunkerCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker warden is stationed in Viscara Republic Engineering Bunker on Viscara. Defeat the warden and recover the Emergency Bunker Cracked Command Crest.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyBunkerCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker Cracked Command Crest has been recovered from Viscara Republic Engineering Bunker. Return it to Nella Voss at the Republic Base grounds on Viscara.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void EmergencyBunkerCircle()
        {
            _builder.Create(EmergencyBunkerCircleQuestId, "Certify: The Chain of Custody")
                .PrerequisiteQuest(EmergencyBunkerBreachQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyBunkerQuartermasterOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyBunkerQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker inner circle, four in number, holds Viscara Republic Engineering Bunker on Viscara. Defeat them and recover the Emergency Bunker Quartermaster Override Chip.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyBunkerQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker Quartermaster Override Chip has been recovered from Viscara Republic Engineering Bunker. Return it to Nella Voss at the Republic Base grounds on Viscara.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void EmergencyBunkerMastery()
        {
            _builder.Create(EmergencyBunkerMasteryQuestId, "Certify: The Patient Wall")
                .PrerequisiteQuest(EmergencyBunkerCircleQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker master is stationed in Viscara Republic Engineering Bunker on Viscara. Defeat the master to complete the certification.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker master has been defeated. Return to Nella Voss at the Republic Base grounds on Viscara to complete the certification.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.EmergencyBunker);
                });
        }

        private static void RemoveHutlarQionTestSiteAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                DevicesCapstoneQuestDefinition.ThermalDetonatorFoundationQuestId,
                DevicesCapstoneQuestDefinition.ThermalDetonatorMeasureQuestId,
                DevicesCapstoneQuestDefinition.ThermalDetonatorBreachQuestId,
                DevicesCapstoneQuestDefinition.ThermalDetonatorCircleQuestId,
                DevicesCapstoneQuestDefinition.ThermalDetonatorMasteryQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageFoundationQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageMeasureQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageBreachQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageCircleQuestId,
                DevicesCapstoneQuestDefinition.OverloadBarrageMasteryQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryFoundationQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryMeasureQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryBreachQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryCircleQuestId,
                ThrowingCapstoneQuestDefinition.PerfectFlurryMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneHutlarQionTestSiteKey, questIds);
        }

        private static void RemoveViscaraRepublicEngineeringBunkerAccessIfNoLongerNeeded(uint player)
        {
            var questIds = new[]
            {
                DevicesCapstoneQuestDefinition.KillzoneBeaconFoundationQuestId,
                DevicesCapstoneQuestDefinition.KillzoneBeaconMeasureQuestId,
                DevicesCapstoneQuestDefinition.KillzoneBeaconBreachQuestId,
                DevicesCapstoneQuestDefinition.KillzoneBeaconCircleQuestId,
                DevicesCapstoneQuestDefinition.KillzoneBeaconMasteryQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerFoundationQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerMeasureQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerBreachQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerCircleQuestId,
                DevicesCapstoneQuestDefinition.EmergencyBunkerMasteryQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandFoundationQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandMeasureQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandBreachQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandCircleQuestId,
                LeadershipCapstoneQuestDefinition.DecisiveCommandMasteryQuestId,
            };

            RemoveAreaAccessIfNoLongerNeeded(player, KeyItemType.CapstoneViscaraRepublicEngineeringBunkerKey, questIds);
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
