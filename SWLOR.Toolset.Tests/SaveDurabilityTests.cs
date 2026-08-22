using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Validation;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// What survives a save that does not finish, and what Revert actually reverts to.
    /// </summary>
    [TestFixture]
    public class SaveDurabilityTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_save_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(_root, "are"));
            Directory.CreateDirectory(Path.Combine(_root, "git"));
            Directory.CreateDirectory(Path.Combine(_root, "utc"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        /// <summary>
        /// CommitAll moves each original aside before installing its replacement. Killing the process
        /// between those two moves leaves the canonical file gone and its only copy sitting beside it
        /// under a .save-backup name - which nothing looked for, so the next open failed on a file
        /// that was in fact right there.
        /// </summary>
        [Test]
        public void AnInterruptedGroupedSaveGetsItsOriginalBack()
        {
            var target = Path.Combine(_root, "git", "cantina.git.json");
            var backup = target + "." + Guid.NewGuid().ToString("N") + SaveService.BackupSuffix;
            File.WriteAllText(backup, "{\"__data_type\":\"GIT \"}");

            File.Exists(target).Should().BeFalse("this is the state the interruption leaves behind");

            var restored = SaveService.RecoverInterruptedSaves(_root);

            restored.Should().ContainSingle().Which.Should().Be(target);
            File.Exists(target).Should().BeTrue();
            File.Exists(backup).Should().BeFalse();
        }

        /// <summary>
        /// A backup beside a canonical file that exists is the tidy-up the successful save did not
        /// get to. Restoring it would overwrite the newer content with the older.
        /// </summary>
        [Test]
        public void ALeftoverBackupBesideASavedFileIsDiscardedNotRestored()
        {
            var target = Path.Combine(_root, "git", "cantina.git.json");
            File.WriteAllText(target, "{\"saved\":true}");
            var backup = target + "." + Guid.NewGuid().ToString("N") + SaveService.BackupSuffix;
            File.WriteAllText(backup, "{\"stale\":true}");

            SaveService.RecoverInterruptedSaves(_root).Should().BeEmpty();

            File.ReadAllText(target).Should().Be("{\"saved\":true}");
            File.Exists(backup).Should().BeFalse();
        }

        [Test]
        public void AnInterruptedGroupedSaveRollsBackEveryMember()
        {
            const string newAre = "{\"generation\":\"new\"}";
            const string newGit = "{\"generation\":\"new-git\"}";
            var transactionId = Guid.NewGuid().ToString("N");
            var areTarget = Path.Combine(_root, "are", "cantina.are.json");
            var gitTarget = Path.Combine(_root, "git", "cantina.git.json");
            var areBackup = areTarget + "." + transactionId + SaveService.BackupSuffix;
            var gitBackup = gitTarget + "." + transactionId + SaveService.BackupSuffix;

            File.WriteAllText(areTarget, newAre);
            File.WriteAllText(areBackup, "{\"generation\":\"old-are\"}");
            File.WriteAllText(gitBackup, "{\"generation\":\"old-git\"}");

            var manifestPath = Path.Combine(
                _root, "." + transactionId + SaveService.TransactionSuffix);
            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(new
                {
                    Entries = new[]
                    {
                        new
                        {
                            TargetPath = areTarget,
                            TemporaryPath = areTarget + ".tmp",
                            BackupPath = areBackup,
                            HadOriginal = true,
                            ReplacementSha256 = Sha256(newAre)
                        },
                        new
                        {
                            TargetPath = gitTarget,
                            TemporaryPath = gitTarget + ".tmp",
                            BackupPath = gitBackup,
                            HadOriginal = true,
                            ReplacementSha256 = Sha256(newGit)
                        }
                    }
                }));

            var restored = SaveService.RecoverInterruptedSaves(_root);

            restored.Should().BeEquivalentTo(areTarget, gitTarget);
            File.ReadAllText(areTarget).Should().Be("{\"generation\":\"old-are\"}");
            File.ReadAllText(gitTarget).Should().Be("{\"generation\":\"old-git\"}");
            File.Exists(manifestPath).Should().BeFalse();
            File.Exists(areBackup).Should().BeFalse();
            File.Exists(gitBackup).Should().BeFalse();
        }

        /// <summary>
        /// If the GIT half of a group cannot be put back (its target is blocked here by an
        /// obstruction standing where the file should go, standing in for a locked file in
        /// practice) recovery must not report success. Silently continuing left the ARE at its new
        /// generation and the GIT missing, with nothing telling WorkspaceContext.Open to refuse -
        /// so the area opened at mixed generations instead of failing loudly.
        /// </summary>
        [Test]
        public void ARecoveryFailureForOneMemberThrowsInsteadOfReportingSuccess()
        {
            const string newAre = "{\"generation\":\"new\"}";
            const string newGit = "{\"generation\":\"new-git\"}";
            var transactionId = Guid.NewGuid().ToString("N");
            var areTarget = Path.Combine(_root, "are", "cantina.are.json");
            var gitTarget = Path.Combine(_root, "git", "cantina.git.json");
            var areBackup = areTarget + "." + transactionId + SaveService.BackupSuffix;
            var gitBackup = gitTarget + "." + transactionId + SaveService.BackupSuffix;

            File.WriteAllText(areTarget, newAre);
            File.WriteAllText(areBackup, "{\"generation\":\"old-are\"}");
            File.WriteAllText(gitBackup, "{\"generation\":\"old-git\"}");

            // Stands in the GIT target's way: File.Move onto an existing directory throws, the way
            // a locked file would, without depending on platform-specific file-locking behavior.
            Directory.CreateDirectory(gitTarget);

            var manifestPath = Path.Combine(
                _root, "." + transactionId + SaveService.TransactionSuffix);
            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(new
                {
                    Entries = new[]
                    {
                        new
                        {
                            TargetPath = areTarget,
                            TemporaryPath = areTarget + ".tmp",
                            BackupPath = areBackup,
                            HadOriginal = true,
                            ReplacementSha256 = Sha256(newAre)
                        },
                        new
                        {
                            TargetPath = gitTarget,
                            TemporaryPath = gitTarget + ".tmp",
                            BackupPath = gitBackup,
                            HadOriginal = true,
                            ReplacementSha256 = Sha256(newGit)
                        }
                    }
                }));

            Action act = () => SaveService.RecoverInterruptedSaves(_root);

            act.Should().Throw<SaveRecoveryException>()
                .Which.Message.Should().Contain(gitTarget);

            File.Exists(manifestPath).Should().BeTrue(
                "the group is still incomplete, so startup must roll it back again next time");
            File.Exists(gitBackup).Should().BeTrue("the only copy of the GIT generation must survive");
        }

        /// <summary>
        /// Recovery owns only the replacement generation recorded in its manifest. If another editor
        /// changes the canonical target after the crash, restoring the backup over it would silently
        /// destroy newer work.
        /// </summary>
        [Test]
        public void RecoveryRefusesToOverwriteATargetChangedAfterTheInterruptedSave()
        {
            const string installed = "{\"generation\":\"interrupted-save\"}";
            const string external = "{\"generation\":\"newer-external-edit\"}";
            var transactionId = Guid.NewGuid().ToString("N");
            var target = Path.Combine(_root, "git", "cantina.git.json");
            var backup = target + "." + transactionId + SaveService.BackupSuffix;
            var manifestPath = Path.Combine(
                _root, "." + transactionId + SaveService.TransactionSuffix);

            File.WriteAllText(target, external);
            File.WriteAllText(backup, "{\"generation\":\"original\"}");
            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(new
                {
                    Entries = new[]
                    {
                        new
                        {
                            TargetPath = target,
                            TemporaryPath = target + ".tmp",
                            BackupPath = backup,
                            HadOriginal = true,
                            ReplacementSha256 = Sha256(installed)
                        }
                    }
                }));

            Action act = () => SaveService.RecoverInterruptedSaves(_root);

            act.Should().Throw<SaveRecoveryException>()
                .Which.Message.Should().Contain(target);
            File.ReadAllText(target).Should().Be(external);
            File.Exists(backup).Should().BeTrue("the original is recovery evidence");
            File.Exists(manifestPath).Should().BeTrue("the unresolved transaction must remain visible");
        }

        /// <summary>
        /// An unreadable manifest means recovery cannot know which files the interrupted transaction
        /// touched, so it protects the backups from the orphan sweep - but a moved ARE/GIT/GIC target
        /// whose only copy is one of those protected backups is still missing from its canonical path.
        /// Recovery must refuse to report success here too, the same as the failed-restoration path,
        /// or WorkspaceContext.Open continues past a group that is still incomplete.
        /// </summary>
        [Test]
        public void AnUnreadableManifestShieldsItsTransactionBackupsAndReportsIncomplete()
        {
            var transactionId = Guid.NewGuid().ToString("N");
            var areTarget = Path.Combine(_root, "are", "cantina.are.json");
            var gitTarget = Path.Combine(_root, "git", "cantina.git.json");
            var areBackup = areTarget + "." + transactionId + SaveService.BackupSuffix;
            var gitBackup = gitTarget + "." + transactionId + SaveService.BackupSuffix;

            // A replacement landed for the ARE but not the GIT: the generic orphan sweep would
            // delete the ARE backup (target exists) while restoring the GIT backup, leaving the
            // group at mixed generations - exactly what the manifest exists to prevent.
            File.WriteAllText(areTarget, "{\"generation\":\"new\"}");
            File.WriteAllText(areBackup, "{\"generation\":\"old-are\"}");
            File.WriteAllText(gitBackup, "{\"generation\":\"old-git\"}");

            var manifestPath = Path.Combine(
                _root, "." + transactionId + SaveService.TransactionSuffix);
            File.WriteAllText(manifestPath, "{ this is not json");

            Action act = () => SaveService.RecoverInterruptedSaves(_root);

            act.Should().Throw<SaveRecoveryException>().Which.Message.Should().Contain(manifestPath);

            File.Exists(manifestPath).Should().BeTrue("unreadable manifests are evidence, not litter");
            File.Exists(areBackup).Should().BeTrue("the transaction's backups must survive the sweep");
            File.Exists(gitBackup).Should().BeTrue();
            File.Exists(gitTarget).Should().BeFalse("no member may be restored piecemeal");
            File.ReadAllText(areTarget).Should().Be("{\"generation\":\"new\"}");
        }

        /// <summary>
        /// "null" is valid JSON: deserialization returns null without throwing, which used to slip
        /// past the unreadable-manifest protection into the generic orphan sweep and then report
        /// success over a group that was never actually put back.
        /// </summary>
        [Test]
        public void AManifestThatDeserializesToNullShieldsItsTransactionBackupsAndReportsIncomplete()
        {
            var transactionId = Guid.NewGuid().ToString("N");
            var areTarget = Path.Combine(_root, "are", "cantina.are.json");
            var gitTarget = Path.Combine(_root, "git", "cantina.git.json");
            var areBackup = areTarget + "." + transactionId + SaveService.BackupSuffix;
            var gitBackup = gitTarget + "." + transactionId + SaveService.BackupSuffix;

            File.WriteAllText(areTarget, "{\"generation\":\"new\"}");
            File.WriteAllText(areBackup, "{\"generation\":\"old-are\"}");
            File.WriteAllText(gitBackup, "{\"generation\":\"old-git\"}");

            var manifestPath = Path.Combine(
                _root, "." + transactionId + SaveService.TransactionSuffix);
            File.WriteAllText(manifestPath, "null");

            Action act = () => SaveService.RecoverInterruptedSaves(_root);

            act.Should().Throw<SaveRecoveryException>().Which.Message.Should().Contain(manifestPath);

            File.Exists(manifestPath).Should().BeTrue("an entry-less manifest is still evidence");
            File.Exists(areBackup).Should().BeTrue("the transaction's backups must survive the sweep");
            File.Exists(gitBackup).Should().BeTrue();
            File.Exists(gitTarget).Should().BeFalse("no member may be restored piecemeal");
        }

        /// <summary>
        /// The staged file is transaction debris once the commit has failed. Left behind as
        /// "foo.nss.tmp", the packer's script copy - which took whatever was in the folder - shipped
        /// it into the .mod as a script resource.
        /// </summary>
        [Test]
        public void AFailedCommitLeavesNoStagedFileBehind()
        {
            var target = Path.Combine(_root, "utc", "guard.utc.json");
            File.WriteAllText(target, "{\"original\":true}");

            // Held open with no sharing, so the move onto it cannot succeed.
            using (var _ = new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var write = () => SaveService.WriteAtomic(target, Encoding.UTF8.GetBytes("{\"new\":true}"));
                write.Should().Throw<Exception>("the target is locked, so the move onto it cannot land");
            }

            File.Exists(target + ".tmp").Should().BeFalse();
            File.ReadAllText(target).Should().Be("{\"original\":true}");
        }

        /// <summary>
        /// Revert means "put this back the way it is on disk". Save does not clear the history, so
        /// unwinding the whole stack also unwound transactions already saved: the document ended up
        /// older than the file, still dirty, and the next save wrote that over committed work.
        /// </summary>
        [Test]
        public void RevertStopsAtTheSavedPositionRatherThanTheBeginning()
        {
            var path = WriteCreature("revert_target", "Original");
            using var session = DocumentSession.Open(path);

            Retag(session, "Committed");
            session.UndoStack.MarkSaved();
            Retag(session, "Unsaved");

            session.UndoStack.IsDirty.Should().BeTrue();

            session.RevertToSaved();

            session.UndoStack.Position.Should().Be(1, "only the unsaved transaction comes off");
            session.UndoStack.IsDirty.Should().BeFalse();
            Tag(session).Should().Be(
                "Committed", "the saved version is what is on disk, and Revert goes back to disk");
        }

        [Test]
        public void RevertWithNothingSavedStillUnwindsEverything()
        {
            var path = WriteCreature("revert_all", "Original");
            using var session = DocumentSession.Open(path);

            Retag(session, "First");
            Retag(session, "Second");

            session.RevertToSaved();

            session.UndoStack.Position.Should().Be(0);
            session.UndoStack.IsDirty.Should().BeFalse();
            Tag(session).Should().Be("Original");
        }

        /// <summary>
        /// A malformed dialog, IFO, palette or journal used to produce no issue at all and then fail
        /// the pack a minute later inside nwn_gff - the least useful place to find out.
        /// </summary>
        [Test]
        public void ValidationReadsEveryPackedGffFolderNotOnlyTheEditableOnes()
        {
            Directory.CreateDirectory(Path.Combine(_root, "dlg"));
            Directory.CreateDirectory(Path.Combine(_root, "jrl"));
            File.WriteAllText(Path.Combine(_root, "utc", "fine.utc.json"), "{\"__data_type\":\"UTC \"}");
            File.WriteAllText(Path.Combine(_root, "dlg", "broken.dlg.json"), "{ not json");
            File.WriteAllText(Path.Combine(_root, "jrl", "module.jrl.json"), "{ also not json");

            var issues = new GffParseRule()
                .Validate(new ValidationContext(new ModuleWorkspace(_root)))
                .ToList();

            issues.Select(issue => issue.ResRef).Should().BeEquivalentTo("broken", "module");
            issues.Should().OnlyContain(issue => issue.Severity == ValidationSeverity.Error);
        }

        /// <summary>
        /// "bad-name" is short and lowercase, so both existing checks passed - and no other default
        /// rule looks at the character set at all.
        /// </summary>
        [Test]
        public void ValidationRejectsAResRefTheEngineCannotAddress()
        {
            File.WriteAllText(Path.Combine(_root, "utc", "bad-name.utc.json"), "{\"__data_type\":\"UTC \"}");
            File.WriteAllText(Path.Combine(_root, "utc", "good_name.utc.json"), "{\"__data_type\":\"UTC \"}");

            var issues = new ResRefLengthRule()
                .Validate(new ValidationContext(new ModuleWorkspace(_root)))
                .ToList();

            issues.Should().ContainSingle().Which.ResRef.Should().Be("bad-name");
        }

        private string WriteCreature(string resRef, string tag)
        {
            var path = Path.Combine(_root, "utc", resRef + ".utc.json");
            File.WriteAllText(
                path,
                "{\"__data_type\":\"UTC \",\"Tag\":{\"type\":\"cexostring\",\"value\":\"" + tag + "\"}}");
            return path;
        }

        private static void Retag(DocumentSession session, string tag)
        {
            session.Execute(
                "Set tag to " + tag,
                () => session.Document.Root.GetOrNull("Tag")!.SetString(tag));
        }

        private static string? Tag(DocumentSession session) =>
            session.Document.Root.GetOrNull("Tag")?.GetString();

        private static string Sha256(string content) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
