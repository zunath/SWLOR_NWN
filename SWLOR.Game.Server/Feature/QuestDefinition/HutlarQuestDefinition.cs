using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class HutlarQuestDefinition: IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            BeatTheByysk();
            CullTheTundraThreat();
            HutlarPowerInvestigation();
            StupendousSlugBile();
            BreakTheByysk();
            HutlarHeatExchangers();
            HutlarSlugBileRun();
            HutlarTigerCull();
            HutlarByyskLine();
            HutlarValleyFlags();
            HutlarFrostSamples();
            HutlarFrozenCaches();
            HutlarAbandonedLogs();
            HutlarSmugglerCrates();
            HutlarCloneTubes();
            HutlarTerminalAftershock();
            HutlarTunnelerRumble();
            HutlarBroodmotherClutch();
            HutlarGuardianPatrol();
            HutlarShamanTotems();
            HutlarChieftainChallenge();
            HutlarChampionScars();
            HutlarFoothillTransmitter();
            HutlarCaveRescue();
            HutlarRationRun();
            HutlarBeaconTriangulation();
            HutlarAntennaParts();
            HutlarStormGlass();
            HutlarBlackLedger();
            HutlarCloneStabilizers();
            HutlarOldRepublicCrate();
            HutlarHeatPacks();
            HutlarNestMap();
            HutlarWarDrums();
            HutlarLongPatrol();
            HutlarQionIcecore();
            HutlarValleySlugtrail();
            HutlarOutpostFilters();
            HutlarFrozenSensors();
            HutlarSmugglerBeacons();
            HutlarCloneLogs();
            HutlarByyskShamanPatrol();
            HutlarChieftainBanner();
            HutlarChampionArmor();
            HutlarBroodmotherShell();
            HutlarTunnelerChitin();
            HutlarQionTigerPelts();
            HutlarSlugMucus();
            HutlarCaveHeatlines();
            HutlarValleyWhiteout();
            HutlarTestsiteCleanup();
            HutlarOutpostLastShift();
            return _builder.Build();
        }

        private void BeatTheByysk()
        {
            _builder.Create("beat_byysk", "Beat the Byysk")
                .AddState()
                .SetStateJournalText("You've agreed to kill fifteen Byysk out in the Qion Tundra. Kill them all!")
                .AddKillObjective(NPCGroupType.Hutlar_Byysk, 15)

                .AddState()
                .SetStateJournalText("Return to Rorrska Buvvien in the Hutlar Outpost and report your progress.")

                .AddGoldReward(1200)
                .AddXPReward(800);
        }

        private void CullTheTundraThreat()
        {
            _builder.Create("tundra_tiger_threat", "Cull the Tundra Tiger Threat")
                .AddState()
                .SetStateJournalText("Kieun Xorxca wants you to head to Qion Tundra and kill ten tigers. Report back when this is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionTigers, 10)

                .AddState()
                .SetStateJournalText("Return to Kieun Xorxca in the Hutlar Outpost and report your progress.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarPowerInvestigation()
        {
            _builder.Create("hut_power_invest", "Hutlar Power Investigation")
                .PrerequisiteQuest("beat_byysk")
                .PrerequisiteQuest("tundra_tiger_threat")
                .PrerequisiteQuest("stup_slug_bile")
                // Use object

                .AddState()
                .SetStateJournalText("Investigate the first power terminal in the southeastern section of the Qion Tundra.")
                // Use object

                .AddState()
                .SetStateJournalText("Investigate the second power terminal in the central section of the Qion Tundra.")
                // Use object

                .AddState()
                .SetStateJournalText("Investigate the third power terminal in the northern section of the Qion Tundra.")
                // Use object

                .AddState()
                .SetStateJournalText("Investigate the fourth power terminal in the southwestern section of the Qion Tundra.")
                // Use object

                .AddState()
                .SetStateJournalText("Investigate the fifth power terminal in the northwestern section of the Qion Tundra.")
                // Talk to NPC

                .AddState()
                .SetStateJournalText("Return to Guylan Verruchi in the Hutlar Outpost and report on your findings.")
                // Use object

                .AddState()
                .SetStateJournalText("Replace the actuator on the power terminal in the northwestern section of Qion Tundra.")
                // Talk to NPC

                .AddState()
                .SetStateJournalText("Return to Guylan Verruchi in the Hutlar Outpost and let him know you've replaced the actuator.")

                .AddGoldReward(1800)
                .AddXPReward(1300)

                .OnAcceptAction((player, sourceObject) =>
                {
                    // Southeast
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "9CD9E7D9-4F10-4A0E-B67D-293CE6EA8EF5", VisibilityType.Visible);
                })

                .OnAbandonAction(player =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "9CD9E7D9-4F10-4A0E-B67D-293CE6EA8EF5", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "989B8C42-B4EE-48B7-8426-9D5C20016AEB", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "4C5721F2-9241-4A6F-9A62-F28CF0525682", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "E9C705B1-2AC9-4F9A-B481-FF3E5E99D8FF", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "83652C7A-7D38-4304-AD4B-92D5783AB279", VisibilityType.Hidden);
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "AA0E6798-38E4-4E50-8F0A-C3177FBF2717", VisibilityType.Hidden);
                })

                .OnAdvanceAction((player, sourceObject, state) =>
                {
                    string visibilityObject;
                    switch (state)
                    {
                        // Central
                        case 2:
                            visibilityObject = "989B8C42-B4EE-48B7-8426-9D5C20016AEB";
                            break;
                        // Northern
                        case 3:
                            visibilityObject = "4C5721F2-9241-4A6F-9A62-F28CF0525682";
                            break;
                        // Southwestern
                        case 4:
                            visibilityObject = "E9C705B1-2AC9-4F9A-B481-FF3E5E99D8FF";
                            break;
                        // Northwestern
                        case 5:
                            visibilityObject = "83652C7A-7D38-4304-AD4B-92D5783AB279";
                            break;
                        // Northwestern again, Actuator
                        case 7:
                            visibilityObject = "AA0E6798-38E4-4E50-8F0A-C3177FBF2717";
                            break;
                        default: return;
                    }
                    ObjectVisibility.AdjustVisibilityByObjectId(player, visibilityObject, VisibilityType.Visible);
                });
        }

        private void StupendousSlugBile()
        {
            _builder.Create("stup_slug_bile", "Stupendious Slug Bile")
                .AddState()
                .SetStateJournalText("Moricho Deine in the Hutlar Outpost has requested you collect five Slug Biles from the Qion Slugs in Qion Tundra. Collect them and give them to him for a reward.")
                .AddCollectItemObjective("slug_bile", 5)

                .AddState()
                .SetStateJournalText("Speak to Moricho Deine for your reward.")

                .AddGoldReward(1113)
                .AddItemReward("slug_shake", 1);
        }

        private void BreakTheByysk()
        {
            _builder.Create("break_the_byysk", "Break the Byysk")
                .AddState()
                .SetStateJournalText("Sharene wants you to kill two hundred and fifty Byysk. Off you go!")
                .AddKillObjective(NPCGroupType.Byysk_Guardian, 250)

                .AddState()
                .SetStateJournalText("That wasn't too bad! It didn't take as long as you thought it would. Good work! Return to Sharene.")

                .AddGoldReward(22500)
                .AddXPReward(15000)
                .AddItemReward("recipe_banners01", 1);
        }

        private void HutlarHeatExchangers()
        {
            _builder.Create("hut_heat_exchangers", "Heat Exchangers")
                .AddState()
                .SetStateJournalText("Mara Vulk asked you to repair heat exchangers in the outpost. The trail points toward Hutlar Outpost. Return to Mara Vulk when it is done.")

                .AddState()
                .SetStateJournalText("Return to Mara Vulk for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarSlugBileRun()
        {
            _builder.Create("hut_slug_bile_run", "Slug Bile Run")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Moricho's Assistant Nenn asked you to gather Qion Slug bile. The trail points toward Hutlar Outpost. Return to Moricho's Assistant Nenn when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionSlugs, 8)
                .AddCollectItemObjective("slug_bile", 5)

                .AddState()
                .SetStateJournalText("Return to Moricho's Assistant Nenn for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarTigerCull()
        {
            _builder.Create("hut_tiger_cull", "Tiger Cull")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Kieun's Scout Ora asked you to deal with Qion Tigers. The trail points toward Hutlar Outpost. Return to Kieun's Scout Ora when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionTigers, 8)

                .AddState()
                .SetStateJournalText("Return to Kieun's Scout Ora for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarByyskLine()
        {
            _builder.Create("hut_byysk_line", "Byysk Line")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Guard Rolska asked you to deal with Byysk warriors in Qion Tundra. The trail points toward the Qion hive. Return to Guard Rolska when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_Byysk, 8)

                .AddState()
                .SetStateJournalText("Return to Guard Rolska for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarValleyFlags()
        {
            _builder.Create("hut_valley_flags", "Valley Weather Flags")
                .AddState()
                .SetStateJournalText("Tech Siva asked you to plant weather flags in Qion Valley. The trail points toward the Byysk valley. Return to Tech Siva when it is done.")

                .AddState()
                .SetStateJournalText("Return to Tech Siva for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarFrostSamples()
        {
            _builder.Create("hut_frost_samples", "Frost Samples")
                .AddState()
                .SetStateJournalText("Dr. Pella asked you to gather frost samples from Frozen Wastes. Return to Dr. Pella when it is done.")
                .AddCollectItemObjective("qi_hutlar_001", 1)

                .AddState()
                .SetStateJournalText("Return to Dr. Pella for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarFrozenCaches()
        {
            _builder.Create("hut_frozen_caches", "Frozen Caches")
                .AddState()
                .SetStateJournalText("Quartermaster Yov asked you to recover Byysk caches in Frozen Caves. The trail points toward the waste caverns. Return to Quartermaster Yov when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_Byysk, 6)
                .AddCollectItemObjective("qi_hutlar_002", 1)

                .AddState()
                .SetStateJournalText("Return to Quartermaster Yov for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarAbandonedLogs()
        {
            _builder.Create("hut_abandoned_logs", "Abandoned Logs")
                .AddState()
                .SetStateJournalText("Salo Benn asked you to recover logs from the Abandoned Outpost. The trail points toward the smuggler bay. Return to Salo Benn when it is done.")
                .AddCollectItemObjective("qi_hutlar_003", 1)

                .AddState()
                .SetStateJournalText("Return to Salo Benn for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarSmugglerCrates()
        {
            _builder.Create("hut_smuggler_crates", "Smuggler Crates")
                .AddState()
                .SetStateJournalText("Customs Agent Urr asked you to recover smuggler crates. The trail points toward the smuggler bay. Return to Customs Agent Urr when it is done.")
                .AddCollectItemObjective("qi_hutlar_004", 1)

                .AddState()
                .SetStateJournalText("Return to Customs Agent Urr for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarCloneTubes()
        {
            _builder.Create("hut_clone_tubes", "Clone Tubes")
                .AddState()
                .SetStateJournalText("Researcher Venn asked you to gather specimen tubes from the Cloning Test Site. Return to Researcher Venn when it is done.")
                .AddCollectItemObjective("qi_hutlar_005", 1)

                .AddState()
                .SetStateJournalText("Return to Researcher Venn for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarTerminalAftershock()
        {
            _builder.Create("hut_terminal_aftershock", "Terminal Aftershock")
                .AddState()
                .SetStateJournalText("Guylan's Aide Pavo asked you to inspect damaged power terminals in Qion Tundra. The trail points toward Hutlar Outpost. Return to Guylan's Aide Pavo when it is done.")

                .AddState()
                .SetStateJournalText("Return to Guylan's Aide Pavo for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarTunnelerRumble()
        {
            _builder.Create("hut_tunneler_rumble", "Tunneler Rumble")
                .AddState()
                .SetStateJournalText("Miner Sava asked you to deal with Qion Hive Tunnelers. Return to Miner Sava when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionHiveTunneler, 6)

                .AddState()
                .SetStateJournalText("Return to Miner Sava for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarBroodmotherClutch()
        {
            _builder.Create("hut_broodmother_clutch", "Broodmother Clutch")
                .AddState()
                .SetStateJournalText("Ranger Olra asked you to recover clutch material from the Qion Broodmother. The trail points toward the Qion hive. Return to Ranger Olra when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionBroodmother, 1)
                .AddCollectItemObjective("qi_hutlar_006", 1)

                .AddState()
                .SetStateJournalText("Return to Ranger Olra for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void HutlarGuardianPatrol()
        {
            _builder.Create("hut_guardian_patrol", "Guardian Patrol")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Sharene's Watcher Vika asked you to deal with Byysk Guardians. The trail points toward Hutlar Outpost. Return to Sharene's Watcher Vika when it is done.")
                .AddKillObjective(NPCGroupType.Byysk_Guardian, 8)

                .AddState()
                .SetStateJournalText("Return to Sharene's Watcher Vika for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarShamanTotems()
        {
            _builder.Create("hut_shaman_totems", "Shaman Totems")
                .AddState()
                .SetStateJournalText("Ritualist Henn asked you to gather Byysk Shaman totems. The trail points toward the waste caverns. Return to Ritualist Henn when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskShaman, 6)
                .AddCollectItemObjective("qi_hutlar_007", 1)

                .AddState()
                .SetStateJournalText("Return to Ritualist Henn for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarChieftainChallenge()
        {
            _builder.Create("hut_chieftain_challenge", "Chieftain Challenge")
                .AddState()
                .SetStateJournalText("Duelist Korr asked you to defeat a Byysk Chieftain. The trail points toward the waste caverns. Return to Duelist Korr when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskChieftain, 1)

                .AddState()
                .SetStateJournalText("Return to Duelist Korr for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void HutlarChampionScars()
        {
            _builder.Create("hut_champion_scars", "Champion Scars")
                .AddState()
                .SetStateJournalText("Hunter Valla asked you to defeat a Byysk Champion and recover trophies. The trail points toward the waste caverns. Return to Hunter Valla when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskChampion, 1)
                .AddCollectItemObjective("qi_hutlar_008", 1)

                .AddState()
                .SetStateJournalText("Return to Hunter Valla for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000)
                .AddItemReward("hut_champ_mark", 1);
        }

        private void HutlarFoothillTransmitter()
        {
            _builder.Create("hut_foothill_transmitter", "Foothill Transmitter")
                .AddState()
                .SetStateJournalText("Signal Tech Yeri asked you to repair a transmitter in Qion Tundra. The trail points toward the Qion hive. Return to Signal Tech Yeri when it is done.")

                .AddState()
                .SetStateJournalText("Return to Signal Tech Yeri for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarCaveRescue()
        {
            _builder.Create("hut_cave_rescue", "Frost Cave Rescue")
                .AddState()
                .SetStateJournalText("Medic Rela asked you to locate a missing survivor in Frozen Caves. The trail points toward Hutlar Outpost. Return to Medic Rela when it is done.")

                .AddState()
                .SetStateJournalText("Return to Medic Rela for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarRationRun()
        {
            _builder.Create("hut_ration_run", "Ration Run")
                .AddState()
                .SetStateJournalText("Cook Merska asked you to gather ration crates around the outpost. The trail points toward Hutlar Outpost. Return to Cook Merska when it is done.")
                .AddCollectItemObjective("qi_hutlar_009", 1)

                .AddState()
                .SetStateJournalText("Return to Cook Merska for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarBeaconTriangulation()
        {
            _builder.Create("hut_beacon_triangulation", "Beacon Triangulation")
                .AddState()
                .SetStateJournalText("Cartographer Den asked you to activate three tundra beacons. The trail points toward the Byysk valley. Return to Cartographer Den when it is done.")

                .AddState()
                .SetStateJournalText("Return to Cartographer Den for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarAntennaParts()
        {
            _builder.Create("hut_antenna_parts", "Antenna Parts")
                .AddState()
                .SetStateJournalText("Engineer Lova asked you to gather antenna parts from Qion Valley. The trail points toward the Byysk valley. Return to Engineer Lova when it is done.")
                .AddCollectItemObjective("qi_hutlar_010", 1)

                .AddState()
                .SetStateJournalText("Return to Engineer Lova for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarStormGlass()
        {
            _builder.Create("hut_storm_glass", "Storm Glass")
                .AddState()
                .SetStateJournalText("Weatherhand Iks asked you to gather storm glass from Frozen Wastes. Return to Weatherhand Iks when it is done.")
                .AddCollectItemObjective("qi_hutlar_011", 1)

                .AddState()
                .SetStateJournalText("Return to Weatherhand Iks for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarBlackLedger()
        {
            _builder.Create("hut_black_ledger", "Black Ledger")
                .AddState()
                .SetStateJournalText("Inspector Vokk asked you to recover a black ledger from the smuggler base. The trail points toward the smuggler bay. Return to Inspector Vokk when it is done.")
                .AddCollectItemObjective("qi_hutlar_012", 1)

                .AddState()
                .SetStateJournalText("Return to Inspector Vokk for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void HutlarCloneStabilizers()
        {
            _builder.Create("hut_clone_stabilizers", "Clone Stabilizers")
                .AddState()
                .SetStateJournalText("Lab Tech Nara asked you to recover clone stabilizers. The trail points toward the cloning test site. Return to Lab Tech Nara when it is done.")
                .AddCollectItemObjective("qi_hutlar_013", 1)

                .AddState()
                .SetStateJournalText("Return to Lab Tech Nara for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarOldRepublicCrate()
        {
            _builder.Create("hut_old_republic_crate", "Old Republic Crate")
                .AddState()
                .SetStateJournalText("Historian Pell asked you to recover an old Republic crate in Frozen Caves. The trail points toward the waste caverns. Return to Historian Pell when it is done.")
                .AddCollectItemObjective("qi_hutlar_014", 1)

                .AddState()
                .SetStateJournalText("Return to Historian Pell for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarHeatPacks()
        {
            _builder.Create("hut_heat_packs", "Heat Packs")
                .AddState()
                .SetStateJournalText("Dr. Havi asked you to gather medical heat packs from scattered caches. The trail points toward Hutlar Outpost. Return to Dr. Havi when it is done.")
                .AddCollectItemObjective("qi_hutlar_015", 1)

                .AddState()
                .SetStateJournalText("Return to Dr. Havi for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarNestMap()
        {
            _builder.Create("hut_nest_map", "Nest Map")
                .AddState()
                .SetStateJournalText("Scout Vesk asked you to clear slugs and tigers while mapping nests. The trail points toward the Qion hive. Return to Scout Vesk when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionSlugs, 6)
                .AddKillObjective(NPCGroupType.Hutlar_QionTigers, 6)

                .AddState()
                .SetStateJournalText("Return to Scout Vesk for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarWarDrums()
        {
            _builder.Create("hut_war_drums", "War Drums")
                .AddState()
                .SetStateJournalText("Rorrska's Runner Pell asked you to recover Byysk war drum pieces. The trail points toward Hutlar Outpost. Return to Rorrska's Runner Pell when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_Byysk, 6)
                .AddCollectItemObjective("qi_hutlar_016", 1)

                .AddState()
                .SetStateJournalText("Return to Rorrska's Runner Pell for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarLongPatrol()
        {
            _builder.Create("hut_long_patrol", "Long Patrol")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Patrol Chief Neer asked you to complete a long patrol by killing Byysk and Qion beasts. The trail points toward Hutlar Outpost. Return to Patrol Chief Neer when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_Byysk, 10)

                .AddState()
                .SetStateJournalText("Return to Patrol Chief Neer for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarQionIcecore()
        {
            _builder.Create("hut_qion_icecore", "Qion Icecore")
                .PrerequisiteQuest("hut_frost_samples")
                .AddState()
                .SetStateJournalText("Dr. Pella asked you to recover icecore samples from Frozen Wastes. Return to Dr. Pella when it is done.")
                .AddCollectItemObjective("qi_hutlar_017", 1)

                .AddState()
                .SetStateJournalText("Return to Dr. Pella for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarValleySlugtrail()
        {
            _builder.Create("hut_valley_slugtrail", "Valley Slugtrail")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Scout Vesk asked you to mark Qion Slug trails through Qion Valley. The trail points toward the Byysk valley. Return to Scout Vesk when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionSlugs, 8)

                .AddState()
                .SetStateJournalText("Return to Scout Vesk for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarOutpostFilters()
        {
            _builder.Create("hut_outpost_filters", "Outpost Filters")
                .PrerequisiteQuest("hut_heat_exchangers")
                .AddState()
                .SetStateJournalText("Mara Vulk asked you to replace clogged outpost air filters. The trail points toward Hutlar Outpost. Return to Mara Vulk when it is done.")

                .AddState()
                .SetStateJournalText("Return to Mara Vulk for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarFrozenSensors()
        {
            _builder.Create("hut_frozen_sensors", "Frozen Sensors")
                .PrerequisiteQuest("hut_storm_glass")
                .AddState()
                .SetStateJournalText("Weatherhand Iks asked you to repair frozen weather sensors. The trail points toward the frozen wastes. Return to Weatherhand Iks when it is done.")

                .AddState()
                .SetStateJournalText("Return to Weatherhand Iks for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarSmugglerBeacons()
        {
            _builder.Create("hut_smuggler_beacons", "Smuggler Beacons")
                .PrerequisiteQuest("hut_black_ledger")
                .AddState()
                .SetStateJournalText("Inspector Vokk asked you to disable smuggler beacons in the Abandoned Outpost. The trail points toward the smuggler bay. Return to Inspector Vokk when it is done.")

                .AddState()
                .SetStateJournalText("Return to Inspector Vokk for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarCloneLogs()
        {
            _builder.Create("hut_clone_logs", "Clone Logs")
                .PrerequisiteQuest("hut_clone_tubes")
                .AddState()
                .SetStateJournalText("Researcher Venn asked you to recover clone experiment logs. The trail points toward the cloning test site. Return to Researcher Venn when it is done.")
                .AddCollectItemObjective("qi_hutlar_018", 1)

                .AddState()
                .SetStateJournalText("Return to Researcher Venn for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000)
                .AddItemReward("hut_clone_chip", 1);
        }

        private void HutlarByyskShamanPatrol()
        {
            _builder.Create("hut_byysk_shaman_patrol", "Byysk Shaman Patrol")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Ritualist Henn asked you to defeat Byysk Shamans in Frozen Caves. The trail points toward the waste caverns. Return to Ritualist Henn when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskShaman, 8)

                .AddState()
                .SetStateJournalText("Return to Ritualist Henn for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarChieftainBanner()
        {
            _builder.Create("hut_chieftain_banner", "Chieftain Banner")
                .PrerequisiteQuest("hut_chieftain_challenge")
                .AddState()
                .SetStateJournalText("Duelist Korr asked you to recover a Byysk Chieftain banner. The trail points toward the waste caverns. Return to Duelist Korr when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskChieftain, 1)
                .AddCollectItemObjective("qi_hutlar_019", 1)

                .AddState()
                .SetStateJournalText("Return to Duelist Korr for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarChampionArmor()
        {
            _builder.Create("hut_champion_armor", "Champion Armor")
                .PrerequisiteQuest("hut_champion_scars")
                .AddState()
                .SetStateJournalText("Hunter Valla asked you to recover Byysk Champion armor scraps. The trail points toward the waste caverns. Return to Hunter Valla when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskChampion, 1)
                .AddCollectItemObjective("qi_hutlar_020", 1)

                .AddState()
                .SetStateJournalText("Return to Hunter Valla for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarBroodmotherShell()
        {
            _builder.Create("hut_broodmother_shell", "Broodmother Shell")
                .PrerequisiteQuest("hut_broodmother_clutch")
                .AddState()
                .SetStateJournalText("Ranger Olra asked you to recover shell fragments from the Qion Broodmother. The trail points toward the Qion hive. Return to Ranger Olra when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionBroodmother, 1)
                .AddCollectItemObjective("qi_hutlar_021", 1)

                .AddState()
                .SetStateJournalText("Return to Ranger Olra for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000)
                .AddItemReward("hut_broodplate", 1);
        }

        private void HutlarTunnelerChitin()
        {
            _builder.Create("hut_tunneler_chitin", "Tunneler Chitin")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Miner Sava asked you to gather Qion Hive Tunneler chitin. Return to Miner Sava when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionHiveTunneler, 8)
                .AddCollectItemObjective("qi_hutlar_022", 1)

                .AddState()
                .SetStateJournalText("Return to Miner Sava for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarQionTigerPelts()
        {
            _builder.Create("hut_qion_tiger_pelts", "Qion Tiger Pelts")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Kieun's Scout Ora asked you to gather Qion Tiger pelts. The trail points toward the Byysk valley. Return to Kieun's Scout Ora when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionTigers, 8)
                .AddCollectItemObjective("qion_tiger_fang", 5)

                .AddState()
                .SetStateJournalText("Return to Kieun's Scout Ora for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarSlugMucus()
        {
            _builder.Create("hut_slug_mucus", "Slug Mucus")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Moricho's Assistant Nenn asked you to gather Qion Slug mucus. The trail points toward the Qion hive. Return to Moricho's Assistant Nenn when it is done.")
                .AddKillObjective(NPCGroupType.Hutlar_QionSlugs, 8)
                .AddCollectItemObjective("qi_hutlar_023", 1)

                .AddState()
                .SetStateJournalText("Return to Moricho's Assistant Nenn for your reward.")

                .AddGoldReward(900)
                .AddXPReward(800);
        }

        private void HutlarCaveHeatlines()
        {
            _builder.Create("hut_cave_heatlines", "Cave Heatlines")
                .PrerequisiteQuest("hut_antenna_parts")
                .AddState()
                .SetStateJournalText("Engineer Lova asked you to restore heatlines in Frozen Caves. The trail points toward the waste caverns. Return to Engineer Lova when it is done.")

                .AddState()
                .SetStateJournalText("Return to Engineer Lova for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarValleyWhiteout()
        {
            _builder.Create("hut_valley_whiteout", "Valley Whiteout")
                .PrerequisiteQuest("hut_beacon_triangulation")
                .AddState()
                .SetStateJournalText("Cartographer Den asked you to recover survey stakes after a whiteout. The trail points toward the Byysk valley. Return to Cartographer Den when it is done.")
                .AddCollectItemObjective("qi_hutlar_024", 1)

                .AddState()
                .SetStateJournalText("Return to Cartographer Den for your reward.")

                .AddGoldReward(825)
                .AddXPReward(800);
        }

        private void HutlarTestsiteCleanup()
        {
            _builder.Create("hut_testsite_cleanup", "Test Site Cleanup")
                .PrerequisiteQuest("hut_clone_stabilizers")
                .AddState()
                .SetStateJournalText("Lab Tech Nara asked you to remove failed specimens from the Cloning Test Site. Return to Lab Tech Nara when it is done.")

                .AddState()
                .SetStateJournalText("Return to Lab Tech Nara for your reward.")

                .AddGoldReward(1800)
                .AddXPReward(1300);
        }

        private void HutlarOutpostLastShift()
        {
            _builder.Create("hut_outpost_last_shift", "Last Shift")
                .AddState()
                .SetStateJournalText("Patrol Chief Neer asked you to complete final perimeter checks around Hutlar Outpost. Return to Patrol Chief Neer when it is done.")

                .AddState()
                .SetStateJournalText("Return to Patrol Chief Neer for your reward.")

                .AddGoldReward(22500)
                .AddXPReward(15000)
                .AddItemReward("hut_last_badge", 1);
        }
    }
}
