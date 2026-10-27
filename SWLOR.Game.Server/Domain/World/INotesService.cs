using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The limits, usage figures and category storage behind the player notes window. The static facade
    /// <c>Service.Notes</c> forwards to the container's instance of this interface and repeats the constants.
    /// </summary>
    public interface INotesService
    {
        /// <summary>Fraction of the note limit a player has used, from 0 to 1.</summary>
        float GetNoteUsagePercentage(int noteCount);

        /// <summary>The note usage as shown in the window, such as "12 / 100 Notes".</summary>
        string GetNoteUsageText(int noteCount);

        /// <summary>The usage bar colour: the "full" colour once the note limit is reached.</summary>
        GuiColor GetNoteUsageColor(int noteCount);

        /// <summary>True when a player has reached the note limit.</summary>
        bool IsNoteListFull(int noteCount);

        /// <summary>Fraction of the category limit a player has used, from 0 to 1.</summary>
        float GetCategoryUsagePercentage(int categoryCount);

        /// <summary>The category usage as shown in the window, such as "3 / 25 Categories".</summary>
        string GetCategoryUsageText(int categoryCount);

        /// <summary>The usage bar colour: the "full" colour once the category limit is reached.</summary>
        GuiColor GetCategoryUsageColor(int categoryCount);

        /// <summary>True when a player has reached the category limit.</summary>
        bool IsCategoryListFull(int categoryCount);

        /// <summary>
        /// Determines whether a player-supplied category name collides with one of the synthetic
        /// combo box options. Such a category would render as a second, visually identical entry
        /// meaning something entirely different, so these names are rejected on creation.
        /// </summary>
        bool IsReservedCategoryName(string name);

        /// <summary>Loads a player's note categories, ordered by name, up to the category limit.</summary>
        List<PlayerNoteCategory> GetCategories(string playerId);

        /// <summary>Creates and saves a note category for a player.</summary>
        PlayerNoteCategory CreateCategory(string playerId, string name);

        /// <summary>Deletes a category and clears it from every note of the player that used it.</summary>
        void DeleteCategory(string playerId, string categoryId);
    }
}
