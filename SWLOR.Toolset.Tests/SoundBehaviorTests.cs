using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Sounds;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class SoundBehaviorTests
    {
        private static readonly Dictionary<string, Dictionary<string, long>> ExpectedIntegerWrites = new()
        {
            [SoundBehaviorCatalog.PointLoopId] = new()
            {
                ["Positional"] = 1, ["RandomPosition"] = 0, ["Looping"] = 1,
                ["Continuous"] = 0, ["Interval"] = 0, ["IntervalVrtn"] = 0, ["Priority"] = 3
            },
            [SoundBehaviorCatalog.PointAmbienceId] = new()
            {
                ["Positional"] = 1, ["RandomPosition"] = 0, ["Continuous"] = 1,
                ["Looping"] = 0, ["Priority"] = 20
            },
            [SoundBehaviorCatalog.AreaLoopId] = new()
            {
                ["Positional"] = 0, ["Looping"] = 1, ["Continuous"] = 0,
                ["Interval"] = 0, ["IntervalVrtn"] = 0, ["Priority"] = 2
            },
            [SoundBehaviorCatalog.AreaAmbienceId] = new()
            {
                ["Positional"] = 0, ["Continuous"] = 1, ["Looping"] = 0, ["Priority"] = 19
            },
            [SoundBehaviorCatalog.ScatteredAmbienceId] = new()
            {
                ["Positional"] = 1, ["RandomPosition"] = 1, ["Continuous"] = 1,
                ["Looping"] = 0, ["Random"] = 1, ["Priority"] = 20
            },
            [SoundBehaviorCatalog.CustomId] = new()
        };

        private static readonly Dictionary<string, Dictionary<string, double>> ExpectedFloatWrites = new()
        {
            [SoundBehaviorCatalog.PointLoopId] = new() { ["PitchVariation"] = 0 },
            [SoundBehaviorCatalog.PointAmbienceId] = new(),
            [SoundBehaviorCatalog.AreaLoopId] = new() { ["PitchVariation"] = 0 },
            [SoundBehaviorCatalog.AreaAmbienceId] = new(),
            [SoundBehaviorCatalog.ScatteredAmbienceId] = new(),
            [SoundBehaviorCatalog.CustomId] = new()
        };

        [Test]
        public void EveryBehaviorWritesTheFieldsItsEngineShapeRequires()
        {
            SoundBehaviorCatalog.All.Select(behavior => behavior.Id)
                .Should().BeEquivalentTo(ExpectedIntegerWrites.Keys);

            foreach (var behavior in SoundBehaviorCatalog.All)
            {
                behavior.Manages.Select(value => value.Name).Should().BeEquivalentTo(
                    ExpectedIntegerWrites[behavior.Id].Keys.Concat(ExpectedFloatWrites[behavior.Id].Keys),
                    $"{behavior.DisplayName} must not silently own an unstated field");

                var store = new SoundValueStore(NewSound());
                foreach (var value in behavior.Manages)
                    store.Apply(value);

                foreach (var (field, expected) in ExpectedIntegerWrites[behavior.Id])
                {
                    store.GetInteger(BehaviorFieldStorage.Field, field)
                        .Should().Be(expected, $"{behavior.DisplayName} owns {field}");
                }

                foreach (var (field, expected) in ExpectedFloatWrites[behavior.Id])
                {
                    store.GetFloat(BehaviorFieldStorage.Field, field)
                        .Should().BeApproximately(expected, 1e-4, $"{behavior.DisplayName} owns {field}");
                }
            }
        }

        [TestCase(0, 1, 0, 0, SoundBehaviorCatalog.AreaLoopId)]
        [TestCase(0, 0, 1, 0, SoundBehaviorCatalog.AreaAmbienceId)]
        [TestCase(1, 0, 1, 1, SoundBehaviorCatalog.ScatteredAmbienceId)]
        [TestCase(1, 1, 0, 0, SoundBehaviorCatalog.PointLoopId)]
        [TestCase(1, 0, 1, 0, SoundBehaviorCatalog.PointAmbienceId)]
        [TestCase(1, 0, 0, 0, SoundBehaviorCatalog.CustomId)]
        public void ClassifierRecognisesTheRawEngineShapes(
            int positional,
            int looping,
            int continuous,
            int randomPosition,
            string expected)
        {
            var store = new SoundValueStore(NewSound());
            SetByte(store, "Positional", positional);
            SetByte(store, "Looping", looping);
            SetByte(store, "Continuous", continuous);
            SetByte(store, "RandomPosition", randomPosition);

            SoundBehaviorCatalog.Classify(store.Sound).Id.Should().Be(expected);
        }

        [Test]
        public void EveryPlacedAndBlueprintSoundClassifiesWithoutThrowing()
        {
            var sounds = CorpusSounds().ToList();
            sounds.Should().NotBeEmpty("the behavior catalog was derived from the module corpus");

            foreach (var sound in sounds)
                SoundBehaviorCatalog.Classify(sound).Should().NotBeNull();
        }

        [Test]
        public void SwappingToALoopKeepsTheFirstSoundAndDropsTheRestAsOneUndoStep()
        {
            var path = Path.Combine(Path.GetTempPath(), $"swlor-sound-{Guid.NewGuid():N}.uts.json");
            var original = NewSoundDocument();
            var initial = new SoundValueStore(original.Root);
            SetByte(initial, "Positional", 1);
            SetByte(initial, "Continuous", 1);
            SetByte(initial, "Looping", 0);
            initial.SetInteger(BehaviorFieldStorage.Field, "Interval", GffFieldType.Dword, 12);
            initial.SetInteger(BehaviorFieldStorage.Field, "IntervalVrtn", GffFieldType.Dword, 4);
            initial.AddSound("first_sound");
            initial.AddSound("second_sound");
            initial.AddSound("third_sound");
            File.WriteAllBytes(path, original.ToBytes());

            try
            {
                using var session = DocumentSession.Open(path);
                var editor = new SoundEditorViewModel(
                    session.Document.Root,
                    "test_sound",
                    isInstance: false,
                    (description, mutation) =>
                    {
                        session.Execute(description, mutation);
                        return true;
                    });

                editor.ChooseBehavior(SoundBehaviorCatalog.Get(SoundBehaviorCatalog.PointLoopId));

                var changed = new SoundValueStore(session.Document.Root);
                changed.GetSounds().Should().Equal("first_sound");
                changed.GetInteger(BehaviorFieldStorage.Field, "Interval").Should().Be(0);
                changed.GetInteger(BehaviorFieldStorage.Field, "IntervalVrtn").Should().Be(0);
                editor.BehaviorChangeNotice.Should().Contain("second_sound").And.Contain("third_sound");

                session.Undo();
                var restored = new SoundValueStore(session.Document.Root);
                restored.GetSounds().Should().Equal("first_sound", "second_sound", "third_sound");
                restored.GetInteger(BehaviorFieldStorage.Field, "Interval").Should().Be(12);
                restored.GetInteger(BehaviorFieldStorage.Field, "IntervalVrtn").Should().Be(4);
                session.UndoStack.CanUndo.Should().BeFalse("the whole behavior swap is one transaction");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void SwappingBehaviorLeavesNoPreviousOnlyValuesBehind()
        {
            var store = new SoundValueStore(NewSound());
            SetByte(store, "Positional", 1);
            SetByte(store, "RandomPosition", 1);
            SetByte(store, "Continuous", 1);
            SetByte(store, "Looping", 0);
            store.SetFloat(BehaviorFieldStorage.Field, "RandomRangeX", 22);
            store.SetFloat(BehaviorFieldStorage.Field, "RandomRangeY", 13);
            store.SetFloat(BehaviorFieldStorage.Field, "MaxDistance", 40);
            store.SetFloat(BehaviorFieldStorage.Field, "Elevation", 7);
            store.AddSound("wind_one");
            store.AddSound("wind_two");

            var editor = new SoundEditorViewModel(
                store.Sound,
                "area_resref",
                isInstance: true,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            editor.ChooseBehavior(SoundBehaviorCatalog.Get(SoundBehaviorCatalog.AreaLoopId));

            store.GetFloat(BehaviorFieldStorage.Field, "RandomRangeX").Should().Be(0);
            store.GetFloat(BehaviorFieldStorage.Field, "RandomRangeY").Should().Be(0);
            store.GetFloat(BehaviorFieldStorage.Field, "MaxDistance").Should().Be(0);
            store.GetFloat(BehaviorFieldStorage.Field, "Elevation").Should().Be(0);
            store.GetInteger(BehaviorFieldStorage.Field, "RandomPosition").Should().Be(0);
            store.GetInteger(BehaviorFieldStorage.Field, "Priority").Should().Be(2);
        }

        [Test]
        public void LeavingCustomClearsItsAdvancedRawValues()
        {
            var store = new SoundValueStore(NewSound());
            SetByte(store, "Positional", 1);
            SetByte(store, "RandomPosition", 1);
            SetByte(store, "Continuous", 0);
            SetByte(store, "Looping", 0);
            store.SetInteger(BehaviorFieldStorage.Field, "Interval", GffFieldType.Dword, 99);
            store.AddSound("custom_sound");

            var editor = new SoundEditorViewModel(
                store.Sound,
                "test_sound",
                isInstance: false,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            editor.Behavior.Id.Should().Be(SoundBehaviorCatalog.CustomId);

            editor.ChooseBehavior(SoundBehaviorCatalog.Get(SoundBehaviorCatalog.AreaAmbienceId));

            store.GetInteger(BehaviorFieldStorage.Field, "RandomPosition").Should().Be(0);
            store.GetInteger(BehaviorFieldStorage.Field, "Interval").Should().Be(0);
        }

        [Test]
        public void HoursIsPreservedAndIsNeverEditable()
        {
            var store = new SoundValueStore(NewSound());
            SetByte(store, "Positional", 1);
            SetByte(store, "Continuous", 1);
            SetByte(store, "Looping", 0);
            store.SetInteger(BehaviorFieldStorage.Field, "Hours", GffFieldType.Dword, 0x1234);

            var editor = new SoundEditorViewModel(
                store.Sound,
                "test_sound",
                isInstance: false,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            editor.ChooseBehavior(SoundBehaviorCatalog.Get(SoundBehaviorCatalog.AreaLoopId));

            store.GetInteger(BehaviorFieldStorage.Field, "Hours").Should().Be(0x1234);
            SoundEditorLayout.Basic.Concat(SoundEditorLayout.RawPlaybackFields)
                .Concat(SoundBehaviorCatalog.All.SelectMany(behavior => behavior.Fields))
                .Should().NotContain(field => field.Name == "Hours");
        }

        [Test]
        public void VariablesExistOnlyUnderCustom()
        {
            SoundBehaviorCatalog.All.Where(behavior => behavior.AllowsVariables)
                .Should().ContainSingle()
                .Which.Id.Should().Be(SoundBehaviorCatalog.CustomId);
        }

        [Test]
        public void TimeChoicesAreOnlyDayNightAndBoth()
        {
            var times = SoundBehaviorCatalog.Get(SoundBehaviorCatalog.PointAmbienceId)
                .Fields.Single(field => field.Name == "Times");

            times.Choices.Select(choice => (choice.Value, choice.Display)).Should().Equal(
                (1L, "Day"),
                (2L, "Night"),
                (3L, "Both"));
            times.Choices.Should().NotContain(choice =>
                choice.Display.Contains("Specific", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void TagIsAPlainUnvalidatedBasicTextField()
        {
            var tag = SoundEditorLayout.Basic.Single(field => field.Name == "Tag");

            tag.Kind.Should().Be(BehaviorFieldKind.Text);
            tag.MaxLength.Should().Be(0);
            tag.ChoicesKey.Should().BeNull();
        }

        [Test]
        public void CommentIsDroppedAndTemplateResRefIsNeverBehaviorOwned()
        {
            SoundEditorLayout.Basic.Concat(SoundEditorLayout.RawPlaybackFields)
                .Concat(SoundBehaviorCatalog.All.SelectMany(behavior => behavior.Fields))
                .Should().NotContain(field => field.Name == "Comment");

            SoundBehaviorCatalog.All.Should().OnlyContain(behavior =>
                behavior.Fields.All(field => field.Name != "TemplateResRef")
                && behavior.Manages.All(value => value.Name != "TemplateResRef"));
        }

        [Test]
        public void LoopBehaviorsOfferOneSoundAndNoTimingOrPlayOrderRows()
        {
            foreach (var behavior in SoundBehaviorCatalog.All.Where(behavior => behavior.IsLoop))
            {
                behavior.Fields.Single(field => field.Name == SoundValueStore.SoundsField)
                    .MaxItems.Should().Be(1);
                behavior.Fields.Should().NotContain(field =>
                    field.Name == "Random" || field.Name == "Interval" || field.Name == "IntervalVrtn");
            }
        }

        [Test]
        public void HeaderNamesTheBehaviorKindAndOwner()
        {
            var store = new SoundValueStore(NewSound());
            SetByte(store, "Positional", 1);
            SetByte(store, "Looping", 1);
            SetByte(store, "Continuous", 0);

            var editor = new SoundEditorViewModel(
                store.Sound, "tat_mos_eisley", isInstance: true, (_, mutation) =>
                {
                    mutation();
                    return true;
                });

            editor.HeaderName.Should().Be("Point Loop");
            editor.HeaderKind.Should().Be("instance");
            editor.HeaderOwner.Should().Be("tat_mos_eisley");
        }

        private static void SetByte(SoundValueStore store, string name, long value) =>
            store.SetInteger(BehaviorFieldStorage.Field, name, GffFieldType.Byte, value);

        private static JsonGffStruct NewSound() => NewSoundDocument().Root;

        private static JsonGffDocument NewSoundDocument()
        {
            return JsonGffDocument.Parse(
                System.Text.Encoding.UTF8.GetBytes(
                    "{\n  \"__data_type\": \"UTS \",\n  \"Tag\": { \"type\": \"cexostring\", \"value\": \"sound\" }\n}\n"));
        }

        private static IEnumerable<JsonGffStruct> CorpusSounds()
        {
            var gitDirectory = Path.Combine(CorpusLocator.ModuleDirectory, "git");
            foreach (var path in Directory.EnumerateFiles(gitDirectory, "*.git.json"))
            {
                foreach (var sound in new GitDocument(JsonGffDocument.Load(path)).Sounds)
                    yield return sound;
            }

            var utsDirectory = Path.Combine(CorpusLocator.ModuleDirectory, "uts");
            foreach (var path in Directory.EnumerateFiles(utsDirectory, "*.uts.json"))
                yield return JsonGffDocument.Load(path).Root;
        }
    }
}
