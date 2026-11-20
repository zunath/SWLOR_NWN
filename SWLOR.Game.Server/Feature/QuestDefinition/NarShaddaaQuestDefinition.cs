using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using System.Collections.Generic;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class NarShaddaaQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            DataSmuggler();
            SpaceDungeon_SlaverRaid();
            SpaceDungeon_PirateHideout();
			GreatArkanianDragonHunt();
            WardrobeExpansion1();
            WardrobeExpansion2();
            WardrobeExpansion3();
            WardrobeExpansion4();
            WardrobeExpansion5();
            BadForBusiness();
            LightFingers();
            RedOnTheCatwalks();
            OffTheBooks();
            UnderTheStreets();
            StreetSweep();
            TheSerpentsCoils();
            BladesInTheDark();
            TheSerpentsHead();
            LineOfSight();
            StationSweep();
            AssetRecovery();
            ScrapRights();
            ChainOfCommand();
            ScalesForTheLab();
            MoonSpice();
            SaltTheRim();
            EelOnIce();
            HoloJelly();
            SweetTooth();

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

        private void SpaceDungeon_SlaverRaid()
        {
            _builder.Create("nar_space_slaver_raid", "Raid the Slaver Convoy")
                .AddState()
                .SetStateJournalText("Board the Abandon Space station and free captured citizens. Eliminate the slaver captain.")
                .AddKillObjective(NPCGroupType.NarShaddaa_SlaverCaptain, 1)

                .AddState()
                .SetStateJournalText("Return to Nar Shaddaa's port authority with proof of the raid.")
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
                .OnAdvanceAction((player, sourceObject, state) =>
                {
                    if (state == 2)
                        CreateItemOnObject("ark_dragon_troph", player, 1);
                })
                .AddState()
                .SetStateJournalText("A bounty hunter has reported a rare Great Arkanian Dragon lurking in an abandoned space station. Hunt down the beast and kill it.")
                .AddKillObjective(NPCGroupType.AbandonedStation_GreatArkanianDragon, 1)

                .AddState()
                .SetStateJournalText("You recovered an Arkanian Dragon Trophy from the slain beast. Bring it back to the GSI representative on Nar Shaddaa.")
                .AddCollectItemObjective("ark_dragon_troph", 1)

                .AddState()
                .SetStateJournalText("Return to the bounty hunter and collect payment for slaying the beast.")
                .AddGoldReward(12000)
                .AddXPReward(15000)
				.AddItemReward("recipe_gardula", 1);
        }

        private void WardrobeExpansion1()
        {
            _builder.Create("wardrobe_expansion_1", "Pattern Locker: First Fittings")
                .OnCompleteAction((player, _) => Wardrobe.ExpandSlotLimit(player))
                .AddState()
                .SetStateJournalText("Vessa Thrane on the Nar Shaddaa Promenade can expand your wardrobe. Bring her Old Scar's Silver Ruff (from Old Scar, a kath hound on Viscara), Stormplume Sheen Down (from Stormplume, a warocas on Viscara) and a crafted Pattern Thread Spool.")
                .AddCollectItemObjective("wd_scar_ruff", 1)
                .AddCollectItemObjective("wd_storm_down", 1)
                .AddCollectItemObjective("wd_thread_spool", 1)

                .AddState()
                .SetStateJournalText("Return to Vessa Thrane on the Nar Shaddaa Promenade so she can expand your wardrobe.");
        }

        private void WardrobeExpansion2()
        {
            _builder.Create("wardrobe_expansion_2", "Pattern Locker: Silk and Glass")
                .PrerequisiteQuest("wardrobe_expansion_1")
                .OnCompleteAction((player, _) => Wardrobe.ExpandSlotLimit(player))
                .AddState()
                .SetStateJournalText("Bring Vessa Thrane on the Nar Shaddaa Promenade a Silkshade Dye Gland (from the Silkshade Matriarch on Viscara), a Shard-Eye Prism Lens (from the Shard-Eye Matriarch on Viscara) and a crafted Pattern Loom Heddle.")
                .AddCollectItemObjective("wd_silk_gland", 1)
                .AddCollectItemObjective("wd_prism_lens", 1)
                .AddCollectItemObjective("wd_loom_heddle", 1)

                .AddState()
                .SetStateJournalText("Return to Vessa Thrane on the Nar Shaddaa Promenade so she can expand your wardrobe.");
        }

        private void WardrobeExpansion3()
        {
            _builder.Create("wardrobe_expansion_3", "Pattern Locker: Clan Colors")
                .PrerequisiteQuest("wardrobe_expansion_2")
                .OnCompleteAction((player, _) => Wardrobe.ExpandSlotLimit(player))
                .AddState()
                .SetStateJournalText("Bring Vessa Thrane on the Nar Shaddaa Promenade Iron-Stripe Clan Pigment (from Harrek Voss, a Mandalorian on Viscara), a Redtail Sheen Scale (from Redtail Kor, a raivor on Viscara) and crafted Pattern Template Plates.")
                .AddCollectItemObjective("wd_clan_pigment", 1)
                .AddCollectItemObjective("wd_redtail_scale", 1)
                .AddCollectItemObjective("wd_tmpl_plates", 1)

                .AddState()
                .SetStateJournalText("Return to Vessa Thrane on the Nar Shaddaa Promenade so she can expand your wardrobe.");
        }

        private void WardrobeExpansion4()
        {
            _builder.Create("wardrobe_expansion_4", "Pattern Locker: Bone and Pearl")
                .PrerequisiteQuest("wardrobe_expansion_3")
                .OnCompleteAction((player, _) => Wardrobe.ExpandSlotLimit(player))
                .AddState()
                .SetStateJournalText("Bring Vessa Thrane on the Nar Shaddaa Promenade a Maw-Seer Bone Needle (from Maw-Seer Ghal, a Vellen flesheater on Viscara), a Krayt Pearl Clasp (from Urrok, the Tusken Gaderffii champion on Tatooine) and crafted Loom Tension Weights.")
                .AddCollectItemObjective("wd_bone_needle", 1)
                .AddCollectItemObjective("wd_krayt_pearl", 1)
                .AddCollectItemObjective("wd_tension_wts", 1)

                .AddState()
                .SetStateJournalText("Return to Vessa Thrane on the Nar Shaddaa Promenade so she can expand your wardrobe.");
        }

        private void WardrobeExpansion5()
        {
            _builder.Create("wardrobe_expansion_5", "Pattern Locker: The Master's Archive")
                .PrerequisiteQuest("wardrobe_expansion_4")
                .OnCompleteAction((player, _) =>
                {
                    Wardrobe.ExpandSlotLimit(player);
                    Achievement.GiveAchievement(player, AchievementType.MasterWardrobe);
                })
                .AddState()
                .SetStateJournalText("Bring Vessa Thrane on the Nar Shaddaa Promenade Apex Maw Horn Buttons (from the Tarn Apex Maw on Dathomir), Grotto Glow Spores (from the Grotto Alpha on Dathomir) and crafted Pattern Index Tags.")
                .AddCollectItemObjective("wd_apex_horn", 1)
                .AddCollectItemObjective("wd_glow_spores", 1)
                .AddCollectItemObjective("wd_index_tags", 1)

                .AddState()
                .SetStateJournalText("Return to Vessa Thrane on the Nar Shaddaa Promenade so she can expand your wardrobe.");
        }
    

        private void BadForBusiness()
        {
            _builder.Create("ns_troublemakers", "Bad for Business")

                .AddState()
                .SetStateJournalText("Troublemakers are shaking down the shops in the Corporate District. Kill 12 Troublemakers in the Corporate District or the Slums on Smuggler's Moon, then return to Vex Tarro in the Corporate District on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_Troublemakers, 12)

                .AddState()
                .SetStateJournalText("You have killed 12 Troublemakers. Return to Vex Tarro in the Corporate District on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void LightFingers()
        {
            _builder.Create("ns_thieves", "Light Fingers")

                .AddState()
                .SetStateJournalText("Thieves are stripping cargo on the Catwalks and in the Shipping District. Kill 12 Thieves on the Catwalks or in the Shipping District on Smuggler's Moon, then return to Vex Tarro in the Corporate District on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_Thieves, 12)

                .AddState()
                .SetStateJournalText("You have killed 12 Thieves. Return to Vex Tarro in the Corporate District on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void RedOnTheCatwalks()
        {
            _builder.Create("ns_red_blades", "Red on the Catwalks")

                .AddState()
                .SetStateJournalText("The Red Blades gang has claimed the Catwalks and charges a toll to cross them. Kill 10 Red Blades on the Catwalks on Smuggler's Moon, then return to Vex Tarro in the Corporate District on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_RedBlades, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Red Blades. Return to Vex Tarro in the Corporate District on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void OffTheBooks()
        {
            _builder.Create("ns_arena", "Off the Books")

                .AddState()
                .SetStateJournalText("Arena Fighters are hiring themselves out as muscle for the gangs in the Shipping District. Kill 10 Arena Fighters in the Shipping District on Smuggler's Moon, then return to Vex Tarro in the Corporate District on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_ArenaFighters, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Arena Fighters. Return to Vex Tarro in the Corporate District on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void UnderTheStreets()
        {
            _builder.Create("ns_pirates", "Under the Streets")

                .AddState()
                .SetStateJournalText("Pirates are moving stolen cargo through the Sewers to ships that nobody inspects. Kill 10 Pirates in the Sewers on Smuggler's Moon or on the abandoned Smuggler's Moon Station, then return to Vex Tarro in the Corporate District on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_Pirates, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Pirates. Return to Vex Tarro in the Corporate District on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void StreetSweep()
        {
            _builder.Create("ns_street_sweep", "Street Sweep")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Vex Tarro pays a standing rate to anyone who keeps the streets of Smuggler's Moon quiet. Kill 10 Troublemakers and 10 Thieves on the streets of Smuggler's Moon, then return to Vex Tarro in the Corporate District on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_Troublemakers, 10)
                .AddKillObjective(NPCGroupType.NarShaddaa_Thieves, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Troublemakers and 10 Thieves. Return to Vex Tarro in the Corporate District on Smuggler's Moon for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void TheSerpentsCoils()
        {
            _builder.Create("ns_serpents", "The Serpent's Coils")

                .AddState()
                .SetStateJournalText("The Black Serpents gang has taken over the abandoned station in orbit above Smuggler's Moon. Kill 10 Black Serpents on the abandoned Smuggler's Moon Station, then return to Dace Korr in the Tilted Visor on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_BlackSerpents, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Black Serpents. Return to Dace Korr in the Tilted Visor on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void BladesInTheDark()
        {
            _builder.Create("ns_hidden_blades", "Blades in the Dark")
                .PrerequisiteQuest("ns_serpents")

                .AddState()
                .SetStateJournalText("The Hidden Blades are assassins who kill for the Black Serpents gang. Kill 10 Hidden Blades on the abandoned Smuggler's Moon Station, then return to Dace Korr in the Tilted Visor on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_HiddenBlades, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Hidden Blades. Return to Dace Korr in the Tilted Visor on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void TheSerpentsHead()
        {
            _builder.Create("ns_serpent_leader", "The Serpent's Head")
                .PrerequisiteQuest("ns_hidden_blades")

                .AddState()
                .SetStateJournalText("The Serpent Leader commands the Black Serpents from the abandoned station above Smuggler's Moon. Kill the Serpent Leader on the abandoned Smuggler's Moon Station, then return to Dace Korr in the Tilted Visor on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_SerpentLeader, 1)

                .AddState()
                .SetStateJournalText("You have killed the Serpent Leader. Return to Dace Korr in the Tilted Visor on Smuggler's Moon for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.TheSerpentsHead);
                });
        }

        private void LineOfSight()
        {
            _builder.Create("ns_sniper", "Line of Sight")

                .AddState()
                .SetStateJournalText("A Rooftop Sniper is killing people for hire from the Catwalks. Kill the Rooftop Sniper on the Catwalks on Smuggler's Moon, then return to Dace Korr in the Tilted Visor on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_Sniper, 1)

                .AddState()
                .SetStateJournalText("You have killed the Rooftop Sniper. Return to Dace Korr in the Tilted Visor on Smuggler's Moon for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void StationSweep()
        {
            _builder.Create("ns_gang_sweep", "Station Sweep")
                .PrerequisiteQuest("ns_serpent_leader")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The Hutts keep a standing bounty on the gangs of the abandoned station above Smuggler's Moon. Kill 10 Black Serpents and 10 Hidden Blades on the abandoned Smuggler's Moon Station, then return to Dace Korr in the Tilted Visor on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_BlackSerpents, 10)
                .AddKillObjective(NPCGroupType.NarShaddaa_HiddenBlades, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Black Serpents and 10 Hidden Blades. Return to Dace Korr in the Tilted Visor on Smuggler's Moon for your reward.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void AssetRecovery()
        {
            _builder.Create("ns_rogue_droids", "Asset Recovery")

                .AddState()
                .SetStateJournalText("Czerka security droids left on the abandoned station have gone rogue. Kill 8 Rogue Droids on the abandoned Smuggler's Moon Station or its Lower Level, then return to Supervisor Lena Voss in the Czerka Shipyard Office on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_RogueDroids, 8)

                .AddState()
                .SetStateJournalText("You have killed 8 Rogue Droids. Return to Supervisor Lena Voss in the Czerka Shipyard Office on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void ScrapRights()
        {
            _builder.Create("ns_scavengers", "Scrap Rights")

                .AddState()
                .SetStateJournalText("Scavenger Droids are stripping Czerka property in the Slums. Kill 10 Scavenger Droids in the Slums on Smuggler's Moon or on the abandoned Smuggler's Moon Station, then return to Supervisor Lena Voss in the Czerka Shipyard Office on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_ScavengerDroids, 10)

                .AddState()
                .SetStateJournalText("You have killed 10 Scavenger Droids. Return to Supervisor Lena Voss in the Czerka Shipyard Office on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void ChainOfCommand()
        {
            _builder.Create("ns_command_droid", "Chain of Command")
                .PrerequisiteQuest("ns_rogue_droids")

                .AddState()
                .SetStateJournalText("A Command Droid on the abandoned station is directing the Rogue Droids. Kill the Command Droid on the abandoned Smuggler's Moon Station, then return to Supervisor Lena Voss in the Czerka Shipyard Office on Smuggler's Moon.")
                .AddKillObjective(NPCGroupType.NarShaddaa_CommandDroid, 1)

                .AddState()
                .SetStateJournalText("You have killed the Command Droid. Return to Supervisor Lena Voss in the Czerka Shipyard Office on Smuggler's Moon for your reward.")

                .AddGoldReward(7500)
                .AddXPReward(8000);
        }

        private void ScalesForTheLab()
        {
            _builder.Create("ns_dragon_scales", "Scales for the Lab")

                .AddState()
                .SetStateJournalText("Czerka's materials lab wants Arkanian Dragon Scales to study. Collect 3 Arkanian Dragon Scales from the Great Arkanian Dragon on the Lower Level of the abandoned Smuggler's Moon Station, then bring them to Supervisor Lena Voss in the Czerka Shipyard Office on Smuggler's Moon.")
                .AddCollectItemObjective("ark_drg_scales", 3)

                .AddState()
                .SetStateJournalText("You handed over 3 Arkanian Dragon Scales. Speak to Supervisor Lena Voss in the Czerka Shipyard Office on Smuggler's Moon for your reward.")

                .AddGoldReward(11250)
                .AddXPReward(12000);
        }

        private void MoonSpice()
        {
            _builder.Create("ns_moon_spice", "Moon Spice")

                .AddState()
                .SetStateJournalText("Jorro flavors the house special at the Hyper Dive Cantina with Moon Spice. Collect 5 Moon Spice from the Troublemakers of the Corporate District or the Slums, then bring them to Jorro in the Hyper Dive Cantina on Smuggler's Moon.")
                .AddCollectItemObjective("ns_moonspice", 5)

                .AddState()
                .SetStateJournalText("You handed over 5 Moon Spice. Speak to Jorro in the Hyper Dive Cantina on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void SaltTheRim()
        {
            _builder.Create("ns_neon_salt", "Salt the Rim")

                .AddState()
                .SetStateJournalText("Jorro rims his glasses with glowing Neon Salt Crystals and is nearly out. Collect 6 Neon Salt Crystals from the Arena Fighters of the Shipping District or the Pirates of the Sewers, then bring them to Jorro in the Hyper Dive Cantina on Smuggler's Moon.")
                .AddCollectItemObjective("ns_neon_salt", 6)

                .AddState()
                .SetStateJournalText("You handed over 6 Neon Salt Crystals. Speak to Jorro in the Hyper Dive Cantina on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void EelOnIce()
        {
            _builder.Create("ns_sludge_eel", "Eel on Ice")

                .AddState()
                .SetStateJournalText("Jorro serves sliced Sludge Eel on ice as a bar snack at the Hyper Dive Cantina. Collect 5 Sludge Eels from the Red Blades of the Catwalks, then bring them to Jorro in the Hyper Dive Cantina on Smuggler's Moon.")
                .AddCollectItemObjective("ns_sludge_eel", 5)

                .AddState()
                .SetStateJournalText("You handed over 5 Sludge Eels. Speak to Jorro in the Hyper Dive Cantina on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void HoloJelly()
        {
            _builder.Create("ns_holo_jelly", "Holo Jelly")

                .AddState()
                .SetStateJournalText("Jorro makes a Holo Jelly dessert that the Hutts order at the Hyper Dive Cantina. Collect 4 Holo Jelly from the Rogue Droids of the abandoned Smuggler's Moon Station, then bring them to Jorro in the Hyper Dive Cantina on Smuggler's Moon.")
                .AddCollectItemObjective("ns_holo_jelly", 4)

                .AddState()
                .SetStateJournalText("You handed over 4 Holo Jelly. Speak to Jorro in the Hyper Dive Cantina on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }

        private void SweetTooth()
        {
            _builder.Create("ns_sugar", "Sweet Tooth")

                .AddState()
                .SetStateJournalText("The Hyper Dive Cantina has run out of Sugar because every shipment is stolen. Collect 8 Sugar from the Thieves of the Catwalks or the Shipping District, then bring them to Jorro in the Hyper Dive Cantina on Smuggler's Moon.")
                .AddCollectItemObjective("sugar", 8)

                .AddState()
                .SetStateJournalText("You handed over 8 Sugar. Speak to Jorro in the Hyper Dive Cantina on Smuggler's Moon for your reward.")

                .AddGoldReward(4500)
                .AddXPReward(5000);
        }
    }
}
