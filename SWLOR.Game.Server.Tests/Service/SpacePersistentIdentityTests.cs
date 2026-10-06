using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class SpacePersistentIdentityTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(55)]
    [TestCase(56)]
    [TestCase(63)]
    [TestCase(64)]
    [TestCase(65)]
    [TestCase(10000)]
    public void PersistedDigest_MatchesPlatformSha256AcrossBlockBoundaries(int length)
    {
        var identity = new string('x', length);
        SpacePersistentIdentity.Digest(identity).Should().Equal(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    [Test]
    public void Utf8Identity_PreservesTheExistingDigestAndGuid()
    {
        var identity = "ship/пилот:High:1:équipé";
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        var actual = SpacePersistentIdentity.Digest(identity);
        actual.Should().Equal(expected);
        new Guid(actual.AsSpan(0, 16)).Should().Be(new Guid(expected.AsSpan(0, 16)));
    }
}
