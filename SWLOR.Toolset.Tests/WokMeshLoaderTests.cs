using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP6.1 <see cref="WokMeshLoader"/>.
    ///
    /// <para>
    /// IMPORTANT EMPIRICAL FINDING, confirmed while building this coverage: every real .wok
    /// resource actually present in this project's corpus - both the SWLOR custom hak-source
    /// folders (e.g. <c>SWLOR_Haks\sw_t_modint\tfb01_p05_01.wok</c>) and a local NWN:EE install's
    /// retail <c>nwn_base.key</c>/<c>.bif</c> data (e.g. <c>dag01_a01_01.wok</c>, read directly
    /// via <c>KeyBifCatalog</c>) - is plain ASCII "NWMax walkmesh" export text, never the binary
    /// "BWM V1.0" layout originally assumed for this work package. A brute-force byte search for
    /// the literal text "BWM V1.0" across every tileset .bif in a local install found zero
    /// matches. <see cref="WokMeshLoader.Parse"/> therefore implements both: an ASCII parser
    /// (the path every real resource in this project actually takes - see
    /// <see cref="Parse_RealCorpusWok_ParsesAsAsciiWalkmesh"/>) and the originally-specified
    /// binary BWM parser, kept as a defensive/forward-compatible fallback and pinned by
    /// <see cref="Parse_HandConstructedBinaryBwm_ParsesVerticesFacesAndMaterial"/> even though no
    /// real binary sample exists to verify it against.
    /// </para>
    /// </summary>
    public class WokMeshLoaderTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var hakBuilderConfig = Path.Combine(current.FullName, "Build", "hakbuilder.json");
                    var haksDirectory = Path.Combine(current.FullName, "SWLOR_Haks");
                    if (File.Exists(hakBuilderConfig) && Directory.Exists(haksDirectory))
                        return current.FullName;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        private static string HakBuilderConfigPath => Path.Combine(RepoRoot, "Build", "hakbuilder.json");
        private static string HaksDirectory => Path.Combine(RepoRoot, "SWLOR_Haks");

        private static ResourceIndex BuildHakOnlyIndex() =>
            ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);

        // ------------------------------------------------------------------------------------
        // Deterministic ASCII unit tests - pin the format every real .wok resource actually uses,
        // independent of any game files.
        // ------------------------------------------------------------------------------------

        private const string AsciiWalkmeshTemplate =
            "#MAXWALKMESH  ASCII\r\n" +
            "beginwalkmeshgeom test_tile\r\n" +
            "node aabb Object1\r\n" +
            "  parent test_tile\r\n" +
            "  position {0} {1} {2}\r\n" +
            "  orientation 0.0 0.0 0.0 0.0\r\n" +
            "  wirecolor 0.5 0.5 0.5\r\n" +
            "    verts 3\r\n" +
            "     0.0 0.0 5.0\r\n" +
            "     1.0 0.0 5.0\r\n" +
            "     0.0 1.0 5.0\r\n" +
            "    faces 1\r\n" +
            "     0 1 2  1  0 0 0  3\r\n" +
            "aabb -5.00 -5.00 0.00 5.00 5.00 5.00 -1\r\n" +
            "endnode\r\n" +
            "endwalkmeshgeom test_tile\r\n";

        private static byte[] AsciiWalkmeshBytes(float px = 0f, float py = 0f, float pz = 0f) =>
            Encoding.ASCII.GetBytes(string.Format(
                System.Globalization.CultureInfo.InvariantCulture, AsciiWalkmeshTemplate, px, py, pz));

        [Test]
        public void Parse_AsciiWalkmesh_ExtractsVerticesAndFaceWithMaterial()
        {
            var bytes = AsciiWalkmeshBytes();

            var mesh = WokMeshLoader.Parse(bytes, _ => true);

            mesh.Should().NotBeNull();
            mesh!.Vertices.Should().HaveCount(3);
            mesh.Vertices[0].Should().Be(new Vector3(0f, 0f, 5f));
            mesh.Vertices[1].Should().Be(new Vector3(1f, 0f, 5f));
            mesh.Vertices[2].Should().Be(new Vector3(0f, 1f, 5f));

            mesh.Faces.Should().HaveCount(1);
            var face = mesh.Faces[0];
            face.A.Should().Be(0);
            face.B.Should().Be(1);
            face.C.Should().Be(2);
            face.Material.Should().Be(3);
            face.Walkable.Should().BeTrue();
        }

        [Test]
        public void Parse_AsciiWalkmesh_WalkableFlagReflectsThePredicate()
        {
            var bytes = AsciiWalkmeshBytes();

            var walkableMesh = WokMeshLoader.Parse(bytes, _ => true);
            var nonWalkableMesh = WokMeshLoader.Parse(bytes, _ => false);

            walkableMesh!.Faces[0].Walkable.Should().BeTrue();
            nonWalkableMesh!.Faces[0].Walkable.Should().BeFalse();
        }

        [Test]
        public void Parse_AsciiWalkmesh_MaterialIdIsPassedToThePredicate()
        {
            var bytes = AsciiWalkmeshBytes();
            var seenMaterials = new List<int>();

            WokMeshLoader.Parse(bytes, id =>
            {
                seenMaterials.Add(id);
                return true;
            });

            seenMaterials.Should().ContainSingle().Which.Should().Be(3);
        }

        [Test]
        public void Parse_AsciiWalkmesh_NodePositionOffsetsEveryVertex()
        {
            var bytes = AsciiWalkmeshBytes(10f, 20f, 30f);

            var mesh = WokMeshLoader.Parse(bytes, _ => true);

            mesh.Should().NotBeNull();
            mesh!.Vertices[0].Should().Be(new Vector3(10f, 20f, 35f));
            mesh.Vertices[1].Should().Be(new Vector3(11f, 20f, 35f));
            mesh.Vertices[2].Should().Be(new Vector3(10f, 21f, 35f));
        }

        [Test]
        public void Parse_AsciiWalkmeshWithMultimaterialLegendAndTverts_StillParsesCorrectly()
        {
            // Real base-game samples (e.g. dag01_a01_01.wok) carry an optional "multimaterial N"
            // legend block (N bare material-name lines) and an optional "tverts" block between
            // "faces" and the "aabb" bounding-volume-tree lines. Neither carries walkmesh geometry
            // and both must be skipped without confusing the line-based parser.
            var text =
                "# Exported from NWmax 0.8 b50 at 1/1/2020 12:00:00 PM\r\n" +
                "# wok file\r\n" +
                "#\r\n" +
                "#NWmax WALKMESH  ASCII\r\n" +
                "beginwalkmeshgeom real_shape\r\n" +
                "node aabb WalkMesh\r\n" +
                "  parent real_shape\r\n" +
                "  position 0.0 0.0 0.0\r\n" +
                "  orientation 1.0 0.0 0.0 0.0\r\n" +
                "  wirecolor 0.9 0.6 0.8\r\n" +
                "  multimaterial 3\r\n" +
                "    Dirt\r\n" +
                "    Obscuring\r\n" +
                "    Grass\r\n" +
                "  ambient 0.0 0.0 0.0\r\n" +
                "  diffuse 0.6 0.34 0.16\r\n" +
                "  specular 0.0 0.0 0.0\r\n" +
                "  shininess 10.0\r\n" +
                "  bitmap Dirt\r\n" +
                "  verts 3\r\n" +
                "    0.0 0.0 2.0\r\n" +
                "    1.0 0.0 2.0\r\n" +
                "    0.0 1.0 2.0\r\n" +
                "  faces 1\r\n" +
                "    0 1 2  0  0 1 2  1\r\n" +
                "  tverts 3\r\n" +
                "    0.0 0.0 0\r\n" +
                "    1.0 0.0 0\r\n" +
                "    0.0 1.0 0\r\n" +
                "  aabb -5.0 -5.0 0.0 5.0 5.0 3.0 -1\r\n" +
                "endnode\r\n" +
                "endwalkmeshgeom real_shape\r\n";
            var bytes = Encoding.ASCII.GetBytes(text);

            var mesh = WokMeshLoader.Parse(bytes, _ => true);

            mesh.Should().NotBeNull("the multimaterial legend and tverts block must be skipped, not misparsed as geometry");
            mesh!.Vertices.Should().HaveCount(3);
            mesh.Faces.Should().HaveCount(1);
            mesh.Faces[0].Material.Should().Be(1);
        }

        // ------------------------------------------------------------------------------------
        // Deterministic binary BWM unit test - pins the byte layout from the original WP6.1
        // spec exactly, even though no real corpus sample exercises this path (see the class
        // doc comment). Guarantees the fallback parser is at least internally self-consistent.
        // ------------------------------------------------------------------------------------

        private static byte[] BuildBinaryWok(Vector3[] vertices, (int A, int B, int C, int Material)[] faces, Vector3 position = default)
        {
            const int headerSize = 112;
            var vertexOffset = headerSize;
            var vertexBytes = vertices.Length * 12;
            var faceIndicesOffset = vertexOffset + vertexBytes;
            var faceIndicesBytes = faces.Length * 12;
            var faceMaterialsOffset = faceIndicesOffset + faceIndicesBytes;
            var faceMaterialsBytes = faces.Length * 4;
            var faceNormalsOffset = faceMaterialsOffset + faceMaterialsBytes; // self-consistency: normals - materials == faceCount * 4
            var totalSize = faceNormalsOffset;

            var buffer = new byte[totalSize];
            Encoding.ASCII.GetBytes("BWM V1.0").CopyTo(buffer, 0);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x08, 4), 1); // walkmeshType: area/tile WOK
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(0x24, 4), position.X);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(0x28, 4), position.Y);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(0x2C, 4), position.Z);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x30, 4), (uint)vertices.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x34, 4), (uint)vertexOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x38, 4), (uint)faces.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x3C, 4), (uint)faceIndicesOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x40, 4), (uint)faceMaterialsOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x44, 4), (uint)faceNormalsOffset);

            for (var v = 0; v < vertices.Length; v++)
            {
                var offset = vertexOffset + v * 12;
                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset, 4), vertices[v].X);
                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 4, 4), vertices[v].Y);
                BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 8, 4), vertices[v].Z);
            }

            for (var f = 0; f < faces.Length; f++)
            {
                var indexOffset = faceIndicesOffset + f * 12;
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(indexOffset, 4), (uint)faces[f].A);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(indexOffset + 4, 4), (uint)faces[f].B);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(indexOffset + 8, 4), (uint)faces[f].C);

                var materialOffset = faceMaterialsOffset + f * 4;
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(materialOffset, 4), (uint)faces[f].Material);
            }

            return buffer;
        }

        [Test]
        public void Parse_HandConstructedBinaryBwm_ParsesVerticesFacesAndMaterial()
        {
            var vertices = new[] { new Vector3(0f, 0f, 7f), new Vector3(1f, 0f, 7f), new Vector3(0f, 1f, 7f) };
            var faces = new[] { (A: 0, B: 1, C: 2, Material: 4) };
            var bytes = BuildBinaryWok(vertices, faces);

            var walkableMesh = WokMeshLoader.Parse(bytes, _ => true);
            var nonWalkableMesh = WokMeshLoader.Parse(bytes, _ => false);

            walkableMesh.Should().NotBeNull();
            walkableMesh!.Vertices.Should().HaveCount(3);
            walkableMesh.Vertices[0].Should().Be(vertices[0]);
            walkableMesh.Vertices[1].Should().Be(vertices[1]);
            walkableMesh.Vertices[2].Should().Be(vertices[2]);

            walkableMesh.Faces.Should().HaveCount(1);
            walkableMesh.Faces[0].A.Should().Be(0);
            walkableMesh.Faces[0].B.Should().Be(1);
            walkableMesh.Faces[0].C.Should().Be(2);
            walkableMesh.Faces[0].Material.Should().Be(4);
            walkableMesh.Faces[0].Walkable.Should().BeTrue();

            nonWalkableMesh!.Faces[0].Walkable.Should().BeFalse();
        }

        [Test]
        public void Parse_HandConstructedBinaryBwm_NonZeroPositionOffsetsVertices()
        {
            var vertices = new[] { new Vector3(0f, 0f, 0f) };
            var faces = new[] { (A: 0, B: 0, C: 0, Material: 0) };
            var bytes = BuildBinaryWok(vertices, faces, position: new Vector3(2f, 3f, 4f));

            var mesh = WokMeshLoader.Parse(bytes, _ => true);

            mesh.Should().NotBeNull();
            mesh!.Vertices[0].Should().Be(new Vector3(2f, 3f, 4f));
        }

        // ------------------------------------------------------------------------------------
        // Malformed input - never throws, always degrades to null.
        // ------------------------------------------------------------------------------------

        [Test]
        public void Parse_EmptySpan_ReturnsNull()
        {
            Func<int, bool> isWalkable = _ => true;
            Action act = () => WokMeshLoader.Parse(ReadOnlySpan<byte>.Empty, isWalkable).Should().BeNull();

            act.Should().NotThrow();
        }

        [Test]
        public void Parse_TooShortGarbageSpan_ReturnsNull()
        {
            var garbage = new byte[] { 1, 2, 3, 4, 5 };

            Action act = () => WokMeshLoader.Parse(garbage, _ => true).Should().BeNull();

            act.Should().NotThrow();
        }

        [Test]
        public void Parse_WrongBinaryMagic_ReturnsNull()
        {
            var bytes = BuildBinaryWok(
                new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f) },
                new[] { (A: 0, B: 1, C: 2, Material: 0) });
            Encoding.ASCII.GetBytes("BWM V1.1").CopyTo(bytes, 0); // corrupt the version tag

            Action act = () => WokMeshLoader.Parse(bytes, _ => true).Should().BeNull();

            act.Should().NotThrow();
        }

        [Test]
        public void Parse_AsciiTextWithoutBeginWalkmeshGeomKeyword_ReturnsNull()
        {
            var bytes = Encoding.ASCII.GetBytes("this is just some unrelated text file, not a walkmesh at all\r\n");

            Action act = () => WokMeshLoader.Parse(bytes, _ => true).Should().BeNull();

            act.Should().NotThrow();
        }

        [Test]
        public void Parse_BinaryHeaderClaimingMoreDataThanBufferHolds_ReturnsNull()
        {
            // A header that parses fine but whose vertexCount would read past the actual buffer
            // must be rejected by the bounds guard, not throw an out-of-range exception.
            var bytes = BuildBinaryWok(
                new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f) },
                new[] { (A: 0, B: 1, C: 2, Material: 0) });
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x30, 4), 999_999); // lie about vertexCount

            Action act = () => WokMeshLoader.Parse(bytes, _ => true).Should().BeNull();

            act.Should().NotThrow();
        }

        // ------------------------------------------------------------------------------------
        // Real corpus probe - confirms the parser against actual .wok resources from this
        // project's own asset corpus (loose hak-source files; no NWN install required since
        // these particular resources are all loose, hak-only content).
        // ------------------------------------------------------------------------------------

        [Test]
        public void Parse_RealCorpusWok_ParsesAsAsciiWalkmesh()
        {
            var index = BuildHakOnlyIndex();
            var wokType = ResourceIdentity.TypeFromExtension("wok");

            // tfb01_l01_08 (bank.are's tile #0 model, per AreaSceneBuilderTests) has no loose
            // .wok in this repo; fall back to other known tile models per the WP6.1 brief.
            string[] candidates = { "tfb01_l01_08", "tfb01_p05_01", "ttd02_f04_01" };
            byte[]? bytes = null;
            string? resolvedResRef = null;

            foreach (var candidate in candidates)
            {
                var identity = new ResourceIdentity(candidate, wokType);
                if (index.TryLookup(identity, out var handle))
                {
                    var candidateBytes = handle.GetBytes();
                    if (candidateBytes.Length > 0)
                    {
                        bytes = candidateBytes;
                        resolvedResRef = candidate;
                        break;
                    }
                }
            }

            if (bytes == null)
            {
                Assert.Ignore("None of the known tile .wok resrefs resolved through the hak-only index; skipping the real-corpus probe.");
                return;
            }

            TestContext.Out.WriteLine($"Resolved '{resolvedResRef}.wok' ({bytes.Length} bytes).");
            var headerLen = Math.Min(112, bytes.Length);
            TestContext.Out.WriteLine($"First {headerLen} bytes (hex): {Convert.ToHexString(bytes, 0, headerLen)}");
            var textPreviewLen = Math.Min(120, bytes.Length);
            TestContext.Out.WriteLine($"Decoded as ASCII text (first {textPreviewLen} bytes): {Encoding.ASCII.GetString(bytes, 0, textPreviewLen)}");

            var isBinaryMagic = bytes.Length >= 8 && Encoding.ASCII.GetString(bytes, 0, 8) == "BWM V1.0";
            TestContext.Out.WriteLine($"Matches binary 'BWM V1.0' magic: {isBinaryMagic}");

            var mesh = WokMeshLoader.Parse(bytes, _ => true);

            mesh.Should().NotBeNull($"'{resolvedResRef}.wok' is a real corpus resource and should parse");
            mesh!.Vertices.Should().NotBeEmpty();
            mesh.Faces.Should().NotBeEmpty();

            foreach (var face in mesh.Faces)
            {
                face.A.Should().BeInRange(0, mesh.Vertices.Count - 1);
                face.B.Should().BeInRange(0, mesh.Vertices.Count - 1);
                face.C.Should().BeInRange(0, mesh.Vertices.Count - 1);
                face.Material.Should().BeInRange(0, 300, "surfacemat.2da rows are a small, bounded set");
            }

            // Sane tile-local range. Empirically, real NWN tile geometry (both this SWLOR custom
            // tile and a verified base-game tile, dag01_a01_01) is authored CENTERED at local
            // origin (roughly [-5,5] on X/Y for a 10m tile), not corner-at-origin [0,10] as
            // originally assumed for this work package - the bound below is intentionally wide
            // enough to accommodate either convention while still catching a genuinely broken parse.
            foreach (var vertex in mesh.Vertices)
            {
                vertex.X.Should().BeInRange(-12f, 17f);
                vertex.Y.Should().BeInRange(-12f, 17f);
                MathF.Abs(vertex.Z).Should().BeLessThan(60f);
            }
        }

        [Test]
        public void Parse_RealCorpusWok_FromBaseGameKeyBif_ParsesAsAsciiWalkmesh()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE installation found; skipping the base-game .wok probe.");
                return;
            }

            var dataDirectory = Path.Combine(installPath, "data");
            if (!File.Exists(Path.Combine(dataDirectory, "nwn_base.key")))
            {
                Assert.Ignore("NWN install found but no nwn_base.key under its data directory; skipping.");
                return;
            }

            var baseLayer = KeyBifCatalog.Load(dataDirectory);
            var wokType = ResourceIdentity.TypeFromExtension("wok");

            // dag01_a01_01 is a real base-game area tile (Undermountain tileset) confirmed present
            // in nwn_base.key while building this coverage.
            var identity = new ResourceIdentity("dag01_a01_01", wokType);
            if (!baseLayer.TryGetBytes(identity, out var bytes) || bytes.Length == 0)
            {
                Assert.Ignore("'dag01_a01_01.wok' was not found in this install's nwn_base.key; skipping.");
                return;
            }

            TestContext.Out.WriteLine($"Resolved base-game 'dag01_a01_01.wok' ({bytes.Length} bytes).");
            var headerLen = Math.Min(112, bytes.Length);
            TestContext.Out.WriteLine($"First {headerLen} bytes (hex): {Convert.ToHexString(bytes, 0, headerLen)}");

            var mesh = WokMeshLoader.Parse(bytes, _ => true);

            mesh.Should().NotBeNull("dag01_a01_01.wok is a real, shipped base-game resource and should parse");
            mesh!.Vertices.Should().NotBeEmpty();
            mesh.Faces.Should().NotBeEmpty();

            foreach (var face in mesh.Faces)
            {
                face.A.Should().BeInRange(0, mesh.Vertices.Count - 1);
                face.B.Should().BeInRange(0, mesh.Vertices.Count - 1);
                face.C.Should().BeInRange(0, mesh.Vertices.Count - 1);
            }
        }
    }
}
