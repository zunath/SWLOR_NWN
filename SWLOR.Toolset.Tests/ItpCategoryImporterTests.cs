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

        /// <summary>
        /// CategoryService.RepairPlaceholderNames must be able to tell a real unresolved placeholder
        /// apart from a folder a builder happened to name "Category 7" - and the name text alone cannot
        /// do that, since both look identical. This marker is the only place that distinction is made.
        /// </summary>
        [Test]
        public void An_Unresolved_StrRef_Category_Is_Marked_As_A_Placeholder()
        {
            var section = ItpCategoryImporter.Import(RealisticPalette(), _ => null);

            section.Find("Category 6688")!.IsUnresolvedPlaceholder.Should().BeTrue();
        }

        [Test]
        public void A_Resolved_StrRef_Category_Is_Not_Marked_As_A_Placeholder()
        {
            var section = ItpCategoryImporter.Import(
                RealisticPalette(), strRef => strRef == 6688 ? "Special" : null);

            section.Find("Special")!.IsUnresolvedPlaceholder.Should().BeFalse();
        }

        [Test]
        public void A_Category_Named_Directly_Is_Never_Marked_As_A_Placeholder()
        {
            var section = ItpCategoryImporter.Import(RealisticPalette(), _ => null);

            section.Find("Bulkheads")!.IsUnresolvedPlaceholder.Should().BeFalse();
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

        /// <summary>
        /// Leaf names are the only names the base game's blueprints have - they are not in the module,
        /// so the module catalog cannot supply one. Discarding them left the Standard palette showing a
        /// bare resref for nearly every entry.
        /// </summary>
        [Test]
        public void Leaf_Names_Are_Reported_By_ResRef()
        {
            ItpCategoryImporter.Import(RealisticPalette(), out var names, strRef => $"Strref {strRef}");

            names["_mdrn_dt_slid006"].Should().Be("Bulkhead Door");
            names["_mdrn_dt_jumpto"].Should().Be("Jump to Creature");
        }

        /// <summary>A leaf may label itself by STRREF instead of NAME, exactly as its parent can.</summary>
        [Test]
        public void A_Leaf_Named_By_StrRef_Is_Resolved()
        {
            ItpCategoryImporter.Import(Parse("""
            {
              "__data_type": "ITP ",
              "MAIN": { "type": "list", "value": [
                { "__struct_id": 0, "NAME": { "type": "cexostring", "value": "Doors" },
                  "LIST": { "type": "list", "value": [
                    { "__struct_id": 0, "STRREF": { "type": "dword", "value": 5555 },
                      "RESREF": { "type": "resref", "value": "door01" } }
                  ] } }
              ] }
            }
            """), out var names, strRef => strRef == 5555 ? "Castle Gate" : null);

            names["door01"].Should().Be("Castle Gate");
        }

        [Test]
        public void An_Empty_Palette_Imports_As_An_Empty_Section()
        {
            var section = ItpCategoryImporter.Import(Parse("""
                { "__data_type": "ITP ", "MAIN": { "type": "list", "value": [] } }
                """));

            section.Folders.Should().BeEmpty();
        }

        /// <summary>
        /// The base game's own item palette ships categories called "Skin/Hide" and
        /// "Crafting/Tradeskill Material", and a folder name may not hold the path separator. Importing
        /// them verbatim threw, so seeding a module from that palette wrote a name the reader then refused.
        /// </summary>
        [Test]
        public void A_Palette_Category_Holding_The_Path_Separator_Is_Repaired_On_Import()
        {
            var section = ItpCategoryImporter.Import(Parse("""
            {
              "__data_type": "ITP ",
              "MAIN": { "type": "list", "value": [
                { "__struct_id": 0, "NAME": { "type": "cexostring", "value": "Skin/Hide" },
                  "LIST": { "type": "list", "value": [
                    { "__struct_id": 0, "RESREF": { "type": "resref", "value": "leather_hide" } }
                  ] } }
              ] }
            }
            """));

            var folder = section.Folders.Should().ContainSingle().Subject;
            folder.Name.Should().Be("Skin-Hide");
            folder.Members.Should().Equal("leather_hide");
        }

        [Test]
        public void A_Category_Named_By_StrRef_Is_Repaired_Too()
        {
            var section = ItpCategoryImporter.Import(
                Parse("""
                {
                  "__data_type": "ITP ",
                  "MAIN": { "type": "list", "value": [
                    { "__struct_id": 0, "STRREF": { "type": "dword", "value": 6782 },
                      "LIST": { "type": "list", "value": [
                        { "__struct_id": 0, "RESREF": { "type": "resref", "value": "ore" } }
                      ] } }
                  ] }
                }
                """),
                strRef => strRef == 6782 ? "Crafting/Tradeskill Material" : null);

            section.Folders.Should().ContainSingle()
                .Which.Name.Should().Be("Crafting-Tradeskill Material");
        }
    }
}
