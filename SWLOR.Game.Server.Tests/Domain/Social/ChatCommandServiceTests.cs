using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// Unit tests of the chat command instance: the engine (NWScript and the chat plugin) is replaced by recording fakes.
/// The commands it runs are the server's own definitions plus <see cref="ServiceTestChatCommands"/>, which the service
/// finds the way it finds every definition. The sender is an admin: the authorization lookup reads the super admin CD
/// key from the environment, so no database is needed.
/// </summary>
[TestFixture]
public class ChatCommandServiceTests
{
    private const uint Sender = 1001;
    private const uint Target = 1002;
    private const uint ObjectInvalid = 0x7F000000;
    private const string AdminCdKey = "UNIT-TEST-ADMIN-KEY";

    private FakeNWScript _engine = null!;
    private FakeChatPlugin _chat = null!;
    private TestableService _service = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _pluginScope = null!;
    private string? _previousAdminKey;

    [SetUp]
    public void SetUp()
    {
        _previousAdminKey = Environment.GetEnvironmentVariable("SWLOR_SUPER_ADMIN_CD_KEY");
        Environment.SetEnvironmentVariable("SWLOR_SUPER_ADMIN_CD_KEY", AdminCdKey);

        ServiceTestChatCommands.Calls.Clear();
        _engine = new FakeNWScript();
        _engine.SetReturn("OBJECT_SELF", Sender);
        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturn("GetPCPublicCDKey", AdminCdKey);
        _chat = new FakeChatPlugin();
        _service = new TestableService();
        _engineScope = NWScriptRuntime.Override(_engine);
        _pluginScope = ChatPluginRuntime.Override(_chat);
    }

    [TearDown]
    public void TearDown()
    {
        _pluginScope.Dispose();
        _engineScope.Dispose();
        Environment.SetEnvironmentVariable("SWLOR_SUPER_ADMIN_CD_KEY", _previousAdminKey);
    }

    // A real engine structure frees its native handle when it is finalized, which crashes a process with no engine.
    private sealed class TestableService : ChatCommandService
    {
        protected override Location CreateEmptyLocation() => EngineStructures.Create<Location>();
    }

    private void Says(string message)
    {
        _chat.SetReturn("GetMessage", message);
        _service.HandleChatMessage();
    }

    private IReadOnlyList<string> Messages() =>
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    private void SelectsTarget(uint target, Vector3? position = null)
    {
        _engine.SetReturn("GetLastPlayerToSelectTarget", Sender);
        _engine.SetReturn("GetTargetingModeSelectedObject", target);
        _engine.SetReturn("GetIsObjectValid", target != ObjectInvalid);
        _engine.SetReturn("GetTargetingModeSelectedPosition", position ?? Vector3.Zero);
        Targeting.RunTargetedItemAction();
    }

    // ---- loading the commands

    [Test]
    public void Before_the_module_loads_there_is_no_help_text_and_no_emotes()
    {
        _service.HelpTextPlayer.Should().BeNull();
        _service.HelpTextEmote.Should().BeNull();
        _service.HelpTextDM.Should().BeNull();
        _service.HelpTextAdmin.Should().BeNull();
        _service.EmoteNames.Should().BeEmpty();
        _service.EmoteDescriptions.Should().BeEmpty();
        _service.EmoteAnimations.Should().BeEmpty();
        _service.EmoteIsLooping.Should().BeEmpty();
    }

    [Test]
    public void Loading_builds_the_help_text_of_each_authorization_level()
    {
        _service.OnModuleLoad();

        _service.HelpTextPlayer.Should().Contain("/svcplayeronly").And.Contain("A command for players.");
        _service.HelpTextPlayer.Should().NotContain("/svcplain").And.NotContain("/svcdmonly").And.NotContain("/svcemote");
        _service.HelpTextDM.Should().Contain("/svcdmonly").And.Contain("/svcchecked");
        _service.HelpTextDM.Should().NotContain("/svcplayeronly").And.NotContain("/svcemote");
        _service.HelpTextAdmin.Should().Contain("/svcplain").And.Contain("/svcalias").And.Contain("/svctarget");
        _service.HelpTextAdmin.Should().NotContain("/svcdmonly").And.NotContain("/svcemote");
    }

    [Test]
    public void Emotes_are_listed_apart_from_the_other_commands()
    {
        _service.OnModuleLoad();

        _service.HelpTextEmote.Should().Contain("/svcemote").And.Contain("An emote.");
        _service.HelpTextEmote.Should().NotContain("/svcplain").And.NotContain("/svcadminemote");
    }

    [Test]
    public void Help_text_is_listed_in_alphabetical_order_of_the_command_names()
    {
        _service.OnModuleLoad();

        var admin = _service.HelpTextAdmin!;
        admin.IndexOf("/svcalias", StringComparison.Ordinal).Should().BeLessThan(admin.IndexOf("/svcplain", StringComparison.Ordinal));
        admin.IndexOf("/svcplain", StringComparison.Ordinal).Should().BeLessThan(admin.IndexOf("/svctarget", StringComparison.Ordinal));
    }

    [Test]
    public void Loading_fills_the_emote_lists_in_step_with_each_other()
    {
        _service.OnModuleLoad();

        _service.EmoteNames.Count.Should().Be(_service.EmoteDescriptions.Count)
            .And.Be(_service.EmoteAnimations.Count).And.Be(_service.EmoteIsLooping.Count);
        var salute = _service.EmoteNames.IndexOf("svcemote");
        salute.Should().BeGreaterThanOrEqualTo(0);
        _service.EmoteDescriptions[salute].Should().Be("An emote.");
        _service.EmoteAnimations[salute].Should().Be(Animation.Salute);
        _service.EmoteIsLooping[salute].Should().BeFalse();
        var meditate = _service.EmoteNames.IndexOf("svcadminemote");
        meditate.Should().BeGreaterThanOrEqualTo(0);
        _service.EmoteAnimations[meditate].Should().Be(Animation.LoopingMeditate);
        _service.EmoteIsLooping[meditate].Should().BeTrue();
        _service.EmoteNames.Should().NotContain("svcplain");
    }

    [Test]
    public void Loading_includes_the_servers_own_definitions()
    {
        _service.OnModuleLoad();

        _service.HelpTextPlayer.Should().Contain("/help");
        _service.EmoteNames.Count.Should().BeGreaterThan(2);
    }

    [Test]
    public void Each_instance_keeps_its_own_commands()
    {
        _service.OnModuleLoad();

        new TestableService().HelpTextAdmin.Should().BeNull();
    }

    // ---- which messages are commands

    [TestCase("hello there")]
    [TestCase("//not a command")]
    [TestCase("/")]
    [TestCase("")]
    public void A_message_that_is_not_a_command_is_left_alone(string message)
    {
        _service.OnModuleLoad();

        Says(message);

        _chat.CountOf("SkipMessage").Should().Be(0);
        Messages().Should().BeEmpty();
    }

    [Test]
    public void A_command_from_something_other_than_a_player_or_dm_is_left_alone()
    {
        _engine.SetReturn("GetIsPC", false);
        _service.OnModuleLoad();

        Says("/svcplain");

        _chat.CountOf("SkipMessage").Should().Be(0);
        ServiceTestChatCommands.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_dm_can_send_commands()
    {
        _engine.SetReturn("GetIsPC", false);
        _engine.SetReturn("GetIsDM", true);
        _service.OnModuleLoad();

        Says("/svcplain");

        ServiceTestChatCommands.Calls.Should().ContainSingle();
    }

    [Test]
    public void A_possessing_dms_creature_can_send_commands()
    {
        _engine.SetReturn("GetIsPC", false);
        _engine.SetReturn("GetIsDMPossessed", true);
        _service.OnModuleLoad();

        Says("/svcplain");

        ServiceTestChatCommands.Calls.Should().ContainSingle();
    }

    [Test]
    public void An_unknown_command_is_swallowed_and_the_sender_is_pointed_at_help()
    {
        _service.OnModuleLoad();

        Says("/nosuchcommand");

        _chat.CountOf("SkipMessage").Should().Be(1);
        Messages().Should().ContainSingle().Which.Should().Contain("Invalid chat command. Use '/help'");
        _engine.CallsTo("SendMessageToPC").Single().Arguments[0].Should().Be(Sender);
    }

    // ---- running commands

    [Test]
    public void A_command_runs_with_its_arguments_and_the_message_is_swallowed()
    {
        _service.OnModuleLoad();

        Says("/svcplain one two");

        _chat.CountOf("SkipMessage").Should().Be(1);
        var call = ServiceTestChatCommands.Calls.Should().ContainSingle().Subject;
        call.Command.Should().Be("svcplain");
        call.User.Should().Be(Sender);
        call.Target.Should().Be(ObjectInvalid);
        call.Args.Should().Equal("one", "two");
        Messages().Should().BeEmpty();
    }

    [Test]
    public void A_command_with_no_arguments_gets_an_empty_list()
    {
        _service.OnModuleLoad();

        Says("/svcplain");

        ServiceTestChatCommands.Calls.Single().Args.Should().BeEmpty();
    }

    [Test]
    public void The_command_name_is_not_case_sensitive_and_the_message_is_trimmed()
    {
        _service.OnModuleLoad();

        Says("   /SvcPlain Mixed Case  ");

        ServiceTestChatCommands.Calls.Single().Args.Should().Equal("Mixed", "Case");
    }

    [Test]
    public void An_alias_runs_the_same_command()
    {
        _service.OnModuleLoad();

        Says("/svcalias x");

        var call = ServiceTestChatCommands.Calls.Should().ContainSingle().Subject;
        call.Command.Should().Be("svcplain");
        call.Args.Should().Equal("x");
    }

    [Test]
    public void A_command_whose_arguments_fail_validation_shows_the_error_and_does_not_run()
    {
        _service.OnModuleLoad();

        Says("/svcchecked");

        Messages().Should().ContainSingle().Which.Should().Be("Give an argument.");
        ServiceTestChatCommands.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_command_whose_arguments_pass_validation_runs()
    {
        _service.OnModuleLoad();

        Says("/svcchecked fine");

        Messages().Should().BeEmpty();
        ServiceTestChatCommands.Calls.Should().ContainSingle().Which.Args.Should().Equal("fine");
    }

    [Test]
    public void A_command_the_senders_level_may_not_use_looks_like_an_unknown_command()
    {
        _service.OnModuleLoad();

        Says("/svcplayeronly");
        Says("/svcdmonly");

        Messages().Should().HaveCount(2).And.OnlyContain(message => message.Contains("Invalid chat command. Use '/help'"));
        ServiceTestChatCommands.Calls.Should().BeEmpty();
    }

    [Test]
    public void An_emote_command_plays_its_animation_on_the_sender()
    {
        _service.OnModuleLoad();

        Says("/svcemote");

        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Sender);
        Messages().Should().BeEmpty();
    }

    // ---- commands that need a target

    [Test]
    public void A_targeted_command_asks_for_a_target_and_runs_when_one_is_chosen()
    {
        _service.OnModuleLoad();

        Says("/svctarget one");

        _engine.CallsTo("EnterTargetingMode").Should().ContainSingle()
            .Which.Arguments.Take(2).Should().Equal(Sender, ObjectType.Creature);
        Messages().Should().ContainSingle().Which.Should().Be("Please click on a target for this chat command.");
        ServiceTestChatCommands.Calls.Should().BeEmpty();

        SelectsTarget(Target);

        var call = ServiceTestChatCommands.Calls.Should().ContainSingle().Subject;
        call.Command.Should().Be("svctarget");
        call.User.Should().Be(Sender);
        call.Target.Should().Be(Target);
        call.Args.Should().Equal("one");
    }

    [Test]
    public void A_targeted_command_that_allows_ground_runs_on_a_location_when_no_object_is_chosen()
    {
        _service.OnModuleLoad();
        Says("/svctarget");

        SelectsTarget(ObjectInvalid, new Vector3(1f, 2f, 3f));

        var call = ServiceTestChatCommands.Calls.Should().ContainSingle().Subject;
        call.Target.Should().Be(ObjectInvalid);
        _engine.CountOf("Location").Should().Be(1);
    }

    [Test]
    public void A_targeted_command_runs_on_the_chosen_objects_location()
    {
        _service.OnModuleLoad();
        Says("/svctarget");

        SelectsTarget(Target);

        _engine.CallsTo("GetLocation").Should().ContainSingle().Which.Arguments[0].Should().Be(Target);
    }

    [Test]
    public void A_targeted_command_whose_arguments_fail_validation_never_asks_for_a_target()
    {
        _service.OnModuleLoad();

        Says("/svctarget one two");

        Messages().Should().ContainSingle().Which.Should().Be("Too many arguments.");
        _engine.CountOf("EnterTargetingMode").Should().Be(0);
    }

    [Test]
    public void A_targeted_command_the_senders_level_may_not_use_looks_like_an_unknown_command()
    {
        _service.OnModuleLoad();

        Says("/svctargetdmonly");

        Messages().Should().ContainSingle().Which.Should().Contain("Invalid chat command. Use '/help'");
        _engine.CountOf("EnterTargetingMode").Should().Be(0);
    }
}
