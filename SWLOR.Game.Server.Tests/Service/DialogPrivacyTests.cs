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
    public void NuiNpcConversations_ForcePrivateConversations()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "Conversation.cs").Replace("\r\n", "\n");
        var routerSource = ReadSource("SWLOR.Game.Server", "Service", "ConversationMenu.cs");
        var scriptNames = ReadSource("SWLOR.Game.Server", "Core", "ScriptName.cs");
        var defaultConversationScript = ReadSource("Module", "nss", "nw_c2_default4.nss").Replace("\r\n", "\n");
        var startAssigned = ExtractMethod(source, "public static void StartAssignedCreatureConversation()");
        var makeCreatureConversationPrivate = ExtractMethod(source, "private static void MakeCreatureConversationPrivate(uint creature)");

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
        source.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureConversationBefore)]\n        public static void StartAssignedCreatureConversation()");
        startAssigned.Should().Contain("MakeCreatureConversationPrivate(OBJECT_SELF);");
        startAssigned.IndexOf("MakeCreatureConversationPrivate(OBJECT_SELF);", StringComparison.Ordinal)
            .Should()
            .BeLessThan(startAssigned.IndexOf("TryStartAssigned(player, OBJECT_SELF)", StringComparison.Ordinal));
        startAssigned.Should().Contain("SetLocalInt(OBJECT_SELF, NuiConversationHandledLocal, 1);");
        startAssigned.Should().NotContain("EventsPlugin.SkipEvent();");

        makeCreatureConversationPrivate.Should().Contain("GetIsPC(creature)");
        makeCreatureConversationPrivate.Should().Contain("GetIsDM(creature)");
        makeCreatureConversationPrivate.Should().Contain("GetIsDMPossessed(creature)");
        makeCreatureConversationPrivate.Should().Contain("ObjectPlugin.SetConversationPrivate(creature, true);");

        routerSource.Should().Contain("EventScript.Creature_OnDialogue => GetLastSpeaker()",
            "the creature OnConversation event exposes its initiating player as the last speaker");
        routerSource.Should().NotContain("EventScript.Creature_OnDialogue => GetPCSpeaker()",
            "GetPCSpeaker is only populated after NWN has already entered a native conversation");
    }

    [Test]
    public void DirectNuiRouter_PreservesCreatureGuardsAndDmPossession()
    {
        var conversationSource = ReadSource("SWLOR.Game.Server", "Service", "Conversation.cs")
            .Replace("\r\n", "\n");
        var routerSource = ReadSource("SWLOR.Game.Server", "Service", "ConversationMenu.cs")
            .Replace("\r\n", "\n");
        var aiSource = ReadSource("SWLOR.Game.Server", "Service", "AI.cs")
            .Replace("\r\n", "\n");
        var participantGuard = ExtractMethod(
            conversationSource, "internal static bool IsValidParticipant(uint player)");
        var assignedStart = ExtractMethod(
            conversationSource, "public static void StartAssignedCreatureConversation()");
        var graphStart = ExtractMethod(
            conversationSource, "public static void Start(\n            uint player,");
        var openSession = ExtractMethod(
            conversationSource, "public static void OpenSession(");
        var objectRoute = ExtractMethod(
            routerSource, "public static void StartFromObjectEvent()");
        var aiFallback = ExtractMethod(
            aiSource, "public static void CreatureConversation()");
        var creatureGuard = ExtractMethod(
            routerSource, "private static bool CanStartCreatureConversation(uint creature)");
        var menuStart = ExtractMethod(
            routerSource, "public static void Start(uint player, uint owner, string name,");

        participantGuard.Should().Contain("GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)");
        assignedStart.Should().Contain("if (!IsValidParticipant(player))");
        graphStart.Should().Contain("if (!IsValidParticipant(player))");
        openSession.Should().Contain("if (GetIsDMPossessed(player))");
        openSession.Should().Contain("uiTarget = player;");
        openSession.Should().Contain("player = GetMaster(player);");
        openSession.IndexOf("player = GetMaster(player);", StringComparison.Ordinal)
            .Should()
            .BeLessThan(openSession.IndexOf(
                "Gui.TogglePlayerWindow(player, GuiWindowType.Conversation, payload, tetherObject, uiTarget);",
                StringComparison.Ordinal),
                "possessed DMs use their master-owned GUI state while the creature remains the NUI target");
        objectRoute.Should().Contain("if (!Conversation.IsValidParticipant(player))");
        objectRoute.Should().Contain("eventScript == EventScript.Creature_OnDialogue");
        objectRoute.Should().Contain("!CanStartCreatureConversation(owner)");
        menuStart.Should().Contain("if (!Conversation.IsValidParticipant(player))");

        objectRoute.Should().Contain("var owner = OBJECT_SELF;");
        objectRoute.Should().Contain(
            "AssignCommand(player, () => ActionStartConversation(owner, string.Empty, true, false));");
        objectRoute.Should().NotContain("ActionStartConversation(OBJECT_SELF");
        aiFallback.Should().Contain("var owner = OBJECT_SELF;");
        aiFallback.Should().Contain(
            "AssignCommand(talker, () => ActionStartConversation(owner, conversation, true, false));");
        aiFallback.Should().NotContain("ActionStartConversation(OBJECT_SELF");

        creatureGuard.Should().Contain("effectType == EffectTypeScript.Petrify");
        creatureGuard.Should().Contain("if (GetIsDead(creature))");
        creatureGuard.Should().Contain("return GetCommandable(creature) || isCharmed;");
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

        // Walk to the .sln rather than .git: in a git worktree .git is a file, not a directory.
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests should run inside the repository checkout");
        return directory!;
    }
}
