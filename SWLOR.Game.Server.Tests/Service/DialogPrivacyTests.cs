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

    [Test]
    public void DynamicNpcDialogs_ForcePrivateConversations()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "Dialog.cs").Replace("\r\n", "\n");
        var scriptNames = ReadSource("SWLOR.Game.Server", "Core", "ScriptName.cs");
        var defaultConversationScript = ReadSource("Module", "nss", "nw_c2_default4.nss");
        var makeStartingCreatureConversationPrivate = ExtractMethod(source, "public static void MakeStartingCreatureConversationPrivate()");
        var startConversation = ExtractMethod(source, "public static void StartConversation(uint player, uint talkTo, string @class)");
        var makeCreatureConversationPrivate = ExtractMethod(source, "private static void MakeCreatureConversationPrivate(uint creature)");

        scriptNames.Should().Contain("public const string OnCreatureConversationBefore = \"crea_convo_bef\";");
        defaultConversationScript.Should().Contain("ExecuteScript(\"crea_convo_bef\", OBJECT_SELF);");
        defaultConversationScript.IndexOf("ExecuteScript(\"crea_convo_bef\", OBJECT_SELF);", StringComparison.Ordinal)
            .Should()
            .BeLessThan(defaultConversationScript.IndexOf("BeginConversation();", StringComparison.Ordinal));
        source.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureConversationBefore)]\n        public static void MakeStartingCreatureConversationPrivate()");
        makeStartingCreatureConversationPrivate.Should().Contain("MakeCreatureConversationPrivate(OBJECT_SELF);");

        startConversation.Should().Contain("MakeCreatureConversationPrivate(talkTo);");
        startConversation.IndexOf("MakeCreatureConversationPrivate(talkTo);", StringComparison.Ordinal)
            .Should()
            .BeLessThan(startConversation.IndexOf("BeginConversation(\"dialog\" + dialog.DialogNumber);", StringComparison.Ordinal));

        makeCreatureConversationPrivate.Should().Contain("GetIsPC(creature)");
        makeCreatureConversationPrivate.Should().Contain("GetIsDM(creature)");
        makeCreatureConversationPrivate.Should().Contain("GetIsDMPossessed(creature)");
        makeCreatureConversationPrivate.Should().Contain("ObjectPlugin.SetConversationPrivate(creature, true);");
    }

    [Test]
    public void DynamicDialogTokens_ArePlayerSpecific()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "Dialog.cs");

        source.Should().Contain("PlayerPlugin.SetCustomToken(player, 90000 + dialogOffset, newNodeText);");
        source.Should().Contain("PlayerPlugin.SetCustomToken(player, 90001 + nodeId + dialogOffset, newNodeText);");
        source.Should().NotContain("SetCustomToken(90000 + dialogOffset");
        source.Should().NotContain("SetCustomToken(90001 + nodeId + dialogOffset");
    }

    [Test]
    public void NpcDialogText_DoesNotExposePlayerNameTokens()
    {
        var guildMasterSource = ReadSource(
            "SWLOR.Game.Server",
            "Feature",
            "DialogDefinition",
            "GuildMasterDialog.cs");
        guildMasterSource.Should().NotContain("GetName(player)");
        guildMasterSource.Should().NotContain("Welcome to my guild, ");

        foreach (var dialogFile in Directory.GetFiles(Path.Combine(FindRepositoryRoot().FullName, "Module", "dlg"), "*.dlg.json"))
        {
            var source = File.ReadAllText(dialogFile);
            source.Should().NotContain("<FirstName>", $"{Path.GetFileName(dialogFile)} should not reveal player names in NPC dialogue");
            source.Should().NotContain("<LastName>", $"{Path.GetFileName(dialogFile)} should not reveal player names in NPC dialogue");
            source.Should().NotContain("<FullName>", $"{Path.GetFileName(dialogFile)} should not reveal player names in NPC dialogue");
        }
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
