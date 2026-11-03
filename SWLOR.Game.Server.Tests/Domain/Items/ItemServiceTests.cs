using System.Numerics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// Unit tests of the item instance: the engine is a recording fake with real local variables and a scripted world of
/// items (<see cref="ItemWorld"/>), the database an in-memory one. The perk and skill services (not converted yet)
/// and the native item repository sit behind the instance's overridable one-line wrappers, so the test instance
/// answers them. The facade resolves the same instance, because the twin pistol rules call it back.
/// </summary>
[TestFixture]
public class ItemServiceTests
{
    private const uint User = 1001;
    private const uint Other = 1002;
    private const uint Target = 3001;
    private const uint Area1 = 4001;
    private const uint Area2 = 4002;
    private const uint ItemA = 2001;
    private const uint ItemB = 2002;
    private const uint Bag = 2003;
    private const uint ItemC = 2004;
    private const uint ObjectInvalid = ItemWorld.ObjectInvalid;
    private const string PlayerId = PlayerEngine.PlayerId;

    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private FakeCreaturePlugin _creaturePlugin = null!;
    private FakeObjectPlugin _objectPlugin = null!;
    private LocalVariables _locals = null!;
    private ItemWorld _world = null!;
    private InMemoryDBService _db = null!;
    private TestItemService _service = null!;
    private FacadeContainer _container = null!;
    private readonly List<IDisposable> _scopes = new();

    private readonly HashSet<uint> _players = new();
    private readonly HashSet<uint> _dms = new();
    private readonly HashSet<uint> _possessed = new();
    private readonly Dictionary<uint, RacialType> _races = new();
    private readonly Dictionary<uint, Vector3> _positions = new();
    private readonly Dictionary<string, string> _eventData = new();
    private readonly HashSet<uint> _craftWindowOpen = new();
    private int _simpleModel;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _events = new FakeEventsPlugin();
        _playerPlugin = new FakePlayerPlugin();
        _creaturePlugin = new FakeCreaturePlugin();
        _objectPlugin = new FakeObjectPlugin();
        _locals = new LocalVariables(_engine);
        _world = new ItemWorld(_engine);
        _db = new InMemoryDBService();
        _players.Clear();
        _dms.Clear();
        _possessed.Clear();
        _races.Clear();
        _positions.Clear();
        _eventData.Clear();
        _craftWindowOpen.Clear();
        _simpleModel = 7;
        ScriptedItems.Details.Clear();

        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDM", args => _dms.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDMPossessed", args => _possessed.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetObjectUUID", _ => PlayerId);
        _engine.SetReturnHandler("GetRacialType", args => _races.GetValueOrDefault((uint)args[0]!, RacialType.Human));
        _engine.SetReturnHandler("GetPosition", args => _positions.GetValueOrDefault((uint)args[0]!));
        _engine.SetReturnHandler("Vector3", args => new Vector3((float)args[0]!, (float)args[1]!, (float)args[2]!));
        _engine.SetReturnHandler("StringToObject", args => uint.TryParse((string)args[0]!, out var id) ? id : ObjectInvalid);
        _engine.SetReturnHandler("NuiFindWindow", args => _craftWindowOpen.Contains((uint)args[0]!) ? 1 : 0);
        _engine.SetReturnHandler("GetArea", args => (uint)args[0]! == Target ? Area2 : Area1);
        _engine.SetReturnHandler("GetItemAppearance", args => (ItemAppearanceType)args[1]! switch
        {
            ItemAppearanceType.SimpleModel => _simpleModel,
            ItemAppearanceType.WeaponModel => 2,
            _ => 1
        });
        _engine.SetReturn("OBJECT_SELF", User);
        _events.SetReturnHandler("GetEventData", args => _eventData.GetValueOrDefault((string)args[0]!, "0"));

        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(EventsPluginRuntime.Override(_events));
        _scopes.Add(PlayerPluginRuntime.Override(_playerPlugin));
        _scopes.Add(CreaturePluginRuntime.Override(_creaturePlugin));
        _scopes.Add(ObjectPluginRuntime.Override(_objectPlugin));

        _world.Add(User);
        _players.Add(User);
        _service = new TestItemService(new BeastMasteryService(_db, null!, null!), _db);
        _container = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton<IItemService>(_service)));
        _scopes.Add(_container.Install());
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--)
            _scopes[index].Dispose();
        _scopes.Clear();
        _container.Dispose();
        ScriptedItems.Details.Clear();
    }

    // ---- base item type groups

    [Test]
    public void TheBaseItemTypeGroupsAreTheInstancesOwnLists()
    {
        _service.ForceSensitiveWeaponBaseItemTypes.Should().BeEquivalentTo(
            _service.LightsaberBaseItemTypes.Concat(_service.SaberstaffBaseItemTypes));
        _service.PowerGloveBaseItemTypes.Should().Equal(BaseItem.PowerGlove);
        _service.WeaponBaseItemTypes.Should().Contain(new[] { BaseItem.Lightsaber, BaseItem.Rifle, BaseItem.Pistol });
        _service.ArmorBaseItemTypes.Should().Contain(new[] { BaseItem.Armor, BaseItem.Helmet, BaseItem.SmallShield });
        _service.ShieldBaseItemTypes.Should().Equal(BaseItem.LargeShield, BaseItem.SmallShield, BaseItem.TowerShield);
        _service.DroidBaseItemTypes.Should().Contain(BaseItem.Armor).And.NotContain(BaseItem.SmallShield);
        _service.TwoHandedMeleeItemTypes.Should().Contain(BaseItem.GreatSword);
        _service.OneHandedMeleeItemTypes.Should().Contain(BaseItem.Longsword);

        var other = new TestItemService(new BeastMasteryService(_db, null!, null!), _db);
        other.WeaponBaseItemTypes.Should().NotBeSameAs(_service.WeaponBaseItemTypes, "each instance owns its lists");
        _service.WeaponBaseItemTypes.Should().BeSameAs(_service.WeaponBaseItemTypes, "an instance hands out the same list every time");
    }

    [TestCase(BaseItem.Longsword, true)]
    [TestCase(BaseItem.Rifle, true)]
    [TestCase(BaseItem.CreatureSlashWeapon, true)]
    [TestCase(BaseItem.Armor, false)]
    [TestCase(BaseItem.SmallShield, false)]
    public void IsAttackWeaponType_CoversEveryWeaponProfileCombatReads(BaseItem baseItem, bool expected)
    {
        _service.IsAttackWeaponType(baseItem).Should().Be(expected);
    }

    [TestCase(BaseItem.DoubleAxe, true)]
    [TestCase(BaseItem.TwoBladedSword, true)]
    [TestCase(BaseItem.Saberstaff, true)]
    [TestCase(BaseItem.TwinElectroBlade, true)]
    [TestCase(BaseItem.Longsword, false)]
    public void IsDoubleWeaponType_IsTrueForTheTwinBladeAndSaberstaffGroups(BaseItem baseItem, bool expected)
    {
        _service.IsDoubleWeaponType(baseItem).Should().Be(expected);
    }

    [Test]
    public void IsBaseItemType_NeedsAValidItemOfOneOfTheTypes()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);

        _service.IsBaseItemType(ItemA, _service.WeaponBaseItemTypes).Should().BeTrue();
        _service.IsBaseItemType(ItemA, _service.ArmorBaseItemTypes).Should().BeFalse();
        _service.IsBaseItemType(ItemB, _service.WeaponBaseItemTypes).Should().BeFalse("the item is not valid");
        _service.IsBaseItemType((global::NWN.Native.API.CNWSItem?)null!, _service.WeaponBaseItemTypes).Should().BeFalse();
    }

    // ---- the item cache and the weapon ability mappings

    [Test]
    public void CacheData_MapsTheMeleeAndRangedBaseItemsToTheirDamageAndAccuracyAbilities()
    {
        _service.CacheData();

        _service.GetWeaponDamageAbilityType(BaseItem.Longsword).Should().Be(AbilityType.Might);
        _service.GetWeaponAccuracyAbilityType(BaseItem.Longsword).Should().Be(AbilityType.Perception);
        _service.GetWeaponDamageAbilityType(BaseItem.PowerGlove).Should().Be(AbilityType.Might);
        _service.GetWeaponDamageAbilityType(BaseItem.Rifle).Should().Be(AbilityType.Perception);
        _service.GetWeaponAccuracyAbilityType(BaseItem.Rifle).Should().Be(AbilityType.Agility);
        _service.GetWeaponDamageAbilityType(BaseItem.Armor).Should().Be(AbilityType.Invalid);
        _service.GetWeaponAccuracyAbilityType(BaseItem.Armor).Should().Be(AbilityType.Invalid);
    }

    [Test]
    public void TheMappingsAreEmptyUntilTheCacheIsLoaded()
    {
        _service.GetWeaponDamageAbilityType(BaseItem.Longsword).Should().Be(AbilityType.Invalid);
        _service.GetWeaponAccuracyAbilityType(BaseItem.Longsword).Should().Be(AbilityType.Invalid);
    }

    [Test]
    public void CacheData_ReadsTheBaseItemsTableAndRegistersTheDefinitions()
    {
        _engine.SetReturn("Get2DARowCount", 2);
        _world.TwoDA("baseitems", "CritThreat", 0, "2");
        _world.TwoDA("baseitems", "CritHitMult", 0, "3");
        _world.TwoDA("baseitems", "WeaponSize", 0, "4");
        ScriptedItems.Details["USED_ITEM"] = new ItemDetail();
        _service.Definitions.Add(typeof(ScriptedItems));
        ArrangeUse("USED_ITEM", null);

        _service.CacheData();
        _service.UseItem();

        _engine.CountOf("Get2DAString").Should().Be(6, "three columns for each of the two rows");
        _events.CountOf("SkipEvent").Should().Be(1, "the cached definition took the use over from the engine");
    }

    [Test]
    public void TheDefaultDefinitionSearchFindsTheServersItemLists()
    {
        var types = new ProbingItemService(new BeastMasteryService(_db, null!, null!), _db).Search().ToList();

        types.Should().NotBeEmpty();
        types.Should().OnlyContain(type => typeof(IItemListDefinition).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface);
    }

    // ---- repairing activation actions

    [Test]
    public void EnsureAcquiredItemActivation_AddsTheDeclaredSpellOnce()
    {
        var spell = CastSpell.ACTIVATE_ITEM;
        RegisterItem("SPELL_ITEM", new ItemDetail { ActivationSpell = spell });
        _world.Add(ItemA, "SPELL_ITEM", BaseItem.MiscSmall);
        _engine.SetReturn("GetModuleItemAcquired", ItemA);

        _service.EnsureAcquiredItemActivation();

        _engine.CallsTo("ItemPropertyCastSpell").Should().ContainSingle()
            .Which.Arguments.Should().Equal(spell, CastSpellNumberUses.UNLIMITED_USE);
        var added = _engine.CallsTo("AddItemProperty").Should().ContainSingle().Subject;
        added.Arguments[0].Should().Be(DurationType.Permanent);
        added.Arguments[2].Should().Be(ItemA);
    }

    [Test]
    public void EnsureAcquiredItemActivation_LeavesAnItemThatAlreadyHasTheSpell()
    {
        RegisterItem("SPELL_ITEM", new ItemDetail { ActivationSpell = CastSpell.ACTIVATE_ITEM });
        _world.Add(ItemA, "SPELL_ITEM", BaseItem.MiscSmall);
        _world.AddProperty(ItemA, ItemPropertyType.DMG, costTableValue: 3);
        _world.AddProperty(ItemA, ItemPropertyType.CastSpell, (int)CastSpell.ACTIVATE_ITEM);
        _engine.SetReturn("GetModuleItemAcquired", ItemA);

        _service.EnsureAcquiredItemActivation();

        _engine.CountOf("AddItemProperty").Should().Be(0);
    }

    [Test]
    public void EnsureAcquiredItemActivation_RepairsTheSpellWhenOnlyAnotherCastSpellIsOnTheItem()
    {
        RegisterItem("SPELL_ITEM", new ItemDetail { ActivationSpell = CastSpell.ACTIVATE_ITEM });
        _world.Add(ItemA, "SPELL_ITEM", BaseItem.MiscSmall);
        _world.AddProperty(ItemA, ItemPropertyType.CastSpell, (int)CastSpell.AID_3);
        _engine.SetReturn("GetModuleItemAcquired", ItemA);

        _service.EnsureAcquiredItemActivation();

        _engine.CountOf("AddItemProperty").Should().Be(1);
    }

    [Test]
    public void EnsureAcquiredItemActivation_IgnoresItemsWithoutADefinitionOrASpellAndInvalidItems()
    {
        RegisterItem("PLAIN_ITEM", new ItemDetail());
        _world.Add(ItemA, "PLAIN_ITEM", BaseItem.MiscSmall);
        _world.Add(ItemB, "UNREGISTERED", BaseItem.MiscSmall);
        foreach (var item in new[] { ItemA, ItemB, ItemC })
        {
            _engine.SetReturn("GetModuleItemAcquired", item);
            _service.EnsureAcquiredItemActivation();
        }

        _engine.CountOf("AddItemProperty").Should().Be(0);
    }

    [Test]
    public void EnsureAcquiredItemActivation_RepairsTheContentsOfABagToo()
    {
        RegisterItem("SPELL_ITEM", new ItemDetail { ActivationSpell = CastSpell.ACTIVATE_ITEM });
        _world.Add(Bag, "BAG", BaseItem.MiscSmall);
        _world.Add(ItemA, "SPELL_ITEM", BaseItem.MiscSmall);
        _world.Add(ItemB, "SPELL_ITEM", BaseItem.MiscSmall);
        _world.Carry(Bag, ItemA, ItemB);
        _engine.SetReturn("GetModuleItemAcquired", Bag);

        _service.EnsureAcquiredItemActivation();

        _engine.CallsTo("AddItemProperty").Select(call => (uint)call.Arguments[2]!).Should().Equal(ItemA, ItemB);
    }

    [Test]
    public void EnsureInventoryItemActivations_RepairsAPlayersInventoryAndSkipsDMsAndCreatures()
    {
        RegisterItem("SPELL_ITEM", new ItemDetail { ActivationSpell = CastSpell.ACTIVATE_ITEM });
        _world.Add(ItemA, "SPELL_ITEM", BaseItem.MiscSmall);
        _world.Add(Bag, "BAG", BaseItem.MiscSmall);
        _world.Add(ItemB, "SPELL_ITEM", BaseItem.MiscSmall);
        _world.Carry(User, ItemA, Bag);
        _world.Carry(Bag, ItemB);
        _world.Add(Other);
        _world.Carry(Other, ItemC);

        _engine.SetReturn("GetEnteringObject", Other);
        _service.EnsureInventoryItemActivations();
        _dms.Add(User);
        _engine.SetReturn("GetEnteringObject", User);
        _players.Add(Other);
        _service.EnsureInventoryItemActivations();
        _engine.CountOf("AddItemProperty").Should().Be(0, "a creature that is not a player and a DM are skipped");

        _dms.Remove(User);
        _service.EnsureInventoryItemActivations();

        _engine.CallsTo("AddItemProperty").Select(call => (uint)call.Arguments[2]!).Should().Equal(ItemA, ItemB);
    }

    // ---- using a registered item

    [Test]
    public void UseItem_SkipsAnItemTheCacheDoesNotKnow()
    {
        ArrangeUse("UNKNOWN", null);

        _service.UseItem();

        _events.CountOf("SkipEvent").Should().Be(0);
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void UseItem_RefusesAUserWhoDoesNotMeetTheItemsRequirements()
    {
        ArrangeUse("USED_ITEM", new ItemDetail());
        _world.AddProperty(ItemA, ItemPropertyType.UseLimitationPerk, (int)PerkType.AlphaRhythm, 3);

        _service.UseItem();

        _events.CountOf("SkipEvent").Should().Be(1);
        Messages().Should().Equal("You do not meet the requirements to use this item.");
    }

    [Test]
    public void UseItem_RefusesABusyUser()
    {
        ArrangeUse("USED_ITEM", new ItemDetail());
        Activity.SetBusy(User, ActivityStatusType.UseItem);

        _service.UseItem();

        Messages().Should().Equal("You are busy.");
    }

    [Test]
    public void UseItem_RefusesWhileTheRecastGroupIsOnCooldown()
    {
        var group = Enum.GetValues<RecastGroup>().First(value => value != RecastGroup.Invalid);
        ArrangeUse("USED_ITEM", new ItemDetail { RecastGroup = group, RecastCooldown = 30f });
        _players.Remove(User);
        _engine.SetLocalString(User, $"ABILITY_RECAST_ID_{(int)group}", RecastTimestamp.Format(DateTime.UtcNow.AddMinutes(10)));

        _service.UseItem();

        Messages().Should().ContainSingle().Which.Should().StartWith("This item can be used in ");
    }

    [Test]
    public void UseItem_SendsTheValidationMessageAndStops()
    {
        ArrangeUse("USED_ITEM", new ItemDetail
        {
            ValidateAction = (_, _, _, _, _) => "Not now.",
            ApplyAction = (_, _, _, _, _) => Assert.Fail("a failed validation never applies the item")
        });

        _service.UseItem();

        Messages().Should().Equal("Not now.");
        _engine.CountOf("DelayCommand").Should().Be(0);
    }

    [Test]
    public void UseItem_SendsTheInitializationMessageAndAppliesTheItemAfterTheDelay()
    {
        var applied = new List<(uint User, uint Item, uint Target, int Index)>();
        ArrangeUse("USED_ITEM", new ItemDetail
        {
            InitializationMessageAction = (_, _, _, _, _) => "You begin.",
            DelayAction = (_, _, _, _, _) => 2f,
            UserFacesTarget = true,
            ActivationAnimation = Animation.LoopingPause,
            ApplyAction = (user, item, target, _, index) => applied.Add((user, item, target, index))
        });
        _eventData["ITEM_PROPERTY_INDEX"] = "4";
        _eventData["TARGET_OBJECT_ID"] = Target.ToString();
        _world.Add(Target);
        _positions[Target] = new Vector3(1, 2, 3);
        _engine.SetReturnHandler("GetArea", _ => Area1);
        _engine.SetReturn("GetDistanceBetween", 1f);

        _service.UseItem();

        Messages().Should().Equal("You begin.");
        _events.CountOf("SkipEvent").Should().Be(1);
        _engine.CallsTo("AssignCommand").Select(call => (uint)call.Arguments[0]!).Should().Equal(User, User);
        _playerPlugin.CallsTo("StartGuiTimingBar").Single().Arguments.Take(2).Should().Equal(User, 2f);
        _locals.Int(User, "IS_BUSY").Should().Be(1);
        _locals.Int(User, "BUSY_TYPE").Should().Be((int)ActivityStatusType.UseItem);
        var delays = _engine.CallsTo("DelayCommand").ToList();
        delays.Select(call => (float)call.Arguments[0]!).Should().Equal(0.1f, 2f + 0.1f);

        ((Action)delays[1].Arguments[1]!)();

        applied.Should().Equal((User, ItemA, Target, 4));
        _locals.Int(User, "IS_BUSY").Should().Be(0);
        _engine.CountOf("DestroyObject").Should().Be(0);
    }

    [Test]
    public void UseItem_WatchesTheUsersPositionAndInterruptsTheActionWhenTheyMove()
    {
        var applied = 0;
        ArrangeUse("USED_ITEM", new ItemDetail { ApplyAction = (_, _, _, _, _) => applied++ });

        _service.UseItem();
        var check = _engine.CallsTo("DelayCommand").First();
        _positions[User] = new Vector3(5, 0, 0);
        ((Action)check.Arguments[1]!)();

        Messages().Should().Equal("You move and interrupt your action.");
        _playerPlugin.CallsTo("StopGuiTimingBar").Should().ContainSingle();
        _locals.Int(User, "IS_BUSY").Should().Be(0);

        applied.Should().Be(0);
    }

    [Test]
    public void UseItem_KeepsWatchingWhileTheUserStandsStill()
    {
        ArrangeUse("USED_ITEM", new ItemDetail { ApplyAction = (_, _, _, _, _) => { } });

        _service.UseItem();
        var check = _engine.CallsTo("DelayCommand").First();
        _locals.ClearCalls();
        ((Action)check.Arguments[1]!)();

        _engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(0.1f);
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void UseItem_AppliesNothingWhenTheUserMovedBeforeTheDelayEnded()
    {
        var applied = 0;
        ArrangeUse("USED_ITEM", new ItemDetail { ApplyAction = (_, _, _, _, _) => applied++ });

        _service.UseItem();
        var apply = _engine.CallsTo("DelayCommand").Last();
        _positions[User] = new Vector3(1, 1, 1);
        ((Action)apply.Arguments[1]!)();

        applied.Should().Be(0);
        _locals.Int(User, "IS_BUSY").Should().Be(0);
    }

    [Test]
    public void UseItem_ValidatesAgainWhenTheDelayEndsAndSendsWhatItFinds()
    {
        var validations = 0;
        var applied = 0;
        ArrangeUse("USED_ITEM", new ItemDetail
        {
            ValidateAction = (_, _, _, _, _) => ++validations == 1 ? string.Empty : "Too late.",
            ApplyAction = (_, _, _, _, _) => applied++
        });

        _service.UseItem();
        var apply = _engine.CallsTo("DelayCommand").Last();
        ((Action)apply.Arguments[1]!)();

        applied.Should().Be(0);
        Messages().Should().Equal("Too late.");
    }

    [Test]
    public void UseItem_StartsTheRecastWhenTheItemIsApplied()
    {
        var group = Enum.GetValues<RecastGroup>().First(value => value != RecastGroup.Invalid);
        ArrangeUse("USED_ITEM", new ItemDetail
        {
            RecastGroup = group,
            RecastCooldown = 30f,
            ApplyAction = (_, _, _, _, _) => { }
        });
        _players.Remove(User);

        _service.UseItem();
        ((Action)_engine.CallsTo("DelayCommand").Last().Arguments[1]!)();

        _engine.CallsTo("SetLocalString").Should().Contain(call => (string)call.Arguments[1]! == $"ABILITY_RECAST_ID_{(int)group}");
    }

    [Test]
    public void UseItem_UsesUpAChargeAndDestroysTheItemOnTheLastOne()
    {
        ArrangeUse("USED_ITEM", new ItemDetail
        {
            ApplyAction = (_, _, _, _, _) => { },
            ReducesItemChargeAction = (_, _, _, _, _) => true
        });
        _engine.SetReturnSequence("GetItemCharges", 3, 1);

        _service.UseItem();
        ((Action)_engine.CallsTo("DelayCommand").Last().Arguments[1]!)();
        _engine.CallsTo("SetItemCharges").Should().ContainSingle().Which.Arguments.Should().Equal(ItemA, 2);
        _engine.CountOf("DestroyObject").Should().Be(0);

        _locals.ClearCalls();
        _service.UseItem();
        ((Action)_engine.CallsTo("DelayCommand").Last().Arguments[1]!)();
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(ItemA);
    }

    [Test]
    public void UseItem_RefusesATargetThatIsTooFarAwayOrInAnotherArea()
    {
        ArrangeUse("USED_ITEM", new ItemDetail { ApplyAction = (_, _, _, _, _) => { } });
        _world.Add(Target);
        _eventData["TARGET_OBJECT_ID"] = Target.ToString();
        _engine.SetReturn("GetDistanceBetween", 2f);

        _service.UseItem();
        Messages().Should().Equal("Your target is too far away.");

        _locals.ClearCalls();
        _engine.SetReturnHandler("GetArea", _ => Area1);
        _engine.SetReturn("GetDistanceBetween", 9f);
        _service.UseItem();
        Messages().Should().Equal("Your target is too far away.");
    }

    [Test]
    public void UseItem_AcceptsATargetInRangeAndLetsAnItemOverrideTheDistance()
    {
        ArrangeUse("USED_ITEM", new ItemDetail
        {
            ApplyAction = (_, _, _, _, _) => { },
            CalculateDistanceAction = (_, _, _, _, _) => 20f
        });
        _world.Add(Target);
        _eventData["TARGET_OBJECT_ID"] = Target.ToString();
        _engine.SetReturnHandler("GetArea", _ => Area1);
        _engine.SetReturn("GetDistanceBetween", 15f);

        _service.UseItem();

        Messages().Should().BeEmpty();
        _engine.CallsTo("DelayCommand").Should().HaveCount(2);
    }

    [Test]
    public void UseItem_RefusesAGroundLocationThatIsTooFarAway()
    {
        ArrangeUse("USED_ITEM", new ItemDetail { ApplyAction = (_, _, _, _, _) => { } });
        _engine.SetReturn("GetDistanceBetweenLocations", 8f);

        _service.UseItem();
        Messages().Should().Equal("That location is too far away.");

        _locals.ClearCalls();
        _engine.SetReturn("GetDistanceBetweenLocations", 1f);
        _engine.SetReturn("GetAreaFromLocation", Area2);
        _service.UseItem();
        Messages().Should().Equal(new[] { "That location is too far away." }, "another area is too far however near the location is");
    }

    [Test]
    public void UseItem_DoesNotCheckTheDistanceToAnItemTheUserCarries()
    {
        ArrangeUse("USED_ITEM", new ItemDetail { ApplyAction = (_, _, _, _, _) => { } });
        _world.Add(Target);
        _world.SetPossessor(Target, User);
        _eventData["TARGET_OBJECT_ID"] = Target.ToString();
        _engine.SetReturn("GetDistanceBetween", 50f);

        _service.UseItem();

        Messages().Should().BeEmpty();
    }

    [Test]
    public void UseItem_DoesNothingBeyondTheMessageForAnItemWithoutAnApplyAction()
    {
        ArrangeUse("USED_ITEM", new ItemDetail { InitializationMessageAction = (_, _, _, _, _) => "Hello." });

        _service.UseItem();

        Messages().Should().Equal("Hello.");
        _engine.CountOf("DelayCommand").Should().Be(0);
        _locals.Int(User, "IS_BUSY").Should().Be(0);
    }

    [Test]
    public void UseItem_ReadsTheTargetPositionFromTheEventWhenThereIsNoTarget()
    {
        ArrangeUse("USED_ITEM", new ItemDetail { UserFacesTarget = true });
        _eventData["TARGET_POSITION_X"] = "1.5";
        _eventData["TARGET_POSITION_Y"] = "2.5";
        _eventData["TARGET_POSITION_Z"] = "3.5";

        _service.UseItem();
        ((Action)_engine.CallsTo("AssignCommand").Single().Arguments[1]!)();

        _engine.CallsTo("SetFacingPoint").Single().Arguments[0].Should().Be(new Vector3(1.5f, 2.5f, 3.5f));
    }

    // ---- use limits

    [Test]
    public void GetCreatureItemUseError_NamesTheFirstUnmetPerkRequirement()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.AddProperty(ItemA, ItemPropertyType.UseLimitationPerk, (int)PerkType.Invalid, 5);
        _world.AddProperty(ItemA, ItemPropertyType.UseLimitationPerk, (int)PerkType.AlphaRhythm, 3);
        _service.PerkLevels[PerkType.AlphaRhythm] = 2;

        _service.GetCreatureItemUseError(User, ItemA).Should().Be($"This item requires '{PerkType.AlphaRhythm}' level 3 to use.");

        _service.PerkLevels[PerkType.AlphaRhythm] = 3;
        _service.GetCreatureItemUseError(User, ItemA).Should().BeEmpty();
        _service.CanCreatureUseItem(User, ItemA).Should().BeTrue();
    }

    [Test]
    public void GetCreatureItemUseError_NamesTheFirstUnmetSkillRequirement()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.AddProperty(ItemA, ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade, 40);
        _service.SkillRanks[SkillType.Vibroblade] = 39;

        _service.GetCreatureItemUseError(User, ItemA).Should().Be($"This item requires {SkillType.Vibroblade} rank 40 to use.");
        _service.CanCreatureUseItem(User, ItemA).Should().BeFalse();

        _service.SkillRanks[SkillType.Vibroblade] = 40;
        _service.GetCreatureItemUseError(User, ItemA).Should().BeEmpty();
    }

    [Test]
    public void GetCreatureItemUseError_ChecksTheRawAbilityScoreAndNamesTheAbilityFromTheTable()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.AddProperty(ItemA, ItemPropertyType.RequiresStat, (int)AbilityType.Might, 25);
        _creaturePlugin.SetReturn("GetRawAbilityScore", 24);

        _service.GetCreatureItemUseError(User, ItemA).Should().Be($"This item requires {AbilityType.Might} 25 to use.");

        _world.TwoDA("iprp_reqstat", "Name", (int)AbilityType.Might, "500");
        _world.StrRef(500, "Might");
        _service.GetCreatureItemUseError(User, ItemA).Should().Be("This item requires Might 25 to use.");

        _creaturePlugin.SetReturn("GetRawAbilityScore", 25);
        _service.GetCreatureItemUseError(User, ItemA).Should().BeEmpty();
    }

    [Test]
    public void GetCreatureItemUseError_ReadsAPairedMainHandPistolsSkillRequirementAsTwinPistol()
    {
        _world.Add(ItemA, baseItem: BaseItem.Pistol);
        _world.AddProperty(ItemA, ItemPropertyType.RequiresSkill, (int)SkillType.Pistol, 30);
        _service.SkillRanks[SkillType.Pistol] = 100;
        _service.SkillRanks[SkillType.TwinPistol] = 10;

        _service.GetCreatureItemUseError(User, ItemA).Should().BeEmpty();
        _service.GetCreatureItemUseError(User, ItemA, SkillType.TwinPistol)
            .Should().Be($"This item requires {SkillType.TwinPistol} rank 30 to use.");
    }

    // ---- equipping

    [Test]
    public void CanEquip_RefusesAPairingOfALegacyPistolWithACompanionPistol()
    {
        _world.Add(ItemA, baseItem: BaseItem.LegacyPistol);
        _world.Add(ItemB, baseItem: BaseItem.CompanionPistol);
        _world.Equip(User, InventorySlot.RightHand, ItemA);
        _world.Equip(User, InventorySlot.LeftHand, ItemB);

        _service.CanEquip(User, ItemA).Should().Be("Legacy pistols cannot be paired with a companion pistol.");
    }

    [Test]
    public void CanEquip_ChecksTheHandsTheItemWouldTakeWhenItIsGoingIntoASlot()
    {
        _world.Add(ItemA, baseItem: BaseItem.LegacyPistol);
        _world.Add(ItemB, baseItem: BaseItem.CompanionPistol);
        _world.Add(ItemC, baseItem: BaseItem.Longsword);
        _world.Equip(User, InventorySlot.LeftHand, ItemB);

        _service.CanEquip(User, ItemA, InventorySlot.RightHand).Should().Be("Legacy pistols cannot be paired with a companion pistol.");
        _service.CanEquip(User, ItemC, InventorySlot.Head).Should().BeEmpty("a slot other than a hand does not take the pair rule");
    }

    [Test]
    public void CanEquip_LetsOnlyForceSensitivePlayersEquipLightsabersAndSaberstaffs()
    {
        _world.Add(ItemA, baseItem: BaseItem.Lightsaber);
        _db.With(new Player(PlayerId) { CharacterType = CharacterType.Standard });

        _service.CanEquip(User, ItemA).Should().Be("Only Force Sensitive characters may equip that item.");

        _db.With(new Player(PlayerId) { CharacterType = CharacterType.ForceSensitive });
        _service.CanEquip(User, ItemA).Should().BeEmpty();
    }

    [Test]
    public void CanEquip_LetsDMsAndNonPlayerCreaturesEquipAnythingElse()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.Add(ItemB, baseItem: BaseItem.Longsword);
        _world.AddProperty(ItemA, ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade, 99);
        _world.Add(Other);

        _service.CanEquip(Other, ItemA).Should().BeEmpty("a creature that is not a player or a droid is not limited");

        _players.Add(Other);
        _dms.Add(Other);
        _service.CanEquip(Other, ItemA).Should().BeEmpty("a DM is not limited");
        _dms.Remove(Other);
        _possessed.Add(Other);
        _service.CanEquip(Other, ItemA).Should().BeEmpty("a possessed creature is not limited");
    }

    [Test]
    public void CanEquip_RefusesWhileTheCraftWindowIsOpen()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _craftWindowOpen.Add(User);

        _service.CanEquip(User, ItemA).Should().Be("Items cannot be equipped while crafting.");
    }

    [Test]
    public void CanEquip_ChecksTheUseLimitsOfAnOrdinaryItemAndThenItsRacialLimits()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.AddProperty(ItemA, ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade, 5);
        _service.SkillRanks[SkillType.Vibroblade] = 1;

        _service.CanEquip(User, ItemA).Should().Be($"This item requires {SkillType.Vibroblade} rank 5 to use.");

        _service.SkillRanks[SkillType.Vibroblade] = 5;
        _service.CanEquip(User, ItemA).Should().BeEmpty();

        _world.AddProperty(ItemA, ItemPropertyType.UseLimitationRacialType, (int)RacialType.Droid);
        _service.CanEquip(User, ItemA).Should().Be("This item may only be equipped by Droids.");
    }

    [Test]
    public void CanEquip_AppliesTheLimitsToADroidTooAndIgnoresAPlainCreature()
    {
        _world.Add(Other, resref: Droid.DroidResref);
        _races[Other] = RacialType.Droid;
        _world.Add(ItemA, baseItem: BaseItem.Armor);

        _service.CanEquip(Other, ItemA).Should().Be("Droids may not equip that item.");
    }

    [Test]
    public void CanEquip_ExplainsHowTheCompanionPistolIsMade()
    {
        _world.Add(ItemA, baseItem: BaseItem.CompanionPistol);
        _world.Add(ItemB, baseItem: BaseItem.Pistol);
        _world.Equip(User, InventorySlot.RightHand, ItemB);

        _service.CanEquip(User, ItemA, InventorySlot.LeftHand)
            .Should().Be("The off-hand pistol is created automatically by equipping a Twin Pistols item.");
    }

    [Test]
    public void CanEquip_RefusesALinkedCompanionPistolWithAnUnsupportedProperty()
    {
        var pair = ArrangeLinkedPair();
        _world.AddProperty(pair.Off, ItemPropertyType.ACBonus);

        _service.CanEquip(User, pair.Off, InventorySlot.LeftHand)
            .Should().Be("Companion pistols cannot use native or creature-wide equipment bonuses.");
    }

    [Test]
    public void CanEquip_AcceptsALinkedPairWhoseMembersMeetTheirRequirements()
    {
        var pair = ArrangeLinkedPair();

        _service.CanEquip(User, pair.Main).Should().BeEmpty();
        _service.CanEquip(User, pair.Off).Should().BeEmpty();
        _service.CanCreatureUseItem(User, pair.Main).Should().BeTrue("an active pair reads the main hand's skill as Twin Pistol");
    }

    [Test]
    public void CanEquip_ChecksBothMembersOfThePairWhicheverHandIsEquippedFirst()
    {
        var pair = ArrangeLinkedPair();
        _world.AddProperty(pair.Main, ItemPropertyType.RequiresSkill, (int)SkillType.Pistol, 10);
        _service.SkillRanks[SkillType.TwinPistol] = 1;

        _service.CanEquip(User, pair.Main).Should().Be($"This item requires {SkillType.TwinPistol} rank 10 to use.");

        _service.SkillRanks[SkillType.TwinPistol] = 10;
        _world.AddProperty(pair.Off, ItemPropertyType.RequiresSkill, (int)SkillType.Pistol, 20);
        _service.SkillRanks[SkillType.Pistol] = 1;
        _service.CanEquip(User, pair.Off).Should().Be($"This item requires {SkillType.Pistol} rank 20 to use.");
    }

    [Test]
    public void CanEquip_RefusesATwinPistolItemWhileAnUnlinkedItemIsInTheOffHand()
    {
        var pair = ArrangeLinkedPair();
        _world.Add(ItemC, baseItem: BaseItem.SmallShield);
        _world.Equip(User, InventorySlot.LeftHand, ItemC);

        _service.CanEquip(User, pair.Main, InventorySlot.RightHand)
            .Should().Be("Twin Pistols require both hands. Unequip your off-hand item first.");
    }

    // ---- racial limits

    [Test]
    public void GetRacialEquipError_RequiresTheDroidLimitationOnDroidGearForADroid()
    {
        _world.Add(ItemA, baseItem: BaseItem.Armor);
        _races[User] = RacialType.Droid;

        _service.GetRacialEquipError(User, ItemA).Should().Be("Droids may not equip that item.");

        _world.AddProperty(ItemA, ItemPropertyType.UseLimitationRacialType, (int)RacialType.Droid);
        _service.GetRacialEquipError(User, ItemA).Should().BeEmpty();
    }

    [Test]
    public void GetRacialEquipError_KeepsDroidOnlyGearFromEveryoneElse()
    {
        _world.Add(ItemA, baseItem: BaseItem.Armor);
        _world.AddProperty(ItemA, ItemPropertyType.UseLimitationRacialType, (int)RacialType.Human);
        _world.AddProperty(ItemA, ItemPropertyType.UseLimitationRacialType, (int)RacialType.Droid);

        _service.GetRacialEquipError(User, ItemA).Should().Be("This item may only be equipped by Droids.");
    }

    [Test]
    public void GetRacialEquipError_LeavesAnItemWithNoRacialLimitAlone()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.AddProperty(ItemA, ItemPropertyType.DMG, costTableValue: 2);
        _races[User] = RacialType.Droid;

        _service.GetRacialEquipError(User, ItemA).Should().BeEmpty("a sword is not droid gear");
    }

    // ---- Force Sensitive weapons

    [Test]
    public void CanEquipForceSensitiveWeapon_IsTrueForDMsAndPossessedCreatures()
    {
        _dms.Add(Other);
        _service.CanEquipForceSensitiveWeapon(Other).Should().BeTrue();
        _dms.Remove(Other);
        _possessed.Add(Other);
        _service.CanEquipForceSensitiveWeapon(Other).Should().BeTrue();
    }

    [Test]
    public void CanEquipForceSensitiveWeapon_IsFalseForDroidsAndBeasts()
    {
        _world.Add(Other, resref: Droid.DroidResref);
        _service.CanEquipForceSensitiveWeapon(Other).Should().BeFalse();

        _world.Add(ItemA);
        _engine.SetLocalInt(ItemA, "BEAST_TYPE", (int)Enum.GetValues<BeastType>().First(type => type != BeastType.Invalid));
        _service.CanEquipForceSensitiveWeapon(ItemA).Should().BeFalse();
    }

    [Test]
    public void CanEquipForceSensitiveWeapon_IsTrueForAnyOtherCreatureThatIsNotAPlayer()
    {
        _world.Add(Other);
        _service.CanEquipForceSensitiveWeapon(Other).Should().BeTrue();
    }

    [Test]
    public void CanEquipForceSensitiveWeapon_AnswersForADMOrAPossessedCreatureWithoutReadingAPlayerRecord()
    {
        _dms.Add(User);
        _service.CanEquipForceSensitiveWeapon(User).Should().BeTrue();
        _dms.Remove(User);
        _possessed.Add(User);
        _service.CanEquipForceSensitiveWeapon(User).Should().BeTrue();

        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void CanEquip_AppliesTheForceSensitiveLimitBeforeTheShortcutForCreaturesThatAreNotPlayers()
    {
        const uint beast = 1003;
        const uint plainCreature = 1004;
        _world.Add(ItemA, baseItem: BaseItem.Saberstaff);
        _world.Add(Other, resref: Droid.DroidResref);
        _world.Add(beast);
        _engine.SetLocalInt(beast, "BEAST_TYPE", (int)Enum.GetValues<BeastType>().First(type => type != BeastType.Invalid));
        _world.Add(plainCreature);

        _service.CanEquip(Other, ItemA).Should().Be("Only Force Sensitive characters may equip that item.");
        _service.CanEquip(beast, ItemA).Should().Be("Only Force Sensitive characters may equip that item.");
        _service.CanEquip(plainCreature, ItemA).Should().BeEmpty();
    }

    [Test]
    public void CanEquipForceSensitiveWeapon_ReadsAPlayersCharacterType()
    {
        _service.CanEquipForceSensitiveWeapon(User).Should().BeFalse("a player with no record is not Force Sensitive");

        _db.With(new Player(PlayerId) { CharacterType = CharacterType.Standard });
        _service.CanEquipForceSensitiveWeapon(User).Should().BeFalse();

        _db.With(new Player(PlayerId) { CharacterType = CharacterType.ForceSensitive });
        _service.CanEquipForceSensitiveWeapon(User).Should().BeTrue();
    }

    // ---- returning and counting items

    [Test]
    public void ReturnItem_GivesAContainerBackThroughItsPossessor()
    {
        _world.Add(Bag, baseItem: BaseItem.MiscSmall);
        _world.MakeContainer(Bag);
        _world.SetPossessor(Bag, Other);

        _service.ReturnItem(User, Bag);

        var assigned = _engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(Other);
        _locals.ClearCalls();
        ((Action)assigned.Arguments[1]!)();
        _engine.CallsTo("ActionGiveItem").Should().ContainSingle().Which.Arguments.Should().Equal(Bag, User);
    }

    [Test]
    public void ReturnItem_CopiesAnOrdinaryItemToTheTargetAndDestroysTheOriginal()
    {
        _world.Add(ItemA, baseItem: BaseItem.MiscSmall);

        _service.ReturnItem(User, ItemA);

        _engine.CallsTo("CopyItem").Should().ContainSingle().Which.Arguments.Should().Equal(ItemA, User, true);
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(ItemA);
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void GetInventoryItemCount_IsMinusOneWithoutAnInventoryAndOtherwiseTheNumberOfItems()
    {
        _world.Add(Bag);
        _service.GetInventoryItemCount(Bag).Should().Be(-1);

        _world.MakeContainer(Bag);
        _service.GetInventoryItemCount(Bag).Should().Be(0);

        _world.Add(ItemA);
        _world.Add(ItemB);
        _world.Carry(Bag, ItemA, ItemB);
        _service.GetInventoryItemCount(Bag).Should().Be(2);
    }

    [Test]
    public void TryAcquireItem_MovesAnItemThatFitsIntoTheInventory()
    {
        _world.Add(ItemA);
        _objectPlugin.SetReturn("AcquireItem", true);

        _service.TryAcquireItem(User, ItemA).Should().BeTrue();

        _objectPlugin.CallsTo("AcquireItem").Should().ContainSingle().Which.Arguments.Should().Equal(User, ItemA);
        _engine.CountOf("DestroyObject").Should().Be(0);
    }

    [Test]
    public void TryAcquireItem_DestroysAnItemThatHasNoRoom()
    {
        _world.Add(ItemA);
        _service.Fits = (_, _) => false;
        _objectPlugin.SetReturn("AcquireItem", true);

        _service.TryAcquireItem(User, ItemA).Should().BeFalse();

        _objectPlugin.CountOf("AcquireItem").Should().Be(0, "the engine removes carried items when an item has no room, so it is never asked");
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(ItemA);
    }

    [Test]
    public void TryAcquireItem_DestroysAnItemTheEngineRefuses()
    {
        _world.Add(ItemA);
        _objectPlugin.SetReturn("AcquireItem", false);

        _service.TryAcquireItem(User, ItemA).Should().BeFalse();

        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(ItemA);
    }

    [Test]
    public void TryAcquireItem_LeavesAnInvalidItemAlone()
    {
        _service.TryAcquireItem(User, ItemA).Should().BeFalse();

        _engine.CountOf("DestroyObject").Should().Be(0);
        _objectPlugin.CountOf("AcquireItem").Should().Be(0);
    }

    // ---- icons

    [Test]
    public void GetIconResref_UsesTheCloakIconWhenItExists()
    {
        _world.Add(ItemA, baseItem: BaseItem.Cloak);
        _world.Texture("iit_cloak", ResType.DDS);

        _service.GetIconResref(ItemA).Should().Be("iit_cloak");
        _service.HasInventoryIcon(ItemA).Should().BeTrue();
    }

    [Test]
    public void GetIconResref_TakesAScrollsIconFromItsCastSpellProperty()
    {
        _world.Add(ItemA, baseItem: BaseItem.SpellScroll);
        _world.AddProperty(ItemA, ItemPropertyType.DMG);
        _world.AddProperty(ItemA, ItemPropertyType.CastSpell, 12);
        _world.TwoDA("iprp_spells", "Icon", 12, "isp_fireball");
        _world.TwoDA("baseitems", "DefaultIcon", (int)BaseItem.SpellScroll, "iit_scroll");
        _world.Texture("isp_fireball");

        _service.GetIconResref(ItemA).Should().Be("isp_fireball");
    }

    [Test]
    public void GetIconResref_GivesAScrollWithoutACastSpellItsDefaultIcon()
    {
        _world.Add(ItemB, baseItem: BaseItem.EnchantedScroll);
        _world.TwoDA("baseitems", "DefaultIcon", (int)BaseItem.EnchantedScroll, "iit_scroll");
        _world.Texture("iit_scroll");

        _service.GetIconResref(ItemB).Should().Be("iit_scroll");
    }

    [Test]
    public void GetIconResref_BuildsASimpleModelIconFromItsItemClass()
    {
        _world.Add(ItemA, baseItem: BaseItem.MiscSmall);
        _world.TwoDA("baseitems", "ModelType", (int)BaseItem.MiscSmall, "0");
        _world.TwoDA("baseitems", "ItemClass", (int)BaseItem.MiscSmall, "MiscSm");
        _world.Texture("imiscsm_007");

        _service.GetIconResref(ItemA).Should().Be("imiscsm_007");
    }

    [TestCase(BaseItem.MiscSmall, "iit_smlmisc_")]
    [TestCase(BaseItem.MiscMedium, "iit_midmisc_")]
    [TestCase(BaseItem.MiscLarge, "iit_talmisc_")]
    [TestCase(BaseItem.MiscThin, "iit_thnmisc_")]
    public void GetIconResref_ChoosesThePrefixOfASimpleModelByItsSize(BaseItem baseItem, string prefix)
    {
        _world.Add(ItemA, baseItem: baseItem);
        _world.TwoDA("baseitems", "ModelType", (int)baseItem, "0");
        _world.TwoDA("baseitems", "DefaultIcon", (int)baseItem, "iinvalid_2x2");
        _world.Texture(prefix + "007");

        _service.GetIconResref(ItemA).Should().Be(prefix + "007");
    }

    [Test]
    public void GetIconResref_StripsTheNumberSuffixOfADefaultIconBeforeAddingTheModelNumber()
    {
        _world.Add(ItemA, baseItem: BaseItem.Gold);
        _world.TwoDA("baseitems", "ModelType", (int)BaseItem.Gold, "0");
        _world.TwoDA("baseitems", "DefaultIcon", (int)BaseItem.Gold, "iit_coin_001");
        _world.Texture("iit_coin_007");

        _service.GetIconResref(ItemA).Should().Be("iit_coin_007");
    }

    [Test]
    public void GetIconResref_FallsBackToTheDefaultIconAndThenToTheGenericOne()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.TwoDA("baseitems", "DefaultIcon", (int)BaseItem.Longsword, "iwsword");

        _service.GetIconResref(ItemA).Should().Be("iit_smlmisc_001");
        _service.HasInventoryIcon(ItemA).Should().BeFalse("only the generic placeholder resolved");

        _world.Texture("iwsword_001");
        _service.GetIconResref(ItemA).Should().Be("iwsword_001", "some default icons keep a variant suffix the table omits");

        _world.Texture("iwsword");
        _service.GetIconResref(ItemA).Should().Be("iwsword");
        _service.HasInventoryIcon(ItemA).Should().BeTrue();
    }

    [Test]
    public void GetIconResref_NeverReturnsAnInvalidPlaceholderIcon()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.TwoDA("baseitems", "DefaultIcon", (int)BaseItem.Longsword, "iinvalid_2x2");
        _world.Texture("iinvalid_2x2");

        _service.GetIconResref(ItemA).Should().Be("iit_smlmisc_001");
    }

    [Test]
    public void GetIconLayers_GivesAThreePartWeaponOneIconPerModelPart()
    {
        _world.Add(ItemA, baseItem: BaseItem.Lightsaber);
        _world.TwoDA("baseitems", "ModelType", (int)BaseItem.Lightsaber, "2");
        _world.TwoDA("baseitems", "ItemClass", (int)BaseItem.Lightsaber, "WSwDg");
        foreach (var part in new[] { "b", "m", "t" })
            _world.Texture($"iwswdg_{part}_021");

        _service.GetIconLayers(ItemA).Should().Equal("iwswdg_b_021", "iwswdg_m_021", "iwswdg_t_021");
    }

    [Test]
    public void GetIconLayers_FallsBackToTheSingleIconWhenAPartIsMissingAndForOtherItems()
    {
        _world.Add(ItemA, baseItem: BaseItem.Lightsaber);
        _world.TwoDA("baseitems", "ModelType", (int)BaseItem.Lightsaber, "2");
        _world.TwoDA("baseitems", "ItemClass", (int)BaseItem.Lightsaber, "WSwDg");
        _world.TwoDA("baseitems", "DefaultIcon", (int)BaseItem.Lightsaber, "iwswdg_b_001");
        _world.Texture("iwswdg_b_021");
        _world.Texture("iwswdg_b_001");

        _service.GetIconLayers(ItemA).Should().Equal("iwswdg_b_001");

        _world.TwoDA("baseitems", "ItemClass", (int)BaseItem.Lightsaber, "");
        _service.GetIconLayers(ItemA).Should().Equal(new[] { "iwswdg_b_001" }, "without an item class there are no part icons");

        _world.Add(ItemB, baseItem: BaseItem.Longsword);
        _world.TwoDA("baseitems", "DefaultIcon", (int)BaseItem.Longsword, "iwsword");
        _world.Texture("iwsword");
        _service.GetIconLayers(ItemB).Should().Equal("iwsword");
    }

    // ---- economy

    [Test]
    public void IsEconomyRestricted_ChecksTheNoEconomyFlagTheNameTheBaseItemAndTheIcon()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword, name: "Basic Longsword");
        _world.TwoDA("baseitems", "DefaultIcon", (int)BaseItem.Longsword, "iwsword");
        _world.Texture("iwsword");

        _service.IsEconomyRestricted(ItemA).Should().BeFalse();

        _engine.SetLocalInt(ItemA, ItemService.NoEconomyVariable, 1);
        _service.IsEconomyRestricted(ItemA).Should().BeTrue("a blueprint can opt out of the economy");
    }

    [Test]
    public void IsEconomyRestricted_HidesNpcGearCreatureItemsAndItemsWithoutARealIcon()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword, name: "[NPC] Longsword");
        _world.Add(ItemB, baseItem: BaseItem.CreatureItem, name: "Skin");
        _world.Add(ItemC, baseItem: BaseItem.Longsword, name: "Plain Longsword");

        _service.IsEconomyRestricted(ItemA).Should().BeTrue();
        _service.IsEconomyRestricted(ItemB).Should().BeTrue();
        _service.IsEconomyRestricted(ItemC).Should().BeTrue("it has no real inventory icon");
    }

    [Test]
    public void TheDataOnlyEconomyChecksAreTheOnesTheModuleEditorUses()
    {
        _service.IsEconomyRestricted(BaseItem.Longsword, "Basic Longsword", false, true).Should().BeFalse();
        _service.IsEconomyRestricted(BaseItem.Longsword, "Basic Longsword", true, true).Should().BeTrue();
        _service.IsEconomyRestricted(BaseItem.Longsword, "Basic Longsword", false, false).Should().BeTrue();
        _service.IsEconomyRestricted(BaseItem.CompanionPowerGlove, "Glove", false, true).Should().BeTrue();
        _service.IsEconomyRestricted(BaseItem.Longsword, "(NPC 2) Sword", false, true).Should().BeTrue();
        _service.IsEconomyRestrictedName("  [npc] Sword").Should().BeTrue();
        _service.IsEconomyRestrictedName("Sword").Should().BeFalse();
    }

    // ---- item property text

    [Test]
    public void BuildItemPropertyString_JoinsTheNameSubTypeParameterAndCostOfEachProperty()
    {
        ArrangePropertyText();

        var text = _service.BuildItemPropertyString(ItemA);

        text.Should().Be("Damage Bonus Slashing Plus 1d4 +3\nAttack Bonus\n");
    }

    [Test]
    public void BuildItemPropertyList_HasOneEntryPerPropertyAndAnEmptyOneForAPropertyWithoutAName()
    {
        ArrangePropertyText();
        _world.AddProperty(ItemA, ItemPropertyType.ACBonus);

        var list = _service.BuildItemPropertyList(ItemA);

        list.Should().Equal("Damage Bonus Slashing Plus 1d4 +3", "Attack Bonus", string.Empty);
    }

    [Test]
    public void BuildItemPropertyList_AlsoDescribesAListOfProperties()
    {
        ArrangePropertyText();
        var properties = new List<ItemProperty> { _world.Property(ItemA, 1), _world.Property(ItemA, 0) };

        var list = _service.BuildItemPropertyList(properties);

        list.Should().Equal("Attack Bonus", "Damage Bonus Slashing Plus 1d4 +3");
    }

    // ---- storing, damage rating, stacks, slots

    [Test]
    public void CanBePersistentlyStored_NamesTheFirstReasonAnItemCannotBeStored()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword, resref: "sword");

        _service.CanBePersistentlyStored(User, ItemA).Should().Be("Item must be in your inventory.");

        _world.Carry(User, ItemA);
        _service.CanBePersistentlyStored(User, ItemA).Should().BeEmpty();

        _world.Equip(User, InventorySlot.RightHand, ItemA);
        _service.CanBePersistentlyStored(User, ItemA).Should().Be("Unequip the item first.");
    }

    [Test]
    public void CanBePersistentlyStored_RefusesContainersCreditsCursedItemsAndTheDroidControlItem()
    {
        _world.Add(Bag, baseItem: BaseItem.MiscSmall);
        _world.MakeContainer(Bag);
        _world.Carry(User, Bag);
        _service.CanBePersistentlyStored(User, Bag).Should().Be("Containers cannot be stored.");

        _world.Add(ItemA, baseItem: BaseItem.Gold);
        _world.Carry(User, ItemA);
        _service.CanBePersistentlyStored(User, ItemA).Should().Be("Credits cannot be placed inside.");

        _world.Add(ItemB, baseItem: BaseItem.MiscSmall);
        _world.Carry(User, ItemB);
        _engine.SetReturnHandler("GetItemCursedFlag", args => (uint)args[0]! == ItemB);
        _service.CanBePersistentlyStored(User, ItemB).Should().Be("That item cannot be stored.");

        _world.Add(ItemC, baseItem: BaseItem.MiscSmall, resref: Droid.DroidControlItemResref);
        _world.Carry(User, ItemC);
        _service.CanBePersistentlyStored(User, ItemC).Should().Be("That item cannot be stored.");
    }

    [Test]
    public void GetDMG_AddsUpTheDamageRatingAndIsNeverBelowOne()
    {
        _world.Add(ItemA);
        _service.GetDMG(ItemA).Should().Be(1);

        _world.AddProperty(ItemA, ItemPropertyType.ACBonus, costTableValue: 9);
        _world.AddProperty(ItemA, ItemPropertyType.DMG, costTableValue: 3);
        _world.AddProperty(ItemA, ItemPropertyType.DMG, costTableValue: 4);
        _service.GetDMG(ItemA).Should().Be(7);

        _world.Add(ItemB);
        _world.AddProperty(ItemB, ItemPropertyType.DMG, costTableValue: 0);
        _service.GetDMG(ItemB).Should().Be(1);
    }

    [Test]
    public void ReduceItemStack_ShrinksTheStackAndDestroysTheItemAtZero()
    {
        _engine.SetReturn("GetItemStackSize", 5);

        _service.ReduceItemStack(ItemA, 2).Should().BeTrue();
        _engine.CallsTo("SetItemStackSize").Should().ContainSingle().Which.Arguments.Should().Equal(ItemA, 3);

        _locals.ClearCalls();
        _service.ReduceItemStack(ItemA, 5).Should().BeTrue();
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(ItemA);
        _engine.CountOf("SetItemStackSize").Should().Be(0);
    }

    [Test]
    public void ReduceItemStack_RefusesWhatTheStackCannotGive()
    {
        _engine.SetReturn("GetItemStackSize", 2);

        _service.ReduceItemStack(ItemA, 3).Should().BeFalse("the stack is smaller than the amount");
        _service.ReduceItemStack(ItemA, 0).Should().BeFalse("at least one must be taken");

        _engine.CountOf("DestroyObject").Should().Be(0);
        _engine.CountOf("SetItemStackSize").Should().Be(0);
    }

    [Test]
    public void TheItemTagFlagsAreReadFromTheItem()
    {
        _world.Add(ItemA, "LEGACY_ITEM");
        _world.Add(ItemB, "OTHER");
        _engine.SetLocalInt(ItemA, ItemService.PlayerProducedItemVariable, 1);

        _service.IsLegacyItem(ItemA).Should().BeTrue();
        _service.IsLegacyItem(ItemB).Should().BeFalse();
        _service.IsPlayerProducedItem(ItemA).Should().BeTrue();
        _service.IsPlayerProducedItem(ItemB).Should().BeFalse();
    }

    [TestCase(InventorySlot.Head)]
    [TestCase(InventorySlot.RightHand)]
    [TestCase(InventorySlot.Neck)]
    [TestCase(InventorySlot.CreatureArmor)]
    public void GetItemSlot_FindsTheSlotAnItemIsEquippedIn(InventorySlot slot)
    {
        _world.Add(ItemA);
        _world.Equip(User, slot, ItemA);

        _service.GetItemSlot(User, ItemA).Should().Be(slot);
    }

    [Test]
    public void GetItemSlot_IsInvalidForAnItemThatIsNotEquipped()
    {
        _world.Add(ItemA);

        _service.GetItemSlot(User, ItemA).Should().Be(InventorySlot.Invalid);
    }

    // ---- the facade resolves this instance

    [Test]
    public void TheFacadeForwardsToTheContainersInstance()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);

        Item.IsBaseItemType(ItemA, Item.WeaponBaseItemTypes).Should().BeTrue();
        Item.IsAttackWeaponType(BaseItem.Longsword).Should().BeTrue();
        Item.WeaponBaseItemTypes.Should().BeSameAs(_service.WeaponBaseItemTypes);
    }

    // ---- scenery

    private void RegisterItem(string tag, ItemDetail detail)
    {
        ScriptedItems.Details[tag] = detail;
        if (!_service.Definitions.Contains(typeof(ScriptedItems)))
            _service.Definitions.Add(typeof(ScriptedItems));
        _service.CacheData();
    }

    private void ArrangeUse(string tag, ItemDetail? detail)
    {
        if (detail != null)
            RegisterItem(tag, detail);
        _world.Add(ItemA, tag, BaseItem.MiscSmall);
        _eventData["ITEM_OBJECT_ID"] = ItemA.ToString();
        _eventData["TARGET_OBJECT_ID"] = ObjectInvalid.ToString();
        _eventData["ITEM_PROPERTY_INDEX"] = "0";
        _eventData["TARGET_POSITION_X"] = "0";
        _eventData["TARGET_POSITION_Y"] = "0";
        _eventData["TARGET_POSITION_Z"] = "0";
        _positions[User] = Vector3.Zero;
        _engine.SetReturn("GetDistanceBetweenLocations", 1f);
        _engine.SetReturn("GetAreaFromLocation", Area1);
    }

    private IReadOnlyList<string> Messages() =>
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    /// <summary>A main-hand Twin Pistols item and its generated off-hand companion, linked as the pair rules need.</summary>
    private (uint Main, uint Off) ArrangeLinkedPair()
    {
        _world.Add(ItemA, baseItem: BaseItem.Pistol);
        _world.Add(ItemB, baseItem: BaseItem.CompanionPistol);
        _engine.SetLocalInt(ItemA, TwinPistolEquipment.PairVariable, 1);
        _engine.SetLocalInt(ItemB, TwinPistolEquipment.SpareVariable, 1);
        _engine.SetLocalObject(ItemB, TwinPistolEquipment.MainWeaponVariable, ItemA);
        _engine.SetLocalObject(ItemB, TwinPistolEquipment.OwnerVariable, User);
        _world.Equip(User, InventorySlot.RightHand, ItemA);
        _world.Equip(User, InventorySlot.LeftHand, ItemB);
        return (ItemA, ItemB);
    }

    private void ArrangePropertyText()
    {
        _world.Add(ItemA, baseItem: BaseItem.Longsword);
        _world.AddProperty(ItemA, ItemPropertyType.DMG, subType: 2, costTable: 4, costTableValue: 3, param1: 1, param1Value: 5);
        _world.AddProperty(ItemA, ItemPropertyType.Delay);
        _world.TwoDA("itempropdef", "GameStrRef", (int)ItemPropertyType.DMG, "100");
        _world.TwoDA("itempropdef", "SubTypeResRef", (int)ItemPropertyType.DMG, "iprp_damagetype");
        _world.TwoDA("iprp_damagetype", "Name", 2, "200");
        _world.TwoDA("iprp_paramtable", "TableResRef", 1, "iprp_bonuscost");
        _world.TwoDA("iprp_bonuscost", "Name", 5, "300");
        _world.TwoDA("iprp_costtable", "Name", 4, "iprp_meleecost");
        _world.TwoDA("iprp_meleecost", "Name", 3, "400");
        _world.TwoDA("itempropdef", "GameStrRef", (int)ItemPropertyType.Delay, "101");
        _world.StrRef(100, "Damage Bonus");
        _world.StrRef(101, "Attack Bonus");
        _world.StrRef(200, "Slashing");
        _world.StrRef(300, "Plus 1d4");
        _world.StrRef(400, "+3");
    }

    /// <summary>The item instance with every call that needs the database, the static catalogs or the native engine answered by the test.</summary>
    private sealed class TestItemService : ItemService
    {
        public TestItemService(IBeastMasteryService beastMastery, IDBService db)
            : base(beastMastery, db)
        {
        }

        public List<Type> Definitions { get; } = new();

        public Func<uint, uint, bool> Fits { get; set; } = (_, _) => true;

        public Dictionary<PerkType, int> PerkLevels { get; } = new();

        public Dictionary<SkillType, int> SkillRanks { get; } = new();

        protected override IEnumerable<Type> GetItemListDefinitionTypes() => Definitions;

        public override bool FitsInInventory(uint creature, uint item) => Fits(creature, item);

        protected override int GetPerkLevel(uint creature, PerkType perkType) => PerkLevels.GetValueOrDefault(perkType);

        protected override string GetPerkName(PerkType perkType) => perkType.ToString();

        protected override int GetCreatureSkillRank(uint creature, SkillType skillType) => SkillRanks.GetValueOrDefault(skillType);

        protected override string GetSkillName(SkillType skillType) => skillType.ToString();
    }

    /// <summary>The instance with the default definition search, which a test can run.</summary>
    private sealed class ProbingItemService : ItemService
    {
        public ProbingItemService(IBeastMasteryService beastMastery, IDBService db)
            : base(beastMastery, db)
        {
        }

        public IEnumerable<Type> Search() => GetItemListDefinitionTypes();
    }

    /// <summary>An item list whose items the running test chooses.</summary>
    public sealed class ScriptedItems : IItemListDefinition
    {
        public static Dictionary<string, ItemDetail> Details { get; } = new();

        public Dictionary<string, ItemDetail> BuildItems() => new(Details);
    }
}
