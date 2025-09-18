using System.Collections.Generic;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class KorribanQuestlineDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            MeetTheInquisitor();
            TheArtifactRecovery();
            TheSithCodeTest();
            ProvingYourDominance();
            EliminateKlorSlug();
            SkinsInTheShadows();
            FactoryWorkerParts();
            FrogBoss();
            return _builder.Build();
        }

        // Quest 1: Meeting the Inquisitor
        private void MeetTheInquisitor()
        {
            _builder.Create("meet_inquisitor", "Meeting the Inquisitor")

                .AddState()
                .SetStateJournalText("The recruiter has instructed you to meet with Inquisitor Dral'kor Keth in the Sith Academy. Speak with him to begin your training.")
                

                .AddState()
                .SetStateJournalText("You have met Inquisitor Dral'kor Keth. Return to him when you are ready to prove your worth.")
                .AddXPReward(1500);
        }

        // Quest 2: The Artifact Recovery
        private void TheArtifactRecovery()
        {
            _builder.Create("artifact_recovery", "The Artifact Recovery")
                .PrerequisiteQuest("meet_inquisitor")

                .AddState()
                .SetStateJournalText("Inquisitor Dral'kor Keth has tasked you with retrieving a stolen Sith artifacts from rogue initiates in the outskirts of the Sith Academy.")
                .AddKillObjective(NPCGroupType.Korriban_RogueInitiates, 3)
                .AddCollectItemObjective("stolen_s_artifac", 3)

                .AddState()
                .SetStateJournalText("You have recovered the stolen Sith artifact. Return it to Inquisitor Dral'kor Keth.")
                .AddXPReward(2000)
                .AddGoldReward(1000);
                
        }

        // Quest 3: The Sith Code Test
        private void TheSithCodeTest()
        {
            _builder.Create("sith_code_test", "The Sith Code Test")
                .PrerequisiteQuest("artifact_recovery")

                .AddState()
                .SetStateJournalText("Inquisitor Dral'kor Keth wishes to test your understanding of the Sith Code. You must answer his questions correctly to proceed.")
                

                .AddState()
                .SetStateJournalText("You have demonstrated your knowledge of the Sith Code.")
                .AddXPReward(5000)
                .AddGoldReward(1500);
        }

        // Quest 4: Proving Your Dominance
        private void ProvingYourDominance()
        {
            _builder.Create("prove_dominance", "Proving Your Dominance")
                .PrerequisiteQuest("sith_code_test")

                .AddState()
                .SetStateJournalText("Inquisitor Dral'kor Keth has given you a final test: eliminate another Sith apprentice who has shown weakness. Travel to the Valley Temples and complete this task.")
                .AddKillObjective(NPCGroupType.Korriban_SithApprenticeGhost, 1)

                .AddState()
                .SetStateJournalText("You have eliminated the weak apprentice. Return to Inquisitor Dral'kor Keth to complete your training.")
                .AddXPReward(8000)
                .AddGoldReward(3000)
                .AddItemReward("apprentice_dark_", 1);
        }
        private void FrogBoss()
        {
            _builder.Create("alchemized_frog", "Curse of the Alchemized One")
                .PrerequisiteQuest("prove_dominance")

                .AddState()
                .SetStateJournalText("Warrior Camila has sensed dark alchemical energy emanating from the ancient tombs. She tasks you with investigating the disturbance.")
                .AddKillObjective(NPCGroupType.Korriban_AlchemizedFrog, 1)
                .AddState()

                .AddState()
                .SetStateJournalText("The creature is slain. Return to Dral'kor Keth and report your findings.")
                .AddXPReward(10000)
                .AddGoldReward(4000);
                
        }
        private void EliminateKlorSlug()
        {
            _builder.Create("eliminate_klorslug", "Eliminate the K'lor'slug")

                .AddState()
                .SetStateJournalText("A Korriban citizen has requested your help in eliminating dangerous K'lor'slugs threatening the wastes.")
                .AddKillObjective(NPCGroupType.Korriban_Hssiss, 10)

                .AddState()
                .SetStateJournalText("You have successfully eliminated the K'lor'slugs. Return to the citizen to inform them of your success.")
                .AddXPReward(5000)
                .AddGoldReward(1000)
                .AddItemReward("slug_surprise", 1);
        }
        private void SkinsInTheShadows()
        {
            _builder.Create("hides_tukata", "Skins in the Shadows")

                .AddState()
                .SetStateJournalText("A nervous smuggler in the shadows has asked you to gather hides from the fierce Tuk’ata beasts roaming the inside of the tombs. Be cautious.")
                .AddCollectItemObjective("tukata_hide", 5)

                .AddState()
                .SetStateJournalText("You've gathered the Tuk'ata hides. Return to the smuggler — and don't draw any attention.")
                .AddXPReward(6000)
                .AddGoldReward(2000);

        }
        private void FactoryWorkerParts()
        {
            _builder.Create("factory_worker_parts", "Factory Worker Needs Parts")

                .AddState()
                .SetStateJournalText("A factory worker has requested your help in gathering electronic parts to repair malfunctioning droids.")
                .AddCollectItemObjective("elec_flawed", 30) 

                .AddState()
                .SetStateJournalText("You have collected the electronic parts. Return to the factory worker.")
                .AddXPReward(5000)
                .AddGoldReward(1800);
        }
    }
}



