using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests
{
    public class ModelResourceResolverTests
    {
        [TestCase("target\n", true)]
        [TestCase("TARGET\r\nother 10\n", true)]
        [TestCase("missing\n", false)]
        [TestCase("alias\n", false)]
        [TestCase("../target\n", false)]
        [TestCase("\n", false)]
        public void LodTakesPrecedenceOverMdlAndRejectsBrokenTargets(string contents, bool expected)
        {
            var scratch = Path.Combine(Path.GetTempPath(), "swlor-model-resolver-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(Path.Combine(scratch, "alias.mdl"), "shadowed");
                File.WriteAllText(Path.Combine(scratch, "alias.lod"), contents);
                File.WriteAllText(Path.Combine(scratch, "target.mdl"), "canonical");
                var index = new ResourceIndex(null, [new("fixture", scratch)]);
                ModelResourceResolver.TryResolve(index, "alias", out var handle).Should().Be(expected);
                if (expected)
                    handle.GetBytes().Should().Equal(System.Text.Encoding.UTF8.GetBytes("canonical"));
                ModelResourceResolver.TryResolve(index, "target", out _).Should().BeTrue();
            }
            finally
            {
                Directory.Delete(scratch, true);
            }
        }
    }
}
