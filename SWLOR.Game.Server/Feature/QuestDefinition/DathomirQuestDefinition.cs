using System.Collections.Generic;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class DathomirQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            DathomirLandingPerimeter();
            DathomirCzerkaBlackbox();
            DathomirShearMiteLine();
            DathomirBugGlands();
            DathomirTotemRecovery();
            DathomirShamanFetishes();
            DathomirGuardianChallenge();
            DathomirCaveInscriptions();
            DathomirPurboleHides();
            DathomirTurtleShells();
            DathomirDesertWaterstones();
            DathomirDesertPatrol();
            DathomirSsurianCull();
            DathomirSquellbugIchor();
            DathomirSprantalTeeth();
            DathomirMitePaste();
            DathomirRuinResidue();
            DathomirHiddenWebs();
            DathomirChirodactylWings();
            DathomirDarkAdeptSigns();
            DathomirRancorSpoor();
            DathomirWaterfallPlates();
            DathomirSupplyCaches();
            DathomirLockedCrates();
            DathomirJungleScouts();
            DathomirBossTrophies();
            DathomirLanguageStones();
            DathomirWeatherStation();
            DathomirCavePurboleCull();
            DathomirSardineSamples();
            DathomirCzerkaFieldNotes();
            DathomirLandingMedkits();
            DathomirTarnishedRoots();
            DathomirNorthJungleMarkers();
            DathomirDesertBonefield();
            DathomirWestDesertCompass();
            DathomirRuinBaseKeys();
            DathomirWaterfallEchoes();
            DathomirMountainAnchors();
            DathomirCaveRuinGuardians();
            DathomirGrottoLumens();
            DathomirHiddenSpiderEggs();
            DathomirTribalMasks();
            DathomirShamanAshes();
            DathomirSsurianBile();
            DathomirSprantalSpines();
            DathomirSquellbugChitin();
            DathomirChirodactylScreech();
            DathomirRancorBone();
            DathomirDarkAdeptRelic();
            DathomirFishingCamp();
            DathomirWeatheredTablets();
            return _builder.Build();
        }

        private void DathomirLandingPerimeter()
        {
            _builder.Create("dath_landing_perimeter", "Landing Perimeter")
                .AddState()
                .SetStateJournalText("Scout Nera asked you to clear swampland bugs near Jungle Landing. Return to Scout Nera when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_SwamplandBug, 6)

                .AddState()
                .SetStateJournalText("Return to Scout Nera for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirCzerkaBlackbox()
        {
            _builder.Create("dath_czerka_blackbox", "Czerka Black Box")
                .AddState()
                .SetStateJournalText("Agent Lohr asked you to recover a black box from the Czerka Base. Return to Agent Lohr when it is done.")
                .AddCollectItemObjective("qi_dathomir_001", 1)

                .AddState()
                .SetStateJournalText("Return to Agent Lohr for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirShearMiteLine()
        {
            _builder.Create("dath_shear_mite_line", "Shear Mite Line")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Voss Tarin asked you to deal with Shear Mites in Tarnished Jungles. Return to Voss Tarin when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_ShearMite, 8)

                .AddState()
                .SetStateJournalText("Return to Voss Tarin for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirBugGlands()
        {
            _builder.Create("dath_bug_glands", "Swampland Glands")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Dr. Pell Varo asked you to gather swampland bug glands. The trail points toward the jungle landing. Return to Dr. Pell Varo when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_SwamplandBug, 8)
                .AddCollectItemObjective("qi_dathomir_002", 1)

                .AddState()
                .SetStateJournalText("Return to Dr. Pell Varo for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirTotemRecovery()
        {
            _builder.Create("dath_totem_recovery", "Totem Recovery")
                .AddState()
                .SetStateJournalText("Kiva Noll asked you to recover totems from Kwi Tribal enemies. The trail points toward the tribal village. Return to Kiva Noll when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiTribal, 6)
                .AddCollectItemObjective("qi_dathomir_003", 1)

                .AddState()
                .SetStateJournalText("Return to Kiva Noll for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirShamanFetishes()
        {
            _builder.Create("dath_shaman_fetishes", "Shaman Fetishes")
                .AddState()
                .SetStateJournalText("Mara Senn asked you to gather fetishes from Kwi Shamans. The trail points toward the tribal village. Return to Mara Senn when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiShaman, 6)
                .AddCollectItemObjective("qi_dathomir_004", 1)

                .AddState()
                .SetStateJournalText("Return to Mara Senn for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirGuardianChallenge()
        {
            _builder.Create("dath_guardian_challenge", "Guardian Challenge")
                .AddState()
                .SetStateJournalText("Ulren Vos asked you to defeat Kwi Guardians in the Ruin Base. Return to Ulren Vos when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiGuardian, 6)

                .AddState()
                .SetStateJournalText("Return to Ulren Vos for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirCaveInscriptions()
        {
            _builder.Create("dath_cave_inscriptions", "Cave Inscriptions")
                .AddState()
                .SetStateJournalText("Scholar Anvi asked you to copy inscriptions in the Cave Ruins. Return to Scholar Anvi when it is done.")

                .AddState()
                .SetStateJournalText("Return to Scholar Anvi for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirPurboleHides()
        {
            _builder.Create("dath_purbole_hides", "Purbole Hides")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Tava Orell asked you to gather purbole hides. The trail points toward the red desert. Return to Tava Orell when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Purbole, 8)
                .AddCollectItemObjective("qi_dathomir_005", 1)

                .AddState()
                .SetStateJournalText("Return to Tava Orell for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirTurtleShells()
        {
            _builder.Create("dath_turtle_shells", "Dragon Turtle Shells")
                .AddState()
                .SetStateJournalText("Shellwright Vek asked you to gather dragon turtle shell fragments. The trail points toward the rancor grottos. Return to Shellwright Vek when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_DragonTurtle, 6)
                .AddCollectItemObjective("qi_dathomir_006", 1)

                .AddState()
                .SetStateJournalText("Return to Shellwright Vek for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirDesertWaterstones()
        {
            _builder.Create("dath_desert_waterstones", "Desert Waterstones")
                .AddState()
                .SetStateJournalText("Cera Pell asked you to gather waterstones in Desert West Side. The trail points toward the western desert. Return to Cera Pell when it is done.")
                .AddCollectItemObjective("qi_dathomir_007", 1)

                .AddState()
                .SetStateJournalText("Return to Cera Pell for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirDesertPatrol()
        {
            _builder.Create("dath_desert_patrol", "Desert Patrol")
                .AddState()
                .SetStateJournalText("Ranger Tov asked you to deal with Kwi patrols in the desert. The trail points toward the red desert. Return to Ranger Tov when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiTribal, 6)

                .AddState()
                .SetStateJournalText("Return to Ranger Tov for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirSsurianCull()
        {
            _builder.Create("dath_ssurian_cull", "Ssurian Cull")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Hunter Orla asked you to deal with Ssurians in Grotto Caverns. Return to Hunter Orla when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Ssurian, 8)

                .AddState()
                .SetStateJournalText("Return to Hunter Orla for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirSquellbugIchor()
        {
            _builder.Create("dath_squellbug_ichor", "Squellbug Ichor")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Chemist Navo asked you to gather squellbug ichor. The trail points toward the rancor grottos. Return to Chemist Navo when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Squellbug, 8)
                .AddCollectItemObjective("qi_dathomir_008", 1)

                .AddState()
                .SetStateJournalText("Return to Chemist Navo for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirSprantalTeeth()
        {
            _builder.Create("dath_sprantal_teeth", "Sprantal Teeth")
                .AddState()
                .SetStateJournalText("Vexa Lorn asked you to gather Sprantal teeth in the mountains. The trail points toward the mountain paths. Return to Vexa Lorn when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Sprantal, 6)
                .AddCollectItemObjective("qi_dathomir_009", 1)

                .AddState()
                .SetStateJournalText("Return to Vexa Lorn for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirMitePaste()
        {
            _builder.Create("dath_mite_paste", "Mite Paste")
                .AddState()
                .SetStateJournalText("Pel Ordo asked you to gather Shear Mite paste from Mountain Caves. Return to Pel Ordo when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_ShearMite, 6)
                .AddCollectItemObjective("qi_dathomir_010", 1)

                .AddState()
                .SetStateJournalText("Return to Pel Ordo for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirRuinResidue()
        {
            _builder.Create("dath_ruin_residue", "Ruin Residue")
                .AddState()
                .SetStateJournalText("Seer Hala asked you to survey Force residue in the Ruin Base. Return to Seer Hala when it is done.")

                .AddState()
                .SetStateJournalText("Return to Seer Hala for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirHiddenWebs()
        {
            _builder.Create("dath_hidden_webs", "Hidden Webs")
                .AddState()
                .SetStateJournalText("Caver Jann asked you to gather web sacs in the Hidden Cave. The trail points toward the hidden tunnels. Return to Caver Jann when it is done.")
                .AddCollectItemObjective("spider_guts", 3)

                .AddState()
                .SetStateJournalText("Return to Caver Jann for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirChirodactylWings()
        {
            _builder.Create("dath_chirodactyl_wings", "Chirodactyl Wings")
                .AddState()
                .SetStateJournalText("Avian Keeper Sol asked you to gather Chirodactyl wing membranes. The trail points toward the rancor grottos. Return to Avian Keeper Sol when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Chirodactyl, 6)
                .AddCollectItemObjective("qi_dathomir_011", 1)

                .AddState()
                .SetStateJournalText("Return to Avian Keeper Sol for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirDarkAdeptSigns()
        {
            _builder.Create("dath_dark_adept_signs", "Dark Adept Signs")
                .AddState()
                .SetStateJournalText("Watcher Pell asked you to defeat a Dark Adept and recover its signs. The trail points toward the rancor grottos. Return to Watcher Pell when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_DarkAdept, 1)
                .AddCollectItemObjective("qi_dathomir_012", 1)

                .AddState()
                .SetStateJournalText("Return to Watcher Pell for your reward.")

                .AddGoldReward(7200)
                .AddXPReward(8000);
        }

        private void DathomirRancorSpoor()
        {
            _builder.Create("dath_rancor_spoor", "Rancor Spoor")
                .AddState()
                .SetStateJournalText("Beastmaster Nesh asked you to gather rancor spoor samples. The trail points toward the rancor grottos. Return to Beastmaster Nesh when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Rancor, 1)
                .AddCollectItemObjective("qi_dathomir_013", 1)

                .AddState()
                .SetStateJournalText("Return to Beastmaster Nesh for your reward.")

                .AddGoldReward(7200)
                .AddXPReward(8000);
        }

        private void DathomirWaterfallPlates()
        {
            _builder.Create("dath_waterfall_plates", "Waterfall Plates")
                .AddState()
                .SetStateJournalText("Lira Sen asked you to recover stone plates from Waterfall Ruins. Return to Lira Sen when it is done.")
                .AddCollectItemObjective("qi_dathomir_014", 1)

                .AddState()
                .SetStateJournalText("Return to Lira Sen for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirSupplyCaches()
        {
            _builder.Create("dath_supply_caches", "Landing Caches")
                .AddState()
                .SetStateJournalText("Quartermaster Ren asked you to recover scattered supply caches. The trail points toward the jungle landing. Return to Quartermaster Ren when it is done.")
                .AddCollectItemObjective("qi_dathomir_015", 1)

                .AddState()
                .SetStateJournalText("Return to Quartermaster Ren for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirLockedCrates()
        {
            _builder.Create("dath_locked_crates", "Locked Crates")
                .AddState()
                .SetStateJournalText("Czerka Clerk Mav asked you to open and recover locked Czerka crates. The trail points toward the Czerka base. Return to Czerka Clerk Mav when it is done.")
                .AddCollectItemObjective("qi_dathomir_016", 1)

                .AddState()
                .SetStateJournalText("Return to Czerka Clerk Mav for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirJungleScouts()
        {
            _builder.Create("dath_jungle_scouts", "Jungle Scouts")
                .AddState()
                .SetStateJournalText("Scout Brinna asked you to recover signs from lost scouts in Tarnished Jungles North. The trail points toward the northern jungle. Return to Scout Brinna when it is done.")
                .AddCollectItemObjective("qi_dathomir_017", 1)

                .AddState()
                .SetStateJournalText("Return to Scout Brinna for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirBossTrophies()
        {
            _builder.Create("dath_boss_trophies", "Trophies of the Grottos")
                .AddState()
                .SetStateJournalText("Talia Voss asked you to gather trophies from high-danger grotto enemies. The trail points toward the rancor grottos. Return to Talia Voss when it is done.")
                .AddCollectItemObjective("qi_dathomir_018", 1)

                .AddState()
                .SetStateJournalText("Return to Talia Voss for your reward.")

                .AddGoldReward(7200)
                .AddXPReward(8000)
                .AddItemReward("dath_boss_fang", 1);
        }

        private void DathomirLanguageStones()
        {
            _builder.Create("dath_language_stones", "Language Stones")
                .AddState()
                .SetStateJournalText("Elder Sava asked you to gather carved language stones. The trail points toward the tribal village. Return to Elder Sava when it is done.")
                .AddCollectItemObjective("qi_dathomir_019", 1)

                .AddState()
                .SetStateJournalText("Return to Elder Sava for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirWeatherStation()
        {
            _builder.Create("dath_weather_station", "Weather Station")
                .AddState()
                .SetStateJournalText("Tech Iren asked you to repair a weather station in the mountains. The trail points toward the mountain paths. Return to Tech Iren when it is done.")

                .AddState()
                .SetStateJournalText("Return to Tech Iren for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirCavePurboleCull()
        {
            _builder.Create("dath_cave_purbole_cull", "Cave Purbole Cull")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Daro Kess asked you to deal with Purbole near the Cave Ruins. Return to Daro Kess when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Purbole, 8)

                .AddState()
                .SetStateJournalText("Return to Daro Kess for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirSardineSamples()
        {
            _builder.Create("dath_sardine_samples", "Sardine Samples")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Fisher Rell asked you to gather Dathomir Sardine samples. The trail points toward the jungle landing. Return to Fisher Rell when it is done.")
                .AddCollectItemObjective("dath_sardine", 5)

                .AddState()
                .SetStateJournalText("Return to Fisher Rell for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirCzerkaFieldNotes()
        {
            _builder.Create("dath_czerka_field_notes", "Czerka Field Notes")
                .PrerequisiteQuest("dath_czerka_blackbox")
                .AddState()
                .SetStateJournalText("Agent Lohr asked you to recover scattered Czerka field notes. The trail points toward the Czerka base. Return to Agent Lohr when it is done.")
                .AddCollectItemObjective("qi_dathomir_020", 1)

                .AddState()
                .SetStateJournalText("Return to Agent Lohr for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirLandingMedkits()
        {
            _builder.Create("dath_landing_medkits", "Landing Medkits")
                .AddState()
                .SetStateJournalText("Medic Sera asked you to recover medkits lost around Jungle Landing. Return to Medic Sera when it is done.")
                .AddCollectItemObjective("qi_dathomir_021", 1)

                .AddState()
                .SetStateJournalText("Return to Medic Sera for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirTarnishedRoots()
        {
            _builder.Create("dath_tarnished_roots", "Tarnished Roots")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Botanist Heth asked you to gather root samples from Tarnished Jungles. Return to Botanist Heth when it is done.")
                .AddCollectItemObjective("qi_dathomir_022", 1)

                .AddState()
                .SetStateJournalText("Return to Botanist Heth for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirNorthJungleMarkers()
        {
            _builder.Create("dath_north_jungle_markers", "North Jungle Markers")
                .PrerequisiteQuest("dath_jungle_scouts")
                .AddState()
                .SetStateJournalText("Scout Brinna asked you to place trail markers in Tarnished Jungles North. The trail points toward the northern jungle. Return to Scout Brinna when it is done.")

                .AddState()
                .SetStateJournalText("Return to Scout Brinna for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirDesertBonefield()
        {
            _builder.Create("dath_desert_bonefield", "Desert Bonefield")
                .AddState()
                .SetStateJournalText("Archivist Orla asked you to catalog remains in the Dathomir Desert. The trail points toward the red desert. Return to Archivist Orla when it is done.")

                .AddState()
                .SetStateJournalText("Return to Archivist Orla for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirWestDesertCompass()
        {
            _builder.Create("dath_west_desert_compass", "West Desert Compass")
                .PrerequisiteQuest("dath_desert_patrol")
                .AddState()
                .SetStateJournalText("Ranger Tov asked you to recover compass stones from Desert West Side. The trail points toward the western desert. Return to Ranger Tov when it is done.")
                .AddCollectItemObjective("qi_dathomir_023", 1)

                .AddState()
                .SetStateJournalText("Return to Ranger Tov for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirRuinBaseKeys()
        {
            _builder.Create("dath_ruin_base_keys", "Ruin Base Keys")
                .PrerequisiteQuest("dath_ruin_residue")
                .AddState()
                .SetStateJournalText("Seer Hala asked you to recover ancient key fragments in Ruin Base. Return to Seer Hala when it is done.")
                .AddCollectItemObjective("qi_dathomir_024", 1)

                .AddState()
                .SetStateJournalText("Return to Seer Hala for your reward.")

                .AddGoldReward(7200)
                .AddXPReward(8000);
        }

        private void DathomirWaterfallEchoes()
        {
            _builder.Create("dath_waterfall_echoes", "Waterfall Echoes")
                .PrerequisiteQuest("dath_waterfall_plates")
                .AddState()
                .SetStateJournalText("Lira Sen asked you to place echo chimes in Waterfall Ruins. Return to Lira Sen when it is done.")

                .AddState()
                .SetStateJournalText("Return to Lira Sen for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirMountainAnchors()
        {
            _builder.Create("dath_mountain_anchors", "Mountain Anchors")
                .PrerequisiteQuest("dath_weather_station")
                .AddState()
                .SetStateJournalText("Tech Iren asked you to set climbing anchors across the mountains. The trail points toward the mountain paths. Return to Tech Iren when it is done.")

                .AddState()
                .SetStateJournalText("Return to Tech Iren for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirCaveRuinGuardians()
        {
            _builder.Create("dath_cave_ruin_guardians", "Cave Ruin Guardians")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Scholar Anvi asked you to defeat Kwi Guardians near the Cave Ruins. Return to Scholar Anvi when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiGuardian, 8)

                .AddState()
                .SetStateJournalText("Return to Scholar Anvi for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirGrottoLumens()
        {
            _builder.Create("dath_grotto_lumens", "Grotto Lumens")
                .AddState()
                .SetStateJournalText("Chemist Navo asked you to gather luminous fungi from the Grottos. The trail points toward the rancor grottos. Return to Chemist Navo when it is done.")
                .AddCollectItemObjective("qi_dathomir_025", 1)

                .AddState()
                .SetStateJournalText("Return to Chemist Navo for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirHiddenSpiderEggs()
        {
            _builder.Create("dath_hidden_spider_eggs", "Hidden Spider Eggs")
                .PrerequisiteQuest("dath_hidden_webs")
                .AddState()
                .SetStateJournalText("Caver Jann asked you to gather spider egg sacs in the Hidden Cave. The trail points toward the hidden tunnels. Return to Caver Jann when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_GapingSpider, 6)
                .AddCollectItemObjective("spider_guts", 3)

                .AddState()
                .SetStateJournalText("Return to Caver Jann for your reward.")

                .AddGoldReward(5500)
                .AddXPReward(5000);
        }

        private void DathomirTribalMasks()
        {
            _builder.Create("dath_tribal_masks", "Tribal Masks")
                .PrerequisiteQuest("dath_totem_recovery")
                .AddState()
                .SetStateJournalText("Kiva Noll asked you to recover ceremonial masks from the Tribe Village. The trail points toward the tribal village. Return to Kiva Noll when it is done.")
                .AddCollectItemObjective("qi_dathomir_026", 1)

                .AddState()
                .SetStateJournalText("Return to Kiva Noll for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirShamanAshes()
        {
            _builder.Create("dath_shaman_ashes", "Shaman Ashes")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Mara Senn asked you to gather ritual ash from Kwi Shamans. The trail points toward the tribal village. Return to Mara Senn when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiShaman, 8)
                .AddCollectItemObjective("qi_dathomir_027", 1)

                .AddState()
                .SetStateJournalText("Return to Mara Senn for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirSsurianBile()
        {
            _builder.Create("dath_ssurian_bile", "Ssurian Bile")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Hunter Orla asked you to gather Ssurian bile samples. The trail points toward the grotto caverns. Return to Hunter Orla when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Ssurian, 8)
                .AddCollectItemObjective("qi_dathomir_028", 1)

                .AddState()
                .SetStateJournalText("Return to Hunter Orla for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirSprantalSpines()
        {
            _builder.Create("dath_sprantal_spines", "Sprantal Spines")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Vexa Lorn asked you to gather Sprantal spine clusters. The trail points toward the mountain paths. Return to Vexa Lorn when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Sprantal, 8)
                .AddCollectItemObjective("qi_dathomir_029", 1)

                .AddState()
                .SetStateJournalText("Return to Vexa Lorn for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirSquellbugChitin()
        {
            _builder.Create("dath_squellbug_chitin", "Squellbug Chitin")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Pel Ordo asked you to gather squellbug chitin plates. The trail points toward the rancor grottos. Return to Pel Ordo when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Squellbug, 8)
                .AddCollectItemObjective("wild_leg", 5)

                .AddState()
                .SetStateJournalText("Return to Pel Ordo for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirChirodactylScreech()
        {
            _builder.Create("dath_chirodactyl_screech", "Chirodactyl Screech")
                .PrerequisiteQuest("dath_chirodactyl_wings")
                .AddState()
                .SetStateJournalText("Avian Keeper Sol asked you to deploy sound recorders near Chirodactyl roosts. The trail points toward the rancor grottos. Return to Avian Keeper Sol when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Chirodactyl, 6)

                .AddState()
                .SetStateJournalText("Return to Avian Keeper Sol for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2500);
        }

        private void DathomirRancorBone()
        {
            _builder.Create("dath_rancor_bone", "Rancor Bone")
                .PrerequisiteQuest("dath_rancor_spoor")
                .AddState()
                .SetStateJournalText("Beastmaster Nesh asked you to recover a rancor bone from the Grottos. The trail points toward the rancor grottos. Return to Beastmaster Nesh when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_Rancor, 1)
                .AddCollectItemObjective("qi_dathomir_030", 1)

                .AddState()
                .SetStateJournalText("Return to Beastmaster Nesh for your reward.")

                .AddGoldReward(7200)
                .AddXPReward(8000);
        }

        private void DathomirDarkAdeptRelic()
        {
            _builder.Create("dath_dark_adept_relic", "Dark Adept Relic")
                .PrerequisiteQuest("dath_dark_adept_signs")
                .AddState()
                .SetStateJournalText("Watcher Pell asked you to defeat a Dark Adept and recover a relic. The trail points toward the rancor grottos. Return to Watcher Pell when it is done.")
                .AddKillObjective(NPCGroupType.Dathomir_DarkAdept, 1)
                .AddCollectItemObjective("qi_dathomir_031", 1)

                .AddState()
                .SetStateJournalText("Return to Watcher Pell for your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("dath_adept_rel", 1);
        }

        private void DathomirFishingCamp()
        {
            _builder.Create("dath_fishing_camp", "Fishing Camp")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Fisher Rell asked you to recover supplies from Dathomir fishing camps. The trail points toward the jungle landing. Return to Fisher Rell when it is done.")
                .AddCollectItemObjective("qi_dathomir_032", 1)

                .AddState()
                .SetStateJournalText("Return to Fisher Rell for your reward.")

                .AddGoldReward(2500)
                .AddXPReward(2000);
        }

        private void DathomirWeatheredTablets()
        {
            _builder.Create("dath_weathered_tablets", "Weathered Tablets")
                .PrerequisiteQuest("dath_language_stones")
                .AddState()
                .SetStateJournalText("Elder Sava asked you to recover weathered stone tablets from Waterfall Ruins. Return to Elder Sava when it is done.")
                .AddCollectItemObjective("qi_dathomir_033", 1)

                .AddState()
                .SetStateJournalText("Return to Elder Sava for your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("dath_tab_frag", 1);
        }
    }
}
