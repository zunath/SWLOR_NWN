using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// The database and engine side of the crafting style selection: recording, forgetting and restoring the style effects. The
/// record rules are covered by <c>CraftingStyleSelectionTests</c>.
/// </summary>
[TestFixture]
public class CraftStyleEffectsServiceTests
{
    private const uint Handle = 1001;

    private sealed class TestStyles : CraftStyleEffectsService
    {
        public TestStyles(IDBService db)
            : base(db, FacadeForwarder.For<IStatusEffectService>())
        {
        }

        public HashSet<Type> Active { get; } = new();
        public List<Type> Applied { get; } = new();
        public List<Type> Removed { get; } = new();

        protected override bool HasEffect(Type effect, uint creature) => Active.Contains(effect);

        protected override void ApplyPermanentEffect(Type effect, uint creature) => Applied.Add(effect);

        protected override void RemoveEffect(uint creature, Type effect) => Removed.Add(effect);
    }

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private Player _player = null!;
    private TestStyles _service = null!;
    private CraftingStyleDefinition _definition = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _engine.Engine.SetReturn("GetIsObjectValid", true);
        _engine.Engine.SetReturn("GetIsDMPossessed", false);
        _engine.Engine.SetReturn("GetIsDead", false);
        _db = new InMemoryDBService();
        _player = new Player(PlayerEngine.PlayerId);
        _db.With(_player);
        _service = new TestStyles(_db);
        _definition = CraftingStyleSelection.Definitions.First();
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    [Test]
    public void Record_saves_the_selected_style_once()
    {
        _service.Record(Handle, _definition.Skill, _definition.Perk);
        _service.Record(Handle, _definition.Skill, _definition.Perk);

        CraftingStyleSelection.Selected(_player, _definition.Skill).Should().Be(_definition.Perk);
        _db.Saved.Should().ContainSingle("a repeated selection changes nothing");
    }

    [Test]
    public void Record_and_ClearIfSelected_ignore_a_creature_that_is_not_a_player()
    {
        _engine.Engine.SetReturn("GetIsPC", false);

        _service.Record(Handle, _definition.Skill, _definition.Perk);
        _service.ClearIfSelected(Handle, _definition.Skill, _definition.Perk);
        _service.Restore(Handle);

        _db.Saved.Should().BeEmpty();
        _db.Reads.Should().BeEmpty();
    }

    [Test]
    public void Record_ignores_a_player_without_a_record()
    {
        _engine.Engine.SetReturn("GetObjectUUID", "nobody");

        _service.Record(Handle, _definition.Skill, _definition.Perk);
        _service.ClearIfSelected(Handle, _definition.Skill, _definition.Perk);
        _service.Restore(Handle);

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void ClearIfSelected_forgets_the_selection_only_when_it_names_the_perk()
    {
        CraftingStyleSelection.Set(_player, _definition.Skill, _definition.Perk);

        _service.ClearIfSelected(Handle, _definition.Skill, PerkType.Invalid);
        CraftingStyleSelection.Selected(_player, _definition.Skill).Should().Be(_definition.Perk);
        _db.Saved.Should().BeEmpty();

        _service.ClearIfSelected(Handle, _definition.Skill, _definition.Perk);
        CraftingStyleSelection.Selected(_player, _definition.Skill).Should().Be(PerkType.Invalid);
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void Restore_applies_the_selected_style_the_player_still_owns_and_removes_every_other_style_effect()
    {
        CraftingStyleSelection.Set(_player, _definition.Skill, _definition.Perk);
        _player.Perks[_definition.Perk] = 1;
        var others = CraftingStyleSelection.Definitions.Where(definition => definition != _definition).ToList();
        _service.Active.Add(others[0].StatusEffectType);

        _service.Restore(Handle);

        _service.Applied.Should().Equal(_definition.StatusEffectType);
        _service.Removed.Should().Equal(others[0].StatusEffectType);
    }

    [Test]
    public void Restore_leaves_an_active_style_alone_and_drops_a_selection_whose_perk_was_lost()
    {
        CraftingStyleSelection.Set(_player, _definition.Skill, _definition.Perk);
        _service.Active.Add(_definition.StatusEffectType);
        _player.Perks[_definition.Perk] = 1;

        _service.Restore(Handle);
        _service.Applied.Should().BeEmpty();
        _service.Removed.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();

        _player.Perks.Clear();
        _service.Restore(Handle);
        _service.Removed.Should().Equal(_definition.StatusEffectType);
        _player.SelectedCraftingStyles.Should().BeEmpty();
        _db.Saved.Should().ContainSingle("the dropped selection is saved");
    }

    [Test]
    public void Restore_leaves_a_dead_player_alone_until_they_revive()
    {
        _engine.Engine.SetReturn("GetIsDead", true);
        CraftingStyleSelection.Set(_player, _definition.Skill, _definition.Perk);
        _player.Perks[_definition.Perk] = 1;

        _service.Restore(Handle);

        _service.Applied.Should().BeEmpty();
        _db.Reads.Should().BeEmpty();
    }
}
