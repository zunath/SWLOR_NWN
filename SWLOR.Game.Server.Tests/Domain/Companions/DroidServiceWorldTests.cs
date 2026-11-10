using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// The droid instance driven through a stateful engine (<see cref="DroidWorld"/>): spawning from the controller, the
/// saved equipment and inventory, despawning, the item snapshot and every droid event handler. The neighbour services
/// are recording stand-ins, so each test reads what the droid asked of them.
/// </summary>
[TestFixture]
public class DroidServiceWorldTests
{
    private const uint Invalid = DroidWorld.ObjectInvalid;

    private DroidWorld _world = null!;
    private AmmunitionDroidService _service = null!;

    private sealed class AmmunitionDroidService : DroidService
    {
        public AmmunitionDroidService(DroidWorld world)
            : base(world.Ai, world.Recorder, world.Companion, world.Diagnostics, world.Gui, world.Item, world.Perk, world.Glove,
                world.Race, world.Recast, world.Resistance, world.Stat, world.StatusEffect, world.Twin)
        {
        }

        public (bool Arrows, bool Bullets, bool Bolts)? Flags { get; set; }

        protected override (bool Arrows, bool Bullets, bool Bolts)? ReadGeneratedAmmunitionFlags(uint droid) => Flags;
    }

    [SetUp]
    public void SetUp()
    {
        _world = new DroidWorld();
        _service = new AmmunitionDroidService(_world);
        _service.CacheData();
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    private ConstructedDroid Saved() => _service.LoadConstructedDroid(DroidWorld.Controller);

    private void Self(uint obj) => _world.Engine.SetReturn("OBJECT_SELF", obj);

    private void ConfigureController()
    {
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.Tier, 3);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.HP, 40);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.STM, 7);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.MGT, 11);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.PER, 12);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.VIT, 13);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.WIL, 14);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.AGI, 15);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.SOC, 16);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.ResistanceFire, 5);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.Armor, 2);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.Rifle, 4);
        _world.AddProperty(DroidWorld.Controller, ItemPropertyType.DroidPersonality, (int)DroidPersonalityType.Sarcastic, 0);
        _service.SaveConstructedDroid(DroidWorld.Controller, new ConstructedDroid { Name = "Beepy" });
    }

    // ---- property details

    [Test]
    public void LoadDroidItemPropertyDetails_ReadsEveryStatOfTheController()
    {
        ConfigureController();
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.HP, 10);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.AISlots, 2);

        var details = _service.LoadDroidItemPropertyDetails(DroidWorld.Controller);

        details.Tier.Should().Be(3);
        details.Level.Should().Be(25);
        details.HP.Should().Be(50);
        details.AISlots.Should().Be(2);
        (details.STM, details.MGT, details.PER, details.VIT, details.WIL, details.AGI, details.SOC)
            .Should().Be((7, 11, 12, 13, 14, 15, 16));
        details.Resistances[ResistanceType.Fire].Should().Be(5);
        details.Skills[SkillType.Armor].Should().Be(2);
        details.Skills[SkillType.Rifle].Should().Be(4);
        details.PersonalityType.Should().Be(DroidPersonalityType.Sarcastic);
        details.CustomName.Should().Be("Beepy");
        details.Perks.Should().BeEmpty();
    }

    [Test]
    public void LoadDroidItemPropertyDetails_AddsEveryResistanceAndWeaponSkillAndIgnoresTheSaberSkills()
    {
        var resistances = new Dictionary<DroidStatSubType, ResistanceType>
        {
            [DroidStatSubType.ResistanceFire] = ResistanceType.Fire,
            [DroidStatSubType.ResistancePoison] = ResistanceType.Poison,
            [DroidStatSubType.ResistanceElectrical] = ResistanceType.Electrical,
            [DroidStatSubType.ResistanceIce] = ResistanceType.Ice,
            [DroidStatSubType.ResistanceMind] = ResistanceType.Mind,
            [DroidStatSubType.ResistanceMobility] = ResistanceType.Mobility,
            [DroidStatSubType.ResistanceTrauma] = ResistanceType.Trauma,
            [DroidStatSubType.ResistanceDisruption] = ResistanceType.Disruption
        };
        var skills = new Dictionary<DroidStatSubType, SkillType>
        {
            [DroidStatSubType.Armor] = SkillType.Armor,
            [DroidStatSubType.Vibroblade] = SkillType.Vibroblade,
            [DroidStatSubType.Vibroknife] = SkillType.Vibroknife,
            [DroidStatSubType.HeavyVibroblade] = SkillType.HeavyVibroblade,
            [DroidStatSubType.Spear] = SkillType.Spear,
            [DroidStatSubType.TwinBlade] = SkillType.TwinBlade,
            [DroidStatSubType.Katar] = SkillType.Katar,
            [DroidStatSubType.Staff] = SkillType.Staff,
            [DroidStatSubType.Pistol] = SkillType.Pistol,
            [DroidStatSubType.Rifle] = SkillType.Rifle,
            [DroidStatSubType.Throwing] = SkillType.Throwing,
            [DroidStatSubType.Unarmed] = SkillType.Unarmed,
            [DroidStatSubType.TwinPistol] = SkillType.TwinPistol
        };
        foreach (var subType in resistances.Keys.Concat(skills.Keys))
        {
            _world.AddStat(DroidWorld.Controller, subType, 3);
            _world.AddStat(DroidWorld.Controller, subType, 2);
        }
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.Lightsaber, 9);
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.Saberstaff, 9);

        var details = _service.LoadDroidItemPropertyDetails(DroidWorld.Controller);

        details.Resistances.Should().HaveCount(resistances.Count);
        foreach (var (_, resistance) in resistances)
            details.Resistances[resistance].Should().Be(5);
        details.Skills.Should().HaveCount(skills.Count);
        foreach (var (_, skill) in skills)
            details.Skills[skill].Should().Be(5);
    }

    [Test]
    public void LoadDroidPartItemPropertyDetails_AddsEveryStatOfThePart()
    {
        var part = _world.Create("droid_part");
        foreach (var subType in new[]
                 {
                     DroidStatSubType.AISlots, DroidStatSubType.STM, DroidStatSubType.MGT, DroidStatSubType.PER, DroidStatSubType.VIT,
                     DroidStatSubType.WIL, DroidStatSubType.AGI, DroidStatSubType.SOC, DroidStatSubType.Armor,
                     DroidStatSubType.ResistancePoison, DroidStatSubType.ResistanceElectrical, DroidStatSubType.ResistanceIce,
                     DroidStatSubType.ResistanceMind, DroidStatSubType.ResistanceMobility, DroidStatSubType.ResistanceTrauma,
                     DroidStatSubType.ResistanceDisruption, DroidStatSubType.Vibroblade, DroidStatSubType.Vibroknife,
                     DroidStatSubType.HeavyVibroblade, DroidStatSubType.Spear, DroidStatSubType.TwinBlade, DroidStatSubType.Katar,
                     DroidStatSubType.Staff, DroidStatSubType.Pistol, DroidStatSubType.Rifle, DroidStatSubType.Throwing,
                     DroidStatSubType.Unarmed, DroidStatSubType.TwinPistol
                 })
        {
            _world.AddStat(part, subType, 2);
            _world.AddStat(part, subType, 1);
        }

        var details = _service.LoadDroidPartItemPropertyDetails(part);

        (details.AISlots, details.STM, details.MGT, details.PER, details.VIT, details.WIL, details.AGI, details.SOC, details.Armor)
            .Should().Be((3, 3, 3, 3, 3, 3, 3, 3, 3));
        (details.Vibroblade, details.Vibroknife, details.HeavyVibroblade, details.Spear, details.TwinBlade, details.Katar)
            .Should().Be((3, 3, 3, 3, 3, 3));
        (details.Staff, details.Pistol, details.Rifle, details.Throwing, details.Unarmed, details.TwinPistol)
            .Should().Be((3, 3, 3, 3, 3, 3));
        details.Resistances.Should().HaveCount(7).And.OnlyContain(pair => pair.Value == 3);
    }

    // ---- spawning

    private static readonly string[] DroidScripts =
    {
        ScriptName.OnDroidBlocked, ScriptName.OnDroidRoundEnd, ScriptName.OnDroidConversation, ScriptName.OnDroidDamaged,
        ScriptName.OnDroidDeath, ScriptName.OnDroidDisturbed, ScriptName.OnDroidHeartbeat, ScriptName.OnDroidAttacked,
        ScriptName.OnDroidRest, ScriptName.OnDroidSpawn, ScriptName.OnDroidSpellCast, ScriptName.OnDroidUserDefined
    };

    [Test]
    public void SpawnDroid_BuildsTheDroidFromTheControllerAndLinksItToThePlayer()
    {
        ConfigureController();
        _world.OpenWindows = true;

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);
        var droid = _world.Droid;

        _world.GuiCalls.Calls.Select(call => call.Member).Should().Contain("CloseWindow");
        _world.Calls("CreateObject").Should().ContainSingle().Which.Arguments[1].Should().Be("pc_droid");
        _world.Calls("SetName").Should().ContainSingle().Which.Arguments.Should().Equal(droid, "Beepy");
        _world.Creatures.CallsTo("SetRawAbilityScore").Select(call => ((AbilityType)call.Arguments[1]!, (int)call.Arguments[2]!))
            .Should().Equal(
                (AbilityType.Might, 11), (AbilityType.Perception, 12), (AbilityType.Vitality, 13),
                (AbilityType.Willpower, 14), (AbilityType.Agility, 15), (AbilityType.Social, 16));
        _world.Creatures.CallsTo("SetBaseAC").Single().Arguments.Should().Equal(droid, 10);
        _world.Creatures.CallsTo("SetBaseAttackBonus").Single().Arguments.Should().Equal(droid, 1);
        var hp = _world.StatCalls.Calls.Single(call => call.Member == "SetNPCMaxHitPoints");
        hp.Arguments.Should().Equal(droid, 40 + 2 * 3, true);
        _world.Calls("SetEventScript").Select(call => (string)call.Arguments[2]!).Should().Equal(DroidScripts);
        _world.AiCalls.Calls.Single().Arguments.Should().Equal(droid, AIProfileType.DroidCompanion);
        _world.Calls("AddHenchman").Single().Arguments.Should().Equal(DroidWorld.Player, droid);
        _world.Locals.Object(DroidWorld.Player, "ACTIVE_DROID").Should().Be(droid);
        _world.Locals.Object(DroidWorld.Player, "ACTIVE_DROID_ITEM").Should().Be(DroidWorld.Controller);
        _world.Locals.Object(droid, "ACTIVE_DROID_ITEM").Should().Be(DroidWorld.Controller);
        _world.Locals.Has(droid, "DROID_IS_SPAWNING").Should().BeFalse("the spawning flag is cleared when the restore is done");
        _service.GetDroid(DroidWorld.Player).Should().Be(droid);
        _service.GetControllerItem(droid).Should().Be(DroidWorld.Controller);
        _world.Calls("ExecuteScript").Should().ContainSingle(call => (uint)call.Arguments[1]! == droid);
        _world.GloveCalls.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(droid);
        _world.TwinCalls.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(droid);
        _world.DiagnosticsCalls.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(droid);
    }

    [Test]
    public void SpawnDroid_AddsTheSkinPropertiesForLevelHitPointsStaminaResistancesAndSkills()
    {
        ConfigureController();

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        // Level, hit points and stamina, one resistance, and the two weapon/armor skills.
        _world.Calls("AddItemProperty").Should().HaveCount(6);
        _world.Calls("ItemPropertyCustom").Select(call => ((ItemPropertyType)call.Arguments[0]!, (int)call.Arguments[1]!, (int)call.Arguments[2]!))
            .Should().Equal(
                (ItemPropertyType.NPCLevel, -1, 25),
                (ItemPropertyType.NPCHP, -1, 46),
                (ItemPropertyType.Stamina, -1, 7),
                (ItemPropertyType.Resistance, (int)ResistanceType.Fire, 1005),
                (ItemPropertyType.NPCSkill, (int)SkillType.Armor, 2),
                (ItemPropertyType.NPCSkill, (int)SkillType.Rifle, 4));
        _world.ResistanceCalls.Calls.Single().Arguments.Should().Equal(5);
    }

    [Test]
    public void SpawnDroid_GreetsInThePersonalityAndHealsAfterFourSeconds()
    {
        ConfigureController();

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        _world.Calls("ApplyEffectToObject").Should().BeEmpty("the heal waits for its delay");

        _world.RunDeferred();

        _world.Calls("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(4f);
        _world.Spoken().Should().ContainSingle().Which.Should().NotBeNullOrWhiteSpace();
        _world.Calls("ApplyEffectToObject").Should().ContainSingle().Which.Arguments[2].Should().Be(_world.Droid);
        _world.Calls("EffectHeal").Single().Arguments[0].Should().Be(120);
    }

    [Test]
    public void SpawnDroid_UsesTheSavedAppearancePartsAndTheDefaultsForTheRest()
    {
        ConfigureController();
        var saved = Saved();
        saved.AppearanceParts[CreaturePart.Head] = 55;
        saved.AppearanceParts[CreaturePart.LeftFoot] = 66;
        saved.TintOverrides["TINT_A"] = 9;
        saved.TintOverrides["TINT_B"] = 0;
        _service.SaveConstructedDroid(DroidWorld.Controller, saved);

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        var parts = _world.Calls("SetCreatureBodyPart").ToDictionary(call => (CreaturePart)call.Arguments[0]!, call => (int)call.Arguments[1]!);
        parts.Should().HaveCount(16);
        parts[CreaturePart.Head].Should().Be(55);
        parts[CreaturePart.LeftFoot].Should().Be(66);
        parts[CreaturePart.Neck].Should().Be(2);
        parts[CreaturePart.Torso].Should().Be(3);
        parts[CreaturePart.RightHand].Should().Be(7);
        parts[CreaturePart.LeftShin].Should().Be(15);
        _world.Locals.Int(_world.Droid, "TINT_A").Should().Be(9);
        _world.Locals.Has(_world.Droid, "TINT_B").Should().BeFalse();
    }

    [Test]
    public void SpawnDroid_RecordsThePortraitAndSoundsetTheFirstTimeAndAppliesThemAfterwards()
    {
        ConfigureController();

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        _world.Calls("SetPortraitId").Should().BeEmpty();
        _world.Calls("SetSoundset").Should().BeEmpty();
        Saved().PortraitId.Should().Be(77);
        Saved().SoundSetId.Should().Be(88);

        var saved = Saved();
        saved.PortraitId = 5;
        saved.SoundSetId = 6;
        _service.SaveConstructedDroid(DroidWorld.Controller, saved);
        _world.Locals.ClearCalls();

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        _world.Calls("SetPortraitId").Single().Arguments[1].Should().Be(5);
        _world.Calls("SetSoundset").Single().Arguments[1].Should().Be(6);
    }

    [Test]
    public void SpawnDroid_FallsBackToTheDefaultNameWhenTheControllerHasNone()
    {
        _world.AddStat(DroidWorld.Controller, DroidStatSubType.Tier, 1);

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        _world.Calls("SetName").Single().Arguments[1].Should().Be("Droid");
    }

    private (string Rifle, string Medpac) SaveLoadout(ConstructedDroid saved)
    {
        var rifle = _world.Blob("oph_rifle", BaseItem.Rifle);
        var medpac = _world.Blob("medpac", BaseItem.Potions);
        saved.EquippedItems[InventorySlot.RightHand] = rifle;
        saved.Inventory["medpac-id"] = medpac;
        return (rifle, medpac);
    }

    [Test]
    public void SpawnDroid_EquipsTheSavedGearAndCarriesTheSavedItems()
    {
        ConfigureController();
        var saved = Saved();
        SaveLoadout(saved);
        _service.SaveConstructedDroid(DroidWorld.Controller, saved);

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        var droid = _world.Droid;
        var rifle = _world.InSlot(droid, InventorySlot.RightHand);
        _world.ResRefOf(rifle).Should().Be("oph_rifle");
        _world.PossessorOf(rifle).Should().Be(droid);
        _world.Inventory(droid).Select(_world.ResRefOf).Should().Equal("medpac");
        _world.Calls("SetDroppableFlag").Select(call => (bool)call.Arguments[1]!).Should().AllSatisfy(flag => flag.Should().BeFalse());
        Saved().EquippedItems.Should().ContainKey(InventorySlot.RightHand);
        Saved().Inventory.Should().HaveCount(1);
        _world.Locals.String(rifle, "DROID_ITEM_ID").Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void SpawnDroid_ReturnsStoredContainersAndPendingReturnsToThePlayer()
    {
        ConfigureController();
        var saved = Saved();
        saved.Inventory["bag"] = _world.Blob("bag", BaseItem.MiscSmall, container: true);
        saved.Inventory["DROID_RETURN_ITEM:abc"] = _world.Blob("ration", BaseItem.MiscSmall);
        saved.Inventory["unreadable"] = "not an item";
        _service.SaveConstructedDroid(DroidWorld.Controller, saved);

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        _world.Inventory(DroidWorld.Player).Select(_world.ResRefOf).Should().BeEquivalentTo("bag", "ration");
        _world.Inventory(_world.Droid).Should().BeEmpty();
        Saved().Inventory.Should().BeEmpty();
        _world.Calls("SetDroppableFlag").Select(call => (bool)call.Arguments[1]!).Should().Contain(true);
    }

    [Test]
    public void SpawnDroid_KeepsAnItemItCannotHandBackUnderAReturnKeyAndTellsThePlayer()
    {
        ConfigureController();
        _world.MoveToFails = true;
        var saved = Saved();
        saved.Inventory["bag"] = _world.Blob("bag", BaseItem.MiscSmall, container: true);
        _service.SaveConstructedDroid(DroidWorld.Controller, saved);

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        Saved().Inventory.Keys.Should().ContainSingle().Which.Should().StartWith("DROID_RETURN_ITEM:");
        _world.Messages().Should().Contain("Make room in your inventory and summon your droid again to recover its returned items.");
        _world.Inventory(DroidWorld.Player).Should().BeEmpty();
    }

    [Test]
    public void SpawnDroid_KeepsAnItemTheDroidCannotTakeUnderARetryKeyAndRetriesItNextTime()
    {
        ConfigureController();
        var saved = Saved();
        var (rifle, medpac) = SaveLoadout(saved);
        _service.SaveConstructedDroid(DroidWorld.Controller, saved);
        _world.RefusedBlobs.Add(rifle);
        _world.RefusedBlobs.Add(medpac);

        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        _world.Messages().Should().Contain("Some droid items could not be restored. They remain saved and will be retried on the next summon.");
        _world.InSlot(_world.Droid, InventorySlot.RightHand).Should().Be(Invalid);
        var kept = Saved().Inventory;
        kept.Keys.Should().HaveCount(2).And.OnlyContain(key => key.StartsWith("DROID_RESTORE_ITEM:"));
        kept.Keys.Should().Contain(key => key.StartsWith("DROID_RESTORE_ITEM:" + (int)InventorySlot.RightHand + ":"));
        kept.Keys.Should().Contain(key => key.StartsWith("DROID_RESTORE_ITEM:" + (int)InventorySlot.Invalid + ":"));

        _world.RefusedBlobs.Clear();
        _world.Locals.ClearCalls();
        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);

        _world.ResRefOf(_world.InSlot(_world.Droid, InventorySlot.RightHand)).Should().Be("oph_rifle");
        _world.Inventory(_world.Droid).Select(_world.ResRefOf).Should().Equal("medpac");
        Saved().Inventory.Keys.Should().NotContain(key => key.StartsWith("DROID_RESTORE_ITEM:"));
    }

    // ---- despawning

    private uint SpawnConfigured()
    {
        ConfigureController();
        _service.SpawnDroid(DroidWorld.Player, DroidWorld.Controller);
        _world.Locals.ClearCalls();
        _world.GuiCalls.Calls.ToList();
        return _world.Droid;
    }

    [Test]
    public void OnPlayerExit_DismissesTheDroidSavesItAndStartsTheRecast()
    {
        var droid = SpawnConfigured();
        var bag = _world.GiveItem(droid, "bag", BaseItem.MiscSmall, container: true);
        _world.OpenWindows = true;
        _world.Engine.SetReturn("GetExitingObject", DroidWorld.Player);

        _service.OnPlayerExit();

        _world.CompanionCalls.Calls.Should().ContainSingle(call => call.Member == "Clear").Which.Arguments.Should().Equal(droid);
        _world.PossessorOf(bag).Should().Be(DroidWorld.Player, "a container the droid cannot keep goes back to the player");
        _world.Calls("DestroyObject").Should().Contain(call => (uint)call.Arguments[0]! == droid && (float)call.Arguments[1]! == 0.1f);
        _world.Calls("SetItemCursedFlag").Single().Arguments.Should().Equal(DroidWorld.Controller, false);
        _world.Locals.Has(DroidWorld.Player, "ACTIVE_DROID").Should().BeFalse();
        _world.Locals.Has(DroidWorld.Player, "ACTIVE_DROID_ITEM").Should().BeFalse();
        _world.Locals.Has(droid, "ACTIVE_DROID_ITEM").Should().BeFalse();
        _world.RecastCalls.Calls.Single().Arguments.Should().Equal(DroidWorld.Player, RecastGroup.DroidController, 1800f);
        _world.GuiCalls.Calls.Should().Contain(call => call.Member == "CloseWindow" && (GuiWindowType)call.Arguments[1]! == GuiWindowType.AppearanceEditor);
        _world.RunDeferred();
        _world.Spoken().Should().ContainSingle();
    }

    [Test]
    public void RemoveAssociate_DismissesTheDroidOfThePlayerWhoLeftItsParty()
    {
        var droid = SpawnConfigured();
        Self(DroidWorld.Player);

        _service.RemoveAssociate();

        _world.Calls("DestroyObject").Should().Contain(call => (uint)call.Arguments[0]! == droid);
        _world.RecastCalls.Calls.Should().ContainSingle();
    }

    [Test]
    public void RemoveAssociate_WithoutADroid_DoesNothing()
    {
        Self(DroidWorld.Player);

        _service.RemoveAssociate();

        _world.Calls("DestroyObject").Should().BeEmpty();
        _world.RecastCalls.Calls.Should().BeEmpty();
    }

    [Test]
    public void RemovingTheDroidWhenARejectedItemCannotMove_SavesItUnderAReturnKey()
    {
        var droid = SpawnConfigured();
        _world.GiveItem(droid, "bag", BaseItem.MiscSmall, container: true);
        _world.MoveToFails = true;
        Self(DroidWorld.Player);

        _service.RemoveAssociate();

        Saved().Inventory.Keys.Should().ContainSingle().Which.Should().StartWith("DROID_RETURN_ITEM:");
    }

    // ---- death

    [Test]
    public void DroidOnDeath_ClearsTheDroidSpeaksItsDeathPhraseAndStartsTheRecast()
    {
        var droid = SpawnConfigured();
        Self(droid);

        _service.DroidOnDeath();

        _world.CompanionCalls.Calls.Should().ContainSingle(call => call.Member == "Clear");
        _world.Calls("ExecuteScript").Should().ContainSingle().Which.Arguments.Should().Equal("x2_hen_death", droid);
        _world.Spoken().Should().ContainSingle().Which.Should().NotBeNullOrWhiteSpace();
        _world.RecastCalls.Calls.Single().Arguments.Should().Equal(DroidWorld.Player, RecastGroup.DroidController, 1800f);
        _world.Locals.Has(DroidWorld.Player, "ACTIVE_DROID").Should().BeFalse();
    }

    // ---- items

    private void AcquireBy(uint droid, uint item)
    {
        _world.Engine.SetReturn("GetModuleItemAcquiredBy", droid);
        _world.Engine.SetReturn("GetModuleItemAcquired", item);
        _service.OnAcquireItem();
    }

    [Test]
    public void OnAcquireItem_StoresACarriedItemOnTheController()
    {
        var droid = SpawnConfigured();
        var item = _world.GiveItem(droid, "medpac", BaseItem.Potions);

        AcquireBy(droid, item);

        _world.Calls("SetDroppableFlag").Single().Arguments.Should().Equal(item, false);
        Saved().Inventory.Values.Should().ContainSingle();
    }

    [Test]
    public void OnAcquireItem_HandsBackAContainerAndTellsTheMaster()
    {
        var droid = SpawnConfigured();
        var bag = _world.GiveItem(droid, "bag", BaseItem.MiscSmall, container: true);

        AcquireBy(droid, bag);

        _world.Messages().Should().Equal("Containers cannot be stored.");
        _world.ItemCalls.Calls.Should().Contain(call => call.Member == "ReturnItem" && (uint)call.Arguments[0]! == DroidWorld.Player);
        _world.Calls("ClearAllActions").Should().BeEmpty();
        _world.RunDeferred();
        _world.Calls("ClearAllActions").Should().HaveCount(1);
    }

    [Test]
    public void OnAcquireItem_RefusesCreditsWithAMessage()
    {
        var droid = SpawnConfigured();
        var gold = _world.GiveItem(droid, "nw_it_gold001", BaseItem.Gold);

        AcquireBy(droid, gold);

        _world.Messages().Should().Equal("Credits cannot be placed inside.");
    }

    [Test]
    public void OnAcquireItem_WhenTheDroidIsTooHeavy_SaysSoAndHandsTheItemBack()
    {
        var droid = SpawnConfigured();
        var item = _world.GiveItem(droid, "medpac", BaseItem.Potions);
        _world.Weight = 51;

        AcquireBy(droid, item);
        _world.RunDeferred();

        _world.Spoken().Should().Equal("I'm sorry master. I cannot carry any more items.");
        _world.ItemCalls.Calls.Should().Contain(call => call.Member == "ReturnItem" && (uint)call.Arguments[1]! == item);
        _world.Calls("SetDroppableFlag").Should().BeEmpty();
    }

    [Test]
    public void OnAcquireItem_IgnoresACreatureThatIsNotADroidASpawningDroidAndGeneratedItems()
    {
        var droid = SpawnConfigured();
        var item = _world.GiveItem(droid, "medpac", BaseItem.Potions);
        var generated = _world.GiveItem(droid, "claw", BaseItem.CreatureSlashWeapon);
        var other = _world.Create("goblin");

        AcquireBy(other, item);
        AcquireBy(droid, generated);
        AcquireBy(droid, Invalid);
        _world.Engine.SetLocalBool(droid, "DROID_IS_SPAWNING", true);
        AcquireBy(droid, item);

        _world.Calls("SetDroppableFlag").Should().BeEmpty();
        _world.Messages().Should().BeEmpty();
    }

    [Test]
    public void OnAcquireItem_IgnoresADroidWithoutAController()
    {
        var droid = _world.CreateDroid(linkController: false);
        var item = _world.GiveItem(droid, "medpac", BaseItem.Potions);

        AcquireBy(droid, item);

        _world.Calls("SetDroppableFlag").Should().BeEmpty();
    }

    [Test]
    public void OnLostItem_MakesAStoredItemDroppableAgainAndSavesTheDroid()
    {
        var droid = SpawnConfigured();
        var kept = _world.GiveItem(droid, "medpac", BaseItem.Potions);
        var lost = _world.Create("ration", BaseItem.MiscSmall);
        _service.SaveItemSnapshot(droid);
        _world.Engine.SetReturn("GetModuleItemLostBy", droid);
        _world.Engine.SetReturn("GetModuleItemLost", lost);

        _service.OnLostItem();

        _world.Calls("SetDroppableFlag").Single().Arguments.Should().Equal(lost, true);
        Saved().Inventory.Should().HaveCount(1);
        kept.Should().NotBe(Invalid);
    }

    [Test]
    public void OnLostItem_IgnoresACreatureThatIsNotADroidAndAGeneratedItem()
    {
        var droid = SpawnConfigured();
        var generated = _world.Create("claw", BaseItem.CreatureItem);
        _world.Engine.SetReturn("GetModuleItemLostBy", _world.Create("goblin"));
        _service.OnLostItem();
        _world.Calls("SetDroppableFlag").Should().BeEmpty();

        _world.Engine.SetReturn("GetModuleItemLostBy", droid);
        _world.Engine.SetReturn("GetModuleItemLost", generated);
        _service.OnLostItem();
        _world.Calls("SetDroppableFlag").Should().BeEmpty();
    }

    [Test]
    public void OnEquipItemAndOnUnequipItem_HideAndShowTheNativeHelmetOfADroid()
    {
        var droid = SpawnConfigured();
        var helmet = _world.GiveItem(droid, "helmet", BaseItem.Helmet);
        var rifle = _world.GiveItem(droid, "rifle", BaseItem.Rifle);
        Self(droid);

        _world.EventItem = helmet;
        _service.OnEquipItem();
        _world.EventItem = rifle;
        _service.OnEquipItem();
        _world.EventItem = helmet;
        _service.OnUnequipItem();
        _world.EventItem = rifle;
        _service.OnUnequipItem();

        _world.Calls("SetHiddenWhenEquipped").Select(call => (call.Arguments[0], call.Arguments[1]))
            .Should().Equal((helmet, true), (helmet, false));
    }

    [Test]
    public void OnEquipItemAndOnUnequipItem_IgnoreACreatureThatIsNotADroid()
    {
        var other = _world.Create("goblin");
        var helmet = _world.GiveItem(other, "helmet", BaseItem.Helmet);
        Self(other);
        _world.EventItem = helmet;

        _service.OnEquipItem();
        _service.OnUnequipItem();

        _world.Calls("SetHiddenWhenEquipped").Should().BeEmpty();
    }

    [Test]
    public void OnEquipmentChanged_SavesTheEquippedGear()
    {
        var droid = SpawnConfigured();
        _world.GiveItem(droid, "oph_rifle", BaseItem.Rifle, InventorySlot.RightHand);
        Self(droid);

        _service.OnEquipmentChanged();

        Saved().EquippedItems.Keys.Should().Equal(InventorySlot.RightHand);
    }

    [Test]
    public void UpdateEquippedItemSnapshot_ReserializesAnItemTheSnapshotAlreadyHolds()
    {
        var droid = SpawnConfigured();
        var rifle = _world.GiveItem(droid, "oph_rifle", BaseItem.Rifle, InventorySlot.RightHand);
        _service.SaveItemSnapshot(droid);
        var before = Saved().EquippedItems[InventorySlot.RightHand];

        _service.UpdateEquippedItemSnapshot(droid, rifle);

        Saved().EquippedItems[InventorySlot.RightHand].Should().NotBe(before, "the item was serialized again");
        Saved().EquippedItems[InventorySlot.RightHand].Should().StartWith("blob:oph_rifle:");
    }

    [Test]
    public void UpdateEquippedItemSnapshot_IgnoresWhatTheSnapshotDoesNotHold()
    {
        var droid = SpawnConfigured();
        var rifle = _world.GiveItem(droid, "oph_rifle", BaseItem.Rifle, InventorySlot.RightHand);
        var carried = _world.GiveItem(droid, "medpac", BaseItem.Potions);
        var noController = _world.CreateDroid(linkController: false);
        var stray = _world.GiveItem(noController, "x", BaseItem.Rifle, InventorySlot.RightHand);

        _service.UpdateEquippedItemSnapshot(droid, rifle);
        _service.UpdateEquippedItemSnapshot(droid, carried);
        _service.UpdateEquippedItemSnapshot(droid, Invalid);
        _service.UpdateEquippedItemSnapshot(noController, stray);
        _service.UpdateEquippedItemSnapshot(_world.Create("goblin"), rifle);

        Saved().EquippedItems.Should().BeEmpty();
    }

    // ---- the snapshot

    [Test]
    public void SaveItemSnapshot_SavesGearAndCarriedItemsAndKeepsPendingEntries()
    {
        var droid = SpawnConfigured();
        var saved = Saved();
        saved.Inventory["DROID_RETURN_ITEM:r"] = "returned";
        saved.Inventory["DROID_RESTORE_ITEM:2:x"] = "restore";
        saved.Inventory["stale"] = "gone";
        _service.SaveConstructedDroid(DroidWorld.Controller, saved);
        var rifle = _world.GiveItem(droid, "oph_rifle", BaseItem.Rifle, InventorySlot.RightHand);
        var medpac = _world.GiveItem(droid, "medpac", BaseItem.Potions);
        _world.GiveItem(droid, "claw", BaseItem.CreatureSlashWeapon);
        _world.GiveItem(droid, "bag", BaseItem.MiscSmall, container: true);
        _world.GiveItem(droid, "glove", BaseItem.CompanionPowerGlove, InventorySlot.LeftHand);
        _world.Engine.SetLocalString(medpac, "DROID_ITEM_ID", "keep-me");

        _service.SaveItemSnapshot(droid);

        var result = Saved();
        result.EquippedItems.Keys.Should().Equal(InventorySlot.RightHand);
        result.Inventory.Keys.Should().BeEquivalentTo("DROID_RETURN_ITEM:r", "DROID_RESTORE_ITEM:2:x", "keep-me");
        result.Inventory["keep-me"].Should().StartWith("blob:medpac:");
        rifle.Should().NotBe(Invalid);
    }

    [Test]
    public void SaveItemSnapshot_GivesEachItemItsOwnIdWhenTwoShareOne()
    {
        var droid = SpawnConfigured();
        var first = _world.GiveItem(droid, "medpac", BaseItem.Potions);
        var second = _world.GiveItem(droid, "medpac", BaseItem.Potions);
        _world.Engine.SetLocalString(first, "DROID_ITEM_ID", "same");
        _world.Engine.SetLocalString(second, "DROID_ITEM_ID", "same");

        _service.SaveItemSnapshot(droid);

        Saved().Inventory.Keys.Should().HaveCount(2).And.Contain("same");
        _world.Locals.String(second, "DROID_ITEM_ID").Should().NotBe("same");
    }

    [Test]
    public void SaveItemSnapshot_LeavesOutGeneratedAmmunition()
    {
        var droid = SpawnConfigured();
        var arrows = _world.GiveItem(droid, "arrows", BaseItem.MiscSmall, InventorySlot.Arrows);
        _world.GiveItem(droid, "bolts", BaseItem.MiscSmall, InventorySlot.Bolts);
        _world.GiveItem(droid, "bullets", BaseItem.MiscSmall, InventorySlot.Bullets);
        _service.Flags = (true, false, true);

        _service.SaveItemSnapshot(droid);

        Saved().EquippedItems.Keys.Should().Equal(InventorySlot.Bullets);
        arrows.Should().NotBe(Invalid);
    }

    [Test]
    public void SaveItemSnapshot_SavesNothingForANonDroidASpawningDroidOrOneWithoutAController()
    {
        var droid = SpawnConfigured();
        _world.GiveItem(droid, "medpac", BaseItem.Potions);
        var noController = _world.CreateDroid(linkController: false);

        _service.SaveItemSnapshot(_world.Create("goblin"));
        _service.SaveItemSnapshot(noController);
        _world.Engine.SetLocalBool(droid, "DROID_IS_SPAWNING", true);
        _service.SaveItemSnapshot(droid);

        Saved().Inventory.Should().BeEmpty();
    }

    // ---- appearance and instructions

    [Test]
    public void EditDroidAppearance_SavesTheBodyPartsOfTheDroid()
    {
        var droid = SpawnConfigured();
        Self(droid);

        _service.EditDroidAppearance();

        var parts = Saved().AppearanceParts;
        parts.Should().HaveCount(15).And.NotContainKey(CreaturePart.Neck);
        parts[CreaturePart.Head].Should().Be((int)CreaturePart.Head * 10 + 3);
        parts[CreaturePart.LeftFoot].Should().Be((int)CreaturePart.LeftFoot * 10 + 3);
    }

    [Test]
    public void EditDroidAppearance_IgnoresACreatureThatIsNotADroid()
    {
        SpawnConfigured();
        Self(_world.Create("goblin"));

        _service.EditDroidAppearance();

        Saved().AppearanceParts.Should().BeEmpty();
    }

    [Test]
    public void SaveInstructions_ReplacesTheInstructionPropertiesAndSavesTheLoadout()
    {
        _world.AddProperty(DroidWorld.Controller, ItemPropertyType.DroidInstruction, 1, 1);
        _world.AddProperty(DroidWorld.Controller, ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, 2);
        var droid = new ConstructedDroid { Name = "Beepy" };
        droid.ActivePerks.Add(new DroidPerk(SWLOR.Game.Server.Domain.Character.PerkType.Invalid, 2));
        droid.ActivePerks.Add(new DroidPerk(SWLOR.Game.Server.Domain.Character.PerkType.Invalid, 3));

        _service.SaveInstructions(DroidWorld.Controller, droid);

        _world.Calls("RemoveItemProperty").Should().ContainSingle();
        _world.Calls("AddItemProperty").Should().HaveCount(2);
        Saved().ActivePerks.Should().HaveCount(2);
        Saved().Name.Should().Be("Beepy");
    }

    // ---- the event handlers

    [Test]
    public void DroidOnBlocked_RunsTheStockHenchmanScript()
    {
        Self(_world.Create("pc_droid"));

        _service.DroidOnBlocked();

        _world.Calls("ExecuteScript").Single().Arguments[0].Should().Be("x0_ch_hen_block");
    }

    [Test]
    public void DroidOnConversation_FallsBackToTheStockScriptWhenTheCompanionControlDoesNotHandleIt()
    {
        var droid = _world.CreateDroid();
        Self(droid);

        _world.HandlesConversation = true;
        _service.DroidOnConversation();
        _world.Calls("ExecuteScript").Should().BeEmpty();

        _world.HandlesConversation = false;
        _service.DroidOnConversation();
        _world.Calls("ExecuteScript").Single().Arguments[0].Should().Be("x0_ch_hen_conv");
    }

    [Test]
    public void TheCombatHandlers_PassTheThreatToTheCompanionControl()
    {
        var droid = _world.CreateDroid();
        var attacker = _world.Create("goblin");
        Self(droid);
        _world.Engine.SetReturn("GetLastDamager", attacker);
        _world.Engine.SetReturn("GetLastAttacker", attacker);
        _world.Engine.SetReturn("GetLastSpellCaster", attacker);

        _service.DroidOnDamaged();
        _service.DroidOnPhysicalAttacked();
        _world.Engine.SetReturn("GetLastSpellHarmful", false);
        _service.DroidOnSpellCastAt();
        _world.Engine.SetReturn("GetLastSpellHarmful", true);
        _service.DroidOnSpellCastAt();
        _service.DroidOnEndCombatRound();
        _service.DroidOnHeartbeat();

        var calls = _world.CompanionCalls.Calls;
        calls.Where(call => call.Member == "RegisterDefensiveThreat").Select(call => call.Arguments[1])
            .Should().Equal(attacker, attacker, attacker);
        calls.Should().Contain(call => call.Member == "ProcessCombatRound" && (uint)call.Arguments[0]! == droid);
        calls.Should().Contain(call => call.Member == "ProcessHeartbeat" && (uint)call.Arguments[0]! == droid);
        _world.StatCalls.Calls.Should().Contain(call => call.Member == "RestoreNPCStats" && (bool)call.Arguments[0]! == false);
    }

    [Test]
    public void DroidOnDisturbed_StoresAnAddedItemSavesARemovedOneAndRetaliatesForATheft()
    {
        var droid = SpawnConfigured();
        var thief = _world.Create("goblin");
        var item = _world.GiveItem(droid, "medpac", BaseItem.Potions);
        Self(droid);
        _world.Engine.SetReturn("GetInventoryDisturbItem", item);

        _world.Engine.SetReturn("GetInventoryDisturbType", DisturbType.Added);
        _service.DroidOnDisturbed();
        Saved().Inventory.Should().HaveCount(1);

        _world.Locals.ClearCalls();
        _world.Engine.SetReturn("GetInventoryDisturbType", DisturbType.Removed);
        _service.DroidOnDisturbed();
        _world.Calls("SetDroppableFlag").Single().Arguments.Should().Equal(item, true);
        _world.CompanionCalls.Calls.Should().NotContain(call => call.Member == "RegisterDefensiveThreat");

        _world.Engine.SetReturn("GetLastDisturbed", thief);
        _world.Engine.SetReturn("GetInventoryDisturbType", DisturbType.Stolen);
        _service.DroidOnDisturbed();
        _world.CompanionCalls.Calls.Should().ContainSingle(call => call.Member == "RegisterDefensiveThreat")
            .Which.Arguments.Should().Equal(droid, thief);
    }

    [Test]
    public void DroidOnRested_RunsTheStockScriptStopsTheDroidAndAppliesTheRestEffect()
    {
        var droid = _world.CreateDroid();
        Self(droid);

        _service.DroidOnRested();
        _world.RunDeferred();

        _world.Calls("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_rest", droid);
        _world.Calls("ClearAllActions").Should().HaveCount(1);
        var call = _world.StatusEffectCalls.Calls.Single();
        call.Arguments[0].Should().Be(droid);
        call.Arguments[1].Should().Be(droid);
    }

    [Test]
    public void DroidOnSpawn_MakesTheDroidDestroyableLoadsItsStatsAndInitializesTheCompanion()
    {
        var droid = _world.CreateDroid();
        Self(droid);

        _service.DroidOnSpawn();
        _world.RunDeferred();

        _world.Calls("SetIsDestroyable").Single().Arguments.Take(3).Should().Equal(true, false, false);
        _world.StatCalls.Calls.Select(call => call.Member).Should().Equal("LoadNPCStats", "ApplyCreatureMovementRate");
        _world.CompanionCalls.Calls.Single().Arguments.Should().Equal(droid, true);
    }

    [Test]
    public void DroidOnUserDefined_RunsTheStockScript()
    {
        var droid = _world.CreateDroid();
        Self(droid);

        _service.DroidOnUserDefined();

        _world.Calls("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_usrdef", droid);
    }
}
