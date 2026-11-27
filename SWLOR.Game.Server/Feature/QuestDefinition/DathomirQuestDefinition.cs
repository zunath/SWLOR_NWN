using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class DathomirQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            ClearingThePerimeter();
            VeinsOfTheMountain();
            HyphaeHarvest();
            FieldRations();
            SilkAndVenom();
            WebCutters();
            PurboleCull();
            ApexSurveySprantal();
            ApexSurveySquellbug();
            ApexSurveySsurian();
            DeepJungleSweep();
            KwiEncroachmentDrumsInTheDark();
            KwiEncroachmentStolenRites();
            KwiEncroachmentBreakTheCircle();
            JungleWatch();
            TheShellOfTheOldOne();
            WingsOverTheGrotto();
            TheRancorPit();
            ShadowInTheCanopy();
            TheTrophyWall();

            return _builder.Build();
        }

        private void ClearingThePerimeter()
        {
            _builder.Create("dath_perimeter", "Clearing the Perimeter")

                .AddState()
                .SetStateJournalText("Shear Mites and Jungle Bugs keep wrecking the perimeter fence of the Czerka base. Kill 10 Jungle Bugs and 10 Shear Mites in the Tarnished Jungles on Dathomir, then return to Site Engineer Halv Renner in the Czerka Base on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_SwamplandBug, 10)
                .AddKillObjective(NPCGroupType.Dathomir_ShearMite, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Jungle Bugs and 10 Shear Mites in the Tarnished Jungles. Return to Site Engineer Halv Renner in the Czerka Base on Dathomir to collect your pay.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void VeinsOfTheMountain()
        {
            _builder.Create("dath_mountain_ore", "Veins of the Mountain")

                .AddState()
                .SetStateJournalText("The Czerka base needs ore to cast a replacement generator housing. Mine 10 Raw Arkoxit and 10 Raw Jasioclase on Dathomir, then bring them to Site Engineer Halv Renner in the Czerka Base on Dathomir.")
                .AddCollectItemObjective("raw_arkoxit", 10)
                .AddCollectItemObjective("raw_jasioclase", 10)

                .AddState()
                .SetStateJournalText("You have 10 Raw Arkoxit and 10 Raw Jasioclase. Bring them to Site Engineer Halv Renner in the Czerka Base on Dathomir to collect your pay.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void HyphaeHarvest()
        {
            _builder.Create("dath_hyphae", "Hyphae Harvest")

                .AddState()
                .SetStateJournalText("The palisade around the Czerka base is rotting. Cut 10 Hyphae Wood from the hyphae trees of Dathomir, then bring it to Site Engineer Halv Renner in the Czerka Base on Dathomir.")
                .AddCollectItemObjective("hyphae_wood", 10)

                .AddState()
                .SetStateJournalText("You have 10 Hyphae Wood. Bring it to Site Engineer Halv Renner in the Czerka Base on Dathomir to collect your pay.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void FieldRations()
        {
            _builder.Create("dath_rations", "Field Rations")

                .AddState()
                .SetStateJournalText("The Czerka mess has run out of fresh food. Gather 6 Dathomir Oregano and 6 Turnips on Dathomir, then bring them to Site Engineer Halv Renner in the Czerka Base on Dathomir.")
                .AddCollectItemObjective("herb_x", 6)
                .AddCollectItemObjective("turnip", 6)

                .AddState()
                .SetStateJournalText("You have 6 Dathomir Oregano and 6 Turnips. Bring them to Site Engineer Halv Renner in the Czerka Base on Dathomir to collect your pay.")

                .AddGoldReward(3750)
                .AddXPReward(4000)
                .AddItemReward("dath_hsoup", 3);
        }

        private void SilkAndVenom()
        {
            _builder.Create("dath_silk_venom", "Silk and Venom")

                .AddState()
                .SetStateJournalText("Dr. Imra Sol needs Gaping Spider samples to make an antivenom. Collect 6 Spider Guts and 6 Spider Legs from the Gaping Spiders of the Hidden Cave on Dathomir, then bring them to her at the Jungle Landing on Dathomir.")
                .AddCollectItemObjective("spider_guts", 6)
                .AddCollectItemObjective("spider_leg", 6)

                .AddState()
                .SetStateJournalText("You have 6 Spider Guts and 6 Spider Legs. Bring them to Dr. Imra Sol at the Jungle Landing on Dathomir to collect your fee.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void WebCutters()
        {
            _builder.Create("dath_web_cutters", "Web Cutters")

                .AddState()
                .SetStateJournalText("Gaping Spiders have webbed over the Hidden Cave, north-east of the Jungle Landing. Kill 10 Gaping Spiders in the Hidden Cave on Dathomir, then return to Dr. Imra Sol at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_GapingSpider, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Gaping Spiders in the Hidden Cave. Return to Dr. Imra Sol at the Jungle Landing on Dathomir to collect your fee.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void PurboleCull()
        {
            _builder.Create("dath_purbole", "Purbole Cull")

                .AddState()
                .SetStateJournalText("Purbole packs are denning in the ruins that Dr. Imra Sol wants to study. Kill 12 Purbole in the Cave Ruins, the Ruin Base or the Grotto Caverns on Dathomir, then return to her at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_Purbole, 12)

                .AddState()
                .SetStateJournalText("You killed 12 Purbole in the ruins of Dathomir. Return to Dr. Imra Sol at the Jungle Landing on Dathomir to collect your fee.")

                .AddGoldReward(3750)
                .AddXPReward(4000);
        }

        private void ApexSurveySprantal()
        {
            _builder.Create("dath_apex_1", "Apex Survey: Sprantal")

                .AddState()
                .SetStateJournalText("Dr. Imra Sol is surveying the apex predators of Dathomir and needs to see how Sprantal fight. Kill 10 Sprantal in the Mountains or the Mountain Caves, then return to her at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_Sprantal, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Sprantal. Return to Dr. Imra Sol at the Jungle Landing on Dathomir to collect the survey rate.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void ApexSurveySquellbug()
        {
            _builder.Create("dath_apex_2", "Apex Survey: Squellbug")
                .PrerequisiteQuest("dath_apex_1")

                .AddState()
                .SetStateJournalText("The second apex predator candidate in Dr. Imra Sol's survey is the Squellbug. Kill 12 Squellbugs in the Grottos, the Mountains or the Mountain Caves of Dathomir, then return to her at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_Squellbug, 12)

                .AddState()
                .SetStateJournalText("You killed 12 Squellbugs. Return to Dr. Imra Sol at the Jungle Landing on Dathomir to collect the survey rate.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void ApexSurveySsurian()
        {
            _builder.Create("dath_apex_3", "Apex Survey: Ssurian")
                .PrerequisiteQuest("dath_apex_2")

                .AddState()
                .SetStateJournalText("The last apex predator candidate in Dr. Imra Sol's survey is the Ssurian. Kill 10 Ssurians in the Grottos, the Grotto Caverns or the Mountain Caves of Dathomir, then return to her at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_Ssurian, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Ssurians. Return to Dr. Imra Sol at the Jungle Landing on Dathomir to collect the survey rate.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void DeepJungleSweep()
        {
            _builder.Create("dath_deep_sweep", "Deep Jungle Sweep")
                .PrerequisiteQuest("dath_apex_3")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Dr. Imra Sol pays for follow-up data on the predators of Dathomir. Kill 10 Sprantal and 10 Ssurians on Dathomir, then return to her at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_Sprantal, 10)
                .AddKillObjective(NPCGroupType.Dathomir_Ssurian, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Sprantal and 10 Ssurians. Return to Dr. Imra Sol at the Jungle Landing on Dathomir to collect the data rate.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void KwiEncroachmentDrumsInTheDark()
        {
            _builder.Create("dath_kwi_1", "The Kwi Encroachment: Drums in the Dark")

                .AddState()
                .SetStateJournalText("Kwi war parties are probing the Czerka landing. Kill 15 Kwi Tribals on Dathomir, then return to Security Chief Garrik Vane at the Czerka Base on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiTribal, 15)

                .AddState()
                .SetStateJournalText("You killed 15 Kwi Tribals. Return to Security Chief Garrik Vane at the Czerka Base on Dathomir to collect your pay.")

                .AddGoldReward(6000)
                .AddXPReward(5000);
        }

        private void KwiEncroachmentStolenRites()
        {
            _builder.Create("dath_kwi_2", "The Kwi Encroachment: Stolen Rites")
                .PrerequisiteQuest("dath_kwi_1")

                .AddState()
                .SetStateJournalText("Kwi Shamans are driving the war parties with ritual fetishes. Take 5 Kwi Ritual Fetishes from Kwi Shamans on Dathomir, then bring them to Security Chief Garrik Vane at the Czerka Base on Dathomir.")
                .AddCollectItemObjective("dath_kwi_fetish", 5)

                .AddState()
                .SetStateJournalText("You have 5 Kwi Ritual Fetishes. Bring them to Security Chief Garrik Vane at the Czerka Base on Dathomir to collect your pay.")

                .AddGoldReward(6000)
                .AddXPReward(6000);
        }

        private void KwiEncroachmentBreakTheCircle()
        {
            _builder.Create("dath_kwi_3", "The Kwi Encroachment: Break the Circle")
                .PrerequisiteQuest("dath_kwi_2")

                .AddState()
                .SetStateJournalText("A circle of Kwi Shamans and their Guardians is directing the attacks on the Czerka landing. Kill 5 Kwi Shamans and 5 Kwi Guardians on Dathomir, then return to Security Chief Garrik Vane at the Czerka Base on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiShaman, 5)
                .AddKillObjective(NPCGroupType.Dathomir_KwiGuardian, 5)

                .AddState()
                .SetStateJournalText("You killed 5 Kwi Shamans and 5 Kwi Guardians. Return to Security Chief Garrik Vane at the Czerka Base on Dathomir to collect your pay and a reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000)
                .AddItemReward("kwi_spirit_charm", 1)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.TheKwiEncroachment);
                });
        }

        private void JungleWatch()
        {
            _builder.Create("dath_jungle_watch", "Jungle Watch")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Czerka security pays for patrols against the Kwi war parties. Kill 20 Kwi Tribals on Dathomir, then return to Security Chief Garrik Vane at the Czerka Base on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_KwiTribal, 20)

                .AddState()
                .SetStateJournalText("You killed 20 Kwi Tribals. Return to Security Chief Garrik Vane at the Czerka Base on Dathomir to collect the patrol rate.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void TheShellOfTheOldOne()
        {
            _builder.Create("dath_turtle", "The Shell of the Old One")

                .AddState()
                .SetStateJournalText("Marrok Dane wants a Dragon Turtle brought down for his trophy wall. Kill a Dragon Turtle in the Desert, the Cave Ruins or the Grotto Caverns on Dathomir, then return to him at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_DragonTurtle, 1)

                .AddState()
                .SetStateJournalText("You killed the Dragon Turtle for the trophy wall. Return to Marrok Dane at the Jungle Landing on Dathomir to collect your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000);
        }

        private void WingsOverTheGrotto()
        {
            _builder.Create("dath_chiro", "Wings over the Grotto")

                .AddState()
                .SetStateJournalText("Marrok Dane wants the Chirodactyl dead. It ranges over the Grottos, the Mountains, the Mountain Caves, the Cave Ruins, the Ruin Base and the western Desert, and is not always there. Kill the Chirodactyl, then return to him at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_GrottoChirodactyl, 1)

                .AddState()
                .SetStateJournalText("You killed the Chirodactyl for the trophy wall. Return to Marrok Dane at the Jungle Landing on Dathomir to collect your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000);
        }

        private void TheRancorPit()
        {
            _builder.Create("dath_rancor", "The Rancor Pit")

                .AddState()
                .SetStateJournalText("Marrok Dane wants a Rancor dead. Rancors roam the Grottos, the Mountains, the Mountain Caves, the Cave Ruins, the Ruin Base and the western Desert, and are not always there. Kill a Rancor on Dathomir, then return to him at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_Rancor, 1)

                .AddState()
                .SetStateJournalText("You killed the Rancor for the trophy wall. Return to Marrok Dane at the Jungle Landing on Dathomir to collect your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("rancorhide_belt", 1);
        }

        private void ShadowInTheCanopy()
        {
            _builder.Create("dath_adept", "Shadow in the Canopy")

                .AddState()
                .SetStateJournalText("A Dark Side Adept stalks the Grottos, the Mountains, the Mountain Caves, the Cave Ruins, the Ruin Base and the western Desert of Dathomir, and is not always there. Kill the Dark Side Adept, then return to Marrok Dane at the Jungle Landing on Dathomir.")
                .AddKillObjective(NPCGroupType.Dathomir_DarkSideAdept, 1)

                .AddState()
                .SetStateJournalText("You killed the Dark Side Adept. Return to Marrok Dane at the Jungle Landing on Dathomir to collect your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000)
                .AddItemReward("canopy_band", 1);
        }

        private void TheTrophyWall()
        {
            _builder.Create("dath_trophy", "The Trophy Wall")
                .PrerequisiteQuest("dath_turtle")
                .PrerequisiteQuest("dath_chiro")
                .PrerequisiteQuest("dath_rancor")
                .PrerequisiteQuest("dath_adept")

                .AddState()
                .SetStateJournalText("You have finished all four of Marrok Dane's hunts: the Dragon Turtle, the Chirodactyl, the Rancor and the Dark Side Adept. Speak to him at the Jungle Landing on Dathomir about his trophy wall.")

                .AddXPReward(5000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.DathomirTrophyWall);
                });
        }
    }
}
