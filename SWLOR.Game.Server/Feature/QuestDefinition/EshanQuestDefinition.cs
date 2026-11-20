using System.Collections.Generic;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class EshanQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            EshanWolfLine();
            EshanFrostLine();
            EshanAlphaHunt();
            EshanIceboundHunt();
            EshanScoutScreen();
            EshanHunterTrap();
            EshanVanguardBreak();
            EshanHeavySilence();
            EshanMedicCutoff();
            EshanCaptainFall();
            EshanThyrsianExiles();
            EshanScrapyardSmugglers();
            BreakTheGarrisonTheLine();
            BreakTheGarrisonFieldHospital();
            BreakTheGarrisonFieldCommand();
            TheExiles();
            MarchesPatrol();
            GarrisonPatrol();
            Dispatches();
            DeepSilverwood();
            FrostFangs();
            ThePackLeaders();
            SilverwoodPatrol();
            WinterStores();
            TheIceboundsHide();
            HideTally();
            CapturedRations();
            TheExilesLarder();
            ScraplandTrade();
            EyesOnTheConvoys();
            TheRaiders();
            ScraplandsSweep();

            return _builder.Build();
        }

        private void EshanWolfLine()
        {
            _builder.Create("eshan_wolf_line", "Thin the Silverwood Packs")

                .AddState()
                .SetStateJournalText("Cull eight Eshan Dire Wolves in the Silverwood Expanse, then report to Security Officer Talia Venn in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_DireWolf, 8)

                .AddState()
                .SetStateJournalText("Eight dire wolves are down. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(4000)
                .AddGoldReward(4800);
        }

        private void EshanFrostLine()
        {
            _builder.Create("eshan_frost_line", "Break the Frost Pack")

                .AddState()
                .SetStateJournalText("Defeat six Eshan Frost Dire Wolves beyond Eshan City, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_FrostWolf, 6)

                .AddState()
                .SetStateJournalText("Six frost dire wolves have been defeated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(3000)
                .AddGoldReward(3600);
        }

        private void EshanAlphaHunt()
        {
            _builder.Create("eshan_alpha_hunt", "The Alpha's Challenge")

                .AddState()
                .SetStateJournalText("Hunt the Eshan Dire Wolf Alpha threatening the city approaches, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_DireWolfAlpha, 1)

                .AddState()
                .SetStateJournalText("The dire wolf alpha is dead. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(8000)
                .AddGoldReward(4500);
        }

        private void EshanIceboundHunt()
        {
            _builder.Create("eshan_icebound_hunt", "Hunt the Icebound")

                .AddState()
                .SetStateJournalText("Defeat Gorak Vesh, the Icebound, in the Silverwood Expanse, then report to Security Officer Talia Venn in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_GorakVesh, 1)

                .AddState()
                .SetStateJournalText("Gorak Vesh, the Icebound, is dead. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(8000)
                .AddGoldReward(4500);
        }

        private void EshanScoutScreen()
        {
            _builder.Create("eshan_scout_screen", "Eyes in the Snow")

                .AddState()
                .SetStateJournalText("Eliminate seven Neo-Crusader Scouts operating in the Winter Marches, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderScout, 7)

                .AddState()
                .SetStateJournalText("Seven Neo-Crusader scouts have been eliminated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(3500)
                .AddGoldReward(4200);
        }

        private void EshanHunterTrap()
        {
            _builder.Create("eshan_hunter_trap", "Close the Hunter's Trail")

                .AddState()
                .SetStateJournalText("Eliminate six Neo-Crusader Hunters operating in the Winter Marches, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderHunter, 6)

                .AddState()
                .SetStateJournalText("Six Neo-Crusader hunters have been eliminated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(3000)
                .AddGoldReward(3600);
        }

        private void EshanVanguardBreak()
        {
            _builder.Create("eshan_vanguard_break", "Break the Vanguard")

                .AddState()
                .SetStateJournalText("Defeat eight Neo-Crusader Vanguards probing Eshan's defenses, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderVanguard, 8)

                .AddState()
                .SetStateJournalText("Eight Neo-Crusader vanguards have fallen. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(4000)
                .AddGoldReward(4800);
        }

        private void EshanHeavySilence()
        {
            _builder.Create("eshan_heavy_silence", "Silence the Heavy Guns")

                .AddState()
                .SetStateJournalText("Eliminate five Neo-Crusader Heavy Gunners threatening Eshan's routes, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderHeavy, 5)

                .AddState()
                .SetStateJournalText("Five Neo-Crusader Heavy Gunners have been eliminated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(2500)
                .AddGoldReward(3000);
        }

        private void EshanMedicCutoff()
        {
            _builder.Create("eshan_medic_cutoff", "Cut the Lifeline")

                .AddState()
                .SetStateJournalText("Eliminate five Neo-Crusader Field Medics supporting enemy assault cells, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderMedic, 5)

                .AddState()
                .SetStateJournalText("Five Neo-Crusader medics have been eliminated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(2500)
                .AddGoldReward(3000);
        }

        private void EshanCaptainFall()
        {
            _builder.Create("eshan_captain_fall", "Shatter Field Command")

                .AddState()
                .SetStateJournalText("Defeat three Neo-Crusader Captains directing attacks against Eshan, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderCaptain, 3)

                .AddState()
                .SetStateJournalText("Three Neo-Crusader captains have fallen. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(1500)
                .AddGoldReward(1800);
        }

        private void EshanThyrsianExiles()
        {
            _builder.Create("eshan_sunguard_stand", "Thyrsian Incursion")

                .AddState()
                .SetStateJournalText("Defeat four former Revanite Thyrsian warriors fighting on Eshan, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_SunGuard, 4)

                .AddState()
                .SetStateJournalText("Four Thyrsian exiles have been defeated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(2000)
                .AddGoldReward(2400);
        }

        private void EshanScrapyardSmugglers()
        {
            _builder.Create("eshan_scrapyard_smugglers", "Scrap Without Questions")

                .AddState()
                .SetStateJournalText("Raalo Kes wants the rival smugglers occupying the caves in Eshan's Scraplands driven away. Enter the Scrapland Caves and defeat eight Scrapyard Smugglers, then return to Raalo near the smuggler landing point.")
                .AddKillObjective(NPCGroupType.Eshan_ScrapyardSmuggler, 8)

                .AddState()
                .SetStateJournalText("Eight Scrapyard Smugglers have been defeated. Return to Raalo Kes near the smuggler landing point in Eshan's Scraplands.")

                .AddXPReward(4000)
                .AddGoldReward(4800);
        }

    

        private void BreakTheGarrisonTheLine()
        {
            _builder.Create("esh_garrison_1", "Break the Garrison: The Line")

                .AddState()
                .SetStateJournalText("The Neo-Crusaders launch their raids on Eshan City from the Ruined Garrison outside its walls. Kill 10 Neo-Crusader Vanguards in the Ruined Garrison or the Winter Marches on Eshan, then return to Captain Teral Voss in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderVanguard, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Neo-Crusader Vanguards. Return to Captain Teral Voss in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void BreakTheGarrisonFieldHospital()
        {
            _builder.Create("esh_garrison_2", "Break the Garrison: Field Hospital")
                .PrerequisiteQuest("esh_garrison_1")

                .AddState()
                .SetStateJournalText("Neo-Crusader medics and gunners keep the Ruined Garrison in the fight. Kill 6 Neo-Crusader Field Medics and 6 Neo-Crusader Heavy Gunners in the Ruined Garrison or the Battlegrounds on Eshan, then return to Captain Teral Voss in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderMedic, 6)
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderHeavy, 6)

                .AddState()
                .SetStateJournalText("You have killed 6 Neo-Crusader Field Medics and 6 Neo-Crusader Heavy Gunners. Return to Captain Teral Voss in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void BreakTheGarrisonFieldCommand()
        {
            _builder.Create("esh_garrison_3", "Break the Garrison: Field Command")
                .PrerequisiteQuest("esh_garrison_2")

                .AddState()
                .SetStateJournalText("Five Neo-Crusader Captains command the soldiers of the Ruined Garrison. Kill 5 Neo-Crusader Captains in the Ruined Garrison or the Battlegrounds on Eshan, then return to Captain Teral Voss in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderCaptain, 5)

                .AddState()
                .SetStateJournalText("You have killed 5 Neo-Crusader Captains. Return to Captain Teral Voss in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(8000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.BreakTheGarrison);
                });
        }

        private void TheExiles()
        {
            _builder.Create("esh_thyrsian", "The Exiles")

                .AddState()
                .SetStateJournalText("Thyrsian Exiles are fighting both the Neo-Crusaders and the city's defenders on the Battlegrounds of Eshan. Kill 8 Thyrsian Exiles in the Battlegrounds on Eshan, then return to Captain Teral Voss in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_SunGuard, 8)

                .AddState()
                .SetStateJournalText("You have killed 8 Thyrsian Exiles. Return to Captain Teral Voss in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(4800)
                .AddXPReward(4000);
        }

        private void MarchesPatrol()
        {
            _builder.Create("esh_marches_patrol", "Marches Patrol")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The city's defense pays a standing rate for patrols that thin out the Neo-Crusaders in the Winter Marches. Kill 10 Neo-Crusader Scouts and 10 Neo-Crusader Hunters in the Winter Marches on Eshan, then return to Captain Teral Voss in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderScout, 10)
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderHunter, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Neo-Crusader Scouts and 10 Neo-Crusader Hunters. Return to Captain Teral Voss in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void GarrisonPatrol()
        {
            _builder.Create("esh_garrison_patrol", "Garrison Patrol")
                .PrerequisiteQuest("esh_garrison_3")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The city's defense pays a standing rate to keep the Neo-Crusaders from holding the Ruined Garrison again. Kill 10 Neo-Crusader Vanguards and 10 Neo-Crusader Heavy Gunners in the Ruined Garrison on Eshan, then return to Captain Teral Voss in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderVanguard, 10)
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderHeavy, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Neo-Crusader Vanguards and 10 Neo-Crusader Heavy Gunners. Return to Captain Teral Voss in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void Dispatches()
        {
            _builder.Create("esh_dispatches", "Dispatches")

                .AddState()
                .SetStateJournalText("Captain Teral Voss needs to know how many days of rations the wall has left. Ask Quartermaster Ysa Morn in Eshan City's Silver Gate District for the ration count, then return to Captain Teral Voss with her answer.")

                .AddState()
                .SetStateJournalText("Quartermaster Ysa Morn says the wall has exactly 9 days of rations. Return to Captain Teral Voss in Eshan City's Silver Gate District and tell him her answer.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void DeepSilverwood()
        {
            _builder.Create("esh_deep_silverwood", "Deep Silverwood")

                .AddState()
                .SetStateJournalText("Dire Wolf packs breeding deep in the Silverwood Expanse are driving the city's foresters out of the wood. Kill 12 Eshan Dire Wolves in the Silverwood Expanse on Eshan, then return to Huntmaster Daven Ryl in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_DireWolf, 12)

                .AddState()
                .SetStateJournalText("You have killed 12 Eshan Dire Wolves. Return to Huntmaster Daven Ryl in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void FrostFangs()
        {
            _builder.Create("esh_frost_fangs", "Frost Fangs")

                .AddState()
                .SetStateJournalText("A hard winter has pushed Frost Dire Wolves down from the high country into the Silverwood Expanse. Kill 10 Eshan Frost Dire Wolves in the Silverwood Expanse on Eshan, then return to Huntmaster Daven Ryl in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_FrostWolf, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Eshan Frost Dire Wolves. Return to Huntmaster Daven Ryl in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void ThePackLeaders()
        {
            _builder.Create("esh_pack_leaders", "The Pack Leaders")
                .PrerequisiteQuest("esh_deep_silverwood")

                .AddState()
                .SetStateJournalText("Dire Wolf Alphas hold the large packs of the Silverwood Expanse together. Kill 3 Eshan Dire Wolf Alphas in the Silverwood Expanse on Eshan, then return to Huntmaster Daven Ryl in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_DireWolfAlpha, 3)

                .AddState()
                .SetStateJournalText("You have killed 3 Eshan Dire Wolf Alphas. Return to Huntmaster Daven Ryl in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private void SilverwoodPatrol()
        {
            _builder.Create("esh_pack_patrol", "Silverwood Patrol")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The huntmaster pays a standing rate for patrols that keep the Silverwood wolf packs in check. Kill 10 Eshan Dire Wolves and 10 Eshan Frost Dire Wolves in the Silverwood Expanse on Eshan, then return to Huntmaster Daven Ryl in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_DireWolf, 10)
                .AddKillObjective(NPCGroupType.Eshan_FrostWolf, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Eshan Dire Wolves and 10 Eshan Frost Dire Wolves. Return to Huntmaster Daven Ryl in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void WinterStores()
        {
            _builder.Create("esh_wolf_meat", "Winter Stores")

                .AddState()
                .SetStateJournalText("The huntmaster is laying in winter stores of cured wolf meat for Eshan City. Collect 8 Eshan Dire Wolf Meat from the Dire Wolves of the Silverwood Expanse, then bring them to Huntmaster Daven Ryl in Eshan City's Silver Gate District.")
                .AddCollectItemObjective("esh_wolf_meat", 8)

                .AddState()
                .SetStateJournalText("You handed over 8 Eshan Dire Wolf Meat. Speak to Huntmaster Daven Ryl in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(4800)
                .AddXPReward(4000);
        }

        private void TheIceboundsHide()
        {
            _builder.Create("esh_ridged_hide", "The Icebound's Hide")

                .AddState()
                .SetStateJournalText("The huntmaster wants the hide of Gorak Vesh, the Icebound, to hang in the hunting lodge. Collect 1 Ridged Chiro Hide from Gorak Vesh, the Icebound, in the Silverwood Expanse, then bring it to Huntmaster Daven Ryl in Eshan City's Silver Gate District.")
                .AddCollectItemObjective("esh_gorak_hide", 1)

                .AddState()
                .SetStateJournalText("You handed over 1 Ridged Chiro Hide. Speak to Huntmaster Daven Ryl in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private void HideTally()
        {
            _builder.Create("esh_hide_tally", "Hide Tally")

                .AddState()
                .SetStateJournalText("Huntmaster Daven Ryl needs to know how many hides the city wants this winter. Ask Quartermaster Ysa Morn in Eshan City's Silver Gate District for the figure, then return to Huntmaster Daven Ryl with her answer.")

                .AddState()
                .SetStateJournalText("Quartermaster Ysa Morn says the city wants 200 hides this winter, properly cured. Return to Huntmaster Daven Ryl in Eshan City's Silver Gate District and tell him her answer.")

                .AddGoldReward(1500)
                .AddXPReward(2000);
        }

        private void CapturedRations()
        {
            _builder.Create("esh_rations", "Captured Rations")

                .AddState()
                .SetStateJournalText("The Neo-Crusaders are eating food stolen from Eshan's farms. Collect 6 Eshan White Rice and 4 Eshan Farm Eggs from the Neo-Crusaders of the Winter Marches, the Battlegrounds or the Ruined Garrison, then bring them to Quartermaster Ysa Morn in Eshan City's Silver Gate District.")
                .AddCollectItemObjective("esh_rice", 6)
                .AddCollectItemObjective("esh_egg", 4)

                .AddState()
                .SetStateJournalText("You handed over 6 Eshan White Rice and 4 Eshan Farm Eggs. Speak to Quartermaster Ysa Morn in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(4800)
                .AddXPReward(4000);
        }

        private void TheExilesLarder()
        {
            _builder.Create("esh_chili", "The Exiles' Larder")

                .AddState()
                .SetStateJournalText("The district kitchens want the chili oil and pickled greens the Thyrsian Exiles make. Collect 4 Eshan Red Chili Oil and 4 Eshan Pickled Greens from the Thyrsian Exiles of the Battlegrounds, then bring them to Quartermaster Ysa Morn in Eshan City's Silver Gate District.")
                .AddCollectItemObjective("esh_chili_oil", 4)
                .AddCollectItemObjective("esh_pickles", 4)

                .AddState()
                .SetStateJournalText("You handed over 4 Eshan Red Chili Oil and 4 Eshan Pickled Greens. Speak to Quartermaster Ysa Morn in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(4800)
                .AddXPReward(4000);
        }

        private void ScraplandTrade()
        {
            _builder.Create("esh_fish", "Scrapland Trade")

                .AddState()
                .SetStateJournalText("Scrapyard Smugglers are shipping stolen Eshan food off-world. Collect 5 Eshan River-Fish Fillets and 5 Eshan Farm Poultry from the Scrapyard Smugglers of the Scraplands Caves, then bring them to Quartermaster Ysa Morn in Eshan City's Silver Gate District.")
                .AddCollectItemObjective("esh_fish", 5)
                .AddCollectItemObjective("esh_poultry", 5)

                .AddState()
                .SetStateJournalText("You handed over 5 Eshan River-Fish Fillets and 5 Eshan Farm Poultry. Speak to Quartermaster Ysa Morn in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(4800)
                .AddXPReward(4000);
        }

        private void EyesOnTheConvoys()
        {
            _builder.Create("esh_scout_cache", "Eyes on the Convoys")

                .AddState()
                .SetStateJournalText("Neo-Crusader Scouts in the Winter Marches are tracking Eshan's supply convoys for the raiders. Kill 10 Neo-Crusader Scouts in the Winter Marches on Eshan, then return to Quartermaster Ysa Morn in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderScout, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Neo-Crusader Scouts. Return to Quartermaster Ysa Morn in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void TheRaiders()
        {
            _builder.Create("esh_hunter_line", "The Raiders")

                .AddState()
                .SetStateJournalText("Neo-Crusader Hunters in the Winter Marches are raiding Eshan's supply convoys. Kill 10 Neo-Crusader Hunters in the Winter Marches on Eshan, then return to Quartermaster Ysa Morn in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderHunter, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Neo-Crusader Hunters. Return to Quartermaster Ysa Morn in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void ScraplandsSweep()
        {
            _builder.Create("esh_smuggler_sweep", "Scraplands Sweep")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The quartermaster pays a fixed rate to keep smugglers out of the Scraplands Caves. Kill 20 Scrapyard Smugglers in the Scraplands Caves on Eshan, then return to Quartermaster Ysa Morn in Eshan City's Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_ScrapyardSmuggler, 20)

                .AddState()
                .SetStateJournalText("You have killed 20 Scrapyard Smugglers. Return to Quartermaster Ysa Morn in Eshan City's Silver Gate District for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }
    }
}
