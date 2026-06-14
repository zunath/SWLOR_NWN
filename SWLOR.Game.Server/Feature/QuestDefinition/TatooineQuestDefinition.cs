using System.Collections.Generic;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class TatooineQuestDefinition : IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder();
            WorkinForTheMan(builder);
            StinkyWomprats(builder);
            TuskenRampage(builder);
            TatooineDockingManifest(builder);
            TatooineWaterDebt(builder);
            TatooineDroidCoolant(builder);
            TatooineWompratCellar(builder);
            TatooineSandswimmerSightings(builder);
            TatooineBeetlePlates(builder);
            TatooineSandDemonMarks(builder);
            TatooineBoundaryRaiders(builder);
            TatooineTentMap(builder);
            TatooineCaveScouts(builder);
            TatooineKraytListening(builder);
            TatooineSarlaccTeeth(builder);
            TatooineWormVibrations(builder);
            TatooineJawaRepair(builder);
            TatooineBazaarLedgers(builder);
            TatooineGocorpProbe(builder);
            TatooineMineClaims(builder);
            TatooineSignalMirrors(builder);
            TatooineMoistureValves(builder);
            TatooineTocheeParcels(builder);
            TatooineMoseisleyBeacons(builder);
            TatooineElevagiiSeed(builder);
            TatooineRancorSpoor(builder);
            TatooinePalaceLedger(builder);
            TatooineBountyMarks(builder);
            TatooineMotivatorRun(builder);
            TatooineMedcenterDelivery(builder);
            TatooineSouthernCaravan(builder);
            TatooineSarlaccMucus(builder);
            TatooineAncientHusk(builder);
            TatooineChasmMarkers(builder);
            TatooineNorthernDuneBones(builder);
            TatooineFlatlandCompass(builder);
            TatooineTuskenEliteOrders(builder);
            TatooineSandWormCastings(builder);
            TatooineAstroportStowaways(builder);
            TatooineJunixTabs(builder);
            TatooineDuneWeatherVanes(builder);
            TatooineCantinaDebtbook(builder);
            TatooineJawaPowerCore(builder);
            TatooineSmeskWatchlist(builder);
            TatooineSouthpassSigns(builder);
            TatooineRockyPassRaiders(builder);
            TatooineAncientWormTooth(builder);
            TatooineDroidTuneup(builder);
            TatooineMedicSaline(builder);
            TatooineSarlaccStings(builder);
            TatooineMoseisleySignals(builder);
            TatooineBeetlePlateOrder(builder);
            return builder.Build();
        }

        private static void WorkinForTheMan(QuestBuilder builder)
        {
            builder.Create("workin_for_man", "Workin' for the Man")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("You've been recruited by Czerka to take care of their Tusken problem. Explore the dunes of Tatooine and thin their numbers.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 20)

                .AddState()
                .SetStateJournalText("Report back to the dockmaster.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void StinkyWomprats(QuestBuilder builder)
        {
            builder.Create("stinky_womprats", "Stinky Womprats")
                .AddState()
                .SetStateJournalText("You've agreed to take care of those pesky, stinky, womprats. Slay them and return 10 hides to Haderach in Anchorhead.")
                .AddCollectItemObjective("womprathide", 10)

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TuskenRampage(QuestBuilder builder)
        {
            builder.Create("tusken_rampage", "Tusken Rampage")
                .AddState()
                .SetStateJournalText("The Militia wants you to kill one hundred and fifty Tusken Raiders.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 150)

                .AddState()
                .SetStateJournalText("That's all those Tuskens taken care of. Go talk to that man!")

                .AddGoldReward(11250)
                .AddXPReward(7500)
                .AddItemReward("recipe_hazrdwall", 1);
        }

        private static void TatooineDockingManifest(QuestBuilder builder)
        {
            builder.Create("tat_docking_manifest", "Docking Manifest")
                .AddState()
                .SetStateJournalText("Dockhand Ral asked you to recover missing docking manifests in Anchorhead. The trail points toward Anchorhead astroport. Return to Dockhand Ral when it is done.")
                .AddCollectItemObjective("qi_tatooine_001", 1)

                .AddState()
                .SetStateJournalText("Return to Dockhand Ral for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineWaterDebt(QuestBuilder builder)
        {
            builder.Create("tat_water_debt", "Water Debt")
                .AddState()
                .SetStateJournalText("Vessa Marr asked you to gather water chits from Anchorhead residents. The trail points toward the Anchorhead cantina. Return to Vessa Marr when it is done.")
                .AddCollectItemObjective("qi_tatooine_002", 1)

                .AddState()
                .SetStateJournalText("Return to Vessa Marr for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineDroidCoolant(QuestBuilder builder)
        {
            builder.Create("tat_droid_coolant", "Droid Coolant")
                .AddState()
                .SetStateJournalText("HX-44 asked you to gather coolant canisters for the droid shop. The trail points toward the Anchorhead droid shop. Return to HX-44 when it is done.")
                .AddCollectItemObjective("qi_tatooine_003", 1)

                .AddState()
                .SetStateJournalText("Return to HX-44 for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineWompratCellar(QuestBuilder builder)
        {
            builder.Create("tat_womprat_cellar", "Womprat Cellar")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Hader Gelt asked you to gather womprat hides from nearby tunnels. The trail points toward the southern district. Return to Hader Gelt when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_Womprat, 8)
                .AddCollectItemObjective("womprathide", 5)

                .AddState()
                .SetStateJournalText("Return to Hader Gelt for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineSandswimmerSightings(QuestBuilder builder)
        {
            builder.Create("tat_sandswimmer_sightings", "Sandswimmer Sightings")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Kel Dravos asked you to deal with sandswimmers in the dunes. The trail points toward the northern district. Return to Kel Dravos when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_Sandswimmer, 8)

                .AddState()
                .SetStateJournalText("Return to Kel Dravos for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineBeetlePlates(QuestBuilder builder)
        {
            builder.Create("tat_beetle_plates", "Beetle Plates")
                .AddState()
                .SetStateJournalText("Mera Vepp asked you to gather sand beetle plates. The trail points toward Verpex Bazaar. Return to Mera Vepp when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_SandBeetle, 6)
                .AddCollectItemObjective("qi_tatooine_004", 1)

                .AddState()
                .SetStateJournalText("Return to Mera Vepp for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineSandDemonMarks(QuestBuilder builder)
        {
            builder.Create("tat_sand_demon_marks", "Sand Demon Marks")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Orlo Pehn asked you to deal with sand demons and recover marked stones. The trail points toward the Anchorhead cantina. Return to Orlo Pehn when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_SandDemon, 8)
                .AddCollectItemObjective("sand_demon_leg", 5)

                .AddState()
                .SetStateJournalText("Return to Orlo Pehn for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineBoundaryRaiders(QuestBuilder builder)
        {
            builder.Create("tat_boundary_raiders", "Boundary Raiders")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Lt. Brask asked you to deal with Tusken Raiders near the boundary. The trail points toward the northern district. Return to Lt. Brask when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 8)

                .AddState()
                .SetStateJournalText("Return to Lt. Brask for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineTentMap(QuestBuilder builder)
        {
            builder.Create("tat_tent_map", "The Tent Map")
                .AddState()
                .SetStateJournalText("Sena Vor asked you to recover a map from the Tusken Raider Tent. The trail points toward the Tusken tents. Return to Sena Vor when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 6)
                .AddCollectItemObjective("qi_tatooine_005", 1)

                .AddState()
                .SetStateJournalText("Return to Sena Vor for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineCaveScouts(QuestBuilder builder)
        {
            builder.Create("tat_cave_scouts", "Cave Scouts")
                .AddState()
                .SetStateJournalText("Renn Var asked you to clear Tusken scouts from the cave main floor. The trail points toward the Tusken cave. Return to Renn Var when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 6)

                .AddState()
                .SetStateJournalText("Return to Renn Var for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineKraytListening(QuestBuilder builder)
        {
            builder.Create("tat_krayt_listening", "Krayt Listening Post")
                .AddState()
                .SetStateJournalText("Davin Orel asked you to place listening devices in the rocky desert. Return to Davin Orel when it is done.")

                .AddState()
                .SetStateJournalText("Return to Davin Orel for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineSarlaccTeeth(QuestBuilder builder)
        {
            builder.Create("tat_sarlacc_teeth", "Sarlacc Teeth")
                .AddState()
                .SetStateJournalText("Greevo Nask asked you to gather baby sarlacc teeth. The trail points toward the baby sarlacc cave. Return to Greevo Nask when it is done.")
                .AddCollectItemObjective("qi_tatooine_006", 1)

                .AddState()
                .SetStateJournalText("Return to Greevo Nask for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineWormVibrations(QuestBuilder builder)
        {
            builder.Create("tat_worm_vibrations", "Worm Vibrations")
                .AddState()
                .SetStateJournalText("Prof. Hal Marr asked you to calibrate vibration stakes in the Worm Den. Return to Prof. Hal Marr when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_SandWorm, 6)

                .AddState()
                .SetStateJournalText("Return to Prof. Hal Marr for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(4000);
        }

        private static void TatooineJawaRepair(QuestBuilder builder)
        {
            builder.Create("tat_jawa_repair", "Broken Jawa Machine")
                .AddState()
                .SetStateJournalText("Jawa Foreman Jik asked you to recover droid parts to repair the machine. The trail points toward the broken Jawa camp. Return to Jawa Foreman Jik when it is done.")
                .AddCollectItemObjective("qi_tatooine_007", 1)

                .AddState()
                .SetStateJournalText("Return to Jawa Foreman Jik for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineBazaarLedgers(QuestBuilder builder)
        {
            builder.Create("tat_bazaar_ledgers", "Bazaar Ledgers")
                .AddState()
                .SetStateJournalText("Pera Konn asked you to recover misplaced Verpex Bazaar ledgers. Return to Pera Konn when it is done.")
                .AddCollectItemObjective("qi_tatooine_008", 1)

                .AddState()
                .SetStateJournalText("Return to Pera Konn for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineGocorpProbe(QuestBuilder builder)
        {
            builder.Create("tat_gocorp_probe", "Go-Corp Probe")
                .AddState()
                .SetStateJournalText("Lonn Secura asked you to deploy probe hardware near Go-Corp Station. Return to Lonn Secura when it is done.")

                .AddState()
                .SetStateJournalText("Return to Lonn Secura for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineMineClaims(QuestBuilder builder)
        {
            builder.Create("tat_mine_claims", "Mine Claims")
                .AddState()
                .SetStateJournalText("Hask Bren asked you to mark claim stakes along North Mine Cliffs. Return to Hask Bren when it is done.")

                .AddState()
                .SetStateJournalText("Return to Hask Bren for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineSignalMirrors(QuestBuilder builder)
        {
            builder.Create("tat_signal_mirrors", "Signal Mirrors")
                .AddState()
                .SetStateJournalText("Miri Voss asked you to align signal mirrors through Rocky Pass. Return to Miri Voss when it is done.")

                .AddState()
                .SetStateJournalText("Return to Miri Voss for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineMoistureValves(QuestBuilder builder)
        {
            builder.Create("tat_moisture_valves", "Moisture Valves")
                .AddState()
                .SetStateJournalText("Ola Dav asked you to repair moisture valves in the Hilly Desert. Return to Ola Dav when it is done.")

                .AddState()
                .SetStateJournalText("Return to Ola Dav for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineTocheeParcels(QuestBuilder builder)
        {
            builder.Create("tat_tochee_parcels", "Tochee Parcels")
                .AddState()
                .SetStateJournalText("Daro Pell asked you to deliver parcels from Anchorhead to Tochee. The trail points toward Tochee Station. Return to Daro Pell when it is done.")

                .AddState()
                .SetStateJournalText("Return to Daro Pell for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineMoseisleyBeacons(QuestBuilder builder)
        {
            builder.Create("tat_moseisley_beacons", "Road to Mos Eisley")
                .AddState()
                .SetStateJournalText("Captain Set asked you to activate road beacons toward Mos Eisley. The trail points toward the road to Mos Eisley. Return to Captain Set when it is done.")

                .AddState()
                .SetStateJournalText("Return to Captain Set for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineElevagiiSeed(QuestBuilder builder)
        {
            builder.Create("tat_elevagii_seed", "Elevagii Seed Run")
                .AddState()
                .SetStateJournalText("Farmer Keth asked you to recover seed crates from the dunes. The trail points toward Elevagii Farm. Return to Farmer Keth when it is done.")
                .AddCollectItemObjective("qi_tatooine_009", 1)

                .AddState()
                .SetStateJournalText("Return to Farmer Keth for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineRancorSpoor(QuestBuilder builder)
        {
            builder.Create("tat_rancor_spoor", "Rancor Spoor")
                .AddState()
                .SetStateJournalText("Hunter Jass asked you to gather spoor samples from the Rancor Cave. Return to Hunter Jass when it is done.")
                .AddCollectItemObjective("qi_tatooine_010", 1)

                .AddState()
                .SetStateJournalText("Return to Hunter Jass for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(4000)
                .AddItemReward("tat_rancor_sp", 1);
        }

        private static void TatooinePalaceLedger(QuestBuilder builder)
        {
            builder.Create("tat_palace_ledger", "Palace Ledger")
                .AddState()
                .SetStateJournalText("Salli Qor asked you to recover a spice ledger near Smesk's Palace. Return to Salli Qor when it is done.")
                .AddCollectItemObjective("qi_tatooine_011", 1)

                .AddState()
                .SetStateJournalText("Return to Salli Qor for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineBountyMarks(QuestBuilder builder)
        {
            builder.Create("tat_bounty_marks", "Bounty Marks")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Militia Clerk Edro asked you to gather bounty marks from Tusken Raiders. The trail points toward the northern district. Return to Militia Clerk Edro when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 8)
                .AddCollectItemObjective("qi_tatooine_012", 1)

                .AddState()
                .SetStateJournalText("Return to Militia Clerk Edro for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineMotivatorRun(QuestBuilder builder)
        {
            builder.Create("tat_motivator_run", "Motivator Run")
                .AddState()
                .SetStateJournalText("D4-KL asked you to gather droid motivators from desert wreckage. The trail points toward the Anchorhead droid shop. Return to D4-KL when it is done.")
                .AddCollectItemObjective("qi_tatooine_013", 1)

                .AddState()
                .SetStateJournalText("Return to D4-KL for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineMedcenterDelivery(QuestBuilder builder)
        {
            builder.Create("tat_medcenter_delivery", "Medcenter Delivery")
                .AddState()
                .SetStateJournalText("Dr. Saal asked you to deliver emergency supplies across Anchorhead. The trail points toward Anchorhead medical. Return to Dr. Saal when it is done.")

                .AddState()
                .SetStateJournalText("Return to Dr. Saal for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineSouthernCaravan(QuestBuilder builder)
        {
            builder.Create("tat_southern_caravan", "Southern Caravan")
                .AddState()
                .SetStateJournalText("Orrin Bel asked you to mark caravan stones through Southern Pass. The trail points toward the southern entrance. Return to Orrin Bel when it is done.")

                .AddState()
                .SetStateJournalText("Return to Orrin Bel for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineSarlaccMucus(QuestBuilder builder)
        {
            builder.Create("tat_sarlacc_mucus", "Sarlacc Mucus")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Bovo Greel asked you to gather baby sarlacc mucus. The trail points toward the baby sarlacc cave. Return to Bovo Greel when it is done.")
                .AddCollectItemObjective("qi_tatooine_014", 1)

                .AddState()
                .SetStateJournalText("Return to Bovo Greel for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineAncientHusk(QuestBuilder builder)
        {
            builder.Create("tat_ancient_husk", "Ancient Husk")
                .AddState()
                .SetStateJournalText("Old Varin asked you to recover ancient worm husk fragments. The trail points toward the worm den. Return to Old Varin when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_AncientSandWorm, 1)
                .AddCollectItemObjective("qi_tatooine_015", 1)

                .AddState()
                .SetStateJournalText("Return to Old Varin for your reward.")

                .AddGoldReward(11250)
                .AddXPReward(7500)
                .AddItemReward("tat_husk_core", 1);
        }

        private static void TatooineChasmMarkers(QuestBuilder builder)
        {
            builder.Create("tat_chasm_markers", "Chasm Markers")
                .AddState()
                .SetStateJournalText("Surveyor Tekk asked you to place survey markers through Chasm Pass. Return to Surveyor Tekk when it is done.")

                .AddState()
                .SetStateJournalText("Return to Surveyor Tekk for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineNorthernDuneBones(QuestBuilder builder)
        {
            builder.Create("tat_northern_dune_bones", "Northern Dune Bones")
                .AddState()
                .SetStateJournalText("Bonepicker Jass asked you to recover bleached bones from Northern Dunes. Return to Bonepicker Jass when it is done.")
                .AddCollectItemObjective("qi_tatooine_016", 1)

                .AddState()
                .SetStateJournalText("Return to Bonepicker Jass for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineFlatlandCompass(QuestBuilder builder)
        {
            builder.Create("tat_flatland_compass", "Flatland Compass")
                .AddState()
                .SetStateJournalText("Scout Pava asked you to recover compass parts from the Flatlands. Return to Scout Pava when it is done.")
                .AddCollectItemObjective("qi_tatooine_017", 1)

                .AddState()
                .SetStateJournalText("Return to Scout Pava for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineTuskenEliteOrders(QuestBuilder builder)
        {
            builder.Create("tat_tusken_elite_orders", "Tusken Elite Orders")
                .AddState()
                .SetStateJournalText("Militia Captain Vos asked you to defeat Tusken Elite and recover orders. The trail points toward the northern district. Return to Militia Captain Vos when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenElite, 6)
                .AddCollectItemObjective("qi_tatooine_018", 1)

                .AddState()
                .SetStateJournalText("Return to Militia Captain Vos for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(4000)
                .AddItemReward("tat_tusk_blade", 1);
        }

        private static void TatooineSandWormCastings(QuestBuilder builder)
        {
            builder.Create("tat_sand_worm_castings", "Sand Worm Castings")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Prof. Ulren asked you to gather sand worm castings from the Worm Den. Return to Prof. Ulren when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_SandWorm, 8)
                .AddCollectItemObjective("sandwormtooth", 5)

                .AddState()
                .SetStateJournalText("Return to Prof. Ulren for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineAstroportStowaways(QuestBuilder builder)
        {
            builder.Create("tat_astroport_stowaways", "Astroport Stowaways")
                .AddState()
                .SetStateJournalText("Dockmaster Venn asked you to find stowaway caches in Anchorhead Astroport. Return to Dockmaster Venn when it is done.")

                .AddState()
                .SetStateJournalText("Return to Dockmaster Venn for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineJunixTabs(QuestBuilder builder)
        {
            builder.Create("tat_junix_tabs", "Junix's Tabs")
                .AddState()
                .SetStateJournalText("Junix Clerk Bera asked you to gather overdue tabs from Anchorhead patrons. The trail points toward Junix's place. Return to Junix Clerk Bera when it is done.")
                .AddCollectItemObjective("qi_tatooine_019", 1)

                .AddState()
                .SetStateJournalText("Return to Junix Clerk Bera for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineDuneWeatherVanes(QuestBuilder builder)
        {
            builder.Create("tat_dune_weather_vanes", "Dune Weather Vanes")
                .AddState()
                .SetStateJournalText("Weatherhand Lor asked you to repair weather vanes in the Arid Hilly Desert. The trail points toward the arid hills. Return to Weatherhand Lor when it is done.")

                .AddState()
                .SetStateJournalText("Return to Weatherhand Lor for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineCantinaDebtbook(QuestBuilder builder)
        {
            builder.Create("tat_cantina_debtbook", "Cantina Debtbook")
                .AddState()
                .SetStateJournalText("Bartender Ree asked you to recover a stolen debtbook from local thieves. The trail points toward the Anchorhead cantina. Return to Bartender Ree when it is done.")
                .AddCollectItemObjective("qi_tatooine_020", 1)

                .AddState()
                .SetStateJournalText("Return to Bartender Ree for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineJawaPowerCore(QuestBuilder builder)
        {
            builder.Create("tat_jawa_power_core", "Jawa Power Core")
                .AddState()
                .SetStateJournalText("Jawa Tech Neb asked you to recover a replacement power core from desert scrap. The trail points toward the broken Jawa camp. Return to Jawa Tech Neb when it is done.")
                .AddCollectItemObjective("qi_tatooine_021", 1)

                .AddState()
                .SetStateJournalText("Return to Jawa Tech Neb for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineSmeskWatchlist(QuestBuilder builder)
        {
            builder.Create("tat_smesk_watchlist", "Smesk Watchlist")
                .PrerequisiteQuest("tat_palace_ledger")
                .AddState()
                .SetStateJournalText("Salli Qor asked you to recover names from palace informants. The trail points toward Smesk's palace. Return to Salli Qor when it is done.")
                .AddCollectItemObjective("qi_tatooine_022", 1)

                .AddState()
                .SetStateJournalText("Return to Salli Qor for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineSouthpassSigns(QuestBuilder builder)
        {
            builder.Create("tat_southpass_signs", "Southern Pass Signs")
                .AddState()
                .SetStateJournalText("Road Warden Mell asked you to repair route signs through Southern Pass. Return to Road Warden Mell when it is done.")

                .AddState()
                .SetStateJournalText("Return to Road Warden Mell for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1000);
        }

        private static void TatooineRockyPassRaiders(QuestBuilder builder)
        {
            builder.Create("tat_rocky_pass_raiders", "Rocky Pass Raiders")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Hunter Doma asked you to clear Tusken Raiders from Rocky Pass. The trail points toward the rocky passage. Return to Hunter Doma when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 8)

                .AddState()
                .SetStateJournalText("Return to Hunter Doma for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineAncientWormTooth(QuestBuilder builder)
        {
            builder.Create("tat_ancient_worm_tooth", "Ancient Worm Tooth")
                .PrerequisiteQuest("tat_ancient_husk")
                .AddState()
                .SetStateJournalText("Old Varin asked you to recover a tooth from an ancient sand worm. The trail points toward the worm den. Return to Old Varin when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_AncientSandWorm, 1)
                .AddCollectItemObjective("qi_tatooine_023", 1)

                .AddState()
                .SetStateJournalText("Return to Old Varin for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(4000);
        }

        private static void TatooineDroidTuneup(QuestBuilder builder)
        {
            builder.Create("tat_droid_tuneup", "Droid Tune-Up")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("HX-44 asked you to gather tune-up parts from desert wreckage. The trail points toward the Anchorhead droid shop. Return to HX-44 when it is done.")
                .AddCollectItemObjective("qi_tatooine_024", 1)

                .AddState()
                .SetStateJournalText("Return to HX-44 for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void TatooineMedicSaline(QuestBuilder builder)
        {
            builder.Create("tat_medic_saline", "Saline Shortage")
                .PrerequisiteQuest("tat_medcenter_delivery")
                .AddState()
                .SetStateJournalText("Dr. Saal asked you to recover saline packs from caravan debris. The trail points toward Anchorhead medical. Return to Dr. Saal when it is done.")
                .AddCollectItemObjective("qi_tatooine_025", 1)

                .AddState()
                .SetStateJournalText("Return to Dr. Saal for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineSarlaccStings(QuestBuilder builder)
        {
            builder.Create("tat_sarlacc_stings", "Sarlacc Stings")
                .PrerequisiteQuest("tat_sarlacc_teeth")
                .AddState()
                .SetStateJournalText("Greevo Nask asked you to gather stinging barbs from the Baby Sarlacc Cave. Return to Greevo Nask when it is done.")
                .AddCollectItemObjective("qi_tatooine_026", 1)

                .AddState()
                .SetStateJournalText("Return to Greevo Nask for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(1750);
        }

        private static void TatooineMoseisleySignals(QuestBuilder builder)
        {
            builder.Create("tat_moseisley_signals", "Mos Eisley Signals")
                .PrerequisiteQuest("tat_moseisley_beacons")
                .AddState()
                .SetStateJournalText("Captain Set asked you to restore relay flags on the road to Mos Eisley. Return to Captain Set when it is done.")

                .AddState()
                .SetStateJournalText("Return to Captain Set for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(4000);
        }

        private static void TatooineBeetlePlateOrder(QuestBuilder builder)
        {
            builder.Create("tat_beetle_plate_order", "Beetle Plate Order")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Mera Vepp asked you to gather a standing order of sand beetle plates. The trail points toward Verpex Bazaar. Return to Mera Vepp when it is done.")
                .AddKillObjective(NPCGroupType.Tatooine_SandBeetle, 8)
                .AddCollectItemObjective("qi_tatooine_027", 1)

                .AddState()
                .SetStateJournalText("Return to Mera Vepp for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }
    }
}
