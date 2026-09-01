using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="AreaTemplateFactory"/>: the tile-struct shape, reshaping a loaded
    /// template .are into a fresh solid-filled area (with round-trip fidelity), and idempotent
    /// registration in module.ifo's Mod_Area_list. Uses the repository's real template/ifo so the
    /// corpus field shapes are exercised, mutating only in memory (never writing back).
    /// </summary>
    public class AreaTemplateFactoryTests
    {
        [Test]
        public void CreateTileStruct_HasCorpusFieldShape()
        {
            var tile = AreaTemplateFactory.CreateTileStruct(202, 3, heightLevel: 2);

            Encoding.ASCII.GetString(tile.RawStructId!).Should().Be("1", "Tile_List entries use __struct_id 1");

            int Field(string name) => tile.TryGet(name, out var f) ? (int)f.GetInteger() : int.MinValue;
            Field("Tile_ID").Should().Be(202);
            Field("Tile_Orientation").Should().Be(3);
            Field("Tile_Height").Should().Be(2);
            Field("Tile_MainLight1").Should().Be(0);
            Field("Tile_MainLight2").Should().Be(0);
            Field("Tile_SrcLight1").Should().Be(0);
            Field("Tile_SrcLight2").Should().Be(0);
            Field("Tile_AnimLoop1").Should().Be(1, "the toolset default enables animation loop 1");
            Field("Tile_AnimLoop2").Should().Be(1);
            Field("Tile_AnimLoop3").Should().Be(1);
        }

        [Test]
        public void PopulateNewArea_RewritesIdentityAndFillsGrid()
        {
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            var (are, _, _) = workspace.LoadArea("area_template");
            var fogBefore = are.FogClipDist; // an incidental template field that must survive untouched

            AreaTemplateFactory.PopulateNewArea(are, "wp73_probe", "Probe Area", "tms01", 3, 4, fillTileId: 12);

            are.Width.Should().Be(3);
            are.Height.Should().Be(4);
            are.Tileset.Should().Be("tms01");
            are.Tag.Should().Be("wp73_probe");
            are.Name.Text.Should().Be("Probe Area");
            are.Fields.TryGet("ResRef", out var resRefField).Should().BeTrue();
            resRefField.GetString().Should().Be("wp73_probe");
            are.FogClipDist.Should().Be(fogBefore, "an unrelated template field must flow through untouched");

            are.Tiles.Should().HaveCount(12, "Tile_List is regenerated to width*height cells");
            foreach (var tile in are.Tiles)
            {
                (tile.TryGet("Tile_ID", out var id) ? (int)id.GetInteger() : -1).Should().Be(12);
                (tile.TryGet("Tile_Orientation", out var o) ? (int)o.GetInteger() : -1).Should().Be(0);
            }
        }

        [Test]
        public void PopulateNewArea_RoundTripsThroughSerialization()
        {
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            var (are, _, _) = workspace.LoadArea("area_template");

            AreaTemplateFactory.PopulateNewArea(are, "wp73_probe", "Probe Area", "tms01", 2, 2, fillTileId: 7);

            var reloaded = AreDocument.Parse(are.ToBytes());
            reloaded.Width.Should().Be(2);
            reloaded.Height.Should().Be(2);
            reloaded.Tileset.Should().Be("tms01");
            reloaded.Tiles.Should().HaveCount(4);
            reloaded.Name.Text.Should().Be("Probe Area");
        }

        [Test]
        public void AddAreaToModule_AppendsOnce_AndIsIdempotent()
        {
            var ifoPath = Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");
            var ifo = IfoDocument.Load(ifoPath);
            var countBefore = ifo.AreaList.Count;

            const string newResRef = "wp73_probe_area";
            AreaTemplateFactory.AddAreaToModule(ifo, newResRef).Should().BeTrue("a not-yet-listed area is appended");
            ifo.AreaResRefs.Should().Contain(newResRef);
            ifo.AreaList.Count.Should().Be(countBefore + 1);

            var appended = ifo.AreaList[^1];
            Encoding.ASCII.GetString(appended.RawStructId!).Should().Be("6", "Mod_Area_list entries use __struct_id 6");

            AreaTemplateFactory.AddAreaToModule(ifo, newResRef).Should().BeFalse("a second add for the same resref is a no-op");
            ifo.AreaList.Count.Should().Be(countBefore + 1, "no duplicate entry is created");
        }

        [Test]
        public void RemoveAreaFromModule_RemovesEveryDuplicateRegistration()
        {
            var ifoPath = Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");
            var ifo = IfoDocument.Load(ifoPath);
            const string resRef = "delete_probe";
            AreaTemplateFactory.AddAreaToModule(ifo, resRef).Should().BeTrue();

            // Reproduce a hand-edited/legacy duplicate. Delete must not leave a second registration
            // pointing at an ARE/GIT/GIC triplet that no longer exists.
            var list = ifo.Fields.GetOrAddList("Mod_Area_list");
            list.Add(list.Single(entry => entry.GetStringOrNull("Area_Name") == resRef));

            AreaTemplateFactory.RemoveAreaFromModule(ifo, resRef).Should().Be(2);
            ifo.AreaResRefs.Should().NotContain(resRef);
            AreaTemplateFactory.RemoveAreaFromModule(ifo, resRef).Should().Be(0);
        }
    }
}
