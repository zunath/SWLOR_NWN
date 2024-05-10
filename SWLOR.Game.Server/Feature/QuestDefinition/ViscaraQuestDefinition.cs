using System;
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

                .AddGoldReward(1000)
                .AddXPReward(4000);
        }

        private void CoxxionInitiation()
        {
            _builder.Create("caxx_init", "Coxxion Initiation")

                .AddState()
                .SetStateJournalText("Denam Reyholm has instructed you to locate someone in Veles Colony. He doesn't know the person's real name or what he looks like. All he could tell you is that he goes by \"L\" and he's somewhere in the colony. Speak to him and speak the code phrases.")

                .AddState()
                .SetStateJournalText("You located \"L\", gave the appropriate pass phrases and he gave you an old tome. Return the tome to Denam Reyholm and let him know what happened.")

                .AddGoldReward(750)
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

                .AddGoldReward(1500)
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
        [NWNEventHandler("qst_force_crys")]
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

                .AddGoldReward(4000)
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

                .AddGoldReward(1750)
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
                
                .AddGoldReward(1500)
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
                .AddGoldReward(1750);
        }

        private void RepairingCoxxionEquipment()
        {
            _builder.Create("caxx_repair", "Repairing Coxxion Equipment")

                .AddState()
                .AddCollectItemObjective("fiberp_ruined", 2)
                .AddCollectItemObjective("elec_ruined", 2)
                .AddCollectItemObjective("jade", 1)
                .SetStateJournalText("Farah Oersted needs you to collect the following items: Ruined Electronics, Ruined Fiberplast, and Jade. Gather them and give them to her for your reward.")

                .AddGoldReward(8000)
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

                .AddGoldReward(2750)
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

                .AddGoldReward(8000)
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
                .AddGoldReward(2000, false)
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

                .AddGoldReward(7500)
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

                .AddGoldReward(1000)
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

                .AddGoldReward(3000)
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
                .AddGoldReward(2000)
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
                .SetStateJournalText("Well done, you've gathered what Shelby needed. Make sure you talk to her for a reward.")

                .AddGoldReward(5000)
                .AddXPReward(2500)
                .AddItemReward("recipe_fabmedic1", 1);
        }

        private void SpiceOneSmallFavour()
        {
            _builder.Create("spice_onesmallfavour", "Spice: One Small Favour")

                .AddState()
                .SetStateJournalText("Stephen needs you to gather some different fiberplast so he can make some cool new rags he saw in a Magazine.")
                .AddCollectItemObjective("fiberp_ruined", 12)
                .AddCollectItemObjective("fiberp_flawed", 13)
                .AddCollectItemObjective("fiberp_good", 21)
                .AddCollectItemObjective("fiberp_imperfect", 19)

                .AddState()
                .SetStateJournalText("Fiberplast handed over - Looks like he'll get to wear those new rags now!")

                .AddState()
                .SetStateJournalText("Now Stephen needs you to get him some electronics so that he can make a sick new electric guitar.")
                .AddCollectItemObjective("elec_ruined", 12)
                .AddCollectItemObjective("elec_flawed", 19)
                .AddCollectItemObjective("elec_good", 24)
                .AddCollectItemObjective("elec_imperfect", 17)

                .AddState()
                .SetStateJournalText("You gave him the electronics, but not sure if he even has power down here.")

                .AddState()
                .SetStateJournalText("Looks like he now wants some different woods so he can build his Ma a new rocking chair.")
                .AddCollectItemObjective("wood", 15)
                .AddCollectItemObjective("fine_wood", 15)
                .AddCollectItemObjective("ancient_wood", 21)
                .AddCollectItemObjective("aracia_wood", 18)

                .AddState()
                .SetStateJournalText("All the wood has been given to him. Kind of cute that he wants to make a chair for his Ma.")

                .AddState()
                .SetStateJournalText("After hearing the thugs in the sewers talk about the Viscaran air being poison, Stephen wants some different meats to make a protein shake.")
                .AddCollectItemObjective("kath_meat_1", 6)
                .AddCollectItemObjective("aradile_meat", 9)
                .AddCollectItemObjective("tiger_meat", 5)
                .AddCollectItemObjective("wompratmeat", 3)

                .AddState()
                .SetStateJournalText("Interesting conspiracy, but you've handed the meat over. Just be glad you don't have to drink that.")

                .AddState()
                .SetStateJournalText("Apparently Stephen likes to have picnics on the surface, near the entrance to the Colony and now wants some 'rocks' to hold his blanket.")
                .AddCollectItemObjective("raw_veldite", 15)
                .AddCollectItemObjective("raw_scordspar", 25)
                .AddCollectItemObjective("raw_plagionite", 19)
                .AddCollectItemObjective("raw_keromber", 27)

                .AddState()
                .SetStateJournalText("This is just getting weird... But, atleast his picnic blanket won't move around now.")

                .AddState()
                .SetStateJournalText("He hasn't even made the electric guitar, but now he has asked for some different herb's to help with his jam sessions.")
                .AddCollectItemObjective("herb_v", 25)
                .AddCollectItemObjective("herb_c", 14)
                .AddCollectItemObjective("herb_t", 21)
                .AddCollectItemObjective("herb_x", 36)

                .AddState()
                .SetStateJournalText("Not sure that's what he's going to use the herbs for, but who are you to judge?")

                .AddState()
                .SetStateJournalText("This is going to far now. Apparently his 'home' needs some decorations. Grab these creature pieces and see what happens.")
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
                .SetStateJournalText("That's the last one... Chiro shards are not easy to come by, so he better give me something for all of this now.")

                .AddGoldReward(25000)
                .AddXPReward(25000)
                .AddItemReward("recipe_fabswoop1", 1);
        }
    }
}
