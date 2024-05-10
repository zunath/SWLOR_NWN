using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class MonCalaQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            FishingGuildQuests();
            PartyRoomForPedro();

            return _builder.Build();
        }

        private void FishingGuildQuests()
        {
            _builder.Create("fish_rod_1", "The Clothespole Rod")

                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("moat_carp", 2)
                .AddCollectItemObjective("lamp_marimo", 2)
                .AddCollectItemObjective("visc_urchin", 2)
                .AddCollectItemObjective("cobalt_jellyfish", 2)
                .AddCollectItemObjective("denizanasi", 2)
                .AddCollectItemObjective("cala_lobster", 2)
                .AddCollectItemObjective("bibikibo", 2)
                .AddCollectItemObjective("dath_sardine", 2)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for the Clothespole Rod.")
                
                .AddItemReward("clothespole", 1);

            _builder.Create("fish_rod_2", "The Fastwater Rod")
                .PrerequisiteQuest("fish_rod_1")

                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("hamsi", 2)
                .AddCollectItemObjective("sen_sardine", 2)
                .AddCollectItemObjective("rakaz_shellfish", 2)
                .AddCollectItemObjective("bast_sweeper", 2)
                .AddCollectItemObjective("mackerel", 2)
                .AddCollectItemObjective("greedie", 2)
                .AddCollectItemObjective("copper_frog", 2)
                .AddCollectItemObjective("yellow_globe", 2)
                .AddCollectItemObjective("muddy_siredon", 2)
                .AddCollectItemObjective("istavrit", 2)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for the Fastwater Rod.")

                .AddItemReward("fastwater_rod", 1);

            _builder.Create("fish_rod_3", "The Judge's Rod")
                .PrerequisiteQuest("fish_rod_2")

                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("quus", 2)
                .AddCollectItemObjective("forest_carp", 2)
                .AddCollectItemObjective("tiny_goldfish", 2)
                .AddCollectItemObjective("cheval_salmon", 2)
                .AddCollectItemObjective("yorchete", 2)
                .AddCollectItemObjective("white_lobster", 2)
                .AddCollectItemObjective("fat_greedie", 2)
                .AddCollectItemObjective("moorish_idol", 2)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for the Judge's Rod.")

                .AddItemReward("judge_rod", 1);

            _builder.Create("fish_rod_4", "The Yew Rod")
                .PrerequisiteQuest("fish_rod_3")

                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("nebimonite", 2)
                .AddCollectItemObjective("tricolored_carp", 2)
                .AddCollectItemObjective("blindfish", 2)
                .AddCollectItemObjective("pipira", 2)
                .AddCollectItemObjective("tiger_cod", 2)
                .AddCollectItemObjective("bonefish", 2)
                .AddCollectItemObjective("giant_catfish", 2)
                .AddCollectItemObjective("yayinbaligi", 2)
                .AddCollectItemObjective("deadmoiselle", 2)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for the Yew Rod.")

                .AddItemReward("yew_rod", 1);

            _builder.Create("fish_rod_5", "The Legendary Rod")
                .PrerequisiteQuest("fish_rod_4")

                .AddState()
                .SetStateJournalText("Return to Lu Shang in the Elite Hotel on Mon Cal with the requested fish.")
                .AddCollectItemObjective("moat_carp", 10000)

                .AddState()
                .SetStateJournalText("Return to Lu Shang for Lu Shang's Fishing Rod.")

                .AddItemReward("lushang_rod", 1)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.TheLegendaryRod);
                });
        }

        private void PartyRoomForPedro()
        {
            _builder.Create("partyroom_pedro", "Party Room for P3DR0")

                .AddState()
                .SetStateJournalText("P3DR0 wants a new place to party where they're not going to get kicked out of. Bring them five speakers, a jukebox and the schematics for a new cantina.")
                .AddCollectItemObjective("structure_0272", 5)
                .AddCollectItemObjective("structure_0005", 1)
                .AddCollectItemObjective("structure_5004", 1)

                .AddState()
                .SetStateJournalText("Look's like P3DR0's going to be able to party. Make sure you talk to them!")

                .AddGoldReward(5000)
                .AddXPReward(2500)
                .AddItemReward("recipe_fabdance1", 1);
        }
    }
}