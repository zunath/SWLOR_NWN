using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class ViscaraQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new QuestBuilder();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            BlastTheMandalorianRangers();
            CoxxionInitiation();
            WeaponsForKrystalle();
            FindCaptainNguth();
            FirstRites();
            HelpTheTalyronFamily();
            KathHoundHunting();
            LocateTheMandalorianFacility();
            MandalorianDogTags();
            RepairingCoxxionEquipment();
            SlicingTheMandalorianFacility();
            SmuggleRoyMossPackage();
            StuffKeepsBreaking();
            TheMandalorianLeader();
            VanquishTheVellenRaiders();
            WarWithTheMandalorianWarriors();
            KathHoundPartCollection();
            TaxiTerminalRepairs();
            JoiningTheRepublic();
            MedicalEquipmentForShelby();
            SpiceOneSmallFavour();
            DantooineHerbs();
            RouteLedger();
            MarkerCodes();
            RunnerManifest();
            BurrowSurvey();
            FieldDressings();
            CacheCipher();
            ViscaraColonyLedgers();
            ViscaraSewerGrates();
            ViscaraGeneratorSplice();
            ViscaraSwampMold();
            ViscaraColdTrail();
            ViscaraLakeSurvey();
            ViscaraRangerTags();
            ViscaraDeepwoodsCourier();
            ViscaraSwampBurners();
            ViscaraFleshleaderReport();
            ViscaraRaivorRidge();
            ViscaraSpiderVenom();
            ViscaraLakePrisms();
            ViscaraJediRecords();
            ViscaraArchiveKeys();
            ViscaraGardenSoil();
            ViscaraManifestGap();
            ViscaraMerchantEscort();
            ViscaraRepublicShortfall();
            ViscaraCoxxionRumors();
            ViscaraHiddenRelay();
            ViscaraNashtahWatch();
            ViscaraScoutMaps();
            ViscaraSignalMountain();
            return _builder.Build();
        }

        private void BlastTheMandalorianRangers()
        {
            _builder.Create("blast_mand_rangers", "Blast the Mandalorian Rangers")
                .PrerequisiteQuest("war_mand_warriors")
                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_MandalorianRangers, 9)
                .SetStateJournalText("Beat up nine Mandalorian Rangers and return to Orlando Doon for your reward.")

                .AddState()
                .SetStateJournalText("You beat up nine Mandalorian Rangers. Return to Orlando Doon in Veles Colony for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(4000);
        }

        private void CoxxionInitiation()
        {
            _builder.Create("caxx_init", "Coxxion Initiation")
                .AddState()
                .SetStateJournalText("Denam Reyholm has instructed you to locate someone in Veles Colony. He doesn't know the person's real name or what he looks like. All he could tell you is that he goes by \"L\" and he's somewhere in the colony. Speak to him and speak the code phrases.")

                .AddState()
                .SetStateJournalText("You located \"L\", gave the appropriate pass phrases and he gave you an old tome. Return the tome to Denam Reyholm and let him know what happened.")

                .AddGoldReward(1125)
                .AddXPReward(4000)

                .OnAcceptAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "FF65A192706B40A6A97474B935796B82", VisibilityType.Visible);
                })

                .OnAbandonAction(player =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "FF65A192706B40A6A97474B935796B82", VisibilityType.Hidden);
                })

                .OnAdvanceAction((player, sourceObject, state) =>
                {
                    ObjectVisibility.AdjustVisibility(player, sourceObject, VisibilityType.Hidden);
                })

                .OnCompleteAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "D4C44145731048F1B7DA23D974E59FCE", VisibilityType.Visible);
                });
        }

        private void WeaponsForKrystalle()
        {
            _builder.Create("daggers_crystal", "Weapons for Krystalle")
                .AddState()
                .SetStateJournalText("Krystalle in Veles Colony needs two basic spears and three basic pistols. Collect them and return them to her.")
                .AddCollectItemObjective("b_pistol", 3)
                .AddCollectItemObjective("b_spear", 2)

                .AddState()
                .SetStateJournalText("You delivered the spears and pistols to Krystalle. Talk to her for your reward.")

                .AddXPReward(4000)
                .AddItemReward("p_crystal_red_qs", 1);
        }

        private void FindCaptainNguth()
        {
            _builder.Create("find_cap_nguth", "Find Captain N'Guth")
                .PrerequisiteQuest("locate_m_fac")
                .AddState()
                .SetStateJournalText("Tal'gar needs you to find Captain N'guth, who he sent out to the Wildwoods in search of the Mandalorian facility. Find him and bring him back to Veles Colony.")

                .AddState()
                .SetStateJournalText("You found the remains of Captain N'guth. Return to Tal'gar in Veles Colony to report.")

                .AddGoldReward(2250)
                .AddXPReward(4000)

                .OnAcceptAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "A61BB617B2D34E2F863C6301A4A04143", VisibilityType.Visible);
                })

                .OnAbandonAction(player =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "A61BB617B2D34E2F863C6301A4A04143", VisibilityType.Hidden);
                })

                .OnCompleteAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "A61BB617B2D34E2F863C6301A4A04143", VisibilityType.Hidden);
                });
        }
        //todo: review the first rites quest.
        /// <summary>
        /// When a force crystal is touched, run the progression logic for the First Rites quest.
        /// </summary>
        [NWNEventHandler(ScriptName.OnQuestForceCrystal)]
        public static void FirstRitesForceCrystal()
        {
            const string InactiveQuestText = "The crystal glows quietly...";
            var player = GetLastUsedBy();
            // Not a player.
            if (!GetIsPC(player) || GetIsDM(player))
            {
                SendMessageToPC(player, InactiveQuestText);
                return;
            }
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            // Player doesn't have this quest yet.
            if (!dbPlayer.Quests.ContainsKey("first_rites"))
            {
                SendMessageToPC(player, InactiveQuestText);
                return;
            }
            // Player is not on the appropriate state of the quest.
            var playerQuestState = dbPlayer.Quests["first_rites"];
            if (playerQuestState.CurrentState != 2)
            {
                SendMessageToPC(player, InactiveQuestText);
                return;
            }
            var quest = Quest.GetQuestById("first_rites");
            var crystal = OBJECT_SELF;
            var type = GetLocalInt(crystal, "CRYSTAL_COLOR_TYPE");
            string cluster;
            switch (type)
            {
                case 1: cluster = "c_cluster_blue"; break; // Blue
                case 2: cluster = "c_cluster_red"; break; // Red
                case 3: cluster = "c_cluster_green"; break; // Green
                case 4: cluster = "c_cluster_yellow"; break; // Yellow
                default: throw new Exception("Invalid crystal color type.");
            }
            CreateItemOnObject(cluster, player);
            quest.Advance(player, crystal);
            ObjectVisibility.AdjustVisibilityByObjectId(player, "81533EBB-2084-4C97-B004-8E1D8C395F56", VisibilityType.Hidden);
            var waypoint = GetObjectByTag("FORCE_QUEST_LANDING");
            var location = GetLocation(waypoint);
            AssignCommand(player, () => ActionJumpToLocation(location));
            // todo: unlock perk
            FloatingTextStringOnCreature("You have unlocked the Lightsaber Blueprints perk.", player, false);
        }

        private void FirstRites()
        {
            _builder.Create("first_rites", "First Rites")
                // Use object

                .AddState()
                .SetStateJournalText("Jhoren has requested you search the nearby cavern in Viscara Wildlands for a source of power and return it to him.")
                // Use object

                .AddState()
                .SetStateJournalText("Select a crystal and begin on your path towards becoming one with the Force.")

                .OnAcceptAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "81533EBB-2084-4C97-B004-8E1D8C395F56", VisibilityType.Visible);
                })

                .OnAbandonAction(player =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "81533EBB-2084-4C97-B004-8E1D8C395F56", VisibilityType.Hidden);
                })

                .OnAdvanceAction((player, sourceObject, state) =>
                {
                    ObjectVisibility.AdjustVisibility(player, sourceObject, VisibilityType.Hidden);
                });
        }

        private void HelpTheTalyronFamily()
        {
            _builder.Create("help_talyron_family", "Help the Talyron Family")
                .AddState()
                .SetStateJournalText("Reid Coxxion needs you to talk to the head of the Talyron family. Their home can be found in the southwestern part of the mountain valley. Find them, help them, and return to Reid.")

                .AddState()
                .SetStateJournalText("Tristan Talyron needs you to take down several Cairnmogs stalking around his homestead. Slay ten of them and return to him.")
                .AddKillObjective(NPCGroupType.Viscara_ValleyCairnmogs, 10)

                .AddState()
                .SetStateJournalText("You've slain ten of the Cairnmogs stalking the mountain valley. Return to Tristan Talyron to notify him the deed is done.")

                .AddState()
                .SetStateJournalText("Return to Reid Coxxion to notify him the work is done.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void KathHoundHunting()
        {
            _builder.Create("k_hound_hunting", "Kath Hound Hunting")
                .AddState()
                .SetStateJournalText("You're responsible for culling back the Kath Hound population in the Viscara Wildlands. Slay 7 of them and return to Moira Halaz in the Veles Colony for your reward.")
                .AddKillObjective(NPCGroupType.Viscara_WildlandKathHounds, 7)

                .AddState()
                .SetStateJournalText("You killed 7 Kath Hounds in the Viscara Wildlands. Return to Moira Halaz in the Veles Colony for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(3000)
                .AddItemReward("map_052", 1);
        }

        private void LocateTheMandalorianFacility()
        {
            _builder.Create("locate_m_fac", "Locate the Mandalorian Facility")
                // Enter trigger

                .AddState()
                .SetStateJournalText("There are reports of a Mandalorian facility located somewhere in the Wildwoods. Search the woods, find the facility, and report back to Tal'gar in Veles Colony.")
                // Talk to NPC

                .AddState()
                .SetStateJournalText("You found the Mandalorian facility but it's locked. Return to Tal'gar and report your findings.")

                .AddGoldReward(2250)
                .AddXPReward(2000);
        }

        private void MandalorianDogTags()
        {
            _builder.Create("mand_dog_tags", "Mandalorian Dog Tags")
                .PrerequisiteQuest("find_cap_nguth")
                .AddState()
                .AddCollectItemObjective("man_tags", 5)
                .SetStateJournalText("Defeat Mandalorian raiders and return five of their dog tags to Irene Colsstaad in Veles Colony.")

                .AddState()
                .SetStateJournalText("Speak to Irene Colsstaad for your reward.")

                .AddXPReward(4000)
                .AddGoldReward(2625);
        }

        private void RepairingCoxxionEquipment()
        {
            _builder.Create("caxx_repair", "Repairing Coxxion Equipment")
                .AddState()
                .AddCollectItemObjective("fiberp_ruined", 2)
                .AddCollectItemObjective("elec_ruined", 2)
                .AddCollectItemObjective("jade", 1)
                .SetStateJournalText("Farah Oersted needs you to collect the following items: Ruined Electronics, Ruined Fiberplast, and Jade. Gather them and give them to her for your reward.")

                .AddGoldReward(12000)
                .AddXPReward(6000);
        }

        private void SlicingTheMandalorianFacility()
        {
            void AdjustVisibility(uint player, VisibilityType type)
            {
                string[] visibilityObjectIDs =
                {
                    "C1888BC5BBBC45F28B40293D7C6E76EC",
                    "C3F31641D4F34D6AAEA51295CBE9014D",
                    "6FABDF6EDF4F47A4A9684E6224700A78",
                    "5B56B9EF160D4B078E28C775723BA95F",
                    "141D32140AA847B18AD5896C82223C8D",
                    "B0839B0F597140EEAEC567C22FFD1A86"
                };
                foreach (var objId in visibilityObjectIDs)
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, objId, type);
                }
            }
            _builder.Create("mandalorian_slicing", "Slicing the Mandalorian Facility")
                .PrerequisiteQuest("war_mand_warriors")
                .PrerequisiteQuest("blast_mand_rangers")
                // Use object

                .AddState()
                .SetStateJournalText("Harry Mason needs you to slice six terminals found in the Mandalorian Facility. Obtain the data from each of the terminals and return them to him.")

                .AddGoldReward(4125)
                .AddXPReward(6000)

                .OnAcceptAction((player, sourceObject) =>
                {
                    AdjustVisibility(player, VisibilityType.Visible);
                })

                .OnAbandonAction(player =>
                {
                    AdjustVisibility(player, VisibilityType.Hidden);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc1);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc2);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc3);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc4);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc5);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc6);
                })

                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc1);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc2);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc3);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc4);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc5);
                    KeyItem.RemoveKeyItem(player, KeyItemType.DataDisc6);
                });
        }

        private void SmuggleRoyMossPackage()
        {
            _builder.Create("smuggle_roy_moss", "Smuggle Roy Moss's Package")
                .AddState()
                .SetStateJournalText("Roy Moss gave you a less-than-legal package to deliver to Denam Reyholm. He can be found out in the mountain region of Viscara, near an old refinery.")

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.PackageForDenamReyholm);
                })

                .OnAbandonAction(player =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.PackageForDenamReyholm);
                })

                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.PackageForDenamReyholm);
                });
        }

        private void StuffKeepsBreaking()
        {
            _builder.Create("caxx_repair_2", "Stuff Keeps Breaking!")
                .AddState()
                .SetStateJournalText("Farah Oersted needs you to collect the following items: Flawed Leather, Flawed Electronics, and Agate. Gather them and give them to her for your reward.")
                .AddCollectItemObjective("lth_flawed", 2)
                .AddCollectItemObjective("elec_flawed", 2)
                .AddCollectItemObjective("agate", 1)

                .AddGoldReward(12000)
                .AddXPReward(8000);
        }

        private void TheMandalorianLeader()
        {
            _builder.Create("the_manda_leader", "The Mandalorian Leader")
                .PrerequisiteQuest("find_cap_nguth")
                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_MandalorianLeader, 1)
                .SetStateJournalText("Tal'gar wants you to avenge Captain N'guth's death. Enter the Mandalorian facility, kill the War Hero, and report back to him when it's done.")

                .AddState()
                .SetStateJournalText("You found and killed the Mandalorian War Hero. Return to Tal'gar to report.")
                .HasRewardSelection()

                .AddGoldReward(3000, false)
                .AddXPReward(6000, false)
                .AddItemReward("cap_longsword", 1)
                .AddItemReward("cap_knife", 1)
                .AddItemReward("cap_gswd", 1)
                .AddItemReward("cap_spear", 1)
                .AddItemReward("cap_katar", 1)
                .AddItemReward("cap_staff", 1)
                .AddItemReward("cap_pistol", 1)
                .AddItemReward("cap_shuriken", 1)
                .AddItemReward("cap_twinblade", 1)
                .AddItemReward("cap_rifle", 1)
                .AddItemReward("cap_sabstaff", 1)
                .AddItemReward("cap_eblade", 1)

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.MandalorianFacilityKey);
                })

                .OnAbandonAction(player =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.MandalorianFacilityKey);
                });
        }

        private void VanquishTheVellenRaiders()
        {
            _builder.Create("vanquish_vellen", "Vanquish the Vellen Raiders")
                .PrerequisiteQuest("help_talyron_family")
                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_VellenFleshleader, 1)
                .SetStateJournalText("Infiltrate the Coxxion base and drive the raiders out of it. Return to Reid Coxxion when the work is done.")

                .AddState()
                .SetStateJournalText("You defeated the Coxxion Fleshleader. Return to Reid Coxxion to finish the job.")

                .AddGoldReward(11250)
                .AddXPReward(12000)

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CoxxionBaseKey);
                })

                .OnAbandonAction(player =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.CoxxionBaseKey);
                });
        }

        private void WarWithTheMandalorianWarriors()
        {
            _builder.Create("war_mand_warriors", "War With the Mandalorian Warriors")
                .PrerequisiteQuest("find_cap_nguth")
                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_MandalorianWarriors, 9)
                .SetStateJournalText("Beat up nine Mandalorian Warriors and return to Orlando Doon for your reward.")

                .AddState()
                .SetStateJournalText("You beat up nine Mandalorian Warriors. Return to Orlando Doon in Veles Colony for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(8000);
        }

        private void KathHoundPartCollection()
        {
            _builder.Create("k_hound_parts", "Kath Hound Part Collection")
                .AddState()
                .SetStateJournalText("Szaan in Veles Colony needs five units of Kath Hound Teeth and five units of Kath Hound Fur. Return to him with these items to collect your reward.")
                .AddCollectItemObjective("k_hound_tooth", 5)
                .AddCollectItemObjective("k_hound_fur", 5)

                .AddState()
                .SetStateJournalText("Speak to Szaan in Veles Colony to retrieve your reward.")

                .AddGoldReward(4500)
                .AddXPReward(4000);
        }

        private void TaxiTerminalRepairs()
        {
            _builder.Create("taxi_term_repairs", "Taxi Terminal Repairs")
                .AddState()
                .SetStateJournalText(
                    "Dessta Bocktorb needs twenty flawed electronics and five units of agate to repair the taxi terminals around Veles Colony. Return to her with these items to collect your reward.")
                .AddCollectItemObjective("elec_flawed", 20)
                .AddCollectItemObjective("agate", 5)

                .AddState()
                .SetStateJournalText("Speak to Dessta Bocktorb for your reward.")

                .AddKeyItemReward(KeyItemType.TaxiHailingDevice);
        }

        private void JoiningTheRepublic()
        {
            _builder.Create("joining_the_republic", "Joining the Republic")
                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_DeepMountainRaivors, 10)
                .SetStateJournalText("Lieutenant Marbury Grant has instructed you to thin out the raivors that roam the Viscaran mountains to prove yourself worthy of enlisting in the Republic's Special Forces.")

                .AddState()
                .SetStateJournalText("You have hunted down ten raivors. Return to the Lieutenant to continue your path towards enlisting as a soldier of the Republic.")

                .AddState()
                .SetStateJournalText("Lieutenant Marbury Grant has instructed you to speak to a training droid located in the mess hall at Outpost Hope.")

                .AddState()
                .SetStateJournalText("You have completed the Lieutenant's test. Return to the Lieutenant to continue your path towards enlisting as a soldier of the Republic.")

                .AddState()
                .SetStateJournalText("Lieutenant Marbury Grant has instructed you to speak to Sergeant Nahulu, who awaits you are the parade square of Outpost Hope to reaffirm your oath of allegiance.")

                .AddState()
                .SetStateJournalText("You have reaffirmed your oath of allegiance to the Republic and the Senate. Return to the Lieutenant and conclude your enlistment as a soldier of the Republic.")

                .AddXPReward(10000)
                .AddGoldReward(3000)
                .AddItemReward("key_rep_01", 1);
        }

        private void MedicalEquipmentForShelby()
        {
            _builder.Create("medical_equipget", "Medical Equipment for Shelby")
                .AddState()
                .SetStateJournalText("Nurse Shelby in Veles Medical Center needs ten new medical beds and the schematics for a new medical center.")
                .AddCollectItemObjective("structure_0137", 10)
                .AddCollectItemObjective("structure_5002", 1)

                .AddState()
                .SetStateJournalText("Well done! You've gathered what Shelby needed. Make sure you talk to her for a reward.")

                .AddGoldReward(7500)
                .AddXPReward(2500)
                .AddItemReward("recipe_fabmedic1", 1);
        }

        private void SpiceOneSmallFavour()
        {
            _builder.Create("spice_onesmallfavour", "Spice: One Small Favour")
                .AddState()
                .SetStateJournalText("Stephen needs you to gather some different fiberplast so he can make some cool new rags he saw in a magazine.")
                .AddCollectItemObjective("fiberp_ruined", 12)
                .AddCollectItemObjective("fiberp_flawed", 13)
                .AddCollectItemObjective("fiberp_good", 21)
                .AddCollectItemObjective("fiberp_imperfect", 19)

                .AddState()
                .SetStateJournalText("Fiberplast handed over - looks like he'll get to wear those new rags now!")

                .AddState()
                .SetStateJournalText("Now Stephen needs you to get him some electronics so that he can make a sick new electric guitar.")
                .AddCollectItemObjective("elec_ruined", 12)
                .AddCollectItemObjective("elec_flawed", 19)
                .AddCollectItemObjective("elec_good", 24)
                .AddCollectItemObjective("elec_imperfect", 17)

                .AddState()
                .SetStateJournalText("You gave him the electronics, but you're not sure if he even has power down here.")

                .AddState()
                .SetStateJournalText("Looks like he now wants some different woods so he can build his ma a new rocking chair.")
                .AddCollectItemObjective("wood", 15)
                .AddCollectItemObjective("fine_wood", 15)
                .AddCollectItemObjective("ancient_wood", 21)
                .AddCollectItemObjective("aracia_wood", 18)

                .AddState()
                .SetStateJournalText("All the wood has been given to him. Kind of cute that he wants to make a chair for his ma.")

                .AddState()
                .SetStateJournalText("After hearing the thugs in the sewers talk about the Viscaran air being poison, Stephen wants some different meats to make a protein shake.")
                .AddCollectItemObjective("kath_meat_1", 6)
                .AddCollectItemObjective("aradile_meat", 9)
                .AddCollectItemObjective("tiger_meat", 5)
                .AddCollectItemObjective("wompratmeat", 3)

                .AddState()
                .SetStateJournalText("Interesting conspiracy, but you've handed the meat over. Just be glad you don't have to drink that.")

                .AddState()
                .SetStateJournalText("Apparently, Stephen likes to have picnics on the surface near the entrance to the Colony and now wants some 'rocks' to hold his blanket.")
                .AddCollectItemObjective("raw_veldite", 15)
                .AddCollectItemObjective("raw_scordspar", 25)
                .AddCollectItemObjective("raw_plagionite", 19)
                .AddCollectItemObjective("raw_keromber", 27)

                .AddState()
                .SetStateJournalText("This is just getting weird... But at least his picnic blanket won't move around now.")

                .AddState()
                .SetStateJournalText("He hasn't even made the electric guitar, but now he has asked for some different herbs to help with his jam sessions.")
                .AddCollectItemObjective("herb_v", 25)
                .AddCollectItemObjective("herb_c", 14)
                .AddCollectItemObjective("herb_t", 21)
                .AddCollectItemObjective("herb_x", 36)

                .AddState()
                .SetStateJournalText("Not sure that's what he's going to use the herbs for, but who are you to judge?")

                .AddState()
                .SetStateJournalText("This is going too far now. Apparently, his 'home' needs some decorations. Grab these creature pieces and see what happens.")
                .AddCollectItemObjective("raivor_tail_bone", 6)
                .AddCollectItemObjective("scorch_chitin", 4)
                .AddCollectItemObjective("sandswimmerh", 7)
                .AddCollectItemObjective("tusken_bones", 11)

                .AddState()
                .SetStateJournalText("He does know he lives in the sewers, right? Oh well, you've handed them over.")

                .AddState()
                .SetStateJournalText("Stephen recently heard some people talking about a big shiny shard from Dathomir, and now he wants one...")
                .AddCollectItemObjective("chiro_shard", 1)

                .AddState()
                .SetStateJournalText("That's the last one. Chiro shards are not easy to come by, so he better give you something for all of this now.")

                .AddGoldReward(37500)
                .AddXPReward(25000)
                .AddItemReward("recipe_fabswoop1", 1);
        }

        private void DantooineHerbs()
        {
            _builder.Create("dantooine_herbs", "Collect Dantooine Starwort Herbs")
                .AddState()
                .SetStateJournalText("Collect 20 Dantooine Starwort Herbs and bring them to Doc Joe in Veles Colony.")
                .AddCollectItemObjective("dant_starwort", 20)

                .AddState()
                .SetStateJournalText("You have collected 20 Dantooine Starwort Herbs. Return to Doc Joe in Veles Colony for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(4000);
        }

        private void RouteLedger()
        {
            _builder.Create("visc_route_ledger", "Route Ledger")
                .AddState()
                .SetStateJournalText("Lysa Harn needs Fen Dral to mark unsafe supply paths before the next refugee cart leaves Veles Colony. Take her route ledger to Fen in the Veles cantina.")

                .AddState()
                .SetStateJournalText("Fen Dral marked Lysa's route ledger with the current hazards. Return the ledger to Lysa Harn in Veles Colony.")

                .AddGoldReward(500)
                .AddXPReward(1000);
        }

        private void MarkerCodes()
        {
            _builder.Create("visc_marker_codes", "Marker Codes")
                .PrerequisiteQuest("visc_route_ledger")
                .AddState()
                .SetStateJournalText("Fen Dral needs Tavia Orell to update the runner code sheet before route markers are moved. Bring Fen's marker codes to Tavia near the Veles starport.")

                .AddState()
                .SetStateJournalText("Tavia Orell added the runner notes to Fen's marker codes. Return the updated sheet to Fen Dral in the Veles cantina.")

                .AddGoldReward(750)
                .AddXPReward(1250);
        }

        private void RunnerManifest()
        {
            _builder.Create("visc_runner_manifest", "Runner Manifest")
                .PrerequisiteQuest("visc_marker_codes")
                .AddState()
                .SetStateJournalText("Tavia Orell needs supply counts before she can assign the next refugee runners. Ask Sella Morn in the Veles shops which field kits are ready.")

                .AddState()
                .SetStateJournalText("Sella Morn gave you the field kit count. Return the manifest update to Tavia Orell near the Veles starport.")

                .AddGoldReward(1000)
                .AddXPReward(1500);
        }

        private void BurrowSurvey()
        {
            _builder.Create("visc_burrow_survey", "Burrow Survey")
                .AddState()
                .SetStateJournalText("Nold Bren is worried the next heavy cart will cross weak ground near the Wildwoods entrance. Ask Lysa Harn in Veles Colony when the cart is expected.")

                .AddState()
                .SetStateJournalText("Lysa Harn gave you the cart schedule. Return to Nold Bren near the Wildwoods entrance so he can mark the unsafe ground.")

                .AddGoldReward(750)
                .AddXPReward(1250);
        }

        private void FieldDressings()
        {
            _builder.Create("visc_field_dressings", "Field Dressings")
                .PrerequisiteQuest("visc_runner_manifest")
                .AddState()
                .SetStateJournalText("Sella Morn has enough kits for only part of the next runner wave. Ask Tavia Orell near the Veles starport which routes should receive the first field dressings.")

                .AddState()
                .SetStateJournalText("Tavia Orell prioritized the runner routes for Sella's field dressings. Return the priority list to Sella Morn in the Veles shops.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void CacheCipher()
        {
            _builder.Create("visc_cache_cipher", "Cache Cipher")
                .PrerequisiteQuest("find_cap_nguth")
                .PrerequisiteQuest("visc_marker_codes")
                .AddState()
                .SetStateJournalText("Jorren Kade found Mandalorian field marks that may point to a minor cache, but he needs Fen Dral's route code notes to separate patrol marks from supply marks. Ask Fen Dral in the Veles cantina to compare the cipher.")

                .AddState()
                .SetStateJournalText("Fen Dral matched the cache cipher against his route codes. Return the decoded notes to Jorren Kade in Veles Colony.")

                .AddGoldReward(3000)
                .AddXPReward(3000)
                .AddItemReward("visc_kara_sig", 1);
        }

        private void ViscaraColonyLedgers()
        {
            _builder.Create("visc_colony_ledgers", "Colony Ledgers")
                .AddState()
                .SetStateJournalText("Mara Veyne asked you to gather lost colony ledgers from Veles Sewers. The trail points toward Veles Colony. Return to Mara Veyne when it is done.")
                .AddCollectItemObjective("qi_viscara_001", 1)

                .AddState()
                .SetStateJournalText("Return to Mara Veyne for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(4000);
        }

        private void ViscaraSewerGrates()
        {
            _builder.Create("visc_sewer_grates", "Under the Grates")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Brel Narsk asked you to deal with Viscara outlaws in Veles Sewers. The trail points toward the Veles sheriff's office. Return to Brel Narsk when it is done.")
                .AddKillObjective(NPCGroupType.Viscara_WildwoodsOutlaws, 8)

                .AddState()
                .SetStateJournalText("Return to Brel Narsk for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1500);
        }

        private void ViscaraGeneratorSplice()
        {
            _builder.Create("visc_generator_splice", "Generator Splice")
                .AddState()
                .SetStateJournalText("Ivo Rennik asked you to gather fuse cells from Czerka Archives and return to Veles. The trail points toward the Czerka tower. Return to Ivo Rennik when it is done.")
                .AddCollectItemObjective("qi_viscara_002", 1)

                .AddState()
                .SetStateJournalText("Return to Ivo Rennik for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(4000);
        }

        private void ViscaraSwampMold()
        {
            _builder.Create("visc_swamp_mold", "The Mold That Bites")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Helna Quist asked you to gather swamp mold from Eastern Swamplands. The trail points toward the Veles general store. Return to Helna Quist when it is done.")
                .AddCollectItemObjective("qi_viscara_003", 1)

                .AddState()
                .SetStateJournalText("Return to Helna Quist for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1500);
        }

        private void ViscaraColdTrail()
        {
            _builder.Create("visc_cold_trail", "The Cold Trail")
                .AddState()
                .SetStateJournalText("Sheriff Dorran Vale asked you to use three tracking markers in Viscara Wildlands. The trail points toward the Veles sheriff's office. Return to Sheriff Dorran Vale when it is done.")

                .AddState()
                .SetStateJournalText("Return to Sheriff Dorran Vale for your reward.")

                .AddGoldReward(1125)
                .AddXPReward(2000);
        }

        private void ViscaraLakeSurvey()
        {
            _builder.Create("visc_lake_survey", "Lake Survey")
                .AddState()
                .SetStateJournalText("Arin Pell asked you to gather water samples around Viscara Lake. Return to Arin Pell when it is done.")
                .AddCollectItemObjective("qi_viscara_004", 1)

                .AddState()
                .SetStateJournalText("Return to Arin Pell for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(4000);
        }

        private void ViscaraRangerTags()
        {
            _builder.Create("visc_ranger_tags", "Ranger Tags")
                .AddState()
                .SetStateJournalText("Orla Senn asked you to gather ranger tags from Mandalorian Rangers. The trail points toward Veles Colony. Return to Orla Senn when it is done.")
                .AddCollectItemObjective("man_tags", 3)

                .AddState()
                .SetStateJournalText("Return to Orla Senn for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(4000);
        }

        private void ViscaraDeepwoodsCourier()
        {
            _builder.Create("visc_deepwoods_courier", "Deepwoods Courier")
                .AddState()
                .SetStateJournalText("Petyr Rane asked you to activate courier beacons in the Deepwoods. The trail points toward the Veles interior. Return to Petyr Rane when it is done.")

                .AddState()
                .SetStateJournalText("Return to Petyr Rane for your reward.")

                .AddGoldReward(1125)
                .AddXPReward(2000);
        }

        private void ViscaraSwampBurners()
        {
            _builder.Create("visc_swamp_burners", "Swamp Burners")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Ged Marko asked you to deal with Vellen Flesheaters in the swamplands. The trail points toward the northern swamp. Return to Ged Marko when it is done.")
                .AddKillObjective(NPCGroupType.Viscara_VellenFlesheater, 8)

                .AddState()
                .SetStateJournalText("Return to Ged Marko for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1500);
        }

        private void ViscaraFleshleaderReport()
        {
            _builder.Create("visc_fleshleader_report", "Fleshleader Report")
                .AddState()
                .SetStateJournalText("Kala Ordo asked you to deal with a Vellen Fleshleader and return its orders. The trail points toward the Coxxion base. Return to Kala Ordo when it is done.")
                .AddKillObjective(NPCGroupType.Viscara_VellenFleshleader, 1)

                .AddState()
                .SetStateJournalText("Return to Kala Ordo for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void ViscaraRaivorRidge()
        {
            _builder.Create("visc_raivor_ridge", "Raivor Ridge")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Enna Vor asked you to deal with Deep Mountain Raivors. The trail points toward the deep mountains. Return to Enna Vor when it is done.")
                .AddKillObjective(NPCGroupType.Viscara_DeepMountainRaivors, 8)

                .AddState()
                .SetStateJournalText("Return to Enna Vor for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1500);
        }

        private void ViscaraSpiderVenom()
        {
            _builder.Create("visc_spider_venom", "Crystal Spider Venom")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Dr. Reni Soth asked you to gather venom from Crystal Spiders. The trail points toward the Czerka tower. Return to Dr. Reni Soth when it is done.")
                .AddKillObjective(NPCGroupType.Viscara_CrystalSpider, 8)
                .AddCollectItemObjective("qi_viscara_005", 1)

                .AddState()
                .SetStateJournalText("Return to Dr. Reni Soth for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1500);
        }

        private void ViscaraLakePrisms()
        {
            _builder.Create("visc_lake_prisms", "Lake Prisms")
                .AddState()
                .SetStateJournalText("Olan Treth asked you to recover prism fragments around Lake Grounds. Return to Olan Treth when it is done.")
                .AddCollectItemObjective("qi_viscara_006", 1)

                .AddState()
                .SetStateJournalText("Return to Olan Treth for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(4000);
        }

        private void ViscaraJediRecords()
        {
            _builder.Create("visc_jedi_records", "Records in the Roots")
                .AddState()
                .SetStateJournalText("Sera Vaal asked you to inspect damaged Jedi record stones. The trail points toward the Jedi grounds. Return to Sera Vaal when it is done.")

                .AddState()
                .SetStateJournalText("Return to Sera Vaal for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(6000)
                .AddItemReward("visc_jedi_dat", 1);
        }

        private void ViscaraArchiveKeys()
        {
            _builder.Create("visc_archive_keys", "Archive Keys")
                .AddState()
                .SetStateJournalText("Paxon Mire asked you to recover Czerka archive keys. The trail points toward the Viscara archive. Return to Paxon Mire when it is done.")
                .AddCollectItemObjective("qi_viscara_007", 1)

                .AddState()
                .SetStateJournalText("Return to Paxon Mire for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(4000);
        }

        private void ViscaraGardenSoil()
        {
            _builder.Create("visc_garden_soil", "Rest Garden Soil")
                .AddState()
                .SetStateJournalText("Mena Rest asked you to gather soil samples from Rest's Public Gardens and Lake Grounds. Return to Mena Rest when it is done.")
                .AddCollectItemObjective("qi_viscara_008", 1)

                .AddState()
                .SetStateJournalText("Return to Mena Rest for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(4000);
        }

        private void ViscaraManifestGap()
        {
            _builder.Create("visc_manifest_gap", "The Manifest Gap")
                .AddState()
                .SetStateJournalText("Corel Ith asked you to recover passenger manifests from Veles Starport. The trail points toward the Veles interior. Return to Corel Ith when it is done.")
                .AddCollectItemObjective("qi_viscara_009", 1)

                .AddState()
                .SetStateJournalText("Return to Corel Ith for your reward.")

                .AddGoldReward(2625)
                .AddXPReward(4000);
        }

        private void ViscaraMerchantEscort()
        {
            _builder.Create("visc_merchant_escort", "Merchant Escort")
                .AddState()
                .SetStateJournalText("Varro Bex asked you to escort route by activating markers between Veles and Wildwoods. The trail points toward the Veles market. Return to Varro Bex when it is done.")

                .AddState()
                .SetStateJournalText("Return to Varro Bex for your reward.")

                .AddGoldReward(1125)
                .AddXPReward(2000);
        }

        private void ViscaraRepublicShortfall()
        {
            _builder.Create("visc_republic_shortfall", "Republic Shortfall")
                .AddState()
                .SetStateJournalText("Lt. Nara Pell asked you to gather supply crates from Wildlands wreckage. The trail points toward the Republic base exterior. Return to Lt. Nara Pell when it is done.")
                .AddCollectItemObjective("qi_viscara_010", 1)

                .AddState()
                .SetStateJournalText("Return to Lt. Nara Pell for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void ViscaraCoxxionRumors()
        {
            _builder.Create("visc_coxxion_rumors", "Coxxion Rumors")
                .AddState()
                .SetStateJournalText("Halen Vox asked you to speak to three informants and return to Halen. The trail points toward the Veles cantina. Return to Halen Vox when it is done.")

                .AddState()
                .SetStateJournalText("Return to Halen Vox for your reward.")

                .AddGoldReward(1125)
                .AddXPReward(2000);
        }

        private void ViscaraHiddenRelay()
        {
            _builder.Create("visc_hidden_relay", "Hidden Relay")
                .AddState()
                .SetStateJournalText("Tessa Kord asked you to repair a hidden comm relay in the Deepwoods. Return to Tessa Kord when it is done.")

                .AddState()
                .SetStateJournalText("Return to Tessa Kord for your reward.")

                .AddGoldReward(1125)
                .AddXPReward(2000);
        }

        private void ViscaraNashtahWatch()
        {
            _builder.Create("visc_nashtah_watch", "Nashtah Watch")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Rell Torvik asked you to deal with Nashtah in Mountain Valley. The trail points toward the western wildlands. Return to Rell Torvik when it is done.")
                .AddKillObjective(NPCGroupType.Viscara_ValleyNashtah, 8)

                .AddState()
                .SetStateJournalText("Return to Rell Torvik for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1500);
        }

        private void ViscaraScoutMaps()
        {
            _builder.Create("visc_scout_maps", "Scout Maps")
                .AddState()
                .SetStateJournalText("Vera Odain asked you to recover scout maps from Mandalorian Scouts. The trail points toward Veles Colony. Return to Vera Odain when it is done.")
                .AddCollectItemObjective("qi_viscara_011", 1)

                .AddState()
                .SetStateJournalText("Return to Vera Odain for your reward.")

                .AddGoldReward(1125)
                .AddXPReward(2000);
        }

        private void ViscaraSignalMountain()
        {
            _builder.Create("visc_signal_mountain", "Signal on the Mountain")
                .AddState()
                .SetStateJournalText("Kiran Sol asked you to use signal equipment after clearing Raivors. The trail points toward the deep mountains. Return to Kiran Sol when it is done.")
                .AddKillObjective(NPCGroupType.Viscara_DeepMountainRaivors, 6)

                .AddState()
                .SetStateJournalText("Return to Kiran Sol for your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("visc_sig_core", 1);
        }
    }
}
