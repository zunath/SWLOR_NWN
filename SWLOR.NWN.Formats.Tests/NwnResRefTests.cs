using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.NWN.Formats.Tests;

public sealed class NwnResRefTests
{
    [TestCase("a")]
    [TestCase("abcdefghijklmnop")]
    [TestCase("mixed_CASE_123")]
    public void ValidShapeAcceptsLegalAuroraResourceReferences(string value)
    {
        NwnResRef.IsValid(value).Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("abcdefghijklmnopq")]
    [TestCase("bad-name")]
    [TestCase("bad name")]
    public void ValidShapeRejectsInvalidAuroraResourceReferences(string? value)
    {
        NwnResRef.IsValid(value).Should().BeFalse();
    }

    [Test]
    public void CanonicalShapeRequiresLowercase()
    {
        NwnResRef.IsCanonical("lower_case_123").Should().BeTrue();
        NwnResRef.IsCanonical("UPPER_CASE").Should().BeFalse();
    }

    [Test]
    public void SharedLimitMatchesTheEngineFieldWidth()
    {
        NwnResRef.MaxLength.Should().Be(16);
    }
}
