using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class ViscaraQuestDefinition : IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder();
            BlastTheMandalorianRangers(builder);
            CoxxionInitiation(builder);
            DaggersForCrystal(builder);
            FindCaptainNguth(builder);
            FirstRites(builder);
            HelpTheTalyronFamily(builder);
            KathHoundHunting(builder);
            LocateTheMandalorianFacility(builder);
            MandalorianDogTags(builder);
            RepairingCoxxionEquipment(builder);
            SlicingTheMandalorianFacility(builder);
            SmuggleRoyMossPackage(builder);
            StuffKeepsBreaking(builder);
            TheMandalorianLeader(builder);
            VanquishTheVellenRaiders(builder);
            WarWithTheMandalorianWarriors(builder);
            KathHoundPartCollection(builder);
            TheAbandonedStation(builder);
            CullingThePopulace(builder);
            CullingThePopulacePartTwo(builder);
            CullingtheHerd(builder);

            return builder.Build();
        }

        private static void BlastTheMandalorianRangers(QuestBuilder builder)
        {
            builder.Create("blast_mand_rangers", "Blast the Mandalorian Rangers")
                .PrerequisiteQuest("war_mand_warriors")

                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_MandalorianRangers, 9)
                .SetStateJournalText("Beat up nine Mandalorian Rangers and return to Orlando Doon for your reward.")

                .AddState()
                .SetStateJournalText("You beat up nine Mandalorian Rangers. Return to Orlando Doon in Veles Colony for your reward.")

                .AddGoldReward(200)
                .AddItemReward("xp_tome_1", 1);
        }

        private static void CoxxionInitiation(QuestBuilder builder)
        {
            builder.Create("caxx_init", "Coxxion Initiation")

                .AddState()
                .SetStateJournalText("Denam Reyholm has instructed you to locate someone in Veles Colony. He doesn't know the person's real name or what he looks like. All he could tell you is that he goes by \"L\" and he's somewhere in the colony. Speak to him and speak the code phrases.")

                .AddState()
                .SetStateJournalText("You located \"L\", gave the appropriate pass phrases and he gave you an old tome. Return the tome to Denam Reyholm and let him know what happened.")

                .AddGoldReward(150)
                
                .OnAcceptAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "FF65A192706B40A6A97474B935796B82", VisibilityType.Visible);
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

        private static void DaggersForCrystal(QuestBuilder builder)
        {
            builder.Create("daggers_crystal", "Daggers for Crystal")

                .AddState()
                .SetStateJournalText("Crystal in Veles Colony needs five units of Basic Finesse Vibroblade D. Collect them and return them to her.")
                .AddCollectItemObjective("dagger_b", 5)

                .AddState()
                .SetStateJournalText("You delivered five units of Basic Finesse Vibroblade D to Crystal. Talk to her for your reward.")

                .AddItemReward("p_crystal_red_qs", 1);
        }

        private static void FindCaptainNguth(QuestBuilder builder)
        {
            builder.Create("find_cap_nguth", "Find Captain N'Guth")
                .PrerequisiteQuest("locate_m_fac") 

                .AddState()
                .SetStateJournalText("Tal'gar needs you to find Captain N'guth, who he sent out to the Wildwoods in search of the Mandalorian facility. Find him and bring him back to Veles Colony.")

                .AddState()
                .SetStateJournalText("You found the remains of Captain N'guth. Return to Tal'gar in Veles Colony to report.")

                .AddGoldReward(300)
                
                .OnAcceptAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "A61BB617B2D34E2F863C6301A4A04143", VisibilityType.Visible);
                });
        }

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
        
        private static void FirstRites(QuestBuilder builder)
        {
            builder.Create("first_rites", "First Rites")

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
                
                .OnAdvanceAction((player, sourceObject, state) =>
                {
                    ObjectVisibility.AdjustVisibility(player, sourceObject, VisibilityType.Hidden);
                });
        }

        private static void HelpTheTalyronFamily(QuestBuilder builder)
        {
            builder.Create("help_talyron_family", "Help the Talyron Family")

                .AddState()
                .SetStateJournalText("Reid Coxxion needs you to talk to the head of the Talyron family. Their home can be found in the southwestern part of the mountain valley. Find them, help them, and return to Reid.")

                .AddState()
                .SetStateJournalText("Tristan Talyron needs you to take down several Cairnmogs stalking around his homestead. Slay ten of them and return to him.")
                .AddKillObjective(NPCGroupType.Viscara_ValleyCairnmogs, 10)

                .AddState()
                .SetStateJournalText("You've slain ten of the Cairnmogs stalking the mountain valley. Return to Tristan Talyron to notify him the deed is done.")

                .AddState()
                .SetStateJournalText("Return to Reid Coxxion to notify him the work is done.")

                .AddGoldReward(800)
                .AddItemReward("xp_tome_2", 1);
        }

        private static void KathHoundHunting(QuestBuilder builder)
        {
            builder.Create("k_hound_hunting", "Kath Hound Hunting")

                .AddState()
                .SetStateJournalText("You're responsible for culling back the Kath Hound population in the Viscara Wildlands. Slay 7 of them and return to Moira Halaz in the Veles Colony for your reward.")
                .AddKillObjective(NPCGroupType.Viscara_WildlandKathHounds, 7)

                .AddState()
                .SetStateJournalText("You killed 7 Kath Hounds in the Viscara Wildlands. Return to Moira Halaz in the Veles Colony for your reward.")

                .AddGoldReward(350);
        }

        private static void LocateTheMandalorianFacility(QuestBuilder builder)
        {
            builder.Create("locate_m_fac", "Locate the Mandalorian Facility")

                // Enter trigger
                .AddState()
                .SetStateJournalText("There are reports of a Mandalorian facility located somewhere in the Wildwoods. Search the woods, find the facility, and report back to Tal'gar in Veles Colony.")

                // Talk to NPC
                .AddState()
                .SetStateJournalText("You found the Mandalorian facility but it's locked. Return to Tal'gar and report your findings.");
        }

        private static void MandalorianDogTags(QuestBuilder builder)
        {
            builder.Create("mand_dog_tags", "Mandalorian Dog Tags")
                .PrerequisiteQuest("find_cap_nguth") 

                .AddState()
                .AddCollectItemObjective("man_tags", 5)
                .SetStateJournalText("Defeat Mandalorian raiders and return five of their dog tags to Irene Colsstaad in Veles Colony.")

                .AddState()
                .SetStateJournalText("Speak to Irene Colsstaad for your reward.")
                
                .AddGoldReward(350);
        }

        private static void RepairingCoxxionEquipment(QuestBuilder builder)
        {
            builder.Create("caxx_repair", "Repairing Coxxion Equipment")

                .AddState()
                .AddCollectItemObjective("la_rep_1", 1)
                .AddCollectItemObjective("br_rep_1", 1)
                .AddCollectItemObjective("bp_rep_1", 1)
                .SetStateJournalText("Farah Oersted needs you to collect the following items: Light Armor Repair Kit I, Blaster Rifle Repair Kit I, and Blaster Pistol Repair Kit I. These can be created at the appropriate crafting workbench. Gather them and give them to her for your reward.")

                .AddGoldReward(800)
                .AddItemReward("xp_tome_1", 1);
        }

        private static void SlicingTheMandalorianFacility(QuestBuilder builder)
        {
            builder.Create("mandalorian_slicing", "Slicing the Mandalorian Facility")
                .PrerequisiteQuest("war_mand_warriors")
                .PrerequisiteQuest("blast_mand_rangers") 

                // Use object
                .AddState()
                .SetStateJournalText("Harry Mason needs you to slice six terminals found in the Mandalorian Facility. Obtain the data from each of the terminals and return them to him.")

                .AddGoldReward(550)
                .AddItemReward("xp_tome_1", 1)
                
                .OnAcceptAction((player, sourceObject) =>
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
                        ObjectVisibility.AdjustVisibilityByObjectId(player, objId, VisibilityType.Visible);
                    }
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

        private static void SmuggleRoyMossPackage(QuestBuilder builder)
        {
            builder.Create("smuggle_roy_moss", "Smuggle Roy Moss's Package")

                .AddState()
                .SetStateJournalText("Roy Moss gave you a less-than-legal package to deliver to Denam Reyholm. He can be found out in the mountain region of Viscara, near an old refinery.")

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.PackageForDenamReyholm);
                })
                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.PackageForDenamReyholm);
                });
        }

        private static void StuffKeepsBreaking(QuestBuilder builder)
        {
            builder.Create("caxx_repair_2", "Stuff Keeps Breaking!")

                .AddState()
                .SetStateJournalText("Farah Oersted needs you to collect the following items: Finesse Vibroblade Repair Kit I, Heavy Vibroblade Repair Kit I, and Polearm Repair Kit I. These can be created at the appropriate crafting workbench. Gather them and give them to her for your reward.")
                .AddCollectItemObjective("fv_rep_1", 1)
                .AddCollectItemObjective("hv_rep_1", 1)
                .AddCollectItemObjective("po_rep_1", 1)

                .AddGoldReward(800)
                .AddItemReward("xp_tome_1", 1);
        }

        private static void TheMandalorianLeader(QuestBuilder builder)
        {
            builder.Create("the_manda_leader", "The Mandalorian Leader")
                .PrerequisiteQuest("find_cap_nguth")

                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_MandalorianLeader, 1)
                .SetStateJournalText("Tal'gar wants you to avenge Captain N'guth's death. Enter the Mandalorian facility, kill the War Hero, and report back to him when it's done.")

                .AddState()
                .SetStateJournalText("You found and killed the Mandalorian War Hero. Return to Tal'gar to report.")

                .HasRewardSelection()
                .AddGoldReward(200, false)
                .AddItemReward("rifle_tran", 1)
                .AddItemReward("blaster_tran", 1)
                .AddItemReward("xp_tome_1", 1)
                .AddItemReward("bst_sword_tran", 1)
                .AddItemReward("twinblade_tran", 1)
                .AddItemReward("kukri_tran", 1)
                .AddItemReward("halberd_tran", 1)
                .AddItemReward("greataxe_tran", 1)
                
                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.MandalorianFacilityKey);
                });
        }

        private static void VanquishTheVellenRaiders(QuestBuilder builder)
        {
            builder.Create("vanquish_vellen", "Vanquish the Vellen Raiders")
                .PrerequisiteQuest("")

                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_VellenFleshleader, 1)
                .SetStateJournalText("Infiltrate the Coxxion base and drive the raiders out of it. Return to Reid Coxxion when the work is done.")

                .AddState()
                .SetStateJournalText("You defeated the Coxxion Fleshleader. Return to Reid Coxxion to finish the job.")

                .AddGoldReward(750)
                .AddItemReward("xp_tome_1", 1)

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CoxxionBaseKey);
                });
        }

        private static void WarWithTheMandalorianWarriors(QuestBuilder builder)
        {
            builder.Create("war_mand_warriors", "War With the Mandalorian Warriors")
                .PrerequisiteQuest("find_cap_nguth") 

                .AddState()
                .AddKillObjective(NPCGroupType.Viscara_MandalorianWarriors, 9)
                .SetStateJournalText("Beat up nine Mandalorian Warriors and return to Orlando Doon for your reward.")

                .AddState()
                .SetStateJournalText("You beat up nine Mandalorian Warriors. Return to Orlando Doon in Veles Colony for your reward.")

                .AddGoldReward(200)
                .AddItemReward("xp_tome_1", 1);
        }

        private static void KathHoundPartCollection(QuestBuilder builder)
        {
            builder.Create("k_hound_parts", "Kath Hound Part Collection")

                .AddState()
                .SetStateJournalText("Szaan in Veles Colony needs five units of Kath Hound Teeth and five units of Kath Hound Fur. Return to him with these items to collect your reward.")
                .AddCollectItemObjective("k_hound_tooth", 5)
                .AddCollectItemObjective("k_hound_fur", 5)

                .AddState()
                .SetStateJournalText("Speak to Szaan in Veles Colony to retrieve your reward.")

                .AddGoldReward(600);
        }

        private static void TheAbandonedStation(QuestBuilder builder)
        {
            builder.Create("aban_station", "The Abandoned Station")
                .PrerequisiteQuest("vanquish_vellen") 

                .AddState()
                .SetStateJournalText("Investigate the abandoned station.")
                .AddKillObjective(NPCGroupType.AbandonedStation_Boss, 1)

                .AddState()
                .SetStateJournalText("Return to Telford Brelnak to report your findings.")

                .AddGoldReward(4000);
        }

        private static void CullingThePopulace(QuestBuilder builder)
        {
            builder.Create("culling_one", "Culling The Populace")

                .AddState()
                .SetStateJournalText("I have been tasked to kill five Kath Hounds.")
                .AddKillObjective(NPCGroupType.Viscara_WildlandKathHounds, 5)

                .AddState()
                .SetStateJournalText("Return to Saroph Wildanter in Racin' Jims for your reward.")

                .AddGoldReward(10);
        }

        private static void CullingThePopulacePartTwo(QuestBuilder builder)
        { 
            builder.Create("culling_two", "Culling The Populace Part Two")
                .PrerequisiteQuest("culling_one")

                .AddState()
                .SetStateJournalText("The pay is not great but it anything to help, now it is five Warocas to kill.")
                .AddKillObjective(NPCGroupType.Viscara_WildlandsWarocas, 5)

                .AddState()
                .SetStateJournalText("I've killed the Warocas, back to Saroph with me. Maybe this will be worth it...")

                .AddGoldReward(1000);
        }

        private static void CullingtheHerd(QuestBuilder builder)
        {
            builder.Create("culling_secret_one", "Something is not quite right here...")
                .PrerequisiteQuest("culling_one")

                .AddState()
                .SetStateJournalText("Saroph tasked me to kill two Gimpassas and then some Kinrath.")
                .AddKillObjective(NPCGroupType.Viscara_WildwoodsGimpassas, 2)
                .AddKillObjective(NPCGroupType.Viscara_WildwoodsKinraths, 10)
                
                .AddState()
                .SetStateJournalText("I have gotten the kills required, now to return to Saroph.")

                .AddGoldReward(100);
        }
    }
}