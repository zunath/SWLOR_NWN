using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// Unit tests of the lightsaber workbench instance: the part catalog, the rules for what carries onto a built saber
/// and the checks for who may use the bench. The engine is a recording fake, the database an in-memory one, and the
/// currency service the real one over that database. The window that the bench opens belongs to the GUI service,
/// which is not converted yet, so the test records that call instead.
/// </summary>
[TestFixture]
public class LightsaberWorkbenchServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint PlayerObject = 1001;
    private const uint Bench = 2002;
    private const uint Token = 3003;
    private const uint OtherItem = 3004;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private BenchService _service = null!;
    private readonly List<uint> _carried = new();
    private int _cursor;

    [SetUp]
    public void SetUp()
    {
        _carried.Clear();
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _service = new BenchService(_db);
        _engine.Engine.SetReturnHandler("GetFirstItemInInventory", arguments => CarriedAt(_cursor = 0));
        _engine.Engine.SetReturnHandler("GetNextItemInInventory", arguments => CarriedAt(++_cursor));
        _engine.Engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _engine.Engine.SetReturnHandler("GetTag", arguments => (uint)arguments[0]! == Token ? LightsaberWorkbenchService.KyberTokenTag : "other");
        _engine.Engine.SetReturn("GetName", "Jedi");
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
    }

    // ---- what carries onto a built saber

    [TestCase(ItemPropertyType.DMG, -1, true)]
    [TestCase(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Electrical, true)]
    [TestCase(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical, false)]
    [TestCase(ItemPropertyType.Accuracy, -1, true)]
    [TestCase(ItemPropertyType.Stamina, -1, true)]
    [TestCase(ItemPropertyType.Delay, -1, false)]
    [TestCase(ItemPropertyType.RequiresSkill, 0, false)]
    [TestCase(ItemPropertyType.UseLimitationPerk, 0, false)]
    public void ShouldTransferSubmissionProperty_KeepsCraftedStatsAndDropsTheBlueprintScaffolding(
        ItemPropertyType type, int subType, bool transfers)
    {
        _service.ShouldTransferSubmissionProperty(type, subType).Should().Be(transfers);
    }

    // ---- the part catalog

    [Test]
    public void GetHilts_ListsTheSaberstaffHiltsForSaberstaffsAndTheLightsaberHiltsForEverythingElse()
    {
        var staff = _service.GetHilts(BaseItem.Saberstaff);
        var saber = _service.GetHilts(BaseItem.Lightsaber);

        staff.Should().HaveCount(10);
        staff.Select(hilt => hilt.PreviewResref).Should().OnlyContain(resref => resref.StartsWith("ui_ssh_"));
        staff.Select(hilt => hilt.PartValue).Should().NotContain(51, "the S-curved staff hilt has no top that connects to it");
        saber.Should().HaveCount(42);
        saber.Select(hilt => hilt.PreviewResref).Should().OnlyContain(resref => resref.StartsWith("ui_lsh_"));
        saber.Select(hilt => hilt.PartValue).Should().OnlyHaveUniqueItems();
        _service.GetHilts(BaseItem.Longsword).Should().Equal(saber, "any other weapon type reads the lightsaber catalog");
    }

    [Test]
    public void GetBladeColors_ListsOnlyTheColorsAvailableForTheConfiguration()
    {
        var straight = _service.GetBladeColors(BaseItem.Lightsaber, false);
        var curved = _service.GetBladeColors(BaseItem.Lightsaber, true);
        var staff = _service.GetBladeColors(BaseItem.Saberstaff, false);

        straight.Should().HaveCount(43);
        curved.Should().HaveCount(14, "legacy curved emitters keep their original model slots");
        staff.Should().HaveCount(40);
        straight.Should().OnlyContain(color => color.StraightTopValue > -1);
        curved.Should().OnlyContain(color => color.CurvedTopValue > -1);
        staff.Should().OnlyContain(color => color.SaberstaffTopValue > -1);
        straight.Select(color => color.StraightTopValue).Should().Contain(new[] { 191, 192, 193, 194 }).And.NotContain(new[] { 71, 72, 73, 74 });
        curved.Select(color => color.CurvedTopValue).Should().Contain(new[] { 71, 72, 73, 74 });
    }

    [Test]
    public void GetTopValue_PicksTheModelForTheWeaponTypeAndHiltStyle()
    {
        var blue = _service.GetBladeColors(BaseItem.Lightsaber, false).Single(color => color.Name == "Blue");

        _service.GetTopValue(blue, BaseItem.Lightsaber, false).Should().Be(blue.StraightTopValue);
        _service.GetTopValue(blue, BaseItem.Lightsaber, true).Should().Be(blue.CurvedTopValue);
        _service.GetTopValue(blue, BaseItem.Saberstaff, false).Should().Be(blue.SaberstaffTopValue);
        _service.GetTopValue(blue, BaseItem.Saberstaff, true).Should().Be(blue.SaberstaffTopValue, "a staff has no curved style");
    }

    [Test]
    public void ResolveLightColor_FollowsTheBladeOnlyWhenMatchBladeIsSelected()
    {
        var blue = _service.GetBladeColors(BaseItem.Lightsaber, false).Single(color => color.Name == "Blue");
        var red = _service.GetBladeColors(BaseItem.Lightsaber, false).Single(color => color.Name == "Red");

        _service.ResolveLightColor(blue, LightsaberWorkbenchService.MatchBladeLightColor).Should().Be((int)LightColor.Blue);
        _service.ResolveLightColor(red, LightsaberWorkbenchService.MatchBladeLightColor).Should().Be((int)LightColor.Red);
        _service.ResolveLightColor(red, (int)LightColor.Blue).Should().Be((int)LightColor.Blue, "an explicit choice sticks");
        Enum.IsDefined(typeof(LightColor), LightsaberWorkbenchService.MatchBladeLightColor).Should().BeFalse();
    }

    // ---- the kyber token

    [Test]
    public void GetKyberTokenItem_FindsTheTokenAmongTheCarriedItems()
    {
        _carried.AddRange(new[] { OtherItem, Token });

        _service.GetKyberTokenItem(PlayerObject).Should().Be(Token);
    }

    [Test]
    public void GetKyberTokenItem_WithoutOne_ReturnsInvalid()
    {
        _carried.Add(OtherItem);

        _service.GetKyberTokenItem(PlayerObject).Should().Be(ObjectInvalid);
    }

    // ---- who may use the bench

    [Test]
    public void ValidateAccess_RefusesNonPlayersAndDMs()
    {
        _engine.Engine.SetReturn("GetIsPC", false);
        _service.ValidateAccess(PlayerObject).Should().Be("Only players may use the workbench.");

        _engine.Engine.SetReturn("GetIsPC", true);
        _engine.Engine.SetReturn("GetIsDM", true);
        _service.ValidateAccess(PlayerObject).Should().Be("Only players may use the workbench.");
    }

    [Test]
    public void ValidateAccess_RefusesCharactersThatAreNotForceSensitive()
    {
        StorePlayer(CharacterType.Standard, kyberTokens: 1);

        _service.ValidateAccess(PlayerObject).Should().Be("Only force sensitive characters may use the workbench.");
    }

    [Test]
    public void ValidateAccess_WithoutKyberTokenCurrency_AsksToAttuneACarriedToken()
    {
        StorePlayer(CharacterType.ForceSensitive, kyberTokens: 0);
        _carried.Add(Token);

        _service.ValidateAccess(PlayerObject).Should().Be("Use your Kyber Token to attune it before working the bench.");
    }

    [Test]
    public void ValidateAccess_WithoutKyberTokenCurrencyOrAToken_SaysStaffIssueThem()
    {
        StorePlayer(CharacterType.ForceSensitive, kyberTokens: 0);

        _service.ValidateAccess(PlayerObject).Should()
            .Be("You need a Kyber Token to use the workbench. Kyber Tokens are issued by staff.");
    }

    [Test]
    public void ValidateAccess_WithAKyberTokenInTheWallet_Allows()
    {
        StorePlayer(CharacterType.ForceSensitive, kyberTokens: 1);

        _service.ValidateAccess(PlayerObject).Should().BeEmpty();
    }

    // ---- using the bench

    [Test]
    public void UseLightsaberWorkbench_OpensTheBenchWindowForAnAllowedPlayer()
    {
        StorePlayer(CharacterType.ForceSensitive, kyberTokens: 1);
        _engine.Engine.SetReturn("GetLastUsedBy", PlayerObject);
        _engine.Engine.SetReturn("OBJECT_SELF", Bench);

        _service.UseLightsaberWorkbench();

        _service.Opened.Should().Equal((PlayerObject, GuiWindowType.LightsaberWorkbench, Bench));
        _engine.Engine.CountOf("FloatingTextStringOnCreature").Should().Be(0);
    }

    [Test]
    public void UseLightsaberWorkbench_ShowsADeniedPlayerWhyAndOpensNothing()
    {
        StorePlayer(CharacterType.ForceSensitive, kyberTokens: 0);
        _engine.Engine.SetReturn("GetLastUsedBy", PlayerObject);
        _engine.Engine.SetReturn("OBJECT_SELF", Bench);

        _service.UseLightsaberWorkbench();

        _service.Opened.Should().BeEmpty();
        var text = _engine.Engine.CallsTo("FloatingTextStringOnCreature").Should().ContainSingle().Which;
        text.Arguments[0].Should().Be("You need a Kyber Token to use the workbench. Kyber Tokens are issued by staff.");
        text.Arguments[1].Should().Be(PlayerObject);
    }

    private void StorePlayer(CharacterType type, int kyberTokens)
    {
        var player = new Player(PlayerEngine.PlayerId) { CharacterType = type };
        if (kyberTokens > 0)
            player.Currencies[CurrencyType.KyberToken] = kyberTokens;
        _db.With(player);
    }

    private uint CarriedAt(int index) => index < _carried.Count ? _carried[index] : ObjectInvalid;

    /// <summary>The bench with the window call recorded instead of sent to the GUI service.</summary>
    private sealed class BenchService : LightsaberWorkbenchService
    {
        public BenchService(InMemoryDBService db)
            : base(db, new CurrencyService(db))
        {
        }

        public List<(uint Player, GuiWindowType Window, uint Target)> Opened { get; } = new();

        protected override void TogglePlayerWindow(uint player, GuiWindowType windowType, uint targetObject)
        {
            Opened.Add((player, windowType, targetObject));
        }
    }
}
