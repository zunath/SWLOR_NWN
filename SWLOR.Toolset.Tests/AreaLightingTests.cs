using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP6.2 per-area lighting: the NWN packed-color decode (byte order) and the
    /// day/night sun-vs-moon color selection <see cref="AreaSceneBuilder"/> writes onto
    /// <see cref="AreaScene.Lighting"/>. Lighting is decoded purely from the .are document, so these
    /// need no NWN install or resolved tileset (the tiles may fall back; lighting is independent).
    /// </summary>
    public class AreaLightingTests
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
                        return current.FullName;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the repository root from the test context.");
            }
        }

        private static ResourceIndex BuildHakOnlyIndex() => ResourceIndex.FromHakBuilderConfig(
            Path.Combine(RepoRoot, "Build", "hakbuilder.json"), Path.Combine(RepoRoot, "SWLOR_Haks"));

        private static (AreDocument Are, GitDocument Git) LoadArea(string resRef)
        {
            var (are, git, _) = new ModuleWorkspace(CorpusLocator.ModuleDirectory).LoadArea(resRef);
            return (are, git);
        }

        private static AreaScene Build(AreDocument are, GitDocument git)
        {
            var index = BuildHakOnlyIndex();
            return AreaSceneBuilder.Build(are, git, new TilesetCatalog(index), new TileModelCache(index));
        }

        [Test]
        public void DecodeColor_UnpacksBbggrrByteOrder()
        {
            AreaLighting.DecodeColor(0x000000).Should().Be(Vector3.Zero);
            AreaLighting.DecodeColor(0xFFFFFF).Should().Be(new Vector3(1f, 1f, 1f));

            // Low byte is red, middle green, high blue (0x00BBGGRR).
            AreaLighting.DecodeColor(0x0000FF).Should().Be(new Vector3(1f, 0f, 0f));
            AreaLighting.DecodeColor(0x00FF00).Should().Be(new Vector3(0f, 1f, 0f));
            AreaLighting.DecodeColor(0xFF0000).Should().Be(new Vector3(0f, 0f, 1f));

            // The real moon colors from pw_ar_czarmrange.are.json (1185815 = 0x121817, 1709075 = 0x1A1413).
            AreaLighting.DecodeColor(1185815).Should().Be(new Vector3(0x17 / 255f, 0x18 / 255f, 0x12 / 255f));
            AreaLighting.DecodeColor(1709075).Should().Be(new Vector3(0x13 / 255f, 0x14 / 255f, 0x1A / 255f));
        }

        [Test]
        public void Build_NightArea_UsesDecodedMoonColors()
        {
            // pw_ar_czarmrange is flagged night (IsNight 1) with MoonAmbientColor 1185815,
            // MoonDiffuseColor 1709075 and Sun colors 0.
            var (are, git) = LoadArea("pw_ar_czarmrange");

            var scene = Build(are, git);

            scene.Lighting.IsNight.Should().BeTrue();
            scene.Lighting.AmbientColor.Should().Be(AreaLighting.DecodeColor(1185815));
            scene.Lighting.DiffuseColor.Should().Be(AreaLighting.DecodeColor(1709075));
        }

        [Test]
        public void Build_DayArea_UsesSunColors()
        {
            var (are, git) = LoadArea("bank");
            are.IsNight = false;
            are.SunAmbientColor = 0x00FF00; // green
            are.SunDiffuseColor = 0xFF0000; // blue
            are.MoonAmbientColor = 0x0000FF; // red - must be ignored during the day

            var scene = Build(are, git);

            scene.Lighting.IsNight.Should().BeFalse();
            scene.Lighting.AmbientColor.Should().Be(new Vector3(0f, 1f, 0f));
            scene.Lighting.DiffuseColor.Should().Be(new Vector3(0f, 0f, 1f));
        }

        [Test]
        public void AreaScene_WithoutExplicitLighting_DefaultsToNeutralGray()
        {
            // Synthetic scenes (e.g. picking/walkmesh tests) never set Lighting - they must get the
            // neutral default rather than a null.
            var scene = new AreaScene
            {
                Tileset = "x",
                Width = 1,
                Height = 1,
                Tiles = new List<TilePlacement>(),
                Instances = new List<InstanceMarker>(),
                Diagnostics = new AreaSceneDiagnostics()
            };

            scene.Lighting.Should().BeSameAs(AreaLighting.Default);
            scene.Lighting.AmbientColor.Should().Be(new Vector3(0.5f, 0.5f, 0.5f));
        }
    }
}
