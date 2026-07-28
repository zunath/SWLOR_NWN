using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Guards that a placed instance is drawn facing the heading its data carries.
    /// </summary>
    /// <remarks>
    /// Everything shares one convention - model forward is +X, and the heading is a plain rotation
    /// about Z - except the toolset's waypoint flag artwork, which is authored pointing +Y and is
    /// turned onto the convention by <see cref="WaypointMarkerModel.ForwardCorrection"/>. These tests
    /// assert both halves: that the correction lands the flag's arrow on the stored heading, and that
    /// no other kind is quietly turned with it.
    /// </remarks>
    public class InstanceFacingTests
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

                throw new DirectoryNotFoundException("Could not locate the repository root from the test context.");
            }
        }

        private static ResourceIndex BuildIndex() => ResourceIndex.FromHakBuilderConfig(
            Path.Combine(RepoRoot, "Build", "hakbuilder.json"), Path.Combine(RepoRoot, "SWLOR_Haks"));

        /// <summary>
        /// The haks plus the base game, because the waypoint flag markers are base-game artwork the
        /// haks do not override - a hak-only index resolves none of them. Null when no local install
        /// was found, which the model-reading tests skip on rather than fail.
        /// </summary>
        private static ResourceIndex? BuildIndexWithBaseGame()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
                return null;

            var dataDirectory = Path.Combine(installPath, "data");
            if (!File.Exists(Path.Combine(dataDirectory, "nwn_base.key")))
                return null;

            return ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(dataDirectory));
        }

        /// <summary>
        /// The flag markers really are authored pointing +Y - the premise the correction rests on.
        /// </summary>
        /// <remarks>
        /// Measured from the arrow rather than assumed: the ground arrow is the geometry in the bottom
        /// fifth of the model, and its furthest point from the pole is the tip. If BioWare's artwork
        /// ever changed axis, or a hak overrode these resrefs with something authored differently, the
        /// correction would start making things worse and this is what would say so.
        /// </remarks>
        [TestCase("gi_waypoint01")]
        [TestCase("gi_waypoint02")]
        [TestCase("gi_waypoint03")]
        [TestCase("gi_waypoint04")]
        public void WaypointFlagArtwork_PointsAlongModelPlusY(string resRef)
        {
            var tip = ArrowTip(resRef);
            if (tip == null)
            {
                Assert.Ignore($"'{resRef}' did not resolve (no local NWN:EE install); skipping.");
                return;
            }

            var degrees = MathF.Atan2(tip.Value.Y, tip.Value.X) * 180f / MathF.PI;
            degrees.Should().BeApproximately(90f, 2f);
        }

        /// <summary>
        /// With the correction applied, the arrow on screen points at the heading the waypoint stores.
        /// </summary>
        /// <remarks>
        /// The whole point of the fix, asserted end to end: take the model's arrow tip, push it through
        /// the same <see cref="AreaPicking.ComputeInstanceTransform"/> the renderer and the picker use,
        /// and check where it ends up. Without the correction every one of these is off by exactly 90.
        /// </remarks>
        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(-90f)]
        [TestCase(37f)]
        public void PlacedWaypoint_DrawsItsArrowAlongTheStoredHeading(float headingDegrees)
        {
            var tip = ArrowTip("gi_waypoint01");
            if (tip == null)
            {
                Assert.Ignore("gi_waypoint01 did not resolve; skipping.");
                return;
            }

            var heading = headingDegrees * MathF.PI / 180f;
            var marker = new InstanceMarker
            {
                Kind = InstanceMarkerKind.Waypoint,
                TemplateResRef = "wp",
                Tag = "wp",
                Position = new Vector3(12f, 34f, 0f),
                Orientation = new Vector2(MathF.Cos(heading), MathF.Sin(heading)),
                VisualTransform = WaypointMarkerModel.ForwardCorrection
            };

            var world = Vector3.Transform(tip.Value, AreaPicking.ComputeInstanceTransform(marker));
            var drawn = world - marker.Position;

            var degrees = MathF.Atan2(drawn.Y, drawn.X) * 180f / MathF.PI;
            var delta = ((degrees - headingDegrees + 540f) % 360f) - 180f;

            delta.Should().BeApproximately(0f, 2f,
                "the arrow must point where the waypoint is set to face, not a quarter turn off");
        }

        /// <summary>
        /// The correction is scoped to waypoints. Doors are the proof it must not spread: across the
        /// corpus a door's Bearing matches its doorway's orientation to 0 or 180 degrees and never 90,
        /// so a door model - whose leaf spans +X - is already laid along its wall by a plain rotation.
        /// </summary>
        [Test]
        public void OnlyWaypointMarkers_CarryTheForwardCorrection()
        {
            var index = BuildIndex();
            var scene = AreaSceneBuilder.Build(
                LoadArea("cz220shipbreakin").Are, LoadArea("cz220shipbreakin").Git,
                new TilesetCatalog(index), new TileModelCache(index));

            var waypoints = scene.Instances.Where(i => i.Kind == InstanceMarkerKind.Waypoint).ToList();
            waypoints.Should().NotBeEmpty("cz220shipbreakin should carry waypoints");

            foreach (var waypoint in waypoints)
            {
                // The correction turns model +Y onto +X; check it by where it sends the Y axis.
                var turned = Vector3.TransformNormal(Vector3.UnitY, waypoint.VisualTransform);
                turned.X.Should().BeApproximately(1f, 0.001f);
                turned.Y.Should().BeApproximately(0f, 0.001f);
            }

            foreach (var other in scene.Instances.Where(i => i.Kind != InstanceMarkerKind.Waypoint))
            {
                var turned = Vector3.TransformNormal(Vector3.UnitY, other.VisualTransform);
                turned.X.Should().BeApproximately(0f, 0.001f,
                    $"a {other.Kind} must keep the shared convention, not the waypoint artwork's");
            }
        }

        private static (Domain.Documents.AreDocument Are, Domain.Documents.GitDocument Git) LoadArea(string resRef)
        {
            var (are, git, _) = new ModuleWorkspace(CorpusLocator.ModuleDirectory).LoadArea(resRef);
            return (are, git);
        }

        /// <summary>The tip of a waypoint marker's ground arrow, in model space, or null when the model will not resolve.</summary>
        private static Vector3? ArrowTip(string resRef)
        {
            var index = BuildIndexWithBaseGame();
            if (index == null)
                return null;

            var identity = new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("mdl"));
            if (!index.TryLookup(identity, out var handle))
                return null;

            var model = MdlMeshBuilder.Build(new MdlReader().Parse(handle.GetBytes()));
            var points = new List<Vector3>();
            foreach (var mesh in model.Meshes)
            {
                for (var i = 0; i + 2 < mesh.Positions.Length; i += 3)
                {
                    points.Add(Vector3.Transform(
                        new Vector3(mesh.Positions[i], mesh.Positions[i + 1], mesh.Positions[i + 2]), mesh.Transform));
                }
            }

            if (points.Count == 0)
                return null;

            var minZ = points.Min(p => p.Z);
            var maxZ = points.Max(p => p.Z);
            var groundCut = minZ + (maxZ - minZ) * 0.2f;

            return points
                .Where(p => p.Z <= groundCut)
                .OrderByDescending(p => p.X * p.X + p.Y * p.Y)
                .FirstOrDefault();
        }
    }
}
