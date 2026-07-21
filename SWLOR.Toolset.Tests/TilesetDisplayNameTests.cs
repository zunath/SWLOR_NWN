using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the tileset picker's labels: the header-only [GENERAL] read
    /// (<see cref="SetFileParser.ParseHeader"/>) and <see cref="TilesetCatalog.GetDisplayLabel"/>.
    /// Needs no NWN install - every tileset exercised here ships in the SWLOR haks.
    /// </summary>
    public class TilesetDisplayNameTests
    {
        private static string RepoRoot
        {
            get
            {
                var c = new DirectoryInfo(AppContext.BaseDirectory);
                while (c != null)
                {
                    if (File.Exists(Path.Combine(c.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(c.FullName, "SWLOR_Haks")))
                        return c.FullName;
                    c = c.Parent;
                }
                throw new DirectoryNotFoundException("repo root not found");
            }
        }

        private static TilesetCatalog CreateCatalog() => new(
            ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks")));

        [Test]
        public void ParseHeader_StopsAtTheEndOfTheGeneralBlock()
        {
            // The terrain blocks that follow [GENERAL] also have "Name=" keys, so a reader that did
            // not stop would report the first TERRAIN's name as the tileset name.
            var set = Encoding.ASCII.GetBytes(string.Join("\n",
                "; comment",
                "[GENERAL]",
                "Name=TMS01",
                "DisplayName=2325",
                "",
                "[GRASS]",
                "Grass=1",
                "",
                "[TERRAIN0]",
                "Name=Grass"));

            var header = SetFileParser.ParseHeader(set);

            header.Name.Should().Be("TMS01", "the terrain block's own Name must not leak into the header");
            header.DisplayNameStrRef.Should().Be(2325);
            header.UnlocalizedName.Should().BeEmpty();
        }

        [Test]
        public void ParseHeader_ReadsUnlocalizedName()
        {
            var set = Encoding.ASCII.GetBytes("[GENERAL]\nName=ZTD01\nUnlocalizedName=[CEP] Desert\n");

            var header = SetFileParser.ParseHeader(set);

            header.UnlocalizedName.Should().Be("[CEP] Desert");
        }

        [Test]
        public void ParseHeader_MissingGeneralBlock_YieldsEmptyHeaderRatherThanThrowing()
        {
            var header = SetFileParser.ParseHeader(Encoding.ASCII.GetBytes("[TILES]\nCount=0\n"));

            header.Name.Should().BeEmpty();
            header.UnlocalizedName.Should().BeEmpty();
            header.DisplayNameStrRef.Should().Be(-1);
        }

        [Test]
        public void GetDisplayLabel_UsesTheReadableNameFromTheRealTileset()
        {
            var catalog = CreateCatalog();

            // ztd01 declares UnlocalizedName=[CEP] Desert.
            catalog.GetDisplayLabel("ztd01").Should().Be("ztd01 - [CEP] Desert");
        }

        [Test]
        public void GetDisplayLabel_FallsBackToTheBareResRef_WhenTheNameAddsNothing()
        {
            var catalog = CreateCatalog();

            // tms01 declares no UnlocalizedName and Name=TMS01, which is just the resref in caps -
            // repeating it would give the useless "tms01 - TMS01".
            catalog.GetDisplayLabel("tms01").Should().Be("tms01");
        }

        [Test]
        public void GetDisplayLabel_UnknownTileset_ReturnsTheResRef()
        {
            CreateCatalog().GetDisplayLabel("no_such_tileset").Should().Be("no_such_tileset");
        }

        [Test]
        public void EveryTilesetInTheHaks_GetsANonEmptyLabel()
        {
            var catalog = CreateCatalog();
            var names = catalog.GetTilesetNames();

            names.Should().NotBeEmpty();
            foreach (var name in names)
                catalog.GetDisplayLabel(name).Should().NotBeNullOrWhiteSpace()
                    .And.StartWith(name, "every picker entry must still show the resref that gets written to the area");
        }
    }
}
