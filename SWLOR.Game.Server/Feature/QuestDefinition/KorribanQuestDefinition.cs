using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using static SWLOR.Game.Server.Composition.GameServices;

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
            SwarmSeason();
            SerpentsInTheRavine();
            TheShyrackCaves();
            WraidOnTheDunes();
            WastesPatrol();
            OreForTheGarrison();
            ClawBroth();
            SerpentBile();
            WingsOnTheGrill();
            AMildToxin();
            TeethForTheWall();
            TheRestlessApprentices();
            ColdInTheCrypt();
            TheTombHounds();
            RogueRoster();
            StormTheFortressTheGuard();
            StormTheFortressBladesAndSorcery();
            StormTheFortressTheGate();
            StormTheFortressTheMaster();
            ThePrototype();
            TheCouncilsShadow();

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
                .AddGoldReward(1500);

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
                .AddGoldReward(2250);
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
                .AddGoldReward(4500)
                .AddItemReward("apprentice_dark_", 1);
        }
        private void FrogBoss()
        {
            _builder.Create("alchemized_frog", "Curse of the Alchemized One")
                .PrerequisiteQuest("prove_dominance")
                .OnAdvanceAction((player, sourceObject, state) =>
                {
                    // Kill credit advances each eligible party member separately.
                    if (state == 2)
                        CreateItemOnObject("frogguts", player, 1);
                })

                .AddState()
                .SetStateJournalText("Sith Warrior Camila in the Sith Academy has asked you to slay the alchemized frog in the ancient tombs and recover its Frog Guts.")
                .AddKillObjective(NPCGroupType.Korriban_AlchemizedFrog, 1)

                .AddState()
                .SetStateJournalText("The alchemized frog is slain. You recovered its Frog Guts. Bring them to Sith Warrior Camila in the Sith Academy.")
                .AddCollectItemObjective("frogguts", 1)

                .AddState()
                .SetStateJournalText("You have delivered the Frog Guts. Speak with Sith Warrior Camila in the Sith Academy to receive your reward.")
                .AddXPReward(10000)
                .AddGoldReward(6000);

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
                .AddGoldReward(1500)
                .AddItemReward("slug_surprise", 1);
        }
        private void SkinsInTheShadows()
        {
            _builder.Create("hides_tukata", "Skins in the Shadows")

                .AddState()
                .SetStateJournalText("A nervous smuggler in the shadows has asked you to gather hides from the fierce Tuk'ata beasts roaming the inside of the tombs. Be cautious.")
                .AddCollectItemObjective("tukata_hide", 5)

                .AddState()
                .SetStateJournalText("You've gathered the Tuk'ata hides. Return to the smuggler - and don't draw any attention.")
                .AddXPReward(6000)
                .AddGoldReward(3000);

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
                .AddGoldReward(2700);
        }
    

        private void SwarmSeason()
        {
            _builder.Create("kor_pelko", "Swarm Season")

                .AddState()
                .SetStateJournalText("Pelko Bug Swarms are fouling the supply road that feeds the Korriban starport. Kill 12 Pelko Bug Swarms in the Wastelands, the Ravine or the Dunes on Korriban, then return to Sergeant Varn Tolek at the Korriban Starport.")
                .AddKillObjective(NPCGroupType.Korriban_Tukata, 12)

                .AddState()
                .SetStateJournalText("You have killed 12 Pelko Bug Swarms. Return to Sergeant Varn Tolek at the Korriban Starport for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000);
        }

        private void SerpentsInTheRavine()
        {
            _builder.Create("kor_serpents", "Serpents in the Ravine")

                .AddState()
                .SetStateJournalText("Moraband Serpents are killing the garrison's patrols in the Ravine. Kill 10 Moraband Serpents in the Ravine or the Caverns on Korriban, then return to Sergeant Varn Tolek at the Korriban Starport.")
                .AddKillObjective(NPCGroupType.Korriban_MorabandSerpent, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Moraband Serpents. Return to Sergeant Varn Tolek at the Korriban Starport for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000);
        }

        private void TheShyrackCaves()
        {
            _builder.Create("kor_shyrack", "The Shyrack Caves")

                .AddState()
                .SetStateJournalText("Shyrack have overrun the Caverns where the starport garrison stores its munitions. Kill 10 Shyrack in the Caverns on Korriban, then return to Sergeant Varn Tolek at the Korriban Starport.")
                .AddKillObjective(NPCGroupType.Korriban_Shyrack, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Shyrack. Return to Sergeant Varn Tolek at the Korriban Starport for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000);
        }

        private void WraidOnTheDunes()
        {
            _builder.Create("kor_wraid", "Wraid on the Dunes")

                .AddState()
                .SetStateJournalText("Wraid are attacking the starport garrison's patrols in the Dunes. Kill 8 Wraid in the Dunes on Korriban, then return to Sergeant Varn Tolek at the Korriban Starport.")
                .AddKillObjective(NPCGroupType.Korriban_Terentatek, 8)

                .AddState()
                .SetStateJournalText("You have killed 8 Wraid. Return to Sergeant Varn Tolek at the Korriban Starport for your reward.")

                .AddGoldReward(2250)
                .AddXPReward(5000);
        }

        private void WastesPatrol()
        {
            _builder.Create("kor_wastes_patrol", "Wastes Patrol")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The Korriban starport garrison pays outsiders to patrol the Wastelands. Kill 10 Pelko Bug Swarms and 10 K'lor'slugs in the Wastelands on Korriban, then return to Sergeant Varn Tolek at the Korriban Starport.")
                .AddKillObjective(NPCGroupType.Korriban_Tukata, 10)
                .AddKillObjective(NPCGroupType.Korriban_Hssiss, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Pelko Bug Swarms and 10 K'lor'slugs. Return to Sergeant Varn Tolek at the Korriban Starport for your reward.")

                .AddGoldReward(400)
                .AddXPReward(950);
        }

        private void OreForTheGarrison()
        {
            _builder.Create("kor_ore", "Ore for the Garrison")

                .AddState()
                .SetStateJournalText("The starport garrison needs ore so that its armorer can repair damaged armor. Collect 8 Raw Plagionite and 8 Raw Scordspar mined in the Caverns or the Dunes on Korriban, then bring them to Sergeant Varn Tolek at the Korriban Starport.")
                .AddCollectItemObjective("raw_plagionite", 8)
                .AddCollectItemObjective("raw_scordspar", 8)

                .AddState()
                .SetStateJournalText("You handed over 8 Raw Plagionite and 8 Raw Scordspar. Speak to Sergeant Varn Tolek at the Korriban Starport for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000);
        }

        private void ClawBroth()
        {
            _builder.Create("kor_slug_claws", "Claw Broth")

                .AddState()
                .SetStateJournalText("Brask cooks K'lor'slug for the troopers in the Korriban Starport Cantina. Collect 5 K'lor'slug Claws and 5 K'lor'slug Meat from the K'lor'slugs of the Wastelands, then bring them to Brask in the Korriban Starport Cantina.")
                .AddCollectItemObjective("klorslug_tail", 5)
                .AddCollectItemObjective("klorslug_meat", 5)

                .AddState()
                .SetStateJournalText("You handed over 5 K'lor'slug Claws and 5 K'lor'slug Meat. Speak to Brask in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000)
                .AddItemReward("slug_surprise", 1);
        }

        private void SerpentBile()
        {
            _builder.Create("kor_serpent_bile", "Serpent Bile")

                .AddState()
                .SetStateJournalText("Brask distills a strong spirit from Moraband Serpent Bile. Collect 4 Moraband Serpent Bile and 4 Moraband Serpent Meat from the Moraband Serpents of the Ravine, then bring them to Brask in the Korriban Starport Cantina.")
                .AddCollectItemObjective("mserp_bile", 4)
                .AddCollectItemObjective("mserp_meat", 4)

                .AddState()
                .SetStateJournalText("You handed over 4 Moraband Serpent Bile and 4 Moraband Serpent Meat. Speak to Brask in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000);
        }

        private void WingsOnTheGrill()
        {
            _builder.Create("kor_shyrack_wings", "Wings on the Grill")

                .AddState()
                .SetStateJournalText("Brask grills Shyrack wing, the most popular dish in the Korriban Starport Cantina. Collect 6 Shyrack Wings from the Shyrack of the Caverns, then bring them to Brask in the Korriban Starport Cantina.")
                .AddCollectItemObjective("shyrack_wing", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 Shyrack Wings. Speak to Brask in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000);
        }

        private void AMildToxin()
        {
            _builder.Create("kor_pelko_toxin", "A Mild Toxin")

                .AddState()
                .SetStateJournalText("Brask seasons his stew with a small measure of Pelko Bug Toxin. Collect 5 Pelko Bug Toxin and 5 Pelko Bug Meat from the Pelko Bug Swarms of the Wastelands, then bring them to Brask in the Korriban Starport Cantina.")
                .AddCollectItemObjective("pelko_blood", 5)
                .AddCollectItemObjective("pelko_meat", 5)

                .AddState()
                .SetStateJournalText("You handed over 5 Pelko Bug Toxin and 5 Pelko Bug Meat. Speak to Brask in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000);
        }

        private void TeethForTheWall()
        {
            _builder.Create("kor_wraid_trophies", "Teeth for the Wall")

                .AddState()
                .SetStateJournalText("Brask wants Wraid trophies to hang on the bare wall of his cantina. Collect 4 Korriban Wraid Teeth and 4 Korriban Wraid Claws from the Wraid of the Dunes, then bring them to Brask in the Korriban Starport Cantina.")
                .AddCollectItemObjective("wraid_tooth", 4)
                .AddCollectItemObjective("wraid_claw2", 4)

                .AddState()
                .SetStateJournalText("You handed over 4 Korriban Wraid Teeth and 4 Korriban Wraid Claws. Speak to Brask in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(2250)
                .AddXPReward(5000);
        }

        private void TheRestlessApprentices()
        {
            _builder.Create("kor_crypt_ghosts", "The Restless Apprentices")

                .AddState()
                .SetStateJournalText("The ghosts of failed Sith Apprentices are killing acolytes in the Sith Crypt. Kill 8 Sith Apprentices in the Sith Crypt on Korriban, then return to Malen Dray in the Korriban Starport Cantina.")
                .AddKillObjective(NPCGroupType.Korriban_SithApprenticeGhost, 8)

                .AddState()
                .SetStateJournalText("You have killed 8 Sith Apprentices. Return to Malen Dray in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(3000)
                .AddXPReward(6000);
        }

        private void ColdInTheCrypt()
        {
            _builder.Create("kor_frostbinders", "Cold in the Crypt")

                .AddState()
                .SetStateJournalText("Sith Frostbinders haunt the Sith Crypt alongside the ghosts of the apprentices. Kill 4 Sith Frostbinders in the Sith Crypt on Korriban, then return to Malen Dray in the Korriban Starport Cantina.")
                .AddKillObjective(NPCGroupType.Korriban_SithFrostbinder, 4)

                .AddState()
                .SetStateJournalText("You have killed 4 Sith Frostbinders. Return to Malen Dray in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(3000)
                .AddXPReward(6000);
        }

        private void TheTombHounds()
        {
            _builder.Create("kor_tukata", "The Tomb Hounds")

                .AddState()
                .SetStateJournalText("Tuk'ata guard the temple interiors and kill the acolytes who are sent there. Kill 6 Tuk'ata in the Valley Temple Interiors on Korriban, then return to Malen Dray in the Korriban Starport Cantina.")
                .AddKillObjective(NPCGroupType.Korriban_TukataHound, 6)

                .AddState()
                .SetStateJournalText("You have killed 6 Tuk'ata. Return to Malen Dray in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(3000)
                .AddXPReward(6000);
        }

        private void RogueRoster()
        {
            _builder.Create("kor_initiates", "Rogue Roster")

                .AddState()
                .SetStateJournalText("Initiates who deserted the Sith Academy are hiding in the temples. Kill 6 Rogue Initiates in the Valley Temple Interiors on Korriban, then return to Malen Dray in the Korriban Starport Cantina.")
                .AddKillObjective(NPCGroupType.Korriban_RogueInitiates, 6)

                .AddState()
                .SetStateJournalText("You have killed 6 Rogue Initiates. Return to Malen Dray in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(1500)
                .AddXPReward(5000);
        }

        private void StormTheFortressTheGuard()
        {
            _builder.Create("kor_fort_1", "Storm the Fortress: The Guard")

                .AddState()
                .SetStateJournalText("A renegade Sith Temple Master has sealed himself in the Sith Fortress with a private army. Kill 12 Sith Temple Guards in the Sith Fortress on Korriban, then return to Malen Dray in the Korriban Starport Cantina.")
                .AddKillObjective(NPCGroupType.Korriban_FortressGuard, 12)

                .AddState()
                .SetStateJournalText("You have killed 12 Sith Temple Guards. Return to Malen Dray in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void StormTheFortressBladesAndSorcery()
        {
            _builder.Create("kor_fort_2", "Storm the Fortress: Blades and Sorcery")
                .PrerequisiteQuest("kor_fort_1")

                .AddState()
                .SetStateJournalText("The renegade Temple Master's Marauders and Sorceresses hold the courtyard of the Sith Fortress. Kill 6 Sith Marauders or Sorceresses in the Sith Fortress on Korriban, then return to Malen Dray in the Korriban Starport Cantina.")
                .AddKillObjective(NPCGroupType.Korriban_FortressMarauder, 6)

                .AddState()
                .SetStateJournalText("You have killed 6 Sith Marauders or Sorceresses. Return to Malen Dray in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void StormTheFortressTheGate()
        {
            _builder.Create("kor_fort_3", "Storm the Fortress: The Gate")
                .PrerequisiteQuest("kor_fort_2")

                .AddState()
                .SetStateJournalText("The Temple Guardian holds the gate that leads to the Temple Master in the Sith Fortress. Kill the Temple Guardian in the Sith Fortress on Korriban, then return to Malen Dray in the Korriban Starport Cantina.")
                .AddKillObjective(NPCGroupType.Korriban_TempleGuardian, 1)

                .AddState()
                .SetStateJournalText("You have killed the Temple Guardian. Return to Malen Dray in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void StormTheFortressTheMaster()
        {
            _builder.Create("kor_fort_4", "Storm the Fortress: The Master")
                .PrerequisiteQuest("kor_fort_3")

                .AddState()
                .SetStateJournalText("The renegade Sith Temple Master waits in the Lord's Quarters of the Sith Fortress. Kill the Sith Temple Master in the Lord's Quarters of the Sith Fortress on Korriban, then return to Malen Dray in the Korriban Starport Cantina.")
                .AddKillObjective(NPCGroupType.Korriban_TempleMaster, 1)

                .AddState()
                .SetStateJournalText("You have killed the Sith Temple Master. Return to Malen Dray in the Korriban Starport Cantina for your reward.")

                .AddGoldReward(12000)
                .AddXPReward(15000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.StormTheFortress);
                });
        }

        private void ThePrototype()
        {
            _builder.Create("kor_warform", "The Prototype")

                .AddState()
                .SetStateJournalText("An Imperial Prototype Warform built in the Sith Fortress has turned on its makers. Kill the Imperial Prototype Warform in the Forgeworks of the Sith Fortress or the Valley Temple Interiors on Korriban, then return to Karth Veyl at the Korriban Starport.")
                .AddKillObjective(NPCGroupType.Korriban_PrototypeWarform, 1)

                .AddState()
                .SetStateJournalText("You have killed the Imperial Prototype Warform. Return to Karth Veyl at the Korriban Starport for your reward.")

                .AddGoldReward(12000)
                .AddXPReward(15000);
        }

        private void TheCouncilsShadow()
        {
            _builder.Create("kor_council_guard", "The Council's Shadow")

                .AddState()
                .SetStateJournalText("A Temple Council Guardian stands in the Lord's Quarters of the Sith Fortress. Kill the Temple Council Guardian in the Lord's Quarters of the Sith Fortress on Korriban, then return to Karth Veyl at the Korriban Starport.")
                .AddKillObjective(NPCGroupType.Korriban_CouncilGuardian, 1)

                .AddState()
                .SetStateJournalText("You have killed the Temple Council Guardian. Return to Karth Veyl at the Korriban Starport for your reward.")

                .AddGoldReward(12000)
                .AddXPReward(15000);
        }
    }
}
