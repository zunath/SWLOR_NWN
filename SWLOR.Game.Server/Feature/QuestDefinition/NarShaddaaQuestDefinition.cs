 using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class NarShaddaaQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            DataSmuggler();
            RooftopSniper();
            SpaceDungeon_SlaverRaid();
            SpaceDungeon_PirateHideout();
			GreatArkanianDragonHunt();
			return _builder.Build();
        }

        private void DataSmuggler()
        {
            _builder.Create("nar_data_smuggler", "Data Smuggler")
                .AddState()
                .SetStateJournalText("A slicer in the Core District needs encrypted data chips stolen from a rival gang. Retrieve 5 data chips from the Black Serpents.")
                .AddCollectItemObjective("data_chip_encryp", 5)

                .AddState()
                .SetStateJournalText("Return to the slicer in the Core District with the data chips.")
                .AddGoldReward(4500)
                .AddXPReward(5000)
                .AddItemReward("recipe_boganoga", 1);
		}

        private void RooftopSniper()
        {
            _builder.Create("nar_rooftop_sniper", "Neutralize the Rooftop Sniper")
                .AddState()
                .SetStateJournalText("A sniper has been terrorizing travelers near the upper levels. Find and eliminate him.")
                .AddKillObjective(NPCGroupType.NarShaddaa_Sniper, 1)

                .AddState()
                .SetStateJournalText("Report to the district marshal once the sniper is neutralized.")
                .AddGoldReward(8000)
                .AddXPReward(9500)
                .AddItemReward("recipe_cartelck", 1);

		}

        private void SpaceDungeon_SlaverRaid()
        {
            _builder.Create("nar_space_slaver_raid", "Raid the Slaver Convoy")
                .AddState()
                .SetStateJournalText("Board the Abandon Space station and free captured citizens. Eliminate the slaver captain.")
                .AddKillObjective(NPCGroupType.NarShaddaa_SlaverCaptain, 1)

                .AddState()
                .SetStateJournalText("Return to Nar Shaddaa’s port authority with proof of the raid.")
                .AddGoldReward(12000)
                .AddXPReward(15000)
                .AddItemReward("recipe_nalhfizz", 1);
        }

        private void SpaceDungeon_PirateHideout()
        {
            _builder.Create("nar_space_pirate_hideout", "Clear the Pirate Hideout")
                .IsRepeatable()
                .AddState()
                .SetStateJournalText("Hutt sensors detected a pirate outpost in the asteroid belt. Clear out ten pirates and destroy their command droid.")
                .AddKillObjective(NPCGroupType.NarShaddaa_Pirates, 10)
                .AddKillObjective(NPCGroupType.NarShaddaa_CommandDroid, 1)

                .AddState()
                .SetStateJournalText("Return to the Hutt Agent for your reward.")
                .AddGoldReward(15000)
                .AddXPReward(18000)
				.AddItemReward("recipe_sandogdz", 1);
		}

        private void GreatArkanianDragonHunt()
        {
            _builder.Create("nar_great_arkanian_dragon", "Hunt the Great Arkanian Dragon")
                .AddState()
                .SetStateJournalText("A bounty hunter has reported a rare Great Arkanian Dragon lurking in an abandoned space station. Hunt down the beast and kill it.")
                .AddKillObjective(NPCGroupType.AbandonedStation_GreatArkanianDragon, 1)

                .AddState()
                .SetStateJournalText("Bring proof of the dragon's death back to the bounty hunter.")
                .AddCollectItemObjective("ark_dragon_troph", 1)

                .AddState()
                .SetStateJournalText("Return to the bounty hunter and collect payment for slaying the beast.")
                .AddGoldReward(12000)
                .AddXPReward(15000)
				.AddItemReward("recipe_gardula", 1);
        }
    }
}

