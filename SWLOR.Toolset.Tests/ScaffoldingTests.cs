using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests
{
    public class ScaffoldingTests
    {
        [Test]
        public void RadoubFormats_IsReferencedAndLoadable()
        {
            var gffFileType = typeof(Radoub.Formats.Gff.GffFile);

            gffFileType.Assembly.GetName().Name.Should().Be("Radoub.Formats");
        }
    }
}
