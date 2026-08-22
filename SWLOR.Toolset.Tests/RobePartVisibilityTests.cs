using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    public sealed class RobePartVisibilityTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks", "sw_2da")))
                        return current.FullName;
                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate SWLOR_Haks/sw_2da.");
            }
        }

        [Test]
        public void ChiMedCoatHidesOnlyTheArmSegmentsDeclaredByPartsRobe()
        {
            var twoDa = new TwoDaService(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_2da"));

            var found = RobePartVisibility.TryGetHiddenParts(
                twoDa,
                "pmh0_robe010",
                out var hidden);

            found.Should().BeTrue();
            hidden.Should().BeEquivalentTo(
                "forer", "forel", "bicepr", "bicepl", "shor", "shol");
            hidden.Should().NotContain(
                new[] { "chest", "belt", "pelvis", "legl", "legr", "handl", "handr" });
        }

        [Test]
        public void ValidAllClearRowDoesNotFallBackToGeometryGuessing()
        {
            var twoDa = new TwoDaService(Path.Combine(RepoRoot, "SWLOR_Haks", "sw_2da"));

            var found = RobePartVisibility.TryGetHiddenParts(
                twoDa,
                "pfh0_robe018",
                out var hidden);

            found.Should().BeTrue();
            hidden.Should().BeEmpty();
        }
    }
}
