using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
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
            ClearTheSmugglersLane();

            TeethInTheShallows();
            SmallFry();
            FireInTheMire();
            LegendsOfTheDeepReefmaw();
            LegendsOfTheDeepGlassjaw();
            LegendsOfTheDeepInkveil();
            SwampSweep();
            AntivenomRun();
            GreyMatter();
            ToxinTrade();
            PoisonedTideCutTheLines();
            PoisonedTideStolenSamples();
            PoisonedTideTheReefsAnswer();
            WantedSableQuarr();
            WantedKaelDrox();
            ReefPatrol();
            ATasteOfHome();
            InkForTheArchives();
            FeelersOut();
            OreForTheShipwrights();

            return _builder.Build();
        }

        private void ClearTheSmugglersLane()
        {
            _builder.Create("mon_starlane", "Clear the Smuggler's Lane")
                .AddState()
                .SetStateJournalText("Nera Venn, the route broker beside the dockhand in Mon Cala's Elite Hotel, needs the Krakenborne pirates driven off her shipping route. Destroy 10 Krakenborne ships in Mon Cala orbit, then return to her for a permanent Smuggler's Moon Starlane Pass.")
                .AddKillObjective(NPCGroupType.MonCala_Krakenborne, 10, 55f)

                .AddState()
                .SetStateJournalText("You have destroyed 10 Krakenborne ships. Return to Nera Venn beside the dockhand in Mon Cala's Elite Hotel to collect your Smuggler's Moon Starlane Pass. It opens the new route at the south edge of Mon Cala orbit and the return route at the east edge of Smuggler's Moon orbit.")
                .AddKeyItemReward(KeyItemType.SmugglersMoonStarlanePass, false);
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
                .SetStateJournalText("P3DR0 wants a new place to party where they're not going to get kicked out. Bring them five speakers, a jukebox, and the schematics for a new cantina.")
                .AddCollectItemObjective("structure_0330", 5)
                .AddCollectItemObjective("structure_0005", 1)
                .AddCollectItemObjective("structure_5004", 1)

                .AddState()
                .SetStateJournalText("Looks like P3DR0's going to be able to party. Make sure you talk to them!")

                .AddGoldReward(7500)
                .AddXPReward(2500)
                .AddItemReward("recipe_fabdance1", 1);
        }
    

        private void TeethInTheShallows()
        {
            _builder.Create("mon_reef_cull", "Teeth in the Shallows")

                .AddState()
                .SetStateJournalText("Aradile and Amphi-Hydrus are attacking divers in the shallows around Dac City. Kill 10 Aradile and 8 Amphi-Hydrus on the Coral Isles of Mon Cala, then return to Warden Issa Tarn in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_Aradile, 10)
                .AddKillObjective(NPCGroupType.MonCala_AmphiHydrus, 8)

                .AddState()
                .SetStateJournalText("You killed 10 Aradile and 8 Amphi-Hydrus. Return to Warden Issa Tarn in the Elite Hotel in Dac City to collect your pay.")

                .AddGoldReward(1500)
                .AddXPReward(4000);
        }

        private void SmallFry()
        {
            _builder.Create("mon_small_fry", "Small Fry")

                .AddState()
                .SetStateJournalText("Microtench are breeding out of control in the caves under the Sharptooth Jungle on Mon Cala. Kill 12 Microtench in the caves or the Sunkenhedge Swamps, then return to Warden Issa Tarn in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_Microtench, 12)

                .AddState()
                .SetStateJournalText("You killed 12 Microtench. Return to Warden Issa Tarn in the Elite Hotel in Dac City on Mon Cala to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(5000);
        }

        private void FireInTheMire()
        {
            _builder.Create("mon_fire_mire", "Fire in the Mire")

                .AddState()
                .SetStateJournalText("Scorchellus have overrun the dry ground in the Sunkenhedge Swamps on Mon Cala. Kill 12 Scorchellus and collect 6 Scorchellus Tails there, then bring the tails to Warden Issa Tarn in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_Scorchellus, 12)
                .AddCollectItemObjective("scorch_tail", 6)

                .AddState()
                .SetStateJournalText("You killed 12 Scorchellus and delivered 6 Scorchellus Tails. Speak to Warden Issa Tarn in the Elite Hotel in Dac City to collect your reward.")

                .AddGoldReward(2700)
                .AddXPReward(5500)
                .AddItemReward("reefwarden_band", 1);
        }

        private void LegendsOfTheDeepReefmaw()
        {
            _builder.Create("mon_legend_reef", "Legends of the Deep: Reefmaw")
                .PrerequisiteQuest("mon_reef_cull")

                .AddState()
                .SetStateJournalText("Reefmaw Tidebreaker, an old Aradile, has been sinking skiffs around the inner Coral Isles of Mon Cala. It does not always show itself. Kill Reefmaw Tidebreaker there, then return to Warden Issa Tarn in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_ReefmawTidebreaker, 1)

                .AddState()
                .SetStateJournalText("You killed Reefmaw Tidebreaker. Return to Warden Issa Tarn in the Elite Hotel in Dac City on Mon Cala to collect your pay.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private void LegendsOfTheDeepGlassjaw()
        {
            _builder.Create("mon_legend_glass", "Legends of the Deep: Glassjaw")
                .PrerequisiteQuest("mon_legend_reef")

                .AddState()
                .SetStateJournalText("The Glassjaw Stalker lairs in the caves under the Sharptooth Jungle on Mon Cala and has stopped the cave survey. It does not always show itself. Kill the Glassjaw Stalker, then return to Warden Issa Tarn in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_GlassjawStalker, 1)

                .AddState()
                .SetStateJournalText("You killed the Glassjaw Stalker. Return to Warden Issa Tarn in the Elite Hotel in Dac City on Mon Cala to collect your pay.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private void LegendsOfTheDeepInkveil()
        {
            _builder.Create("mon_legend_ink", "Legends of the Deep: Inkveil")
                .PrerequisiteQuest("mon_legend_glass")

                .AddState()
                .SetStateJournalText("The Inkveil Matron, mother of the Octotench, rules the Sunkenhedge Swamps on Mon Cala. She does not always show herself. Kill the Inkveil Matron, then return to Warden Issa Tarn in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_InkveilMatron, 1)

                .AddState()
                .SetStateJournalText("You killed the Inkveil Matron. Return to Warden Issa Tarn in the Elite Hotel in Dac City on Mon Cala to collect your pay.")

                .AddGoldReward(4500)
                .AddXPReward(8000)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.LegendsOfTheDeep);
                });
        }

        private void SwampSweep()
        {
            _builder.Create("mon_swamp_sweep", "Swamp Sweep")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Warden Issa Tarn pays a standing rate for keeping the Tench down on Mon Cala. Kill 10 Microtench and 10 Octotench in the Sunkenhedge Swamps, then return to Warden Issa Tarn in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_Microtench, 10)
                .AddKillObjective(NPCGroupType.MonCala_Octotench, 10)

                .AddState()
                .SetStateJournalText("You killed 10 Microtench and 10 Octotench. Return to Warden Issa Tarn in the Elite Hotel in Dac City to collect the standing rate.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void AntivenomRun()
        {
            _builder.Create("mon_antivenom", "Antivenom Run")

                .AddState()
                .SetStateJournalText("The Dac City clinic has run out of antivenom. Collect 6 Viper Bile from the Vipers on the Coral Isles of Mon Cala, then bring them to Medic Orlu Vess in the Elite Hotel in Dac City.")
                .AddCollectItemObjective("viper_bile", 6)

                .AddState()
                .SetStateJournalText("You delivered 6 Viper Bile. Speak to Medic Orlu Vess in the Elite Hotel in Dac City on Mon Cala to collect your reward.")

                .AddGoldReward(1500)
                .AddXPReward(4000)
                .AddItemReward("med_supplies", 5);
        }

        private void GreyMatter()
        {
            _builder.Create("mon_grey_matter", "Grey Matter")

                .AddState()
                .SetStateJournalText("Medic Orlu Vess is studying why Amphi-Hydrus venom numbs divers. Collect 5 Amphi-Hydrus Brains from the Amphi-Hydrus on the Coral Isles of Mon Cala, then bring them to Medic Orlu Vess in the Elite Hotel in Dac City.")
                .AddCollectItemObjective("amphi_brain", 5)

                .AddState()
                .SetStateJournalText("You delivered 5 Amphi-Hydrus Brains. Speak to Medic Orlu Vess in the Elite Hotel in Dac City on Mon Cala to collect your reward.")

                .AddGoldReward(1500)
                .AddXPReward(4000);
        }

        private void ToxinTrade()
        {
            _builder.Create("mon_toxin_trade", "Toxin Trade")

                .AddState()
                .SetStateJournalText("The Dac City clinic has run out of its Octotench nerve block. Collect 4 Tench Poison and 4 Tench Sacs from the Octotench of the Sunkenhedge Swamps, then bring them to Medic Orlu Vess in the Elite Hotel in Dac City.")
                .AddCollectItemObjective("mtench_poison", 4)
                .AddCollectItemObjective("mtench_sac", 4)

                .AddState()
                .SetStateJournalText("You delivered 4 Tench Poison and 4 Tench Sacs. Speak to Medic Orlu Vess in the Elite Hotel in Dac City to collect your reward.")

                .AddGoldReward(2250)
                .AddXPReward(5000);
        }

        private void PoisonedTideCutTheLines()
        {
            _builder.Create("mon_tide_1", "Poisoned Tide: Cut the Lines")

                .AddState()
                .SetStateJournalText("Eco-terrorists are sabotaging the kelp farms that feed Dac City. Kill 15 Eco-Terrorists in the Sharptooth Jungle or at the Coral Isles facility on Mon Cala, then return to Captain Serrin Mool in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_EcoTerrorist, 15)

                .AddState()
                .SetStateJournalText("You killed 15 Eco-Terrorists. Return to Captain Serrin Mool in the Elite Hotel in Dac City on Mon Cala to collect your pay.")

                .AddGoldReward(2250)
                .AddXPReward(5000);
        }

        private void PoisonedTideStolenSamples()
        {
            _builder.Create("mon_tide_2", "Poisoned Tide: Stolen Samples")
                .PrerequisiteQuest("mon_tide_1")

                .AddState()
                .SetStateJournalText("Eco-terrorists stole living coral samples from a Dac City survey team. Recover 5 Coral Sample Canisters from the Eco-Terrorists on Mon Cala, then bring them to Captain Serrin Mool in the Elite Hotel in Dac City.")
                .AddCollectItemObjective("mon_coral_canist", 5)

                .AddState()
                .SetStateJournalText("You delivered 5 Coral Sample Canisters. Speak to Captain Serrin Mool in the Elite Hotel in Dac City on Mon Cala to collect your pay.")

                .AddGoldReward(2700)
                .AddXPReward(5000);
        }

        private void PoisonedTideTheReefsAnswer()
        {
            _builder.Create("mon_tide_3", "Poisoned Tide: The Reef's Answer")
                .PrerequisiteQuest("mon_tide_2")

                .AddState()
                .SetStateJournalText("The eco-terrorist cell answers to a leader who never leaves the Coral Isles facility on Mon Cala. Kill the Eco-Terrorist Leader there, then return to Captain Serrin Mool in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_EcoTerroristLeader, 1)

                .AddState()
                .SetStateJournalText("You killed the Eco-Terrorist Leader. Return to Captain Serrin Mool in the Elite Hotel in Dac City on Mon Cala to collect your reward.")

                .AddGoldReward(4500)
                .AddXPReward(8000)
                .AddItemReward("tidecall_pendant", 1)
                .OnCompleteAction((player, sourceObject) =>
                {
                    Achievement.GiveAchievement(player, AchievementType.PoisonedTide);
                });
        }

        private void WantedSableQuarr()
        {
            _builder.Create("mon_wanted_sable", "Wanted: Sable Quarr")
                .PrerequisiteQuest("mon_tide_1")

                .AddState()
                .SetStateJournalText("Sable Quarr, an eco-terrorist lieutenant, moves between the camps in the north of the Sharptooth Jungle on Mon Cala and is not always in the field. Kill Sable Quarr, then return to Captain Serrin Mool in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_SableQuarr, 1)

                .AddState()
                .SetStateJournalText("You killed Sable Quarr. Return to Captain Serrin Mool in the Elite Hotel in Dac City on Mon Cala to collect the bounty.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private void WantedKaelDrox()
        {
            _builder.Create("mon_wanted_kael", "Wanted: Kael Drox")
                .PrerequisiteQuest("mon_tide_1")

                .AddState()
                .SetStateJournalText("Kael Drox, an eco-terrorist commander, supplies the cell from the Coral Isles facility on Mon Cala and is not always present. Kill Kael Drox, then return to Captain Serrin Mool in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_KaelDrox, 1)

                .AddState()
                .SetStateJournalText("You killed Kael Drox. Return to Captain Serrin Mool in the Elite Hotel in Dac City on Mon Cala to collect the bounty.")

                .AddGoldReward(4500)
                .AddXPReward(8000);
        }

        private void ReefPatrol()
        {
            _builder.Create("mon_reef_patrol", "Reef Patrol")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("Dac City Security pays a standing rate for patrol sweeps against the eco-terrorists. Kill 20 Eco-Terrorists in the Sharptooth Jungle or at the Coral Isles facility on Mon Cala, then return to Captain Serrin Mool in the Elite Hotel in Dac City.")
                .AddKillObjective(NPCGroupType.MonCala_EcoTerrorist, 20)

                .AddState()
                .SetStateJournalText("You killed 20 Eco-Terrorists. Return to Captain Serrin Mool in the Elite Hotel in Dac City to collect the patrol rate.")

                .AddGoldReward(750)
                .AddXPReward(1750);
        }

        private void ATasteOfHome()
        {
            _builder.Create("mon_taste_home", "A Taste of Home")

                .AddState()
                .SetStateJournalText("The Elite Hotel kitchen wants local game for its menu. Collect 6 Aradile Tails and 4 Viper Meat on the Coral Isles of Mon Cala, then bring them to Quartermaster Dessa Korr in the Elite Hotel in Dac City.")
                .AddCollectItemObjective("aradile_tail", 6)
                .AddCollectItemObjective("viper_meat", 4)

                .AddState()
                .SetStateJournalText("You delivered 6 Aradile Tails and 4 Viper Meat. Speak to Quartermaster Dessa Korr in the Elite Hotel in Dac City to collect your reward.")

                .AddGoldReward(1500)
                .AddXPReward(4000)
                .AddItemReward("aradile_mball", 3);
        }

        private void InkForTheArchives()
        {
            _builder.Create("mon_archive_ink", "Ink for the Archives")

                .AddState()
                .SetStateJournalText("The Dac City archive writes its permanent ledgers in Tench ink. Collect 5 Tench Ink and 5 Tench Mantles from the Tench of the Sunkenhedge Swamps, then bring them to Quartermaster Dessa Korr in the Elite Hotel in Dac City.")
                .AddCollectItemObjective("mtench_ink", 5)
                .AddCollectItemObjective("tench_mantle", 5)

                .AddState()
                .SetStateJournalText("You delivered 5 Tench Ink and 5 Tench Mantles. Speak to Quartermaster Dessa Korr in the Elite Hotel in Dac City to collect your reward.")

                .AddGoldReward(2250)
                .AddXPReward(5000);
        }

        private void FeelersOut()
        {
            _builder.Create("mon_feelers", "Feelers Out")

                .AddState()
                .SetStateJournalText("A Dac City sensor technician wants to study how Scorchellus sense heat. Collect 6 Scorchellus Antennae from the Scorchellus of the Sunkenhedge Swamps on Mon Cala, then bring them to Quartermaster Dessa Korr in the Elite Hotel in Dac City.")
                .AddCollectItemObjective("scorch_antennae", 6)

                .AddState()
                .SetStateJournalText("You delivered 6 Scorchellus Antennae. Speak to Quartermaster Dessa Korr in the Elite Hotel in Dac City on Mon Cala to collect your reward.")

                .AddGoldReward(2700)
                .AddXPReward(5000);
        }

        private void OreForTheShipwrights()
        {
            _builder.Create("mon_shipwright_ore", "Ore for the Shipwrights")

                .AddState()
                .SetStateJournalText("The Dac City shipwrights are short of ore for patching hulls. Mine 10 Raw Plagionite and 10 Raw Scordspar on the Coral Isles of Mon Cala, then bring them to Quartermaster Dessa Korr in the Elite Hotel in Dac City.")
                .AddCollectItemObjective("raw_plagionite", 10)
                .AddCollectItemObjective("raw_scordspar", 10)

                .AddState()
                .SetStateJournalText("You delivered 10 Raw Plagionite and 10 Raw Scordspar. Speak to Quartermaster Dessa Korr in the Elite Hotel in Dac City to collect your reward.")

                .AddGoldReward(1500)
                .AddXPReward(3000);
        }
    }
}
