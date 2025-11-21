using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class HuntersGuildQuestDefinition : IQuestListDefinition
    {
        private class RewardDetails
        {
            public int Gold { get; set; }
            public int GP { get; set; }

            public RewardDetails(int gold, int gp)
            {
                Gold = gold;
                GP = gp;
            }
        }

        private readonly Dictionary<int, RewardDetails> _rewardDetails = new()
        {
            { 0, new RewardDetails(138, 21)},
            { 1, new RewardDetails(343, 81)},
            { 2, new RewardDetails(532, 117)},
            { 3, new RewardDetails(733, 156)},
            { 4, new RewardDetails(874, 195)},
            { 5, new RewardDetails(960, 246)},
        };

        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {


            // Tier 1 (Rank 0)
            BuildKillTask("hun_tsk_001", NPCGroupType.CZ220_ColicoidExperiment, 3, 0);
            BuildKillTask("hun_tsk_002", NPCGroupType.CZ220_MalfunctioningDroids, 10, 0);
            BuildKillTask("hun_tsk_003", NPCGroupType.CZ220_Mynocks, 10, 0);
            BuildItemTask("hun_tsk_004", "k_hound_fur", 6, 0);
            BuildItemTask("hun_tsk_005", "kath_meat_1", 6, 0);
            BuildItemTask("hun_tsk_006", "k_hound_tooth", 6, 0);
            BuildItemTask("hun_tsk_007", "mynock_tooth", 6, 0);
            BuildItemTask("hun_tsk_008", "mynock_wing", 6, 0);
            BuildKillTask("hun_tsk_009", NPCGroupType.Viscara_WildlandKathHounds, 10, 0);
            BuildKillTask("hun_tsk_010", NPCGroupType.Viscara_WildlandsWarocas, 10, 0);
            BuildItemTask("hun_tsk_011", "waro_leg", 6, 0);
            BuildItemTask("hun_tsk_012", "warocas_meat", 6, 0);
            BuildItemTask("hun_tsk_013", "waro_feathers", 6, 0);
            BuildKillTask("hun_tsk_014", NPCGroupType.Viscara_WildwoodsGimpassas, 8, 0);
            BuildKillTask("hun_tsk_015", NPCGroupType.Viscara_WildwoodsKinraths, 10, 0);
            BuildKillTask("hun_tsk_016", NPCGroupType.Viscara_WildwoodsOutlaws, 10, 0);
            BuildItemTask("hun_tsk_017", "aluminum", 3, 0);
            BuildItemTask("hun_tsk_018", "elec_ruined", 10, 0);

            // Tier 2 (Rank 1)
            BuildItemTask("hun_tsk_200", "herb_m", 6, 1);
            BuildItemTask("hun_tsk_201", "m_blast_parts", 6, 1);
            BuildItemTask("hun_tsk_202", "man_tags", 10, 1);
            BuildItemTask("hun_tsk_203", "m_lvibro_parts", 6, 1);
            BuildKillTask("hun_tsk_204", NPCGroupType.Viscara_MandalorianLeader, 1, 1);
            BuildItemTask("hun_tsk_205", "m_ls_parts", 6, 1);
            BuildItemTask("hun_tsk_206", "m_plexiplate", 6, 1);
            BuildItemTask("hun_tsk_207", "m_polearm_parts", 6, 1);
            BuildKillTask("hun_tsk_208", NPCGroupType.Viscara_MandalorianRangers, 10, 1);
            BuildItemTask("hun_tsk_209", "m_vibro_parts", 6, 1);
            BuildKillTask("hun_tsk_210", NPCGroupType.Viscara_MandalorianWarriors, 10, 1);
            BuildKillTask("hun_tsk_211", NPCGroupType.Viscara_ValleyCairnmogs, 10, 1);
            BuildKillTask("hun_tsk_212", NPCGroupType.Viscara_ValleyNashtah, 10, 1);
            BuildKillTask("hun_tsk_213", NPCGroupType.Viscara_DeepMountainRaivors, 10, 1);
            BuildKillTask("hun_tsk_214", NPCGroupType.Viscara_CrystalSpider, 10, 1);
            BuildItemTask("hun_tsk_215", "steel", 3, 1);
            BuildItemTask("hun_tsk_216", "elec_flawed", 10, 1);
            BuildKillTask("hun_tsk_217", NPCGroupType.Korriban_Tukata, 10, 1);
            BuildKillTask("hun_tsk_218", NPCGroupType.Korriban_Hssiss, 10, 1);
            BuildKillTask("hun_tsk_219", NPCGroupType.Korriban_Shyrack, 10, 1);
            BuildKillTask("hun_tsk_220", NPCGroupType.Korriban_MorabandSerpent, 10, 1);
            BuildKillTask("hun_tsk_221", NPCGroupType.Korriban_SithApprenticeGhost, 10, 1);
            BuildKillTask("hun_tsk_222", NPCGroupType.Korriban_Terentatek, 5, 1);
            BuildKillTask("hun_tsk_223", NPCGroupType.MonCala_Octotench, 10, 1);
            BuildKillTask("hun_tsk_224", NPCGroupType.MonCala_Microtench, 10, 1);
            BuildKillTask("hun_tsk_225", NPCGroupType.MonCala_Scorchellus, 10, 1);

            // Tier 3 (Rank 2)
            BuildItemTask("hun_tsk_400", "amphi_brain", 6, 2);
            BuildItemTask("hun_tsk_401", "amphi_brain2", 6, 2);
            BuildKillTask("hun_tsk_402", NPCGroupType.MonCala_AmphiHydrus, 10, 2);
            BuildKillTask("hun_tsk_403", NPCGroupType.MonCala_Aradile, 10, 2);
            BuildKillTask("hun_tsk_404", NPCGroupType.MonCala_Viper, 10, 2);
            BuildKillTask("hun_tsk_405", NPCGroupType.Viscara_VellenFlesheater, 10, 2);
            BuildKillTask("hun_tsk_406", NPCGroupType.Viscara_VellenFleshleader, 1, 2);
            BuildKillTask("hun_tsk_407", NPCGroupType.Hutlar_Byysk, 10, 2);
            BuildKillTask("hun_tsk_408", NPCGroupType.Hutlar_QionSlugs, 10, 2);
            BuildKillTask("hun_tsk_409", NPCGroupType.Hutlar_QionTigers, 10, 2);
            BuildItemTask("hun_tsk_410", "obsidian", 3, 2);
            BuildItemTask("hun_tsk_411", "elec_good", 10, 2);
            BuildItemTask("hun_tsk_412", "byysk_longsword", 3, 2);
            BuildItemTask("hun_tsk_413", "byysk_knife", 3, 2);
            BuildItemTask("hun_tsk_414", "byysk_gswd", 3, 2);
            BuildItemTask("hun_tsk_415", "byysk_spear", 3, 2);
            BuildItemTask("hun_tsk_416", "byysk_katar", 3, 2);
            BuildItemTask("hun_tsk_417", "byysk_staff", 3, 2);
            BuildItemTask("hun_tsk_418", "byysk_pistol", 3, 2);
            BuildItemTask("hun_tsk_419", "byysk_shuriken", 3, 2);
            BuildItemTask("hun_tsk_420", "byysk_twinblade", 3, 2);
            BuildItemTask("hun_tsk_421", "byysk_rifle", 3, 2);
            BuildItemTask("hun_tsk_422", "byysk_meat", 6, 2);
            BuildItemTask("hun_tsk_423", "byysk_tail", 6, 2);
            BuildItemTask("hun_tsk_424", "citrine", 3, 2);
            BuildItemTask("hun_tsk_425", "slug_bile", 6, 2);
            BuildItemTask("hun_tsk_426", "slug_tooth", 6, 2);
            BuildItemTask("hun_tsk_427", "qion_tiger_fang", 6, 2);
            BuildItemTask("hun_tsk_428", "tiger_blood", 6, 2);
            BuildItemTask("hun_tsk_429", "tiger_meat", 6, 2);
            BuildItemTask("hun_tsk_430", "q_tiger_paw", 3, 2);

            // Tier 4 (Rank 3)
            BuildKillTask("hun_tsk_600", NPCGroupType.Tatooine_Womprat, 10, 3);
            BuildKillTask("hun_tsk_601", NPCGroupType.Tatooine_Sandswimmer, 10, 3);
            BuildKillTask("hun_tsk_602", NPCGroupType.Tatooine_SandBeetle, 10, 3);
            BuildKillTask("hun_tsk_603", NPCGroupType.Tatooine_SandDemon, 10, 3);
            BuildKillTask("hun_tsk_604", NPCGroupType.Tatooine_TuskenRaider, 10, 3);
            BuildItemTask("hun_tsk_605", "crystal", 3, 3);
            BuildItemTask("hun_tsk_606", "elec_imperfect", 10, 3);
            BuildItemTask("hun_tsk_607", "womprathide", 6, 3);
            BuildItemTask("hun_tsk_608", "womprattooth", 6, 3);
            BuildItemTask("hun_tsk_609", "wompratclaw", 6, 3);
            BuildItemTask("hun_tsk_610", "wompratmeat", 6, 3);
            BuildItemTask("hun_tsk_611", "womp_innards", 6, 3);
            BuildItemTask("hun_tsk_612", "sandswimmerfin", 6, 3);
            BuildItemTask("hun_tsk_613", "sandswimmerh", 6, 3);
            BuildItemTask("hun_tsk_614", "sandswimmerleg", 6, 3);
            BuildItemTask("hun_tsk_615", "shat_beetle_chit", 6, 3);
            BuildItemTask("hun_tsk_616", "beetle_chitin", 6, 3);
            BuildItemTask("hun_tsk_617", "wild_leg", 6, 3);
            BuildItemTask("hun_tsk_618", "sanddemonclaw", 6, 3);
            BuildItemTask("hun_tsk_619", "sanddemonhide", 6, 3);
            BuildItemTask("hun_tsk_620", "sand_demon_leg", 6, 3);
            BuildItemTask("hun_tsk_621", "ruby", 3, 3);
            BuildItemTask("hun_tsk_622", "sandwormtooth", 3, 3);
            BuildItemTask("hun_tsk_623", "raider_longsword", 3, 3);
            BuildItemTask("hun_tsk_624", "raider_knife", 3, 3);
            BuildItemTask("hun_tsk_625", "raider_gswd", 3, 3);
            BuildItemTask("hun_tsk_626", "raider_spear", 3, 3);
            BuildItemTask("hun_tsk_627", "raider_katar", 3, 3);
            BuildItemTask("hun_tsk_628", "raider_staff", 3, 3);
            BuildItemTask("hun_tsk_629", "raider_pistol", 3, 3);
            BuildItemTask("hun_tsk_630", "raider_shuriken", 3, 3);
            BuildItemTask("hun_tsk_631", "raider_twinblade", 3, 3);
            BuildItemTask("hun_tsk_632", "raider_rifle", 3, 3);
            BuildItemTask("hun_tsk_633", "tusken_meat", 6, 3);
            BuildItemTask("hun_tsk_634", "tusken_bones", 6, 3);
            BuildItemTask("hun_tsk_635", "tusken_blood", 6, 3);

            // Tier 5 (Rank 4)
            BuildItemTask("hun_tsk_800", "diamond", 3, 4);
            BuildItemTask("hun_tsk_801", "elec_high", 10, 4);
            BuildItemTask("hun_tsk_802", "emerald", 6, 4);
            BuildKillTask("hun_tsk_803", NPCGroupType.Dathomir_DragonTurtle, 10, 4);
            BuildKillTask("hun_tsk_804", NPCGroupType.Dathomir_KwiGuardian, 10, 4);
            BuildKillTask("hun_tsk_805", NPCGroupType.Dathomir_KwiShaman, 10, 4);
            BuildKillTask("hun_tsk_806", NPCGroupType.Dathomir_KwiTribal, 10, 4);
            BuildKillTask("hun_tsk_807", NPCGroupType.Dathomir_Purbole, 10, 4);
            BuildKillTask("hun_tsk_808", NPCGroupType.Dathomir_ShearMite, 10, 4);
            BuildKillTask("hun_tsk_809", NPCGroupType.Dathomir_Sprantal, 10, 4);
            BuildKillTask("hun_tsk_810", NPCGroupType.Dathomir_Squellbug, 10, 4);
            BuildKillTask("hun_tsk_811", NPCGroupType.Dathomir_Ssurian, 10, 4);
            BuildKillTask("hun_tsk_812", NPCGroupType.Dathomir_SwamplandBug, 10, 4);
            BuildKillTask("hun_tsk_813", NPCGroupType.Dantooine_KinrathQueen, 10, 4);
            BuildKillTask("hun_tsk_814", NPCGroupType.Dantooine_Iriaz, 10, 4);
            BuildKillTask("hun_tsk_815", NPCGroupType.Dantooine_VoritorLizard, 10, 4);
            BuildKillTask("hun_tsk_816", NPCGroupType.Dantooine_Gizka, 10, 4);
            BuildKillTask("hun_tsk_817", NPCGroupType.Dantooine_PlainsThune, 10, 4);
            BuildKillTask("hun_tsk_818", NPCGroupType.Dantooine_Bol, 10, 4);
            BuildKillTask("hun_tsk_819", NPCGroupType.Dantooine_DantariShaman, 10, 4);




            return _builder.Build();
        }

        private void BuildItemTask(
            string questId, 
            string resref, 
            int amount, 
            int guildRank)
        {
            var itemName = Cache.GetItemNameByResref(resref);
            var rewardDetails = _rewardDetails[guildRank];

            _builder.Create(questId, $"{amount}x {itemName}")
                .IsRepeatable()
                .IsGuildTask(GuildType.HuntersGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Collect {amount}x {itemName} and return to the Hunter's Guildmaster")
                .AddCollectItemObjective(resref, amount)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.HuntersGuild, rewardDetails.GP);
        }
        private void BuildKillTask(
            string questId,
            NPCGroupType group,
            int amount,
            int guildRank)
        {
            var groupDetail = NPCGroup.GetNPCGroup(group);
            var rewardDetails = _rewardDetails[guildRank];

            _builder.Create(questId, $"Kill {amount}x {groupDetail.Name}")
                .IsRepeatable()
                .IsGuildTask(GuildType.HuntersGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Kill {amount}x {groupDetail.Name} and return to the Hunter's Guildmaster")
                .AddKillObjective(group, amount)

                .AddState()
                .SetStateJournalText($"Return to the Hunter's Guildmaster to report your progress.")

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.HuntersGuild, rewardDetails.GP);
        }
    }
}
