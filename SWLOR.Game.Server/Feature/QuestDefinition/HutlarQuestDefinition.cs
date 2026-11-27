using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using static SWLOR.Game.Server.Composition.GameServices;

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
            ShutDownTheLine();

            FangsForTheTrappers();
            SlugTeeth();
            FoothillWarbands();
            TundraWatch();
            TigerBloodTonic();
            ColdCuts();
            OreForTheGenerators();
            AraciaTimber();
            BreakTheStrongholdThawTheAdepts();
            BreakTheStrongholdTheChampion();
            BreakTheStrongholdSilenceTheShaman();
            BreakTheStrongholdTheChieftain();
            HiveScouts();
            SealTheHiveTunnelers();
            SealTheHiveTheBroodmother();
            FoundryMemoryCores();
            FoundryLineStoppage();
            FoundryPullThePlug();
            ScrapBountySorterPrime();
            ScrapBountyForemanRX12();
            ScrapBountyInspectorQ0();

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

        private void ShutDownTheLine()
        {
            _builder.Create("fdy_shut_line", "Shut Down the Line")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Kesa Morn says the old droid foundry beneath the Abandoned Outpost has started building war droids again, and its patrols are already testing the outpost doors. Destroy twenty foundry droids on any floor to set the line back.")
                .AddKillObjective(NPCGroupType.Hutlar_FoundryDroid, 20)

                .AddState()
                .SetStateJournalText("Return to Kesa Morn by the Foundry Access door in the Abandoned Outpost on Hutlar.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

    

        private void FangsForTheTrappers()
        {
            _builder.Create("hut_fangs", "Fangs for the Trappers")

                .AddState()
                .SetStateJournalText("Brannoc Tull sets tiger fangs into his trap jaws because steel snaps in the cold. Collect 6 Qion Tiger Fangs from the Qion Tigers of the Qion Tundra, the Qion Valley or the Frozen Wastes, then bring them to Brannoc Tull at the Hutlar Outpost.")
                .AddCollectItemObjective("qion_tiger_fang", 6)

                .AddState()
                .SetStateJournalText("You collected 6 Qion Tiger Fangs. Bring them to Brannoc Tull at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(4000);
        }

        private void SlugTeeth()
        {
            _builder.Create("hut_slug_teeth", "Slug Teeth")

                .AddState()
                .SetStateJournalText("The outpost toolmaker cuts ice blocks with Qion Slug teeth, and Brannoc Tull supplies her. Collect 8 Slug Teeth from the Qion Slugs of the Qion Tundra or the Qion Valley, then bring them to Brannoc Tull at the Hutlar Outpost.")
                .AddCollectItemObjective("slug_tooth", 8)

                .AddState()
                .SetStateJournalText("You collected 8 Slug Teeth. Bring them to Brannoc Tull at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(4000);
        }

        private void FoothillWarbands()
        {
            _builder.Create("hut_foothill_bands", "Foothill Warbands")

                .AddState()
                .SetStateJournalText("Byysk warbands are breaking Brannoc Tull's traps to drive the outpost off the tundra. Kill 12 Byysk on the Qion Tundra, in the Qion Foothills or in the Frozen Caves, then return to Brannoc Tull at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_Byysk, 12)

                .AddState()
                .SetStateJournalText("You killed 12 Byysk and answered the warbands. Return to Brannoc Tull at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(4000);
        }

        private void TundraWatch()
        {
            _builder.Create("hut_tundra_watch", "Tundra Watch")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Brannoc Tull pays a standing watch rate to keep his trap lines clear of animals. Kill 10 Qion Tigers and 10 Qion Slugs on Hutlar, then return to Brannoc Tull at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_QionTigers, 10)
                .AddKillObjective(NPCGroupType.Hutlar_QionSlugs, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Qion Tigers and 10 Qion Slugs. Return to Brannoc Tull at the Hutlar Outpost on Hutlar to collect the watch rate.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void TigerBloodTonic()
        {
            _builder.Create("hut_tiger_tonic", "Tiger Blood Tonic")

                .AddState()
                .SetStateJournalText("Marla Keth cooks Tiger Blood into a tonic that treats frostbite. Collect 5 Tiger Blood from the Qion Tigers of the Qion Tundra, the Qion Valley or the Frozen Wastes, then bring it to Marla Keth at the Hutlar Outpost.")
                .AddCollectItemObjective("tiger_blood", 5)

                .AddState()
                .SetStateJournalText("You collected 5 Tiger Blood. Bring it to Marla Keth at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(4000);
        }

        private void ColdCuts()
        {
            _builder.Create("hut_cold_cuts", "Cold Cuts")

                .AddState()
                .SetStateJournalText("The supply run is late and the Hutlar Outpost is short of meat. Collect 6 Byysk Meat and 6 Byysk Tails from the Byysk of the Qion Tundra, then bring them to Marla Keth at the Hutlar Outpost.")
                .AddCollectItemObjective("byysk_meat", 6)
                .AddCollectItemObjective("byysk_tail", 6)

                .AddState()
                .SetStateJournalText("You collected 6 Byysk Meat and 6 Byysk Tails. Bring them to Marla Keth at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(4000)
                .AddItemReward("byysk_mball", 3);
        }

        private void OreForTheGenerators()
        {
            _builder.Create("hut_generator_ore", "Ore for the Generators")

                .AddState()
                .SetStateJournalText("Two heaters at the Hutlar Outpost have burned out their coils, and new coils need ore. Collect 10 Raw Keromber and 10 Raw Plagionite, mined on Hutlar, then bring them to Marla Keth at the Hutlar Outpost.")
                .AddCollectItemObjective("raw_keromber", 10)
                .AddCollectItemObjective("raw_plagionite", 10)

                .AddState()
                .SetStateJournalText("You collected 10 Raw Keromber and 10 Raw Plagionite. Bring them to Marla Keth at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(4000);
        }

        private void AraciaTimber()
        {
            _builder.Create("hut_aracia_timber", "Aracia Timber")

                .AddState()
                .SetStateJournalText("The Hutlar Outpost needs timber that does not split in the cold to rebuild its store racks. Collect 8 Aracia Wood from the aracia trees of the Qion Tundra or the Qion Valley, then bring it to Marla Keth at the Hutlar Outpost.")
                .AddCollectItemObjective("aracia_wood", 8)

                .AddState()
                .SetStateJournalText("You collected 8 Aracia Wood. Bring it to Marla Keth at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(4000);
        }

        private void BreakTheStrongholdThawTheAdepts()
        {
            _builder.Create("hut_strong_1", "Break the Stronghold: Thaw the Adepts")

                .AddState()
                .SetStateJournalText("Byysk Cryo Adepts freeze the militia line so the warbands can break it. Kill 5 Byysk Cryo Adepts in the Frozen Caves or the Qion Foothills on Hutlar, then return to Sergeant Dova Rhun at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskCryoAdept, 5)

                .AddState()
                .SetStateJournalText("You killed 5 Byysk Cryo Adepts. Return to Sergeant Dova Rhun at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void BreakTheStrongholdTheChampion()
        {
            _builder.Create("hut_strong_2", "Break the Stronghold: The Champion")
                .PrerequisiteQuest("hut_strong_1")

                .AddState()
                .SetStateJournalText("The Byysk Champion guards the first chamber of the ziggurat in the Qion Hive on Hutlar. Kill the Byysk Champion, then return to Sergeant Dova Rhun at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskChampion, 1)

                .AddState()
                .SetStateJournalText("You killed the Byysk Champion. Return to Sergeant Dova Rhun at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void BreakTheStrongholdSilenceTheShaman()
        {
            _builder.Create("hut_strong_3", "Break the Stronghold: Silence the Shaman")
                .PrerequisiteQuest("hut_strong_2")

                .AddState()
                .SetStateJournalText("The Byysk Shaman trains the Cryo Adepts and holds the second chamber of the ziggurat in the Qion Hive on Hutlar. Kill the Byysk Shaman, then return to Sergeant Dova Rhun at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskShaman, 1)

                .AddState()
                .SetStateJournalText("You killed the Byysk Shaman. Return to Sergeant Dova Rhun at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void BreakTheStrongholdTheChieftain()
        {
            _builder.Create("hut_strong_4", "Break the Stronghold: The Chieftain")
                .PrerequisiteQuest("hut_strong_3")

                .AddState()
                .SetStateJournalText("The Byysk Chieftain commands the warbands from the last chamber of the ziggurat in the Qion Hive on Hutlar. Kill the Byysk Chieftain, then return to Sergeant Dova Rhun at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_ByyskChieftain, 1)

                .AddState()
                .SetStateJournalText("You killed the Byysk Chieftain. Return to Sergeant Dova Rhun at the Hutlar Outpost on Hutlar to collect your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("chieftain_torc", 1)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.BreakTheStronghold);
                });
        }

        private void HiveScouts()
        {
            _builder.Create("hut_hive_scouts", "Hive Scouts")
                .PrerequisiteQuest("hut_strong_4")

                .AddState()
                .SetStateJournalText("A door behind the Chieftain's chamber leads into the Outer Hive, which is full of pale slugs. Kill 12 Qion Hive Slugs in the Outer Hive of the Qion Hive on Hutlar, then return to Sergeant Dova Rhun at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_QionHiveSlug, 12)

                .AddState()
                .SetStateJournalText("You killed 12 Qion Hive Slugs. Return to Sergeant Dova Rhun at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void SealTheHiveTunnelers()
        {
            _builder.Create("hut_hive_1", "Seal the Hive: Tunnelers")
                .PrerequisiteQuest("hut_strong_4")

                .AddState()
                .SetStateJournalText("Qion Hive Tunnelers are digging outward from the Outer Hive toward the outpost's power structures. Kill 12 Qion Hive Tunnelers in the Outer Hive of the Qion Hive on Hutlar, then return to Sergeant Dova Rhun at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_QionHiveTunneler, 12)

                .AddState()
                .SetStateJournalText("You killed 12 Qion Hive Tunnelers. Return to Sergeant Dova Rhun at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void SealTheHiveTheBroodmother()
        {
            _builder.Create("hut_hive_2", "Seal the Hive: The Broodmother")
                .PrerequisiteQuest("hut_hive_1")

                .AddState()
                .SetStateJournalText("The Qion Hive Broodmother lays every Tunneler and lies at the heart of the Qion Hive on Hutlar, beyond the Outer Hive. Kill the Qion Hive Broodmother, then return to Sergeant Dova Rhun at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_QionHiveBroodmother, 1)

                .AddState()
                .SetStateJournalText("You killed the Qion Hive Broodmother. Return to Sergeant Dova Rhun at the Hutlar Outpost on Hutlar to collect your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("broodguard_ring", 1);
        }

        private void FoundryMemoryCores()
        {
            _builder.Create("hut_fdy_cores", "Foundry: Memory Cores")

                .AddState()
                .SetStateJournalText("Teb Orrin wants to read what the Hutlar Droid Foundry is building. Collect 5 Foundry Memory Cores from the foundry's droids, past Kesa Morn at the Foundry Access door in the Abandoned Outpost, then bring them to Teb Orrin at the Hutlar Outpost.")
                .AddCollectItemObjective("fdy_memory_core", 5)

                .AddState()
                .SetStateJournalText("You collected 5 Foundry Memory Cores. Bring them to Teb Orrin at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void FoundryLineStoppage()
        {
            _builder.Create("hut_fdy_stoppage", "Foundry: Line Stoppage")
                .PrerequisiteQuest("hut_fdy_cores")

                .AddState()
                .SetStateJournalText("The Hutlar Droid Foundry's line cannot run without its Smelter Hulks and Forge Wardens. Destroy 2 Smelter Hulks in the Intake and 2 Forge Wardens on the Assembly Line, then return to Teb Orrin at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_FoundrySmelterHulk, 2)
                .AddKillObjective(NPCGroupType.Hutlar_FoundryForgeWarden, 2)

                .AddState()
                .SetStateJournalText("You destroyed 2 Smelter Hulks and 2 Forge Wardens. Return to Teb Orrin at the Hutlar Outpost on Hutlar to collect your pay.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void FoundryPullThePlug()
        {
            _builder.Create("hut_fdy_plug", "Foundry: Pull the Plug")
                .PrerequisiteQuest("hut_fdy_stoppage")

                .AddState()
                .SetStateJournalText("The Foundry Overseer woke the Hutlar Droid Foundry and runs it from the Core. Destroy the Foundry Overseer in the Core of the foundry, then return to Teb Orrin at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_FoundryOverseer, 1)

                .AddState()
                .SetStateJournalText("You destroyed the Foundry Overseer in the Core. Return to Teb Orrin at the Hutlar Outpost on Hutlar to collect your reward.")

                .AddGoldReward(12000)
                .AddXPReward(15000)
                .AddItemReward("fdybreak_bracer", 1);
        }

        private void ScrapBountySorterPrime()
        {
            _builder.Create("hut_scrap_sorter", "Scrap Bounty: Sorter Prime")
                .PrerequisiteQuest("hut_fdy_cores")

                .AddState()
                .SetStateJournalText("Sorter Prime is a supervisor droid that directs salvage in the Intake of the Hutlar Droid Foundry. It is not always on the line. Destroy Sorter Prime, then return to Teb Orrin at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_SorterPrime, 1)

                .AddState()
                .SetStateJournalText("You destroyed Sorter Prime in the Intake. Return to Teb Orrin at the Hutlar Outpost on Hutlar to collect the bounty.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void ScrapBountyForemanRX12()
        {
            _builder.Create("hut_scrap_foreman", "Scrap Bounty: Foreman RX-12")
                .PrerequisiteQuest("hut_fdy_cores")

                .AddState()
                .SetStateJournalText("Foreman RX-12 is a supervisor droid that sets the pace on the Assembly Line of the Hutlar Droid Foundry. It is not always on the line. Destroy Foreman RX-12, then return to Teb Orrin at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_ForemanRX12, 1)

                .AddState()
                .SetStateJournalText("You destroyed Foreman RX-12 on the Assembly Line. Return to Teb Orrin at the Hutlar Outpost on Hutlar to collect the bounty.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void ScrapBountyInspectorQ0()
        {
            _builder.Create("hut_scrap_inspect", "Scrap Bounty: Inspector Q-0")
                .PrerequisiteQuest("hut_fdy_cores")

                .AddState()
                .SetStateJournalText("Inspector Q-0 is a supervisor droid that checks every war droid built in the Core of the Hutlar Droid Foundry. It is not always on the line. Destroy Inspector Q-0, then return to Teb Orrin at the Hutlar Outpost.")
                .AddKillObjective(NPCGroupType.Hutlar_InspectorQ0, 1)

                .AddState()
                .SetStateJournalText("You destroyed Inspector Q-0 in the Core. Return to Teb Orrin at the Hutlar Outpost on Hutlar to collect the bounty.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }
    }
}
