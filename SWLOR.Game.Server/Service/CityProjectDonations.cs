using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityProjectService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Donating to a City Project (Player City Revamp, "City Projects"). The materials live in the donor's character file and the project
    /// in the database, which cannot be saved together, so a donation is journaled the way <see cref="PendingCredits"/> and
    /// <see cref="OverflowStorage"/> move credits and items. The donation is recorded (<see cref="CityProjectDonation"/>), the materials
    /// are taken and the character file is saved with a mark naming the record, the project is saved with the donation and its receipt,
    /// the record is deleted, and the mark is cleared. The steps and the decision for each restart point are
    /// <see cref="CityProjectDonationProcess"/> and <see cref="CityProjectDonationRules"/>; this class reads and writes the records, the
    /// inventory and the mark.
    /// <para>
    /// A restart is settled at the donor's next login: a record with no mark never left the inventory and is dropped; a record with a
    /// mark the project already holds is cleared; a record with a mark the project does not yet hold is applied, or returned to the
    /// donor's overflow store when the project cannot take it; a mark with no record is cleared. The records of donors who are offline
    /// wait for their login, since a character file can only be read once the character is loaded; the project never counts them until
    /// then, and cancelling it refunds only the donations it holds.
    /// </para>
    /// </summary>
    public static class CityProjectDonations
    {
        /// <summary>The character-file local naming the donations whose materials the file no longer holds, as comma-separated record ids.</summary>
        private const string MarkVariable = "CITY_PROJECT_DONATIONS";

        /// <summary>The client needs a moment after entering before chat messages show.</summary>
        private const float MessageDelaySeconds = 6f;

        #region Donating

        /// <summary>
        /// Donates up to <paramref name="count"/> of a material from the player's inventory. The project takes only what it still needs,
        /// so a player offering more keeps the rest. Anyone may donate, visitors included.
        /// </summary>
        /// <returns>How many items the project took.</returns>
        public static int Donate(uint player, string projectId, string resref, int count)
        {
            var project = CityProjects.GetProject(projectId);
            if (project == null || !GetIsPC(player) || GetIsDM(player))
                return 0;

            var offered = Math.Min(Math.Max(0, count), CityProjects.CountCarried(player, resref));
            var donation = CityProjectDonationRules.Plan(project, GetObjectUUID(player), Disguise.GetDisplayDescriptor(player), resref, offered, DateTime.UtcNow);
            if (donation == null)
                return 0;

            var settlement = CityProjectDonationProcess.Run(new Store(player), donation);
            if (settlement == CityProjectDonationSettlement.Drop)
                return 0;

            if (settlement == CityProjectDonationSettlement.Return)
            {
                SendMessageToPC(player, ColorToken.Red(CityProjectDonationRules.BuildReturnedText(donation, CityProjectRules.GetName(project), CityProjects.GetItemName(resref))));
                return 0;
            }

            return donation.Count;
        }

        #endregion

        #region Login

        /// <summary>Settles the donations a restart interrupted, before anything else the donor does.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var marks = CityProjectDonationRules.ParseMarks(ObjectPlugin.GetString(player, MarkVariable));
            var records = Load(nameof(CityProjectDonation.PlayerId), GetObjectUUID(player));
            if (marks.Count == 0 && records.Count == 0)
                return;

            var outcomes = CityProjectDonationProcess.SettleAtLogin(new Store(player), marks, records);
            foreach (var (donation, settlement) in outcomes)
            {
                Tell(player, donation, settlement);
            }
        }

        private static void Tell(uint player, CityProjectDonation donation, CityProjectDonationSettlement settlement)
        {
            if (settlement != CityProjectDonationSettlement.Apply && settlement != CityProjectDonationSettlement.Return)
                return;

            var project = CityProjects.GetProject(donation.ProjectId);
            var projectName = project == null ? "The city project" : CityProjectRules.GetName(project);
            var itemName = CityProjects.GetItemName(donation.Resref);
            var text = settlement == CityProjectDonationSettlement.Apply
                ? ColorToken.Green(CityProjectDonationRules.BuildAppliedText(donation, projectName, itemName))
                : ColorToken.Red(CityProjectDonationRules.BuildReturnedText(donation, projectName, itemName));
            DelayCommand(MessageDelaySeconds, () =>
            {
                if (GetIsObjectValid(player))
                    SendMessageToPC(player, text);
            });
        }

        #endregion

        #region Records

        /// <summary>Every donation record of a project, oldest first.</summary>
        internal static List<CityProjectDonation> LoadForProject(string projectId)
        {
            return Load(nameof(CityProjectDonation.ProjectId), projectId);
        }

        private static List<CityProjectDonation> Load(string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<CityProjectDonation>();

            var query = new DBQuery<CityProjectDonation>().AddFieldSearch(field, value, false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<CityProjectDonation>();

            return DB.Search(query.AddPaging(count, 0))
                .Where(x => x != null)
                .OrderBy(x => x.CreatedUtc)
                .ToList();
        }

        #endregion

        /// <summary>The records, the project and the donor's character, behind the steps <see cref="CityProjectDonationProcess"/> orders.</summary>
        private sealed class Store : ICityProjectDonationStore
        {
            private readonly uint _player;

            public Store(uint player)
            {
                _player = player;
            }

            public void SavePending(CityProjectDonation donation)
            {
                DB.Set(donation);
            }

            public void DeletePending(string donationId)
            {
                DB.Delete<CityProjectDonation>(donationId);
            }

            public bool TakeAndMark(CityProjectDonation donation)
            {
                // The materials leave the inventory before the file is saved, not just get queued for destruction: native destruction
                // waits for the end of this script, so a file saved now would still hold them.
                var removal = new InventoryRemoval(_player);
                try
                {
                    if (!removal.TryTake(donation.Resref, donation.Count))
                    {
                        removal.Restore();
                        return false;
                    }

                    ObjectPlugin.SetString(_player, MarkVariable, CityProjectDonationRules.AddMark(ObjectPlugin.GetString(_player, MarkVariable), donation.Id), true);
                    CharacterCheckpoint.Save(_player);
                }
                catch
                {
                    removal.Restore();
                    throw;
                }

                removal.Destroy();
                return true;
            }

            public void ClearMark(string donationId)
            {
                var mark = CityProjectDonationRules.RemoveMark(ObjectPlugin.GetString(_player, MarkVariable), donationId);
                if (string.IsNullOrEmpty(mark))
                    ObjectPlugin.DeleteString(_player, MarkVariable);
                else
                    ObjectPlugin.SetString(_player, MarkVariable, mark, true);

                CharacterCheckpoint.Save(_player);
            }

            public bool IsApplied(CityProjectDonation donation)
            {
                return CityProjectDonationRules.IsApplied(DB.Get<CityProject>(donation.ProjectId), donation.Id);
            }

            public bool ProjectTakes(CityProjectDonation donation)
            {
                return CityProjectDonationRules.CanApply(DB.Get<CityProject>(donation.ProjectId), donation);
            }

            public void ApplyToProject(CityProjectDonation donation)
            {
                var project = DB.Get<CityProject>(donation.ProjectId);
                if (!CityProjectDonationRules.Apply(project, donation, DateTime.UtcNow))
                    return;

                DB.Set(project);

                Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(_player)} donated {donation.Count} x {donation.Resref} to city project '{CityProjectRules.GetName(project)}' ({project.Id}) of city {project.CityId}; donation {donation.Id}.");
                if (project.SuppliedUtc.HasValue)
                {
                    Log.Write(LogGroup.Property, $"City project '{CityProjectRules.GetName(project)}' ({project.Id}) is fully supplied and completes at {project.CompletesUtc:u}.");
                }
            }

            public void ReturnToDonor(CityProjectDonation donation)
            {
                CityProjects.WriteOverflow(
                    donation.PlayerId,
                    donation.Resref,
                    donation.Count,
                    chunk => CityProjectDonationRules.GetOverflowRecordId(donation.Id, chunk),
                    $"City project donation {donation.Id}");
                Log.Write(LogGroup.Property, $"Donation {donation.Id} of {donation.Count} x {donation.Resref} by {PlayerName.GetAuditName(_player)} to city project {donation.ProjectId} was returned to overflow: the project could not take it.");
            }
        }
    }
}
