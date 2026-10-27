using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The limits, usage figures and category storage behind the player notes window: the logic that used to be static
    /// in <c>Service.Notes</c>, moved here unchanged. The facade repeats the constants and forwards every call to the
    /// container's instance.
    /// </summary>
    public class NotesService : INotesService
    {
        public const int MaxNumberOfNotes = 100;
        public const int MaxNumberOfCategories = 25;
        public const int MaxNoteLength = 1000;
        public const int MaxCategoryNameLength = 32;
        public const int EntriesPerPage = 20;

        /// <summary>
        /// Label for the synthetic "no category assigned" option prepended to the note editor's
        /// category combo box.
        /// </summary>
        public const string UncategorizedLabel = "Uncategorized";

        /// <summary>
        /// Label for the synthetic "no filter" option prepended to the category filter combo box.
        /// </summary>
        public const string AllCategoriesLabel = "<All Categories>";

        private readonly IDBService _db;
        private readonly GuiColor _usageFreeColor = new(90, 150, 95);
        private readonly GuiColor _usageFullColor = new(200, 100, 70);

        public NotesService(IDBService db)
        {
            _db = db;
        }

        public float GetNoteUsagePercentage(int noteCount)
        {
            return GetUsagePercentage(noteCount, MaxNumberOfNotes);
        }

        public string GetNoteUsageText(int noteCount)
        {
            return $"{noteCount} / {MaxNumberOfNotes} Notes";
        }

        public GuiColor GetNoteUsageColor(int noteCount)
        {
            return IsNoteListFull(noteCount) ? _usageFullColor : _usageFreeColor;
        }

        public bool IsNoteListFull(int noteCount)
        {
            return noteCount >= MaxNumberOfNotes;
        }

        public float GetCategoryUsagePercentage(int categoryCount)
        {
            return GetUsagePercentage(categoryCount, MaxNumberOfCategories);
        }

        public string GetCategoryUsageText(int categoryCount)
        {
            return $"{categoryCount} / {MaxNumberOfCategories} Categories";
        }

        public GuiColor GetCategoryUsageColor(int categoryCount)
        {
            return IsCategoryListFull(categoryCount) ? _usageFullColor : _usageFreeColor;
        }

        public bool IsCategoryListFull(int categoryCount)
        {
            return categoryCount >= MaxNumberOfCategories;
        }

        private float GetUsagePercentage(int count, int maximum)
        {
            return count >= maximum
                ? 1f
                : (float)count / maximum;
        }

        public bool IsReservedCategoryName(string name)
        {
            var trimmed = (name ?? string.Empty).Trim();

            return string.Equals(trimmed, UncategorizedLabel, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trimmed, AllCategoriesLabel, StringComparison.OrdinalIgnoreCase);
        }

        public List<PlayerNoteCategory> GetCategories(string playerId)
        {
            // An unpaged DBQuery silently falls back to a 50 record limit, so every query here
            // states its own limit rather than relying on that default.
            var query = new DBQuery<PlayerNoteCategory>()
                .AddFieldSearch(nameof(PlayerNoteCategory.PlayerId), playerId, false)
                .OrderBy(nameof(PlayerNoteCategory.Name))
                .AddPaging(MaxNumberOfCategories, 0);

            return _db.Search(query).ToList();
        }

        public PlayerNoteCategory CreateCategory(string playerId, string name)
        {
            var category = new PlayerNoteCategory
            {
                PlayerId = playerId,
                Name = name,
            };

            _db.Set(category);
            return category;
        }

        public void DeleteCategory(string playerId, string categoryId)
        {
            // Must be paged to the full note cap - any note past the default 50 record limit would
            // keep pointing at the deleted category.
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), playerId, false)
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), false)
                .AddPaging(MaxNumberOfNotes, 0);

            foreach (var note in _db.Search(query).Where(note => note.CategoryId == categoryId))
            {
                note.CategoryId = string.Empty;
                _db.Set(note);
            }

            _db.Delete<PlayerNoteCategory>(categoryId);
        }
    }
}
