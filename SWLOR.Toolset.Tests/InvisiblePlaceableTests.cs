using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests;

[TestFixture]
public class InvisiblePlaceableTests
{
    private static MdlTrimeshNode Surface(string name, bool render = false) => new()
    {
        Name = name,
        Render = render,
        Vertices = [new(0, 0, 0), new(2, 0, 0), new(0, 2, 0)],
        Faces = [new() { VertexIndex0 = 0, VertexIndex1 = 1, VertexIndex2 = 2 }]
    };

    private static MdlModel Model(params MdlNode[] children)
    {
        var root = new MdlNode { Name = "root" };
        foreach (var child in children)
            root.Children.Add(child);
        return new() { Name = "invisible_probe", GeometryRoot = root };
    }

    [Test]
    public void HiddenPlaceableSurface_IsEditableWithoutChangingOrdinaryGeometry()
    {
        var source = Model(Surface("selection_volume"));
        var editor = MdlMeshBuilder.BuildPlaceableEditor(source);
        editor.IsInvisiblePlaceableGeometry.Should().BeTrue();
        editor.IsDoorTransitionGeometry.Should().BeFalse();
        editor.Meshes.Should().ContainSingle().Which.NodeName.Should().Be("selection_volume");
        MdlMeshBuilder.Build(source).Meshes.Should().BeEmpty();
        MdlMeshBuilder.BuildPlaceablePreview(source).IsInvisiblePlaceableGeometry.Should().BeTrue();

        var instance = new InstanceMarker
        {
            Kind = InstanceMarkerKind.Placeable, Position = Vector3.Zero,
            Orientation = Vector2.UnitX, Model = editor
        };
        AreaPicking.DrawsAsModel(instance, true).Should().BeTrue();
        AreaPicking.PickInstance(new PickRay(new(1.2f, 0.2f, 3f), -Vector3.UnitZ), instance, true)
            .Should().NotBeNull("the authored surface extends beyond the old pyramid marker");
    }

    [Test]
    public void VisiblePropsAndEffects_DoNotExposeHiddenHelpers()
    {
        var visible = MdlMeshBuilder.BuildPlaceableEditor(Model(Surface("visible", true), Surface("helper")));
        visible.IsInvisiblePlaceableGeometry.Should().BeFalse();
        visible.Meshes.Should().ContainSingle().Which.NodeName.Should().Be("visible");
        var effect = MdlMeshBuilder.BuildPlaceableEditor(Model(Surface("helper"), new MdlEmitterNode { Name = "effect" }));
        effect.IsInvisiblePlaceableGeometry.Should().BeFalse();
        effect.Meshes.Should().BeEmpty();
    }

    [Test]
    public void CollisionPlaceholdersAndInvalidFaces_AreNotSelectionSurfaces()
    {
        var collision = Surface("collision");
        collision.IsWalkmesh = true;
        var invalid = Surface("invalid");
        invalid.Faces[0].VertexIndex2 = 99;
        var editor = MdlMeshBuilder.BuildPlaceableEditor(Model(collision, Surface("sam"), invalid));
        editor.IsInvisiblePlaceableGeometry.Should().BeFalse();
        editor.Meshes.Should().BeEmpty();
    }

    [TestCase("plc_u02")]
    [TestCase("plc_inv")]
    public void NativeInvisiblePlaceables_KeepTheirRealSelectionVolumes(string resref)
    {
        var install = NwnInstallLocator.Locate();
        if (install == null)
        {
            Assert.Ignore("Native model check requires a local NWN installation.");
            return;
        }
        var resources = new ResourceIndex(KeyBifCatalog.Load(Path.Combine(install, "data")), []);
        var cache = new TileModelCache(resources);
        var editor = cache.GetOrBuildPlaceableEditor(resref);
        editor.Should().NotBeNull();
        editor!.IsInvisiblePlaceableGeometry.Should().BeTrue();
        editor.Meshes.Should().NotBeEmpty();
        editor.ComputeBounds().Should().NotBeNull();
        cache.GetOrBuild(resref)!.Meshes.Should().BeEmpty("the editor cache must not expose geometry in ordinary rendering");
        var preview = cache.GetOrBuildPlaceablePreview(resref);
        preview!.IsInvisiblePlaceableGeometry.Should().BeTrue();
        ThumbnailRenderer.Render(preview, 64).Should().NotBeNull();
    }

    [TestCase("pw_sc_jeditrial")]
    [TestCase("pw_sc_velescmd")]
    [TestCase("pw_sc_korrforge")]
    [TestCase("pw_sc_canyonpit")]
    [TestCase("pw_sc_qioncore")]
    [TestCase("pw_sc_sithritual")]
    [TestCase("pw_sc_repubcmd")]
    [TestCase("pw_sc_tarnalpha")]
    public void BossActivators_ResolveAsInvisiblePlaceablesInTheAreaEditor(string area)
    {
        var install = NwnInstallLocator.Locate();
        if (install == null) { Assert.Ignore("Requires native invisible-object models."); return; }
        var resources = new ResourceIndex(KeyBifCatalog.Load(Path.Combine(install, "data")), []);
        var cache = new TileModelCache(resources);
        var haks = Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks");
        var appearances = new PlaceableAppearanceService(new TwoDaService(Path.Combine(haks, "sw_2da")),
            TlkService.Load(Path.Combine(haks, "sw_tlk", "sw_tlk.tlk.json")));
        var git = GitDocument.Load(Path.Combine(CorpusLocator.ModuleDirectory, "git", area + ".git.json"));
        var activators = git.Placeables.Where(p => p.GetStringOrNull("OnUsed") == "quest_enc").ToList();
        activators.Should().HaveCount(3);
        foreach (var activator in activators)
        {
            activator.GetIntOrNull("Useable").Should().Be(1);
            activator.GetIntOrNull("Static").Should().Be(0);
            var marker = AreaSceneBuilder.BuildInstanceMarker(ResourceType.Utp, activator, cache, appearances);
            marker.Model!.IsInvisiblePlaceableGeometry.Should().BeTrue();
            AreaPicking.DrawsAsModel(marker, true).Should().BeTrue();
            marker.Kind.Should().Be(InstanceMarkerKind.Placeable);
        }
    }
}
