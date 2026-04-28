using System.Collections.Generic;
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

				.AddGoldReward(500)
				.AddXPReward(1750);
		}

		private static void StinkyWomprats(QuestBuilder builder)
		{
			builder.Create("stinky_womprats", "Stinky Womprats")

				.AddState()
				.SetStateJournalText("You've agreed to take care of those pesky, stinky, womprats. Slay them and return 10 hides to Haderach in Anchorhead.")
				.AddCollectItemObjective("womprathide", 10)

				.AddGoldReward(1000)
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

				.AddGoldReward(7500)
				.AddXPReward(7500)
				.AddItemReward("recipe_hazrdwall", 1);
		}

		private static void SmugglerFavor(QuestBuilder builder)
		{
			builder.Create("smuggler_favor", "A Smuggler's Favor")

				.AddState()
				.SetStateJournalText("A shady smuggler has asked for your help. He lost a shipment to a local ancient sandworm and needs you to recover it. Find and eliminate the sandworm, then retrieve the stolen cargo.")
				.AddKillObjective(NPCGroupType.Tatooine_AncientSandworm, 1)

				.AddState()
				.SetStateJournalText("You've dealt with the sandworm. Search the area and recover the stolen goods.")
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

	}
}