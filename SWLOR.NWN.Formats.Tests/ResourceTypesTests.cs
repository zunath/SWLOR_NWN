using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.NWN.Formats.Tests;

public class ResourceTypesTests
{
    [TestCase("mdl", 2002)]
    [TestCase(".UTI", 2025)]
    [TestCase("mtr", 2072)]
    [TestCase("png", 2080)]
    public void ExtensionMapping_IsBidirectionalAndCaseInsensitive(string extension, int expectedValue)
    {
        var expected = checked((ushort)expectedValue);
        ResourceTypes.FromExtension(extension).Should().Be(expected);
        ResourceTypes.FromExtension(ResourceTypes.GetExtension(expected)).Should().Be(expected);
    }

    [Test]
    public void UnknownValues_UseTheInvalidSentinel()
    {
        ResourceTypes.FromExtension(".unknown").Should().Be(ResourceTypes.Invalid);
        ResourceTypes.GetExtension(ResourceTypes.Invalid).Should().BeEmpty();
    }
}
