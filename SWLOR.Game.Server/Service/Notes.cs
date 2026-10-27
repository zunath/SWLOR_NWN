using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the player notes limits and categories. The logic lives in <see cref="INotesService"/>; every
    /// member here forwards to the container's instance.
    /// </summary>
    public static class Notes
    {
        // Compile-time constants: callers get the values inlined, as before.
        public const int MaxNumberOfNotes = Domain.World.NotesService.MaxNumberOfNotes;
        public const int MaxNumberOfCategories = Domain.World.NotesService.MaxNumberOfCategories;
        public const int MaxNoteLength = Domain.World.NotesService.MaxNoteLength;
        public const int MaxCategoryNameLength = Domain.World.NotesService.MaxCategoryNameLength;
        public const int EntriesPerPage = Domain.World.NotesService.EntriesPerPage;

        /// <summary>
        /// Label for the synthetic "no category assigned" option prepended to the note editor's
        /// category combo box.
        /// </summary>
        public const string UncategorizedLabel = Domain.World.NotesService.UncategorizedLabel;

        /// <summary>
        /// Label for the synthetic "no filter" option prepended to the category filter combo box.
        /// </summary>
        public const string AllCategoriesLabel = Domain.World.NotesService.AllCategoriesLabel;

        private static INotesService Instance => FacadeAccessor.Resolve<INotesService>();

        public static float GetNoteUsagePercentage(int noteCount)
        {
            return Instance.GetNoteUsagePercentage(noteCount);
        }

        public static string GetNoteUsageText(int noteCount)
        {
            return Instance.GetNoteUsageText(noteCount);
        }

        public static GuiColor GetNoteUsageColor(int noteCount)
        {
            return Instance.GetNoteUsageColor(noteCount);
        }

        public static bool IsNoteListFull(int noteCount)
        {
            return Instance.IsNoteListFull(noteCount);
        }

        public static float GetCategoryUsagePercentage(int categoryCount)
        {
            return Instance.GetCategoryUsagePercentage(categoryCount);
        }

        public static string GetCategoryUsageText(int categoryCount)
        {
            return Instance.GetCategoryUsageText(categoryCount);
        }

        public static GuiColor GetCategoryUsageColor(int categoryCount)
        {
            return Instance.GetCategoryUsageColor(categoryCount);
        }

        public static bool IsCategoryListFull(int categoryCount)
        {
            return Instance.IsCategoryListFull(categoryCount);
        }

        /// <summary>
        /// Determines whether a player-supplied category name collides with one of the synthetic
        /// combo box options. Such a category would render as a second, visually identical entry
        /// meaning something entirely different, so these names are rejected on creation.
        /// </summary>
        public static bool IsReservedCategoryName(string name)
        {
            return Instance.IsReservedCategoryName(name);
        }

        public static List<PlayerNoteCategory> GetCategories(string playerId)
        {
            return Instance.GetCategories(playerId);
        }

        public static PlayerNoteCategory CreateCategory(string playerId, string name)
        {
            return Instance.CreateCategory(playerId, name);
        }

        public static void DeleteCategory(string playerId, string categoryId)
        {
            Instance.DeleteCategory(playerId, categoryId);
        }
    }
}
