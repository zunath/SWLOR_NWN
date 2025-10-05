using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for AreaNote entity operations.
    /// </summary>
    public interface IAreaNoteRepository
    {
        /// <summary>
        /// Gets an area note by its unique identifier.
        /// </summary>
        /// <param name="id">The area note's unique identifier</param>
        /// <returns>The area note if found, null otherwise</returns>
        AreaNote GetById(string id);

        /// <summary>
        /// Gets all area notes by area resref.
        /// </summary>
        /// <param name="areaResref">The area resref to search for</param>
        /// <returns>Collection of area notes for the specified area</returns>
        IEnumerable<AreaNote> GetByAreaResref(string areaResref);

        /// <summary>
        /// Saves an area note entity.
        /// </summary>
        /// <param name="areaNote">The area note to save</param>
        void Save(AreaNote areaNote);

        /// <summary>
        /// Deletes an area note by its unique identifier.
        /// </summary>
        /// <param name="id">The area note's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if an area note exists by its unique identifier.
        /// </summary>
        /// <param name="id">The area note's unique identifier</param>
        /// <returns>True if the area note exists, false otherwise</returns>
        bool Exists(string id);
    }
}
