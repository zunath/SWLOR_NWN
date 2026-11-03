using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.CityProjectService;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// City Projects (Player City Revamp, "City Projects"): a facility upgrade, Home Ground, or a station's Jump Beacon level, paid for
    /// by the treasury and built from materials that anyone donates at City Hall. The rules are <see cref="CityProjectRules"/>; this
    /// class reads and writes the records and the inventory.
    /// <para>
    /// Every step that moves money or items is journaled on the project's own state, so a restart repeats the steps that did not finish
    /// and none that did. The credits sit in the city's escrow under the project's id, with the part from each balance remembered.
    /// Starting: the project is saved as Starting, the credits are held in one save of the city, then it is Open. Completing: the
    /// project is saved as Completing, then one save of the city pays the escrow out and applies the level, then the project is
    /// deleted. Cancelling: Cancelling, then each donor's materials are written to their overflow store under ids that never change,
    /// then the escrow returns to its balances, then the project is deleted. A completed project is applied at server start, so one
    /// that came due while the server was down is applied at the next start.
    /// </para>
    /// </summary>
    public static class CityProjects
    {
        #region Home Ground

        /// <summary>Whether the city has built Home Ground. Every Home Ground bonus reads this, so there is one source of truth.</summary>
        public static bool HasHomeGround(WorldProperty city)
        {
            return CityProjectRules.IsHomeGroundBuilt(city, DateTime.UtcNow);
        }

        #endregion

        #region Records

        public static CityProject GetProject(string projectId)
        {
            return string.IsNullOrWhiteSpace(projectId) ? null : DB.Get<CityProject>(projectId);
        }

        /// <summary>Every project the city has, in any state, oldest first.</summary>
        public static List<CityProject> GetProjects(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return new List<CityProject>();

            return Search(new DBQuery<CityProject>().AddFieldSearch(nameof(CityProject.CityId), cityId, false))
                .OrderBy(project => project.StartedUtc)
                .ToList();
        }

        /// <summary>The city's project of the type, in any state, or null.</summary>
        public static CityProject GetProjectOfType(string cityId, CityProjectType type)
        {
            return GetProjects(cityId).FirstOrDefault(project => project.ProjectType == type);
        }

        private static List<CityProject> Search(DBQuery<CityProject> query)
        {
            var count = (int)DB.SearchCount(query);
            return count <= 0 ? new List<CityProject>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        public static string GetItemName(string resref)
        {
            var name = Cache.GetItemNameByResref(resref);
            return string.IsNullOrWhiteSpace(name) ? resref : name;
        }

        #endregion

        #region Starting

        /// <summary>
        /// The refusal for starting the project now, or <see cref="CityProjectRefusal.None"/>: the facility's next level, or Home Ground.
        /// </summary>
        public static CityProjectRefusal CheckStart(WorldProperty city, CityProjectType type, string playerId)
        {
            if (city == null || city.PropertyType != PropertyType.City || city.IsQueuedForDeletion)
                return CityProjectRefusal.Unknown;

            var upgrade = CityProjectRules.GetUpgradeType(type);
            var currentLevel = upgrade == PropertyUpgradeType.Invalid ? 0 : city.Upgrades.GetValueOrDefault(upgrade, 1);
            return CityProjectRules.CheckStart(
                type,
                currentLevel,
                CityOfficers.GetCityLevel(city),
                GetMayorCityManagementRank(city),
                Station.IsStation(city),
                HasHomeGround(city),
                GetProjectOfType(city.Id, type) != null,
                CityTreasuryRules.GetSpendable(city),
                CityPowers.Can(city, playerId, CityPower.StartProjects));
        }

        private static int GetMayorCityManagementRank(WorldProperty city)
        {
            var mayor = string.IsNullOrWhiteSpace(city.OwnerPlayerId) ? null : DB.Get<Player>(city.OwnerPlayerId);
            return mayor != null && mayor.Perks.TryGetValue(PerkType.CityManagement, out var rank) ? rank : 0;
        }

        /// <summary>The level a facility project builds next: one above the city's current level of it. 0 for Home Ground.</summary>
        public static int GetNextLevel(WorldProperty city, CityProjectType type)
        {
            var upgrade = CityProjectRules.GetUpgradeType(type);
            return upgrade == PropertyUpgradeType.Invalid ? 0 : city.Upgrades.GetValueOrDefault(upgrade, 1) + 1;
        }

        /// <summary>
        /// Starts the project. Its credits leave the treasury, restricted funds first, for the city's escrow, and the project opens for
        /// donations. The city is read again, so another official's start a moment earlier is seen.
        /// </summary>
        public static CityProjectRefusal Start(string cityId, CityProjectType type, string playerId, out CityProject project)
        {
            project = null;
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            var refusal = CheckStart(city, type, playerId);
            if (refusal != CityProjectRefusal.None)
                return refusal;

            var now = DateTime.UtcNow;
            project = CityProjectRules.Create(city.Id, type, GetNextLevel(city, type), playerId, now);
            if (project == null)
                return CityProjectRefusal.Unknown;

            // Saved first, so a restart between the two saves finds a Starting project and decides from the escrow what happened.
            DB.Set(project);
            if (!CityTreasury.PostEscrow(city, project.Id, project.CreditCost, restrictedFirst: true).Succeeded)
            {
                DB.Delete<CityProject>(project.Id);
                project = null;
                return CityProjectRefusal.CannotAfford;
            }

            project.State = CityProjectState.Open;
            DB.Set(project);

            Log.Write(LogGroup.Property, $"City project '{CityProjectRules.GetName(project)}' ({project.Id}) started for city '{city.CustomName}' ({city.Id}) by player {playerId}; {project.CreditCost} credits held.");
            CityGazette.Post(
                city.Id,
                CityGazetteEntryType.Project,
                CityProjectRules.BuildStartedText(project, GetItemName),
                playerId,
                CityPowers.GetDelegateTitle(city, playerId));
            return CityProjectRefusal.None;
        }

        #endregion

        #region Donating

        /// <summary>How many of the item the player carries in their inventory, stacks included.</summary>
        public static int CountCarried(uint player, string resref)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref)
                    count += GetItemStackSize(item);
            }

            return count;
        }

        /// <summary>
        /// Donates up to <paramref name="count"/> of a material from the player's inventory. The project takes only what it still needs,
        /// so a player offering more keeps the rest. The donation is recorded first and the items are taken after, so a restart never
        /// loses the credit for items that were taken. Anyone may donate, visitors included.
        /// </summary>
        /// <returns>How many items were taken.</returns>
        public static int Donate(uint player, string projectId, string resref, int count)
        {
            var project = GetProject(projectId);
            if (project == null || !GetIsPC(player) || GetIsDM(player))
                return 0;

            var offered = Math.Min(Math.Max(0, count), CountCarried(player, resref));
            var accepted = CityProjectRules.Accept(project, GetObjectUUID(player), Disguise.GetDisplayDescriptor(player), resref, offered, DateTime.UtcNow);
            if (accepted <= 0)
                return 0;

            DB.Set(project);
            TakeItems(player, resref, accepted);

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} donated {accepted} x {resref} to city project '{CityProjectRules.GetName(project)}' ({project.Id}) of city {project.CityId}.");
            if (project.SuppliedUtc.HasValue)
            {
                Log.Write(LogGroup.Property, $"City project '{CityProjectRules.GetName(project)}' ({project.Id}) is fully supplied and completes at {project.CompletesUtc:u}.");
            }

            return accepted;
        }

        private static void TakeItems(uint player, string resref, int count)
        {
            var remaining = count;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item) && remaining > 0; item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) != resref)
                    continue;

                var size = GetItemStackSize(item);
                if (size <= remaining)
                {
                    remaining -= size;
                    DestroyObject(item);
                }
                else
                {
                    SetItemStackSize(item, size - remaining);
                    remaining = 0;
                }
            }
        }

        #endregion

        #region Cancelling

        /// <summary>Whether the player may cancel the city's projects: the power to start them.</summary>
        public static bool CanCancel(WorldProperty city, string playerId)
        {
            return city != null && CityPowers.Can(city, playerId, CityPower.StartProjects);
        }

        /// <summary>
        /// Cancels an open project. Each donor's materials go to their overflow store, and each credit portion goes back to the balance it
        /// came from, so restricted income never becomes credits the mayor can withdraw.
        /// </summary>
        /// <returns>False when the project is not open or the player may not cancel it.</returns>
        public static bool Cancel(string projectId, string playerId)
        {
            var project = GetProject(projectId);
            var city = project == null ? null : DB.Get<WorldProperty>(project.CityId);
            if (project == null || project.State != CityProjectState.Open || !CanCancel(city, playerId))
                return false;

            project.State = CityProjectState.Cancelling;
            DB.Set(project);
            FinishCancel(project, city);

            Log.Write(LogGroup.Property, $"City project '{CityProjectRules.GetName(project)}' ({project.Id}) of city '{city.CustomName}' ({city.Id}) was cancelled by player {playerId}.");
            CityGazette.Post(city.Id, CityGazetteEntryType.Project, CityProjectRules.BuildCancelledText(project), playerId, CityPowers.GetDelegateTitle(city, playerId));
            return true;
        }

        private static void FinishCancel(CityProject project, WorldProperty city)
        {
            ReturnMaterials(project);

            if (city != null && !city.IsQueuedForDeletion)
                CityTreasury.RefundEscrow(city, project.Id);

            DB.Delete<CityProject>(project.Id);
        }

        /// <summary>
        /// Writes each donor's materials to their overflow store. The record ids never change, so repeating this after a restart writes the
        /// same records again.
        /// </summary>
        private static void ReturnMaterials(CityProject project)
        {
            foreach (var refund in CityProjectRules.GetRefunds(project))
            {
                var remaining = refund.Count;
                for (var chunk = 0; remaining > 0; chunk++)
                {
                    var item = CreateObject(ObjectType.Item, refund.Resref, GetStartingLocation());
                    if (!GetIsObjectValid(item))
                    {
                        Log.Write(LogGroup.Error, $"City project {project.Id}: item '{refund.Resref}' could not be created, so {remaining} owed to player {refund.DonorId} are lost.");
                        break;
                    }

                    SetItemStackSize(item, remaining);
                    var size = Math.Max(1, GetItemStackSize(item));
                    DB.Set(new OverflowItem
                    {
                        Id = CityProjectRules.GetOverflowRecordId(project.Id, refund.DonorId, refund.Resref, chunk),
                        PlayerId = refund.DonorId,
                        Name = GetName(item),
                        Resref = refund.Resref,
                        Tag = GetTag(item),
                        Quantity = size,
                        Data = ObjectPlugin.Serialize(item)
                    });
                    DestroyObject(item);
                    remaining -= size;
                }
            }
        }

        /// <summary>
        /// A city is being removed: its open projects are cancelled and each donor's materials go to their overflow store. The credits in
        /// escrow go with the treasury, as the rest of the city's money does.
        /// </summary>
        public static void OnCityDeleted(WorldProperty city)
        {
            if (city == null)
                return;

            foreach (var project in GetProjects(city.Id))
            {
                ReturnMaterials(project);
                DB.Delete<CityProject>(project.Id);
                Log.Write(LogGroup.Property, $"City project '{CityProjectRules.GetName(project)}' ({project.Id}) ended because city '{city.CustomName}' ({city.Id}) was removed; donated materials went to overflow.");
            }
        }

        #endregion

        #region Server start

        /// <summary>
        /// Settles every project at server start: finishes the steps a restart interrupted, then applies each project whose 2 days are up,
        /// with the completion time it always had. A project of a city that no longer exists returns its materials.
        /// </summary>
        public static void ProcessAtServerStart()
        {
            var now = DateTime.UtcNow;
            foreach (var project in Search(new DBQuery<CityProject>()))
            {
                try
                {
                    Process(project, now);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"City project {project.Id} could not be processed at server start: {ex}");
                }
            }
        }

        private static void Process(CityProject project, DateTime now)
        {
            var city = DB.Get<WorldProperty>(project.CityId);
            if (city == null)
            {
                ReturnMaterials(project);
                DB.Delete<CityProject>(project.Id);
                Log.Write(LogGroup.Property, $"City project {project.Id} belonged to a city that no longer exists; its donated materials went to overflow.");
                return;
            }

            // A city being removed cancels its projects when it is deleted.
            if (city.IsQueuedForDeletion)
                return;

            switch (project.State)
            {
                case CityProjectState.Starting:
                    if (!city.Escrow.ContainsKey(project.Id))
                    {
                        DB.Delete<CityProject>(project.Id);
                        return;
                    }

                    project.State = CityProjectState.Open;
                    DB.Set(project);
                    break;
                case CityProjectState.Cancelling:
                    FinishCancel(project, city);
                    return;
                case CityProjectState.Completing:
                    FinishComplete(project, city);
                    return;
            }

            if (CityProjectRules.IsDue(project, now))
            {
                project.State = CityProjectState.Completing;
                DB.Set(project);
                FinishComplete(project, city);
            }
        }

        private static void FinishComplete(CityProject project, WorldProperty city)
        {
            var completedUtc = project.CompletesUtc ?? DateTime.UtcNow;

            // A Wild Space station's drill is settled up to the completion time at the rate it had, so earlier days never earn the 5%.
            if (project.ProjectType == CityProjectType.HomeGround && Edict.IsWildSpaceStation(city))
                StationBuildings.SettleDrills(city, completedUtc);

            // One save of the city pays the escrow out and applies the level, so a restart cannot split them.
            var release = CityTreasuryRules.PayOutEscrow(city, project.Id);
            CityProjectRules.ApplyCompletion(city, project);
            DB.Set(city);

            Log.Write(LogGroup.Property, $"City project '{CityProjectRules.GetName(project)}' ({project.Id}) completed for city '{city.CustomName}' ({city.Id}); {release.Credits} credits paid out of escrow.");
            if (release.Found)
                CityGazette.Post(city.Id, CityGazetteEntryType.Project, CityProjectRules.BuildCompletedText(project));

            DB.Delete<CityProject>(project.Id);
        }

        #endregion
    }
}
