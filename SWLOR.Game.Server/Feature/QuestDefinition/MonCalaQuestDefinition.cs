using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class MonCalaQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            FishingGuildQuests();
            PartyRoomForPedro();
            MonCalaPumpPressure();
            MonCalaHotelProvisions();
            MonCalaCoralMarkers();
            MonCalaViperAntidote();
            MonCalaAradileShells();
            MonCalaHydrusSamples();
            MonCalaReefCourier();
            MonCalaManifestoRecovery();
            MonCalaEcoRationLine();
            MonCalaLeaderBeacon();
            MonCalaSensorGrid();
            MonCalaPressureValves();
            MonCalaSwampBloom();
            MonCalaOctotenchInk();
            MonCalaMicrotenchMigration();
            MonCalaScorchellusMarks();
            MonCalaJungleWaterpath();
            MonCalaCaveRescue();
            MonCalaCoralNursery();
            MonCalaHotelEntertainment();
            MonCalaSurfaceLights();
            MonCalaMemorialTags();
            MonCalaSwampDredge();
            MonCalaEchoSurvey();
            MonCalaAquacultureSabotage();
            MonCalaCustomsCrates();
            MonCalaSeaweedContract();
            MonCalaReefMedrun();
            MonCalaCorrosionChecks();
            MonCalaHunterJaws();
            MonCalaSurfaceCustoms();
            MonCalaFacilityAirlocks();
            MonCalaCoralGardeners();
            MonCalaViperDen();
            MonCalaLifeguardShifts();
            MonCalaDiplomaticSeals();
            MonCalaSunkenCables();
            MonCalaOctotenchNests();
            MonCalaSharptoothMaps();
            MonCalaJunglePressure();
            MonCalaHotelShortwave();
            MonCalaCoralisleBeacons();
            MonCalaReefPlaque();
            MonCalaCivicFilters();
            MonCalaSwampMedicine();
            MonCalaTidewatchRounds();
            return _builder.Build();
        }

        private void FishingGuildQuests()
        {
            _builder.Create("fish_rod_1", "The Clothespole Rod")
                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("moat_carp", 2)
                .AddCollectItemObjective("lamp_marimo", 2)
                .AddCollectItemObjective("visc_urchin", 2)
                .AddCollectItemObjective("cobalt_jellyfish", 2)
                .AddCollectItemObjective("denizanasi", 2)
                .AddCollectItemObjective("cala_lobster", 2)
                .AddCollectItemObjective("bibikibo", 2)
                .AddCollectItemObjective("dath_sardine", 2)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for the Clothespole Rod.")

                .AddItemReward("clothespole", 1);
            _builder.Create("fish_rod_2", "The Fastwater Rod")
                .PrerequisiteQuest("fish_rod_1")
                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("hamsi", 2)
                .AddCollectItemObjective("sen_sardine", 2)
                .AddCollectItemObjective("rakaz_shellfish", 2)
                .AddCollectItemObjective("bast_sweeper", 2)
                .AddCollectItemObjective("mackerel", 2)
                .AddCollectItemObjective("greedie", 2)
                .AddCollectItemObjective("copper_frog", 2)
                .AddCollectItemObjective("yellow_globe", 2)
                .AddCollectItemObjective("muddy_siredon", 2)
                .AddCollectItemObjective("istavrit", 2)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for the Fastwater Rod.")

                .AddItemReward("fastwater_rod", 1);
            _builder.Create("fish_rod_3", "The Judge's Rod")
                .PrerequisiteQuest("fish_rod_2")
                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("quus", 2)
                .AddCollectItemObjective("forest_carp", 2)
                .AddCollectItemObjective("tiny_goldfish", 2)
                .AddCollectItemObjective("cheval_salmon", 2)
                .AddCollectItemObjective("yorchete", 2)
                .AddCollectItemObjective("white_lobster", 2)
                .AddCollectItemObjective("fat_greedie", 2)
                .AddCollectItemObjective("moorish_idol", 2)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for the Judge's Rod.")

                .AddItemReward("judge_rod", 1);
            _builder.Create("fish_rod_4", "The Yew Rod")
                .PrerequisiteQuest("fish_rod_3")
                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("nebimonite", 2)
                .AddCollectItemObjective("tricolored_carp", 2)
                .AddCollectItemObjective("blindfish", 2)
                .AddCollectItemObjective("pipira", 2)
                .AddCollectItemObjective("tiger_cod", 2)
                .AddCollectItemObjective("bonefish", 2)
                .AddCollectItemObjective("giant_catfish", 2)
                .AddCollectItemObjective("yayinbaligi", 2)
                .AddCollectItemObjective("deadmoiselle", 2)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for the Yew Rod.")

                .AddItemReward("yew_rod", 1);
            _builder.Create("fish_rod_5", "The Legendary Rod")
                .PrerequisiteQuest("fish_rod_4")
                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("moat_carp", 10000)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for Lu Shang's Fishing Rod.")

                .AddItemReward("lushang_rod", 1)

                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.TheLegendaryRod);
                });
        }

        private void PartyRoomForPedro()
        {
            _builder.Create("partyroom_pedro", "Party Room for P3DR0")
                .AddState()
                .SetStateJournalText("P3DR0 wants a new place to party where they're not going to get kicked out. Bring them five speakers, a jukebox, and the schematics for a new cantina.")
                .AddCollectItemObjective("structure_0330", 5)
                .AddCollectItemObjective("structure_0005", 1)
                .AddCollectItemObjective("structure_5004", 1)

                .AddState()
                .SetStateJournalText("Looks like P3DR0's going to be able to party. Make sure you talk to them!")

                .AddGoldReward(7500)
                .AddXPReward(2500)
                .AddItemReward("recipe_fabdance1", 1);
        }

        private void MonCalaPumpPressure()
        {
            _builder.Create("mon_pump_pressure", "Pump Pressure")
                .AddState()
                .SetStateJournalText("Ithal Merr asked you to inspect pump terminals on Dac City Surface. Return to Ithal Merr when it is done.")

                .AddState()
                .SetStateJournalText("Return to Ithal Merr for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaHotelProvisions()
        {
            _builder.Create("mon_hotel_provisions", "Hotel Provisions")
                .AddState()
                .SetStateJournalText("Neti Vaash asked you to deliver provisions to the Elite Hotel kitchen. The trail points toward Dac City exterior. Return to Neti Vaash when it is done.")

                .AddState()
                .SetStateJournalText("Return to Neti Vaash for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaCoralMarkers()
        {
            _builder.Create("mon_coral_markers", "Coral Markers")
                .AddState()
                .SetStateJournalText("Sulo Renn asked you to calibrate markers in the Coral Isles. The trail points toward the inner coral isles. Return to Sulo Renn when it is done.")

                .AddState()
                .SetStateJournalText("Return to Sulo Renn for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaViperAntidote()
        {
            _builder.Create("mon_viper_antidote", "Viper Antidote")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Dr. Kelles asked you to gather viper venom sacs. The trail points toward Dac City exterior. Return to Dr. Kelles when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_Viper, 8)
                .AddCollectItemObjective("viper_bile", 5)

                .AddState()
                .SetStateJournalText("Return to Dr. Kelles for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaAradileShells()
        {
            _builder.Create("mon_aradile_shells", "Aradile Shell Study")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Tanis Voro asked you to gather aradile shell chips. The trail points toward the outer coral isles. Return to Tanis Voro when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_Aradile, 8)
                .AddCollectItemObjective("aradile_tail", 5)

                .AddState()
                .SetStateJournalText("Return to Tanis Voro for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaHydrusSamples()
        {
            _builder.Create("mon_hydrus_samples", "Hydrus Samples")
                .AddState()
                .SetStateJournalText("Pello Maark asked you to gather Amphi-Hydrus tissue samples. The trail points toward the inner coral isles. Return to Pello Maark when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_AmphiHydrus, 6)
                .AddCollectItemObjective("amphi_blood", 3)

                .AddState()
                .SetStateJournalText("Return to Pello Maark for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaReefCourier()
        {
            _builder.Create("mon_reef_courier", "Reef Courier")
                .AddState()
                .SetStateJournalText("Jossi Pell asked you to deliver sealed messages between Dac City and Coral Isles. The trail points toward Dac City surface. Return to Jossi Pell when it is done.")

                .AddState()
                .SetStateJournalText("Return to Jossi Pell for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaManifestoRecovery()
        {
            _builder.Create("mon_manifesto_recovery", "Manifesto Recovery")
                .AddState()
                .SetStateJournalText("Captain Orbel asked you to recover eco-terrorist manifestos. The trail points toward Dac City surface. Return to Captain Orbel when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_EcoTerrorist, 6)
                .AddCollectItemObjective("qi_moncala_001", 1)

                .AddState()
                .SetStateJournalText("Return to Captain Orbel for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaEcoRationLine()
        {
            _builder.Create("mon_eco_ration_line", "Ration Line Defense")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Nura Selk asked you to deal with eco-terrorists near the facility. The trail points toward the civic facility. Return to Nura Selk when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_EcoTerrorist, 8)

                .AddState()
                .SetStateJournalText("Return to Nura Selk for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaLeaderBeacon()
        {
            _builder.Create("mon_leader_beacon", "Leader Beacon")
                .AddState()
                .SetStateJournalText("Inspector Varesh asked you to defeat an eco-terrorist leader and recover its beacon. The trail points toward the civic facility. Return to Inspector Varesh when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_EcoTerrorist, 1)
                .AddCollectItemObjective("qi_moncala_002", 1)

                .AddState()
                .SetStateJournalText("Return to Inspector Varesh for your reward.")

                .AddGoldReward(4000)
                .AddXPReward(4000)
                .AddItemReward("mon_beac_core", 1);
        }

        private void MonCalaSensorGrid()
        {
            _builder.Create("mon_sensor_grid", "Sensor Grid")
                .AddState()
                .SetStateJournalText("Boro Pannik asked you to repair submerged sensor nodes. The trail points toward the civic facility. Return to Boro Pannik when it is done.")

                .AddState()
                .SetStateJournalText("Return to Boro Pannik for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaPressureValves()
        {
            _builder.Create("mon_pressure_valves", "Pressure Valves")
                .AddState()
                .SetStateJournalText("Yessa Tor asked you to gather pressure seals and repair valve boxes. The trail points toward Dac City surface. Return to Yessa Tor when it is done.")
                .AddCollectItemObjective("qi_moncala_003", 1)

                .AddState()
                .SetStateJournalText("Return to Yessa Tor for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaSwampBloom()
        {
            _builder.Create("mon_swamp_bloom", "Swamp Bloom")
                .AddState()
                .SetStateJournalText("Reva Lonn asked you to gather Sunkenhedge bloom samples. The trail points toward the sunken swamps. Return to Reva Lonn when it is done.")
                .AddCollectItemObjective("qi_moncala_004", 1)

                .AddState()
                .SetStateJournalText("Return to Reva Lonn for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaOctotenchInk()
        {
            _builder.Create("mon_octotench_ink", "Octotench Ink")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Chorr Das asked you to gather octotench ink sacs. The trail points toward the sunken swamps. Return to Chorr Das when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_Octotench, 8)
                .AddCollectItemObjective("mtench_ink", 5)

                .AddState()
                .SetStateJournalText("Return to Chorr Das for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaMicrotenchMigration()
        {
            _builder.Create("mon_microtench_migration", "Microtench Migration")
                .AddState()
                .SetStateJournalText("Hek Tal asked you to survey microtench dens in the caverns. The trail points toward the lower sea caves. Return to Hek Tal when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_Microtench, 6)

                .AddState()
                .SetStateJournalText("Return to Hek Tal for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaScorchellusMarks()
        {
            _builder.Create("mon_scorchellus_marks", "Scorchellus Marks")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Pelu Qarr asked you to gather scorchellus burn marks and tissue. The trail points toward the sunken swamps. Return to Pelu Qarr when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_Scorchellus, 8)
                .AddCollectItemObjective("scorch_chitin", 5)

                .AddState()
                .SetStateJournalText("Return to Pelu Qarr for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaJungleWaterpath()
        {
            _builder.Create("mon_jungle_waterpath", "Jungle Waterpath")
                .AddState()
                .SetStateJournalText("Sian Voro asked you to map the southern Sharptooth Jungle waterpath. The trail points toward the southern jungle. Return to Sian Voro when it is done.")

                .AddState()
                .SetStateJournalText("Return to Sian Voro for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaCaveRescue()
        {
            _builder.Create("mon_cave_rescue", "Cave Rescue")
                .AddState()
                .SetStateJournalText("Lora Finn asked you to locate a missing diver in Sharptooth Jungle Caves. The trail points toward Dac City surface. Return to Lora Finn when it is done.")

                .AddState()
                .SetStateJournalText("Return to Lora Finn for your reward.")

                .AddGoldReward(4000)
                .AddXPReward(4000);
        }

        private void MonCalaCoralNursery()
        {
            _builder.Create("mon_coral_nursery", "Coral Nursery Defense")
                .AddState()
                .SetStateJournalText("Nurra Pell asked you to clear threats around the coral nursery. The trail points toward the inner coral isles. Return to Nurra Pell when it is done.")

                .AddState()
                .SetStateJournalText("Return to Nurra Pell for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaHotelEntertainment()
        {
            _builder.Create("mon_hotel_entertainment", "Entertainment Contract")
                .AddState()
                .SetStateJournalText("P3DR1 asked you to recover entertainment equipment for the Elite Hotel. The trail points toward Dac City exterior. Return to P3DR1 when it is done.")
                .AddCollectItemObjective("qi_moncala_005", 1)

                .AddState()
                .SetStateJournalText("Return to P3DR1 for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaSurfaceLights()
        {
            _builder.Create("mon_surface_lights", "Surface Lights")
                .AddState()
                .SetStateJournalText("Jalen Voss asked you to repair safety lights on Dac City Surface. Return to Jalen Voss when it is done.")

                .AddState()
                .SetStateJournalText("Return to Jalen Voss for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaMemorialTags()
        {
            _builder.Create("mon_memorial_tags", "Memorial Tags")
                .AddState()
                .SetStateJournalText("Ora Tannis asked you to recover memorial tags from Coral Isles Outer. The trail points toward the outer coral isles. Return to Ora Tannis when it is done.")
                .AddCollectItemObjective("qi_moncala_006", 1)

                .AddState()
                .SetStateJournalText("Return to Ora Tannis for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaSwampDredge()
        {
            _builder.Create("mon_swamp_dredge", "Swamp Dredge")
                .AddState()
                .SetStateJournalText("Cavi Rol asked you to gather dredge samples from Sunkenhedge Swamps. The trail points toward the sunken swamps. Return to Cavi Rol when it is done.")
                .AddCollectItemObjective("qi_moncala_007", 1)

                .AddState()
                .SetStateJournalText("Return to Cavi Rol for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaEchoSurvey()
        {
            _builder.Create("mon_echo_survey", "Echo Survey")
                .AddState()
                .SetStateJournalText("Bem Oss asked you to place echo beacons in Sharptooth Jungle Caves. The trail points toward the lower sea caves. Return to Bem Oss when it is done.")

                .AddState()
                .SetStateJournalText("Return to Bem Oss for your reward.")

                .AddGoldReward(4000)
                .AddXPReward(4000);
        }

        private void MonCalaAquacultureSabotage()
        {
            _builder.Create("mon_aquaculture_sabotage", "Aquaculture Sabotage")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Foreman Ven asked you to deal with eco-terrorists and recover sabotage parts. The trail points toward the civic facility. Return to Foreman Ven when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_EcoTerrorist, 8)
                .AddCollectItemObjective("qi_moncala_008", 1)

                .AddState()
                .SetStateJournalText("Return to Foreman Ven for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaCustomsCrates()
        {
            _builder.Create("mon_customs_crates", "Customs Crates")
                .AddState()
                .SetStateJournalText("Jaro Minn asked you to recover misplaced customs crates. The trail points toward Dac City surface. Return to Jaro Minn when it is done.")
                .AddCollectItemObjective("qi_moncala_009", 1)

                .AddState()
                .SetStateJournalText("Return to Jaro Minn for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaSeaweedContract()
        {
            _builder.Create("mon_seaweed_contract", "Seaweed Contract")
                .AddState()
                .SetStateJournalText("Pell Shenn asked you to gather seaweed bundles from Coral Isles. The trail points toward Dac City exterior. Return to Pell Shenn when it is done.")
                .AddCollectItemObjective("qi_moncala_010", 1)

                .AddState()
                .SetStateJournalText("Return to Pell Shenn for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaReefMedrun()
        {
            _builder.Create("mon_reef_medrun", "Reef Medrun")
                .AddState()
                .SetStateJournalText("Dr. Siva asked you to deliver medpacs to a reef survey team. The trail points toward Dac City surface. Return to Dr. Siva when it is done.")

                .AddState()
                .SetStateJournalText("Return to Dr. Siva for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaCorrosionChecks()
        {
            _builder.Create("mon_corrosion_checks", "Corrosion Checks")
                .AddState()
                .SetStateJournalText("Katha Noll asked you to inspect corrosion points around Dac City Surface. Return to Katha Noll when it is done.")

                .AddState()
                .SetStateJournalText("Return to Katha Noll for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaHunterJaws()
        {
            _builder.Create("mon_hunter_jaws", "Hunter Jaws")
                .AddState()
                .SetStateJournalText("Bess Olan asked you to gather predator jaw trophies from jungle threats. The trail points toward the southern jungle. Return to Bess Olan when it is done.")
                .AddCollectItemObjective("qi_moncala_011", 1)

                .AddState()
                .SetStateJournalText("Return to Bess Olan for your reward.")

                .AddGoldReward(4000)
                .AddXPReward(4000)
                .AddItemReward("mon_jaw_charm", 1);
        }

        private void MonCalaSurfaceCustoms()
        {
            _builder.Create("mon_surface_customs", "Surface Customs")
                .AddState()
                .SetStateJournalText("Customs Officer Ruun asked you to recover customs stamps from misplaced cargo lockers. The trail points toward Dac City surface. Return to Customs Officer Ruun when it is done.")
                .AddCollectItemObjective("qi_moncala_012", 1)

                .AddState()
                .SetStateJournalText("Return to Customs Officer Ruun for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaFacilityAirlocks()
        {
            _builder.Create("mon_facility_airlocks", "Facility Airlocks")
                .AddState()
                .SetStateJournalText("Airlock Tech Vesh asked you to inspect and repair Coral Isles facility airlocks. The trail points toward the civic facility. Return to Airlock Tech Vesh when it is done.")

                .AddState()
                .SetStateJournalText("Return to Airlock Tech Vesh for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaCoralGardeners()
        {
            _builder.Create("mon_coral_gardeners", "Coral Gardeners")
                .AddState()
                .SetStateJournalText("Keeper Nima asked you to gather coral clipping samples for reef restoration. The trail points toward the inner coral isles. Return to Keeper Nima when it is done.")
                .AddCollectItemObjective("qi_moncala_013", 1)

                .AddState()
                .SetStateJournalText("Return to Keeper Nima for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaViperDen()
        {
            _builder.Create("mon_viper_den", "Viper Den")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Ranger Pello asked you to clear vipers from a Coral Isles Outer nesting path. The trail points toward the outer coral isles. Return to Ranger Pello when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_Viper, 8)

                .AddState()
                .SetStateJournalText("Return to Ranger Pello for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaLifeguardShifts()
        {
            _builder.Create("mon_lifeguard_shifts", "Lifeguard Shifts")
                .AddState()
                .SetStateJournalText("Watcher Della asked you to visit watch points around Dac City Surface. Return to Watcher Della when it is done.")

                .AddState()
                .SetStateJournalText("Return to Watcher Della for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaDiplomaticSeals()
        {
            _builder.Create("mon_diplomatic_seals", "Diplomatic Seals")
                .AddState()
                .SetStateJournalText("Envoy Varo asked you to recover diplomatic seals from the Elite Hotel service wing. The trail points toward Dac City exterior. Return to Envoy Varo when it is done.")
                .AddCollectItemObjective("qi_moncala_014", 1)

                .AddState()
                .SetStateJournalText("Return to Envoy Varo for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaSunkenCables()
        {
            _builder.Create("mon_sunken_cables", "Sunken Cables")
                .AddState()
                .SetStateJournalText("Cablehand Reth asked you to recover sunken cable bundles from the swamps. The trail points toward the sunken swamps. Return to Cablehand Reth when it is done.")
                .AddCollectItemObjective("qi_moncala_015", 1)

                .AddState()
                .SetStateJournalText("Return to Cablehand Reth for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaOctotenchNests()
        {
            _builder.Create("mon_octotench_nests", "Octotench Nests")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Biologist Ora asked you to clear octotench nests and collect nest fibers. The trail points toward the sunken swamps. Return to Biologist Ora when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_Octotench, 8)
                .AddCollectItemObjective("mtench_ink", 5)

                .AddState()
                .SetStateJournalText("Return to Biologist Ora for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaSharptoothMaps()
        {
            _builder.Create("mon_sharptooth_maps", "Sharptooth Maps")
                .AddState()
                .SetStateJournalText("Scout Jalen asked you to map safe paths in Sharptooth Jungle North. The trail points toward the wild jungle. Return to Scout Jalen when it is done.")

                .AddState()
                .SetStateJournalText("Return to Scout Jalen for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaJunglePressure()
        {
            _builder.Create("mon_jungle_pressure", "Jungle Pressure")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Patrol Lead Oss asked you to cull jungle predators near the southern trail. The trail points toward the southern jungle. Return to Patrol Lead Oss when it is done.")

                .AddState()
                .SetStateJournalText("Return to Patrol Lead Oss for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaHotelShortwave()
        {
            _builder.Create("mon_hotel_shortwave", "Hotel Shortwave")
                .AddState()
                .SetStateJournalText("Signal Clerk Nessa asked you to repair shortwave relays in the Elite Hotel. The trail points toward Dac City exterior. Return to Signal Clerk Nessa when it is done.")

                .AddState()
                .SetStateJournalText("Return to Signal Clerk Nessa for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaCoralisleBeacons()
        {
            _builder.Create("mon_coralisle_beacons", "Coral Isle Beacons")
                .AddState()
                .SetStateJournalText("Beacon Tech Hesh asked you to activate navigation beacons across Coral Isles Inner. The trail points toward the inner coral isles. Return to Beacon Tech Hesh when it is done.")

                .AddState()
                .SetStateJournalText("Return to Beacon Tech Hesh for your reward.")

                .AddGoldReward(4000)
                .AddXPReward(4000);
        }

        private void MonCalaReefPlaque()
        {
            _builder.Create("mon_reef_plaque", "Reef Plaque")
                .AddState()
                .SetStateJournalText("Historian Bel asked you to recover broken dedication plaques from Coral Isles Outer. The trail points toward the outer coral isles. Return to Historian Bel when it is done.")
                .AddCollectItemObjective("qi_moncala_016", 1)

                .AddState()
                .SetStateJournalText("Return to Historian Bel for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void MonCalaCivicFilters()
        {
            _builder.Create("mon_civic_filters", "Civic Filters")
                .AddState()
                .SetStateJournalText("Civic Engineer Dova asked you to replace filter cartridges in Dac City infrastructure. The trail points toward Dac City surface. Return to Civic Engineer Dova when it is done.")

                .AddState()
                .SetStateJournalText("Return to Civic Engineer Dova for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaSwampMedicine()
        {
            _builder.Create("mon_swamp_medicine", "Swamp Medicine")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Dr. Hala asked you to gather medicinal swamp algae and microtench samples. The trail points toward the sunken swamps. Return to Dr. Hala when it is done.")
                .AddKillObjective(NPCGroupType.MonCala_Microtench, 8)
                .AddCollectItemObjective("qi_moncala_017", 1)

                .AddState()
                .SetStateJournalText("Return to Dr. Hala for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private void MonCalaTidewatchRounds()
        {
            _builder.Create("mon_tidewatch_rounds", "Tidewatch Rounds")
                .AddState()
                .SetStateJournalText("Tidewatcher Pell asked you to complete tidewatch rounds and report abnormal readings. The trail points toward Dac City surface. Return to Tidewatcher Pell when it is done.")

                .AddState()
                .SetStateJournalText("Return to Tidewatcher Pell for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(7500)
                .AddItemReward("mon_tide_lens", 1);
        }
    }
}
