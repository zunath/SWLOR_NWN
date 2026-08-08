using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class DocumentSessionExternalChangeTests
    {
        private string _path = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), $"swlor_session_{Guid.NewGuid():N}.utc.json");
            File.Copy(CorpusFiles.FindFileWithMutableInteger("utc"), _path);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }

        [Test]
        public void SameTimestampDifferentContent_IsStillAnExternalChange()
        {
            var recordedMtime = File.GetLastWriteTimeUtc(_path);

            // Prepare the external replacement before a session's edit guard is ambient.
            var external = JsonGffDocument.Load(_path);
            var externalField = CorpusFiles.FindFirstMutableInteger(external.Root)!;
            externalField.SetInteger(externalField.GetInteger() + 1);
            var externalBytes = external.ToBytes();

            using var session = DocumentSession.Open(_path);

            // An external tool replaces the file but preserves the timestamp (or the filesystem's
            // granularity makes both writes share one). The fingerprint must catch it.
            File.WriteAllBytes(_path, externalBytes);
            File.SetLastWriteTimeUtc(_path, recordedMtime);

            session.HasExternalChange().Should().BeTrue(
                "identical timestamps must not hide different bytes");
        }

        [Test]
        public void FromLoadedContent_FileDeletedAfterRead_IsAnExternalChange()
        {
            var content = File.ReadAllBytes(_path);
            var document = JsonGffDocument.Parse(content);
            File.Delete(_path);

            using var session = DocumentSession.FromLoadedContent(_path, document, content);

            session.HasExternalChange().Should().BeTrue(
                "bytes loaded from an existing file must retain a baseline when that file disappears before binding");
        }

        [Test]
        public void RevertAfterTheSavedHistoryWasDiscarded_ReloadsTheDiskState()
        {
            using var session = DocumentSession.Open(_path);
            var field = CorpusFiles.FindFirstMutableInteger(session.Document.Root)!;
            var initialValue = field.GetInteger();

            // Save a real edit to disk, then undo past it and branch: the saved history is gone.
            using (session.Begin("edit A"))
                field.SetInteger(initialValue + 1);
            var savedBytes = session.ToBytes();
            File.WriteAllBytes(_path, savedBytes);
            session.UndoStack.MarkSaved();
            session.RecordCurrentFileState(savedBytes);

            session.Undo();
            using (session.Begin("edit B"))
                field.SetInteger(initialValue);

            session.RevertToSaved();

            CorpusFiles.FindFirstMutableInteger(session.Document.Root)!.GetInteger().Should().Be(
                initialValue + 1,
                "Revert must land on the version committed to disk, not the initial load state");
            session.UndoStack.IsDirty.Should().BeFalse();
            session.HasExternalChange().Should().BeFalse();
        }

        [Test]
        public void ReloadFromDisk_ReplacesDocumentAndResetsExternalChangeBaseline()
        {
            var external = JsonGffDocument.Load(_path);
            var externalField = CorpusFiles.FindFirstMutableInteger(external.Root)!;
            var originalValue = externalField.GetInteger();
            externalField.SetInteger(originalValue + 1);
            var externalBytes = external.ToBytes();

            using var session = DocumentSession.Open(_path);
            File.WriteAllBytes(_path, externalBytes);
            File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(2));

            session.HasExternalChange().Should().BeTrue();
            session.ReloadFromDisk();

            CorpusFiles.FindFirstMutableInteger(session.Document.Root)!.GetInteger().Should().Be(originalValue + 1);
            session.UndoStack.IsDirty.Should().BeFalse();
            session.HasExternalChange().Should().BeFalse();
        }

        [Test]
        public void RecordCurrentFileState_PreventsOwnSaveFromLookingExternal()
        {
            using var session = DocumentSession.Open(_path);
            var field = CorpusFiles.FindFirstMutableInteger(session.Document.Root)!;
            using (session.Begin("edit"))
                field.SetInteger(field.GetInteger() + 1);

            var savedBytes = session.ToBytes();
            File.WriteAllBytes(_path, savedBytes);
            session.UndoStack.MarkSaved();
            session.RecordCurrentFileState(savedBytes);

            session.HasExternalChange().Should().BeFalse();
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        [Test]
        public void RecordCurrentFileState_DoesNotAdoptAReplacementAfterTheSave()
        {
            using var session = DocumentSession.Open(_path);
            var field = CorpusFiles.FindFirstMutableInteger(session.Document.Root)!;
            using (session.Begin("edit"))
                field.SetInteger(field.GetInteger() + 1);
            var savedBytes = session.ToBytes();

            File.WriteAllBytes(_path, savedBytes);
            File.WriteAllText(_path, "external replacement after save");

            session.RecordCurrentFileState(savedBytes);

            session.HasExternalChange().Should().BeTrue(
                "the baseline hash belongs to the bytes the session saved, not a later disk read");
        }

        [Test]
        public void ReloadFrom_DoesNotAdoptAReplacementReadAfterTheDocument()
        {
            var first = JsonGffDocument.Load(_path);
            var firstField = CorpusFiles.FindFirstMutableInteger(first.Root)!;
            var originalValue = firstField.GetInteger();
            firstField.SetInteger(originalValue + 1);
            var firstBytes = first.ToBytes();

            var second = JsonGffDocument.Load(_path);
            CorpusFiles.FindFirstMutableInteger(second.Root)!.SetInteger(originalValue + 2);
            var secondBytes = second.ToBytes();

            using var session = DocumentSession.Open(_path);
            File.WriteAllBytes(_path, firstBytes);
            var parsedFirst = JsonGffDocument.Parse(firstBytes);
            File.WriteAllBytes(_path, secondBytes);

            session.ReloadFrom(parsedFirst, firstBytes);

            CorpusFiles.FindFirstMutableInteger(session.Document.Root)!.GetInteger()
                .Should().Be(originalValue + 1);
            session.HasExternalChange().Should().BeTrue(
                "the document and baseline must remain tied to the first reload generation");
        }

        [Test]
        public void CaptureSnapshots_WaitsForAnOpenEditTransaction()
        {
            using var session = DocumentSession.Open(_path);
            var field = CorpusFiles.FindFirstMutableInteger(session.Document.Root)!;
            using var transaction = session.Begin("edit");
            field.SetInteger(field.GetInteger() + 1);

            var capture = Task.Run(() => DocumentSession.CaptureSnapshots(session));
            Thread.Sleep(50);
            capture.IsCompleted.Should().BeFalse("snapshot serialization must not race a live edit");

            transaction.Commit();
            var snapshot = capture.GetAwaiter().GetResult().Single();

            JsonGffDocument.Parse(snapshot).Should().NotBeNull();
        }

        [Test]
        public void AtomicSaveRefusesAFileChangedAfterSerialization()
        {
            using var session = DocumentSession.Open(_path);
            var field = CorpusFiles.FindFirstMutableInteger(session.Document.Root)!;
            using (session.Begin("edit"))
                field.SetInteger(field.GetInteger() + 1);
            var preparedBytes = session.ToBytes();

            File.WriteAllText(_path, "newer external generation");

            SaveService.TryWriteAtomicIfUnchanged(session, preparedBytes).Should().BeFalse();
            File.ReadAllText(_path).Should().Be("newer external generation");
        }

        [Test]
        public async Task OrdinarySaveFailsFastWhenAnotherProcessOwnsTheModuleLease()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(), $"swlor_save_lock_{Guid.NewGuid():N}");
            var utcDirectory = Path.Combine(moduleRoot, "utc");
            Directory.CreateDirectory(utcDirectory);
            var path = Path.Combine(utcDirectory, "guard.utc.json");
            File.Copy(CorpusFiles.FindFileWithMutableInteger("utc"), path);
            var originalBytes = File.ReadAllBytes(path);
            var acquired = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Task holder;
            using (ExecutionContext.SuppressFlow())
            {
                holder = Task.Run(async () =>
                {
                    using var moduleWriteLock = ModuleWriteLock.Acquire(moduleRoot);
                    acquired.SetResult();
                    await release.Task;
                });
            }

            try
            {
                await acquired.Task.WaitAsync(TimeSpan.FromSeconds(5));
                using var session = DocumentSession.Open(path);
                var field = CorpusFiles.FindFirstMutableInteger(session.Document.Root)!;
                using (session.Begin("edit"))
                    field.SetInteger(field.GetInteger() + 1);
                var log = new OutputLogService();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                new SaveService(log).Save(session).Should().BeFalse();

                stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
                File.ReadAllBytes(path).Should().Equal(originalBytes);
                log.Lines.Should().Contain(line => line.Contains("module writer", StringComparison.Ordinal));
            }
            finally
            {
                release.TrySetResult();
                await holder;
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
        }
    }
}
