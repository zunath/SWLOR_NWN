using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Validation;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for WP3.4's convention validation rules. Each rule gets small, synthetic
    /// module fixtures (built either by copying a real corpus blueprint and mutating a field
    /// through its typed document view, or by constructing a minimal GFF document from scratch
    /// via the public JsonGffStruct/JsonGffField API) so tests stay fast without parsing the
    /// full ~17,900-file corpus. <see cref="ModuleValidatorFullCorpusTests"/> is the sole
    /// exception - an [Explicit] test that runs the real thing for manual review.
    /// </summary>
    public class ResRefLengthRuleTests
    {
        [Test]
        public void ResRef_LongerThan16Characters_FiresError()
        {
            using var module = SyntheticModule.Create();
            module.CopyBlueprintRaw(ResourceType.Utc, "alask", "a_resref_name_too_long");

            var issues = new ResRefLengthRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().Contain(i =>
                i.Severity == ValidationSeverity.Error &&
                i.RuleId == "ResRefLength" &&
                i.ResRef == "a_resref_name_too_long" &&
                i.Message.Contains("16"));
        }

        [Test]
        public void ResRef_Uppercase_FiresError_ButNotLengthError()
        {
            using var module = SyntheticModule.Create();
            module.CopyBlueprintRaw(ResourceType.Utc, "alask", "AlaskTest");

            var issues = new ResRefLengthRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().Contain(i => i.ResRef == "AlaskTest" && i.Message.Contains("lowercase"));
            issues.Should().NotContain(i => i.ResRef == "AlaskTest" && i.Message.Contains("16"));
        }

        [Test]
        public void KnownGoodResRef_ShortAndLowercase_NoIssue()
        {
            using var module = SyntheticModule.Create();
            module.CopyBlueprintRaw(ResourceType.Utc, "alask", "alask");

            var issues = new ResRefLengthRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().BeEmpty();
        }

        [Test]
        public void ResRef_WithIllegalCharacters_FiresError()
        {
            using var module = SyntheticModule.Create();
            module.CopyBlueprintRaw(ResourceType.Utc, "alask", "bad-name");

            var issues = new ResRefLengthRule().Validate(
                new ValidationContext(module.Workspace)).ToList();

            issues.Should().ContainSingle(issue =>
                issue.ResRef == "bad-name" &&
                issue.Message.Contains("only lowercase letters"));
        }
    }

    public class DanglingInstanceTemplateRuleTests
    {
        [Test]
        public void PlaceableInstance_MissingTemplate_FiresError()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("dangletest");

            var git = SyntheticGit.Create();
            git.Fields.Add("Placeable List", SyntheticGit.ListOf(
                SyntheticGit.Instance(("Tag", GffFieldType.CExoString, "Ghost Placeable"),
                    ("TemplateResRef", GffFieldType.ResRef, "missing_utp_bp"))));
            module.WriteGit("dangletest", git);

            var issues = new DanglingInstanceTemplateRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Warning &&
                i.ResRef == "missing_utp_bp" &&
                i.Message.Contains("Utp"));
        }

        [Test]
        public void WaypointInstance_MissingTemplate_FiresError()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("dangletest");

            var git = SyntheticGit.Create();
            git.Fields.Add("WaypointList", SyntheticGit.ListOf(
                SyntheticGit.Instance(("Tag", GffFieldType.CExoString, "MISSING_WP"),
                    ("TemplateResRef", GffFieldType.ResRef, "no_such_wp"))));
            module.WriteGit("dangletest", git);

            var issues = new DanglingInstanceTemplateRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().ContainSingle(i => i.Severity == ValidationSeverity.Warning && i.ResRef == "no_such_wp");
        }

        [Test]
        public void StoreInstance_UsesResRefFieldName_MissingTemplateFiresError()
        {
            // .utm blueprints (and their git instances) use "ResRef", not "TemplateResRef" -
            // verified against the corpus (bartender.utm.json / ar_scor_kacademy.git.json).
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("dangletest");

            var git = SyntheticGit.Create();
            git.Fields.Add("StoreList", SyntheticGit.ListOf(
                SyntheticGit.Instance(("Tag", GffFieldType.CExoString, "Ghost Store"),
                    ("ResRef", GffFieldType.ResRef, "missing_utm_bp"))));
            module.WriteGit("dangletest", git);

            var issues = new DanglingInstanceTemplateRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().ContainSingle(i => i.Severity == ValidationSeverity.Warning && i.ResRef == "missing_utm_bp");
        }

        [Test]
        public void QuestEncounterPlaceable_WithMissingTemplate_IsExempt()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("dangletest");

            var git = SyntheticGit.Create();
            git.Fields.Add("Placeable List", SyntheticGit.ListOf(
                    SyntheticGit.Instance(("Tag", GffFieldType.CExoString, "bf_kess_call"),
                        ("TemplateResRef", GffFieldType.ResRef, "bf_kess_call"),
                        ("OnUsed", GffFieldType.ResRef, "QUEST_ENC"))));
            module.WriteGit("dangletest", git);

            var issues = new DanglingInstanceTemplateRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().BeEmpty("quest encounter activators are world-instance-only by design and must not be flagged");
        }

        [Test]
        public void ValidTemplate_ThatExistsOnDisk_NoIssue()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("dangletest");
            module.CopyBlueprintRaw(ResourceType.Utp, "building_exit", "building_exit");

            var git = SyntheticGit.Create();
            git.Fields.Add("Placeable List", SyntheticGit.ListOf(
                SyntheticGit.Instance(("Tag", GffFieldType.CExoString, "Exit"),
                    ("TemplateResRef", GffFieldType.ResRef, "building_exit"))));
            module.WriteGit("dangletest", git);

            var issues = new DanglingInstanceTemplateRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().BeEmpty();
        }

        [Test]
        public void UnparseableGitFile_ProducesWarning_NotACrash()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("brokenarea");
            module.WriteRawGitFile("brokenarea", "{ this is not valid json");

            var rule = new DanglingInstanceTemplateRule();
            var act = () => rule.Validate(new ValidationContext(module.Workspace)).ToList();

            var issues = act.Should().NotThrow().Subject;
            issues.Should().ContainSingle(i => i.Severity == ValidationSeverity.Warning && i.ResRef == "brokenarea");
        }
    }

    public class VarTableEnumRuleTests
    {
        [Test]
        public void UtcBlueprint_ValidNpcGroup_NoIssue()
        {
            using var module = SyntheticModule.Create();
            var utc = UtcDocument.Parse(File.ReadAllBytes(RealUtcPath("alask")));
            utc.VarTable.SetInt("QUEST_NPC_GROUP_ID", 1);
            module.WriteBlueprint(ResourceType.Utc, "alask", utc);

            var gameCodeIndex = new FakeGameCodeIndex(validNpcGroups: new[] { 1 });
            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().BeEmpty();
        }

        [Test]
        public void UtcBlueprint_InvalidNpcGroup_FiresError()
        {
            using var module = SyntheticModule.Create();
            var utc = UtcDocument.Parse(File.ReadAllBytes(RealUtcPath("alask")));
            utc.VarTable.SetInt("QUEST_NPC_GROUP_ID", 999);
            module.WriteBlueprint(ResourceType.Utc, "alask", utc);

            var gameCodeIndex = new FakeGameCodeIndex(validNpcGroups: new[] { 1 });
            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Error && i.ResRef == "alask" && i.Message.Contains("QUEST_NPC_GROUP_ID"));
        }

        [Test]
        public void UtcBlueprint_NonIntegerNpcGroup_FiresError()
        {
            using var module = SyntheticModule.Create();
            var utc = UtcDocument.Parse(File.ReadAllBytes(RealUtcPath("alask")));
            utc.VarTable.SetString("QUEST_NPC_GROUP_ID", "not_a_number");
            module.WriteBlueprint(ResourceType.Utc, "alask", utc);

            var gameCodeIndex = new FakeGameCodeIndex(validNpcGroups: new[] { 1 });
            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Error && i.ResRef == "alask" && i.Message.Contains("non-integer"));
        }

        [Test]
        public void PlacedCreature_InvalidNpcGroup_FiresError()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("vartest");

            var creature = SyntheticGit.Instance(("Tag", GffFieldType.CExoString, "Bad Creature"),
                ("TemplateResRef", GffFieldType.ResRef, "some_npc"));
            new VarTable(creature).SetInt("QUEST_NPC_GROUP_ID", 999);

            var git = SyntheticGit.Create();
            git.Fields.Add("Creature List", SyntheticGit.ListOf(creature));
            module.WriteGit("vartest", git);

            var gameCodeIndex = new FakeGameCodeIndex(validNpcGroups: new[] { 1 });
            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Error && i.Message.Contains("placed creature") && i.Message.Contains("QUEST_NPC_GROUP_ID"));
        }

        [Test]
        public void Area_ValidSpawnTableId_NoIssue()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("vartest");

            var git = SyntheticGit.Create();
            git.VarTable.SetString("CREATURE_SPAWN_TABLE_ID", "GOOD_TABLE");
            module.WriteGit("vartest", git);

            var gameCodeIndex = new FakeGameCodeIndex(validSpawnTableIds: new[] { "GOOD_TABLE" });
            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().BeEmpty();
        }

        [Test]
        public void Area_InvalidSpawnTableId_FiresError()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("vartest");

            var git = SyntheticGit.Create();
            git.VarTable.SetString("CREATURE_SPAWN_TABLE_ID", "BOGUS_TABLE");
            module.WriteGit("vartest", git);

            var gameCodeIndex = new FakeGameCodeIndex(validSpawnTableIds: new[] { "GOOD_TABLE" });
            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Error && i.ResRef == "vartest" && i.Message.Contains("CREATURE_SPAWN_TABLE_ID"));
        }

        [Test]
        public void Area_PositiveSpawnCount_NoIssue()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("vartest");

            var git = SyntheticGit.Create();
            git.VarTable.SetInt("CREATURE_SPAWN_COUNT", 10);
            module.WriteGit("vartest", git);

            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().BeEmpty();
        }

        [Test]
        public void Area_NonPositiveSpawnCount_FiresError()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("vartest");

            var git = SyntheticGit.Create();
            git.VarTable.SetInt("CREATURE_SPAWN_COUNT", 0);
            module.WriteGit("vartest", git);

            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Error && i.ResRef == "vartest" && i.Message.Contains("CREATURE_SPAWN_COUNT"));
        }

        [Test]
        public void NoGameCodeIndex_SkipsNpcGroupAndSpawnTableChecksSilently_ButStillChecksSpawnCount()
        {
            using var module = SyntheticModule.Create();
            module.WriteAreaStub("vartest");

            var git = SyntheticGit.Create();
            git.VarTable.SetString("CREATURE_SPAWN_TABLE_ID", "BOGUS_TABLE");
            git.VarTable.SetInt("CREATURE_SPAWN_COUNT", -5);
            module.WriteGit("vartest", git);

            var issues = new VarTableEnumRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().NotContain(i => i.Message.Contains("CREATURE_SPAWN_TABLE_ID"));
            issues.Should().ContainSingle(i => i.Message.Contains("CREATURE_SPAWN_COUNT"));
        }

        private static string RealUtcPath(string resRef) =>
            Path.Combine(CorpusLocator.ModuleDirectory, "utc", resRef + ".utc.json");
    }

    public class QuestActivatorNotInPaletteRuleTests
    {
        [Test]
        public void UtpBlueprint_WithQuestEncOnUsed_FiresError()
        {
            using var module = SyntheticModule.Create();
            var utp = UtpDocument.Parse(File.ReadAllBytes(RealUtpPath("building_exit")));
            utp.OnUsed = "QuEsT_EnC";
            module.WriteBlueprint(ResourceType.Utp, "qtest_activator", utp);
            module.WritePalette("placeablepalcus", SyntheticPalette.Empty());

            var issues = new QuestActivatorNotInPaletteRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Error && i.ResRef == "qtest_activator" && i.Message.Contains("quest_enc"));
        }

        [Test]
        public void PaletteEntry_ReferencingQuestEncBlueprint_FiresError()
        {
            using var module = SyntheticModule.Create();
            var utp = UtpDocument.Parse(File.ReadAllBytes(RealUtpPath("building_exit")));
            utp.OnUsed = "QUEST_ENC";
            module.WriteBlueprint(ResourceType.Utp, "qtest_activator", utp);
            module.WritePalette("placeablepalcus", SyntheticPalette.Flat(("Quest Activator", "qtest_activator")));

            var issues = new QuestActivatorNotInPaletteRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().Contain(i =>
                i.Severity == ValidationSeverity.Error &&
                i.ResRef == "qtest_activator" &&
                i.Message.Contains("palette entry"));
        }

        [Test]
        public void OrdinaryPlaceable_NotInvolvingQuestEnc_NoIssue()
        {
            using var module = SyntheticModule.Create();
            module.CopyBlueprintRaw(ResourceType.Utp, "building_exit", "building_exit");
            module.WritePalette("placeablepalcus", SyntheticPalette.Flat(("Building Exit", "building_exit")));

            var issues = new QuestActivatorNotInPaletteRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().BeEmpty();
        }

        private static string RealUtpPath(string resRef) =>
            Path.Combine(CorpusLocator.ModuleDirectory, "utp", resRef + ".utp.json");
    }

    public class SpawnWaypointPaletteRuleTests
    {
        [Test]
        public void WaypointBlueprint_TagMatchesSpawnTable_MissingFromPalette_FiresError()
        {
            using var module = SyntheticModule.Create();
            var utw = UtwDocument.Parse(File.ReadAllBytes(RealUtwPath("wp_stuck")));
            utw.Tag = "MY_SPAWN_TABLE";
            module.WriteBlueprint(ResourceType.Utw, "spawn_wp_a", utw);
            module.WritePalette("waypointpalcus", SyntheticPalette.Empty());

            var gameCodeIndex = new FakeGameCodeIndex(validSpawnTableIds: new[] { "MY_SPAWN_TABLE" });
            var issues = new SpawnWaypointPaletteRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Error && i.ResRef == "spawn_wp_a" && i.Message.Contains("waypoint palette"));
        }

        [Test]
        public void WaypointBlueprint_TagMatchesSpawnTable_PresentInPalette_NoIssue()
        {
            using var module = SyntheticModule.Create();
            var utw = UtwDocument.Parse(File.ReadAllBytes(RealUtwPath("wp_stuck")));
            utw.Tag = "MY_SPAWN_TABLE";
            module.WriteBlueprint(ResourceType.Utw, "spawn_wp_a", utw);
            module.WritePalette("waypointpalcus", SyntheticPalette.Flat(("General Spawn", "spawn_wp_a")));

            var gameCodeIndex = new FakeGameCodeIndex(validSpawnTableIds: new[] { "MY_SPAWN_TABLE" });
            var issues = new SpawnWaypointPaletteRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().BeEmpty();
        }

        [Test]
        public void NoSourceScanAvailable_SkipsSilently()
        {
            using var module = SyntheticModule.Create();
            var utw = UtwDocument.Parse(File.ReadAllBytes(RealUtwPath("wp_stuck")));
            utw.Tag = "MY_SPAWN_TABLE";
            module.WriteBlueprint(ResourceType.Utw, "spawn_wp_a", utw);
            module.WritePalette("waypointpalcus", SyntheticPalette.Empty());

            var gameCodeIndex = new FakeGameCodeIndex(
                validSpawnTableIds: new[] { "MY_SPAWN_TABLE" }, isSourceScanAvailable: false);
            var issues = new SpawnWaypointPaletteRule().Validate(new ValidationContext(module.Workspace, gameCodeIndex)).ToList();

            issues.Should().BeEmpty();
        }

        private static string RealUtwPath(string resRef) =>
            Path.Combine(CorpusLocator.ModuleDirectory, "utw", resRef + ".utw.json");
    }

    public class SingletonWaypointDestinationRuleTests
    {
        [Test]
        public void DuplicateDeclaredTaxiDestinationFiresError()
        {
            using var module = SyntheticModule.Create();
            foreach (var area in new[] { "taxi_a", "taxi_b" })
            {
                module.WriteAreaStub(area);
                var git = SyntheticGit.Create();
                git.Fields.Add("WaypointList", SyntheticGit.ListOf(
                    SyntheticGit.Instance(
                        ("Tag", GffFieldType.CExoString, "TAXI_UNIQUE"),
                        ("TemplateResRef", GffFieldType.ResRef, "taxi_wp"))));
                module.WriteGit(area, git);
            }

            var gameCodeIndex = new FakeGameCodeIndex(
                taxiDestinations: new[]
                {
                    new TaxiDestinationInfo("TAXI_UNIQUE", "Unique Taxi", 1, 10)
                });

            var issues = new SingletonWaypointDestinationRule()
                .Validate(new ValidationContext(module.Workspace, gameCodeIndex))
                .ToList();

            issues.Should().ContainSingle(issue =>
                issue.Severity == ValidationSeverity.Error &&
                issue.Message.Contains("TAXI_UNIQUE") &&
                issue.Message.Contains("2 times"));
        }

        [Test]
        public void RepeatableTransitionDestinationTagIsNotRejected()
        {
            using var module = SyntheticModule.Create();
            foreach (var area in new[] { "transition_a", "transition_b" })
            {
                module.WriteAreaStub(area);
                var git = SyntheticGit.Create();
                git.Fields.Add("WaypointList", SyntheticGit.ListOf(
                    SyntheticGit.Instance(
                        ("Tag", GffFieldType.CExoString, "ORDINARY_TRANSITION"),
                        ("TemplateResRef", GffFieldType.ResRef, "transition_wp"))));
                module.WriteGit(area, git);
            }

            new SingletonWaypointDestinationRule()
                .Validate(new ValidationContext(module.Workspace, new FakeGameCodeIndex()))
                .Should().BeEmpty();
        }
    }

    public class PaletteOrphanRuleTests
    {
        [Test]
        public void PaletteEntry_MissingBlueprint_FiresError()
        {
            using var module = SyntheticModule.Create();
            module.WritePalette("creaturepalcus", SyntheticPalette.Flat(("Ghost NPC", "ghost_npc")));

            var issues = new PaletteOrphanRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Error && i.ResRef == "ghost_npc" && i.Message.Contains("Utc"));
        }

        [Test]
        public void DeletedPaletteEntry_MissingBlueprint_IsIgnored()
        {
            using var module = SyntheticModule.Create();
            module.WritePalette(
                "creaturepalcus",
                SyntheticPalette.DeletedFlat(("Retired NPC", "retired_npc")));

            var issues = new PaletteOrphanRule().Validate(new ValidationContext(module.Workspace)).ToList();

            issues.Should().NotContain(i => i.ResRef == "retired_npc");
        }

        [Test]
        public void PaletteEntry_WithExistingBlueprint_NoIssue()
        {
            using var module = SyntheticModule.Create();
            module.CopyBlueprintRaw(ResourceType.Utc, "alask", "alask");
            module.WritePalette("creaturepalcus", SyntheticPalette.Flat(("Alask", "alask")));

            var issues = new PaletteOrphanRule().Validate(new ValidationContext(module.Workspace)).ToList();

            // Other palette files (doorpalcus, itempalcus, ...) don't exist in this synthetic
            // module and correctly produce Warning issues (missing file) - only assert no Error
            // was raised for the populated "alask" entry.
            issues.Should().NotContain(i => i.Severity == ValidationSeverity.Error);
        }

        [Test]
        public void PaletteEntry_WithHakBlueprint_NoIssue()
        {
            using var module = SyntheticModule.Create();
            var hakDirectory = System.IO.Path.Combine(module.Path, "test_hak");
            Directory.CreateDirectory(hakDirectory);
            File.WriteAllText(System.IO.Path.Combine(hakDirectory, "hak_item.uti"), "fixture");
            module.WritePalette("itempalcus", SyntheticPalette.Flat(("Hak Item", "hak_item")));

            var index = new ResourceIndex(
                baseLayer: null,
                hakLayersInOrder: new[] { new ResourceIndex.HakLayer("test_hak", hakDirectory) });

            var issues = new PaletteOrphanRule()
                .Validate(new ValidationContext(module.Workspace, resourceIndex: index))
                .ToList();

            issues.Should().NotContain(i =>
                i.Severity == ValidationSeverity.Error && i.ResRef == "hak_item");
        }

        [Test]
        public void MissingPaletteFile_ProducesWarning_NotACrash()
        {
            using var module = SyntheticModule.Create();

            var rule = new PaletteOrphanRule();
            var act = () => rule.Validate(new ValidationContext(module.Workspace)).ToList();

            var issues = act.Should().NotThrow().Subject;
            issues.Should().Contain(i => i.Severity == ValidationSeverity.Warning);
        }
    }

    public class ModuleValidatorTests
    {
        [Test]
        public void Run_AggregatesIssuesAcrossRulesAndReportsTiming()
        {
            using var module = SyntheticModule.Create();
            var rules = new IValidationRule[] { new FixedIssueRule("RuleA", 1), new FixedIssueRule("RuleB", 2) };
            var validator = new ModuleValidator(rules);

            var result = validator.Run(new ValidationContext(module.Workspace));

            result.Issues.Should().HaveCount(3);
            result.Timings.Should().HaveCount(2);
            result.Timings.Select(t => t.RuleId).Should().BeEquivalentTo(new[] { "RuleA", "RuleB" });
            result.ErrorCount.Should().Be(3);
            result.WarningCount.Should().Be(0);
        }

        [Test]
        public void Run_RuleThatThrows_BecomesWarningInsteadOfCrashing()
        {
            using var module = SyntheticModule.Create();
            var validator = new ModuleValidator(new IValidationRule[] { new ThrowingRule() });

            var result = validator.Run(new ValidationContext(module.Workspace));

            result.Issues.Should().ContainSingle(i =>
                i.Severity == ValidationSeverity.Warning && i.RuleId == "Throwing" && i.Message.Contains("boom"));
            result.Timings.Should().ContainSingle(t => t.RuleId == "Throwing");
        }

        [Test]
        public async Task RunAsync_CompletesOnBackgroundThread()
        {
            using var module = SyntheticModule.Create();
            var validator = new ModuleValidator(new IValidationRule[] { new FixedIssueRule("RuleA", 1) });

            var result = await validator.RunAsync(new ValidationContext(module.Workspace));

            result.Issues.Should().HaveCount(1);
        }

        [Test]
        public void DefaultRules_IncludesEveryShippedRule()
        {
            ModuleValidator.DefaultRules().Select(r => r.RuleId).Should().BeEquivalentTo(new[]
            {
                // GffParse is the floor beneath the conventions: the others parse only the files they
                // need, so without it a resource that will not read at all was reported by nobody.
                "GffParse",
                "ResRefLength", "DanglingInstanceTemplate", "VarTableEnum",
                "QuestActivatorNotInPalette", "SpawnWaypointPalette", "SingletonWaypointDestination",
                "PaletteOrphan",
                "DanglingConversation", "UnreferencedConversation"
            });
        }

        private sealed class FixedIssueRule : IValidationRule
        {
            private readonly int _issueCount;

            public FixedIssueRule(string ruleId, int issueCount)
            {
                RuleId = ruleId;
                _issueCount = issueCount;
            }

            public string RuleId { get; }

            public IEnumerable<ValidationIssue> Validate(ValidationContext context) =>
                Enumerable.Range(0, _issueCount)
                    .Select(i => new ValidationIssue(ValidationSeverity.Error, RuleId, $"issue {i}", null, null));
        }

        private sealed class ThrowingRule : IValidationRule
        {
            public string RuleId => "Throwing";

            public IEnumerable<ValidationIssue> Validate(ValidationContext context) =>
                throw new InvalidOperationException("boom");
        }
    }

    /// <summary>
    /// [Explicit] - runs the full <see cref="ModuleValidator"/> over the real module corpus
    /// (thousands of files); slow, and intended for a human to run manually and review the
    /// summary, not for the normal test suite.
    /// </summary>
    public class ModuleValidatorFullCorpusTests
    {
        /// <summary>
        /// Locates the repository root from the test execution context. Deliberately independent
        /// from <see cref="CorpusLocator"/> and <see cref="GameCodeIndexTests"/>'s locator per
        /// this repo's per-file locator convention.
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

        [Test]
        [Explicit("Runs the full ModuleValidator over the real module corpus (thousands of files); slow - run manually to review convention drift.")]
        public void FullRealModuleCorpus_PrintsIssueSummary()
        {
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            var gameCodeIndex = new GameCodeIndex(GameServerSourceRoot);

            // Real hak layers + the base-game KEY/BIF layer (when an NWN install is present) so
            // hak/base-game-provided templates aren't reported as dangling.
            var repoRoot = Directory.GetParent(CorpusLocator.ModuleDirectory)!.FullName;
            var installPath = SWLOR.Toolset.Domain.GameData.Resources.NwnInstallLocator.Locate();
            var baseLayer = installPath == null
                ? null
                : SWLOR.Toolset.Domain.GameData.Resources.KeyBifCatalog.Load(Path.Combine(installPath, "data"));
            var resourceIndex = SWLOR.Toolset.Domain.GameData.Resources.ResourceIndex.FromHakBuilderConfig(
                Path.Combine(repoRoot, "Build", "hakbuilder.json"),
                Path.Combine(repoRoot, "SWLOR_Haks"),
                baseLayer);
            var context = new ValidationContext(workspace, gameCodeIndex, resourceIndex);

            var result = new ModuleValidator().Run(context);

            TestContext.Out.WriteLine($"Total issues: {result.Issues.Count} (Errors: {result.ErrorCount}, Warnings: {result.WarningCount})");
            TestContext.Out.WriteLine($"Total elapsed: {result.TotalElapsed}");
            TestContext.Out.WriteLine("Per-rule timing:");
            foreach (var timing in result.Timings)
                TestContext.Out.WriteLine($"  {timing.RuleId}: {timing.Elapsed}");

            TestContext.Out.WriteLine("Per-rule issue counts:");
            foreach (var group in result.Issues.GroupBy(i => i.RuleId))
                TestContext.Out.WriteLine($"  {group.Key}: {group.Count()} issues");
        }
    }

    /// <summary>
    /// Builds a minimal on-disk module directory (temp folder with just an "are" and "utc"
    /// subfolder, satisfying <see cref="ModuleWorkspace.LooksLikeModuleRoot"/>) that individual
    /// tests populate with exactly the files they need.
    /// </summary>
    internal sealed class SyntheticModule : IDisposable
    {
        public string Path { get; }
        public ModuleWorkspace Workspace { get; }

        private SyntheticModule(string path)
        {
            Path = path;
            Directory.CreateDirectory(System.IO.Path.Combine(path, "are"));
            Directory.CreateDirectory(System.IO.Path.Combine(path, "utc"));
            Workspace = new ModuleWorkspace(path);
        }

        public static SyntheticModule Create()
        {
            var root = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "SWLOR.Toolset.Tests", "validation_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new SyntheticModule(root);
        }

        /// <summary>Copies a real corpus blueprint file byte-for-byte under a (possibly different) resref.</summary>
        public void CopyBlueprintRaw(ResourceType type, string sourceResRef, string destResRef)
        {
            var extension = type.Extension();
            var destDir = EnsureFolder(extension);
            File.Copy(
                System.IO.Path.Combine(CorpusLocator.ModuleDirectory, extension, sourceResRef + "." + extension + ".json"),
                System.IO.Path.Combine(destDir, destResRef + "." + extension + ".json"));
        }

        /// <summary>Writes an already-parsed (and possibly mutated) blueprint document under the given resref.</summary>
        public void WriteBlueprint(ResourceType type, string destResRef, GffDocumentBase document)
        {
            var extension = type.Extension();
            var destDir = EnsureFolder(extension);
            File.WriteAllBytes(
                System.IO.Path.Combine(destDir, destResRef + "." + extension + ".json"),
                document.ToBytes());
        }

        /// <summary>
        /// Writes a minimal valid .are.json stub so the resref appears in
        /// <see cref="ModuleWorkspace.EnumerateAreaResRefs"/>. Content is otherwise irrelevant to
        /// the validation rules, which read the paired .git file directly by path.
        /// </summary>
        public void WriteAreaStub(string areaResRef)
        {
            var destDir = EnsureFolder("are");
            File.WriteAllText(
                System.IO.Path.Combine(destDir, areaResRef + ".are.json"),
                "{\n  \"__data_type\": \"ARE \"\n}\n");
        }

        public void WriteGit(string areaResRef, GitDocument document)
        {
            var destDir = EnsureFolder("git");
            File.WriteAllBytes(System.IO.Path.Combine(destDir, areaResRef + ".git.json"), document.ToBytes());
        }

        /// <summary>Writes arbitrary (possibly invalid) raw text as a .git file, to exercise parse-failure handling.</summary>
        public void WriteRawGitFile(string areaResRef, string rawContent)
        {
            var destDir = EnsureFolder("git");
            File.WriteAllText(System.IO.Path.Combine(destDir, areaResRef + ".git.json"), rawContent);
        }

        public void WritePalette(string paletteName, ItpDocument document)
        {
            var destDir = EnsureFolder("itp");
            File.WriteAllBytes(System.IO.Path.Combine(destDir, paletteName + ".itp.json"), document.ToBytes());
        }

        private string EnsureFolder(string name)
        {
            var dir = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup; leftover temp dirs from a killed test run are harmless.
            }
        }
    }

    /// <summary>
    /// Builds minimal <see cref="GitDocument"/> fixtures and instance structs from scratch via the
    /// public JsonGffStruct/JsonGffField/VarTable API, rather than copying (and de-noising) a real
    /// multi-thousand-line area .git file.
    /// </summary>
    internal static class SyntheticGit
    {
        public static GitDocument Create() => new(new JsonGffDocument("GIT ", new JsonGffStruct()));

        /// <summary>Builds one placed-object instance struct with the given scalar fields (Tag/TemplateResRef/OnUsed/etc.).</summary>
        public static JsonGffStruct Instance(params (string Name, GffFieldType Type, string Value)[] fields)
        {
            var target = new JsonGffStruct();
            foreach (var (name, type, value) in fields)
            {
                var field = JsonGffField.CreateScalar(type, Array.Empty<byte>());
                field.SetString(value);
                target.Add(name, field);
            }

            return target;
        }

        public static JsonGffField ListOf(params JsonGffStruct[] elements)
        {
            var field = JsonGffField.CreateList();
            field.Elements!.AddRange(elements);
            return field;
        }
    }

    /// <summary>Builds minimal <see cref="ItpDocument"/> palette fixtures (a flat "MAIN" list of leaf nodes, no categories).</summary>
    internal static class SyntheticPalette
    {
        public static ItpDocument Empty() => Flat();

        public static ItpDocument Flat(params (string Name, string ResRef)[] entries) =>
            Build(entries, deleted: false);

        public static ItpDocument DeletedFlat(params (string Name, string ResRef)[] entries) =>
            Build(entries, deleted: true);

        private static ItpDocument Build((string Name, string ResRef)[] entries, bool deleted)
        {
            var root = new JsonGffStruct();
            var mainList = JsonGffField.CreateList();

            foreach (var (name, resRef) in entries)
            {
                var leaf = new JsonGffStruct();
                var nameField = JsonGffField.CreateScalar(GffFieldType.CExoString, Array.Empty<byte>());
                nameField.SetString(name);
                leaf.Add("NAME", nameField);

                var resRefField = JsonGffField.CreateScalar(GffFieldType.ResRef, Array.Empty<byte>());
                resRefField.SetString(resRef);
                leaf.Add("RESREF", resRefField);

                if (deleted)
                {
                    var deleteField = JsonGffField.CreateScalar(GffFieldType.Byte, Array.Empty<byte>());
                    deleteField.SetInteger(1);
                    leaf.Add("DELETE_ME", deleteField);
                }

                mainList.Elements!.Add(leaf);
            }

            root.Add("MAIN", mainList);
            return new ItpDocument(new JsonGffDocument("ITP ", root));
        }
    }

    /// <summary>A deterministic <see cref="IGameCodeIndex"/> stand-in for tests, avoiding a dependency on the real SWLOR.Game.Server source scan.</summary>
    internal sealed class FakeGameCodeIndex : IGameCodeIndex
    {
        private readonly HashSet<int> _validNpcGroups;
        private readonly HashSet<string> _validSpawnTableIds;
        private readonly Dictionary<string, QuestDefinitionInfo> _quests;

        public FakeGameCodeIndex(
            IEnumerable<int>? validNpcGroups = null,
            IEnumerable<string>? validSpawnTableIds = null,
            bool isSourceScanAvailable = true,
            IEnumerable<QuestDefinitionInfo>? quests = null,
            IReadOnlyDictionary<int, string>? keyItems = null,
            IReadOnlyDictionary<int, string>? factions = null,
            IReadOnlyDictionary<int, string>? skills = null,
            IEnumerable<string>? fishingSpawnTableIds = null,
            IReadOnlyList<WaypointDestinationInfo>? planetLandingWaypoints = null,
            IReadOnlyList<WaypointDestinationInfo>? orbitWaypoints = null,
            IReadOnlyList<TaxiDestinationInfo>? taxiDestinations = null,
            IEnumerable<string>? deathRespawnWaypointTags = null,
            IEnumerable<string>? rebuildWaypointTags = null)
        {
            _validNpcGroups = new HashSet<int>(validNpcGroups ?? Array.Empty<int>());
            _validSpawnTableIds = new HashSet<string>(validSpawnTableIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            _quests = (quests ?? Array.Empty<QuestDefinitionInfo>())
                .ToDictionary(quest => quest.Id, quest => quest, StringComparer.Ordinal);
            IsSourceScanAvailable = isSourceScanAvailable;
            KeyItems = keyItems ?? new Dictionary<int, string>();
            Factions = factions ?? new Dictionary<int, string>();
            Skills = skills ?? new Dictionary<int, string>();
            SkillEnumNames = Skills;
            SpawnTables = _validSpawnTableIds
                .Select(id => new SpawnTableInfo(id, id))
                .ToList();
            FishingSpawnTableIds = (fishingSpawnTableIds ?? Array.Empty<string>()).ToList();
            FishingSpawnTables = FishingSpawnTableIds
                .Select(id => new SpawnTableInfo(id, id))
                .ToList();
            PlanetLandingWaypoints = planetLandingWaypoints ?? Array.Empty<WaypointDestinationInfo>();
            OrbitWaypoints = orbitWaypoints ?? Array.Empty<WaypointDestinationInfo>();
            TaxiDestinations = taxiDestinations ?? Array.Empty<TaxiDestinationInfo>();
            DeathRespawnWaypointTags =
                (deathRespawnWaypointTags ?? Array.Empty<string>()).ToList();
            RebuildWaypointTags = (rebuildWaypointTags ?? Array.Empty<string>()).ToList();
        }

        public bool IsSourceScanAvailable { get; }

        public IReadOnlyDictionary<int, string> NpcGroups =>
            _validNpcGroups.ToDictionary(value => value, value => $"Group{value}");

        public IReadOnlyDictionary<int, string> KeyItems { get; }

        public IReadOnlyDictionary<int, string> Factions { get; }

        public IReadOnlyDictionary<int, string> Skills { get; }

        public IReadOnlyDictionary<int, string> SkillEnumNames { get; }

        public IReadOnlyCollection<string> QuestIds => _quests.Keys;

        public IReadOnlyDictionary<string, QuestDefinitionInfo> Quests => _quests;

        public IReadOnlyCollection<string> SpawnTableIds => _validSpawnTableIds;

        public IReadOnlyList<SpawnTableInfo> SpawnTables { get; }

        public IReadOnlyCollection<string> FishingSpawnTableIds { get; }

        public IReadOnlyList<SpawnTableInfo> FishingSpawnTables { get; }

        public IReadOnlyList<WaypointDestinationInfo> PlanetLandingWaypoints { get; }

        public IReadOnlyList<WaypointDestinationInfo> OrbitWaypoints { get; }

        public IReadOnlyList<TaxiDestinationInfo> TaxiDestinations { get; }

        public IReadOnlyCollection<string> DeathRespawnWaypointTags { get; }

        public IReadOnlyCollection<string> RebuildWaypointTags { get; }

        public IReadOnlyCollection<string> LootTableIds => Array.Empty<string>();

        public IReadOnlyCollection<string> DialogNames => Array.Empty<string>();

        public IReadOnlyDictionary<int, string> SkillTypes => new Dictionary<int, string>();

        public IReadOnlyDictionary<int, string> MarketRegions => new Dictionary<int, string>();

        public IReadOnlyDictionary<int, string> VisualEffects => new Dictionary<int, string>();

        public IReadOnlyDictionary<int, VisualEffectReferenceInfo> VisualEffectReferences =>
            new Dictionary<int, VisualEffectReferenceInfo>();

        public bool IsValidNpcGroup(int npcGroupValue) => _validNpcGroups.Contains(npcGroupValue);

        public bool IsValidQuestId(string questId) => _quests.ContainsKey(questId);

        public QuestDefinitionInfo? FindQuest(string questId) =>
            _quests.TryGetValue(questId, out var quest) ? quest : null;

        public bool IsValidSpawnTableId(string spawnTableId) => _validSpawnTableIds.Contains(spawnTableId);

        public bool IsValidLootTableId(string lootTableId) => false;

        public bool IsValidDialogName(string dialogName) => false;
    }
}
