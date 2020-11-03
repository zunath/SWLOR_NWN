using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
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

        private Dictionary<int, RewardDetails> _rewardDetails { get; set; }

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            _rewardDetails = new Dictionary<int, RewardDetails>
            {
                { 0, new RewardDetails(23, 7)},
                { 1, new RewardDetails(84, 27)},
                { 2, new RewardDetails(122, 39)},
                { 3, new RewardDetails(184, 52)},
                { 4, new RewardDetails(245, 65)},
                { 5, new RewardDetails(312, 82)},
            };

            var builder = new QuestBuilder();
            AmphiHydrusBrain(builder);
            AmphiHydrusBrainStem(builder);
            CrystalSpider(builder);
            ColicoidExperiment(builder);
            MalfunctioningDroid(builder);
            Mynock(builder);
            DamagedBlueCrystal(builder);
            DamagedGreenCrystal(builder);
            DamagedRedCrystal(builder);
            DamagedYellowCrystal(builder);
            KathHoundFur(builder);
            KathHoundMeat(builder);
            KathHoundTooth(builder);
            MandaloreHerb(builder);
            MandalorianBlasterParts(builder);
            MandalorianDogTags(builder);
            MandalorianLargeVibrobladeParts(builder);
            MandalorianLeader(builder);
            MandalorianLightsaberParts(builder);
            MandalorianPlexiplate(builder);
            MandalorianPolearmParts(builder);
            MandalorianRanger(builder);
            MandalorianVibrobladeParts(builder);
            MandalorianWarrior(builder);
            MonCalaAmphiHydrus(builder);
            MonCalaAradile(builder);
            MonCalaViper(builder);
            MynockTooth(builder);
            MynockWing(builder);
            ValleyCairnmog(builder);
            ValleyNashtah(builder);
            ValleyRaivor(builder);
            VellenFlesheater(builder);
            VellenFleshleader(builder);
            ViscaraKathHound(builder);
            ViscaraWarocas(builder);
            WarocasLeg(builder);
            WarocasMeat(builder);
            WarocasSpine(builder);
            WildwoodsGimpassa(builder);
            WildwoodsKinrath(builder);
            WildwoodsOutlaw(builder);

            return builder.Build();
        }

        private void BuildItemTask(
            QuestBuilder builder, 
            string questId, 
            string resref, 
            int amount, 
            int guildRank)
        {
            var itemName = Cache.GetItemNameByResref(resref);
            var rewardDetails = _rewardDetails[guildRank];

            builder.Create(questId, $"{amount}x {itemName}")
                .IsRepeatable()
                .IsGuildTask(GuildType.HuntersGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Collect {amount}x {itemName} and return to the Hunter's Guildmaster")
                .AddCollectItemObjective(resref, amount)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.HuntersGuild, rewardDetails.GP);
        }
        private void BuildKillTask(
            QuestBuilder builder,
            string questId,
            NPCGroupType group,
            int amount,
            int guildRank)
        {
            var groupDetail = Quest.GetNPCGroup(group);
            var rewardDetails = _rewardDetails[guildRank];

            builder.Create(questId, $"Kill {amount}x {groupDetail.Name}")
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

        private void AmphiHydrusBrain(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_608", "amphi_brain", 6, 2);
        }

        private void AmphiHydrusBrainStem(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_607", "amphi_brain2", 6, 2);
        }

        private void CrystalSpider(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_603", NPCGroupType.Viscara_CrystalSpider, 10, 2);
        }

        private void ColicoidExperiment(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_569", NPCGroupType.CZ220_ColicoidExperiment, 10, 0);
        }

        private void MalfunctioningDroid(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_568", NPCGroupType.CZ220_MalfunctioningDroids, 10, 0);
        }

        private void Mynock(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_567", NPCGroupType.CZ220_Mynocks, 10, 0);
        }

        private void DamagedBlueCrystal(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_599", "p_crystal_blue", 6, 2);
        }
        private void DamagedGreenCrystal(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_600", "p_crystal_green", 6, 2);
        }
        private void DamagedRedCrystal(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_601", "p_crystal_red", 6, 2);
        }
        private void DamagedYellowCrystal(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_602", "p_crystal_yellow", 6, 2);
        }
        private void KathHoundFur(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_579", "k_hound_fur", 6, 0);
        }
        private void KathHoundMeat(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_580", "kath_meat_1", 6, 0);
        }
        private void KathHoundTooth(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_581", "k_hound_tooth", 6, 0);
        }
        private void MandaloreHerb(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_591", "herb_m", 6, 1);
        }
        private void MandalorianBlasterParts(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_594", "m_blast_parts", 6, 1);
        }
        private void MandalorianDogTags(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_592", "man_tags", 6, 1);
        }
        private void MandalorianLargeVibrobladeParts(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_595", "m_lvibro_parts", 6, 1);
        }
        private void MandalorianLeader(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_587", NPCGroupType.Viscara_MandalorianLeader, 1, 1);
        }
        private void MandalorianLightsaberParts(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_596", "m_ls_parts", 6, 1);
        }
        private void MandalorianPlexiplate(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_593", "m_plexiplate", 6, 1);
        }
        private void MandalorianPolearmParts(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_597", "m_polearm_parts", 6, 1);
        }
        private void MandalorianRanger(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_586", NPCGroupType.Viscara_MandalorianRangers, 10, 1);
        }
        private void MandalorianVibrobladeParts(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_598", "m_vibro_parts", 6, 1);
        }
        private void MandalorianWarrior(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_585", NPCGroupType.Viscara_MandalorianWarriors, 10, 1);
        }
        private void MonCalaAmphiHydrus(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_606", NPCGroupType.MonCala_AmphiHydrus, 10, 2);
        }
        private void MonCalaAradile(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_604", NPCGroupType.MonCala_Aradile, 10, 2);
        }
        private void MonCalaViper(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_605", NPCGroupType.MonCala_Viper, 10, 2);
        }
        private void MynockTooth(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_576", "mynock_tooth", 6, 0);
        }
        private void MynockWing(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_575", "mynock_wing", 6, 0);
        }
        private void ValleyCairnmog(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_588", NPCGroupType.Viscara_ValleyCairnmogs, 10, 1);
        }
        private void ValleyNashtah(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_590", NPCGroupType.Viscara_ValleyNashtah, 10, 1);
        }
        private void ValleyRaivor(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_589", NPCGroupType.Viscara_DeepMountainRaivors, 10, 1);
        }
        private void VellenFlesheater(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_610", NPCGroupType.Viscara_VellenFlesheater, 10, 2);
        }
        private void VellenFleshleader(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_609", NPCGroupType.Viscara_VellenFleshleader, 1, 2);
        }
        private void ViscaraKathHound(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_570", NPCGroupType.Viscara_WildlandKathHounds, 10, 0);
        }
        private void ViscaraWarocas(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_571", NPCGroupType.Viscara_WildlandsWarocas, 10, 0);
        }
        private void WarocasLeg(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_582", "waro_leg", 6, 0);
        }
        private void WarocasMeat(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_583", "warocas_meat", 6, 0);
        }
        private void WarocasSpine(QuestBuilder builder)
        {
            BuildItemTask(builder, "hun_tsk_584", "waro_feathers", 6, 0);
        }
        private void WildwoodsGimpassa(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_573", NPCGroupType.Viscara_WildwoodsGimpassas, 8, 0);
        }
        private void WildwoodsKinrath(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_574", NPCGroupType.Viscara_WildwoodsKinraths, 10, 0);
        }
        private void WildwoodsOutlaw(QuestBuilder builder)
        {
            BuildKillTask(builder, "hun_tsk_572", NPCGroupType.Viscara_WildwoodsOutlaws, 10, 0);
        }

    }
}