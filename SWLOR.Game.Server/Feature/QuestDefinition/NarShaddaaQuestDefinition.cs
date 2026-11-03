 using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
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
            SpaceDungeon_SlaverRaid();
            SpaceDungeon_PirateHideout();
			GreatArkanianDragonHunt();
            WardrobeExpansion1();
            WardrobeExpansion2();
            WardrobeExpansion3();
            WardrobeExpansion4();
            WardrobeExpansion5();
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
    }
}
