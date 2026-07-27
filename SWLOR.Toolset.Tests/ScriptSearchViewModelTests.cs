using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Throws on every read, so a test can prove a scan failure is caught and surfaced.</summary>
    file sealed class ThrowingSnapshot : IReadOnlyDictionary<string, string>
    {
        private static InvalidOperationException Boom() => new("snapshot exploded");

        public string this[string key] => throw Boom();
        public IEnumerable<string> Keys => throw Boom();
        public IEnumerable<string> Values => throw Boom();
        public int Count => throw Boom();
        public bool ContainsKey(string key) => throw Boom();
        public bool TryGetValue(string key, out string value) => throw Boom();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw Boom();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// The Find in Scripts panel's debounce/cancel/background-scan behaviour.
    /// </summary>
    /// <remarks>
    /// Searching used to enumerate, read, and lex every module script inline on the UI thread for
    /// every keystroke - about 1.1 MiB across 87 files re-read per character typed, which could stall
    /// the whole editor. What these check is that a keystroke no longer reads anything itself, that
    /// only the last query typed produces results, and that a settled search still finds the right
    /// matches once it actually runs.
    /// </remarks>
    [TestFixture]
    public class ScriptSearchViewModelTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_scriptsearch_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        /// <summary>
        /// The scan is the expensive thing, so this measures whether it happened rather than how long
        /// anything took: a synchronous scan would have to open the file, and an unreadable file on a
        /// background thread cannot fail the assertion.
        /// </summary>
        [Test]
        public void TypingIntoTheQueryDoesNotReadTheScriptCorpus()
        {
            var path = Path.Combine(_root, "hello.nss");
            File.WriteAllText(path, "void main() { int Veldite = 1; }");

            var vm = new ScriptSearchViewModel(_root, (_, _) => { });

            // Held open for writing with no sharing: anything that reads it on this thread throws.
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var typing = () =>
            {
                foreach (var prefix in new[] { "v", "ve", "vel", "veld" })
                    vm.Query = prefix;
            };

            typing.Should().NotThrow("the keystroke path must not open a single script");
        }

        [AvaloniaTest]
        public async Task ASettledSearchFindsTheMatchingIdentifier()
        {
            File.WriteAllText(Path.Combine(_root, "hello.nss"), "void main() { int Veldite = 1; }");
            File.WriteAllText(Path.Combine(_root, "other.nss"), "void main() { int Something = 2; }");

            var vm = new ScriptSearchViewModel(_root, (_, _) => { }) { Query = "Veldite" };

            await WaitUntilAsync(() => !vm.IsSearching);

            vm.Results.Should().ContainSingle().Which.ResRef.Should().Be("hello");
        }

        /// <summary>
        /// Every prefix of a fast-typed word schedules and then cancels its own scan; only the last one
        /// typed should still be running once the debounce elapses, and it is the only one whose
        /// results should ever reach the list.
        /// </summary>
        [AvaloniaTest]
        public async Task OnlyTheLastQueryTypedProducesResults()
        {
            File.WriteAllText(
                Path.Combine(_root, "hello.nss"),
                "void main() { int Veldite = 1; int Other = 2; }");

            var vm = new ScriptSearchViewModel(_root, (_, _) => { });

            foreach (var prefix in new[] { "O", "Ot", "Oth", "Othe", "Other" })
                vm.Query = prefix;

            await WaitUntilAsync(() => !vm.IsSearching);

            vm.Results.Should().ContainSingle().Which.LineText.Should().Contain("Other");
        }

        /// <summary>Clearing the query must not leave a stale scan running or a stale result on screen.</summary>
        [AvaloniaTest]
        public async Task ClearingTheQueryStopsSearchingAndClearsResults()
        {
            File.WriteAllText(Path.Combine(_root, "hello.nss"), "void main() { int Veldite = 1; }");

            var vm = new ScriptSearchViewModel(_root, (_, _) => { }) { Query = "Veldite" };
            await WaitUntilAsync(() => !vm.IsSearching);
            vm.Results.Should().NotBeEmpty();

            vm.Query = string.Empty;

            vm.IsSearching.Should().BeFalse();
            vm.Results.Should().BeEmpty();
        }

        // ---------- the open-script overlay is a snapshot, not a live lookup ----------

        /// <summary>
        /// A builder editing a script tab has unsaved text the disk copy does not carry yet; the
        /// scan has to see that text, sourced through the snapshot the view model captures rather
        /// than by re-reading the open editors itself.
        /// </summary>
        [AvaloniaTest]
        public async Task AnOpenScriptBuffersUnsavedTextOverridesTheSavedFile()
        {
            File.WriteAllText(Path.Combine(_root, "hello.nss"), "void main() { int Saved = 1; }");

            var vm = new ScriptSearchViewModel(
                _root,
                (_, _) => { },
                () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hello"] = "void main() { int Unsaved = 1; }"
                })
            {
                Query = "Unsaved"
            };

            await WaitUntilAsync(() => !vm.IsSearching);

            vm.Results.Should().ContainSingle().Which.ResRef.Should().Be("hello");
        }

        /// <summary>
        /// The whole point of snapshotting is that the worker never dereferences the live
        /// open-editors map itself - it reads a plain, already-built dictionary. Confirming the
        /// snapshot factory runs exactly once per scan (not once per file in the corpus, which is
        /// how a live per-file lookup would behave) is what tells the two apart.
        /// </summary>
        [AvaloniaTest]
        public async Task TheOpenScriptSnapshotIsCapturedOnceUpFrontNotPerFile()
        {
            File.WriteAllText(Path.Combine(_root, "a.nss"), "void main() { int Veldite = 1; }");
            File.WriteAllText(Path.Combine(_root, "b.nss"), "void main() { int Other = 2; }");
            File.WriteAllText(Path.Combine(_root, "c.nss"), "void main() { int Third = 3; }");

            var snapshotCalls = 0;
            var vm = new ScriptSearchViewModel(_root, (_, _) => { }, () =>
            {
                Interlocked.Increment(ref snapshotCalls);
                return new Dictionary<string, string>();
            })
            {
                Query = "Veldite"
            };

            await WaitUntilAsync(() => !vm.IsSearching);

            snapshotCalls.Should().Be(1,
                "the snapshot must be taken once on the UI thread before the scan starts, " +
                "not read live per file by the worker");
        }

        // ---------- a failed scan must not get stuck "searching" ----------

        /// <summary>
        /// Before this fix, a fault reading the live open-editors state during the scan was only
        /// ever caught as <see cref="OperationCanceledException"/>: any other exception left
        /// <see cref="ScriptSearchViewModel.IsSearching"/> true forever with whatever stale results
        /// were already on screen. This forces a non-cancellation failure (a snapshot that throws
        /// on every read) and checks the scan recovers instead of hanging.
        /// </summary>
        [AvaloniaTest]
        public async Task AFailedScanClearsIsSearchingAndReportsWhy()
        {
            File.WriteAllText(Path.Combine(_root, "hello.nss"), "void main() { int Veldite = 1; }");

            var vm = new ScriptSearchViewModel(_root, (_, _) => { }, () => new ThrowingSnapshot())
            {
                Query = "Veldite"
            };

            await WaitUntilAsync(() => !vm.IsSearching);

            vm.IsSearching.Should().BeFalse();
            vm.Results.Should().BeEmpty();
            vm.Summary.Should().Contain("snapshot exploded");
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline)
                    Assert.Fail("Timed out waiting for the script search to settle.");

                await Task.Delay(25);
            }
        }
    }
}
