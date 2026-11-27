using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

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
            FieldTinctures();
            FetchPetTreat();
            CollectHerbsForLibrarian();
            HiddenCave();
            HornsOnThePlains();
            TheHiveWakes();
            GizkaEverywhere();
            Stampede();
            BreakTheHerd();
            PlainsPatrol();
            GarrisonRations();
            TheSouthFieldsHunters();
            TheSouthFieldsShamans();
            SouthFieldsPatrol();
            LizardsInTheCaves();
            TheIceBol();
            TheGraul();
            CoreSamples();
            TheThirdSeam();
            TimberRights();
            APaleHide();
            FieldKitchen();
            BreadForTheCamp();
            SomethingFresh();

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
        private void FieldTinctures()
        {
            _builder.Create("field_tinctures", "Field Tinctures")
               .PrerequisiteQuest("harvest_herbs")

               .AddState()
               .SetStateJournalText("Healer Elara has enough Starwort for the clinic, but she needs stabilizers to keep field tinctures viable. Collect three Wild Innards from Gizka and one sample of Thune Blood from a Thune, then return to her in the Dantooine Colony.")
               .AddCollectItemObjective("wild_innards", 3)
               .AddCollectItemObjective("thune_blood", 1)

               .AddState()
               .SetStateJournalText("Deliver the Wild Innards and Thune Blood to Healer Elara in the Dantooine Colony.")

               .AddXPReward(2200)
               .AddGoldReward(1500)
               .AddItemReward("med_supplies", 5)
               .AddItemReward("stim_pack", 3);
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
    

        private void HornsOnThePlains()
        {
            _builder.Create("dan_iriaz", "Horns on the Plains")

                .AddState()
                .SetStateJournalText("Wild Iriaz are goring colonists on Dantooine. Kill 10 Iriaz in the Forsaken Jungles, by the Lake or in the Canyon Crystal Fields on Dantooine, then return to Sergeant Halden Prax on the Wild Plains of Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_Iriaz, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Iriaz. Return to Sergeant Halden Prax on the Wild Plains of Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void TheHiveWakes()
        {
            _builder.Create("dan_hive", "The Hive Wakes")

                .AddState()
                .SetStateJournalText("Hive Kinrath are spreading out of the Kinrath Caves. Kill 12 Hive Kinrath in the Kinrath Caves or the Forsaken Jungles on Dantooine, then return to Sergeant Halden Prax on the Wild Plains of Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_HiveKinrath, 12)

                .AddState()
                .SetStateJournalText("You have killed 12 Hive Kinrath. Return to Sergeant Halden Prax on the Wild Plains of Dantooine for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void GizkaEverywhere()
        {
            _builder.Create("dan_gizka", "Gizka Everywhere")

                .AddState()
                .SetStateJournalText("Gizka have overrun the Ruined Farmlands. Kill 15 Gizka in the Ruined Farmlands, the Forsaken Jungles or the Janta Caves on Dantooine, then return to Sergeant Halden Prax on the Wild Plains of Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_Gizka, 15)

                .AddState()
                .SetStateJournalText("You have killed 15 Gizka. Return to Sergeant Halden Prax on the Wild Plains of Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void Stampede()
        {
            _builder.Create("dan_thune", "Stampede")

                .AddState()
                .SetStateJournalText("Plains Thune are trampling what is left of the Ruined Farmlands. Kill 10 Plains Thune in the Ruined Farmlands on Dantooine, then return to Sergeant Halden Prax on the Wild Plains of Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_PlainsThune, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Plains Thune. Return to Sergeant Halden Prax on the Wild Plains of Dantooine for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void BreakTheHerd()
        {
            _builder.Create("dan_herd_leaders", "Break the Herd")
                .PrerequisiteQuest("dan_thune")

                .AddState()
                .SetStateJournalText("Herd Leader Thune drive the stampedes on Dantooine. Kill 3 Herd Leader Thune in the Ruined Farmlands, the Forsaken Jungles or by the Lake on Dantooine, then return to Sergeant Halden Prax on the Wild Plains of Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_HerdLeaderThune, 3)

                .AddState()
                .SetStateJournalText("You have killed 3 Herd Leader Thune. Return to Sergeant Halden Prax on the Wild Plains of Dantooine for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void PlainsPatrol()
        {
            _builder.Create("dan_plains_patrol", "Plains Patrol")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The Republic garrison pays for patrols of the Dantooine plains. Kill 10 Iriaz and 10 Gizka on Dantooine, then return to Sergeant Halden Prax on the Wild Plains of Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_Iriaz, 10)
                .AddKillObjective(NPCGroupType.Dantooine_Gizka, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Iriaz and 10 Gizka. Return to Sergeant Halden Prax on the Wild Plains of Dantooine for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void GarrisonRations()
        {
            _builder.Create("dan_thune_steaks", "Garrison Rations")

                .AddState()
                .SetStateJournalText("The Republic garrison is short of rations. Collect 8 Thune Meat from the Thune of Dantooine, then bring them to Sergeant Halden Prax on the Wild Plains of Dantooine.")
                .AddCollectItemObjective("thune_meat", 8)

                .AddState()
                .SetStateJournalText("You handed over 8 Thune Meat. Speak to Sergeant Halden Prax on the Wild Plains of Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void TheSouthFieldsHunters()
        {
            _builder.Create("dan_dantari_1", "The South Fields: Hunters")

                .AddState()
                .SetStateJournalText("Dantari Hunters are ambushing colonists in the South Fields. Kill 10 Dantari Hunters in the South Fields on Dantooine, then return to Corporal Mira Tannis on the Field Trail on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_DantariHunter, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Dantari Hunters. Return to Corporal Mira Tannis on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void TheSouthFieldsShamans()
        {
            _builder.Create("dan_dantari_2", "The South Fields: Shamans")
                .PrerequisiteQuest("dan_dantari_1")

                .AddState()
                .SetStateJournalText("Dantari Shamans are rallying the hunters of the South Fields. Kill 5 Dantari Shamans in the South Fields on Dantooine, then return to Corporal Mira Tannis on the Field Trail on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_DantariShaman, 5)

                .AddState()
                .SetStateJournalText("You have killed 5 Dantari Shamans. Return to Corporal Mira Tannis on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.TheSouthFields);
                });
        }

        private void SouthFieldsPatrol()
        {
            _builder.Create("dan_south_patrol", "South Fields Patrol")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The Republic garrison pays for patrols of the South Fields. Kill 20 Dantari Hunters in the South Fields on Dantooine, then return to Corporal Mira Tannis on the Field Trail on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_DantariHunter, 20)

                .AddState()
                .SetStateJournalText("You have killed 20 Dantari Hunters. Return to Corporal Mira Tannis on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void LizardsInTheCaves()
        {
            _builder.Create("dan_voritor", "Lizards in the Caves")

                .AddState()
                .SetStateJournalText("The Janta Caves are full of Voritor Lizards and Gizka. Kill 8 Voritor Lizards and 8 Gizka in the Janta Caves on Dantooine, then return to Corporal Mira Tannis on the Field Trail on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_VoritorLizard, 8)
                .AddKillObjective(NPCGroupType.Dantooine_Gizka, 8)

                .AddState()
                .SetStateJournalText("You have killed 8 Voritor Lizards and 8 Gizka. Return to Corporal Mira Tannis on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void TheIceBol()
        {
            _builder.Create("dan_bol", "The Ice Bol")

                .AddState()
                .SetStateJournalText("A Mutated Ice Bol roams Dantooine's canyons and is not always there. Kill the Mutated Ice Bol in the Abandoned Warehouse, the Canyon River or the Canyon Crystal Caves on Dantooine, then return to Corporal Mira Tannis on the Field Trail on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_Bol, 1)

                .AddState()
                .SetStateJournalText("You have killed the Mutated Ice Bol. Return to Corporal Mira Tannis on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000);
        }

        private void TheGraul()
        {
            _builder.Create("dan_graul", "The Graul")

                .AddState()
                .SetStateJournalText("A Graul hunts Dantooine's canyons and is not always there. Kill the Graul in the Abandoned Warehouse, the Canyon River or the Canyon Crystal Caves on Dantooine, then return to Corporal Mira Tannis on the Field Trail on Dantooine.")
                .AddKillObjective(NPCGroupType.Dantooine_Graul, 1)

                .AddState()
                .SetStateJournalText("You have killed the Graul. Return to Corporal Mira Tannis on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000);
        }

        private void CoreSamples()
        {
            _builder.Create("dan_ore", "Core Samples")

                .AddState()
                .SetStateJournalText("Czerka is surveying Dantooine's ore. Collect 10 Raw Arkoxit and 10 Raw Jasioclase from the ore veins on the Wild Plains or in the Canyon Crystal Fields of Dantooine, then bring them to Anya Kell on the Field Trail on Dantooine.")
                .AddCollectItemObjective("raw_arkoxit", 10)
                .AddCollectItemObjective("raw_jasioclase", 10)

                .AddState()
                .SetStateJournalText("You handed over 10 Raw Arkoxit and 10 Raw Jasioclase. Speak to Anya Kell on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void TheThirdSeam()
        {
            _builder.Create("dan_keromber", "The Third Seam")

                .AddState()
                .SetStateJournalText("Czerka wants samples of Dantooine's Keromber. Collect 12 Raw Keromber from the ore veins on the Wild Plains or in the Canyon Crystal Fields of Dantooine, then bring them to Anya Kell on the Field Trail on Dantooine.")
                .AddCollectItemObjective("raw_keromber", 12)

                .AddState()
                .SetStateJournalText("You handed over 12 Raw Keromber. Speak to Anya Kell on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void TimberRights()
        {
            _builder.Create("dan_hyphae", "Timber Rights")

                .AddState()
                .SetStateJournalText("Czerka is assessing the hyphae trees of the South Fields. Collect 8 Hyphae Wood from the hyphae trees of the South Fields on Dantooine, then bring them to Anya Kell on the Field Trail on Dantooine.")
                .AddCollectItemObjective("hyphae_wood", 8)

                .AddState()
                .SetStateJournalText("You handed over 8 Hyphae Wood. Speak to Anya Kell on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void APaleHide()
        {
            _builder.Create("dan_bol_leather", "A Pale Hide")

                .AddState()
                .SetStateJournalText("Czerka's lab wants hide from the Mutated Ice Bol. Collect 2 Bol Leather from the Mutated Ice Bol of Dantooine's canyons, then bring them to Anya Kell on the Field Trail on Dantooine.")
                .AddCollectItemObjective("bol_leather", 2)

                .AddState()
                .SetStateJournalText("You handed over 2 Bol Leather. Speak to Anya Kell on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void FieldKitchen()
        {
            _builder.Create("dan_carrots", "Field Kitchen")

                .AddState()
                .SetStateJournalText("The Czerka survey camp is out of fresh food. Collect 6 Carrots and 3 Cultured Butter from the Dantari Hunters of the South Fields, then bring them to Anya Kell on the Field Trail on Dantooine.")
                .AddCollectItemObjective("carrot", 6)
                .AddCollectItemObjective("cultured_butter", 3)

                .AddState()
                .SetStateJournalText("You handed over 6 Carrots and 3 Cultured Butter. Speak to Anya Kell on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void BreadForTheCamp()
        {
            _builder.Create("dan_flour", "Bread for the Camp")

                .AddState()
                .SetStateJournalText("The Czerka survey camp wants to bake bread. Collect 6 Bread Flour from the Hive Kinrath of Dantooine, then bring them to Anya Kell on the Field Trail on Dantooine.")
                .AddCollectItemObjective("bread_flour", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 Bread Flour. Speak to Anya Kell on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void SomethingFresh()
        {
            _builder.Create("dan_tomatoes", "Something Fresh")

                .AddState()
                .SetStateJournalText("The Czerka survey camp wants Tomatoes. Collect 6 Tomatoes from the Voritor Lizards of the Janta Caves, then bring them to Anya Kell on the Field Trail on Dantooine.")
                .AddCollectItemObjective("tomato", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 Tomatoes. Speak to Anya Kell on the Field Trail on Dantooine for your reward.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }
    }
}
