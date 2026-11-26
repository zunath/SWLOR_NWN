using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Perks;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The Mimicry loadout part: the slot budget, equipping and unequipping, the saved builds, the login restore, the skill decay handler and the
/// learned and equipped reads, over the shared Mimicry world.
/// </summary>
[TestFixture]
public class MimicryLoadoutServiceTests : MimicryTestBase
{
    [TestCase(0, 0, false, 0)]
    [TestCase(1, 0, false, 2)]
    [TestCase(1, 3, false, 8)]
    [TestCase(1, 3, true, 10)]
    public void GetMaxSlots_GrowsWithTheAnalyzerMemoryAndTheCapstone(int analyzer, int memory, bool overclocked, int expected)
    {
        var player = NewPlayer(rank: 10, analyzer, memory, overclocked);

        _mimicry.Loadout.GetMaxSlots(player).Should().Be(expected);
    }

    [Test]
    public void GetMaxSlots_IsZeroWithoutARecordOrForAnObjectThatIsNotAPlayer()
    {
        _mimicry.Loadout.GetMaxSlots((Player)null!).Should().Be(0);
        _mimicry.Loadout.GetMaxSlots(Npc).Should().Be(0);
        _mimicry.Loadout.GetMaxSlots(Player).Should().Be(2);
    }


    [Test]
    public void GetUsedSlots_AddsTheCostOfEachKnownEquippedTechnique()
    {
        Technique(Fire1, slots: 2);
        Technique(Ice1, slots: 3);
        _mimicry.Techniques.CacheData();
        Record.EquippedTechniques = new List<FeatType> { Fire1, Ice1, Plain };

        _mimicry.Loadout.GetUsedSlots(Player).Should().Be(5);
        _mimicry.Loadout.GetUsedSlots(Record).Should().Be(5);
        _mimicry.Loadout.GetUsedSlots((Player)null!).Should().Be(0);
    }

    private void EquipSetup()
    {
        Technique(Fire1, rank: 5, slots: 1);
        Technique(Ice1, rank: 5, slots: 2);
        Trait(Trait1);
        _mimicry.Techniques.CacheData();
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;
        Record.LearnedTechniques[Ice1] = DateTime.UtcNow;
        Record.LearnedTechniques[Trait1] = DateTime.UtcNow;
    }

    [Test]
    public void CanEquip_AcceptsALearnedTechniqueThatFitsTheBudget()
    {
        EquipSetup();

        _mimicry.Loadout.CanEquip(Player, Fire1, out var error).Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Test]
    public void CanEquip_ExplainsEachReasonForRefusing()
    {
        EquipSetup();

        _mimicry.Loadout.CanEquip(Npc, Record, Fire1, out var error).Should().BeFalse();
        error.Should().Be("Only players may equip techniques.");
        _mimicry.Loadout.CanEquip(Player, null!, Fire1, out error).Should().BeFalse();
        error.Should().Be("Only players may equip techniques.");
        _isDm = true;
        _mimicry.Loadout.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("Only players may equip techniques.");
        _isDm = false;

        _mimicry.Loadout.CanEquip(Player, Record, Plain, out error).Should().BeFalse();
        error.Should().Be("That is not a valid technique.");

        Record.LearnedTechniques.Remove(Fire1);
        _mimicry.Loadout.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("You have not learned that technique.");
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;

        Record.Skills.Remove(SkillType.Mimicry);
        _mimicry.Loadout.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("You need Mimicry skill rank 5 to equip that technique.");
        Record.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = 20 };

        Record.EquippedTechniques.Add(Fire1);
        _mimicry.Loadout.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("That technique is already equipped.");
        Record.EquippedTechniques.Clear();

        _inCombat = true;
        _mimicry.Loadout.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("You cannot manage techniques while in combat.");
        _inCombat = false;

        Record.EquippedTechniques.Add(Ice1);
        _mimicry.Loadout.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("You do not have enough technique slots available.");
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        _mimicry.Loadout.CanEquip(Player, Record, Fire1, out error).Should().BeTrue("the analyzer memory perk adds slots");
    }

    [Test]
    public void EquipTechnique_SavesTheLoadoutGrantsTheFeatAndPlacesItOnTheFirstEmptyHotbarSlot()
    {
        EquipSetup();
        _bar[0] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item };

        _mimicry.Loadout.EquipTechnique(Player, Fire1).Should().BeTrue();

        Record.EquippedTechniques.Should().Equal(Fire1);
        _db.Saved.Should().Contain(Record);
        _creature.CallsTo("AddFeat").Should().ContainSingle().Which.Arguments[1].Should().Be(Fire1);
        var placed = _playerPlugin.CallsTo("SetQuickBarSlot");
        placed.Should().ContainSingle().Which.Arguments[1].Should().Be(1);
        _quickCastCalls.Should().Contain("ApplyPreferences");
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void EquipTechnique_DoesNotGrantAFeatThePlayerAlreadyKnowsOrPlaceOneAlreadyOnTheBar()
    {
        EquipSetup();
        _knownFeats.Add(Fire1);
        _bar[3] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };

        _mimicry.Loadout.EquipTechnique(Player, Fire1).Should().BeTrue();

        _creature.CountOf("AddFeat").Should().Be(0);
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);
    }

    [Test]
    public void EquipTechnique_LeavesTheBarAloneWhenTheFirstRowIsFullOrTheAbilityHasNoAction()
    {
        EquipSetup();
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        for (var slot = 0; slot < 12; slot++)
            _bar[slot] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item };
        _mimicry.Loadout.EquipTechnique(Player, Fire1).Should().BeTrue();
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);

        _bar.Clear();
        _mimicry.Abilities[Ice1].ImpactAction = null;
        _mimicry.Loadout.EquipTechnique(Player, Ice1).Should().BeTrue();
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);

        _mimicry.Loadout.Registered.Remove(Ice1);
        Record.EquippedTechniques.Remove(Ice1);
        _mimicry.Loadout.EquipTechnique(Player, Ice1).Should().BeTrue();
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);
    }

    [Test]
    public void EquipTechnique_TellsThePlayerWhyItFailed()
    {
        EquipSetup();
        _inCombat = true;

        _mimicry.Loadout.EquipTechnique(Player, Fire1).Should().BeFalse();

        Messages().Should().ContainSingle().Which.Should().Contain("You cannot manage techniques while in combat.");
        Record.EquippedTechniques.Should().BeEmpty();
    }

    [Test]
    public void EquipTechnique_NeverGrantsOrPlacesAPassiveTrait()
    {
        EquipSetup();
        _knownFeats.Add(Trait1);
        _bar[2] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Trait1 };

        _mimicry.Loadout.EquipTechnique(Player, Trait1).Should().BeTrue();

        _creature.CountOf("AddFeat").Should().Be(0);
        _quickCastCalls.Should().Contain($"RemoveFeat:{Trait1}", "a stale feat is stripped");
        var cleared = _playerPlugin.CallsTo("SetQuickBarSlot").Should().ContainSingle().Subject;
        cleared.Arguments[1].Should().Be(2);
    }

    [Test]
    public void UnequipTechnique_RemovesTheFeatItsHotbarSlotAndItsStatusEffects()
    {
        EquipSetup();
        var detail = _mimicry.Abilities[Fire1];
        detail.StatusEffectTypesRemovedOnPerkRefund.Add(typeof(string));
        detail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Add(typeof(int));
        Record.EquippedTechniques.Add(Fire1);
        _bar[4] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };

        _mimicry.Loadout.UnequipTechnique(Player, Fire1).Should().BeTrue();

        Record.EquippedTechniques.Should().BeEmpty();
        _mimicry.Loadout.RemovedEffects.Should().Equal((Player, typeof(string), false));
        _mimicry.Loadout.RemovedSourceEffects.Should().Equal((Player, typeof(int), false, detail));
        _quickCastCalls.Should().Contain($"RemoveFeat:{Fire1}").And.Contain("ApplyPreferences");
        _playerPlugin.CallsTo("SetQuickBarSlot").Should().ContainSingle().Which.Arguments[1].Should().Be(4);
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void UnequipTechnique_RefusesInCombatForCreaturesAndForTechniquesNotEquipped()
    {
        EquipSetup();

        _mimicry.Loadout.UnequipTechnique(Npc, Fire1).Should().BeFalse();
        _mimicry.Loadout.UnequipTechnique(Player, Fire1).Should().BeFalse("it is not equipped");
        Record.EquippedTechniques.Add(Fire1);
        _inCombat = true;
        _mimicry.Loadout.UnequipTechnique(Player, Fire1).Should().BeFalse();

        Messages().Should().ContainSingle().Which.Should().Contain("cannot manage techniques while in combat");
        Record.EquippedTechniques.Should().Equal(Fire1);
    }

    [Test]
    public void UnequipAllTechniques_ClearsTheLoadoutAndLogs()
    {
        EquipSetup();
        Record.EquippedTechniques.AddRange(new[] { Fire1, Ice1 });

        _mimicry.Loadout.UnequipAllTechniques(Player);

        Record.EquippedTechniques.Should().BeEmpty();
        _quickCastCalls.Where(c => c.StartsWith("RemoveFeat")).Should().HaveCount(2);
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void UnequipAllTechniques_DoesNothingForCreaturesOrAnEmptyLoadout()
    {
        EquipSetup();

        _mimicry.Loadout.UnequipAllTechniques(Npc);
        _mimicry.Loadout.UnequipAllTechniques(Player);

        _db.Saved.Should().BeEmpty();
        _techniqueChanges.Should().Be(0);
    }

    // ---- The slot budget and login --------------------------------------------------------------------------------

    [Test]
    public void EnforceSlotBudget_UnequipsTechniquesAboveTheRankThenTheNewestUntilTheBudgetFits()
    {
        Technique(Fire1, rank: 0, slots: 1);
        Technique(Fire2, rank: 30, slots: 1);
        Technique(Ice1, rank: 0, slots: 2);
        Technique((FeatType)3010, rank: 0, slots: 2);
        _mimicry.Techniques.CacheData();
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        Record.EquippedTechniques = new List<FeatType> { Fire1, Ice1, Fire2, (FeatType)3010 };

        _mimicry.Loadout.EnforceSlotBudget(Player);

        Record.EquippedTechniques.Should().Equal(Fire1, Ice1).And.HaveCount(2, "Fire2 is above the rank and the newest is over budget");
        _db.Saved.Should().ContainSingle();
        _quickCastCalls.Should().Contain("ApplyPreferences");
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void EnforceSlotBudget_ChangesNothingWhenTheLoadoutIsValid()
    {
        Technique(Fire1, slots: 1);
        _mimicry.Techniques.CacheData();
        Record.EquippedTechniques = new List<FeatType> { Fire1 };

        _mimicry.Loadout.EnforceSlotBudget(Player);

        _db.Saved.Should().BeEmpty();
        _techniqueChanges.Should().Be(0);
    }

    [Test]
    public void EnforceSlotBudget_IgnoresCreaturesAndMissingRecords()
    {
        _mimicry.Loadout.EnforceSlotBudget(Npc);
        _mimicry.Loadout.EnforceSlotBudget(Player, null!);
        _mimicry.Loadout.EnforceSlotBudget(Npc, Record);

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void EnforceSlotBudget_TreatsAMissingSkillAsRankZero()
    {
        Technique(Fire1, rank: 3);
        _mimicry.Techniques.CacheData();
        Record.Skills.Remove(SkillType.Mimicry);
        Record.EquippedTechniques = new List<FeatType> { Fire1 };

        _mimicry.Loadout.EnforceSlotBudget(Player, Record);

        Record.EquippedTechniques.Should().BeEmpty();
    }

    [Test]
    public void OnMimicrySkillDecay_EnforcesTheBudgetOnlyForTheMimicrySkill()
    {
        Technique(Fire1, rank: 30);
        _mimicry.Techniques.CacheData();
        Record.EquippedTechniques = new List<FeatType> { Fire1 };
        _events.SetReturnHandler("GetEventData", _ => ((int)SkillType.Armor).ToString());
        _mimicry.Loadout.OnMimicrySkillDecay();
        Record.EquippedTechniques.Should().Equal(Fire1);

        _events.SetReturnHandler("GetEventData", _ => ((int)SkillType.Mimicry).ToString());
        _mimicry.Loadout.OnMimicrySkillDecay();
        Record.EquippedTechniques.Should().BeEmpty();
    }

    [Test]
    public void OnPlayerLogin_RestoresTheEquippedTechniquesAfterTheModuleEnterHandlers()
    {
        Technique(Fire1);
        Trait(Trait1);
        _mimicry.Techniques.CacheData();
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 1;
        Record.EquippedTechniques = new List<FeatType> { Fire1, Trait1 };

        _mimicry.Loadout.OnPlayerLogin();
        _creature.CountOf("AddFeat").Should().Be(0, "restoration waits for the delayed command");
        ((Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!)();

        _creature.CallsTo("AddFeat").Should().ContainSingle().Which.Arguments[1].Should().Be(Fire1);
        _quickCastCalls.Should().Contain("ApplyPreferences");
    }

    [Test]
    public void OnPlayerLogin_IgnoresDmsAndCreatures()
    {
        _entering = Npc;
        _mimicry.Loadout.OnPlayerLogin();
        _entering = Player;
        _isDm = true;
        _mimicry.Loadout.OnPlayerLogin();

        _engine.CountOf("DelayCommand").Should().Be(0);
    }

    [TestCase("invalid")]
    [TestCase("norecord")]
    [TestCase("noanalyzer")]
    public void TheDelayedRestore_DoesNothingWhen(string reason)
    {
        Technique(Fire1);
        _mimicry.Techniques.CacheData();
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = reason == "noanalyzer" ? 0 : 1;
        Record.EquippedTechniques = new List<FeatType> { Fire1 };
        _mimicry.Loadout.OnPlayerLogin();
        if (reason == "invalid")
            _invalid.Add(Player);
        if (reason == "norecord")
            _engine.SetReturnHandler("GetObjectUUID", _ => "nobody");

        ((Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!)();

        _creature.CountOf("AddFeat").Should().Be(0);
    }

    // ---- Saved builds ---------------------------------------------------------------------------------------------

    [Test]
    public void ValidateLoadout_ExplainsEachProblemWithADraft()
    {
        Technique(Fire1, rank: 5, slots: 1);
        Technique(Ice1, rank: 40, slots: 1);
        Technique(Fire2, rank: 0, slots: 3);
        _mimicry.Techniques.CacheData();
        foreach (var feat in new[] { Fire1, Ice1, Fire2 })
            Record.LearnedTechniques[feat] = DateTime.UtcNow;

        _mimicry.Loadout.ValidateLoadout(Record, new[] { Fire1 }, out var error).Should().BeTrue();
        error.Should().BeEmpty();

        _mimicry.Loadout.ValidateLoadout(Record, new[] { Fire1, Fire1 }, out error).Should().BeFalse();
        error.Should().Contain("duplicate");
        _mimicry.Loadout.ValidateLoadout(Record, new[] { Plain }, out error).Should().BeFalse();
        error.Should().Contain("unavailable technique");

        Record.LearnedTechniques.Remove(Fire1);
        _mimicry.Loadout.ValidateLoadout(Record, new[] { Fire1 }, out error).Should().BeFalse();
        error.Should().Be("You have not learned Technique 3001.");
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;

        _mimicry.Loadout.ValidateLoadout(Record, new[] { Ice1 }, out error).Should().BeFalse();
        error.Should().Be("Technique 3003 requires Mimicry rank 40.");

        _mimicry.Loadout.ValidateLoadout(Record, new[] { Fire1, Fire2 }, out error).Should().BeFalse();
        error.Should().Be("This build needs 4 technique slots; you have 2.");

        Record.Skills.Remove(SkillType.Mimicry);
        _mimicry.Loadout.ValidateLoadout(Record, new[] { Fire1 }, out error).Should().BeFalse("a missing skill is rank zero");
    }

    [Test]
    public void CaptureLoadoutHotbar_RecordsTheSlotsHoldingTheBuildsTechniques()
    {
        var loadout = new MimicryLoadout { Techniques = new List<FeatType> { Fire1, Ice1 } };
        loadout.HotbarSlots[30] = Fire1;
        _bar[2] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };
        _bar[7] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Ice1 };
        _bar[8] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Plain };
        _bar[9] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item, INTParam1 = (int)Fire1 };

        _mimicry.Loadout.CaptureLoadoutHotbar(Player, loadout);

        loadout.HotbarSlots.Should().BeEquivalentTo(new Dictionary<int, FeatType> { [2] = Fire1, [7] = Ice1 });
    }

    private MimicryLoadout SetUpBuilds(out MimicryLoadout incoming)
    {
        Technique(Fire1, slots: 1);
        Technique(Ice1, slots: 1);
        Trait(Trait1);
        _mimicry.Techniques.CacheData();
        foreach (var feat in new[] { Fire1, Ice1, Trait1 })
            Record.LearnedTechniques[feat] = DateTime.UtcNow;
        var outgoing = new MimicryLoadout { Name = "Old", Techniques = new List<FeatType> { Fire1 } };
        incoming = new MimicryLoadout { Name = "New", Techniques = new List<FeatType> { Ice1, Trait1 } };
        Record.MimicryLoadouts = new List<MimicryLoadout> { outgoing, incoming };
        Record.ActiveMimicryLoadoutId = outgoing.Id;
        Record.EquippedTechniques = new List<FeatType> { Fire1 };
        return outgoing;
    }

    [Test]
    public void ApplyLoadout_CommitsTheBuildRevokesTheOutgoingFeatsAndGrantsTheIncomingOnes()
    {
        var outgoing = SetUpBuilds(out var incoming);
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        _bar[0] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };

        _mimicry.Loadout.ApplyLoadout(Player, incoming.Id, incoming.Techniques, out var error).Should().BeTrue();

        error.Should().BeEmpty();
        Record.EquippedTechniques.Should().Equal(Ice1, Trait1);
        Record.ActiveMimicryLoadoutId.Should().Be(incoming.Id);
        outgoing.HotbarSlots.Should().ContainKey(0, "the outgoing build's hotbar layout was saved");
        _quickCastCalls.Should().Contain($"RemoveFeat:{Fire1}");
        _creature.CallsTo("AddFeat").Select(c => c.Arguments[1]).Should().Equal(Ice1);
        _db.Saved.Should().Contain(Record);
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void ApplyLoadout_RestoresSavedHotbarSlotsOnlyOverEmptyOrMimicryOwnedCells()
    {
        SetUpBuilds(out var incoming);
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        incoming.HotbarSlots[5] = Ice1;
        incoming.HotbarSlots[6] = Ice1;
        incoming.HotbarSlots[7] = Trait1;
        incoming.HotbarSlots[99] = Ice1;
        _bar[6] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item };
        _bar[7] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };

        _mimicry.Loadout.ApplyLoadout(Player, incoming.Id, incoming.Techniques, out _).Should().BeTrue();

        var written = _playerPlugin.CallsTo("SetQuickBarSlot").Select(c => (int)c.Arguments[1]!).ToList();
        written.Should().Contain(5, "an empty cell takes the saved technique");
        written.Should().NotContain(6, "another shortcut takes precedence");
        written.Should().NotContain(99, "a slot beyond the bar is ignored");
        written.Count(slot => slot == 7).Should().Be(1, "the old technique's cell is cleared, and a trait is never placed");
    }

    [Test]
    public void ApplyLoadout_RefusesInvalidRequestsWithoutChangingAnything()
    {
        SetUpBuilds(out var incoming);

        _mimicry.Loadout.ApplyLoadout(Npc, incoming.Id, incoming.Techniques, out var error).Should().BeFalse();
        error.Should().Be("Only players may apply technique builds.");
        _isDm = true;
        _mimicry.Loadout.ApplyLoadout(Player, incoming.Id, incoming.Techniques, out error).Should().BeFalse();
        _isDm = false;
        _inCombat = true;
        _mimicry.Loadout.ApplyLoadout(Player, incoming.Id, incoming.Techniques, out error).Should().BeFalse();
        error.Should().Be("You cannot apply a technique build while in combat.");
        _inCombat = false;
        _mimicry.Loadout.ApplyLoadout(Player, "missing", incoming.Techniques, out error).Should().BeFalse();
        error.Should().Be("That loadout no longer exists.");
        _mimicry.Loadout.ApplyLoadout(Player, incoming.Id, new[] { Plain }, out error).Should().BeFalse();
        error.Should().Contain("unavailable technique");

        Record.EquippedTechniques.Should().Equal(Fire1);
        _db.Saved.Should().BeEmpty();
    }

    // ---- Lists and checks -----------------------------------------------------------------------------------------

    [Test]
    public void TheTechniqueLists_PairEachFeatWithItsDetailAndSkipUnknownOnes()
    {
        Technique(Fire1);
        Technique(Ice1);
        _mimicry.Techniques.CacheData();
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;
        Record.LearnedTechniques[Plain] = DateTime.UtcNow;
        Record.EquippedTechniques = new List<FeatType> { Ice1, Plain };

        _mimicry.Loadout.GetLearnedTechniques(Player).Should().ContainSingle().Which.Feat.Should().Be(Fire1);
        _mimicry.Loadout.GetLearnedTechniques(Record).Should().ContainSingle().Which.Detail.Should().BeSameAs(_mimicry.Abilities[Fire1]);
        _mimicry.Loadout.GetLearnedTechniques((Player)null!).Should().BeEmpty();
        _mimicry.Loadout.GetEquippedTechniques(Player).Should().ContainSingle().Which.Feat.Should().Be(Ice1);
        _mimicry.Loadout.GetEquippedTechniques(Record).Should().ContainSingle();
        _mimicry.Loadout.GetEquippedTechniques((Player)null!).Should().BeEmpty();
    }

    [Test]
    public void IsTechniqueEquipped_ReadsTheLoadoutOfAPlayer()
    {
        Record.EquippedTechniques = new List<FeatType> { Fire1 };

        _mimicry.Loadout.IsTechniqueEquipped(Player, Fire1).Should().BeTrue();
        _mimicry.Loadout.IsTechniqueEquipped(Player, Ice1).Should().BeFalse();
        _mimicry.Loadout.IsTechniqueEquipped(Record, Fire1).Should().BeTrue();
        _mimicry.Loadout.IsTechniqueEquipped((Player)null!, Fire1).Should().BeFalse();
        _mimicry.Loadout.IsTechniqueEquipped(Npc, Fire1).Should().BeFalse();
        _isDm = true;
        _mimicry.Loadout.IsTechniqueEquipped(Player, Fire1).Should().BeFalse();
    }

    // ---- Tests that read the real catalog, the production sources and the container's services ---------------------

    // The code under test reaches converted services through their static facades, which resolve from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();


    [Test]
    public void Mimicry_SkillDecayHandlerRevalidatesEquippedTechniques()
    {
        var handler = typeof(MimicryLoadoutService).GetMethod(
            nameof(MimicryLoadoutService.OnMimicrySkillDecay),
            BindingFlags.Public | BindingFlags.Instance);

        handler.Should().NotBeNull();
        handler!.GetCustomAttributes<NWNEventHandler>()
            .Select(attribute => attribute.Script)
            .Should()
            .Contain(ScriptName.OnSwlorLoseSkill,
                "every Mimicry rank loss must immediately enforce equipped technique requirements");
    }

    [Test]
    public void MimicryTechniqueActivation_RequiresTheTechniqueToRemainEquipped()
    {
        var abilitySource = AbilitySource.Read();

        abilitySource.Should().Contain("ability.IsMimicryTechnique &&");
        abilitySource.Should().Contain("!_mimicryLoadout.IsTechniqueEquipped(activator, abilityType)",
            "both the initial and post-cast activation checks use CanUseAbility, so an unequipped technique cannot resolve");

        var equipped = new Player();
        equipped.EquippedTechniques.Add(FeatType.WardenWallStanceTechnique);

        GameServices.MimicryLoadout.IsTechniqueEquipped(equipped, FeatType.WardenWallStanceTechnique).Should().BeTrue();
        GameServices.MimicryLoadout.IsTechniqueEquipped(equipped, FeatType.SustainBurnStanceTechnique).Should().BeFalse();
        GameServices.MimicryLoadout.IsTechniqueEquipped((Player)null, FeatType.WardenWallStanceTechnique).Should().BeFalse();
    }

    [Test]
    public void MimicryTechniqueUnequip_RemovesDeclaredPersistentEffects()
    {
        var techniques = MimicryTests.BuildAllAbilities(MimicryTests.MimicryTechniqueNamespace)
            .ToDictionary(technique => technique.Feat, technique => technique.Detail);

        foreach (var feat in new[]
                 {
                     FeatType.ApexCollapseStanceTechnique,
                     FeatType.SustainBurnStanceTechnique,
                     FeatType.WardenWallStanceTechnique
                 })
        {
            techniques[feat].IsMimicryStance.Should().BeTrue();
            techniques[feat].StatusEffectTypesRemovedOnPerkRefund.Should().ContainSingle(
                $"{feat} must declare its permanent wearer effect for revocation cleanup");
        }

        techniques[FeatType.WardenWallStanceTechnique]
            .SourceOwnedStatusEffectTypesRemovedOnPerkRefund
            .Should().Contain(typeof(WardenWallStanceAuraStatusEffect),
                "unequipping Warden Wall Stance must also remove the aura it granted to nearby allies");

        foreach (var (feat, statusType) in new[]
                 {
                     (FeatType.FinishingDriveTechnique, typeof(FinishingDriveMomentumStatusEffect)),
                     (FeatType.FinalMandateTechnique, typeof(FinalMandateStatusEffect)),
                     (FeatType.StimCanisterTechnique, typeof(StimCanisterStatusEffect)),
                     (FeatType.SnapRushTechnique, typeof(Hasten1StatusEffect)),
                     (FeatType.WardenSweepTechnique, typeof(WardenSweepStatusEffect)),
                     (FeatType.LastBastionTechnique, typeof(LastBastionBarrierStatusEffect)),
                     (FeatType.LastBastionTechnique, typeof(LastBastionStatusEffect))
                 })
        {
            techniques[feat].SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Should().Contain(statusType,
                "ongoing benefits must still occupy their technique slots, including benefits granted to allies");
        }

        var revokeBody = MimicryBeastDroidSource.Member(MimicryBeastDroidSource.Mimicry, "RevokeTechniqueFeat");

        revokeBody.Should().Contain("detail.StatusEffectTypesRemovedOnPerkRefund");
        revokeBody.Should().Contain("RemoveStatusEffect(player, statusEffectType, false);");
        revokeBody.Should().Contain("detail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund");
        revokeBody.Should().Contain("RemoveStatusEffectsFromAllTargetsBySource(");
    }

    [Test]
    public void Mimicry_MaxSlotsScalesWithCombatAnalyzerAndAnalyzerMemoryPerkLevels()
    {
        var noAnalyzer = new Player();
        GameServices.MimicryLoadout.GetMaxSlots(noAnalyzer).Should().Be(0, "players without Combat Analyzer get no technique slots");

        var memoryWithoutAnalyzer = new Player();
        memoryWithoutAnalyzer.Perks[PerkType.AnalyzerMemory] = 3;
        GameServices.MimicryLoadout.GetMaxSlots(memoryWithoutAnalyzer).Should().Be(0, "Analyzer Memory alone should not grant slots without Combat Analyzer");

        var baseAnalyzer = new Player();
        baseAnalyzer.Perks[PerkType.CombatAnalyzer] = 1;
        GameServices.MimicryLoadout.GetMaxSlots(baseAnalyzer).Should().Be(2);

        var memoryLevel1 = new Player();
        memoryLevel1.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel1.Perks[PerkType.AnalyzerMemory] = 1;
        GameServices.MimicryLoadout.GetMaxSlots(memoryLevel1).Should().Be(4, "Analyzer Memory grants +2 slots per rank");

        var memoryLevel2 = new Player();
        memoryLevel2.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel2.Perks[PerkType.AnalyzerMemory] = 2;
        GameServices.MimicryLoadout.GetMaxSlots(memoryLevel2).Should().Be(6);

        var memoryLevel3 = new Player();
        memoryLevel3.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel3.Perks[PerkType.AnalyzerMemory] = 3;
        GameServices.MimicryLoadout.GetMaxSlots(memoryLevel3).Should().Be(8);

        var overclocked = new Player();
        overclocked.Perks[PerkType.CombatAnalyzer] = 1;
        overclocked.Perks[PerkType.AnalyzerMemory] = 3;
        overclocked.Perks[PerkType.OverclockedAnalyzer] = 1;
        GameServices.MimicryLoadout.GetMaxSlots(overclocked).Should().Be(10, "2 base + 6 from Analyzer Memory + 2 from the Overclocked Analyzer capstone");
    }

    [Test]
    public void Mimicry_UsedSlotsSumsEquippedTechniqueSlotCosts()
    {
        AbilityCatalog.CacheData();
        GameServices.Mimicry.CacheData();

        var empty = new Player();
        GameServices.MimicryLoadout.GetUsedSlots(empty).Should().Be(0);

        var equipped = new Player();
        equipped.EquippedTechniques.Add(FeatType.ToxicSpitTechnique); // 1 slot
        equipped.EquippedTechniques.Add(FeatType.SonicShriekTechnique); // 2 slots
        equipped.EquippedTechniques.Add(FeatType.ToxicCloudTechnique); // 3 slots

        GameServices.MimicryLoadout.GetUsedSlots(equipped).Should().Be(6);
    }

    [Test]
    public void WholeBuildValidationRejectsUnknownUnlearnedDuplicateOverRankAndOverBudgetWithoutMutation()
    {
        AbilityCatalog.CacheData();
        GameServices.Mimicry.CacheData();
        var player = new Player();
        player.Perks[PerkType.CombatAnalyzer] = 1;
        player.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = 50 };
        player.LearnedTechniques[FeatType.FrostSpitTechnique] = DateTime.UtcNow;
        player.LearnedTechniques[FeatType.TerrifyingBellowTechnique] = DateTime.UtcNow;
        player.EquippedTechniques.Add(FeatType.FrostSpitTechnique);
        var before = JsonConvert.SerializeObject(player);
        foreach (var invalid in new FeatType[][]
        {
            [FeatType.Invalid], [FeatType.ToxicSpitTechnique],
            [FeatType.FrostSpitTechnique, FeatType.FrostSpitTechnique], [FeatType.TerrifyingBellowTechnique]
        })
        {
            GameServices.MimicryLoadout.ValidateLoadout(player, invalid, out var error).Should().BeFalse();
            error.Should().NotBeEmpty();
            JsonConvert.SerializeObject(player).Should().Be(before);
        }
        GameServices.MimicryLoadout.ValidateLoadout(player, [FeatType.FrostSpitTechnique], out _).Should().BeTrue();
        player.Skills[SkillType.Mimicry].Rank = 0;
        GameServices.MimicryLoadout.ValidateLoadout(player, [FeatType.FrostSpitTechnique], out var rankError).Should().BeFalse();
        rankError.Should().Contain("rank 3");
        GameServices.MimicryLoadout.ValidateLoadout(player, [], out _).Should().BeTrue();
    }

    // ---- The part itself -------------------------------------------------------------------------------------------

    [Test]
    public void OnPlayerLogin_RunsOnModuleEnterAndOnlyOnALogin()
    {
        var method = typeof(MimicryLoadoutService).GetMethod(nameof(MimicryLoadoutService.OnPlayerLogin))!;

        method.GetCustomAttributes(typeof(NWNEventHandler), false).Cast<NWNEventHandler>().Single().Script
            .Should().Be(ScriptName.OnModuleEnter);
        method.GetCustomAttributes(typeof(PlayerSessionAttribute), false).Cast<PlayerSessionAttribute>().Single().Reload
            .Should().Be(PlayerReload.LoginOnly);
        method.GetCustomAttributes(typeof(BootPhaseAttribute), false).Should().BeEmpty("it runs on a game event, not at boot");
    }

    [Test]
    public void TheLoadoutReadsTheTechniqueTableThroughTheCatalogPartAndCarriesNoState()
    {
        typeof(MimicryLoadoutService).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => field.GetCustomAttributes(typeof(ReloadStateAttribute), false).Length > 0)
            .Should().BeEmpty("the loadout is saved on the player record, so a hot reload has nothing to carry");
        typeof(MimicryLoadoutService).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(IDBService), typeof(IQuickCastService), typeof(IGuiService), typeof(IPerkService),
                typeof(IStatusEffectService), typeof(IMimicryService), typeof(IAbilityCatalog));
    }

    [Test]
    public void TheDefaultSeams_AreTheOriginalCalls()
    {
        var removed = new List<string>();
        var feat = Technique(Fire1);
        var catalog = ScriptedProxy.Create<IAbilityCatalog>(out var catalogScript);
        catalogScript.On("IsFeatRegistered", args => (FeatType)args[0]! == feat);
        catalogScript.On("GetAbilityDetail", args => _mimicry.Abilities[(FeatType)args[0]!]);
        var perk = ScriptedProxy.Create<IPerkService>(out var perkScript);
        perkScript.On("GetPerkLevel", args => (PerkType)args[1]! == PerkType.CombatAnalyzer ? 3 : 0);
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusScript);
        statusScript.On("RemoveStatusEffect", args =>
        {
            removed.Add($"effect:{args[0]}:{((Type)args[1]!).Name}:{args[2]}");
            return null;
        });
        statusScript.On("RemoveStatusEffectsFromAllTargetsBySource", args =>
        {
            removed.Add($"source:{args[0]}:{((Type)args[1]!).Name}:{args[2]}");
            return null;
        });
        var loadout = new DefaultSeamMimicryLoadoutService(_mimicry, _db, catalog, perk, statusEffect);

        loadout.DefaultIsFeatRegistered(feat).Should().BeTrue();
        loadout.DefaultIsFeatRegistered(Plain).Should().BeFalse();
        loadout.DefaultGetAbilityDetail(feat).Should().BeSameAs(_mimicry.Abilities[feat]);
        loadout.DefaultGetPerkLevel(Player, PerkType.CombatAnalyzer).Should().Be(3);
        loadout.DefaultRemoveStatusEffect(Player, typeof(string), true);
        loadout.DefaultRemoveStatusEffectsFromAllTargetsBySource(Player, typeof(int), false, _mimicry.Abilities[feat]);

        removed.Should().Equal($"effect:{Player}:String:True", $"source:{Player}:Int32:False");
    }

    private sealed class DefaultSeamMimicryLoadoutService : MimicryLoadoutService
    {
        public DefaultSeamMimicryLoadoutService(IMimicryService mimicry, IDBService db, IAbilityCatalog catalog, IPerkService perk, IStatusEffectService statusEffect)
            : base(db, FacadeForwarder.For<IQuickCastService>(), FacadeForwarder.For<IGuiService>(), perk, statusEffect, mimicry, catalog)
        {
        }

        public bool DefaultIsFeatRegistered(FeatType feat) => IsFeatRegistered(feat);

        public AbilityDetail DefaultGetAbilityDetail(FeatType feat) => GetAbilityDetail(feat);

        public int DefaultGetPerkLevel(uint creature, PerkType perkType) => GetPerkLevel(creature, perkType);

        public void DefaultRemoveStatusEffect(uint creature, Type type, bool message) => RemoveStatusEffect(creature, type, message);

        public void DefaultRemoveStatusEffectsFromAllTargetsBySource(uint source, Type type, bool message, AbilityDetail ability) =>
            RemoveStatusEffectsFromAllTargetsBySource(source, type, message, ability);
    }
}
