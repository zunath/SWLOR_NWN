using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class CZ220QuestDefinition: IQuestListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly IKeyItemService _keyItemService;
        private readonly IObjectVisibilityService _objectVisibilityService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IQuestService _questService;

        public CZ220QuestDefinition(IDatabaseService db, IKeyItemService keyItemService, IObjectVisibilityService objectVisibilityService, IServiceProvider serviceProvider, IQuestService questService)
        {
            _db = db;
            _keyItemService = keyItemService;
            _objectVisibilityService = objectVisibilityService;
            _serviceProvider = serviceProvider;
            _questService = questService;
        }

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder(_serviceProvider, _questService);
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

            return builder.Build();
        }

        private void SelansRequest(IQuestBuilder builder)
        {
            builder.Create("selan_request", "Selan's Request")

                .AddState()
                .SetStateJournalText("Selan Flembek, receptionist at CZ-220, has requested you complete three tasks around the base. \n\nSpeak to Avix Tatham, the mining coordinator, for information about retrieving ore from the mine.\n\nTalk to security officer Harlon Linth for information about trouble on the maintenance level.\n\nChat with the Crafting Terminal Droid Operator for details about supplies which need to be constructed.\n\nWhen you've obtained receipts from all three employees, return to Selan to collect your reward.")

                .AddXPReward(1000)
                .AddGoldReward(500)
                .AddKeyItemReward(KeyItemType.CZ220ShuttlePass)

                .OnCompleteAction((player, sourceObject) =>
                {
                    _keyItemService.RemoveKeyItem(player, KeyItemType.AvixTathamsWorkReceipt);
                    _keyItemService.RemoveKeyItem(player, KeyItemType.HalronLinthsWorkReceipt);
                    _keyItemService.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt);

                    var cdKey = GetPCPublicCDKey(player);
                    var dbAccount = _db.Get<Account>(cdKey) ?? new Account(cdKey);
                    dbAccount.HasCompletedTutorial = true;

                    _db.Set(dbAccount);
                });
        }

        private void SuppliesSmithery(IQuestBuilder builder)
        {
            builder.Create("cz220_smithery", "CZ-220 Supplies - Smithery")

                .AddState()
                .SetStateJournalText("The Crafting Terminal Droid operator has requested you create a single Basic Knife. You will need to purchase the \"One-Handed Blueprints\" perk in order to create this item. Once you have the perk you can use any smithery terminal to make the item. You will find the necessary resources down on the maintenance level of CZ-220.")
                .AddCollectItemObjective("b_knife", 1)

                .AddState()
                .SetStateJournalText("Speak to the Crafting Terminal Droid operator for your reward.")

                .AddXPReward(200)
                .AddGoldReward(50)
                .AddKeyItemReward(KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt)

                .OnAcceptAction((player, sourceObject) =>
                {
                    _keyItemService.GiveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnAbandonAction(player =>
                {
                    _keyItemService.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnCompleteAction((player, sourceObject) =>
                {
                    _keyItemService.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                });
        }
        private void SuppliesScavenging(IQuestBuilder builder)
        {
            builder.Create("cz220_scavenging", "CZ-220 Supplies - Scavenging")

                .AddState()
                .SetStateJournalText("The Crafting Terminal Droid operator has requested you gather ten units of scrap metal. You will need to scavenge through junk piles throughout CZ-220. When you have enough, return to the droid to collect your receipt and reward.")
                .AddCollectItemObjective("scrap_metal", 10)

                .AddState()
                .SetStateJournalText("Speak to the Crafting Terminal Droid operator for your reward.")

                .AddXPReward(200)
                .AddGoldReward(50)
                .AddKeyItemReward(KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt)

                .OnAcceptAction((player, sourceObject) =>
                {
                    _keyItemService.GiveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnAbandonAction(player =>
                {
                    _keyItemService.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnCompleteAction((player, sourceObject) =>
                {
                    _keyItemService.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                });
        }
        private void SuppliesFabrication(IQuestBuilder builder)
        {
            builder.Create("cz220_fabrication", "CZ-220 Supplies - Fabrication")

                .AddState()
                .SetStateJournalText("The Crafting Terminal Droid operator has requested you create a simple bed roll. You will need to purchase the \"Furniture Blueprints\" perk in order to create this item. You can use any fabrication terminal to make the item. You will find the necessary resources down on the maintenance level of CZ-220.")
                .AddCollectItemObjective("structure_0085", 1)

                .AddState()
                .SetStateJournalText("Speak to the Crafting Terminal Droid operator for your reward.")

                .AddXPReward(200)
                .AddGoldReward(50)
                .AddKeyItemReward(KeyItemType.CraftingTerminalDroidOperatorsWorkReceipt)

                .OnAcceptAction((player, sourceObject) =>
                {
                    _keyItemService.GiveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnAbandonAction(player =>
                {
                    _keyItemService.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                })
                .OnCompleteAction((player, sourceObject) =>
                {
                    _keyItemService.RemoveKeyItem(player, KeyItemType.CraftingTerminalDroidOperatorsWorkOrder);
                });
        }

        private void DatapadRetrieval(IQuestBuilder builder)
        {
            builder.Create("datapad_retrieval", "Datapad Retrieval")

                .AddState()
                .SetStateJournalText("You found a datapad in the room containing an experimental Colicoid. It contains research details on the creature. There's no name listed anywhere in the data but you assume it belongs to someone on CZ-220. Ask around on the main level and see if it's theirs.")

                .AddXPReward(300)
                .AddGoldReward(50)

                .OnAcceptAction((player, sourceObject) =>
                {
                    _objectVisibilityService.AdjustVisibility(player, sourceObject, VisibilityType.Hidden);
                })
                .OnAbandonAction(player =>
                {
                    _objectVisibilityService.AdjustVisibilityByObjectId(player, "3BA0FF2E61C34FB783905E8A78236A30", VisibilityType.Visible);
                });
        }

        private void MynockMayhem(IQuestBuilder builder)
        {
            builder.Create("mynock_mayhem", "Mynock Mayhem")

                .AddState()
                .SetStateJournalText("Halron Linth wants you to head down to the maintenance level and kill some unwanted Mynocks. Cull back their numbers, obtain their teeth and return to him for your receipt. He has requested five of them.")
                .AddKillObjective(NPCGroupType.CZ220_Mynocks, 5)

                .AddState()
                .SetStateJournalText("You've killed five Mynocks and obtained their teeth. Return to Halron Linth to collect your work receipt.")

                .AddXPReward(200)
                .AddGoldReward(100)
                .AddKeyItemReward(KeyItemType.HalronLinthsWorkReceipt);
        }

        private void OreCollection(IQuestBuilder builder)
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
                .AddGoldReward(50)
                .AddKeyItemReward(KeyItemType.AvixTathamsWorkReceipt);
        }

        private void RefineryTrainee(IQuestBuilder builder)
        {
            builder.Create("refinery_trainee", "Refinery Trainee")
                .PrerequisiteQuest("ore_collection")

                .AddState()
                .SetStateJournalText("Avix Tatham wants you to go down and gather more ore. This time, though, he needs you to refine it at one of the refineries near him. You will need the perk 'Refining I' to refine the ore. When you have four pieces of refined Veldite return to him to give them to him.")
                .AddCollectItemObjective("ref_veldite", 4)

                .AddState()
                .SetStateJournalText("Speak to Avix Tatham for your reward.")

                .AddXPReward(300, false)
                .AddGoldReward(400, false);
        }
        private void TheMalfunctioningDroids(IQuestBuilder builder)
        {
            builder.Create("malfun_droids", "The Malfunctioning Droids")
                .PrerequisiteQuest("mynock_mayhem")

                .AddState()
                .SetStateJournalText("Halron Linth wants you to head back down to the maintenance level and take care of a few malfunctioning droids.")
                .AddKillObjective(NPCGroupType.CZ220_MalfunctioningDroids, 5)

                .AddState()
                .SetStateJournalText("You've destroyed five malfunctioning droids. Return to Halron Linth for your reward.")

                .AddGoldReward(100, false)
                .AddXPReward(200);
        }
        private void TheColicoidExperiment(IQuestBuilder builder)
        {
            builder.Create("the_colicoid_experiment", "The Colicoid Experiment")
                .PrerequisiteQuest("mynock_mayhem")
                .PrerequisiteQuest("malfun_droids")

                .AddState()
                .SetStateJournalText("Halron Linth has one final mission for you. He wants you to head back down to the maintenance level of CZ-220 and locate the experimental Colicoid locked in one of the rooms. You should prepare thoroughly as it will be a tough fight!")
                .AddKillObjective(NPCGroupType.CZ220_ColicoidExperiment, 1)

                .AddState()
                .SetStateJournalText("You made quick work of the rampaging Colicoid experiment. Return to Halron Linth to let him know the work is done.")
                
                .AddGoldReward(250, false)
                .AddXPReward(600)
                
                .OnAcceptAction((player, sourceObject) =>
                {
                    _keyItemService.GiveKeyItem(player, KeyItemType.CZ220ExperimentRoomKey);
                })
                .OnAbandonAction(player =>
                {
                    _keyItemService.RemoveKeyItem(player, KeyItemType.CZ220ExperimentRoomKey);
                });
        }

        private void ScrapMetalMonstrosity(IQuestBuilder builder)
        {
            builder.Create("scrapmetal_monster", "Scrap Metal Monstrosity")

                .AddState()
                .SetStateJournalText("5CR4P seems like they would like some more scrap metal. Maybe you should gather a lot for them.")
                .AddCollectItemObjective("scrap_metal", 150)

                .AddState()
                .SetStateJournalText("That's a lot of scrap metal. Hopefully, it's enough. See what 5CR4P has to beep about.")

                .AddGoldReward(2500)
                .AddXPReward(2500)
                .AddItemReward("recipe_fabcz220", 1);
        }
    }
}
