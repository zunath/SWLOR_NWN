using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP2.5 game code index: <see cref="GameCodeIndex"/> reading NPC group and
    /// key item enums via reflection, and quest/spawn table IDs via source scanning of
    /// SWLOR.Game.Server's QuestDefinition and SpawnDefinition folders.
    /// </summary>
    public class GameCodeIndexTests
    {
        /// <summary>
        /// Locates the repository root from the test execution context by walking up from the test
        /// assembly location until a "SWLOR.Game.Server" folder containing
        /// "SWLOR.Game.Server.csproj" is found. Deliberately independent from <see cref="CorpusLocator"/>
        /// and <see cref="ResourceIndexTests"/>'s locator per this repo's per-file locator convention.
        /// </summary>
        private static string GameServerSourceRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "SWLOR.Game.Server");
                    if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.csproj")))
                        return candidate;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the SWLOR.Game.Server source directory from the test context.");
            }
        }

        private static GameCodeIndex CreateIndex() => new(GameServerSourceRoot);

        [Test]
        public void NpcGroups_Contains_Known_Entries_With_Correct_Display_Names()
        {
            var index = CreateIndex();

            // From SWLOR.Game.Server/Service/NPCService/NPCGroupType.cs
            index.NpcGroups.Should().ContainKey(1).WhoseValue.Should().Be("Mynocks");
            index.NpcGroups.Should().ContainKey(22).WhoseValue.Should().Be("Zombie Rancor");

            index.IsValidNpcGroup(1).Should().BeTrue();
            index.IsValidNpcGroup(22).Should().BeTrue();
            index.IsValidNpcGroup(int.MaxValue).Should().BeFalse();
        }

        [Test]
        public void KeyItems_Contains_Known_Entry_With_Correct_Display_Name()
        {
            var index = CreateIndex();

            // From SWLOR.Game.Server/Service/KeyItemService/KeyItemType.cs
            index.KeyItems.Should().ContainKey(5).WhoseValue.Should().Be("CZ-220 Shuttle Pass");
            index.KeyItems.Should().ContainKey(20).WhoseValue.Should().Be("Coxxion Base Key");
        }

        [Test]
        public void QuestIds_Contains_Known_Ids_And_Is_A_Plausible_Count()
        {
            var index = CreateIndex();

            index.IsSourceScanAvailable.Should().BeTrue();

            // From SWLOR.Game.Server/Feature/QuestDefinition/CZ220QuestDefinition.cs
            index.IsValidQuestId("selan_request").Should().BeTrue();
            // From SWLOR.Game.Server/Feature/QuestDefinition/BeastMasteryCapstoneQuestDefinition.cs
            // (declared via `internal const string PrimalOverrunMasteryQuestId = "primal_overrun_mastery";`)
            index.IsValidQuestId("primal_overrun_mastery").Should().BeTrue();

            index.IsValidQuestId("not_a_real_quest_id").Should().BeFalse();

            // There are 32 quest definition files contributing well over 200 IDs (direct literals
            // plus same-file const resolution); 50 is a conservative floor.
            index.QuestIds.Count.Should().BeGreaterThan(50);

            // Guild quests are built by helpers whose FIRST parameter is a QuestBuilder, which the
            // id scanner's string-first pattern cannot expand - the detailed scan's keys must be
            // merged in or the Quest Activator picker loses hundreds of quests.
            index.IsValidQuestId("eng_tsk_001").Should().BeTrue(
                "helper-built guild quests appear only in the detailed scan");
        }

        [Test]
        public void SpawnTableIds_Contains_Known_Ids_And_Is_A_Plausible_Count()
        {
            var index = CreateIndex();

            index.IsSourceScanAvailable.Should().BeTrue();

            // From SWLOR.Game.Server/Feature/SpawnDefinition/CZ220SpawnDefinition.cs
            index.IsValidSpawnTableId("CZ220_DROIDS").Should().BeTrue();
            index.IsValidSpawnTableId("CZ220_MYNOCKS").Should().BeTrue();

            index.IsValidSpawnTableId("NOT_A_REAL_SPAWN_TABLE").Should().BeFalse();

            // 20 spawn definition files contributing well over 150 IDs; 50 is a conservative floor.
            index.SpawnTableIds.Count.Should().BeGreaterThan(50);
        }

        [Test]
        public void SpawnTableIds_ContainEveryTableBuiltAtRuntime()
        {
            var index = CreateIndex();
            var runtimeIds = typeof(ISpawnListDefinition).Assembly
                .GetTypes()
                .Where(type =>
                    !type.IsAbstract &&
                    typeof(ISpawnListDefinition).IsAssignableFrom(type))
                .Select(type => (ISpawnListDefinition)Activator.CreateInstance(type)!)
                .SelectMany(definition => definition.BuildSpawnTables().Keys)
                .ToHashSet(StringComparer.Ordinal);

            index.SpawnTableIds.Should().BeEquivalentTo(runtimeIds);
            index.SpawnTableIds.Should().Contain(
                "SLICING_TERMINAL_T1",
                "SLICING_TERMINAL_T5");
        }

        [Test]
        public void FishingHelpersContributeAllFishingSpawnTableIds()
        {
            var index = CreateIndex();

            index.FishingSpawnTableIds.Should().HaveCount(31);
            index.FishingSpawnTableIds.Should().Contain("FP_VISC_CAVERN");
            index.FishingSpawnTableIds.Should().Contain("FP_DAN_FORSAKEN_JUNGLES");
            index.SpawnTableIds.Should().Contain(index.FishingSpawnTableIds,
                "helper-created tables are still ordinary spawn tables");

            index.FishingSpawnTables.Should().Contain(table =>
                table.Id == "FP_VISC_CAVERN" &&
                table.DisplayName == "Viscara Cavern");
            index.SpawnTables.Should().Contain(table =>
                table.Id == "CZ220_DROIDS" &&
                table.DisplayName == "CZ-220 Droids");
        }

        [Test]
        public void WaypointDestinationsComeFromTheServerDeclarations()
        {
            var index = CreateIndex();

            index.PlanetLandingWaypoints.Should().HaveCount(10);
            index.PlanetLandingWaypoints.Should().Contain(
                destination => destination.Tag == "VISCARA_LANDING" &&
                               destination.DisplayName == "Viscara");

            index.OrbitWaypoints.Should().HaveCount(10);
            index.OrbitWaypoints.Should().Contain(
                destination => destination.Tag == "Viscara_Orbit" &&
                               destination.DisplayName == "Viscara");

            index.TaxiDestinations.Should().HaveCount(14);
            index.TaxiDestinations.Should().Contain(
                destination => destination.Tag == "TAXI_DANTOOINE_GARRISON" &&
                               destination.DisplayName == "Dantooine Republic Garrison" &&
                               destination.RegionId == 2 &&
                               destination.Price == 150);

            index.DeathRespawnWaypointTags.Should().BeEquivalentTo(
                "DEATH_DEFAULT_RESPAWN_POINT",
                "DTH_DEFAULT_RESPAWN_POINT");

            index.RebuildWaypointTags.Should().BeEquivalentTo(
                "REBUILD_LANDING",
                "REBUILD_TO_SPENDING_LANDING");
        }

        [Test]
        public void Missing_Source_Root_Yields_Empty_Collections_And_Unavailable_Flag_Without_Throwing()
        {
            GameCodeIndex? index = null;

            var act = () => index = new GameCodeIndex(@"D:\this\path\does\not\exist\at\all");

            act.Should().NotThrow();

            index!.IsSourceScanAvailable.Should().BeFalse();
            index.QuestIds.Should().BeEmpty();
            index.SpawnTableIds.Should().BeEmpty();
            index.IsValidQuestId("selan_request").Should().BeFalse();
            index.IsValidSpawnTableId("CZ220_DROIDS").Should().BeFalse();

            // Enum-backed collections don't depend on the source root and remain populated.
            index.NpcGroups.Should().NotBeEmpty();
            index.KeyItems.Should().NotBeEmpty();
        }

        [Test]
        public void Null_Source_Root_Yields_Empty_Collections_And_Unavailable_Flag_Without_Throwing()
        {
            GameCodeIndex? index = null;

            var act = () => index = new GameCodeIndex(null);

            act.Should().NotThrow();

            index!.IsSourceScanAvailable.Should().BeFalse();
            index.QuestIds.Should().BeEmpty();
            index.SpawnTableIds.Should().BeEmpty();
        }
    }
}
