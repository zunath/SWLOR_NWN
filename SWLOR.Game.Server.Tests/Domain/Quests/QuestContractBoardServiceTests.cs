using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Quests;

/// <summary>
/// Unit tests of the quest contract board instance: the engine (NWScript, the object, player and util plugins) is
/// replaced by recording fakes with real local variables, and the database by an in-memory one that answers the
/// board's searches. Three calls the unit tests cannot run (the room check in the native item repository, the
/// authorization lookup in the stored authorized DMs, and the native character save) are answered by a test subclass.
/// </summary>
[TestFixture]
public class QuestContractBoardServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Author = 1001;
    private const uint Worker = 1002;
    private const uint DungeonMaster = 1003;
    private const uint Admin = 1004;
    private const uint OwnedItem = 2001;
    private const uint SecondItem = 2002;
    private const uint TemporaryItem = 2003;
    private const uint RestoredItem = 2004;
    private const uint SecondRestoredItem = 2005;
    private const string OreResref = "qcbs_ore";
    private const string BadResref = "qcbs_not_an_item";

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakeObjectPlugin _objects = null!;
    private FakePlayerPlugin _players = null!;
    private FakeUtilPlugin _util = null!;
    private QueryableDBService _db = null!;
    private TestBoard _service = null!;
    private QuestService _quests = null!;
    private readonly List<IDisposable> _scopes = new();
    private readonly List<uint> _online = new();
    private readonly HashSet<uint> _invalid = new();
    private int _cursor;
    private uint _entering;

    [SetUp]
    public void SetUp()
    {
        _online.Clear();
        _invalid.Clear();
        _entering = ObjectInvalid;

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetObjectUUID", arguments => "uuid-" + (uint)arguments[0]!);
        _engine.SetReturnHandler("GetPCPublicCDKey", arguments => "cd-" + (uint)arguments[0]!);
        _engine.SetReturnHandler("GetName", arguments => NameOf((uint)arguments[0]!));
        _engine.SetReturnHandler("GetResRef", arguments => "resref-" + (uint)arguments[0]!);
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is Author or Worker or DungeonMaster or Admin);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! is DungeonMaster);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && !_invalid.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetEnteringObject", _ => _entering);
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _cursor = 0;
            return _online.Count > 0 ? _online[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextPC", _ => ++_cursor < _online.Count ? _online[_cursor] : ObjectInvalid);
        _engine.SetReturnHandler("GetItemPossessor", arguments => (uint)arguments[0]! is OwnedItem or SecondItem ? Author : ObjectInvalid);
        _engine.SetReturnHandler("CreateItemOnObject", _ => TemporaryItem);
        _engine.SetReturn("GetItemStackSize", 1);
        _engine.SetReturn("GetGold", 100000);

        _objects = new FakeObjectPlugin();
        _objects.SetReturnHandler("Serialize", arguments => "blob-" + (uint)arguments[0]!);
        _objects.SetReturnHandler("Deserialize", _ => RestoredItem);
        _objects.SetReturn("AcquireItem", true);
        _players = new FakePlayerPlugin();
        _util = new FakeUtilPlugin();
        _util.SetReturnHandler("StripColors", arguments => Regex.Replace((string)arguments[0]!, "<c[^>]*>|</c>", string.Empty));

        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(ObjectPluginRuntime.Override(_objects));
        _scopes.Add(PlayerPluginRuntime.Override(_players));
        _scopes.Add(UtilPluginRuntime.Override(_util));

        _db = new QueryableDBService();
        _quests = QuestServices.Create(_db);
        _service = new TestBoard(_db, _quests);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    /// <summary>The service with the three calls the unit tests cannot run answered by the test.</summary>
    private sealed class TestBoard : QuestContractBoardService
    {
        public TestBoard(IDBService db, IQuestService quests)
            : base(db, quests)
        {
        }

        public bool InventoryHasRoom { get; set; } = true;

        public AuthorizationLevel Level { get; set; } = AuthorizationLevel.Player;

        public Exception? SaveFailure { get; set; }

        public List<uint> Checkpoints { get; } = new();

        protected override bool FitsInInventory(uint player, uint item) => InventoryHasRoom;

        protected override AuthorizationLevel GetAuthorizationLevel(uint requester) =>
            requester == Admin ? AuthorizationLevel.Admin : requester == DungeonMaster ? AuthorizationLevel.DM : Level;

        protected override void SaveCharacterCheckpoint(uint player)
        {
            if (SaveFailure != null)
                throw SaveFailure;

            Checkpoints.Add(player);
        }
    }

    private string NameOf(uint obj) => obj switch
    {
        Author => "Author Name",
        Worker => "Worker Name",
        DungeonMaster => "Dungeon Master",
        OwnedItem => "Plasma Rifle",
        TemporaryItem => "Plasteel Ore",
        _ => "Name-" + obj
    };

    private static string IdOf(uint player) => "uuid-" + player;

    private QuestContract Seed(QuestContract contract)
    {
        _db.With(contract);
        return contract;
    }

    private QuestContract Published(uint author = Author, int credits = 1000, int daysUntilExpiry = 10)
    {
        return Seed(new QuestContract
        {
            AuthorPlayerId = IdOf(author), AuthorCDKey = "cd-" + author, AuthorName = NameOf(author),
            Status = QuestContractStatus.Published, Title = "Ore Run", Description = "Bring ore.",
            RewardCredits = credits, CompletionsRemaining = 1,
            DatePublished = DateTime.UtcNow.AddDays(-1), DateExpires = DateTime.UtcNow.AddDays(daysUntilExpiry),
            Objectives = new List<QuestContractObjective> { new() { ItemResref = OreResref, ItemName = "Plasteel Ore", Quantity = 3 } }
        });
    }

    private QuestContract Draft(uint author = Author, Action<QuestContract>? configure = null)
    {
        var draft = new QuestContract
        {
            AuthorPlayerId = IdOf(author), AuthorCDKey = "cd-" + author, AuthorName = NameOf(author),
            Status = QuestContractStatus.Draft, CompletionsRemaining = 1
        };
        configure?.Invoke(draft);
        return Seed(draft);
    }

    private QuestContract PublishableDraft(uint author = Author, Action<QuestContract>? configure = null)
    {
        return Draft(author, draft =>
        {
            draft.Title = "  Ore   Run ";
            draft.Description = "Bring <cff0000>ore</c> to me.";
            draft.RewardCredits = 1000;
            draft.Objectives.Add(new QuestContractObjective { ItemResref = OreResref, ItemName = "Plasteel Ore", Quantity = 3 });
            configure?.Invoke(draft);
        });
    }

    private static QuestContractDelivery Delivery(uint player, string sourceId, int credits = 0, params string[] itemData)
    {
        return new QuestContractDelivery
        {
            PlayerId = IdOf(player), SourceContractId = sourceId, SourceContractTitle = "Ore Run", Credits = credits,
            Items = itemData.Select(data => new QuestContractItem { Data = data, Name = "Item " + data, Resref = "r" }).ToList()
        };
    }

    private List<string> MessagesTo(uint player) => _engine.CallsTo("SendMessageToPC")
        .Where(call => (uint)call.Arguments[0]! == player).Select(call => (string)call.Arguments[1]!).ToList();

    /// <summary>The entities of a type that were saved, each once however many times it was saved.</summary>
    private List<T> SavedOf<T>()
        where T : EntityBase => _db.Store.Saved.OfType<T>().Distinct().ToList();

    // ---- pure rules (moved here from the tests of the static class)

    [Test]
    public void QuestContractObjective_DefaultsQuantityToOne()
    {
        new QuestContractObjective().Quantity.Should().Be(1);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void SanitizeContractText_BlankInputReturnsEmpty(string? input)
    {
        _service.SanitizeContractText(input!, 100).Should().Be(string.Empty);
    }

    [Test]
    public void SanitizeContractText_StripsColorTokensAndControlCharacters()
    {
        _service.SanitizeContractText("<cff0000>Red\u0001 Text\t</c>\n", 100).Should().Be("Red Text");

        _util.CallsTo("StripColors").Should().ContainSingle();
    }

    [Test]
    public void SanitizeContractText_CollapsesWhitespaceAndCapsLength()
    {
        _service.SanitizeContractText("a      b    c", 100).Should().Be("a b c");
        _service.SanitizeContractText("hello world", 6).Should().Be("hello");
        _service.SanitizeContractText("hello world", 5).Should().Be("hello");
    }

    [Test]
    public void CalculatePostingFee_ChargesPercentAboveFloor()
    {
        _service.CalculatePostingFee(3000).Should().Be(150);
    }

    [Test]
    public void CalculatePostingFee_FloorsAtMinimumPostingFee()
    {
        _service.CalculatePostingFee(1000).Should().Be(QuestContractBoardService.MinimumPostingFee);
    }

    [Test]
    public void CalculatePostingFee_BoundaryExactlyAtFloorStaysAtFloor()
    {
        _service.CalculatePostingFee(2000).Should().Be(QuestContractBoardService.MinimumPostingFee);
        _service.CalculatePostingFee(2001).Should().Be(QuestContractBoardService.MinimumPostingFee);
        _service.CalculatePostingFee(2100).Should().Be(105);
    }

    [Test]
    public void CalculatePostingFee_ZeroRewardStillChargesFloor()
    {
        _service.CalculatePostingFee(0).Should().Be(QuestContractBoardService.MinimumPostingFee);
    }

    [Test]
    public void CalculateTotalPublishCost_AddsPercentFeeToEscrow()
    {
        _service.CalculateTotalPublishCost(5000).Should().Be(5250);
    }

    [Test]
    public void CalculateTotalPublishCost_UsesFloorFeeForSmallRewards()
    {
        _service.CalculateTotalPublishCost(500).Should().Be(600);
    }

    [Test]
    public void ValidateDraft_RequiresTitle()
    {
        _service.ValidateDraft(CreateValidDraft(), string.Empty, "Description", ResolveKnownItem)
            .Should().Be("Please enter a title for your contract.");
    }

    [Test]
    public void ValidateDraft_RequiresDescription()
    {
        _service.ValidateDraft(CreateValidDraft(), "Title", string.Empty, ResolveKnownItem)
            .Should().Be("Please enter a description for your contract.");
    }

    [Test]
    public void ValidateDraft_RequiresAtLeastOneObjective()
    {
        var draft = CreateValidDraft();
        draft.Objectives = new List<QuestContractObjective>();

        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"A contract must have between 1 and {QuestContractBoardService.MaxObjectives} objectives.");
    }

    [Test]
    public void ValidateDraft_RejectsTooManyObjectives()
    {
        var draft = CreateValidDraft();
        draft.Objectives = new List<QuestContractObjective>();

        for (var i = 0; i <= QuestContractBoardService.MaxObjectives; i++)
            draft.Objectives.Add(new QuestContractObjective { ItemResref = $"item_{i}", Quantity = 1 });

        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"A contract must have between 1 and {QuestContractBoardService.MaxObjectives} objectives.");
    }

    [Test]
    public void ValidateDraft_RejectsObjectiveMissingItemResref()
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].ItemResref = " ";

        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be("One or more objectives is missing an item.");
    }

    [TestCase(0)]
    [TestCase(QuestContractBoardService.MaxObjectiveQuantity + 1)]
    public void ValidateDraft_RejectsObjectiveQuantityOutsideTheRange(int quantity)
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].Quantity = quantity;

        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"Objective quantities must be between 1 and {QuestContractBoardService.MaxObjectiveQuantity}.");
    }

    [Test]
    public void ValidateDraft_AcceptsMaximumObjectiveQuantity()
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].Quantity = QuestContractBoardService.MaxObjectiveQuantity;

        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem).Should().Be(string.Empty);
    }

    [Test]
    public void ValidateDraft_RejectsUnresolvableItemResref()
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].ItemResref = "not_a_real_item";

        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be("Item 'not_a_real_item' is not a valid item.");
    }

    [Test]
    public void ValidateDraft_RejectsRewardCreditsBelowMinimum()
    {
        var draft = CreateValidDraft();
        draft.RewardCredits = QuestContractBoardService.MinRewardCredits - 1;

        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"Reward credits must be at least {QuestContractBoardService.MinRewardCredits}.");
    }

    [Test]
    public void ValidateDraft_AcceptsValidDraftWithOrWithoutRewardItems()
    {
        var draft = CreateValidDraft();
        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem).Should().Be(string.Empty);

        draft.RewardItems.Add(new QuestContractItem { Name = "Sword" });
        _service.ValidateDraft(draft, "Title", "Description", ResolveKnownItem).Should().Be(string.Empty);
    }

    private static string ResolveKnownItem(string resref) => resref == "not_a_real_item" ? string.Empty : "Some Item";

    private static QuestContract CreateValidDraft() => new()
    {
        Title = "Contract Title",
        Description = "Contract Description",
        Objectives = new List<QuestContractObjective> { new() { ItemResref = "item_resref", ItemName = "Some Item", Quantity = 1 } },
        CompletionsRemaining = 1,
        RewardCredits = QuestContractBoardService.MinRewardCredits
    };

    [TestCase(QuestContractStatus.Fulfilled, true)]
    [TestCase(QuestContractStatus.Expired, true)]
    [TestCase(QuestContractStatus.Cancelled, true)]
    [TestCase(QuestContractStatus.Draft, false)]
    [TestCase(QuestContractStatus.Published, false)]
    [TestCase(QuestContractStatus.TakenDown, false)]
    public void CanPostAgain_IsTrueOnlyForReusableTerminalStatuses(QuestContractStatus status, bool expected)
    {
        _service.CanPostAgain(new QuestContract { Status = status }).Should().Be(expected);
    }

    [Test]
    public void ResolveContractItemIcon_KeepsAUsableStoredIconAndRecomputesAPlaceholder()
    {
        _service.ResolveContractItemIcon(new QuestContractItem { IconResref = "iit_real_icon", Resref = OreResref })
            .Should().Be("iit_real_icon");

        var recomputed = Cache.GetItemIconByResref(OreResref);
        _service.ResolveContractItemIcon(new QuestContractItem { IconResref = "iinvalid_2x2", Resref = OreResref })
            .Should().Be(recomputed);
        _service.ResolveContractItemIcon(new QuestContractItem { IconResref = " ", Resref = OreResref })
            .Should().Be(recomputed);
        _service.ResolveContractItemIcon(new QuestContractItem { IconResref = null!, Resref = OreResref })
            .Should().Be(recomputed);
    }

    // ---- drafts

    [Test]
    public void GetDraft_AnswersTheAuthorsDraftAndNeverCreatesOne()
    {
        _service.GetDraft(Author).Should().BeNull();
        _db.Store.Saved.Should().BeEmpty();

        var draft = Draft();
        Seed(new QuestContract { AuthorPlayerId = IdOf(Worker), Status = QuestContractStatus.Draft });
        Seed(new QuestContract { AuthorPlayerId = IdOf(Author), Status = QuestContractStatus.Published });

        _service.GetDraft(Author).Should().BeSameAs(draft);
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void GetOrCreateDraft_ReturnsTheExistingDraft()
    {
        var draft = Draft();

        _service.GetOrCreateDraft(Author).Should().BeSameAs(draft);

        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void GetOrCreateDraft_CreatesAndSavesADraftForThePlayer()
    {
        var created = _service.GetOrCreateDraft(Author);

        created.AuthorPlayerId.Should().Be(IdOf(Author));
        created.AuthorCDKey.Should().Be("cd-" + Author);
        created.AuthorName.Should().Be("Author Name");
        created.Status.Should().Be(QuestContractStatus.Draft);
        created.CompletionsRemaining.Should().Be(1);
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(created);
    }

    [Test]
    public void AddRewardItem_RejectsAFullRewardList()
    {
        Draft(Author, draft =>
        {
            draft.RewardItems.Add(new QuestContractItem());
            draft.RewardItems.Add(new QuestContractItem());
        });

        _service.AddRewardItem(Author, OwnedItem).Should().Be($"A contract can have at most {QuestContractBoardService.MaxRewardItems} reward items.");
    }

    [Test]
    public void AddRewardItem_RejectsItemsThePlayerDoesNotCarry()
    {
        _service.AddRewardItem(Author, 9999).Should().Be("Item must be in your inventory.");
    }

    [Test]
    public void AddRewardItem_RejectsContainers()
    {
        _engine.SetReturn("GetHasInventory", true);

        _service.AddRewardItem(Author, OwnedItem).Should().Be("Containers cannot be used as a contract reward.");
    }

    [TestCase("GetItemCursedFlag")]
    [TestCase("GetPlotFlag")]
    public void AddRewardItem_RejectsCursedAndPlotItems(string flag)
    {
        _engine.SetReturn(flag, true);

        _service.AddRewardItem(Author, OwnedItem).Should().Be("This item cannot be used as a contract reward.");
    }

    [Test]
    public void AddRewardItem_RejectsLegacyItems()
    {
        _engine.SetReturn("GetTag", "LEGACY_ITEM");

        _service.AddRewardItem(Author, OwnedItem).Should().Be("Legacy items cannot be used as a contract reward.");
        _engine.CountOf("DestroyObject").Should().Be(0);
    }

    [Test]
    public void AddRewardItem_EscrowsTheItemIntoTheDraftAndDestroysIt()
    {
        var draft = Draft();

        _service.AddRewardItem(Author, OwnedItem).Should().BeEmpty();

        var escrowed = draft.RewardItems.Should().ContainSingle().Which;
        escrowed.Data.Should().Be("blob-" + OwnedItem);
        escrowed.Name.Should().Be("Plasma Rifle");
        escrowed.Resref.Should().Be("resref-" + OwnedItem);
        escrowed.StackSize.Should().Be(1);
        escrowed.IconResref.Should().NotBeNullOrEmpty();
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(draft);
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(OwnedItem);
    }

    [Test]
    public void RemoveRewardItem_RejectsAnIndexOutsideTheList()
    {
        Draft();

        _service.RemoveRewardItem(Author, 0).Should().Be("That reward item could not be found.");
        _service.RemoveRewardItem(Author, -1).Should().Be("That reward item could not be found.");
    }

    [Test]
    public void RemoveRewardItem_ReturnsTheItemToTheInventory()
    {
        var draft = Draft(Author, d => d.RewardItems.Add(new QuestContractItem { Data = "blob", Name = "Plasma Rifle" }));

        _service.RemoveRewardItem(Author, 0).Should().BeEmpty();

        draft.RewardItems.Should().BeEmpty();
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(draft);
        _objects.CallsTo("AcquireItem").Should().ContainSingle().Which.Arguments[1].Should().Be(RestoredItem);
        _engine.CountOf("DestroyObject").Should().Be(0);
    }

    [Test]
    public void RemoveRewardItem_KeepsTheEscrowAndDestroysTheCopyWhenThereIsNoRoom()
    {
        var draft = Draft(Author, d => d.RewardItems.Add(new QuestContractItem { Data = "blob" }));
        _service.InventoryHasRoom = false;

        _service.RemoveRewardItem(Author, 0).Should().Be("The item could not be returned to your inventory. Make room and try again.");

        draft.RewardItems.Should().ContainSingle();
        _db.Store.Saved.Should().BeEmpty();
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(RestoredItem);
    }

    [Test]
    public void RemoveRewardItem_KeepsTheEscrowWhenTheEngineRefusesTheItem()
    {
        var draft = Draft(Author, d => d.RewardItems.Add(new QuestContractItem { Data = "blob" }));
        _objects.SetReturn("AcquireItem", false);

        _service.RemoveRewardItem(Author, 0).Should().StartWith("The item could not be returned");

        draft.RewardItems.Should().ContainSingle();
        _engine.CallsTo("DestroyObject").Should().ContainSingle();
    }

    [Test]
    public void RemoveRewardItem_KeepsTheEscrowWhenTheItemCannotBeRestored()
    {
        var draft = Draft(Author, d => d.RewardItems.Add(new QuestContractItem { Data = "corrupt" }));
        _objects.SetReturnSequence("Deserialize", ObjectInvalid);

        _service.RemoveRewardItem(Author, 0).Should().StartWith("The item could not be returned");

        draft.RewardItems.Should().ContainSingle();
        _engine.CountOf("DestroyObject").Should().Be(0);
    }

    [Test]
    public void DeleteDraft_RejectsAPlayerWithoutADraft()
    {
        _service.DeleteDraft(Author).Should().Be("You do not have a draft contract.");
    }

    [Test]
    public void DeleteDraft_DeletesTheDraftAndSendsEscrowedItemsBackAsADelivery()
    {
        var draft = Draft(Author, d =>
        {
            d.Title = "Mine";
            d.RewardItems.Add(new QuestContractItem { Data = "kept", Name = "Kept" });
        });

        _service.DeleteDraft(Author).Should().BeEmpty();

        _db.Store.Deleted.Should().ContainSingle().Which.Should().Be(draft.Id);
        var delivery = SavedOf<QuestContractDelivery>().Should().ContainSingle().Which;
        delivery.PlayerId.Should().Be(IdOf(Author));
        delivery.SourceContractId.Should().Be(draft.Id);
        delivery.SourceContractTitle.Should().Be("Mine");
        delivery.Items.Should().ContainSingle().Which.Data.Should().Be("kept");
    }

    [Test]
    public void DeleteDraft_WithoutEscrowCreatesNoDelivery()
    {
        var draft = Draft();

        _service.DeleteDraft(Author).Should().BeEmpty();

        _db.Store.Deleted.Should().ContainSingle().Which.Should().Be(draft.Id);
        SavedOf<QuestContractDelivery>().Should().BeEmpty();
    }

    // ---- post again

    [Test]
    public void PostAgain_CreatesAnEditableDraftForTheAuthor()
    {
        var source = Seed(new QuestContract
        {
            AuthorPlayerId = IdOf(Author), AuthorCDKey = "old-key", AuthorName = "Old Name", Status = QuestContractStatus.Expired,
            Title = "Ore", Description = "Deliver ore.", RewardCredits = 500,
            Objectives = new() { new() { ItemResref = OreResref, ItemName = "Ore", Quantity = 4 } }
        });

        _service.PostAgain(Author, source.Id).Should().BeEmpty();

        var draft = SavedOf<QuestContract>().Should().ContainSingle().Which;
        draft.Id.Should().NotBe(source.Id);
        draft.Status.Should().Be(QuestContractStatus.Draft);
        draft.Title.Should().Be("Ore");
        draft.AuthorCDKey.Should().Be("cd-" + Author, "the repost belongs to the player's current account and name");
        draft.AuthorName.Should().Be("Author Name");
        draft.Objectives.Should().ContainSingle().Which.Quantity.Should().Be(4);
    }

    [Test]
    public void PostAgain_RejectsMissingForeignAndUnreusableSourcesAndExistingDrafts()
    {
        var other = Seed(new QuestContract { AuthorPlayerId = IdOf(Worker), Status = QuestContractStatus.Fulfilled });
        var active = Seed(new QuestContract { AuthorPlayerId = IdOf(Author), Status = QuestContractStatus.Published });
        var done = Seed(new QuestContract { AuthorPlayerId = IdOf(Author), Status = QuestContractStatus.Fulfilled });

        _service.PostAgain(Author, "missing").Should().Be("That contract could not be found.");
        _service.PostAgain(Author, other.Id).Should().Be("That contract could not be found.");
        _service.PostAgain(Author, active.Id).Should().Be("Only completed, expired, or cancelled contracts can be posted again.");

        Draft();
        _service.PostAgain(Author, done.Id).Should().Contain("existing draft");
        SavedOf<QuestContract>().Should().BeEmpty();
    }

    // ---- publishing

    [Test]
    public void PublishContract_ReportsTheFirstValidationProblem()
    {
        Draft();

        _service.PublishContract(Author).Should().Be("Please enter a title for your contract.");
    }

    [Test]
    public void PublishContract_RejectsAnItemThatIsNotInTheCatalog()
    {
        PublishableDraft(Author, draft => draft.Objectives[0].ItemResref = BadResref);
        _engine.SetReturnHandler("GetName", arguments => (uint)arguments[0]! == TemporaryItem ? string.Empty : NameOf((uint)arguments[0]!));

        _service.PublishContract(Author).Should().Be($"Item '{BadResref}' is not a valid item.");
    }

    [Test]
    public void PublishContract_RejectsAPlayerWhoCannotPayTheEscrowAndFee()
    {
        PublishableDraft();
        _engine.SetReturn("GetGold", 1099);

        _service.PublishContract(Author).Should().Be("You need 1100 credits to post this contract (1000 escrowed reward + 100 posting fee).");
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void PublishContract_RejectsAnAccountWithTooManyActiveContracts()
    {
        PublishableDraft();
        for (var i = 0; i < QuestContractBoardService.MaxActiveContractsPerCDKey; i++)
            Published();

        _service.PublishContract(Author)
            .Should().Be($"You may only have {QuestContractBoardService.MaxActiveContractsPerCDKey} active contracts at a time.");
    }

    [Test]
    public void PublishContract_ChargesThePlayerPublishesTheDraftAndRegistersTheQuest()
    {
        var draft = PublishableDraft();

        _service.PublishContract(Author).Should().BeEmpty();

        draft.Status.Should().Be(QuestContractStatus.Published);
        draft.Title.Should().Be("Ore Run", "the title is sanitized");
        draft.Description.Should().Be("Bring ore to me.");
        draft.CompletionsRemaining.Should().Be(1);
        draft.AuthorCDKey.Should().Be("cd-" + Author);
        draft.DateExpires.Should().BeCloseTo(DateTime.UtcNow.AddDays(QuestContractBoardService.ContractDurationDays), TimeSpan.FromMinutes(1));
        draft.DatePublished.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(draft);
        _quests.GetQuestByIdOrDefault(QuestContractFactory.BuildQuestId(draft.Id)).Should().NotBeNull();

        // The fee is taken from the player on the player's own action queue.
        var assigned = _engine.CallsTo("AssignCommand").Should().ContainSingle().Which;
        assigned.Arguments[0].Should().Be(Author);
        ((Action)assigned.Arguments[1]!).Invoke();
        var taken = _engine.CallsTo("TakeGoldFromCreature").Should().ContainSingle().Which;
        taken.Arguments[0].Should().Be(1100);
        taken.Arguments[1].Should().Be(Author);
        taken.Arguments[2].Should().Be(true);
    }

    // ---- taking a contract down

    [Test]
    public void CancelContract_RejectsAMissingAndAnInactiveContract()
    {
        var closed = Seed(new QuestContract { Status = QuestContractStatus.Fulfilled });

        _service.CancelContract(Author, "missing").Should().Be("That contract could not be found.");
        _service.CancelContract(Author, closed.Id).Should().Be("That contract is not currently active.");
    }

    [Test]
    public void CancelContract_RejectsAPlayerWhoIsNeitherTheAuthorNorADm()
    {
        var contract = Published(Author);

        _service.CancelContract(Worker, contract.Id).Should().Be("You do not have permission to take down this contract.");

        contract.Status.Should().Be(QuestContractStatus.Published);
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void CancelContract_LetsTheAuthorCancelAndRefundsTheEscrow()
    {
        var contract = Published(Author, credits: 750);
        contract.RewardItems.Add(new QuestContractItem { Data = "escrowed" });
        _quests.RegisterRuntimeQuest(QuestContractFactory.BuildQuest(contract));

        _service.CancelContract(Author, contract.Id).Should().BeEmpty();

        contract.Status.Should().Be(QuestContractStatus.Cancelled);
        contract.DateClosed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        contract.RewardItems.Should().BeEmpty();
        var refund = SavedOf<QuestContractDelivery>().Should().ContainSingle().Which;
        refund.PlayerId.Should().Be(IdOf(Author));
        refund.Credits.Should().Be(750);
        refund.Items.Should().ContainSingle().Which.Data.Should().Be("escrowed");
        _quests.GetQuestByIdOrDefault(QuestContractFactory.BuildQuestId(contract.Id)).Should().BeNull();
    }

    [TestCase(DungeonMaster)]
    [TestCase(Admin)]
    public void CancelContract_LetsADmTakeItDownAndRecordsWho(uint moderator)
    {
        var contract = Published(Author);

        _service.CancelContract(moderator, contract.Id).Should().BeEmpty();

        contract.Status.Should().Be(QuestContractStatus.TakenDown);
        contract.TakedownPlayerId.Should().Be(IdOf(moderator));
        contract.TakedownReason.Should().Be($"Removed by DM {NameOf(moderator)} [{IdOf(moderator)}].");
    }

    [Test]
    public void CancelContract_RemovesTheQuestFromEveryOnlinePlayerWhoAcceptedIt()
    {
        var contract = Published(Author);
        var questId = QuestContractFactory.BuildQuestId(contract.Id);
        _online.AddRange(new[] { Author, DungeonMaster, Worker, Admin });
        var accepted = new Player(IdOf(Author));
        accepted.Quests[questId] = new PlayerQuest { CurrentState = 1 };
        var finished = new Player(IdOf(Worker));
        finished.Quests[questId] = new PlayerQuest { CurrentState = 1, DateLastCompleted = DateTime.UtcNow, TimesCompleted = 1 };
        _db.With(accepted).With(finished);

        _service.CancelContract(Author, contract.Id).Should().BeEmpty();

        accepted.Quests.Should().NotContainKey(questId);
        finished.Quests.Should().ContainKey(questId, "a completed quest is history, not an open contract");
        _db.Store.Saved.OfType<Player>().Should().ContainSingle().Which.Should().BeSameAs(accepted);
        _engine.CallsTo("RemoveJournalQuestEntry").Should().ContainSingle().Which.Arguments[0].Should().Be(questId);
        var journal = _players.CallsTo("AddCustomJournalEntry").Should().ContainSingle().Which;
        journal.Arguments[0].Should().Be(Author);
        var entry = (JournalEntry)journal.Arguments[1]!;
        entry.Name.Should().Be("Contract: Ore Run");
        entry.Tag.Should().Be(questId);
        entry.IsQuestDisplayed.Should().BeFalse();
        MessagesTo(Author).Should().Contain(message => message.Contains("has been cancelled by its author"));
        MessagesTo(Worker).Should().NotContain(message => message.Contains("has been cancelled"));
        MessagesTo(DungeonMaster).Should().BeEmpty();
    }

    // ---- deliveries and claiming

    [Test]
    public void GetOrCreatePendingDelivery_ReusesTheOpenRefundAndSkipsHeldSubmissionsAndReceipts()
    {
        var refund = Delivery(Author, "contract-1");
        _db.With(refund)
            .With(new QuestContractDelivery { PlayerId = IdOf(Author), SourceContractId = "contract-1", HeldForCompletion = true })
            .With(new QuestContractDelivery { PlayerId = IdOf(Author), SourceContractId = "contract-1", IsRewardPayment = true })
            .With(Delivery(Worker, "contract-1"));

        _service.GetOrCreatePendingDelivery(IdOf(Author), "contract-1", "Ore Run").Should().BeSameAs(refund);

        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void GetOrCreatePendingDelivery_CreatesAndSavesOneAndTellsAnOnlineRecipient()
    {
        _online.Add(Worker);
        _online.Add(Author);

        var created = _service.GetOrCreatePendingDelivery(IdOf(Author), "contract-1", "Ore Run");

        created.PlayerId.Should().Be(IdOf(Author));
        created.SourceContractId.Should().Be("contract-1");
        created.SourceContractTitle.Should().Be("Ore Run");
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(created);
        MessagesTo(Author).Should().ContainSingle().Which.Should().Contain("Contract deliveries are waiting for you");
        MessagesTo(Worker).Should().BeEmpty();
    }

    [Test]
    public void GetOrCreatePendingDelivery_SaysNothingToARecipientWhoIsOffline()
    {
        _service.GetOrCreatePendingDelivery(IdOf(Author), "contract-1", "Ore Run");

        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void IsClaimable_NeedsValueAndNoHold()
    {
        _service.IsClaimable(new QuestContractDelivery { Credits = 1 }).Should().BeTrue();
        _service.IsClaimable(new QuestContractDelivery { Items = { new QuestContractItem() } }).Should().BeTrue();
        _service.IsClaimable(new QuestContractDelivery()).Should().BeFalse();
        _service.IsClaimable(new QuestContractDelivery { Credits = 1, HeldForCompletion = true }).Should().BeFalse();
    }

    [Test]
    public void HasPendingDeliveries_IsTrueOnlyForTheClaimableDeliveriesOfThatPlayer()
    {
        _service.HasPendingDeliveries(IdOf(Author)).Should().BeFalse();

        _db.With(Delivery(Worker, "c", credits: 5));
        _db.With(new QuestContractDelivery { PlayerId = IdOf(Author), IsRewardPayment = true });
        _service.HasPendingDeliveries(IdOf(Author)).Should().BeFalse();

        _db.With(Delivery(Author, "c", credits: 5));
        _service.HasPendingDeliveries(IdOf(Author)).Should().BeTrue();
    }

    [Test]
    public void NotifyPendingDeliveries_TellsAPlayerWhoEntersWithAClaimableDelivery()
    {
        _db.With(Delivery(Author, "c", credits: 5));
        _entering = Author;

        _service.NotifyPendingDeliveries();

        MessagesTo(Author).Should().ContainSingle().Which.Should().Contain("Contract deliveries are waiting for you");
    }

    [Test]
    public void NotifyPendingDeliveries_LeavesOutDmsAndPlayersWithNothingWaiting()
    {
        _db.With(Delivery(DungeonMaster, "c", credits: 5));
        _db.With(Delivery(Worker, "c"));

        _entering = DungeonMaster;
        _service.NotifyPendingDeliveries();
        _entering = Worker;
        _service.NotifyPendingDeliveries();
        _entering = 9999;
        _service.NotifyPendingDeliveries();

        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void ClaimDeliveries_WithNothingPendingSaysSo()
    {
        _service.ClaimDeliveries(Author);

        MessagesTo(Author).Should().ContainSingle().Which.Should().Be("You have no pending contract deliveries.");
        _service.Checkpoints.Should().BeEmpty();
    }

    [Test]
    public void ClaimDeliveries_GivesTheItemsAndCreditsThenDeletesTheDelivery()
    {
        var delivery = Delivery(Author, "c", credits: 500, "one", "two");
        _db.With(delivery);
        _objects.SetReturnHandler("Deserialize", arguments => (string)arguments[0]! == "one" ? RestoredItem : SecondRestoredItem);

        _service.ClaimDeliveries(Author);

        _objects.CallsTo("AcquireItem").Select(call => (uint)call.Arguments[1]!).Should().Equal(RestoredItem, SecondRestoredItem);
        var gold = _engine.CallsTo("GiveGoldToCreature").Should().ContainSingle().Which;
        gold.Arguments[0].Should().Be(Author);
        gold.Arguments[1].Should().Be(500);
        _service.Checkpoints.Should().Equal(Author);
        _db.Store.Deleted.Should().ContainSingle().Which.Should().Be(delivery.Id);
        MessagesTo(Author).Should().Contain("You claimed 500 credits and 2 item(s) from your quest contract deliveries.");
        _locals.Has(Author, "CONTRACT_CLAIM_" + delivery.Id).Should().BeFalse("the checkpoint is cleared once the delivery is settled");
    }

    [Test]
    public void ClaimDeliveries_KeepsAnItemThatDoesNotFitAndTellsThePlayerToMakeRoom()
    {
        var delivery = Delivery(Author, "c", credits: 500, "one");
        _db.With(delivery);
        _service.InventoryHasRoom = false;

        _service.ClaimDeliveries(Author);

        _engine.CountOf("GiveGoldToCreature").Should().Be(0, "credits are paid only once every item is acquired");
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(RestoredItem);
        _db.Store.Deleted.Should().BeEmpty();
        MessagesTo(Author).Should().ContainSingle().Which.Should().Be("Your inventory is full. Make room, then claim your remaining deliveries.");
    }

    [Test]
    public void ClaimDeliveries_ReportsADeliveryWhoseItemsCannotBeRestored()
    {
        _db.With(Delivery(Author, "c", credits: 0, "corrupt"));
        _objects.SetReturnHandler("Deserialize", _ => ObjectInvalid);

        _service.ClaimDeliveries(Author);

        MessagesTo(Author).Should().ContainSingle().Which.Should().Be("Some of your deliveries could not be claimed. Please try again.");
        _db.Store.Deleted.Should().BeEmpty();
    }

    [Test]
    public void ClaimDeliveries_KeepsTheDeliveryAndItsCheckpointWhenTheCharacterCannotBeSaved()
    {
        var delivery = Delivery(Author, "c", credits: 500);
        _db.With(delivery);
        _service.SaveFailure = new InvalidOperationException("no servervault");

        _service.ClaimDeliveries(Author);

        MessagesTo(Author).Should().ContainSingle().Which.Should().Contain("could not be saved");
        _db.Store.Deleted.Should().BeEmpty();
        _locals.Has(Author, "CONTRACT_CLAIM_" + delivery.Id).Should().BeTrue("the checkpoint is retained so a retry does not pay twice");
    }

    [Test]
    public void ClaimDeliveries_KeepsTheEmptyReceiptOfAPaidRewardWhileItsSourceExists()
    {
        var source = Published();
        var receipt = Delivery(Author, source.Id, credits: 300);
        receipt.IsRewardPayment = true;
        _db.With(receipt);

        _service.ClaimDeliveries(Author);

        _db.Store.Deleted.Should().BeEmpty("the receipt keeps settlement recovery from paying the reward twice");
        var remaining = SavedOf<QuestContractDelivery>().Last();
        remaining.Credits.Should().Be(0);
        remaining.IsRewardPayment.Should().BeTrue();
        MessagesTo(Author).Should().Contain("You claimed 300 credits and 0 item(s) from your quest contract deliveries.");
    }

    [Test]
    public void ClaimDeliveries_DeletesTheReceiptOfAPaidRewardOnceItsSourceIsGone()
    {
        var receipt = Delivery(Author, "gone", credits: 300);
        receipt.IsRewardPayment = true;
        _db.With(receipt);

        _service.ClaimDeliveries(Author);

        _db.Store.Deleted.Should().ContainSingle().Which.Should().Be(receipt.Id);
    }

    // ---- submissions and settlement

    [Test]
    public void GetOrCreateSubmission_FindsThePlayersHeldSubmissionOrBuildsAnUnsavedOne()
    {
        var contract = Published();
        var held = Delivery(Worker, contract.Id);
        held.HeldForCompletion = true;
        _db.With(held).With(Delivery(Worker, contract.Id));

        _service.GetOrCreateSubmission(IdOf(Worker), contract).Should().BeSameAs(held);

        var created = _service.GetOrCreateSubmission(IdOf(Author), contract);
        created.HeldForCompletion.Should().BeTrue();
        created.PlayerId.Should().Be(IdOf(Author));
        created.SourceContractId.Should().Be(contract.Id);
        created.SourceContractTitle.Should().Be("Ore Run");
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void ReleaseSubmissions_ReturnsEachHeldSubmissionAndTellsTheirOwnersWhoAreOnline()
    {
        var contract = Published();
        contract.Status = QuestContractStatus.Cancelled;
        var mine = Delivery(Worker, contract.Id, 0, "x");
        mine.HeldForCompletion = true;
        var other = Delivery(Author, contract.Id, 0, "y");
        other.HeldForCompletion = true;
        var refund = Delivery(Author, contract.Id, 10);
        _db.With(mine).With(other).With(refund);
        _online.Add(Worker);

        _service.ReleaseSubmissions(contract.Id);

        mine.HeldForCompletion.Should().BeFalse();
        other.HeldForCompletion.Should().BeFalse();
        _db.Store.Saved.Should().BeEquivalentTo(new[] { mine, other }, options => options.WithoutStrictOrdering());
        MessagesTo(Worker).Should().ContainSingle();
    }

    [Test]
    public void ReleaseSubmissions_ForAnAbandonmentReleasesOnlyThatPlayersAttempt()
    {
        var contract = Published();
        var abandoned = Delivery(Worker, contract.Id, 0, "x");
        abandoned.HeldForCompletion = true;
        var other = Delivery(Author, contract.Id, 0, "y");
        other.HeldForCompletion = true;
        _db.With(abandoned).With(other);

        _service.ReleaseSubmissions(contract.Id, IdOf(Worker));

        abandoned.HeldForCompletion.Should().BeFalse();
        other.HeldForCompletion.Should().BeTrue();
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(abandoned);
    }

    [Test]
    public void ReleaseSubmissions_LeavesSubmissionsOfAContractThatIsStillOpen()
    {
        var contract = Published();
        var held = Delivery(Worker, contract.Id, 0, "x");
        held.HeldForCompletion = true;
        _db.With(held);

        _service.ReleaseSubmissions(contract.Id);

        held.HeldForCompletion.Should().BeTrue();
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void TryReleaseSubmission_RulesAreUnchanged()
    {
        var published = new QuestContract { AuthorPlayerId = "author", CompletedByPlayerId = "worker", Status = QuestContractStatus.Published };
        var submission = new QuestContractDelivery { PlayerId = "worker", HeldForCompletion = true };

        _service.TryReleaseSubmission(new QuestContractDelivery { PlayerId = "worker" }, published, "worker")
            .Should().BeFalse("only a held submission can be released");
        _service.TryReleaseSubmission(submission, published).Should().BeFalse("an open contract keeps its submissions");
        _service.TryReleaseSubmission(submission, published, "other").Should().BeFalse("only the abandoning player's attempt is released");
        _service.TryReleaseSubmission(submission, published, "worker").Should().BeTrue();
        submission.HeldForCompletion.Should().BeFalse();
        submission.PlayerId.Should().Be("worker");

        var winner = new QuestContractDelivery { PlayerId = "worker", HeldForCompletion = true };
        _service.TryReleaseSubmission(winner, new QuestContract { AuthorPlayerId = "author", CompletedByPlayerId = "worker", Status = QuestContractStatus.Fulfilled })
            .Should().BeTrue();
        winner.PlayerId.Should().Be("author", "the winner's submissions go to the author");
    }

    [Test]
    public void CompleteContract_RecordsTheWinnerUnregistersTheQuestAndSettlesTheReward()
    {
        var contract = Published(Author, credits: 400);
        contract.RewardItems.Add(new QuestContractItem { Data = "prize" });
        _quests.RegisterRuntimeQuest(QuestContractFactory.BuildQuest(contract));
        var submission = Delivery(Worker, contract.Id, 0, "turned in");
        submission.HeldForCompletion = true;
        _db.With(submission);
        _online.Add(Worker);

        _service.CompleteContract(contract, IdOf(Worker));

        contract.Status.Should().Be(QuestContractStatus.Fulfilled);
        contract.CompletedByPlayerId.Should().Be(IdOf(Worker));
        contract.CompletionsRemaining.Should().Be(0);
        contract.DateClosed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        contract.SettlementPending.Should().Be(0, "settlement finished");
        contract.RewardItems.Should().BeEmpty();
        _quests.GetQuestByIdOrDefault(QuestContractFactory.BuildQuestId(contract.Id)).Should().BeNull();

        var receipt = _db.Store.Saved.OfType<QuestContractDelivery>().Single(delivery => delivery.Id == contract.Id + "-reward");
        receipt.PlayerId.Should().Be(IdOf(Worker));
        receipt.IsRewardPayment.Should().BeTrue();
        receipt.Credits.Should().Be(400);
        receipt.Items.Should().ContainSingle().Which.Data.Should().Be("prize");
        submission.HeldForCompletion.Should().BeFalse();
        submission.PlayerId.Should().Be(IdOf(Author), "the winner's turned-in items go to the author");
        MessagesTo(Worker).Should().NotBeEmpty("the winner is told the reward is waiting");
    }

    [TestCase(QuestContractStatus.Draft)]
    [TestCase(QuestContractStatus.Fulfilled)]
    [TestCase(QuestContractStatus.Cancelled)]
    public void CompleteContract_IgnoresAContractThatIsNotOpen(QuestContractStatus status)
    {
        var contract = Seed(new QuestContract { Status = status, CompletionsRemaining = 1 });

        _service.CompleteContract(contract, IdOf(Worker));

        contract.Status.Should().Be(status);
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void CompleteContract_IgnoresAContractWithNoCompletionsLeft()
    {
        var contract = Published();
        contract.CompletionsRemaining = 0;

        _service.CompleteContract(contract, IdOf(Worker));

        contract.Status.Should().Be(QuestContractStatus.Published);
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void SettleCompletedContract_PaysTheRewardOnlyOnce()
    {
        var contract = Published(Author, credits: 400);
        contract.Status = QuestContractStatus.Fulfilled;
        contract.CompletedByPlayerId = IdOf(Worker);
        contract.SettlementPending = 1;

        _service.SettleCompletedContract(contract);
        contract.SettlementPending = 1;
        _service.SettleCompletedContract(contract);

        SavedOf<QuestContractDelivery>().Where(delivery => delivery.IsRewardPayment).Should().ContainSingle(
            "the retained receipt makes recovery idempotent");
    }

    [TestCase(QuestContractStatus.Published, 1, "winner")]
    [TestCase(QuestContractStatus.Fulfilled, 0, "winner")]
    [TestCase(QuestContractStatus.Fulfilled, 1, " ")]
    [TestCase(QuestContractStatus.Fulfilled, 1, "")]
    public void SettleCompletedContract_NeedsAFulfilledContractWithAPendingSettlementAndAWinner(
        QuestContractStatus status, int pending, string winner)
    {
        var contract = Seed(new QuestContract { Status = status, SettlementPending = pending, CompletedByPlayerId = winner });

        _service.SettleCompletedContract(contract);

        _db.Store.Saved.Should().BeEmpty();
    }

    // ---- boot sweep

    [Test]
    public void RegisterContracts_RegistersEveryOpenContractAsARuntimeQuest()
    {
        var open = Published();
        var other = Published(Worker);
        Seed(new QuestContract { AuthorPlayerId = IdOf(Author), Status = QuestContractStatus.Draft });

        _service.RegisterContracts();

        _quests.GetQuestByIdOrDefault(QuestContractFactory.BuildQuestId(open.Id)).Should().NotBeNull();
        _quests.GetQuestByIdOrDefault(QuestContractFactory.BuildQuestId(other.Id)).Should().NotBeNull();
        open.Status.Should().Be(QuestContractStatus.Published);
        _db.Store.Saved.Should().BeEmpty("nothing is expired, settled or pruned");
    }

    [Test]
    public void RegisterContracts_ExpiresAndRefundsAContractPastItsExpiryWithoutRegisteringIt()
    {
        var expired = Published(Author, credits: 600, daysUntilExpiry: -2);
        expired.RewardItems.Add(new QuestContractItem { Data = "escrow" });
        var online = Published(Worker, daysUntilExpiry: 5);
        var questId = QuestContractFactory.BuildQuestId(expired.Id);
        var player = new Player(IdOf(Worker));
        player.Quests[questId] = new PlayerQuest { CurrentState = 1 };
        _db.With(player);
        _online.Add(Worker);

        _service.RegisterContracts();

        expired.Status.Should().Be(QuestContractStatus.Expired);
        expired.DateClosed.Should().Be(expired.DateExpires);
        expired.RewardItems.Should().BeEmpty();
        var refund = SavedOf<QuestContractDelivery>().Should().ContainSingle().Which;
        refund.PlayerId.Should().Be(IdOf(Author));
        refund.Credits.Should().Be(600);
        refund.Items.Should().ContainSingle();
        _quests.GetQuestByIdOrDefault(questId).Should().BeNull();
        _quests.GetQuestByIdOrDefault(QuestContractFactory.BuildQuestId(online.Id)).Should().NotBeNull();
        player.Quests.Should().NotContainKey(questId);
        MessagesTo(Worker).Should().Contain(message => message.Contains("has been expired"));
    }

    [Test]
    public void RegisterContracts_GivesAnExpiredContractWithoutADateTheCurrentTimeAsItsClosingDate()
    {
        var expired = Published(daysUntilExpiry: -1);
        expired.DateExpires = default;

        _service.RegisterContracts();

        expired.Status.Should().Be(QuestContractStatus.Expired);
        expired.DateClosed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Test]
    public void RegisterContracts_FinishesAnInterruptedSettlementBeforeRegistering()
    {
        var fulfilled = Seed(new QuestContract
        {
            AuthorPlayerId = IdOf(Author), Status = QuestContractStatus.Fulfilled, CompletedByPlayerId = IdOf(Worker),
            SettlementPending = 1, RewardCredits = 90, DateClosed = DateTime.UtcNow
        });

        _service.RegisterContracts();

        fulfilled.SettlementPending.Should().Be(0);
        var receipt = SavedOf<QuestContractDelivery>().Should().ContainSingle().Which;
        receipt.Id.Should().Be(fulfilled.Id + "-reward");
        receipt.Credits.Should().Be(90);
    }

    [Test]
    public void RegisterContracts_CompletesAContractWhoseHeldSubmissionBelongsToAFinishedQuest()
    {
        var contract = Published();
        var questId = QuestContractFactory.BuildQuestId(contract.Id);
        var winner = new Player(IdOf(Worker));
        winner.Quests[questId] = new PlayerQuest { CurrentState = 1, DateLastCompleted = DateTime.UtcNow, TimesCompleted = 1 };
        var held = Delivery(Worker, contract.Id, 0, "submitted");
        held.HeldForCompletion = true;
        _db.With(winner).With(held);

        _service.RegisterContracts();

        contract.Status.Should().Be(QuestContractStatus.Fulfilled);
        contract.CompletedByPlayerId.Should().Be(IdOf(Worker));
        _quests.GetQuestByIdOrDefault(questId).Should().BeNull();
    }

    [Test]
    public void RegisterContracts_ReturnsTheHeldSubmissionOfAPlayerWhoNoLongerHasTheQuest()
    {
        var contract = Published();
        var held = Delivery(Worker, contract.Id, 0, "submitted");
        held.HeldForCompletion = true;
        _db.With(new Player(IdOf(Worker))).With(held);

        _service.RegisterContracts();

        contract.Status.Should().Be(QuestContractStatus.Published);
        held.HeldForCompletion.Should().BeFalse("the abandoned attempt goes back to its player");
        held.PlayerId.Should().Be(IdOf(Worker));
        _quests.GetQuestByIdOrDefault(QuestContractFactory.BuildQuestId(contract.Id)).Should().NotBeNull();
    }

    [Test]
    public void RegisterContracts_KeepsTheHeldSubmissionOfAPlayerWhoStillHoldsTheOpenQuest()
    {
        var contract = Published();
        var player = new Player(IdOf(Worker));
        player.Quests[QuestContractFactory.BuildQuestId(contract.Id)] = new PlayerQuest { CurrentState = 1 };
        var held = Delivery(Worker, contract.Id, 0, "submitted");
        held.HeldForCompletion = true;
        _db.With(player).With(held);

        _service.RegisterContracts();

        held.HeldForCompletion.Should().BeTrue();
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void RegisterContracts_PrunesSettledHistoryPastTheRetentionAndEmptyOrphanedDeliveries()
    {
        var old = Seed(new QuestContract
        {
            Status = QuestContractStatus.Fulfilled, DateClosed = DateTime.UtcNow.AddDays(-QuestContractHistory.RetentionDays - 1)
        });
        var recent = Seed(new QuestContract { Status = QuestContractStatus.Cancelled, DateClosed = DateTime.UtcNow.AddDays(-1) });
        var emptyReceipt = new QuestContractDelivery { SourceContractId = old.Id, IsRewardPayment = true };
        var orphan = new QuestContractDelivery { SourceContractId = "gone", IsRewardPayment = true };
        var unclaimedOrphan = new QuestContractDelivery { SourceContractId = "gone", Credits = 25 };
        var sourceKept = new QuestContractDelivery { SourceContractId = recent.Id, IsRewardPayment = true };
        _db.With(emptyReceipt).With(orphan).With(unclaimedOrphan).With(sourceKept);

        _service.RegisterContracts();

        _db.Store.Deleted.Should().BeEquivalentTo(new[] { emptyReceipt.Id, old.Id, orphan.Id });
        _db.Store.Exists<QuestContract>(recent.Id).Should().BeTrue();
        _db.Store.Exists<QuestContractDelivery>(unclaimedOrphan.Id).Should().BeTrue("unclaimed value outlives its history");
        _db.Store.Exists<QuestContractDelivery>(sourceKept.Id).Should().BeTrue("a receipt stays while its source can recover settlement");
    }

    [Test]
    public void RegisterContracts_PrunesEveryOldClosedContractNotJustTheFirstPage()
    {
        for (var i = 0; i < 60; i++)
            Seed(new QuestContract { Status = QuestContractStatus.Expired, DateClosed = DateTime.UtcNow.AddDays(-60), DateExpires = DateTime.UtcNow.AddDays(-90) });

        _service.RegisterContracts();

        _db.Store.Deleted.Should().HaveCount(60);
    }
}
