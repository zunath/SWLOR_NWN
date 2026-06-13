using System.Collections.Generic;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class DantooineQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            DanBundle();
            DanMedicalSupplies();
            BlueMilkQuest();
            CullVoritorLizardThreat();
            HarvestingHerbs();
            FetchPetTreat();
            CollectHerbsForLibrarian();
            HiddenCave();
            DantooineWellFilters();
            DantooineThuneDrive();
            DantooineIriazCensus();
            DantooineGizkaInfestation();
            DantooineKoltoCache();
            DantooineTriageSupplies();
            DantooineCrystalHarmonics();
            DantooineCaveShards();
            DantooineLizardEggs();
            DantooineLowerEchoes();
            DantooineKinrathVenom();
            DantooineQueenTracks();
            DantooineArchiveFolios();
            DantooineRelicScans();
            DantooineFallenMarkers();
            DantooineDeserterNotes();
            DantooineMedConvoy();
            DantooineSmugglerManifest();
            DantooineLakeReeds();
            DantooineLakePressure();
            DantooineBolTracks();
            DantooineHerdPressure();
            DantooineDantariRites();
            DantooineHunterPatrol();
            DantooineRopeAnchors();
            DantooineHayRecovery();
            DantooineFieldBeacons();
            DantooineMineralSamples();
            DantooineHiddenPack();
            DantooineSpaHerbs();
            DantooineClearJunglePatrol();
            DantooineTranquilPlainMarks();
            DantooineCrafterBaseOrder();
            DantooineBattleGymFeed();
            DantooineJungleSpores();
            DantooineMountainCrystals();
            DantooineSmugglerMaps();
            DantooineRepublicAmmo();
            DantooineBolWarning();
            DantooineDantariCharms();
            DantooineKinrathEggs();
            DantooineLakeFishline();
            DantooineWarehouseManifest();
            DantooineColonyCircuit();
            return _builder.Build();
        }

        private void BlueMilkQuest()
        {
            _builder.Create("bantha_milk_quest", "Bantha Milk Quest")
               .AddState()
               .SetStateJournalText("The farmer from Dantooine requires milk that has been taken from the Dantari. Find it and bring back the milk.")
               .AddCollectItemObjective("bantha_milk", 20)

               .AddState()
               .SetStateJournalText("Return to the farmer and deliver the stolen blue milk.")

               .AddXPReward(4000)
               .AddGoldReward(3750);
        }

        private void CullVoritorLizardThreat()
        {
            _builder.Create("voritor_lizard_threat", "Cull the Voritor Lizard Threat")
                .AddState()
                .SetStateJournalText("Jason wants you to head to the Janta Caves and kill ten Voritor Lizards. Report back when this is done.")
                .AddKillObjective(NPCGroupType.Dantooine_VoritorLizard, 10)

                .AddState()
                .SetStateJournalText("Return to Jason in the Dantooine Colony and report your progress.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void DanMedicalSupplies()
        {
            _builder.Create("medical_supplies", "Medical Supplies for the Clinic")
                .AddState()
                .SetStateJournalText("The clinic in Dantooine Medical Facility needs  kolto injections and  medi syringes. Collect them from the Abandoned Warehouse and return them to the clinic.")
                .AddCollectItemObjective("kolto_injection", 20)
                .AddCollectItemObjective("medisyringes", 5)

                .AddState()
                .SetStateJournalText("You delivered the kolto injections and medi syringes to the clinic. Talk to the clinic staff for your reward.")

                .AddXPReward(4000)
                .AddGoldReward(7500)
                .AddItemReward("med_supplies", 20)
                .AddItemReward("stim_pack", 10)
                .AddItemReward("wild_sandwich", 1);
        }

        private void DanBundle()
        {
            _builder.Create("hay_bundles", "Hay bales for Wrrl")
                .AddState()
                .SetStateJournalText("The farmer needs help with his herd. Collect 20 bags of hay bales from the Ruin Farmlands and return them to the farmer.")
                .AddCollectItemObjective("haybundle", 20)

                .AddState()
                .SetStateJournalText("You delivered the hay bundles to the farmer. Talk to the farmer for your reward.")

                .AddXPReward(2000)
                .AddGoldReward(1500);
        }

        private void HarvestingHerbs()
        {
            _builder.Create("harvest_herbs", "Harvesting Herbs")
               .IsRepeatable()
               .AddState()
               .SetStateJournalText("Collect rare Dantooine Starwort herbs from the Crystal fields of Dantooine.")
               .AddCollectItemObjective("dant_starwort", 15)

               .AddState()
               .SetStateJournalText("Deliver the herbs to the healer in the Colony.")

               .AddXPReward(600)
               .AddGoldReward(300);
        }

        private void FetchPetTreat()
        {
            _builder.Create("fetch_pet_treat", "Fetch Pet Treat Quest")
               .AddState()
               .SetStateJournalText("The battlegym trainer needs a Yot Beans to make pet treats. Find the Yot Beans and bring it back.")
               .AddCollectItemObjective("yotbean", 10)

               .AddState()
               .SetStateJournalText("Return to the battlegym trainer with the Yot Beans.")

               .AddXPReward(2000)
               .AddGoldReward(2250)
               .AddItemReward("pf_dryfruit_5", 1)
               .AddItemReward("pf_sourfruit_1", 1);
        }

        private void CollectHerbsForLibrarian()
        {
            _builder.Create("collect_herbs_librarian", "Collect Herbs for the Librarian")
               .AddState()
               .SetStateJournalText("The Jedi librarian needs Yot Beans and Dantooine Starworts for his research. Collect these items and bring them back.")
               .AddCollectItemObjective("yotbean", 10)
               .AddCollectItemObjective("dant_starwort", 15)

               .AddState()
               .SetStateJournalText("Return to the Jedi librarian with the collected herbs.")

               .AddItemReward("emerald", 2)
               .AddXPReward(5000)
               .AddGoldReward(4500);
        }

        private void HiddenCave()
        {
            _builder.Create("hidden_cave", "Find the hidden cave")
                .AddState()
                .AddKillObjective(NPCGroupType.Dantooine_KinrathQueen, 1)
                .SetStateJournalText("Head to the kinrath cave and defeat the Kinrath queen. Return to Joran when the work is done.")

                .AddState()
                .SetStateJournalText("You defeated the Kinrath Queen. Return to Joran for that shovel.")

                .AddGoldReward(11250)
                .AddXPReward(12000)

                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.DantooineShovel);
                });
        }

        private void DantooineWellFilters()
        {
            _builder.Create("dan_well_filters", "Well Filters")
                .AddState()
                .SetStateJournalText("Mella Rusk asked you to gather well filters from Colony South Farms. The trail points toward the colony farms. Return to Mella Rusk when it is done.")
                .AddCollectItemObjective("qi_dantooine_001", 1)

                .AddState()
                .SetStateJournalText("Return to Mella Rusk for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineThuneDrive()
        {
            _builder.Create("dan_thune_drive", "Thune Drive")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Farmer Willen asked you to deal with Plains Thune in the ruined farmlands. Return to Farmer Willen when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_PlainsThune, 8)

                .AddState()
                .SetStateJournalText("Return to Farmer Willen for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineIriazCensus()
        {
            _builder.Create("dan_iriaz_census", "Iriaz Census")
                .AddState()
                .SetStateJournalText("Toma Pell asked you to survey Iriaz herds. The trail points toward the Iriaz pastures. Return to Toma Pell when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_Iriaz, 6)

                .AddState()
                .SetStateJournalText("Return to Toma Pell for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineGizkaInfestation()
        {
            _builder.Create("dan_gizka_infestation", "Gizka Infestation")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Beki Lorn asked you to deal with Gizka near the colony. The trail points toward the colony farms. Return to Beki Lorn when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_Gizka, 8)

                .AddState()
                .SetStateJournalText("Return to Beki Lorn for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineKoltoCache()
        {
            _builder.Create("dan_kolto_cache", "Kolto Cache")
                .AddState()
                .SetStateJournalText("Nurse Orva asked you to recover kolto from the Abandoned Warehouse. The trail points toward the colony medical ward. Return to Nurse Orva when it is done.")
                .AddCollectItemObjective("qi_dantooine_002", 1)

                .AddState()
                .SetStateJournalText("Return to Nurse Orva for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineTriageSupplies()
        {
            _builder.Create("dan_triage_supplies", "Triage Supplies")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Dr. Jenso asked you to gather medi supplies for Republic Med Center. Return to Dr. Jenso when it is done.")
                .AddCollectItemObjective("kolto_injection", 10)

                .AddState()
                .SetStateJournalText("Return to Dr. Jenso for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineCrystalHarmonics()
        {
            _builder.Create("dan_crystal_harmonics", "Crystal Harmonics")
                .AddState()
                .SetStateJournalText("Vesa Noll asked you to tune crystal resonators in the field. The trail points toward the crystal fields. Return to Vesa Noll when it is done.")

                .AddState()
                .SetStateJournalText("Return to Vesa Noll for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineCaveShards()
        {
            _builder.Create("dan_cave_shards", "Cave Shards")
                .AddState()
                .SetStateJournalText("Orren Vale asked you to gather crystal shards from the canyon caves. The trail points toward the crystal caves. Return to Orren Vale when it is done.")
                .AddCollectItemObjective("qi_dantooine_003", 1)

                .AddState()
                .SetStateJournalText("Return to Orren Vale for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineLizardEggs()
        {
            _builder.Create("dan_lizard_eggs", "Lizard Eggs")
                .AddState()
                .SetStateJournalText("Jason Marr asked you to gather Voritor Lizard eggs. The trail points toward the Janta caves. Return to Jason Marr when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_VoritorLizard, 6)
                .AddCollectItemObjective("qi_dantooine_004", 1)

                .AddState()
                .SetStateJournalText("Return to Jason Marr for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineLowerEchoes()
        {
            _builder.Create("dan_lower_echoes", "Lower Echoes")
                .AddState()
                .SetStateJournalText("Pella Senn asked you to place echo beacons in the lower Janta caves. Return to Pella Senn when it is done.")

                .AddState()
                .SetStateJournalText("Return to Pella Senn for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineKinrathVenom()
        {
            _builder.Create("dan_kinrath_venom", "Kinrath Venom")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Hira Vos asked you to gather kinrath venom glands. The trail points toward the Kinrath cave. Return to Hira Vos when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_Kinrath, 8)
                .AddCollectItemObjective("qi_dantooine_005", 1)

                .AddState()
                .SetStateJournalText("Return to Hira Vos for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineQueenTracks()
        {
            _builder.Create("dan_queen_tracks", "Queen Tracks")
                .AddState()
                .SetStateJournalText("Joran Vel asked you to track the Kinrath Queen through cave signs. The trail points toward the Kinrath cave. Return to Joran Vel when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_KinrathQueen, 1)

                .AddState()
                .SetStateJournalText("Return to Joran Vel for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(6000)
                .AddItemReward("dan_queen_chit", 1);
        }

        private void DantooineArchiveFolios()
        {
            _builder.Create("dan_archive_folios", "Archive Folios")
                .AddState()
                .SetStateJournalText("Archivist Bess asked you to recover archive folios from nearby ruins. The trail points toward the Jedi library. Return to Archivist Bess when it is done.")
                .AddCollectItemObjective("qi_dantooine_006", 1)

                .AddState()
                .SetStateJournalText("Return to Archivist Bess for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineRelicScans()
        {
            _builder.Create("dan_relic_scans", "Relic Scans")
                .AddState()
                .SetStateJournalText("Jedi Librarian Arel asked you to survey relics in the Jedi Enclave Library. Return to Jedi Librarian Arel when it is done.")

                .AddState()
                .SetStateJournalText("Return to Jedi Librarian Arel for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineFallenMarkers()
        {
            _builder.Create("dan_fallen_markers", "Fallen Markers")
                .AddState()
                .SetStateJournalText("Padawan Eno asked you to restore fallen enclave markers. The trail points toward the Jedi enclave grounds. Return to Padawan Eno when it is done.")

                .AddState()
                .SetStateJournalText("Return to Padawan Eno for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineDeserterNotes()
        {
            _builder.Create("dan_deserter_notes", "Deserter Notes")
                .AddState()
                .SetStateJournalText("Sgt. Venn asked you to recover notes around the Republic Garrison. Return to Sgt. Venn when it is done.")
                .AddCollectItemObjective("qi_dantooine_007", 1)

                .AddState()
                .SetStateJournalText("Return to Sgt. Venn for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(6000);
        }

        private void DantooineMedConvoy()
        {
            _builder.Create("dan_med_convoy", "Med Convoy")
                .AddState()
                .SetStateJournalText("Lt. Porra asked you to recover convoy crates from the field trail. The trail points toward the garrison interior. Return to Lt. Porra when it is done.")
                .AddCollectItemObjective("qi_dantooine_008", 1)

                .AddState()
                .SetStateJournalText("Return to Lt. Porra for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineSmugglerManifest()
        {
            _builder.Create("dan_smuggler_manifest", "Smuggler Manifest")
                .AddState()
                .SetStateJournalText("Nila Voss asked you to recover smuggler manifests from the caverns. The trail points toward the smuggler caverns. Return to Nila Voss when it is done.")
                .AddCollectItemObjective("qi_dantooine_009", 1)

                .AddState()
                .SetStateJournalText("Return to Nila Voss for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(6000);
        }

        private void DantooineLakeReeds()
        {
            _builder.Create("dan_lake_reeds", "Lake Reeds")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Sel Owin asked you to gather lake reed samples. The trail points toward the lake caverns. Return to Sel Owin when it is done.")
                .AddCollectItemObjective("qi_dantooine_010", 1)

                .AddState()
                .SetStateJournalText("Return to Sel Owin for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineLakePressure()
        {
            _builder.Create("dan_lake_pressure", "Lake Pressure")
                .AddState()
                .SetStateJournalText("Forester Daan asked you to deal with Kinraths around the lake. The trail points toward the lake caverns. Return to Forester Daan when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_Kinrath, 6)

                .AddState()
                .SetStateJournalText("Return to Forester Daan for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineBolTracks()
        {
            _builder.Create("dan_bol_tracks", "Thune Tracks")
                .AddState()
                .SetStateJournalText("Hunter Oric asked you to track and kill Plains Thune in the ruined farmlands. Return to Hunter Oric when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_PlainsThune, 6)

                .AddState()
                .SetStateJournalText("Return to Hunter Oric for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineHerdPressure()
        {
            _builder.Create("dan_herd_pressure", "Herd Pressure")
                .AddState()
                .SetStateJournalText("Iraz Keeper Talli asked you to cull aggressive Iriaz and scan herd markers. The trail points toward the Iriaz pastures. Return to Iraz Keeper Talli when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_Iriaz, 6)

                .AddState()
                .SetStateJournalText("Return to Iraz Keeper Talli for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineDantariRites()
        {
            _builder.Create("dan_dantari_rites", "Dantari Rites")
                .AddState()
                .SetStateJournalText("Scout Harlan asked you to recover rite tokens from Dantari Shamans. The trail points toward the Dantari fields. Return to Scout Harlan when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_DantariShaman, 6)
                .AddCollectItemObjective("qi_dantooine_011", 1)

                .AddState()
                .SetStateJournalText("Return to Scout Harlan for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(6000);
        }

        private void DantooineHunterPatrol()
        {
            _builder.Create("dan_hunter_patrol", "Hunter Patrol")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Ranger Elvo asked you to deal with Dantari Hunters in South Fields. The trail points toward the Dantari fields. Return to Ranger Elvo when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_DantariHunter, 8)

                .AddState()
                .SetStateJournalText("Return to Ranger Elvo for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineRopeAnchors()
        {
            _builder.Create("dan_rope_anchors", "Rope Anchors")
                .AddState()
                .SetStateJournalText("Climber Sesk asked you to set rope anchors in the jungle Mountain. The trail points toward the jungle mountain trail. Return to Climber Sesk when it is done.")

                .AddState()
                .SetStateJournalText("Return to Climber Sesk for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineHayRecovery()
        {
            _builder.Create("dan_hay_recovery", "Hay Recovery")
                .AddState()
                .SetStateJournalText("Wrrl Fen asked you to recover hay bales from Ruined Farmlands. Return to Wrrl Fen when it is done.")
                .AddCollectItemObjective("haybundle", 3)

                .AddState()
                .SetStateJournalText("Return to Wrrl Fen for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineFieldBeacons()
        {
            _builder.Create("dan_field_beacons", "Field Beacons")
                .AddState()
                .SetStateJournalText("Road Warden Pava asked you to activate beacon stones on the Field Trail. Return to Road Warden Pava when it is done.")

                .AddState()
                .SetStateJournalText("Return to Road Warden Pava for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineMineralSamples()
        {
            _builder.Create("dan_mineral_samples", "Mineral Samples")
                .AddState()
                .SetStateJournalText("Geologist Ren asked you to gather mineral samples from Enclosed Mountain. The trail points toward the enclosed mountain trail. Return to Geologist Ren when it is done.")
                .AddCollectItemObjective("qi_dantooine_012", 1)

                .AddState()
                .SetStateJournalText("Return to Geologist Ren for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineHiddenPack()
        {
            _builder.Create("dan_hidden_pack", "Hidden Pack")
                .AddState()
                .SetStateJournalText("Scout Vori asked you to recover a lost ranger pack on Hidden Trail. The trail points toward the hidden mountain path. Return to Scout Vori when it is done.")
                .AddCollectItemObjective("qi_dantooine_013", 1)

                .AddState()
                .SetStateJournalText("Return to Scout Vori for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineSpaHerbs()
        {
            _builder.Create("dan_spa_herbs", "Spa Herbs")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Healer Mave asked you to gather herbs for Colony Spa treatments. Return to Healer Mave when it is done.")
                .AddCollectItemObjective("dant_starwort", 5)

                .AddState()
                .SetStateJournalText("Return to Healer Mave for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineClearJunglePatrol()
        {
            _builder.Create("dan_clear_jungle_patrol", "Clear Jungle Patrol")
                .AddState()
                .SetStateJournalText("Ranger Nessa asked you to patrol Clear Jungles and mark safe paths. Return to Ranger Nessa when it is done.")

                .AddState()
                .SetStateJournalText("Return to Ranger Nessa for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineTranquilPlainMarks()
        {
            _builder.Create("dan_tranquil_plain_marks", "Tranquil Plain Marks")
                .AddState()
                .SetStateJournalText("Cartographer Ivo asked you to place survey marks through Tranquil Plains. Return to Cartographer Ivo when it is done.")

                .AddState()
                .SetStateJournalText("Return to Cartographer Ivo for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineCrafterBaseOrder()
        {
            _builder.Create("dan_crafter_base_order", "Crafter Base Order")
                .AddState()
                .SetStateJournalText("Foreman Pell asked you to recover misplaced crafter requisitions. The trail points toward the crafter camp. Return to Foreman Pell when it is done.")
                .AddCollectItemObjective("qi_dantooine_014", 1)

                .AddState()
                .SetStateJournalText("Return to Foreman Pell for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineBattleGymFeed()
        {
            _builder.Create("dan_battle_gym_feed", "Battle Gym Feed")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Trainer Olan asked you to gather feed bundles for Battle Monster Gym. The trail points toward the battle gym grounds. Return to Trainer Olan when it is done.")
                .AddCollectItemObjective("qi_dantooine_015", 1)

                .AddState()
                .SetStateJournalText("Return to Trainer Olan for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineJungleSpores()
        {
            _builder.Create("dan_jungle_spores", "Jungle Spores")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Botanist Hala asked you to gather spore samples from Forsaken Jungles. Return to Botanist Hala when it is done.")
                .AddCollectItemObjective("qi_dantooine_016", 1)

                .AddState()
                .SetStateJournalText("Return to Botanist Hala for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineMountainCrystals()
        {
            _builder.Create("dan_mountain_crystals", "Mountain Crystals")
                .PrerequisiteQuest("dan_rope_anchors")
                .AddState()
                .SetStateJournalText("Climber Sesk asked you to recover mountain crystal shards. The trail points toward the mountain crystal cave. Return to Climber Sesk when it is done.")
                .AddCollectItemObjective("qi_dantooine_017", 1)

                .AddState()
                .SetStateJournalText("Return to Climber Sesk for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(6000)
                .AddItemReward("dan_mtn_focus", 1);
        }

        private void DantooineSmugglerMaps()
        {
            _builder.Create("dan_smuggler_maps", "Smuggler Maps")
                .PrerequisiteQuest("dan_smuggler_manifest")
                .AddState()
                .SetStateJournalText("Nila Voss asked you to recover map cases from Smuggler Caverns. Return to Nila Voss when it is done.")
                .AddCollectItemObjective("qi_dantooine_018", 1)

                .AddState()
                .SetStateJournalText("Return to Nila Voss for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineRepublicAmmo()
        {
            _builder.Create("dan_republic_ammo", "Republic Ammo")
                .PrerequisiteQuest("dan_deserter_notes")
                .AddState()
                .SetStateJournalText("Sgt. Venn asked you to recover ammunition crates for Republic Garrison. Return to Sgt. Venn when it is done.")
                .AddCollectItemObjective("qi_dantooine_019", 1)

                .AddState()
                .SetStateJournalText("Return to Sgt. Venn for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineBolWarning()
        {
            _builder.Create("dan_bol_warning", "Thune Warning")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Hunter Oric asked you to cull Plains Thune and place warning markers in the ruined farmlands. Return to Hunter Oric when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_PlainsThune, 8)

                .AddState()
                .SetStateJournalText("Return to Hunter Oric for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineDantariCharms()
        {
            _builder.Create("dan_dantari_charms", "Dantari Charms")
                .PrerequisiteQuest("dan_dantari_rites")
                .AddState()
                .SetStateJournalText("Scout Harlan asked you to gather charms from Dantari forces. The trail points toward the Dantari fields. Return to Scout Harlan when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_DantariHunter, 6)
                .AddCollectItemObjective("qi_dantooine_020", 1)

                .AddState()
                .SetStateJournalText("Return to Scout Harlan for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void DantooineKinrathEggs()
        {
            _builder.Create("dan_kinrath_eggs", "Kinrath Eggs")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Hira Vos asked you to gather kinrath egg clusters. The trail points toward the Kinrath cave. Return to Hira Vos when it is done.")
                .AddKillObjective(NPCGroupType.Dantooine_Kinrath, 8)
                .AddCollectItemObjective("qi_dantooine_021", 1)

                .AddState()
                .SetStateJournalText("Return to Hira Vos for your reward.")

                .AddGoldReward(300)
                .AddXPReward(600);
        }

        private void DantooineLakeFishline()
        {
            _builder.Create("dan_lake_fishline", "Lake Fishline")
                .AddState()
                .SetStateJournalText("Fisher Rell asked you to repair fishlines around the lake. The trail points toward the lake caverns. Return to Fisher Rell when it is done.")

                .AddState()
                .SetStateJournalText("Return to Fisher Rell for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineWarehouseManifest()
        {
            _builder.Create("dan_warehouse_manifest", "Warehouse Manifest")
                .AddState()
                .SetStateJournalText("Clerk Mavo asked you to recover manifest pages in the Abandoned Warehouse. Return to Clerk Mavo when it is done.")
                .AddCollectItemObjective("qi_dantooine_022", 1)

                .AddState()
                .SetStateJournalText("Return to Clerk Mavo for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DantooineColonyCircuit()
        {
            _builder.Create("dan_colony_circuit", "Colony Circuit")
                .AddState()
                .SetStateJournalText("Technician Lira asked you to inspect colony utility circuits. The trail points toward the central colony. Return to Technician Lira when it is done.")

                .AddState()
                .SetStateJournalText("Return to Technician Lira for your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("dan_col_datapad", 1);
        }
    }
}
