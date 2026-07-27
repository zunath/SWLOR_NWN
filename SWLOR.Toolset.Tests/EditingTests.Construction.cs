using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Gff;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Building a brand-new document while an editing session is open.
    /// </summary>
    /// <remarks>
    /// The guard <see cref="EditScope"/> enforces is ambient per call context, not per document, so an
    /// open <see cref="DocumentSession"/> raises it for everything else running on that context too. That
    /// shipped as a real failure: with any editor open, loading the base game's standard palettes threw
    /// "This document is attached to an undo stack" and the whole Standard half of the palette came back
    /// empty. These pin the construction paths against it.
    /// </remarks>
    public class EditingTestsConstruction
    {
        private static readonly ResourceType[] BlueprintTypes =
        {
            ResourceType.Utc, ResourceType.Uti, ResourceType.Utp, ResourceType.Utd,
            ResourceType.Utt, ResourceType.Uts, ResourceType.Utw
        };

        /// <summary>A session over a real document, standing in for "an editor tab is open".</summary>
        private static DocumentSession OpenSession()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            return new DocumentSession(path, JsonGffDocument.Parse(File.ReadAllBytes(path)));
        }

        [TestCaseSource(nameof(BlueprintTypes))]
        public void ANewBlueprint_CanBeBuiltWhileASessionIsOpen(ResourceType type)
        {
            using var session = OpenSession();

            var act = () => BlueprintTemplateFactory.CreateFileContent(type, "probe_resref", "Probe");

            act.Should().NotThrow(
                "creating a blueprint while an editor is open builds a new document, not an edit to the open one");
        }

        [TestCase(ResourceType.Dlg)]
        [TestCase(ResourceType.Nss)]
        public void ANewModuleResource_CanBeBuiltWhileASessionIsOpen(ResourceType type)
        {
            using var session = OpenSession();

            var act = () => ModuleResourceTemplateFactory.CreateFileContent(type, "probe_resref", "Probe");

            act.Should().NotThrow();
        }

        /// <summary>
        /// The exact shipped failure: the palette converts base-game .itp files through the bridge, and
        /// the bridge builds its structs with the guarded entry point.
        /// </summary>
        [Test]
        public void AParsedGffModel_CanBeConvertedToJsonWhileASessionIsOpen()
        {
            var root = new GffStruct { Type = 0 };
            root.Fields.Add(new GffField(GffField.CExoString, "Tag", "probe_resref"));
            var parsed = new GffFile { FileType = "UTP ", FileVersion = "V3.2", RootStruct = root };

            using var session = OpenSession();

            var act = () => GffJsonBridge.ToJsonDocument(parsed);

            act.Should().NotThrow(
                "converting a parsed GFF produces a new document that no session owns");
        }

        /// <summary>
        /// Construction must not land on the open document's undo stack: a new document's fields are not
        /// edits to the one a transaction is recording.
        /// </summary>
        [Test]
        public void ConstructionInsideATransaction_DoesNotJoinThatTransaction()
        {
            using var session = OpenSession();

            using (session.Begin("build something unrelated"))
            {
                BlueprintTemplateFactory.CreateFileContent(ResourceType.Utw, "probe_resref", "Probe");
            }

            session.UndoStack.Entries.Should().BeEmpty(
                "building a separate document is not an edit to the document being recorded");
        }

        /// <summary>
        /// The guard still has to work afterwards - the construction scope restores what it suspended.
        /// </summary>
        [Test]
        public void AfterConstruction_TheGuardIsStillArmed()
        {
            using var session = OpenSession();

            BlueprintTemplateFactory.CreateFileContent(ResourceType.Utc, "probe_resref", "Probe");

            var act = () => session.Document.Root.Add("ProbeField", JsonGffField.CreateList());

            act.Should().Throw<InvalidOperationException>(
                "mutating the session's own document outside a transaction must still be refused");
        }
    }
}
