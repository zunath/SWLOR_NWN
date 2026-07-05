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
            _builder.Create(ThermalDetonatorFoundationQuestId, "First Principle: Thermal Detonator")
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
                    "The Thermal Detonator capstone line continues in Hutlar Qion Test Site. Defeat Thermal Detonator adepts and secure the Thermal Detonator Qion Test Log.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneThermalDetonatorQionTestLog)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Thermal Detonator Qion Test Log from Hutlar Qion Test Site. Return to Ruk Halven for the next Thermal Detonator lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void ThermalDetonatorMeasure()
        {
            _builder.Create(ThermalDetonatorMeasureQuestId, "The Measure of Thermal Detonator")
                .PrerequisiteQuest(ThermalDetonatorFoundationQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneThermalDetonatorCryoRangeRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneThermalDetonatorCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator capstone line continues in Hutlar Qion Test Site. Defeat Thermal Detonator specialists and secure the Thermal Detonator Cryo-Range Regulator.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneThermalDetonatorCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Thermal Detonator Cryo-Range Regulator from Hutlar Qion Test Site. Return to Ruk Halven for the next Thermal Detonator lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void ThermalDetonatorBreach()
        {
            _builder.Create(ThermalDetonatorBreachQuestId, "Fault Line: Thermal Detonator")
                .PrerequisiteQuest(ThermalDetonatorMeasureQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneThermalDetonatorFrostburnedTestCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneThermalDetonatorFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator capstone line continues in Hutlar Qion Test Site. Defeat the Thermal Detonator warden and secure the Thermal Detonator Frostburned Test Crest.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneThermalDetonatorFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Thermal Detonator Frostburned Test Crest from Hutlar Qion Test Site. Return to Ruk Halven for the next Thermal Detonator lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void ThermalDetonatorCircle()
        {
            _builder.Create(ThermalDetonatorCircleQuestId, "Circle of Proof: Thermal Detonator")
                .PrerequisiteQuest(ThermalDetonatorBreachQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneThermalDetonatorSiteChiefsOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneThermalDetonatorSiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Thermal Detonator capstone line continues in Hutlar Qion Test Site. Defeat the Thermal Detonator inner circle and secure the Thermal Detonator Site Chief's Override Chip.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneThermalDetonatorSiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Thermal Detonator Site Chief's Override Chip from Hutlar Qion Test Site. Return to Ruk Halven for the next Thermal Detonator lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void ThermalDetonatorMastery()
        {
            _builder.Create(ThermalDetonatorMasteryQuestId, "Thermal Detonator Mastery")
                .PrerequisiteQuest(ThermalDetonatorCircleQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Thermal Detonator master is waiting in Hutlar Qion Test Site. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Hutlar_ThermalDetonator_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Thermal Detonator master is defeated. Return to Ruk Halven and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.ThermalDetonator);
                });
        }

        private void OverloadBarrageFoundation()
        {
            _builder.Create(OverloadBarrageFoundationQuestId, "First Principle: Overload Barrage")
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
                    "The Overload Barrage capstone line continues in Hutlar Qion Test Site. Defeat Overload Barrage adepts and secure the Overload Barrage Qion Test Log.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOverloadBarrageQionTestLog)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Overload Barrage Qion Test Log from Hutlar Qion Test Site. Return to Miri Koss for the next Overload Barrage lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void OverloadBarrageMeasure()
        {
            _builder.Create(OverloadBarrageMeasureQuestId, "The Measure of Overload Barrage")
                .PrerequisiteQuest(OverloadBarrageFoundationQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOverloadBarrageCryoRangeRegulator)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOverloadBarrageCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage capstone line continues in Hutlar Qion Test Site. Defeat Overload Barrage specialists and secure the Overload Barrage Cryo-Range Regulator.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOverloadBarrageCryoRangeRegulator)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Overload Barrage Cryo-Range Regulator from Hutlar Qion Test Site. Return to Miri Koss for the next Overload Barrage lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void OverloadBarrageBreach()
        {
            _builder.Create(OverloadBarrageBreachQuestId, "Fault Line: Overload Barrage")
                .PrerequisiteQuest(OverloadBarrageMeasureQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOverloadBarrageFrostburnedTestCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOverloadBarrageFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage capstone line continues in Hutlar Qion Test Site. Defeat the Overload Barrage warden and secure the Overload Barrage Frostburned Test Crest.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOverloadBarrageFrostburnedTestCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Overload Barrage Frostburned Test Crest from Hutlar Qion Test Site. Return to Miri Koss for the next Overload Barrage lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void OverloadBarrageCircle()
        {
            _builder.Create(OverloadBarrageCircleQuestId, "Circle of Proof: Overload Barrage")
                .PrerequisiteQuest(OverloadBarrageBreachQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneOverloadBarrageSiteChiefsOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneOverloadBarrageSiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Overload Barrage capstone line continues in Hutlar Qion Test Site. Defeat the Overload Barrage inner circle and secure the Overload Barrage Site Chief's Override Chip.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneOverloadBarrageSiteChiefsOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Overload Barrage Site Chief's Override Chip from Hutlar Qion Test Site. Return to Miri Koss for the next Overload Barrage lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void OverloadBarrageMastery()
        {
            _builder.Create(OverloadBarrageMasteryQuestId, "Overload Barrage Mastery")
                .PrerequisiteQuest(OverloadBarrageCircleQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Overload Barrage master is waiting in Hutlar Qion Test Site. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Hutlar_OverloadBarrage_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Overload Barrage master is defeated. Return to Miri Koss and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.OverloadBarrage);
                });
        }

        private void KillzoneBeaconFoundation()
        {
            _builder.Create(KillzoneBeaconFoundationQuestId, "First Principle: Killzone Beacon")
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
                    "The Killzone Beacon capstone line continues in Viscara Republic Engineering Bunker. Defeat Killzone Beacon adepts and secure the Killzone Beacon Republic Bunker Docket.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillzoneBeaconRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Killzone Beacon Republic Bunker Docket from Viscara Republic Engineering Bunker. Return to Aric Jorr for the next Killzone Beacon lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void KillzoneBeaconMeasure()
        {
            _builder.Create(KillzoneBeaconMeasureQuestId, "The Measure of Killzone Beacon")
                .PrerequisiteQuest(KillzoneBeaconFoundationQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillzoneBeaconShieldGridRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillzoneBeaconShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon capstone line continues in Viscara Republic Engineering Bunker. Defeat Killzone Beacon specialists and secure the Killzone Beacon Shield Grid Relay.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillzoneBeaconShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Killzone Beacon Shield Grid Relay from Viscara Republic Engineering Bunker. Return to Aric Jorr for the next Killzone Beacon lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void KillzoneBeaconBreach()
        {
            _builder.Create(KillzoneBeaconBreachQuestId, "Fault Line: Killzone Beacon")
                .PrerequisiteQuest(KillzoneBeaconMeasureQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillzoneBeaconCrackedCommandCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillzoneBeaconCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon capstone line continues in Viscara Republic Engineering Bunker. Defeat the Killzone Beacon warden and secure the Killzone Beacon Cracked Command Crest.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillzoneBeaconCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Killzone Beacon Cracked Command Crest from Viscara Republic Engineering Bunker. Return to Aric Jorr for the next Killzone Beacon lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void KillzoneBeaconCircle()
        {
            _builder.Create(KillzoneBeaconCircleQuestId, "Circle of Proof: Killzone Beacon")
                .PrerequisiteQuest(KillzoneBeaconBreachQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneKillzoneBeaconQuartermasterOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneKillzoneBeaconQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Killzone Beacon capstone line continues in Viscara Republic Engineering Bunker. Defeat the Killzone Beacon inner circle and secure the Killzone Beacon Quartermaster Override Chip.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneKillzoneBeaconQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Killzone Beacon Quartermaster Override Chip from Viscara Republic Engineering Bunker. Return to Aric Jorr for the next Killzone Beacon lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void KillzoneBeaconMastery()
        {
            _builder.Create(KillzoneBeaconMasteryQuestId, "Killzone Beacon Mastery")
                .PrerequisiteQuest(KillzoneBeaconCircleQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Killzone Beacon master is waiting in Viscara Republic Engineering Bunker. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Viscara_KillzoneBeacon_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Killzone Beacon master is defeated. Return to Aric Jorr and claim the completed lesson.")
                .AddXPReward(30000)
                .AddGoldReward(18000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.KillzoneBeacon);
                });
        }

        private void EmergencyBunkerFoundation()
        {
            _builder.Create(EmergencyBunkerFoundationQuestId, "First Principle: Emergency Bunker")
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
                    "The Emergency Bunker capstone line continues in Viscara Republic Engineering Bunker. Defeat Emergency Bunker adepts and secure the Emergency Bunker Republic Bunker Docket.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_Adept, 6)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyBunkerRepublicBunkerDocket)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Emergency Bunker Republic Bunker Docket from Viscara Republic Engineering Bunker. Return to Nella Voss for the next Emergency Bunker lesson.")
                .AddXPReward(15000)
                .AddGoldReward(7500);
        }

        private void EmergencyBunkerMeasure()
        {
            _builder.Create(EmergencyBunkerMeasureQuestId, "The Measure of Emergency Bunker")
                .PrerequisiteQuest(EmergencyBunkerFoundationQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyBunkerShieldGridRelay)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyBunkerShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker capstone line continues in Viscara Republic Engineering Bunker. Defeat Emergency Bunker specialists and secure the Emergency Bunker Shield Grid Relay.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_Specialist, 5)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyBunkerShieldGridRelay)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Emergency Bunker Shield Grid Relay from Viscara Republic Engineering Bunker. Return to Nella Voss for the next Emergency Bunker lesson.")
                .AddXPReward(17500)
                .AddGoldReward(9000);
        }

        private void EmergencyBunkerBreach()
        {
            _builder.Create(EmergencyBunkerBreachQuestId, "Fault Line: Emergency Bunker")
                .PrerequisiteQuest(EmergencyBunkerMeasureQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyBunkerCrackedCommandCrest)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyBunkerCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker capstone line continues in Viscara Republic Engineering Bunker. Defeat the Emergency Bunker warden and secure the Emergency Bunker Cracked Command Crest.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_Warden, 1)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyBunkerCrackedCommandCrest)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Emergency Bunker Cracked Command Crest from Viscara Republic Engineering Bunker. Return to Nella Voss for the next Emergency Bunker lesson.")
                .AddXPReward(20000)
                .AddGoldReward(10500);
        }

        private void EmergencyBunkerCircle()
        {
            _builder.Create(EmergencyBunkerCircleQuestId, "Circle of Proof: Emergency Bunker")
                .PrerequisiteQuest(EmergencyBunkerBreachQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)
                .RemoveKeyItemOnAbandon(KeyItemType.CapstoneEmergencyBunkerQuartermasterOverrideChip)
                .RemoveKeyItemOnComplete(KeyItemType.CapstoneEmergencyBunkerQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    "The Emergency Bunker capstone line continues in Viscara Republic Engineering Bunker. Defeat the Emergency Bunker inner circle and secure the Emergency Bunker Quartermaster Override Chip.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_InnerCircle, 4)
                .GrantKeyItemOnAdvance(KeyItemType.CapstoneEmergencyBunkerQuartermasterOverrideChip)

                .AddState()
                .SetStateJournalText(
                    $"You secured the Emergency Bunker Quartermaster Override Chip from Viscara Republic Engineering Bunker. Return to Nella Voss for the next Emergency Bunker lesson.")
                .AddXPReward(22500)
                .AddGoldReward(12000);
        }

        private void EmergencyBunkerMastery()
        {
            _builder.Create(EmergencyBunkerMasteryQuestId, "Emergency Bunker Mastery")
                .PrerequisiteQuest(EmergencyBunkerCircleQuestId)
                .PrerequisiteSkill(SkillType.Devices, 50)

                .AddState()
                .SetStateJournalText(
                    "The final Emergency Bunker master is waiting in Viscara Republic Engineering Bunker. Defeat the master and end the capstone trial.")
                .AddKillObjective(NPCGroupType.Viscara_EmergencyBunker_Master, 1)

                .AddState()
                .SetStateJournalText(
                    "The final Emergency Bunker master is defeated. Return to Nella Voss and claim the completed lesson.")
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
