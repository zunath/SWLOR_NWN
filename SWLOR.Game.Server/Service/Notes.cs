using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service
{
    public static class Notes
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

        private static readonly GuiColor _usageFreeColor = new(90, 150, 95);
        private static readonly GuiColor _usageFullColor = new(200, 100, 70);

        public static float GetNoteUsagePercentage(int noteCount)
        {
            return GetUsagePercentage(noteCount, MaxNumberOfNotes);
        }

        public static string GetNoteUsageText(int noteCount)
        {
            return $"{noteCount} / {MaxNumberOfNotes} Notes";
        }

        public static GuiColor GetNoteUsageColor(int noteCount)
        {
            return IsNoteListFull(noteCount) ? _usageFullColor : _usageFreeColor;
        }

        public static bool IsNoteListFull(int noteCount)
        {
            return noteCount >= MaxNumberOfNotes;
        }

        public static float GetCategoryUsagePercentage(int categoryCount)
        {
            return GetUsagePercentage(categoryCount, MaxNumberOfCategories);
        }

        public static string GetCategoryUsageText(int categoryCount)
        {
            return $"{categoryCount} / {MaxNumberOfCategories} Categories";
        }

        public static GuiColor GetCategoryUsageColor(int categoryCount)
        {
            return IsCategoryListFull(categoryCount) ? _usageFullColor : _usageFreeColor;
        }

        public static bool IsCategoryListFull(int categoryCount)
        {
            return categoryCount >= MaxNumberOfCategories;
        }

        private static float GetUsagePercentage(int count, int maximum)
        {
            return count >= maximum
                ? 1f
                : (float)count / maximum;
        }

        /// <summary>
        /// Determines whether a player-supplied category name collides with one of the synthetic
        /// combo box options. Such a category would render as a second, visually identical entry
        /// meaning something entirely different, so these names are rejected on creation.
        /// </summary>
        public static bool IsReservedCategoryName(string name)
        {
            var trimmed = (name ?? string.Empty).Trim();

            return string.Equals(trimmed, UncategorizedLabel, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trimmed, AllCategoriesLabel, StringComparison.OrdinalIgnoreCase);
        }

        public static List<PlayerNoteCategory> GetCategories(string playerId)
        {
            // An unpaged DBQuery silently falls back to a 50 record limit, so every query here
            // states its own limit rather than relying on that default.
            var query = new DBQuery<PlayerNoteCategory>()
                .AddFieldSearch(nameof(PlayerNoteCategory.PlayerId), playerId, false)
                .OrderBy(nameof(PlayerNoteCategory.Name))
                .AddPaging(MaxNumberOfCategories, 0);

            return DB.Search(query).ToList();
        }

        public static PlayerNoteCategory CreateCategory(string playerId, string name)
        {
            var category = new PlayerNoteCategory
            {
                PlayerId = playerId,
                Name = name,
            };

            DB.Set(category);
            return category;
        }

        public static void DeleteCategory(string playerId, string categoryId)
        {
            // Must be paged to the full note cap - any note past the default 50 record limit would
            // keep pointing at the deleted category.
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), playerId, false)
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), false)
                .AddPaging(MaxNumberOfNotes, 0);

            foreach (var note in DB.Search(query).Where(note => note.CategoryId == categoryId))
            {
                note.CategoryId = string.Empty;
                DB.Set(note);
            }

            DB.Delete<PlayerNoteCategory>(categoryId);
        }
    }
}
