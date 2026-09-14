using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class CacheTests
{
    [Test]
    public void ResolveSoundSetPreviewSoundResref_PaddedPlaceholderReturnsEmpty()
    {
        InvokeResolveSoundSetPreviewSoundResref(" **** ")
            .Should()
            .BeEmpty();
    }

    [Test]
    public void ResolveSoundSetPreviewSoundResref_ValidResrefKeepsExistingSuffixBehavior()
    {
        InvokeResolveSoundSetPreviewSoundResref(" c_spider ")
            .Should()
            .Be("c_spider_bat1");
    }

    private static string InvokeResolveSoundSetPreviewSoundResref(string soundSetResref)
    {
        var method = typeof(Cache)
            .GetMethod("ResolveSoundSetPreviewSoundResref", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (string)method.Invoke(null, new object[] { soundSetResref })!;
    }
}
