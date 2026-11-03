using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>
    /// The steps of a City Project donation, in the order that makes a restart safe. The record goes first, so the donation is on
    /// file before anything moves. The materials leave the donor's inventory next, and the character file is saved without them and with
    /// a mark naming the record. The project is then saved with the donation and its receipt, and the record goes. The mark goes last.
    /// A restart after any step is settled at the donor's next login by <see cref="SettleAtLogin"/>, which reads the three places and
    /// repeats only the steps that did not finish (<see cref="CityProjectDonationRules.Settle"/>).
    /// </summary>
    public static class CityProjectDonationProcess
    {
        /// <summary>
        /// Takes a donation from the donor and gives it to the project, or, if the project cannot take it, to the donor's overflow store.
        /// <see cref="CityProjectDonationSettlement.Drop"/>, with nothing changed, when the donor's inventory could not give the materials up.
        /// </summary>
        public static CityProjectDonationSettlement Run(ICityProjectDonationStore store, CityProjectDonation donation)
        {
            store.SavePending(donation);
            if (!store.TakeAndMark(donation))
            {
                store.DeletePending(donation.Id);
                return CityProjectDonationSettlement.Drop;
            }

            return Settle(store, donation, marked: true);
        }

        /// <summary>
        /// Settles what a restart left behind at a donor's login: every donation record they have, by the mark in their character file,
        /// then every mark that has no record, which a donation that finished leaves when the restart came before its mark was cleared.
        /// </summary>
        /// <param name="store">The records and the donor's character.</param>
        /// <param name="markedIds">The donation ids in the donor's character file mark.</param>
        /// <param name="records">The donor's donation records.</param>
        /// <returns>What was done with each record.</returns>
        public static List<(CityProjectDonation Donation, CityProjectDonationSettlement Settlement)> SettleAtLogin(
            ICityProjectDonationStore store,
            IReadOnlyCollection<string> markedIds,
            IReadOnlyList<CityProjectDonation> records)
        {
            var outcomes = new List<(CityProjectDonation, CityProjectDonationSettlement)>();
            foreach (var record in records)
            {
                outcomes.Add((record, Settle(store, record, markedIds.Contains(record.Id))));
            }

            var recorded = records.Select(record => record.Id).ToHashSet();
            foreach (var id in markedIds.Where(id => !recorded.Contains(id)))
            {
                store.ClearMark(id);
            }

            return outcomes;
        }

        private static CityProjectDonationSettlement Settle(ICityProjectDonationStore store, CityProjectDonation donation, bool marked)
        {
            var applied = marked && store.IsApplied(donation);
            var settlement = CityProjectDonationRules.Settle(marked, applied, marked && !applied && store.ProjectTakes(donation));
            switch (settlement)
            {
                case CityProjectDonationSettlement.Drop:
                    store.DeletePending(donation.Id);
                    break;
                case CityProjectDonationSettlement.Forget:
                    store.DeletePending(donation.Id);
                    store.ClearMark(donation.Id);
                    break;
                case CityProjectDonationSettlement.Apply:
                    store.ApplyToProject(donation);
                    store.DeletePending(donation.Id);
                    store.ClearMark(donation.Id);
                    break;
                case CityProjectDonationSettlement.Return:
                    store.ReturnToDonor(donation);
                    store.DeletePending(donation.Id);
                    store.ClearMark(donation.Id);
                    break;
            }

            return settlement;
        }
    }
}
