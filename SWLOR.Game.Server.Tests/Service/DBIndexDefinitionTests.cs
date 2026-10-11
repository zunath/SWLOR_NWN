using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class DBIndexDefinitionTests
{
    [Test]
    public void EachIndexOnlyCoversItsOwnEntityHashes()
    {
        var args = SerializeIndexOptions("Player");

        // Without PREFIX, RediSearch indexes every hash in the keyspace, so all entity indexes held every record.
        var prefix = args.IndexOf("PREFIX");
        prefix.Should().BeGreaterThanOrEqualTo(0);
        args[prefix + 1].Should().Be("1");
        args[prefix + 2].Should().Be("Index:Player:");
        var on = args.IndexOf("ON");
        on.Should().BeGreaterThanOrEqualTo(0);
        args[on + 1].Should().BeEquivalentTo("HASH");
    }

    [Test]
    public void PrefixEndsAtTheTypeNameBoundary()
    {
        // "Index:Player" alone would also match "Index:PlayerNote:..." hashes.
        SerializeIndexOptions("Player").Should().NotContain("Index:Player");
        InvokePrivate<string>("IndexKeyPrefix", "PlayerNote").Should().Be("Index:PlayerNote:");
    }

    private static List<string> SerializeIndexOptions(string keyPrefix)
    {
        var options = InvokePrivate<object>("BuildIndexOptions", keyPrefix);
        var serialize = options.GetType().GetMethod(
            "SerializeRedisArgs",
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(List<object>) })!;
        var args = new List<object>();
        serialize.Invoke(options, new object[] { args });

        return args.Select(arg => arg.ToString()!).ToList();
    }

    private static T InvokePrivate<T>(string methodName, params object[] arguments)
    {
        return (T)typeof(DB).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, arguments)!;
    }
}
