using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Placeables;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Covers the behavior layer: that it recognises what the module already stores, that switching
    /// writes and clears exactly what it claims to, and that none of it disturbs a file's bytes.
    /// </summary>
    public class PlaceableBehaviorTests
    {
        private static string UtpDirectory => Path.Combine(CorpusLocator.ModuleDirectory, "utp");

        [Test]
        public void Catalog_HasNoDuplicateIdsAndDeclaresBothSentinels()
        {
            var behaviors = PlaceableBehaviorCatalog.Behaviors;

            behaviors.Select(behavior => behavior.Id).Should().OnlyHaveUniqueItems();
            PlaceableBehaviorCatalog.None.IsSentinel.Should().BeTrue();
            PlaceableBehaviorCatalog.Custom.AllowsRawEditing.Should().BeTrue();
            behaviors.Where(behavior => !behavior.IsSentinel)
                .Should().OnlyContain(behavior => behavior.Scripts.Count > 0 || behavior.Fields.Count > 0,
                    "a named behavior has to be recognisable from something stored");
        }

        [Test]
        public void Catalog_EveryRequiredFieldIsNamedByRealGameCode()
        {
            // Guards the half of a behavior that cannot be checked against the module: a variable
            // the game code no longer reads would leave the field silently inert.
            var sourceRoot = FindGameServerSource();
            if (sourceRoot == null)
                Assert.Ignore("SWLOR.Game.Server source not found from the test context.");

            var source = string.Join('\n', Directory
                .EnumerateFiles(sourceRoot!, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

            var missing = PlaceableBehaviorCatalog.Behaviors
                .SelectMany(behavior => behavior.Fields.Where(field => field.IsRequired)
                    .Select(field => (behavior.Name, field.VariableName)))
                .Where(entry => !source.Contains($"\"{entry.VariableName}\"", StringComparison.Ordinal))
                .ToList();

            missing.Should().BeEmpty("every required behavior variable must be read by the game code");
        }

        [Test]
        public void Detect_ReadsScavengePointFromItsScriptsAndVariables()
        {
            var root = LoadFirstPlaceableWith("SCAVENGE_POINT_LOOT_TABLE_NAME");
            if (root == null)
                Assert.Ignore("No scavenge point blueprint in the corpus.");

            PlaceableBehaviorDetector.Detect(root!).Id.Should().Be("scavenge_point");
        }

        [Test]
        public void Detect_RecognisesABaseGameChairAsAChair()
        {
            // 2,181 instances still run zep_use_chair and 1,550 run x0_o2_use_chair. Neither is the
            // script SWLOR writes, and both are unmistakably chairs.
            var root = BuildPlaceable(("OnUsed", "zep_use_chair"));

            PlaceableBehaviorDetector.Detect(root).Id.Should().Be("chair");
        }

        [Test]
        public void Detect_PlainDecorIsNoneAndUnknownWiringIsCustom()
        {
            PlaceableBehaviorDetector.Detect(BuildPlaceable()).Id
                .Should().Be(PlaceableBehaviorCatalog.NoneId);

            PlaceableBehaviorDetector.Detect(BuildPlaceable(("OnUsed", "some_unknown_script"))).Id
                .Should().Be(PlaceableBehaviorCatalog.CustomId);
        }

        [Test]
        public void Detect_ToleratesAnExtraScriptTheBehaviorDoesNotOwn()
        {
            // A quarter of the module's scavenge points also run plc_death. Calling those Custom
            // would bury their loot table in a raw grid for no gain.
            var root = BuildPlaceable(
                ("OnOpen", "scav_opened"),
                ("OnClosed", "scav_closed"),
                ("OnInvDisturbed", "scav_disturbed"),
                ("OnDeath", "plc_death"));

            PlaceableBehaviorDetector.Detect(root).Id.Should().Be("scavenge_point");
        }

        [Test]
        public void Apply_WritesScriptsAndFlagsAndClearsWhatTheOldBehaviorOwned()
        {
            var document = BuildDocument();
            var scavenge = PlaceableBehaviorCatalog.FindById("scavenge_point")!;
            var teleporter = PlaceableBehaviorCatalog.FindById("teleporter")!;

            PlaceableBehaviorApplier.Apply(document.Root, PlaceableBehaviorCatalog.None, scavenge);

            document.Root.GetOrNull("OnOpen")!.GetString().Should().Be("scav_opened");
            document.Root.GetOrNull("HasInventory")!.GetInteger().Should().Be(1);
            document.Root.GetOrNull("Useable")!.GetInteger().Should().Be(1);

            new VarTable(document.Root).SetString("SCAVENGE_POINT_LOOT_TABLE_NAME", "SOME_TABLE");

            PlaceableBehaviorApplier.Apply(document.Root, scavenge, teleporter);

            document.Root.GetOrNull("OnOpen")!.GetString().Should().BeEmpty("the old behavior owned that slot");
            document.Root.GetOrNull("OnUsed")!.GetString().Should().Be("teleport");
            new VarTable(document.Root).Any(entry => entry.Name == "SCAVENGE_POINT_LOOT_TABLE_NAME")
                .Should().BeFalse("switching clears the variables only the old behavior used");
        }

        [Test]
        public void Apply_LeavesAHandEditedSlotAlone()
        {
            var document = BuildDocument(("OnOpen", "my_own_script"));
            var scavenge = PlaceableBehaviorCatalog.FindById("scavenge_point")!;

            PlaceableBehaviorApplier.Apply(document.Root, scavenge, PlaceableBehaviorCatalog.None);

            document.Root.GetOrNull("OnOpen")!.GetString().Should().Be("my_own_script",
                "a slot holding something the behavior did not write belongs to whoever wrote it");
        }

        [Test]
        public void ValuesLostBySwitching_NamesOnlyVariablesThatActuallyHoldSomething()
        {
            var document = BuildDocument();
            var scavenge = PlaceableBehaviorCatalog.FindById("scavenge_point")!;

            new VarTable(document.Root).SetString("SCAVENGE_POINT_LOOT_TABLE_NAME", "SOME_TABLE");
            new VarTable(document.Root).SetInt("SCAVENGE_POINT_LEVEL", 0);

            PlaceableBehaviorApplier
                .ValuesLostBySwitching(document.Root, scavenge, PlaceableBehaviorCatalog.None)
                .Should().BeEquivalentTo(new[] { "SCAVENGE_POINT_LOOT_TABLE_NAME" });
        }

        [Test]
        public void UnmanagedVariables_ReportsWhatTheBehaviorDoesNotOwn()
        {
            var document = BuildDocument();
            var teleporter = PlaceableBehaviorCatalog.FindById("teleporter")!;

            new VarTable(document.Root).SetString("DESTINATION", "SOMEWHERE");
            new VarTable(document.Root).SetString("SCRIPT_1", "Placeable.WarpDevice.OnUsed");

            PlaceableBehaviorDetector.UnmanagedVariables(document.Root, teleporter)
                .Should().BeEquivalentTo(new[] { "SCRIPT_1" });
        }

        [Test]
        public void Detect_RunsOverTheWholeCorpusWithoutThrowing()
        {
            var counts = new Dictionary<string, int>();

            foreach (var path in Directory.EnumerateFiles(UtpDirectory, "*.utp.json"))
            {
                var behavior = PlaceableBehaviorDetector.Detect(JsonGffDocument.Load(path).Root);
                counts[behavior.Id] = counts.TryGetValue(behavior.Id, out var existing) ? existing + 1 : 1;
            }

            // Counted from the corpus rather than pinned to a number: the module gains and loses
            // blueprints, and a stale constant would fail for a reason that is not about detection.
            var blueprintCount = Directory.EnumerateFiles(UtpDirectory, "*.utp.json").Count();
            counts.Values.Sum().Should().Be(blueprintCount, "every blueprint gets exactly one behavior");

            // 94% of blueprints set no script at all, so decor has to dominate - a detector that
            // classified most of the module as something else would be finding patterns that are
            // not there.
            counts[PlaceableBehaviorCatalog.NoneId].Should().BeGreaterThan(counts.Values.Sum() * 3 / 4);

            counts.Should().ContainKey("scavenge_point");
            counts.Should().ContainKey("chair");
        }

        [Test]
        public void ApplyThenUndo_RestoresACorpusFileByteForByte()
        {
            // The permanent gate for the whole feature: a behavior is a view, so applying one and
            // taking it back must leave the file exactly as it was found.
            var path = Directory.EnumerateFiles(UtpDirectory, "*.utp.json").First();
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            using var session = new DocumentSession(path, document);

            var detected = PlaceableBehaviorDetector.Detect(document.Root);
            var teleporter = PlaceableBehaviorCatalog.FindById("teleporter")!;

            using (session.Begin("switch behavior"))
                PlaceableBehaviorApplier.Apply(document.Root, detected, teleporter);

            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse(
                "the switch has to have changed something");

            session.UndoStack.Undo();

            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing a behavior switch must restore the original bytes exactly");
        }

        private static JsonGffStruct? LoadFirstPlaceableWith(string variableName)
        {
            foreach (var path in Directory.EnumerateFiles(UtpDirectory, "*.utp.json"))
            {
                var root = JsonGffDocument.Load(path).Root;
                if (new VarTable(root).Any(entry => entry.Name == variableName))
                    return root;
            }

            return null;
        }

        /// <summary>A minimal placeable document with the given script slots set.</summary>
        private static JsonGffDocument BuildDocument(params (string Slot, string Script)[] scripts)
        {
            var json = "{\n  \"__data_type\": \"UTP \",\n  \"TemplateResRef\": { \"type\": \"resref\", \"value\": \"test\" }" +
                       string.Concat(scripts.Select(entry =>
                           $",\n  \"{entry.Slot}\": {{ \"type\": \"resref\", \"value\": \"{entry.Script}\" }}")) +
                       "\n}\n";

            return JsonGffDocument.Parse(System.Text.Encoding.UTF8.GetBytes(json));
        }

        private static JsonGffStruct BuildPlaceable(params (string Slot, string Script)[] scripts) =>
            BuildDocument(scripts).Root;

        private static string? FindGameServerSource()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "SWLOR.Game.Server");
                if (Directory.Exists(Path.Combine(candidate, "Feature")))
                    return candidate;

                current = current.Parent;
            }

            return null;
        }
    }
}
