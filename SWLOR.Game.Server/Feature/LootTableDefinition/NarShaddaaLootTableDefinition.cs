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
			FightClubBackroomsRares();

			CapstoneSmugglersMoonDungeonRares();
			CzerkaArmsRareElites();
			FightClubRareElites();
			return _builder.Build();
		}

		private void FightClubRareElites()
		{
			_builder.Create("FIGHTCLUB_IRONJAW_RARES").IsRare()
				.AddItem("brawl_wraps", 1, 1, true).AddItem("scar_cuirass", 1, 1, true).AddItem("bp_pitcestus", 1, 1, true);
			_builder.Create("FIGHTCLUB_QUICKDRAW_RARES").IsRare()
				.AddItem("gunsling_rig", 1, 1, true).AddItem("padded_coat", 1, 1, true).AddItem("bp_duelvest", 1, 1, true);
			_builder.Create("FIGHTCLUB_HEXCALLER_RARES").IsRare()
				.AddItem("hexweave", 1, 1, true).AddItem("ward_vest", 1, 1, true).AddItem("bp_charmcowl", 1, 1, true);
			_builder.Create("FIGHTCLUB_IRONJAW_COMP").AddItem("arena_token", 1, 1);
			_builder.Create("FIGHTCLUB_QUICKDRAW_COMP").AddItem("spent_charge", 1, 1);
			_builder.Create("FIGHTCLUB_HEXCALLER_COMP").AddItem("hex_focus", 1, 1);
		}

		// Named rare elite droids/troopers in the Czerka Arms Test Range: unique gear + a blueprint,
		// plus a guaranteed encounter-specific salvage component the recipe requires.
		private void CzerkaArmsRareElites()
		{
			_builder.Create("CZERKA_OVERWATCH_RARES")
				.IsRare()
				.AddItem("bipod_rig", 1, 1, true)
				.AddItem("barrel_shroud", 1, 1, true)
				.AddItem("bp_precoptic", 1, 1, true);
			_builder.Create("CZERKA_BLASTBREAKER_RARES")
				.IsRare()
				.AddItem("deton_cestus", 1, 1, true)
				.AddItem("blast_vest", 1, 1, true)
				.AddItem("bp_detonknuck", 1, 1, true);
			_builder.Create("CZERKA_SUPPRESSOR_RARES")
				.IsRare()
				.AddItem("jammer_array", 1, 1, true)
				.AddItem("riot_harness", 1, 1, true)
				.AddItem("bp_jammermesh", 1, 1, true);
			_builder.Create("CZERKA_OVERWATCH_COMP")
				.AddItem("targeting_mod", 1, 1);
			_builder.Create("CZERKA_BLASTBREAKER_COMP")
				.AddItem("detonite_chg", 1, 1);
			_builder.Create("CZERKA_SUPPRESSOR_COMP")
				.AddItem("signal_disr", 1, 1);
		}

		private void CapstoneSmugglersMoonDungeonRares()
		{
			_builder.Create("CAPSTONE_KILLBOX_RARES")
				.IsRare()
				.AddItem("killbox_l1", 1, 1, true)
				.AddItem("killbox_l2", 1, 1, true)
				.AddItem("killbox_l3", 1, 1, true)
				.AddItem("killbox_l4", 1, 1, true)
				.AddItem("killbox_l5", 1, 1, true)
				.AddItem("killbox_l6", 1, 1, true)
				.AddItem("killbox_l7", 1, 1, true)
				.AddItem("killbox_l8", 1, 1, true);
			_builder.Create("CAPSTONE_KILLBOX_WD_RARES")
				.IsRare()
				.AddItem("killbox_w1", 1, 1, true)
				.AddItem("killbox_w2", 1, 1, true)
				.AddItem("killbox_w3", 1, 1, true)
				.AddItem("killbox_w4", 1, 1, true)
				.AddItem("killbox_w5", 1, 1, true);

			_builder.Create("CAPSTONE_ONESHOT_RARES")
				.IsRare()
				.AddItem("oneshot_l1", 1, 1, true)
				.AddItem("oneshot_l2", 1, 1, true)
				.AddItem("oneshot_l3", 1, 1, true)
				.AddItem("oneshot_l4", 1, 1, true)
				.AddItem("oneshot_l5", 1, 1, true)
				.AddItem("oneshot_l6", 1, 1, true)
				.AddItem("oneshot_l7", 1, 1, true)
				.AddItem("oneshot_l8", 1, 1, true);
			_builder.Create("CAPSTONE_ONESHOT_WD_RARES")
				.IsRare()
				.AddItem("oneshot_w1", 1, 1, true)
				.AddItem("oneshot_w2", 1, 1, true)
				.AddItem("oneshot_w3", 1, 1, true)
				.AddItem("oneshot_w4", 1, 1, true)
				.AddItem("oneshot_w5", 1, 1, true);

			_builder.Create("CAPSTONE_RAINSTEEL_RARES")
				.IsRare()
				.AddItem("rainsteel_l1", 1, 1, true)
				.AddItem("rainsteel_l2", 1, 1, true)
				.AddItem("rainsteel_l3", 1, 1, true)
				.AddItem("rainsteel_l4", 1, 1, true)
				.AddItem("rainsteel_l5", 1, 1, true)
				.AddItem("rainsteel_l6", 1, 1, true)
				.AddItem("rainsteel_l7", 1, 1, true)
				.AddItem("rainsteel_l8", 1, 1, true);
			_builder.Create("CAPSTONE_RAINSTEEL_WD_RARES")
				.IsRare()
				.AddItem("rainsteel_w1", 1, 1, true)
				.AddItem("rainsteel_w2", 1, 1, true)
				.AddItem("rainsteel_w3", 1, 1, true)
				.AddItem("rainsteel_w4", 1, 1, true)
				.AddItem("rainsteel_w5", 1, 1, true);
		}

		private void FightClubBackroomsRares()
		{
			_builder.Create("NARSHADDAA_FIGHT_CLUB_LANCER_RARES")
				.IsRare()
				.AddItem("pit_shockpike", 1, 1, true)
				.AddItem("gatehook_spear", 1, 1, true)
				.AddItem("pitguard_wraps", 1, 1, true)
				.AddItem("ringside_belt", 1, 1, true)
				.AddItem("oddsman_mantle", 1, 1, true)
				.AddItem("pit_signet", 1, 1, true)
				.AddItem("lowblow_visor", 1, 1, true)
				.AddItem("sawdust_boots", 1, 1, true);

			_builder.Create("NARSHADDAA_FIGHT_CLUB_STORM_DANCER_RARES")
				.IsRare()
				.AddItem("squall_blades", 1, 1, true)
				.AddItem("crosswind_edge", 1, 1, true)
				.AddItem("dancer_wraps", 1, 1, true)
				.AddItem("galewalk_boots", 1, 1, true)
				.AddItem("updraft_mantle", 1, 1, true)
				.AddItem("surge_band", 1, 1, true)
				.AddItem("stormbet_charm", 1, 1, true)
				.AddItem("headwind_visor", 1, 1, true);

			_builder.Create("NARSHADDAA_FIGHT_CLUB_CRIMSON_DUELIST_RARES")
				.IsRare()
				.AddItem("scarlet_blades", 1, 1, true)
				.AddItem("lastcall_edge", 1, 1, true)
				.AddItem("crimson_wraps", 1, 1, true)
				.AddItem("redline_boots", 1, 1, true)
				.AddItem("victor_mantle", 1, 1, true)
				.AddItem("bloodpact_ring", 1, 1, true)
				.AddItem("lastbet_charm", 1, 1, true)
				.AddItem("redrule_belt", 1, 1, true);

			_builder.Create("NARSHADDAA_FIGHT_CLUB_LANCER_WARDEN_RARES")
				.IsRare()
				.AddItem("pitwarden_pike", 1, 1, true)
				.AddItem("gatekeep_plate", 1, 1, true)
				.AddItem("lockdown_belt", 1, 1, true)
				.AddItem("holdfast_bracer", 1, 1, true)
				.AddItem("pitkeeper_mask", 1, 1, true);

			_builder.Create("NARSHADDAA_FIGHT_CLUB_STORM_DANCER_WARDEN_RARES")
				.IsRare()
				.AddItem("stormcall_blades", 1, 1, true)
				.AddItem("eyewall_harness", 1, 1, true)
				.AddItem("stillair_gloves", 1, 1, true)
				.AddItem("pressure_belt", 1, 1, true)
				.AddItem("eyestorm_bracer", 1, 1, true);

			_builder.Create("NARSHADDAA_FIGHT_CLUB_CRIMSON_DUELIST_WARDEN_RARES")
				.IsRare()
				.AddItem("bloodprice_edge", 1, 1, true)
				.AddItem("redcrown_plate", 1, 1, true)
				.AddItem("bleddry_gloves", 1, 1, true)
				.AddItem("tithe_belt", 1, 1, true)
				.AddItem("housecut_mask", 1, 1, true);
		}

		private void RedBlades()
		{
			_builder.Create("NARSHADDAA_RED_BLADES")
				.AddItem("elec_imperfect", 15)
				.AddItem("fiberp_imperfect", 15)
				.AddItem("ns_sludge_eel", 50)
				.AddGold(35, 20);
		}

		private void BlackSerpents()
		{
			_builder.Create("NARSHADDAA_BLACK_SERPENTS")
				.AddItem("elec_imperfect", 15)
				.AddItem("fiberp_imperfect", 15)
				.AddItem("lth_imperfect", 10)
				.AddItem("data_chip_encryp", 20)
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
				.AddItem("ns_moonspice", 50)
				.AddGold(30, 20);
		}

		private void ArenaFighters()
		{
			_builder.Create("NARSHADDAA_ARENA_FIGHTERS")
				.AddItem("fiberp_good", 10)
				.AddItem("lth_good", 10)
				.AddItem("ns_neon_salt", 50)
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
				.AddItem("ns_moonspice", 1, 1, true)
				.AddItem("map_82", 2, 1, true)
				.AddItem("map_83", 2, 1, true)
				.AddItem("map_84", 2, 1, true)
				.AddItem("map_85", 2, 1, true)
				.AddItem("map_86", 2, 1, true)
				.AddItem("map_87", 2, 1, true)
				.AddItem("map_88", 2, 1, true)
				.AddItem("map_89", 2, 1, true)
				.AddItem("map_90", 2, 1, true)
				.AddItem("map_91", 2, 1, true)
				.AddItem("map_92", 2, 1, true)
				.AddItem("map_93", 2, 1, true)
				.AddItem("map_94", 2, 1, true)
				.AddItem("map_95", 2, 1, true)
				.AddItem("map_96", 2, 1, true)
				.AddItem("map_97", 2, 1, true)
				.AddItem("map_98", 2, 1, true)
				.AddItem("map_99", 2, 1, true)
				.AddItem("map_100", 2, 1, true)
				.AddItem("map_101", 2, 1, true);


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
				.AddItem("fnote_2017", 1, 1, true)
				.AddItem("fnote_2032", 1, 1, true)
				.AddItem("fnote_2039", 1, 1, true)
				.AddItem("ruby", 1, 1, true)
				.AddItem("ns_holo_jelly", 1, 1, true)
				.AddItem("map_82", 2, 1, true)
				.AddItem("map_83", 2, 1, true)
				.AddItem("map_84", 2, 1, true)
				.AddItem("map_85", 2, 1, true)
				.AddItem("map_86", 2, 1, true)
				.AddItem("map_87", 2, 1, true)
				.AddItem("map_88", 2, 1, true)
				.AddItem("map_89", 2, 1, true)
				.AddItem("map_90", 2, 1, true)
				.AddItem("map_91", 2, 1, true)
				.AddItem("map_92", 2, 1, true)
				.AddItem("map_93", 2, 1, true)
				.AddItem("map_94", 2, 1, true)
				.AddItem("map_95", 2, 1, true)
				.AddItem("map_96", 2, 1, true)
				.AddItem("map_97", 2, 1, true)
				.AddItem("map_98", 2, 1, true)
				.AddItem("map_99", 2, 1, true)
				.AddItem("map_100", 2, 1, true)
				.AddItem("map_101", 2, 1, true);

		}

		private void RogueDroid()
		{
			_builder.Create("NARSHADDAA_ROGUE_DROID")
				.AddItem("elec_good", 20)
				.AddItem("ns_holo_jelly", 20)
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
				.AddItem("emerald", 1, 1, true)
				.AddItem("map_82", 2, 1, true)
				.AddItem("map_83", 2, 1, true)
				.AddItem("map_84", 2, 1, true)
				.AddItem("map_85", 2, 1, true)
				.AddItem("map_86", 2, 1, true)
				.AddItem("map_87", 2, 1, true)
				.AddItem("map_88", 2, 1, true)
				.AddItem("map_89", 2, 1, true)
				.AddItem("map_90", 2, 1, true)
				.AddItem("map_91", 2, 1, true)
				.AddItem("map_92", 2, 1, true)
				.AddItem("map_93", 2, 1, true)
				.AddItem("map_94", 2, 1, true)
				.AddItem("map_95", 2, 1, true)
				.AddItem("map_96", 2, 1, true)
				.AddItem("map_97", 2, 1, true)
				.AddItem("map_98", 2, 1, true)
				.AddItem("map_99", 2, 1, true)
				.AddItem("map_100", 2, 1, true)
				.AddItem("map_101", 2, 1, true);

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
				.AddItem("ns_moonspice", 1, 1, true)
				.AddItem("map_82", 2, 1, true)
				.AddItem("map_83", 2, 1, true)
				.AddItem("map_84", 2, 1, true)
				.AddItem("map_85", 2, 1, true)
				.AddItem("map_86", 2, 1, true)
				.AddItem("map_87", 2, 1, true)
				.AddItem("map_88", 2, 1, true)
				.AddItem("map_89", 2, 1, true)
				.AddItem("map_90", 2, 1, true)
				.AddItem("map_91", 2, 1, true)
				.AddItem("map_92", 2, 1, true)
				.AddItem("map_93", 2, 1, true)
				.AddItem("map_94", 2, 1, true)
				.AddItem("map_95", 2, 1, true)
				.AddItem("map_96", 2, 1, true)
				.AddItem("map_97", 2, 1, true)
				.AddItem("map_98", 2, 1, true)
				.AddItem("map_99", 2, 1, true)
				.AddItem("map_100", 2, 1, true)
				.AddItem("map_101", 2, 1, true)
				.AddItem("lockbox_t4", 2, 1, true);

		}

		private void GreatArkanianDragon()
		{
			_builder.Create("NARSHADDAA_GREAT_ARKANIAN_DRAGON")
				.AddItem("ark_drg_scales", 20)
				.AddItem("hyphae_wood", 20)
				.AddItem("wild_meat", 15)
				.AddItem("ns_rack_meat", 15)
				.AddItem("chiro_shard", 2)
				.AddGold(300, 40);

			_builder.Create("NARSHADDAA_GREAT_ARKANIAN_DRAGON_TROPHY")
				.AddItem("ark_dragon_troph", 100);

			_builder.Create("NARSHADDAA_GREAT_ARKANIAN_DRAGON_GEMS")
				.AddItem("emerald", 100, 1, true)
				.AddItem("diamond", 100, 1, true)
				.AddItem("chiro_shard", 50, 1, true);

			_builder.Create("NARSHADDAA_GREAT_ARKANIAN_DRAGON_RARES")
				.IsRare()
				.AddItem("fnote_2044", 2, 1, true)
				.AddItem("chiro_shard", 1, 1, true)
				.AddItem("emerald", 1, 1, true)
				.AddItem("ruby", 1, 1, true)
				.AddItem("map_82", 2, 1, true)
				.AddItem("map_83", 2, 1, true)
				.AddItem("map_84", 2, 1, true)
				.AddItem("map_85", 2, 1, true)
				.AddItem("map_86", 2, 1, true)
				.AddItem("map_87", 2, 1, true)
				.AddItem("map_88", 2, 1, true)
				.AddItem("map_89", 2, 1, true)
				.AddItem("map_90", 2, 1, true)
				.AddItem("map_91", 2, 1, true)
				.AddItem("map_92", 2, 1, true)
				.AddItem("map_93", 2, 1, true)
				.AddItem("map_94", 2, 1, true)
				.AddItem("map_95", 2, 1, true)
				.AddItem("map_96", 2, 1, true)
				.AddItem("map_97", 2, 1, true)
				.AddItem("map_98", 2, 1, true)
				.AddItem("map_99", 2, 1, true)
				.AddItem("map_100", 2, 1, true)
				.AddItem("map_101", 2, 1, true);

		}
		private void DragonLoot()
		{
			_builder.Create("DRAGON_LOOT")
				.AddItem("diamond", 50)
				.AddItem("elec_high", 50);
		}

	}
}
