using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Dirty tracking and external-change detection for the script editor. Writes only to a
    /// throwaway temp directory - the repo module is never touched.
    /// </summary>
    public class ScriptSessionTests
    {
        private string _dir = null!;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "swlor_script_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        private string WriteScript(string name, string content)
        {
            var path = Path.Combine(_dir, name);
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes(content));
            return path;
        }

        [Test]
        public void FreshlyOpenedSession_IsNotDirty()
        {
            var path = WriteScript("a.nss", "void main()\r\n{\r\n}\r\n");
            var session = ScriptSession.Open(path);

            session.IsDirty(session.Document.Text).Should().BeFalse();
        }

        [Test]
        public void ChangedText_IsDirty_AndRevertingIsCleanAgain()
        {
            var path = WriteScript("a.nss", "void main()\r\n{\r\n}\r\n");
            var session = ScriptSession.Open(path);
            var original = session.Document.Text;

            session.IsDirty(original + "\n// edit").Should().BeTrue();

            // Dirtiness is derived, not a flag: typing and then undoing must report clean.
            session.IsDirty(original).Should().BeFalse();
        }

        [Test]
        public void SavingUnchangedText_LeavesFileByteIdentical()
        {
            var content = "void main()\r\n{\r\n}\r\n";
            var path = WriteScript("a.nss", content);
            var before = File.ReadAllBytes(path);

            var session = ScriptSession.Open(path);
            File.WriteAllBytes(path, session.ToBytes(session.Document.Text));

            File.ReadAllBytes(path).Should().Equal(before);
        }

        [Test]
        public void MarkSaved_ResetsDirtyAndTheExternalChangeBaseline()
        {
            var path = WriteScript("a.nss", "void main()\r\n{\r\n}\r\n");
            var session = ScriptSession.Open(path);
            var edited = session.Document.Text + "\n// edit";
            var savedBytes = session.ToBytes(edited);

            File.WriteAllBytes(path, savedBytes);
            session.MarkSaved(edited, savedBytes);

            session.IsDirty(edited).Should().BeFalse();
            session.HasExternalChange().Should().BeFalse("we made that write ourselves");
        }

        [Test]
        public void MarkSavedRetainsTheFingerprintOfTheBytesThatWereWritten()
        {
            var path = WriteScript("a.nss", "void main() {}\n");
            var session = ScriptSession.Open(path);
            const string edited = "void main() { int mine = 1; }\n";
            var savedBytes = session.ToBytes(edited);

            File.WriteAllBytes(path, savedBytes);
            File.WriteAllText(path, "void main() { int external = 1; }\n");
            session.MarkSaved(edited, savedBytes);

            session.IsDirty(edited).Should().BeFalse(
                "the editor buffer is still the generation it successfully wrote");
            session.HasExternalChange().Should().BeTrue(
                "a replacement arriving after the atomic write must not become the saved fingerprint");
        }

        [Test]
        public void MarkSavedDetectsADeletionAfterTheSuccessfulWrite()
        {
            var path = WriteScript("a.nss", "void main() {}\n");
            var session = ScriptSession.Open(path);
            const string edited = "void main() { int mine = 1; }\n";
            var savedBytes = session.ToBytes(edited);

            File.WriteAllBytes(path, savedBytes);
            File.Delete(path);
            session.MarkSaved(edited, savedBytes);

            session.HasExternalChange().Should().BeTrue(
                "a missing path cannot become the baseline of a write that succeeded");
        }

        [Test]
        public void ExternalWrite_IsDetected()
        {
            var path = WriteScript("a.nss", "void main()\r\n{\r\n}\r\n");
            var session = ScriptSession.Open(path);

            // Coarse mtime resolution would make a same-tick rewrite invisible.
            File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddSeconds(5));

            session.HasExternalChange().Should().BeTrue();
        }

        [Test]
        public void DeletedFile_CountsAsExternalChange()
        {
            var path = WriteScript("a.nss", "void main()\r\n{\r\n}\r\n");
            var session = ScriptSession.Open(path);

            File.Delete(path);

            session.HasExternalChange().Should().BeTrue();
        }

        [Test]
        public void Reload_PicksUpNewContentAndClearsDirty()
        {
            var path = WriteScript("a.nss", "void main()\r\n{\r\n}\r\n");
            var session = ScriptSession.Open(path);

            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("void main()\r\n{\r\n    int n;\r\n}\r\n"));
            var reloaded = session.ReloadFromDisk();

            reloaded.Text.Should().Contain("int n;");
            session.IsDirty(reloaded.Text).Should().BeFalse();
            session.HasExternalChange().Should().BeFalse();
        }

        [Test]
        public void DirtyComparison_IgnoresEolStyleOfTheIncomingText()
        {
            var path = WriteScript("a.nss", "void main()\r\n{\r\n}\r\n");
            var session = ScriptSession.Open(path);

            // A caller handing back CRLF text must not read as permanently dirty.
            session.IsDirty("void main()\r\n{\r\n}").Should().BeFalse();
        }

        [Test]
        public void AtomicSaveRefusesAScriptChangedAfterSerialization()
        {
            var path = WriteScript("a.nss", "void main() {}\n");
            var session = ScriptSession.Open(path);
            var preparedBytes = session.ToBytes("void main() { int mine = 1; }\n");

            File.WriteAllText(path, "void main() { int external = 1; }\n");

            SaveService.TryWriteAtomicIfUnchanged(session, preparedBytes).Should().BeFalse();
            File.ReadAllText(path).Should().Contain("external");
        }
    }
}
