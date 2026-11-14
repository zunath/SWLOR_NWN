using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>
    /// The records and the character a donation touches. <see cref="CityProjectDonationProcess"/> calls them in a fixed order, so the
    /// order can be tested without a database or an engine; the service implements them over the real records.
    /// </summary>
    public interface ICityProjectDonationStore
    {
        /// <summary>Saves the donation record.</summary>
        void SavePending(CityProjectDonation donation);

        /// <summary>Deletes the donation record. Deleting one that is gone changes nothing.</summary>
        void DeletePending(string donationId);

        /// <summary>
        /// Takes the donated materials out of the donor's inventory, adds the donation's id to the mark in their character file, and
        /// saves the character file. The materials are out of the inventory when the file is saved, not merely queued for destruction.
        /// Returns false, having changed nothing, when the donor's inventory could not give them up.
        /// </summary>
        bool TakeAndMark(CityProjectDonation donation);

        /// <summary>Removes the donation's id from the mark in the donor's character file and saves the character file.</summary>
        void ClearMark(string donationId);

        /// <summary>Whether the project already holds the donation (its receipt is on the project).</summary>
        bool IsApplied(CityProjectDonation donation);

        /// <summary>Whether the project is open and can take every item of the donation.</summary>
        bool ProjectTakes(CityProjectDonation donation);

        /// <summary>Saves the project with the donation and its receipt in one write. Applying one the project holds changes nothing.</summary>
        void ApplyToProject(CityProjectDonation donation);

        /// <summary>
        /// Writes the donation's materials to the donor's overflow store under record ids that never change, so doing it twice writes the
        /// same records.
        /// </summary>
        void ReturnToDonor(CityProjectDonation donation);
    }
}
