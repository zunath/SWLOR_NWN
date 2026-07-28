using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests
{
    public class ScaffoldingTests
    {
        [Test]
        public void StandaloneFormatsLibrary_IsReferencedAndLoadable()
        {
            var gffFileType = typeof(SWLOR.NWN.Formats.Gff.GffFile);

            gffFileType.Assembly.GetName().Name.Should().Be("SWLOR.NWN.Formats");
        }
    }
}
