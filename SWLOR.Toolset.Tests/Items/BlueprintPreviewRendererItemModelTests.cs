using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.NWN.Formats.Plt;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// End-to-end coverage that <see cref="BlueprintPreviewRenderer.BuildModel(ResourceType, Domain.Gff.JsonGffStruct, bool)"/>
    /// actually produces geometry for a real corpus item now that <see cref="BlueprintModelResolver"/>
    /// has a <see cref="ResourceType.Uti"/> case - before this it always returned null via the
    /// resolver's default arm for every base item, composite or not.
    /// </summary>
    [NonParallelizable]
    public class BlueprintPreviewRendererItemModelTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        private static BlueprintPreviewRenderer BuildRenderer() => BuildRenderer(out _);

        private static BlueprintPreviewRenderer BuildRenderer(out ResourceIndex index)
        {
            var twoDa = new TwoDaService(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_2da"));
            var tlk = TlkService.Load(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json"));
            var resourceIndex = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"));
            resourceIndex.EnsureInitialized();
            index = resourceIndex;

            // BuildModel(type, root) never touches the workspace for a Uti - only creature armor
            // resolution does - so the context is never opened.
            var context = new WorkspaceContext(
                path => new ModuleWorkspace(path, resourceIndex),
                new OutputLogService());

            return new BlueprintPreviewRenderer(
                context, resourceIndex, baseItems: new BaseItemIconService(twoDa), twoDa: twoDa, tlk: tlk);
        }

        [Test]
        public void CompositeLightsaber_BuildModel_ProducesMergedGeometryFromItsThreeParts()
        {
            var root = CorpusItem("bobsaber");
            var renderer = BuildRenderer();

            var model = renderer.BuildModel(ResourceType.Uti, root);

            model.Should().NotBeNull("bobsaber's composite parts (wswglsbr_b_032/_m_011/_t_014) exist in sw_weapon");
            model!.Meshes.Should().NotBeEmpty();
        }

        [Test]
        public void ArmorCarriesItsDyeChoicesOnTheModelSoTheViewportCanColourThem()
        {
            // A PLT is not a picture until its layers are coloured. The 2D icon passed the dye
            // indices straight to the texture cache, but the 3D viewport only ever sees the model -
            // so with nothing on the model, every dyed surface drew at the palette's default row and
            // changing a dye channel did nothing at all in the viewport.
            var root = CorpusItem("adren_harness");
            var renderer = BuildRenderer();

            var store = new ItemValueStore(root);
            store.SetInteger(BehaviorFieldStorage.Field, "Cloth1Color", Domain.Gff.GffFieldType.Byte, 3);
            store.SetInteger(BehaviorFieldStorage.Field, "Metal1Color", Domain.Gff.GffFieldType.Byte, 7);
            var model = renderer.BuildModel(ResourceType.Uti, root);

            model.Should().NotBeNull();
            model!.LayerColorIndices.Should().NotBeEmpty("the mannequin's dyed layers need their palette rows");
            model.LayerColorIndices[PltLayers.Cloth1].Should().Be(3);
            model.LayerColorIndices[PltLayers.Metal1].Should().Be(7);

            store.SetInteger(BehaviorFieldStorage.Field, "Cloth1Color", Domain.Gff.GffFieldType.Byte, 11);
            renderer.BuildModel(ResourceType.Uti, root)!
                .LayerColorIndices[PltLayers.Cloth1].Should().Be(11, "a dye edit reaches the model");
        }


        [Test]
        public void AnItemsUnspecifiedLayersMatchAuroraAtRowZero()
        {
            // An item names no skin, hair or tattoo colour because it has no wearer, and Aurora's
            // item preview shows those layers at palette row 0. Substituting a mid-palette row to
            // make them "nicer" turned the head and hands red: a palette row is a gradient across
            // its columns, and only the brightest column is the pale tone that choice was based on.
            var root = CorpusItem("adren_harness");
            var renderer = BuildRenderer();

            var model = renderer.BuildModel(ResourceType.Uti, root);

            model.Should().NotBeNull();
            model!.LayerColorIndices[PltLayers.Skin].Should().Be(0);
            model.LayerColorIndices[PltLayers.Hair].Should().Be(0);
            model.LayerColorIndices[PltLayers.Tattoo1].Should().Be(0);
        }

        [Test]
        public void ChimedClothesRobeLayersCoatIdleOverWearerBind()
        {
            var renderer = BuildRenderer(out var index);

            var model = renderer.BuildModel(ResourceType.Uti, CorpusItem("chimedclothes"));

            model.Should().NotBeNull();
            var renderedRobe = model!.Meshes
                .Should().ContainSingle(mesh =>
                    mesh.NodeName.Equals("Box01", StringComparison.OrdinalIgnoreCase) &&
                    mesh.TextureName.Equals("pmh0_robe010", StringComparison.OrdinalIgnoreCase))
                .Subject;
            renderedRobe.Transform.Should().Be(Matrix4x4.Identity,
                "a skinmesh is baked into model space rather than moved as one rigid panel");
            model.Meshes.Should().OnlyContain(mesh => mesh.PoseFrames.Count > 1,
                "Aurora's armor item window plays one shared coat idle across the assembled character");
            renderedRobe.PosePositions.Should().HaveCount(renderedRobe.PoseFrames.Count);
            renderedRobe.Positions.Should().Equal(renderedRobe.PosePositions[^1],
                "the still thumbnail and bounds use the final animated pose");

            var source = new MdlReader().Parse(File.ReadAllBytes(
                Path.Combine(RepoRoot, "SWLOR_Haks", "sw_pt_robe", "pmh0_robe010.mdl")));
            var sourceRobe = source.GetMeshNodes()
                .OfType<MdlSkinmeshNode>()
                .Single(mesh => mesh.Name.Equals("Box01", StringComparison.OrdinalIgnoreCase));
            sourceRobe.Vertices.Should().HaveCount(renderedRobe.VertexCount);

            MdlModel? LoadModel(string resRef)
            {
                if (!index.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out var handle))
                    return null;

                return new MdlReader().Parse(handle.GetBytes());
            }

            var wearer = LoadModel("pmh0")!;
            var layeredBind = MdlAnimationPose.BindPose(source).ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
            foreach (var (name, node) in MdlAnimationPose.BindPose(wearer))
                layeredBind[name] = node;

            var layeredFrames = MdlAnimationPose.SampleIdleFrames(source, LoadModel, layeredBind)
                .Select(frame => frame.Pose)
                .ToList();
            var layeredPoseRobe = MdlMeshBuilder.Build(source, layeredFrames).Meshes
                .Single(mesh =>
                    mesh.NodeName.Equals("Box01", StringComparison.OrdinalIgnoreCase) &&
                    mesh.TextureName.Equals("pmh0_robe010", StringComparison.OrdinalIgnoreCase));

            var unlayeredCoatFrames = MdlAnimationPose.SampleIdleFrames(source, LoadModel)
                .Select(frame => frame.Pose)
                .ToList();
            var unlayeredCoatRobe = MdlMeshBuilder.Build(source, unlayeredCoatFrames).Meshes
                .Single(mesh =>
                    mesh.NodeName.Equals("Box01", StringComparison.OrdinalIgnoreCase) &&
                    mesh.TextureName.Equals("pmh0_robe010", StringComparison.OrdinalIgnoreCase));

            var wearerFrames = MdlAnimationPose.SampleIdleFrames(wearer, LoadModel)
                .Select(frame => frame.Pose)
                .ToList();
            var wearerPoseRobe = MdlMeshBuilder.Build(source, wearerFrames).Meshes
                .Single(mesh =>
                    mesh.NodeName.Equals("Box01", StringComparison.OrdinalIgnoreCase) &&
                    mesh.TextureName.Equals("pmh0_robe010", StringComparison.OrdinalIgnoreCase));

            renderedRobe.PosePositions.Should().HaveCount(layeredPoseRobe.PosePositions.Count);
            for (var frame = 0; frame < layeredPoseRobe.PosePositions.Count; frame++)
            {
                renderedRobe.PosePositions[frame].Should().Equal(
                    layeredPoseRobe.PosePositions[frame],
                    "the coat overlay keeps its helper tracks while shared bones inherit the wearer bind");
            }

            renderedRobe.PosePositions[0].Should().NotEqual(
                unlayeredCoatRobe.PosePositions[0],
                "sampling missing coat channels from the robe's zeroed bind collapses the lower body");
            renderedRobe.PosePositions[0].Should().NotEqual(
                wearerPoseRobe.PosePositions[0],
                "falling back to the plain wearer leaves coat-only helpers at bind and cuts the " +
                "weighted waist through the rigid chest sash");
            renderedRobe.PoseNormals.Should().HaveCount(renderedRobe.PosePositions.Count);
            renderedRobe.PoseNormals.Should().OnlyContain(frame =>
                    frame.Length == renderedRobe.Positions.Length,
                "Aurora generates normals for a robe even when its ASCII skinmesh omits them");
            renderedRobe.PosePositions[0].Should().Equal(
                layeredPoseRobe.PosePositions[0],
                "Aurora preserves the robe's authored weighted surface; inflating it along generated " +
                "normals creates shoulder wedges that are absent from the original item preview");
            sourceRobe.Normals.Should().HaveCount(sourceRobe.Vertices.Length,
                "ASCII robes receive the same smoothing-group normal pass as Aurora's compiler");
            sourceRobe.Normals.Should().OnlyContain(normal =>
                    float.IsFinite(normal.X) &&
                    float.IsFinite(normal.Y) &&
                    float.IsFinite(normal.Z) &&
                    MathF.Abs(normal.LengthSquared() - 1f) < 0.0001f,
                "the compiler-derived shell directions remain normalized");

            var bounds = model.ComputeBounds();
            bounds.Should().NotBeNull();
            bounds!.Value.Minimum.Z.Should().BeLessThan(0.2f,
                "the feet remain at ground level instead of being pulled into the torso");
            (bounds.Value.Maximum.Z - bounds.Value.Minimum.Z).Should().BeGreaterThan(1.5f,
                "the assembled mannequin retains normal human height");

            var firstFrame = renderedRobe.PosePositions[0];
            var maximumAnimatedDisplacement = renderedRobe.PosePositions
                .Skip(1)
                .SelectMany(frame => frame.Select(
                    (value, index) => MathF.Abs(value - firstFrame[index])))
                .Max();

            maximumAnimatedDisplacement.Should().BeGreaterThan(0.001f,
                "weighted sleeves and coat panels must advance instead of remaining frozen");
            renderedRobe.PosePositions.SelectMany(frame => frame).Should().OnlyContain(value =>
                float.IsFinite(value) && MathF.Abs(value) < 3f);

            model.Meshes.Should().NotContain(mesh =>
                    mesh.NodeName.Contains("bicep", StringComparison.OrdinalIgnoreCase) ||
                    mesh.NodeName.Contains("forearm", StringComparison.OrdinalIgnoreCase),
                "parts_robe.2da row 10 replaces the ordinary arm segments with the coat sleeves");
            model.Meshes.Should().Contain(mesh =>
                    mesh.NodeName.Contains("hand", StringComparison.OrdinalIgnoreCase),
                "row 10 keeps the hands visible below the sleeves");
            model.Meshes.Should().Contain(mesh =>
                    mesh.TextureName.Equals("pmh0_handl001", StringComparison.OrdinalIgnoreCase));
            model.Meshes.Should().Contain(mesh =>
                    mesh.TextureName.Equals("pmh0_handr001", StringComparison.OrdinalIgnoreCase),
                "NULL in the standard hand MDLs means to keep their stamped body-part PLTs");
            model.Meshes.Should().Contain(mesh =>
                    mesh.TextureName.Equals("pmh0_chest186", StringComparison.OrdinalIgnoreCase),
                "row 10 keeps the selected armor chest instead of treating the coat as a full-body robe");
        }

        private static Domain.Gff.JsonGffStruct CorpusItem(string resRef) =>
            new ModuleWorkspace(CorpusLocator.ModuleDirectory).LoadBlueprint(ResourceType.Uti, resRef).Document.Root;
    }
}
