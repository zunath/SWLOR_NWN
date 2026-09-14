using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class CategoryServiceTests
    {
        private string _root = string.Empty;
        private string _module = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "swlor-category-service-" + Guid.NewGuid().ToString("N"));
            _module = Path.Combine(_root, "Module");
            Directory.CreateDirectory(Path.Combine(_module, "itp"));
            Directory.CreateDirectory(Path.Combine(_module, "are"));
            Directory.CreateDirectory(Path.Combine(_module, "utc"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void MissingPaletteIsRetriedInsteadOfBeingPermanentlyMarkedSeeded()
        {
            var service = OpenService();

            service.Section(ResourceType.Utp)!.IsSeeded.Should().BeFalse();

            File.WriteAllText(Path.Combine(_module, "itp", "placeablepalcus.itp.json"), """
                {
                  "__data_type": "ITP ",
                  "MAIN": { "type": "list", "value": [
                    { "__struct_id": 1,
                      "NAME": { "type": "cexostring", "value": "Cargo" },
                      "DELETE_ME": { "type": "byte", "value": 0 },
                      "LIST": { "type": "list", "value": [
                        { "__struct_id": 0,
                          "NAME": { "type": "cexostring", "value": "Crate" },
                          "RESREF": { "type": "resref", "value": "crate_01" } }
                      ] } }
                  ] }
                }
                """);

            var retried = service.Section(ResourceType.Utp)!;

            retried.IsSeeded.Should().BeTrue();
            retried.Find("Cargo").Should().NotBeNull();
        }

        [Test]
        public void DeletingTheLoadedSidecarIsDetectedAsAnExternalChange()
        {
            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, """
                { "version": 1, "sections": {
                    "utp": { "seeded": true, "folders": [ { "name": "Cargo" } ] }
                } }
                """);
            var service = OpenService();
            var section = service.Section(ResourceType.Utp)!;
            File.Delete(sidecar);
            section.AddFolder("Interiors");

            var result = service.SaveChanges();

            result.Saved.Should().BeFalse();
            result.Problem.Should().Contain("changed outside");
            File.Exists(sidecar).Should().BeFalse("an external deletion must not be silently recreated");
            service.Section(ResourceType.Utp)!.Find("Cargo").Should().NotBeNull();
            service.Section(ResourceType.Utp)!.Find("Interiors").Should().BeNull(
                "a rejected edit must not remain live and leak into a later save");
        }

        /// <summary>
        /// Mtime alone misses an external replacement that preserves the timestamp (or two writes that
        /// land in the same coarse bucket): the conflict check has to fall back to a content
        /// fingerprint, the way DocumentSession's own external-change check does, or the next edit here
        /// silently overwrites the externally changed arrangement.
        /// </summary>
        [Test]
        public void ExternalContentChangeUnderAnUnchangedTimestampIsStillDetected()
        {
            var service = OpenService();
            var section = service.Section(ResourceType.Utp)!;
            section.AddFolder("Original");
            service.SaveChanges().Saved.Should().BeTrue();

            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            var mtime = File.GetLastWriteTimeUtc(sidecar);

            // An external tool (or a second process) replaces the bytes but leaves the mtime exactly as
            // it was - the one case a timestamp-only comparison cannot see.
            File.WriteAllText(sidecar, """
                { "version": 1, "sections": { "utp": {
                    "seeded": true, "folders": [ { "name": "ExternallyChanged" } ] } } }
                """);
            File.SetLastWriteTimeUtc(sidecar, mtime);

            section.AddFolder("AttemptedAfterExternalChange");
            var result = service.SaveChanges();

            result.Saved.Should().BeFalse("the bytes changed even though the timestamp did not");
            result.Problem.Should().Contain("changed outside");
        }

        /// <summary>
        /// A read failure is not proof the file is unchanged - it is proof the file could not be
        /// compared. If an external process (or a transient lock) makes the sidecar unreadable while
        /// its directory stays writable, the save must refuse rather than let the atomic
        /// <c>File.Move</c> replace a file whose current contents were never actually checked.
        /// </summary>
        [Test]
        public void UnreadableSidecarContentRefusesTheSaveInsteadOfAssumingNoConflict()
        {
            var service = OpenService();
            var section = service.Section(ResourceType.Utp)!;
            section.AddFolder("Original");
            service.SaveChanges().Saved.Should().BeTrue();

            var sidecar = CategoryCatalog.DefaultPathFor(_module);

            section.AddFolder("AttemptedWhileUnreadable");

            CategorySaveResult result;
            // Hold an exclusive lock so the sidecar's bytes cannot be read (ComputeHash fails) even
            // though its timestamp is untouched and still matches the recorded baseline - the one case
            // a timestamp-only comparison would wrongly call "no conflict".
            using (new FileStream(sidecar, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                result = service.SaveChanges();
            }

            result.Saved.Should().BeFalse("an unreadable fingerprint must not be treated as proof nothing changed");
            result.Problem.Should().Contain("changed outside");
        }

        [Test]
        public void ReadOnlySidecarRollsBackRejectedInMemoryEdits()
        {
            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, """
                { "version": 2, "sections": {
                    "utp": { "seeded": true, "folders": [ { "name": "Cargo" } ] }
                } }
                """);
            var service = OpenService();
            service.Section(ResourceType.Utp)!.Find("Cargo")!.Rename("Rejected rename");

            var result = service.SaveChanges();

            result.Saved.Should().BeFalse();
            service.Section(ResourceType.Utp)!.Find("Cargo").Should().NotBeNull();
            service.Section(ResourceType.Utp)!.Find("Rejected rename").Should().BeNull();
        }

        /// <summary>
        /// A name resolved out of the TLK is no more trustworthy than one read out of a palette - several
        /// of the base game's carry a path separator. This repair runs over a tree that is already loaded
        /// and on screen, so throwing here took the open module down after it had opened cleanly.
        /// </summary>
        [Test]
        public void APlaceholderResolvingToANameWithThePathSeparator_IsRepairedRatherThanThrownAt()
        {
            const uint StrRef = TlkService.CustomTlkBase + 42;

            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            // "placeholder": true is what ItpCategoryImporter itself now writes for an unresolved name;
            // without it, repair must not touch this folder at all (see the test below).
            File.WriteAllText(sidecar, $$"""
                { "version": 1, "sections": { "utp": {
                    "pinned": [ "Category {{StrRef}}" ],
                    "folders": [ { "name": "Category {{StrRef}}", "placeholder": true, "members": [ "crate01" ] } ] } } }
                """);

            var service = OpenService(Tlk(42, "Skin/Hide"));

            var section = service.Section(ResourceType.Utp)!;
            var folder = section.Folders.Should().ContainSingle().Subject;
            folder.Name.Should().Be("Skin-Hide");
            folder.Members.Should().Contain("crate01", "repairing the name must not lose the contents");
            section.Pinned.Should().Equal(new[] { "Skin-Hide" },
                because: "a pin is stored by path, so it has to move with the name");
        }

        /// <summary>
        /// A folder named "Category 7" is textually identical whether it is an unresolved import
        /// placeholder or a name a builder typed on purpose. Provenance has to come from an explicit
        /// marker rather than the text - and a sidecar with no marker at all (either legacy, predating
        /// the flag, or a builder's deliberate name) must never be auto-renamed just because the name
        /// happens to also resolve against the TLK.
        /// </summary>
        [Test]
        public void ACategoryNamedLikeAPlaceholderSurvivesWithoutAnExplicitMarker()
        {
            const uint StrRef = TlkService.CustomTlkBase + 7;

            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, $$"""
                { "version": 1, "sections": { "utp": {
                    "folders": [ { "name": "Category {{StrRef}}", "members": [ "crate01" ] } ] } } }
                """);

            var service = OpenService(Tlk(7, "Resolved Name"));

            var section = service.Section(ResourceType.Utp)!;
            var folder = section.Folders.Should().ContainSingle().Subject;
            folder.Name.Should().Be($"Category {StrRef}",
                "no marker means no provenance to act on, whether this is a deliberate name or a legacy placeholder");
        }

        /// <summary>
        /// A blueprint rename must carry its category membership with it, in every folder that filed
        /// it - otherwise reopening the module leaves a dangling member under the old resref and the
        /// renamed item unfiled, with nothing to notice either half of that.
        /// </summary>
        [Test]
        public void RefileMemberMovesMembershipInEveryFolderThatHeldTheOldResRef()
        {
            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, """
                { "version": 1, "sections": { "utc": {
                    "seeded": true, "folders": [
                        { "name": "Beasts", "members": [ "womp_rat", "nexu" ] },
                        { "name": "Dathomir", "members": [ "womp_rat" ] }
                    ] } } }
                """);
            var service = OpenService();

            var result = service.RefileMember(ResourceType.Utc, "womp_rat", "womp_rat_2");

            result.Saved.Should().BeTrue();
            var section = service.Section(ResourceType.Utc)!;
            section.Find("Beasts")!.Members.Should().Equal(new[] { "nexu", "womp_rat_2" });
            section.Find("Dathomir")!.Members.Should().Equal(new[] { "womp_rat_2" });
        }

        /// <summary>
        /// A resref filed nowhere has nothing to carry, so this is a no-op that still reports success -
        /// the common case for an item with no custom category, which must not block its rename.
        /// </summary>
        [Test]
        public void RefileMemberOfAnUnfiledResRefIsANoOpThatSucceeds()
        {
            var service = OpenService();
            service.Section(ResourceType.Utc)!.AddFolder("Beasts").AddMember("nexu");

            var result = service.RefileMember(ResourceType.Utc, "womp_rat", "womp_rat_2");

            result.Saved.Should().BeTrue();
            service.Section(ResourceType.Utc)!.Find("Beasts")!.Members.Should().Equal(new[] { "nexu" });
        }

        /// <summary>
        /// The preflight a rename runs before touching any file: refuses only when the resref is
        /// actually filed somewhere AND the sidecar itself cannot be saved, mirroring the delete
        /// preflight <see cref="Shell.Panels.PaletteViewModel"/> already runs.
        /// </summary>
        [Test]
        public void CanRefileMemberRefusesOnlyWhenAFiledResRefsSidecarCannotBeSaved()
        {
            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, """
                { "version": 1, "sections": { "utc": {
                    "seeded": true, "folders": [ { "name": "Beasts", "members": [ "womp_rat" ] } ] } } }
                """);
            var service = OpenService();

            // Unfiled: nothing to carry, so this is allowed even though the sidecar below is unreadable.
            service.CanRefileMember(ResourceType.Utc, "nexu").Should().BeTrue();

            using (new FileStream(sidecar, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                service.CanRefileMember(ResourceType.Utc, "womp_rat").Should().BeFalse(
                    "the resref is filed, and the sidecar cannot be verified unchanged while locked");
            }

            service.CanRefileMember(ResourceType.Utc, "womp_rat").Should().BeTrue(
                "the lock is gone and nothing else changed");
        }

        private static TlkService Tlk(int entryId, string text) =>
            new(TlkJsonFile.Parse($$"""
                { "language": 0, "entries": [ { "id": {{entryId}}, "text": "{{text}}" } ] }
                """));

        private CategoryService OpenService(TlkService? tlk = null)
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            context.Open(_module);
            return new CategoryService(context, log, tlk);
        }
    }
}
