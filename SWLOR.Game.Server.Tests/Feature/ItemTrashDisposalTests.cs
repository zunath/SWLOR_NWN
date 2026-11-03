using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The code that destroys items on a player's behalf (the trash can placeable and the destroy item dialog) audits an
/// item through the <c>ItemTrash</c> facade before it destroys it. The container's audit is replaced by a recorder
/// that notes which items were already destroyed when the audit ran; what the audit writes is tested in
/// <c>ItemTrashServiceTests</c>.
/// </summary>
[TestFixture]
public class ItemTrashDisposalTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1;
    private const uint PossessedAvatar = 2;
    private const uint Can = 10;
    private const uint Owner = 11;
    private const uint Item = 20;
    private const uint SecondItem = 21;

    private AuditRecorder _audits = null!;
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;
    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakeEventsPlugin _events = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _eventsScope = null!;
    private readonly Dictionary<uint, List<uint>> _inventories = new();
    private readonly Dictionary<uint, int> _cursor = new();

    [SetUp]
    public void SetUp()
    {
        _inventories.Clear();
        _cursor.Clear();
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _events = new FakeEventsPlugin();
        _audits = new AuditRecorder(_engine);
        _facades = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton<IItemTrashService>(_audits)));
        _facadeScope = _facades.Install();

        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! is Player or PossessedAvatar or Can or Owner or Item or SecondItem);
        _engine.SetReturnHandler("GetName", arguments => $"name-{(uint)arguments[0]!}");
        _engine.SetReturn("OBJECT_SELF", Can);
        _engine.SetReturnHandler("GetFirstItemInInventory", arguments => ItemAt((uint)arguments[0]!, _cursor[(uint)arguments[0]!] = 0));
        _engine.SetReturnHandler("GetNextItemInInventory", arguments => ItemAt((uint)arguments[0]!, ++_cursor[(uint)arguments[0]!]));
        _engine.SetReturn("GetLastOpenedBy", Player);
        _engine.SetReturn("GetLastClosedBy", Player);
        _engine.SetReturn("GetLastDisturbed", Player);
        _engine.SetReturn("GetInventoryDisturbItem", Item);
        _engine.SetReturn("GetInventoryDisturbType", DisturbType.Added);

        _engineScope = NWScriptRuntime.Override(_engine);
        _eventsScope = EventsPluginRuntime.Override(_events);
    }

    [TearDown]
    public void TearDown()
    {
        _eventsScope.Dispose();
        _engineScope.Dispose();
        _facadeScope.Dispose();
        _facades.Dispose();
    }

    [Test]
    public void OpeningTheTrashCan_RemembersWhoOpenedItAndWarnsThem()
    {
        TrashCan.AlertPlayer();

        _locals.Object(Can, "TRASH_OWNER").Should().Be(Player);
        var warning = _engine.CallsTo("FloatingTextStringOnCreature").Should().ContainSingle().Which;
        warning.Arguments[0].Should().Be("Any item placed inside this trash can will be destroyed permanently.");
        warning.Arguments[1].Should().Be(Player);
    }

    [Test]
    public void AddingAnItem_AuditsItBeforeDestroyingIt()
    {
        TrashCan.DestroyItem();

        _audits.Records.Should().Equal(new Audit(Player, Item, "trash-added", Destroyed: []));
        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(Item);
    }

    [Test]
    public void AddingAnItemWithAnUnknownDisturber_UsesTheRememberedOwner()
    {
        _engine.SetReturn("GetLastDisturbed", ObjectInvalid);
        _engine.SetLocalObject(Can, "TRASH_OWNER", Owner);

        TrashCan.DestroyItem();

        _audits.Records.Single().Player.Should().Be(Owner);
    }

    [Test]
    public void AddingAnItemWithNobodyKnown_AuditsItWithoutAPlayer()
    {
        _engine.SetReturn("GetLastDisturbed", ObjectInvalid);

        TrashCan.DestroyItem();

        _audits.Records.Should().Equal(new Audit(ObjectInvalid, Item, "trash-added", Destroyed: []));
        _engine.CallsTo("DestroyObject").Should().ContainSingle();
    }

    [Test]
    public void TakingAnItemOut_IsNotADisposal()
    {
        _engine.SetReturn("GetInventoryDisturbType", DisturbType.Removed);

        TrashCan.DestroyItem();

        _audits.Records.Should().BeEmpty();
        _engine.CallsTo("DestroyObject").Should().BeEmpty();
    }

    [Test]
    public void ClosingTheTrashCan_AuditsAndDestroysEveryItemInsideThenTheCan()
    {
        _inventories[Can] = new() { Item, SecondItem };

        TrashCan.CleanUp();

        _audits.Records.Should().Equal(
            new Audit(Player, Item, "trash-close", Destroyed: []),
            new Audit(Player, SecondItem, "trash-close", Destroyed: [Item]));
        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(Item, SecondItem, Can);
    }

    [Test]
    public void ClosingTheTrashCanWithAnUnknownCloser_UsesTheRememberedOwner()
    {
        _inventories[Can] = new() { Item };
        _engine.SetReturn("GetLastClosedBy", ObjectInvalid);
        _engine.SetLocalObject(Can, "TRASH_OWNER", Owner);

        TrashCan.CleanUp();

        _audits.Records.Single().Player.Should().Be(Owner);
    }

    [Test]
    public void ClosingAnEmptyTrashCan_OnlyDestroysTheCan()
    {
        TrashCan.CleanUp();

        _audits.Records.Should().BeEmpty();
        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(Can);
    }

    [Test]
    public void ConfirmingTheDestroyItemDialog_AuditsTheItemBeforeDestroyingIt()
    {
        _engine.SetLocalObject(Player, "DESTROY_ITEM", Item);

        var session = StartDestroyItemDialog(Player);
        session.CurrentNode.Text.Should().NotBeEmpty();
        session.SelectChoice(0);
        session.SelectChoice(0);

        _audits.Records.Should().Equal(new Audit(Player, Item, "destroy-item-confirmed", Destroyed: []));
        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(Item);
        session.HasEnded.Should().BeTrue();
        _locals.Has(Player, "DESTROY_ITEM").Should().BeFalse("the request is consumed when the dialog opens");
    }

    [Test]
    public void OpeningTheDestroyItemDialogWithoutAnItem_TellsThePlayerAndEnds()
    {
        var session = StartDestroyItemDialog(Player);

        session.HasEnded.Should().BeTrue();
        _engine.CallsTo("FloatingTextStringOnCreature").Single().Arguments[0]
            .Should().Be("Could not locate item to destroy. Notify an admin.");
        _audits.Records.Should().BeEmpty();
        _engine.CallsTo("DestroyObject").Should().BeEmpty();
    }

    [Test]
    public void DroppingAnItem_IsBlockedForPlayersAndPointsThemToTheTrashCan()
    {
        _engine.SetReturn("OBJECT_SELF", Player);
        _engine.SetReturn("GetIsPC", true);

        TrashCan.PreventItemDrops();

        _events.CountOf("SkipEvent").Should().Be(1);
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle()
            .Which.Arguments[1].ToString().Should().Contain("Please use the trash can option");
    }

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void DroppingAnItem_IsAllowedForCreaturesThatAreNotPlayersAndForDMs(bool isPlayer, bool isDM, bool isPossessed)
    {
        _engine.SetReturn("OBJECT_SELF", Player);
        _engine.SetReturn("GetIsPC", isPlayer);
        _engine.SetReturn("GetIsDM", isDM);
        _engine.SetReturn("GetIsDMPossessed", isPossessed);

        TrashCan.PreventItemDrops();

        _events.CountOf("SkipEvent").Should().Be(0);
        _engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    private uint ItemAt(uint container, int index) =>
        _inventories.TryGetValue(container, out var items) && index < items.Count ? items[index] : ObjectInvalid;

    /// <summary>Opens the dialog the way the conversation menu service does.</summary>
    private static ConversationMenuSession StartDestroyItemDialog(uint player)
    {
        var definition = new DestroyItemDialog();
        Invoke(definition, "Bind", player, ObjectInvalid);
        var session = new ConversationMenuSession(definition.Build(), new ConversationContext(player, ObjectInvalid), new NoRuntime());
        Invoke(definition, "Attach", session);
        session.Start();
        return session;
    }

    private static void Invoke(ConversationMenuDefinition definition, string method, params object[] arguments) =>
        typeof(ConversationMenuDefinition)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(definition, arguments);

    /// <summary>What the audit was asked for, and which items had already been destroyed when it ran.</summary>
    private sealed record Audit(uint Player, uint Item, string Source, uint[] Destroyed)
    {
        public bool Equals(Audit? other) =>
            other != null && Player == other.Player && Item == other.Item && Source == other.Source &&
            Destroyed.SequenceEqual(other.Destroyed);

        public override int GetHashCode() => HashCode.Combine(Player, Item, Source);
    }

    private sealed class AuditRecorder : IItemTrashService
    {
        private readonly FakeNWScript _engine;

        public AuditRecorder(FakeNWScript engine) => _engine = engine;

        public List<Audit> Records { get; } = new();

        public void Audit(uint player, uint item, string source) =>
            Records.Add(new Audit(player, item, source,
                _engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).ToArray()));
    }

    private sealed class NoRuntime : IConversationRuntime
    {
        public bool EvaluateCondition(ConversationContext context, ConversationCondition condition) => false;
        public bool ExecuteAction(ConversationContext context, ConversationAction action) => false;
        public string ResolveText(ConversationContext context, string text) => text;
    }
}
