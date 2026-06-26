using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class DialogPrivacyTests
{
    [Test]
    public void SpawnedNpcDialogs_DefaultToPrivateConversations()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "Spawn.cs").Replace("\r\n", "\n");
        var adjustScripts = ExtractMethod(source, "private static void AdjustScripts(uint spawn)");

        adjustScripts.Should().Contain("if (GetIsPC(spawn) || GetIsDM(spawn) || GetIsDMPossessed(spawn))");
        adjustScripts.Should().Contain("ObjectPlugin.SetConversationPrivate(spawn, true);");
        adjustScripts.IndexOf("ObjectPlugin.SetConversationPrivate(spawn, true);", StringComparison.Ordinal)
            .Should()
            .BeLessThan(adjustScripts.IndexOf("SetEventScript(spawn, EventScript.Creature_OnSpawnIn, \"x2_def_spawn\");", StringComparison.Ordinal));
    }

    private static string ReadSource(params string[] pathParts)
    {
        var fullPath = Path.Combine(new[] { FindRepositoryRoot().FullName }.Concat(pathParts).ToArray());
        return File.ReadAllText(fullPath);
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should exist");

        var openBrace = source.IndexOf('{', start);
        openBrace.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should have an opening brace");

        var depth = 0;
        for (var i = openBrace; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException($"Method '{signature}' was not closed.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests should run inside the repository checkout");
        return directory!;
    }
}
