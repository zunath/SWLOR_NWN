using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.TransferService;

namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>
    /// What a restart does to a City Project donation (Player City Revamp, "City Projects"): when a donation is on record, when the
    /// donor's character file holds its mark, and when the project holds its receipt. No database or engine calls:
    /// <see cref="CityProjectDonationProcess"/> orders the steps and the service reads and writes the records.
    /// </summary>
    public static class CityProjectDonationRules
    {
        #region Planning

        /// <summary>
        /// The donation a player offering <paramref name="offered"/> of a material makes: never more than the project still needs, and
        /// none to a project that is not taking donations. Null when nothing would be taken.
        /// </summary>
        public static CityProjectDonation Plan(CityProject project, string donorId, string donorLabel, string resref, int offered, DateTime nowUtc)
        {
            if (project == null || string.IsNullOrWhiteSpace(donorId) || string.IsNullOrWhiteSpace(resref) || !CityProjectRules.AcceptsDonations(project))
                return null;

            var count = CityProjectRules.ClampDonation(project, resref, offered);
            if (count <= 0)
                return null;

            return new CityProjectDonation
            {
                PlayerId = donorId,
                ProjectId = project.Id,
                Resref = resref,
                Count = count,
                DonorLabel = donorLabel ?? string.Empty,
                CreatedUtc = nowUtc
            };
        }

        #endregion

        #region Deciding

        /// <summary>
        /// What to do with a donation record. The character file is saved without the materials and with the mark before the project is
        /// saved with the donation, so a mark means the materials left the donor:
        /// no mark, they never left, so the record is dropped; a mark and the project's receipt, the project holds them, so the record
        /// and the mark are cleared; a mark and no receipt, the project takes them if it can and the donor gets them back if not.
        /// A mark with no record is a donation that finished, and its mark is cleared.
        /// </summary>
        /// <param name="marked">Whether the donor's character file names the donation.</param>
        /// <param name="applied">Whether the project holds the donation's receipt.</param>
        /// <param name="projectTakesAll">Whether the project is open and can take every item of it.</param>
        public static CityProjectDonationSettlement Settle(bool marked, bool applied, bool projectTakesAll)
        {
            if (!marked)
                return CityProjectDonationSettlement.Drop;
            if (applied)
                return CityProjectDonationSettlement.Forget;

            return projectTakesAll ? CityProjectDonationSettlement.Apply : CityProjectDonationSettlement.Return;
        }

        /// <summary>Whether the project holds the donation: its receipt is on the project.</summary>
        public static bool IsApplied(CityProject project, string donationId)
        {
            return project?.AppliedDonationIds != null && project.AppliedDonationIds.Contains(donationId ?? string.Empty);
        }

        /// <summary>Whether the project can take every item of the donation now. It never takes part of one or the same one twice.</summary>
        public static bool CanApply(CityProject project, CityProjectDonation donation)
        {
            return project != null &&
                   donation is { Count: > 0 } &&
                   !string.IsNullOrWhiteSpace(donation.PlayerId) &&
                   !IsApplied(project, donation.Id) &&
                   CityProjectRules.AcceptsDonations(project) &&
                   CityProjectRules.ClampDonation(project, donation.Resref, donation.Count) >= donation.Count;
        }

        /// <summary>
        /// Gives the project the donation and its receipt, to be saved together. The project that receives its last material starts its
        /// 2 days.
        /// </summary>
        /// <returns>False, changing nothing, when <see cref="CanApply"/> is false.</returns>
        public static bool Apply(CityProject project, CityProjectDonation donation, DateTime nowUtc)
        {
            if (!CanApply(project, donation))
                return false;

            var accepted = CityProjectRules.Accept(project, donation.PlayerId, donation.DonorLabel, donation.Resref, donation.Count, nowUtc);
            if (accepted != donation.Count)
                return false;

            project.AppliedDonationIds ??= new List<string>();
            project.AppliedDonationIds.Add(donation.Id);
            return true;
        }

        /// <summary>
        /// The records of donations the project already holds, which a restart left behind between the project's save and the record's
        /// deletion. They go before the project does, since a record for a project that is gone reads as materials owed back.
        /// </summary>
        public static List<string> GetAppliedRecordIds(CityProject project, IEnumerable<CityProjectDonation> records)
        {
            return records.Where(record => IsApplied(project, record.Id)).Select(record => record.Id).ToList();
        }

        #endregion

        #region Marks

        /// <summary>The donation ids a character file's mark names.</summary>
        public static List<string> ParseMarks(string mark)
        {
            return TransferJournalRules.ParseMarks(mark);
        }

        /// <summary>The mark with the donation's id added, once.</summary>
        public static string AddMark(string mark, string donationId)
        {
            return TransferJournalRules.AddMark(mark, donationId);
        }

        /// <summary>The mark without the donation's id. Empty when nothing is left.</summary>
        public static string RemoveMark(string mark, string donationId)
        {
            return TransferJournalRules.RemoveMark(mark, donationId);
        }

        #endregion

        #region Returning

        /// <summary>
        /// The overflow record's id for one chunk of a returned donation. It never changes, so returning one again after a restart
        /// writes the same records instead of a second set.
        /// </summary>
        public static string GetOverflowRecordId(string donationId, int chunk)
        {
            return $"city-project-donation:{donationId}:{chunk}";
        }

        #endregion

        #region Text

        /// <summary>What the donor reads when a restart interrupted a donation and the project took it at their login.</summary>
        public static string BuildAppliedText(CityProjectDonation donation, string projectName, string itemName)
        {
            return $"A restart interrupted your donation of {donation.Count} {itemName}. {projectName} now has it.";
        }

        /// <summary>What the donor reads when their donation went to overflow because the project could not take it.</summary>
        public static string BuildReturnedText(CityProjectDonation donation, string projectName, string itemName)
        {
            return $"{projectName} could not take your donation of {donation.Count} {itemName}, so it is in your overflow storage. Use /overflow to claim it.";
        }

        #endregion
    }
}
