using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Seeding the sidecar from an existing palette. The JSON here mirrors the real shape found in this
    /// module's <c>itp/</c> files: an unnamed wrapper struct at the root, categories labelled by STRREF
    /// rather than NAME, and leaves carrying both NAME and RESREF.
    /// </summary>
    [TestFixture]
    public class ItpCategoryImporterTests
    {
        private static ItpDocument Parse(string json) =>
            ItpDocument.Parse(Encoding.UTF8.GetBytes(json));

        private static ItpDocument RealisticPalette() => Parse("""
        {
          "__data_type": "ITP ",
          "MAIN": { "type": "list", "value": [
            { "__struct_id": 0, "LIST": { "type": "list", "value": [
              { "__struct_id": 0,
                "ID": { "type": "byte", "value": 0 },
                "STRREF": { "type": "dword", "value": 6688 },
                "LIST": { "type": "list", "value": [
                  { "__struct_id": 0,
                    "NAME": { "type": "cexostring", "value": "Jump to Creature" },
                    "RESREF": { "type": "resref", "value": "_mdrn_dt_jumpto" } }
                ] } },
              { "__struct_id": 0,
                "NAME": { "type": "cexostring", "value": "Bulkheads" },
                "LIST": { "type": "list", "value": [
                  { "__struct_id": 0,
                    "NAME": { "type": "cexostring", "value": "Bulkhead Door" },
                    "RESREF": { "type": "resref", "value": "_mdrn_dt_slid006" } },
                  { "__struct_id": 0,
                    "NAME": { "type": "cexostring", "value": "Narrow" },
                    "LIST": { "type": "list", "value": [
                      { "__struct_id": 0,
                        "RESREF": { "type": "resref", "value": "_mdrn_dt_garage6" } }
                    ] } }
                ] } }
            ] } }
          ] }
        }
        """);

        [Test]
        public void The_Unnamed_Root_Wrapper_Is_Hoisted_Not_Imported_As_A_Folder()
        {
            var section = ItpCategoryImporter.Import(RealisticPalette());

            section.Folders.Select(f => f.Name).Should().BeEquivalentTo(new[] { "Category 6688", "Bulkheads" });
        }

        [Test]
        public void Leaves_Become_Members_Of_The_Enclosing_Folder()
        {
            var section = ItpCategoryImporter.Import(RealisticPalette());

            section.Find("Bulkheads")!.Members.Should().Equal("_mdrn_dt_slid006");
            section.Find("Bulkheads", "Narrow")!.Members.Should().Equal("_mdrn_dt_garage6");
        }

        [Test]
        public void StrRef_Categories_Use_The_Supplied_Resolver()
        {
            var section = ItpCategoryImporter.Import(
                RealisticPalette(),
                strRef => strRef == 6688 ? "Special" : null);

            section.Find("Special")!.Members.Should().Equal("_mdrn_dt_jumpto");
        }

        [Test]
        public void StrRef_Categories_Fall_Back_To_A_Renameable_Placeholder()
        {
            var section = ItpCategoryImporter.Import(RealisticPalette(), _ => null);

            section.Find("Category 6688").Should().NotBeNull(
                because: "an unresolved category still has to be visible and renameable");
        }

        [Test]
        public void Empty_Categories_Are_Not_Imported()
        {
            var section = ItpCategoryImporter.Import(Parse("""
            {
              "__data_type": "ITP ",
              "MAIN": { "type": "list", "value": [
                { "__struct_id": 0, "NAME": { "type": "cexostring", "value": "Filled" },
                  "LIST": { "type": "list", "value": [
                    { "__struct_id": 0, "RESREF": { "type": "resref", "value": "a_thing" } }
                  ] } },
                { "__struct_id": 0, "NAME": { "type": "cexostring", "value": "Hollow" },
                  "LIST": { "type": "list", "value": [
                    { "__struct_id": 0, "NAME": { "type": "cexostring", "value": "Also hollow" },
                      "LIST": { "type": "list", "value": [] } }
                  ] } }
              ] }
            }
            """));

            section.Folders.Select(f => f.Name).Should().Equal("Filled");
        }

        [Test]
        public void Nodes_Marked_DeleteMe_Are_Skipped()
        {
            var section = ItpCategoryImporter.Import(Parse("""
            {
              "__data_type": "ITP ",
              "MAIN": { "type": "list", "value": [
                { "__struct_id": 0, "NAME": { "type": "cexostring", "value": "Keep" },
                  "LIST": { "type": "list", "value": [
                    { "__struct_id": 0, "RESREF": { "type": "resref", "value": "kept" } },
                    { "__struct_id": 0, "DELETE_ME": { "type": "byte", "value": 1 },
                      "RESREF": { "type": "resref", "value": "dropped" } }
                  ] } }
              ] }
            }
            """));

            section.Find("Keep")!.Members.Should().Equal("kept");
        }

        /// <summary>
        /// The seed has to survive every palette this module actually ships, including the ones with
        /// non-UTF8 bytes and base-game STRREF categories. Import is the only path that reads these
        /// files, so if it throws here a builder starts from an empty tree with no explanation.
        /// </summary>
        [Test]
        public void Imports_Every_Palette_In_The_Module()
        {
            var itpDirectory = Path.Combine(CorpusLocator.ModuleDirectory, "itp");
            var files = Directory.GetFiles(itpDirectory, "*.itp.json");
            files.Should().NotBeEmpty();

            var imported = 0;
            foreach (var file in files)
            {
                var section = ItpCategoryImporter.Import(ItpDocument.Load(file));

                foreach (var folder in section.AllFolders())
                {
                    folder.Name.Should().NotBeNullOrWhiteSpace();
                    folder.MembersIncludingDescendants.Should().NotBeEmpty(
                        because: $"{Path.GetFileName(file)} should not import empty branches");
                }

                imported += section.AllFolders().Count();
            }

            imported.Should().BeGreaterThan(0,
                because: "at least one of the module's palettes has categories worth seeding");
        }

        [Test]
        public void An_Empty_Palette_Imports_As_An_Empty_Section()
        {
            var section = ItpCategoryImporter.Import(Parse("""
                { "__data_type": "ITP ", "MAIN": { "type": "list", "value": [] } }
                """));

            section.Folders.Should().BeEmpty();
        }
    }
}
