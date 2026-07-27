using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Staging a write separately from committing it.
    /// </summary>
    /// <remarks>
    /// An area is one logical document split across a .are and a .git. Saving them in sequence meant a
    /// locked or unwritable .git left the .are already replaced on disk with its history marked clean -
    /// a half-saved area no later Discard or Close could take back. Staging both first moves every way a
    /// save realistically fails (locked file, full disk, a document that will not serialize) to a point
    /// where nothing on disk has changed yet.
    /// </remarks>
    [TestFixture]
    public class StagedSaveTests
    {
        private string _directory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "swlor-staged-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_directory))
                    Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        private string FileWith(string name, string content)
        {
            var path = Path.Combine(_directory, name);
            File.WriteAllText(path, content);
            return path;
        }

        [Test]
        public void StagingLeavesTheRealFileUntouched()
        {
            var path = FileWith("area.are.json", "original");

            SaveService.Stage(path, Encoding.UTF8.GetBytes("replacement"));

            File.ReadAllText(path).Should().Be("original", "nothing is replaced until the commit");
        }

        [Test]
        public void CommittingReplacesTheFile()
        {
            var path = FileWith("area.are.json", "original");

            SaveService.Commit(SaveService.Stage(path, Encoding.UTF8.GetBytes("replacement")));

            File.ReadAllText(path).Should().Be("replacement");
        }

        [Test]
        public void DiscardingLeavesTheFileAndRemovesTheTemporary()
        {
            var path = FileWith("area.are.json", "original");

            var staged = SaveService.Stage(path, Encoding.UTF8.GetBytes("replacement"));
            SaveService.Discard(staged);

            File.ReadAllText(path).Should().Be("original");
            File.Exists(staged.TemporaryPath).Should().BeFalse();
        }

        [Test]
        public void DiscardingTwiceDoesNotThrow()
        {
            // Discard runs on the failure path, where the temporary may already be gone. Throwing there
            // would mask the real failure that got us there.
            var staged = SaveService.Stage(FileWith("area.git.json", "original"), Encoding.UTF8.GetBytes("x"));
            SaveService.Discard(staged);

            var act = () => SaveService.Discard(staged);

            act.Should().NotThrow();
        }

        [Test]
        public void TheSecondFileFailingToStageLeavesBothOriginals()
        {
            // The .are stages fine and the .git cannot be written - the case that used to leave the area
            // half-saved. Both files must still read as they did.
            var are = FileWith("area.are.json", "are-original");
            var git = Path.Combine(_directory, "locked.git.json");
            File.WriteAllText(git, "git-original");

            var staged = SaveService.Stage(are, Encoding.UTF8.GetBytes("are-new"));

            using (var hold = new FileStream(git + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var act = () => SaveService.Stage(git, Encoding.UTF8.GetBytes("git-new"));
                act.Should().Throw<IOException>("the staging write is what fails, before any commit");
            }

            SaveService.Discard(staged);

            File.ReadAllText(are).Should().Be("are-original");
            File.ReadAllText(git).Should().Be("git-original");
        }

        [Test]
        public void WriteAtomicStillWorksForSingleFileCallers()
        {
            var path = FileWith("blueprint.utc.json", "original");

            SaveService.WriteAtomic(path, Encoding.UTF8.GetBytes("replacement"));

            File.ReadAllText(path).Should().Be("replacement");
            File.Exists(path + ".tmp").Should().BeFalse("the temporary is consumed by the move");
        }

        [Test]
        public void FailedSingleFileCommitRemovesItsTemporary()
        {
            var path = FileWith("locked.nss", "original");
            using var hold = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

            var act = () => SaveService.WriteAtomic(path, Encoding.UTF8.GetBytes("replacement"));

            var failure = act.Should().Throw<Exception>().Which;
            (failure is IOException or UnauthorizedAccessException).Should().BeTrue();
            File.Exists(path + ".tmp").Should().BeFalse(
                "script packers must never see failed-save transaction debris");
        }

        [Test]
        public void WriteNewAtomicCreatesACompleteFileAndNeverOverwritesARacingDestination()
        {
            var path = Path.Combine(_directory, "new.utc.json");

            SaveService.WriteNewAtomic(path, Encoding.UTF8.GetBytes("complete"));
            var overwrite = () => SaveService.WriteNewAtomic(path, Encoding.UTF8.GetBytes("replacement"));

            File.ReadAllText(path).Should().Be("complete");
            overwrite.Should().Throw<IOException>();
            File.ReadAllText(path).Should().Be("complete");
            Directory.EnumerateFiles(_directory, "*.tmp").Should().BeEmpty(
                "a lost creation race must clean up its fully staged temporary");
        }

        [Test]
        public void ACommittedWriteLeavesNoTemporaryBehind()
        {
            // Stray .tmp files beside module resources are their own problem - they sit in the folders
            // the packer reads.
            var path = FileWith("area.are.json", "original");

            SaveService.Commit(SaveService.Stage(path, Encoding.UTF8.GetBytes("replacement")));

            Directory.EnumerateFiles(_directory, "*.tmp").Should().BeEmpty();
        }

        [Test]
        public void ALaterCommitFailureRestoresEveryEarlierOriginal()
        {
            var are = FileWith("area.are.json", "are-original");
            var git = Path.Combine(_directory, "locked.git.json");
            Directory.CreateDirectory(git);

            var areWrite = SaveService.Stage(are, Encoding.UTF8.GetBytes("are-new"));
            var gitWrite = SaveService.Stage(git, Encoding.UTF8.GetBytes("git-new"));

            var act = () => SaveService.CommitAll(new[] { areWrite, gitWrite });

            var failure = act.Should().Throw<Exception>().Which;
            (failure is IOException || failure is UnauthorizedAccessException).Should().BeTrue(
                "different filesystems report replacing a directory as one of these two I/O failures");
            File.ReadAllText(are).Should().Be(
                "are-original",
                "the first replacement must be rolled back when a later destination cannot be replaced");
            Directory.Exists(git).Should().BeTrue();
            File.Exists(areWrite.TemporaryPath).Should().BeFalse();
            File.Exists(gitWrite.TemporaryPath).Should().BeFalse();
            Directory.EnumerateFiles(_directory, "*.save-backup").Should().BeEmpty();
        }
    }
}
