using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class CapstoneLootTableDefinition : ILootTableDefinition
    {
        private const string VelesMilitiaAnnexLessonLootTableId = "CAPSTONE_VELES_MILITIA_ANNEX_LESSON_LOOT";
        private const string VelesMilitiaAnnexBossLootTableId = "CAPSTONE_VELES_MILITIA_ANNEX_BOSS_LOOT";
        private const string DantooineJediEnclaveTrialHallsLessonLootTableId = "CAPSTONE_DANTOOINE_JEDI_ENCLAVE_TRIAL_HALLS_LESSON_LOOT";
        private const string DantooineJediEnclaveTrialHallsBossLootTableId = "CAPSTONE_DANTOOINE_JEDI_ENCLAVE_TRIAL_HALLS_BOSS_LOOT";
        private const string KorribanForgeCavernsLessonLootTableId = "CAPSTONE_KORRIBAN_FORGE_CAVERNS_LESSON_LOOT";
        private const string KorribanForgeCavernsBossLootTableId = "CAPSTONE_KORRIBAN_FORGE_CAVERNS_BOSS_LOOT";
        private const string SmugglersMoonFightClubBackroomsLessonLootTableId = "CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS_LESSON_LOOT";
        private const string SmugglersMoonFightClubBackroomsBossLootTableId = "CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS_BOSS_LOOT";
        private const string CZ220BreakerYardLessonLootTableId = "CAPSTONE_CZ220_BREAKER_YARD_LESSON_LOOT";
        private const string CZ220BreakerYardBossLootTableId = "CAPSTONE_CZ220_BREAKER_YARD_BOSS_LOOT";
        private const string AnchorheadCanyonRangeLessonLootTableId = "CAPSTONE_ANCHORHEAD_CANYON_RANGE_LESSON_LOOT";
        private const string AnchorheadCanyonRangeBossLootTableId = "CAPSTONE_ANCHORHEAD_CANYON_RANGE_BOSS_LOOT";
        private const string CzerkaArmsTestRangeLessonLootTableId = "CAPSTONE_CZERKA_ARMS_TEST_RANGE_LESSON_LOOT";
        private const string CzerkaArmsTestRangeBossLootTableId = "CAPSTONE_CZERKA_ARMS_TEST_RANGE_BOSS_LOOT";
        private const string HutlarQionTestSiteLessonLootTableId = "CAPSTONE_HUTLAR_QION_TEST_SITE_LESSON_LOOT";
        private const string HutlarQionTestSiteBossLootTableId = "CAPSTONE_HUTLAR_QION_TEST_SITE_BOSS_LOOT";
        private const string KorribanSithCryptDepthsLessonLootTableId = "CAPSTONE_KORRIBAN_SITH_CRYPT_DEPTHS_LESSON_LOOT";
        private const string KorribanSithCryptDepthsBossLootTableId = "CAPSTONE_KORRIBAN_SITH_CRYPT_DEPTHS_BOSS_LOOT";
        private const string ViscaraRepublicEngineeringBunkerLessonLootTableId = "CAPSTONE_VISCARA_REPUBLIC_ENGINEERING_BUNKER_LESSON_LOOT";
        private const string ViscaraRepublicEngineeringBunkerBossLootTableId = "CAPSTONE_VISCARA_REPUBLIC_ENGINEERING_BUNKER_BOSS_LOOT";
        private const string DantooineMedicalSublevelLessonLootTableId = "CAPSTONE_DANTOOINE_MEDICAL_SUBLEVEL_LESSON_LOOT";
        private const string DantooineMedicalSublevelBossLootTableId = "CAPSTONE_DANTOOINE_MEDICAL_SUBLEVEL_BOSS_LOOT";
        private const string DathomirTarnJunglePreserveLessonLootTableId = "CAPSTONE_DATHOMIR_TARN_JUNGLE_PRESERVE_LESSON_LOOT";
        private const string DathomirTarnJunglePreserveBossLootTableId = "CAPSTONE_DATHOMIR_TARN_JUNGLE_PRESERVE_BOSS_LOOT";
        private const string DathomirGrottoApexDenLessonLootTableId = "CAPSTONE_DATHOMIR_GROTTO_APEX_DEN_LESSON_LOOT";
        private const string DathomirGrottoApexDenBossLootTableId = "CAPSTONE_DATHOMIR_GROTTO_APEX_DEN_BOSS_LOOT";

        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            BuildVelesMilitiaAnnex();
            BuildDantooineJediEnclaveTrialHalls();
            BuildKorribanForgeCaverns();
            BuildSmugglersMoonFightClubBackrooms();
            BuildCZ220BreakerYard();
            BuildAnchorheadCanyonRange();
            BuildCzerkaArmsTestRange();
            BuildHutlarQionTestSite();
            BuildKorribanSithCryptDepths();
            BuildViscaraRepublicEngineeringBunker();
            BuildDantooineMedicalSublevel();
            BuildDathomirTarnJunglePreserve();
            BuildDathomirGrottoApexDen();

            return _builder.Build();
        }

        private void BuildVelesMilitiaAnnex()
        {
            BuildLessonLootTable(VelesMilitiaAnnexLessonLootTableId);
            BuildBossLootTable(VelesMilitiaAnnexBossLootTableId);
        }

        private void BuildDantooineJediEnclaveTrialHalls()
        {
            BuildLessonLootTable(DantooineJediEnclaveTrialHallsLessonLootTableId);
            BuildBossLootTable(DantooineJediEnclaveTrialHallsBossLootTableId);
        }

        private void BuildKorribanForgeCaverns()
        {
            BuildLessonLootTable(KorribanForgeCavernsLessonLootTableId);
            BuildBossLootTable(KorribanForgeCavernsBossLootTableId);
        }

        private void BuildSmugglersMoonFightClubBackrooms()
        {
            BuildLessonLootTable(SmugglersMoonFightClubBackroomsLessonLootTableId);
            BuildBossLootTable(SmugglersMoonFightClubBackroomsBossLootTableId);
        }

        private void BuildCZ220BreakerYard()
        {
            BuildLessonLootTable(CZ220BreakerYardLessonLootTableId);
            BuildBossLootTable(CZ220BreakerYardBossLootTableId);
        }

        private void BuildAnchorheadCanyonRange()
        {
            BuildLessonLootTable(AnchorheadCanyonRangeLessonLootTableId);
            BuildBossLootTable(AnchorheadCanyonRangeBossLootTableId);
        }

        private void BuildCzerkaArmsTestRange()
        {
            BuildLessonLootTable(CzerkaArmsTestRangeLessonLootTableId);
            BuildBossLootTable(CzerkaArmsTestRangeBossLootTableId);
        }

        private void BuildHutlarQionTestSite()
        {
            BuildLessonLootTable(HutlarQionTestSiteLessonLootTableId);
            BuildBossLootTable(HutlarQionTestSiteBossLootTableId);
        }

        private void BuildKorribanSithCryptDepths()
        {
            BuildLessonLootTable(KorribanSithCryptDepthsLessonLootTableId);
            BuildBossLootTable(KorribanSithCryptDepthsBossLootTableId);
        }

        private void BuildViscaraRepublicEngineeringBunker()
        {
            BuildLessonLootTable(ViscaraRepublicEngineeringBunkerLessonLootTableId);
            BuildBossLootTable(ViscaraRepublicEngineeringBunkerBossLootTableId);
        }

        private void BuildDantooineMedicalSublevel()
        {
            BuildLessonLootTable(DantooineMedicalSublevelLessonLootTableId);
            BuildBossLootTable(DantooineMedicalSublevelBossLootTableId);
        }

        private void BuildDathomirTarnJunglePreserve()
        {
            BuildLessonLootTable(DathomirTarnJunglePreserveLessonLootTableId);
            BuildBossLootTable(DathomirTarnJunglePreserveBossLootTableId);
        }

        private void BuildDathomirGrottoApexDen()
        {
            BuildLessonLootTable(DathomirGrottoApexDenLessonLootTableId);
            BuildBossLootTable(DathomirGrottoApexDenBossLootTableId);
        }

        private void BuildLessonLootTable(string tableId)
        {
            _builder.Create(tableId)
                .AddItem("elec_good", 20)
                .AddItem("med_supplies", 10, 3)
                .AddItem("stim_pack", 10, 3)
                .AddItem("lth_flawed", 10)
                .AddGold(150, 25);
        }

        private void BuildBossLootTable(string tableId)
        {
            _builder.Create(tableId)
                .AddItem("elec_good", 30)
                .AddItem("med_supplies", 15, 5)
                .AddItem("stim_pack", 15, 5)
                .AddItem("lth_good", 10)
                .AddGold(350, 25);
        }
    }
}
