using System.Collections.Generic;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service;

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
            SmugglerFavor(builder);

            WompratSeason(builder);
            MoistureThieves(builder);
            BeetleRun(builder);
            WaterIsLife(builder);
            ChitinForTheVaporators(builder);
            OreForAnchorhead(builder);
            ToothAndClaw(builder);
            FinSoup(builder);
            DemonOfTheDunes(builder);
            DemonHideCanteens(builder);
            DesertSweep(builder);
            WarlordsTollEliteGuard(builder);
            WarlordsTollWarBanners(builder);
            WarlordsTollEndOfTheLine(builder);
            BountyUrrok(builder);
            BountySharrakLongEye(builder);
            BountyWarcallerAarkha(builder);
            TheThingBeneath(builder);
            ThePearlHuntersWager(builder);
            DeadOrAlive(builder);

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

        private static void SmugglerFavor(QuestBuilder builder)
        {
            builder.Create("smuggler_favor", "A Smuggler's Favor")
                .OnAdvanceAction((player, sourceObject, state) =>
                {
                    if (state == 2)
                        CreateItemOnObject("stolen_goods", player, 5);
                })

                .AddState()
                .SetStateJournalText("A shady smuggler has asked for your help. He lost a shipment to a local ancient sandworm and needs you to recover it. Find and eliminate the sandworm, then retrieve the stolen cargo.")
                .AddKillObjective(NPCGroupType.Tatooine_AncientSandworm, 1)

                .AddState()
                .SetStateJournalText("You've slain the ancient sandworm and recovered five Stolen Goods. Bring the cargo back to the smuggler on Tatooine.")
                .AddCollectItemObjective("stolen_goods", 5)

                .AddState()
                .SetStateJournalText("Return the goods to the smuggler. He promised you something valuable in return.")

                .AddGoldReward(500)
                .AddXPReward(1500)

                .OnCompleteAction((player, sourceObject) =>
                {
                    KeyItem.GiveKeyItem(player, KeyItemType.SmugglerPass);
                });
        }
    

        private static void WompratSeason(QuestBuilder builder)
        {
            builder.Create("tat_womprat_season", "Womprat Season")

                .AddState()
                .SetStateJournalText("Womprats are chewing the condenser seals on Odrin Vass's moisture farm. Kill 15 Womprats in the desert around Anchorhead on Tatooine, then return to Odrin Vass in the Anchorhead Cantina.")
                .AddKillObjective(NPCGroupType.Tatooine_Womprat, 15)

                .AddState()
                .SetStateJournalText("You killed 15 Womprats in the desert around Anchorhead. Return to Odrin Vass in the Anchorhead Cantina on Tatooine to collect your pay.")

                .AddGoldReward(3000)
                .AddXPReward(4000);
        }

        private static void MoistureThieves(QuestBuilder builder)
        {
            builder.Create("tat_moisture", "Moisture Thieves")

                .AddState()
                .SetStateJournalText("Sandswimmers are digging up the coolant lines under Odrin Vass's vaporators. Kill 12 Sandswimmers on the Desert Road, the Flatlands or the Northern Dunes of Tatooine, then return to Odrin Vass in the Anchorhead Cantina.")
                .AddKillObjective(NPCGroupType.Tatooine_Sandswimmer, 12)

                .AddState()
                .SetStateJournalText("You killed 12 Sandswimmers and the vaporator lines are safe. Return to Odrin Vass in the Anchorhead Cantina on Tatooine to collect your pay.")

                .AddGoldReward(3000)
                .AddXPReward(4000);
        }

        private static void BeetleRun(QuestBuilder builder)
        {
            builder.Create("tat_beetle_run", "Beetle Run")

                .AddState()
                .SetStateJournalText("Sand Beetles are wrecking the haulers that carry Odrin Vass's water to town. Kill 12 Sand Beetles in the Deep Canyon, the Rocky Pass, the Flatlands or the Northern Dunes of Tatooine, then return to Odrin Vass in the Anchorhead Cantina.")
                .AddKillObjective(NPCGroupType.Tatooine_SandBeetle, 12)

                .AddState()
                .SetStateJournalText("You killed 12 Sand Beetles and the water route is clear. Return to Odrin Vass in the Anchorhead Cantina on Tatooine to collect your pay.")

                .AddGoldReward(3000)
                .AddXPReward(4000);
        }

        private static void WaterIsLife(QuestBuilder builder)
        {
            builder.Create("tat_water_life", "Water Is Life")

                .AddState()
                .SetStateJournalText("The pressure regulator on Odrin Vass's best vaporator is failing. Ask Bex Tarrow, the mechanic in Anchorhead's North District, whether a replacement can be had, then return to Odrin Vass in the Anchorhead Cantina.")

                .AddState()
                .SetStateJournalText("Bex Tarrow has no replacement regulator but can rebuild the old one if Odrin brings it in. Return to Odrin Vass in the Anchorhead Cantina with the answer.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private static void ChitinForTheVaporators(QuestBuilder builder)
        {
            builder.Create("tat_chitin", "Chitin for the Vaporators")

                .AddState()
                .SetStateJournalText("Bex Tarrow patches worn vaporator casings with beetle shell. Collect 8 Beetle Chitin from the Sand Beetles of Tatooine, then bring them to Bex Tarrow in Anchorhead's North District.")
                .AddCollectItemObjective("beetle_chitin", 8)

                .AddState()
                .SetStateJournalText("You handed over 8 Beetle Chitin for the vaporator casings. Speak to Bex Tarrow in Anchorhead's North District on Tatooine to collect your pay.")

                .AddGoldReward(3000)
                .AddXPReward(4000);
        }

        private static void OreForAnchorhead(QuestBuilder builder)
        {
            builder.Create("tat_anchor_ore", "Ore for Anchorhead")

                .AddState()
                .SetStateJournalText("With the Czerka mines shut, Anchorhead is short of ore. Collect 10 Raw Keromber and 10 Raw Jasioclase on Tatooine, then bring them to Bex Tarrow in Anchorhead's North District. Jasioclase is found in the Arid Hilly Desert.")
                .AddCollectItemObjective("raw_keromber", 10)
                .AddCollectItemObjective("raw_jasioclase", 10)

                .AddState()
                .SetStateJournalText("You handed over 10 Raw Keromber and 10 Raw Jasioclase. Speak to Bex Tarrow in Anchorhead's North District on Tatooine to collect your pay.")

                .AddGoldReward(3000)
                .AddXPReward(4000);
        }

        private static void ToothAndClaw(QuestBuilder builder)
        {
            builder.Create("tat_tooth_claw", "Tooth and Claw")

                .AddState()
                .SetStateJournalText("Karrav Maubar has a Jawa buyer for womprat parts. Collect 6 Womprat Teeth and 6 Womprat Claws from the Womprats of Tatooine, then bring them to Karrav Maubar in Anchorhead's South District.")
                .AddCollectItemObjective("womprattooth", 6)
                .AddCollectItemObjective("wompratclaw", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 Womprat Teeth and 6 Womprat Claws. Speak to Karrav Maubar in Anchorhead's South District on Tatooine to collect your payment.")

                .AddGoldReward(3000)
                .AddXPReward(4000);
        }

        private static void FinSoup(QuestBuilder builder)
        {
            builder.Create("tat_fin_soup", "Fin Soup")

                .AddState()
                .SetStateJournalText("Karrav Maubar supplies the cantina kitchen, and the cook wants Sandswimmer. Collect 6 Sandswimmer Fins and 4 Sandswimmer Legs from the Sandswimmers of Tatooine, then bring them to Karrav Maubar in Anchorhead's South District.")
                .AddCollectItemObjective("sandswimmerfin", 6)
                .AddCollectItemObjective("sandswimmerleg", 4)

                .AddState()
                .SetStateJournalText("You handed over 6 Sandswimmer Fins and 4 Sandswimmer Legs. Speak to Karrav Maubar in Anchorhead's South District on Tatooine to collect your payment.")

                .AddGoldReward(3000)
                .AddXPReward(4000)
                .AddItemReward("sanddem_potpie", 3);
        }

        private static void DemonOfTheDunes(QuestBuilder builder)
        {
            builder.Create("tat_demon_dunes", "Demon of the Dunes")

                .AddState()
                .SetStateJournalText("Sand Demons are attacking Karrav Maubar's caravans. Kill 8 Sand Demons in the North Hills, the Flatlands or the Northern Dunes of Tatooine, then bring 4 Sand Demon Claws to Karrav Maubar in Anchorhead's South District.")
                .AddKillObjective(NPCGroupType.Tatooine_SandDemon, 8)
                .AddCollectItemObjective("sanddemonclaw", 4)

                .AddState()
                .SetStateJournalText("You killed 8 Sand Demons and handed over 4 Sand Demon Claws. Speak to Karrav Maubar in Anchorhead's South District on Tatooine to collect your payment.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private static void DemonHideCanteens(QuestBuilder builder)
        {
            builder.Create("tat_demon_hide", "Demon Hide Canteens")

                .AddState()
                .SetStateJournalText("Karrav Maubar makes caravan canteens from Sand Demon hide. Collect 5 Sand Demon Hides from the Sand Demons of Tatooine, then bring them to Karrav Maubar in Anchorhead's South District.")
                .AddCollectItemObjective("sanddemonhide", 5)

                .AddState()
                .SetStateJournalText("You handed over 5 Sand Demon Hides for the caravan canteens. Speak to Karrav Maubar in Anchorhead's South District on Tatooine to collect your payment.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private static void DesertSweep(QuestBuilder builder)
        {
            builder.Create("tat_desert_sweep", "Desert Sweep")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Karrav Maubar pays a standing rate to keep the caravan lines clear. Kill 10 Sand Demons and 10 Sand Beetles on Tatooine, then return to Karrav Maubar in Anchorhead's South District.")
                .AddKillObjective(NPCGroupType.Tatooine_SandDemon, 10)
                .AddKillObjective(NPCGroupType.Tatooine_SandBeetle, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Sand Demons and 10 Sand Beetles. Return to Karrav Maubar in Anchorhead's South District on Tatooine to collect the sweep rate.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private static void WarlordsTollEliteGuard(QuestBuilder builder)
        {
            builder.Create("tat_toll_1", "The Warlord's Toll: Elite Guard")

                .AddState()
                .SetStateJournalText("A Tusken warlord is taking tribute from the Anchorhead caravans. Kill 10 Tusken Elites in the Tusken Raider Cave on Tatooine, then return to Captain Jorn Hale at the Anchorhead Spaceport.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenElite, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Tusken Elites in the Tusken Raider Cave. Return to Captain Jorn Hale at the Anchorhead Spaceport on Tatooine to collect your pay.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private static void WarlordsTollWarBanners(QuestBuilder builder)
        {
            builder.Create("tat_toll_2", "The Warlord's Toll: War Banners")
                .PrerequisiteQuest("tat_toll_1")

                .AddState()
                .SetStateJournalText("The Tusken warlord's fighters carry his war banners when they collect tribute. Collect 5 Tusken War Banners from the Tusken Elites in the Tusken Raider Cave on Tatooine, then bring them to Captain Jorn Hale at the Anchorhead Spaceport.")
                .AddCollectItemObjective("tat_war_banner", 5)

                .AddState()
                .SetStateJournalText("You handed over 5 Tusken War Banners. Speak to Captain Jorn Hale at the Anchorhead Spaceport on Tatooine to collect your pay.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private static void WarlordsTollEndOfTheLine(QuestBuilder builder)
        {
            builder.Create("tat_toll_3", "The Warlord's Toll: End of the Line")
                .PrerequisiteQuest("tat_toll_2")

                .AddState()
                .SetStateJournalText("The Tusken Warlord has lost his banners and his allies, and holds the bottom floor of the Tusken Raider Cave on Tatooine. Kill the Tusken Warlord there, then return to Captain Jorn Hale at the Anchorhead Spaceport.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenWarlord, 1)

                .AddState()
                .SetStateJournalText("You killed the Tusken Warlord in the Tusken Raider Cave. Return to Captain Jorn Hale at the Anchorhead Spaceport on Tatooine to collect the caravan masters' purse.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("caravan_sash", 1)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.TheWarlordsToll);
                });
        }

        private static void BountyUrrok(QuestBuilder builder)
        {
            builder.Create("tat_bounty_urrok", "Bounty: Urrok")
                .PrerequisiteQuest("tat_toll_1")

                .AddState()
                .SetStateJournalText("Urrok, Gaderffii Champion, fights for the Tusken warlord. Kill Urrok, Gaderffii Champion, in the Tusken Raider Cave on Tatooine, then return to Captain Jorn Hale at the Anchorhead Spaceport. Urrok is not always in the cave.")
                .AddKillObjective(NPCGroupType.Tatooine_UrrokGaderffiiChampion, 1)

                .AddState()
                .SetStateJournalText("You killed Urrok, Gaderffii Champion, in the Tusken Raider Cave. Return to Captain Jorn Hale at the Anchorhead Spaceport on Tatooine to collect the bounty.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private static void BountySharrakLongEye(QuestBuilder builder)
        {
            builder.Create("tat_bounty_sharrak", "Bounty: Sharrak Long-Eye")
                .PrerequisiteQuest("tat_toll_1")

                .AddState()
                .SetStateJournalText("Sharrak Long-Eye, a Tusken sniper, shoots the lead banthas of caravans for the warlord. Kill Sharrak Long-Eye in the Tusken Raider Cave on Tatooine, then return to Captain Jorn Hale at the Anchorhead Spaceport. Sharrak is not always in the cave.")
                .AddKillObjective(NPCGroupType.Tatooine_SharrakLongEye, 1)

                .AddState()
                .SetStateJournalText("You killed Sharrak Long-Eye, the warlord's sniper. Return to Captain Jorn Hale at the Anchorhead Spaceport on Tatooine to collect the bounty.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private static void BountyWarcallerAarkha(QuestBuilder builder)
        {
            builder.Create("tat_bounty_warcaller", "Bounty: Warcaller Aa'rkha")
                .PrerequisiteQuest("tat_toll_1")

                .AddState()
                .SetStateJournalText("Warcaller Aa'rkha rallies the clans for the Tusken warlord. Kill Warcaller Aa'rkha in the Tusken Raider Cave on Tatooine, then return to Captain Jorn Hale at the Anchorhead Spaceport. The Warcaller is not always in the cave.")
                .AddKillObjective(NPCGroupType.Tatooine_WarcallerAarkha, 1)

                .AddState()
                .SetStateJournalText("You killed Warcaller Aa'rkha, who rallied the clans. Return to Captain Jorn Hale at the Anchorhead Spaceport on Tatooine to collect the bounty.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private static void TheThingBeneath(QuestBuilder builder)
        {
            builder.Create("tat_thing_beneath", "The Thing Beneath")

                .AddState()
                .SetStateJournalText("Dreeko Vant has wagered that the Sand Worm cannot be killed. Kill the Sand Worm on Tatooine, where it surfaces in the Flatlands and the Southern Pass, then return to Dreeko Vant at the Anchorhead Spaceport.")
                .AddKillObjective(NPCGroupType.Tatooine_SandWorm, 1)

                .AddState()
                .SetStateJournalText("You killed the Sand Worm. Return to Dreeko Vant at the Anchorhead Spaceport on Tatooine to collect your winnings on the wager.")

                .AddGoldReward(11250)
                .AddXPReward(12000);
        }

        private static void ThePearlHuntersWager(QuestBuilder builder)
        {
            builder.Create("tat_pearl_wager", "The Pearl Hunter's Wager")

                .AddState()
                .SetStateJournalText("Dreeko Vant has wagered that a Krayt Dragon cannot be killed. Kill a Krayt Dragon on the Desert Road, the Flatlands, the Northern Dunes or the Southern Pass of Tatooine, then return to Dreeko Vant at the Anchorhead Spaceport. They are not always there.")
                .AddKillObjective(NPCGroupType.Tatooine_KraytDragon, 1)

                .AddState()
                .SetStateJournalText("You killed a Krayt Dragon. Return to Dreeko Vant at the Anchorhead Spaceport on Tatooine to collect your winnings on the wager.")

                .AddGoldReward(11250)
                .AddXPReward(12000);
        }

        private static void DeadOrAlive(QuestBuilder builder)
        {
            builder.Create("tat_dead_alive", "Dead or Alive")

                .AddState()
                .SetStateJournalText("A peerless bounty hunter is hunting Dreeko Vant. Kill the Peerless Bounty Hunter on the Desert Road, the Flatlands, the Northern Dunes or the Southern Pass of Tatooine, then return to Dreeko Vant at the Anchorhead Spaceport. The hunter is not always there.")
                .AddKillObjective(NPCGroupType.Tatooine_PeerlessBountyHunter, 1)

                .AddState()
                .SetStateJournalText("You killed the Peerless Bounty Hunter. Return to Dreeko Vant at the Anchorhead Spaceport on Tatooine to collect your winnings on the wager.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("hunter_vambrace", 1);
        }
    }
}
