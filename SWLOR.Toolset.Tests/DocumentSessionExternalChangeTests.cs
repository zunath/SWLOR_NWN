using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

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

            File.WriteAllBytes(_path, session.ToBytes());
            session.UndoStack.MarkSaved();
            session.RecordCurrentFileState();

            session.HasExternalChange().Should().BeFalse();
            session.UndoStack.IsDirty.Should().BeFalse();
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
    }
}
