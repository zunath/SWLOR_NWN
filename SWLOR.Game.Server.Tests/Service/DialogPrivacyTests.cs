using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class DialogPrivacyTests
{
    [Test]
    public void NuiNpcConversations_ForcePrivateConversations()
    {
        var scriptNames = ReadSource("SWLOR.Game.Server", "Core", "ScriptName.cs");
        var defaultConversationScript = ReadSource("Module", "nss", "nw_c2_default4.nss").Replace("\r\n", "\n");

        var resetIndex = defaultConversationScript.IndexOf(
            "DeleteLocalInt(OBJECT_SELF, \"SWLOR_NUI_CONVO\");", StringComparison.Ordinal);
        var petrifiedIndex = defaultConversationScript.IndexOf(
            "if (GetHasEffect(EFFECT_TYPE_PETRIFY, OBJECT_SELF) == TRUE)", StringComparison.Ordinal);
        var deadIndex = defaultConversationScript.IndexOf(
            "if (GetIsDead(OBJECT_SELF) == TRUE)", StringComparison.Ordinal);
        var ordinaryConversationIndex = defaultConversationScript.IndexOf(
            "if (nMatch == -1)", StringComparison.Ordinal);
        var commandableIndex = defaultConversationScript.IndexOf(
            "if (GetCommandable(OBJECT_SELF) ||", StringComparison.Ordinal);
        var beforeHookIndex = defaultConversationScript.IndexOf(
            "ExecuteScript(\"crea_convo_bef\", OBJECT_SELF);", StringComparison.Ordinal);
        var handledBranch = ExtractMethod(
            defaultConversationScript,
            "if (GetLocalInt(OBJECT_SELF, \"SWLOR_NUI_CONVO\"))");
        var tailHooks = ExtractMethod(defaultConversationScript, "void RunDialogueTailHooks()");

        scriptNames.Should().Contain("public const string OnCreatureConversationBefore = \"crea_convo_bef\";");
        defaultConversationScript.Should().Contain("ExecuteScript(\"crea_convo_bef\", OBJECT_SELF);");
        defaultConversationScript.Should().Contain("if (GetLocalInt(OBJECT_SELF, \"SWLOR_NUI_CONVO\"))");
        resetIndex.Should().BeLessThan(petrifiedIndex);
        petrifiedIndex.Should().BeLessThan(deadIndex);
        deadIndex.Should().BeLessThan(ordinaryConversationIndex);
        ordinaryConversationIndex.Should().BeLessThan(commandableIndex);
        commandableIndex.Should().BeLessThan(beforeHookIndex,
            "the migrated-conversation hook runs only after the native creature state guards");
        beforeHookIndex.Should().BeLessThan(
            defaultConversationScript.IndexOf("BeginConversation();", StringComparison.Ordinal));
        handledBranch.Should().Contain("RunDialogueTailHooks();");
        handledBranch.IndexOf("RunDialogueTailHooks();", StringComparison.Ordinal).Should().BeLessThan(
            handledBranch.IndexOf("return;", StringComparison.Ordinal));
        handledBranch.Should().NotContain("BeginConversation();");
        tailHooks.Should().Contain("SignalEvent(OBJECT_SELF, EventUserDefined(EVENT_DIALOGUE));");
        tailHooks.Should().Contain("ExecuteScript(\"crea_convo_aft\", OBJECT_SELF);");

        // The handler stays on the static facade, where the script registry binds it. What it does (private before the
        // window opens, the handled flag, the player/DM/possessed guards) is proved by ConversationServiceTests, and
        // that the creature event routes the last speaker by ConversationMenuServiceTests.
        typeof(Conversation).GetMethod(nameof(Conversation.StartAssignedCreatureConversation))!
            .GetCustomAttributes(typeof(NWNEventHandler), false)
            .Cast<NWNEventHandler>()
            .Select(attribute => attribute.Script)
            .Should().BeEquivalentTo(new[] { ScriptName.OnCreatureConversationBefore });
    }

    [Test]
    public void CodeDrivenNuiConversations_DoNotUseCustomTokenSlotsOrGeneratedShells()
    {
        var source = ReadSource(
            "SWLOR.Game.Server",
            "Service",
            "ConversationService",
            "ConversationMenuSession.cs");

        source.Should().NotContain("SetCustomToken");
        source.Should().NotContain("BeginConversation");
        source.Should().NotContain("dialogOffset");
        source.Should().Contain("NUI scrolls it");
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

        foreach (var dialogFile in Directory.GetFiles(Path.Combine(FindRepositoryRoot().FullName, "Module", "dlg"), "*.dlg.json")
                     .Concat(Directory.GetFiles(Path.Combine(FindRepositoryRoot().FullName,
                         "SWLOR.Game.Server", "ConversationData"), "*.conversation.json")))
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

        // Walk to the .sln rather than .git: in a git worktree .git is a file, not a directory.
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests should run inside the repository checkout");
        return directory!;
    }
}
