using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the notes instance: the limits and usage figures are pure, and the category storage runs against the
/// in-memory database, so no server is needed.
/// </summary>
[TestFixture]
public class NotesServiceTests
{
    private const string PlayerId = "player-1";

    private InMemoryDBService _db = null!;
    private NotesService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new InMemoryDBService();
        _service = new NotesService(_db);
    }

    private static string QueryOf<T>(InMemoryDBService db, int index = 0)
        where T : EntityBase =>
        ((DBQuery<T>)db.Queries[index]).BuildQuery().QueryString;

    // ---- limits

    [Test]
    public void The_limits_are_the_values_the_window_was_designed_around()
    {
        NotesService.MaxNumberOfNotes.Should().Be(100);
        NotesService.MaxNumberOfCategories.Should().Be(25);
        NotesService.MaxNoteLength.Should().Be(1000);
        NotesService.MaxCategoryNameLength.Should().Be(32);
        NotesService.EntriesPerPage.Should().Be(20);
        NotesService.UncategorizedLabel.Should().Be("Uncategorized");
        NotesService.AllCategoriesLabel.Should().Be("<All Categories>");
    }

    // ---- notes usage

    [TestCase(0, 0f)]
    [TestCase(25, 0.25f)]
    [TestCase(99, 0.99f)]
    [TestCase(100, 1f)]
    [TestCase(250, 1f)]
    public void GetNoteUsagePercentage_is_the_fraction_of_the_limit_capped_at_one(int count, float expected)
    {
        _service.GetNoteUsagePercentage(count).Should().BeApproximately(expected, 0.0001f);
    }

    [TestCase(0, "0 / 100 Notes")]
    [TestCase(37, "37 / 100 Notes")]
    [TestCase(100, "100 / 100 Notes")]
    public void GetNoteUsageText_shows_the_count_against_the_limit(int count, string expected)
    {
        _service.GetNoteUsageText(count).Should().Be(expected);
    }

    [TestCase(0, false)]
    [TestCase(99, false)]
    [TestCase(100, true)]
    [TestCase(101, true)]
    public void IsNoteListFull_is_true_from_the_limit_on(int count, bool expected)
    {
        _service.IsNoteListFull(count).Should().Be(expected);
    }

    [Test]
    public void GetNoteUsageColor_changes_once_the_list_is_full()
    {
        var free = _service.GetNoteUsageColor(99);
        var full = _service.GetNoteUsageColor(100);

        full.Should().NotBeSameAs(free);
        _service.GetNoteUsageColor(0).Should().BeSameAs(free, "every non-full count shares the same colour");
        _service.GetNoteUsageColor(500).Should().BeSameAs(full);
    }

    // ---- category usage

    [TestCase(0, 0f)]
    [TestCase(5, 0.2f)]
    [TestCase(25, 1f)]
    [TestCase(26, 1f)]
    public void GetCategoryUsagePercentage_is_the_fraction_of_the_limit_capped_at_one(int count, float expected)
    {
        _service.GetCategoryUsagePercentage(count).Should().BeApproximately(expected, 0.0001f);
    }

    [TestCase(0, "0 / 25 Categories")]
    [TestCase(12, "12 / 25 Categories")]
    public void GetCategoryUsageText_shows_the_count_against_the_limit(int count, string expected)
    {
        _service.GetCategoryUsageText(count).Should().Be(expected);
    }

    [TestCase(24, false)]
    [TestCase(25, true)]
    public void IsCategoryListFull_is_true_from_the_limit_on(int count, bool expected)
    {
        _service.IsCategoryListFull(count).Should().Be(expected);
    }

    [Test]
    public void GetCategoryUsageColor_shares_the_note_colours()
    {
        _service.GetCategoryUsageColor(24).Should().BeSameAs(_service.GetNoteUsageColor(0));
        _service.GetCategoryUsageColor(25).Should().BeSameAs(_service.GetNoteUsageColor(100));
    }

    [Test]
    public void Each_instance_has_its_own_colours()
    {
        var other = new NotesService(_db);

        other.GetNoteUsageColor(0).Should().NotBeSameAs(_service.GetNoteUsageColor(0));
    }

    // ---- reserved names

    [TestCase("Uncategorized")]
    [TestCase("uncategorized")]
    [TestCase("  UNCATEGORIZED  ")]
    [TestCase("<All Categories>")]
    [TestCase(" <all categories> ")]
    public void IsReservedCategoryName_rejects_the_synthetic_options_in_any_case(string name)
    {
        _service.IsReservedCategoryName(name).Should().BeTrue();
    }

    [TestCase("Quest Log")]
    [TestCase("Uncategorized Items")]
    [TestCase("All Categories")]
    [TestCase("")]
    [TestCase(null)]
    public void IsReservedCategoryName_allows_everything_else(string name)
    {
        _service.IsReservedCategoryName(name).Should().BeFalse();
    }

    // ---- category storage

    [Test]
    public void GetCategories_asks_for_the_players_categories_by_name_up_to_the_category_limit()
    {
        var first = new PlayerNoteCategory { PlayerId = PlayerId, Name = "A" };
        var second = new PlayerNoteCategory { PlayerId = PlayerId, Name = "B" };
        _db.With(first).With(second);

        var categories = _service.GetCategories(PlayerId);

        categories.Should().BeEquivalentTo(new[] { first, second });
        _db.Queries.Should().ContainSingle();
        QueryOf<PlayerNoteCategory>(_db).Should().Contain("@EntityType:\"PlayerNoteCategory\"").And.Contain("@PlayerId:player\\-1");
    }

    [Test]
    public void GetCategories_returns_a_list_the_caller_may_change()
    {
        _db.With(new PlayerNoteCategory { PlayerId = PlayerId, Name = "A" });

        var categories = _service.GetCategories(PlayerId);
        categories.Clear();

        _service.GetCategories(PlayerId).Should().HaveCount(1);
    }

    [Test]
    public void CreateCategory_saves_a_category_for_the_player_and_returns_it()
    {
        var category = _service.CreateCategory(PlayerId, "Quest Log");

        category.PlayerId.Should().Be(PlayerId);
        category.Name.Should().Be("Quest Log");
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(category);
    }

    [Test]
    public void DeleteCategory_clears_the_category_from_the_notes_that_used_it_then_deletes_it()
    {
        var used = new PlayerNote { PlayerId = PlayerId, Name = "used", CategoryId = "cat-1" };
        var other = new PlayerNote { PlayerId = PlayerId, Name = "other", CategoryId = "cat-2" };
        var none = new PlayerNote { PlayerId = PlayerId, Name = "none" };
        _db.With(used).With(other).With(none);

        _service.DeleteCategory(PlayerId, "cat-1");

        used.CategoryId.Should().BeEmpty();
        other.CategoryId.Should().Be("cat-2");
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(used, "only the note that pointed at the category is rewritten");
        _db.Deleted.Should().Equal("cat-1");
    }

    [Test]
    public void DeleteCategory_searches_the_players_own_notes_and_skips_DM_notes()
    {
        _service.DeleteCategory(PlayerId, "cat-1");

        _db.Queries.Should().ContainSingle();
        QueryOf<PlayerNote>(_db).Should().Contain("@EntityType:\"PlayerNote\"").And.Contain("@PlayerId:player\\-1").And.Contain("@IsDMNote:");
        _db.Saved.Should().BeEmpty();
        _db.Deleted.Should().Equal("cat-1");
    }
}
