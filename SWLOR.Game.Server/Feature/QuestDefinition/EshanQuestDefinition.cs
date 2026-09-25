using System.Collections.Generic;
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
            EshanScoutScreen();
            EshanHunterTrap();
            EshanVanguardBreak();
            EshanHeavySilence();
            EshanMedicCutoff();
            EshanCaptainFall();
            EshanThyrsianExiles();
            EshanScrapyardSmugglers();
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

                .AddXPReward(6500)
                .AddGoldReward(5000);
        }

        private void EshanFrostLine()
        {
            _builder.Create("eshan_frost_line", "Break the Frost Pack")

                .AddState()
                .SetStateJournalText("Defeat six Eshan Frost Dire Wolves beyond Eshan City, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_FrostWolf, 6)

                .AddState()
                .SetStateJournalText("Six frost dire wolves have been defeated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(8000)
                .AddGoldReward(6500);
        }

        private void EshanAlphaHunt()
        {
            _builder.Create("eshan_alpha_hunt", "The Alpha's Challenge")

                .AddState()
                .SetStateJournalText("Hunt the Eshan Dire Wolf Alpha threatening the city approaches, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_DireWolfAlpha, 1)

                .AddState()
                .SetStateJournalText("The dire wolf alpha is dead. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(11000)
                .AddGoldReward(9000);
        }

        private void EshanScoutScreen()
        {
            _builder.Create("eshan_scout_screen", "Eyes in the Snow")

                .AddState()
                .SetStateJournalText("Eliminate seven Neo-Crusader Scouts operating in the Winter Marches, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderScout, 7)

                .AddState()
                .SetStateJournalText("Seven Neo-Crusader scouts have been eliminated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(12000)
                .AddGoldReward(10000);
        }

        private void EshanHunterTrap()
        {
            _builder.Create("eshan_hunter_trap", "Close the Hunter's Trail")

                .AddState()
                .SetStateJournalText("Eliminate six Neo-Crusader Hunters operating near the Veylan Groves, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderHunter, 6)

                .AddState()
                .SetStateJournalText("Six Neo-Crusader hunters have been eliminated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(13500)
                .AddGoldReward(11000);
        }

        private void EshanVanguardBreak()
        {
            _builder.Create("eshan_vanguard_break", "Break the Vanguard")

                .AddState()
                .SetStateJournalText("Defeat eight Neo-Crusader Vanguards probing Eshan's defenses, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderVanguard, 8)

                .AddState()
                .SetStateJournalText("Eight Neo-Crusader vanguards have fallen. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(15000)
                .AddGoldReward(12500);
        }

        private void EshanHeavySilence()
        {
            _builder.Create("eshan_heavy_silence", "Silence the Heavy Guns")

                .AddState()
                .SetStateJournalText("Eliminate five Neo-Crusader Heavy Troopers threatening Eshan's routes, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderHeavy, 5)

                .AddState()
                .SetStateJournalText("Five Neo-Crusader heavy troopers have been eliminated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(17000)
                .AddGoldReward(14000);
        }

        private void EshanMedicCutoff()
        {
            _builder.Create("eshan_medic_cutoff", "Cut the Lifeline")

                .AddState()
                .SetStateJournalText("Eliminate five Neo-Crusader Medics supporting enemy assault cells, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderMedic, 5)

                .AddState()
                .SetStateJournalText("Five Neo-Crusader medics have been eliminated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(18000)
                .AddGoldReward(15000);
        }

        private void EshanCaptainFall()
        {
            _builder.Create("eshan_captain_fall", "Shatter Field Command")

                .AddState()
                .SetStateJournalText("Defeat three Neo-Crusader Captains directing attacks against Eshan, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_NeoCrusaderCaptain, 3)

                .AddState()
                .SetStateJournalText("Three Neo-Crusader captains have fallen. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(21000)
                .AddGoldReward(18000);
        }

        private void EshanThyrsianExiles()
        {
            _builder.Create("eshan_sunguard_stand", "Thyrsian Incursion")

                .AddState()
                .SetStateJournalText("Defeat four former Revanite Thyrsian warriors fighting on Eshan, then report to Security Officer Talia Venn in the Silver Gate District.")
                .AddKillObjective(NPCGroupType.Eshan_ThyrsianExile, 4)

                .AddState()
                .SetStateJournalText("Four Thyrsian exiles have been defeated. Return to Security Officer Talia Venn in Eshan City's Silver Gate District.")

                .AddXPReward(25000)
                .AddGoldReward(22000);
        }

        private void EshanScrapyardSmugglers()
        {
            _builder.Create("eshan_scrapyard_smugglers", "Scrap Without Questions")

                .AddState()
                .SetStateJournalText("Raalo Kes wants the rival smugglers occupying the caves in Eshan's Scraplands driven away. Enter the Scrapland Caves and defeat eight Scrapyard Smugglers, then return to Raalo near the smuggler landing point.")
                .AddKillObjective(NPCGroupType.Eshan_ScrapyardSmuggler, 8)

                .AddState()
                .SetStateJournalText("Eight Scrapyard Smugglers have been defeated. Return to Raalo Kes near the smuggler landing point in Eshan's Scraplands.")

                .AddXPReward(15000)
                .AddGoldReward(13000);
        }

    }
}
