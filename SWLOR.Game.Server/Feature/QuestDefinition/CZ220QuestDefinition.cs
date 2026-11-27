using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class CZ220QuestDefinition: IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder();
            SelansRequest(builder);
            SuppliesSmithery(builder);
            SuppliesScavenging(builder);
            SuppliesFabrication(builder);
            DatapadRetrieval(builder);
            MynockMayhem(builder);
            OreCollection(builder);
            RefineryTrainee(builder);
            TheMalfunctioningDroids(builder);
            TheColicoidExperiment(builder);
            ScrapMetalMonstrosity(builder);

            ToothSamples(builder);
            WingMembrane(builder);
            GalleyStock(builder);
            HidePatches(builder);
            BiteWounds(builder);
            MedicalManifest(builder);
            PestPatrol(builder);
            DroidSweep(builder);
            CircuitSalvage(builder);
            FiberplastPatches(builder);
            DeepClean(builder);
            ContainmentBreach(builder);
            ShiftReport(builder);
            CrateTimber(builder);
            Ballast(builder);
            VelditeQuota(builder);
            RefinedQuota(builder);
            Requisition(builder);
            PartsOrder(builder);
            StationHand(builder);

            return builder.Build();
        }

        private static void SelansRequest(QuestBuilder builder)
        {
            builder.Create("selan_request", "Selan's Request")

                .AddState()
                .SetStateJournalText("Selan Flembek, receptionist at CZ-220, has requested you complete three tasks around the base. \n\nSpeak to Avix Tatham, the mining coordinator, for information about retrieving ore from the mine.\n\nTalk to security officer Harlon Linth for information about trouble on the maintenance level.\n\nChat with the Crafting Terminal Droid Operator for details about supplies which need to be constructed.\n\nWhen you've obtained receipts from all three employees, return to Selan to collect your reward.")

                .AddXPReward(1000)
                .AddGoldReward(750)
                .AddKeyItemReward(KeyItemType.CZ220ShuttlePass)

                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.AvixTathamsWorkReceipt);
                    KeyItem.RemoveKeyItem(player, KeyItemType.HalronLinthsWorkReceipt);
                    KeyItem.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt);

                    var cdKey = GetPCPublicCDKey(player);
                    var dbAccount = DB.Get<Account>(cdKey) ?? new Account(cdKey);
                    dbAccount.HasCompletedTutorial = true;

                    DB.Set(dbAccount);
                });
        }

        private static void SuppliesSmithery(QuestBuilder builder)
        {
            builder.Create("cz220_smithery", "CZ-220 Supplies - Smithery")

                .AddState()
                .SetStateJournalText("The Crafting Terminal Droid operator has requested you create a single Basic Knife. You will need to purchase the \"Weapon Blueprints\" perk in order to create this item. Once you have the perk you can use any smithery terminal to make the item. You will find the necessary resources down on the maintenance level of CZ-220.")
                .AddCollectItemObjective("b_knife", 1)

                .AddState()
                .SetStateJournalText("Speak to the Crafting Terminal Droid operator for your reward.")

                .AddXPReward(200)
                .AddGoldReward(75)
                .AddKeyItemReward(KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt)

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnAbandonAction(player =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                });
        }
        private static void SuppliesScavenging(QuestBuilder builder)
        {
            builder.Create("cz220_scavenging", "CZ-220 Supplies - Scavenging")

                .AddState()
                .SetStateJournalText("The Crafting Terminal Droid operator has requested you gather ten units of scrap metal. You will need to scavenge through junk piles throughout CZ-220. When you have enough, return to the droid to collect your receipt and reward.")
                .AddCollectItemObjective("scrap_metal", 10)

                .AddState()
                .SetStateJournalText("Speak to the Crafting Terminal Droid operator for your reward.")

                .AddXPReward(200)
                .AddGoldReward(75)
                .AddKeyItemReward(KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt)

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnAbandonAction(player =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                });
        }
        private static void SuppliesFabrication(QuestBuilder builder)
        {
            builder.Create("cz220_fabrication", "CZ-220 Supplies - Fabrication")

                .AddState()
                .SetStateJournalText("The Crafting Terminal Droid operator has requested you create a simple bed roll. You will need to purchase the \"Furniture Blueprints\" perk in order to create this item. You can use any fabrication terminal to make the item. You will find the necessary resources down on the maintenance level of CZ-220.")
                .AddCollectItemObjective("structure_0085", 1)

                .AddState()
                .SetStateJournalText("Speak to the Crafting Terminal Droid operator for your reward.")

                .AddXPReward(200)
                .AddGoldReward(75)
                .AddKeyItemReward(KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt)

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnAbandonAction(player =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                });
        }

        private static void DatapadRetrieval(QuestBuilder builder)
        {
            builder.Create("datapad_retrieval", "Datapad Retrieval")

                .AddState()
                .SetStateJournalText("You found a datapad in the room containing an experimental Colicoid. It contains research details on the creature. There's no name listed anywhere in the data but you assume it belongs to someone on CZ-220. Ask around on the main level and see if it's theirs.")

                .AddXPReward(300)
                .AddGoldReward(75)

                .OnAcceptAction((player, sourceObject) =>
                {
                    ObjectVisibility.AdjustVisibility(player, sourceObject, VisibilityType.Hidden);
                })
                .OnAbandonAction(player =>
                {
                    ObjectVisibility.AdjustVisibilityByObjectId(player, "3BA0FF2E61C34FB783905E8A78236A30", VisibilityType.Visible);
                });
        }

        private static void MynockMayhem(QuestBuilder builder)
        {
            builder.Create("mynock_mayhem", "Mynock Mayhem")

                .AddState()
                .SetStateJournalText("Halron Linth wants you to head down to the maintenance level and kill some unwanted Mynocks. Cull back their numbers, obtain their teeth and return to him for your receipt. He has requested five of them.")
                .AddKillObjective(NPCGroupType.CZ220_Mynocks, 5)

                .AddState()
                .SetStateJournalText("You've killed five Mynocks and obtained their teeth. Return to Halron Linth to collect your work receipt.")

                .AddXPReward(200)
                .AddGoldReward(150)
                .AddKeyItemReward(KeyItemType.HalronLinthsWorkReceipt);
        }

        private static void OreCollection(QuestBuilder builder)
        {
            builder.Create("ore_collection", "Ore Collection")
                .OnAcceptAction((player, sourceObject) =>
                {
                    var existingHarvester = GetItemPossessedBy(player, "harvest_r_old");
                    if (GetIsObjectValid(existingHarvester))
                        return;

                    var harvester = CreateItemOnObject("harvest_r_old", player);
                    SetItemCursedFlag(harvester, true);
                })

                .AddState()
                .SetStateJournalText("Avix Tatham needs you to head down to the maintenance level and harvest some ore. When you have four pieces, return to him to collect the work receipt.")
                .AddCollectItemObjective("raw_veldite", 4)

                .AddState()
                .SetStateJournalText("Speak to Avix Tatham for your reward.")

                .AddXPReward(200)
                .AddGoldReward(75)
                .AddKeyItemReward(KeyItemType.AvixTathamsWorkReceipt);
        }

        private static void RefineryTrainee(QuestBuilder builder)
        {
            builder.Create("refinery_trainee", "Refinery Trainee")
                .PrerequisiteQuest("ore_collection")

                .AddState()
                .SetStateJournalText("Avix Tatham wants you to go down and gather more ore. This time, though, he needs you to refine it at one of the refineries near him. When you have four pieces of refined Veldite return to him to give them to him.")
                .AddCollectItemObjective("ref_veldite", 4)

                .AddState()
                .SetStateJournalText("Speak to Avix Tatham for your reward.")

                .AddXPReward(300, false)
                .AddGoldReward(600, false);
        }
        private static void TheMalfunctioningDroids(QuestBuilder builder)
        {
            builder.Create("malfun_droids", "The Malfunctioning Droids")
                .PrerequisiteQuest("mynock_mayhem")

                .AddState()
                .SetStateJournalText("Halron Linth wants you to head back down to the maintenance level and take care of a few malfunctioning droids.")
                .AddKillObjective(NPCGroupType.CZ220_MalfunctioningDroids, 5)

                .AddState()
                .SetStateJournalText("You've destroyed five malfunctioning droids. Return to Halron Linth for your reward.")

                .AddGoldReward(150, false)
                .AddXPReward(200);
        }
        private static void TheColicoidExperiment(QuestBuilder builder)
        {
            builder.Create("the_colicoid_experiment", "The Colicoid Experiment")
                .PrerequisiteQuest("mynock_mayhem")
                .PrerequisiteQuest("malfun_droids")

                .AddState()
                .SetStateJournalText("Halron Linth has one final mission for you. He wants you to head back down to the maintenance level of CZ-220 and locate the experimental Colicoid locked in one of the rooms. You should prepare thoroughly as it will be a tough fight!")
                .AddKillObjective(NPCGroupType.CZ220_ColicoidExperiment, 1)

                .AddState()
                .SetStateJournalText("You made quick work of the rampaging Colicoid experiment. Return to Halron Linth to let him know the work is done.")

                .AddGoldReward(375, false)
                .AddXPReward(600)

                .OnAcceptAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.CZ220ExperimentRoomKey);
                })
                .OnAbandonAction(player =>
                {
                    KeyItem.RemoveKeyItem(player, KeyItemType.CZ220ExperimentRoomKey);
                });
        }

        private static void ScrapMetalMonstrosity(QuestBuilder builder)
        {
            builder.Create("scrapmetal_monster", "Scrap Metal Monstrosity")

                .AddState()
                .SetStateJournalText("5CR4P seems like they would like some more scrap metal. Maybe you should gather a lot for them.")
                .AddCollectItemObjective("scrap_metal", 150)

                .AddState()
                .SetStateJournalText("That's a lot of scrap metal. Hopefully, it's enough. See what 5CR4P has to beep about.")

                .AddGoldReward(3750)
                .AddXPReward(2500)
                .AddItemReward("recipe_fabcz220", 1);
        }
    

        private static void ToothSamples(QuestBuilder builder)
        {
            builder.Create("cz_tooth_sample", "Tooth Samples")

                .AddState()
                .SetStateJournalText("Dr. Ilsa Marn is tracking an infection that follows Mynock bites on CZ-220. Collect 4 Mynock Teeth from the Mynocks on the Maintenance Level of CZ-220, then bring them to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("mynock_tooth", 4)

                .AddState()
                .SetStateJournalText("You handed over 4 Mynock Teeth. Speak to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void WingMembrane(QuestBuilder builder)
        {
            builder.Create("cz_wing_membrane", "Wing Membrane")

                .AddState()
                .SetStateJournalText("Dr. Ilsa Marn dresses burns with cured Mynock wing membrane. Collect 3 Mynock Wings from the Mynocks on the Maintenance Level of CZ-220, then bring them to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("mynock_wing", 3)

                .AddState()
                .SetStateJournalText("You handed over 3 Mynock Wings. Speak to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void GalleyStock(QuestBuilder builder)
        {
            builder.Create("cz_galley_meat", "Galley Stock")

                .AddState()
                .SetStateJournalText("The CZ-220 galley is stretching its protein ration with Mynock meat. Collect 5 pieces of Mynock Meat from the Mynocks on the Maintenance Level of CZ-220, then bring them to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("mynock_meat", 5)

                .AddState()
                .SetStateJournalText("You handed over 5 pieces of Mynock Meat. Speak to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void HidePatches(QuestBuilder builder)
        {
            builder.Create("cz_hide_patches", "Hide Patches")

                .AddState()
                .SetStateJournalText("Sickbay on CZ-220 is out of splint padding. Collect 6 pieces of Ruined Leather from the Mynocks on the Maintenance Level or the supply caches, then bring them to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("lth_ruined", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 pieces of Ruined Leather. Speak to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void BiteWounds(QuestBuilder builder)
        {
            builder.Create("cz_bite_wounds", "Bite Wounds")

                .AddState()
                .SetStateJournalText("Dr. Ilsa Marn wants fewer Mynock bites coming into sickbay. Kill 8 Mynocks on the Maintenance Level of CZ-220, then return to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220.")
                .AddKillObjective(NPCGroupType.CZ220_Mynocks, 8)

                .AddState()
                .SetStateJournalText("You killed 8 Mynocks. Return to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(150)
                .AddXPReward(200);
        }

        private static void MedicalManifest(QuestBuilder builder)
        {
            builder.Create("cz_medical_manifest", "Medical Manifest")

                .AddState()
                .SetStateJournalText("Dr. Ilsa Marn wants to know where her medical supplies are. Ask Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220, then return to Dr. Ilsa Marn in the Recreation room with his answer.")

                .AddState()
                .SetStateJournalText("The medical supplies are on a late shipment, and sickbay is top of the list. Return to Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220 with the answer.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void PestPatrol(QuestBuilder builder)
        {
            builder.Create("cz_pest_patrol", "Pest Patrol")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Foreman Dorn Ketch pays a standing rate for Mynock control. Kill 10 Mynocks on the Maintenance Level of CZ-220, then return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220.")
                .AddKillObjective(NPCGroupType.CZ220_Mynocks, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Mynocks. Return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220 to collect the patrol rate.")

                .AddGoldReward(50)
                .AddXPReward(150);
        }

        private static void DroidSweep(QuestBuilder builder)
        {
            builder.Create("cz_droid_sweep", "Droid Sweep")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Foreman Dorn Ketch pays a standing rate for clearing malfunctioning droids. Destroy 10 Malfunctioning Droids on the Maintenance Level of CZ-220, then return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220.")
                .AddKillObjective(NPCGroupType.CZ220_MalfunctioningDroids, 10)

                .AddState()
                .SetStateJournalText("You destroyed 10 Malfunctioning Droids. Return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220 to collect the sweep rate.")

                .AddGoldReward(50)
                .AddXPReward(150);
        }

        private static void CircuitSalvage(QuestBuilder builder)
        {
            builder.Create("cz_circuit_salvage", "Circuit Salvage")

                .AddState()
                .SetStateJournalText("Foreman Dorn Ketch rebuilds control boards from salvaged parts. Collect 6 sets of Ruined Electronics from the Malfunctioning Droids on the Maintenance Level of CZ-220, then bring them to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("elec_ruined", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 sets of Ruined Electronics. Speak to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void FiberplastPatches(QuestBuilder builder)
        {
            builder.Create("cz_fiberplast_patches", "Fiberplast Patches")

                .AddState()
                .SetStateJournalText("The Maintenance Level coolant lines on CZ-220 need patching. Collect 6 pieces of Ruined Fiberplast from the supply caches around the station, then bring them to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("fiberp_ruined", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 pieces of Ruined Fiberplast. Speak to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void DeepClean(QuestBuilder builder)
        {
            builder.Create("cz_deep_clean", "Deep Clean")

                .AddState()
                .SetStateJournalText("Foreman Dorn Ketch wants the Maintenance Level of CZ-220 safe for a repair crew. Kill 12 Mynocks and destroy 8 Malfunctioning Droids on the Maintenance Level of CZ-220, then return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220.")
                .AddKillObjective(NPCGroupType.CZ220_Mynocks, 12)
                .AddKillObjective(NPCGroupType.CZ220_MalfunctioningDroids, 8)

                .AddState()
                .SetStateJournalText("You killed 12 Mynocks and destroyed 8 Malfunctioning Droids. Return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(150)
                .AddXPReward(300);
        }

        private static void ContainmentBreach(QuestBuilder builder)
        {
            builder.Create("cz_containment_breach", "Containment Breach")
                .PrerequisiteQuest("cz_deep_clean")

                .AddState()
                .SetStateJournalText("The Colicoid experiment grown by the labs is loose on the Maintenance Level of CZ-220 again. Kill the Colicoid Experiment on the Maintenance Level of CZ-220, then return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220.")
                .AddKillObjective(NPCGroupType.CZ220_ColicoidExperiment, 1)

                .AddState()
                .SetStateJournalText("You killed the Colicoid Experiment. Return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(375)
                .AddXPReward(600);
        }

        private static void ShiftReport(QuestBuilder builder)
        {
            builder.Create("cz_shift_report", "Shift Report")

                .AddState()
                .SetStateJournalText("Foreman Dorn Ketch needs his shift report filed. Take it to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220, then return to Foreman Dorn Ketch by the Fabrication Terminals with his answer.")

                .AddState()
                .SetStateJournalText("The shift report is filed and the parts are logged. Return to Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220 with the quartermaster's answer.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void CrateTimber(QuestBuilder builder)
        {
            builder.Create("cz_crate_timber", "Crate Timber")

                .AddState()
                .SetStateJournalText("Quartermaster Tobin Raye needs timber to rebuild shipping crates on CZ-220. Collect 6 pieces of Wood from the supply caches around the station, then bring them to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("wood", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 pieces of Wood. Speak to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void Ballast(QuestBuilder builder)
        {
            builder.Create("cz_pebble_ballast", "Ballast")

                .AddState()
                .SetStateJournalText("Quartermaster Tobin Raye packs fragile cargo with Viscaran Pebbles. Collect 4 Viscaran Pebbles from the supply caches around the station, then bring them to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("v_pebble", 4)

                .AddState()
                .SetStateJournalText("You handed over 4 Viscaran Pebbles. Speak to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void VelditeQuota(QuestBuilder builder)
        {
            builder.Create("cz_veldite_quota", "Veldite Quota")

                .AddState()
                .SetStateJournalText("CZ-220 is behind on its ore quota. Collect 10 pieces of Raw Veldite by mining the Veldite veins on the Maintenance Level of CZ-220, then bring them to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("raw_veldite", 10)

                .AddState()
                .SetStateJournalText("You handed over 10 pieces of Raw Veldite. Speak to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(150)
                .AddXPReward(300);
        }

        private static void RefinedQuota(QuestBuilder builder)
        {
            builder.Create("cz_refined_quota", "Refined Quota")

                .AddState()
                .SetStateJournalText("CZ-220 owes Czerka a shipment of refined ore. Collect 6 ingots of Refined Veldite by smelting Raw Veldite at the station's refinery, then bring them to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220.")
                .AddCollectItemObjective("ref_veldite", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 ingots of Refined Veldite. Speak to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220 for your reward.")

                .AddGoldReward(300)
                .AddXPReward(300);
        }

        private static void Requisition(QuestBuilder builder)
        {
            builder.Create("cz_requisition", "Requisition")

                .AddState()
                .SetStateJournalText("Quartermaster Tobin Raye needs a count of sickbay's splints. Ask Dr. Ilsa Marn in the Recreation room of the Offices & Labs of CZ-220, then return to Quartermaster Tobin Raye in Supplies with the number.")

                .AddState()
                .SetStateJournalText("Sickbay has no splints at all. Return to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220 with the answer.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void PartsOrder(QuestBuilder builder)
        {
            builder.Create("cz_parts_order", "Parts Order")

                .AddState()
                .SetStateJournalText("Quartermaster Tobin Raye is closing the parts order. Ask Foreman Dorn Ketch by the Fabrication Terminals in the Offices & Labs of CZ-220 for his order, then return to Quartermaster Tobin Raye in Supplies.")

                .AddState()
                .SetStateJournalText("The foreman wants lift controllers, coolant line and a bulkhead. Return to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220 with the order.")

                .AddGoldReward(75)
                .AddXPReward(200);
        }

        private static void StationHand(QuestBuilder builder)
        {
            builder.Create("cz_station_hand", "Station Hand")
                .PrerequisiteQuest("cz_deep_clean")
                .PrerequisiteQuest("cz_veldite_quota")
                .PrerequisiteQuest("cz_tooth_sample")

                .AddState()
                .SetStateJournalText("You have helped sickbay, Maintenance and Supplies on CZ-220. Speak to Quartermaster Tobin Raye in Supplies in the Offices & Labs of CZ-220 to be entered on the station roster and collect your bonus.")

                .AddGoldReward(375)
                .AddXPReward(600)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.StationHand);
                });
        }
    }
}
