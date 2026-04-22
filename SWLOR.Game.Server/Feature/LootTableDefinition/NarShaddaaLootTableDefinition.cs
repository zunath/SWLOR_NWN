using SWLOR.Game.Server.Service.LootService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
	public class NarShaddaaLootTableDefinition : ILootTableDefinition
	{
		private readonly LootTableBuilder _builder = new();

		public Dictionary<string, LootTable> BuildLootTables()
		{
			RedBlades();
			BlackSerpents();
			HiddenBlades();
			Troublemakers();
			ArenaFighters();
			Sniper();
			SerpentLeader();
			RogueDroid();
			Pirates();
			CommandDroid();
			Scavengers();
			Thieves();
			SlaverCaptain();
			GreatArkanianDragon();
			DragonLoot();

			return _builder.Build();
		}

		private void RedBlades()
		{
			_builder.Create("NARSHADDAA_RED_BLADES")
				.AddItem("elec_imperfect", 15)
				.AddItem("fiberp_imperfect", 15)
				.AddItem("ns_sludge_eel", 10)
				.AddGold(35, 20);
		}

		private void BlackSerpents()
		{
			_builder.Create("NARSHADDAA_BLACK_SERPENTS")
				.AddItem("elec_imperfect", 15)
				.AddItem("fiberp_imperfect", 15)
				.AddItem("lth_imperfect", 10)
				.AddGold(35, 20);
		}

		private void HiddenBlades()
		{
			_builder.Create("NARSHADDAA_HIDDEN_BLADES")
				.AddItem("elec_good", 12)
				.AddItem("fiberp_good", 12)
				.AddItem("lth_good", 8)
				.AddGold(45, 25);
		}

		private void Troublemakers()
		{
			_builder.Create("NARSHADDAA_TROUBLEMAKERS")
				.AddItem("elec_imperfect", 12)
				.AddItem("fiberp_imperfect", 12)
				.AddItem("herb_t", 8)
				.AddGold(30, 20);
		}

		private void ArenaFighters()
		{
			_builder.Create("NARSHADDAA_ARENA_FIGHTERS")
				.AddItem("fiberp_good", 10)
				.AddItem("lth_good", 10)
				.AddItem("ns_neon_salt", 8)
				.AddGold(50, 20);
		}

		private void Sniper()
		{
			_builder.Create("NARSHADDAA_SNIPER")
				.AddItem("elec_good", 15)
				.AddItem("fiberp_good", 10)
				.AddGold(80, 20);

			_builder.Create("NARSHADDAA_SNIPER_RARES")
				.IsRare()
				.AddItem("ns_moonspice", 1, 1, true);

		}

		private void SerpentLeader()
		{
			_builder.Create("NARSHADDAA_SERPENT_LEADER")
				.AddItem("elec_good", 15)
				.AddItem("fiberp_high", 15)
				.AddItem("lth_good", 10)
				.AddGold(120, 30);

			_builder.Create("NARSHADDAA_SERPENT_LEADER_RARES")
				.IsRare()
				.AddItem("ruby", 1, 1, true)
				.AddItem("ns_holo_jelly", 1, 1, true);
		}

		private void RogueDroid()
		{
			_builder.Create("NARSHADDAA_ROGUE_DROID")
				.AddItem("elec_good", 20)
				.AddItem("scrap_metal", 20)
				.AddGold(60, 20);
		}

		private void Pirates()
		{
			_builder.Create("NARSHADDAA_PIRATES")
				.AddItem("elec_imperfect", 10)
				.AddItem("ns_neon_salt", 10)
				.AddItem("r_flour", 5)
				.AddGold(35, 20);
		}

		private void CommandDroid()
		{
			_builder.Create("NARSHADDAA_COMMAND_DROID")
				.AddItem("elec_high", 10)
				.AddItem("scrap_metal", 25)
				.AddGold(100, 30);

			_builder.Create("NARSHADDAA_COMMAND_DROID_RARES")
				.IsRare()
				.AddItem("ns_holo_jelly", 1, 1, true)
				.AddItem("emerald", 1, 1, true);
		}

		private void Scavengers()
		{
			_builder.Create("NARSHADDAA_SCAVENGERS")
				.AddItem("scrap_metal", 30)
				.AddItem("elec_imperfect", 10)
				.AddGold(20, 10);
		}

		private void Thieves()
		{
			_builder.Create("NARSHADDAA_THIEVES")
				.AddItem("data_chip_encryp", 15)
				.AddItem("sugar", 8)
				.AddItem("lth_imperfect", 10)
				.AddGold(45, 25);
		}

		private void SlaverCaptain()
		{
			_builder.Create("NARSHADDAA_SLAVER_CAPTAIN")
				.AddItem("fiberp_good", 12)
				.AddItem("lth_good", 12)
				.AddGold(120, 35);

			_builder.Create("NARSHADDAA_SLAVER_CAPTAIN_RARES")
				.IsRare()
				.AddItem("ruby", 1, 1, true)
				.AddItem("ns_moonspice", 1, 1, true);
		}

		private void GreatArkanianDragon()
		{
			_builder.Create("NARSHADDAA_GREAT_ARKANIAN_DRAGON")
				.AddItem("fiberp_high", 20)
				.AddItem("lth_high", 20)
				.AddItem("wild_meat", 15)
				.AddItem("ns_rack_meat", 15)
				.AddItem("chiro_shard", 2)
				.AddItem("ark_dragon_troph", 100)
				.AddGold(300, 40);

			_builder.Create("NARSHADDAA_GREAT_ARKANIAN_DRAGON_GEMS")
				.AddItem("emerald", 100, 1, true)
				.AddItem("diamond", 100, 1, true)
				.AddItem("chiro_shard", 50, 1, true);

			_builder.Create("NARSHADDAA_GREAT_ARKANIAN_DRAGON_RARES")
				.IsRare()
				.AddItem("chiro_shard", 1, 1, true)
				.AddItem("emerald", 1, 1, true)
				.AddItem("ruby", 1, 1, true);
		}
		private void DragonLoot()
		{
			_builder.Create("DRAGON_LOOT")
				.AddItem("diamond", 50)
				.AddItem("elec_high", 50);


		}

	}
}

