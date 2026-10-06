using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        private static readonly Dictionary<uint, List<string>> _sitePools = new();
        private static readonly Dictionary<string, uint> _siteObjects = new();
        private static readonly HashSet<string> _hazardWarnings = new();
        private static readonly HashSet<string> _ventWarnings = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadIndustrySites()
        {
            var catalog = SpaceIndustryCatalog.Default;
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var table = GetLocalString(area, "RESOURCE_SPAWN_TABLE_ID");
                if (!SpaceIndustryCatalog.IsRegionTable(table)) continue;
                var starter = table.EndsWith("VISCARA_ORBIT", StringComparison.Ordinal) || table.EndsWith("MONCALA_ORBIT", StringComparison.Ordinal);
                var indices = starter ? Enumerable.Repeat(0, 12).Concat(Enumerable.Repeat(1, 6)).ToArray() :
                    Enumerable.Repeat(2, 8).Concat(Enumerable.Repeat(3, 6)).Concat(Enumerable.Repeat(4, 6)).Concat(Enumerable.Repeat(5, 2)).ToArray();
                var ids = new List<string>();
                for (var slot = 0; slot < indices.Length; slot++)
                {
                    var hash = SHA256.HashData(Encoding.UTF8.GetBytes("space-site/" + GetResRef(area) + "/" + slot));
                    var id = "space-site-" + Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
                    var site = DB.Get<SpaceSite>(id);
                    if (site == null)
                    {
                        site = SpaceWorkClaims.NewDeposit(id, GetResRef(area), slot, catalog.Deposits[indices[slot]], DateTime.UtcNow);
                        var position = GetPositionFromLocation(Walkmesh.GetRandomLocation(area));
                        site.X = position.X; site.Y = position.Y; site.Z = position.Z;
                        DB.Set(site);
                    }
                    // A reserved pre-restart channel has no live operator. Return it once, retaining completed settlement journals.
                    foreach (var claim in site.Claims.Values.Where(x => x.State == SpaceWorkState.Reserved).Select(x => x.Id).ToArray()) SpaceWorkClaims.Cancel(site, claim);
                    DB.Set(site);
                    ids.Add(id);
                }
                _sitePools[area] = ids;
            }
            foreach (var site in FindSpaceSites().Where(x => x.Kind == SpaceSiteKind.Wreck).ToArray())
            {
                for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
                {
                    if (GetResRef(area) != site.AreaResref) continue;
                    if (!_sitePools.TryGetValue(area, out var ids)) _sitePools[area] = ids = new();
                    if (!ids.Contains(site.Id)) ids.Add(site.Id);
                    foreach (var claim in site.Claims.Values.Where(x => x.State == SpaceWorkState.Reserved).Select(x => x.Id).ToArray()) SpaceWorkClaims.Cancel(site, claim);
                    DB.Set(site); break;
                }
            }
            Scheduler.ScheduleRepeating(ProcessIndustrySites, TimeSpan.FromSeconds(1));
        }

        private static IEnumerable<SpaceSite> FindSpaceSites()
        {
            // DB queries default to 50 rows; every finite claim must count toward capacity and recovery.
            for (var offset = 0; ; offset += 200)
            {
                var page = DB.Search(new DBQuery<SpaceSite>().OrderBy(nameof(SpaceSite.Id)).AddPaging(200, offset)).ToArray();
                foreach (var site in page) yield return site;
                if (page.Length < 200) yield break;
            }
        }

        private static SpaceDepositProfile GetSiteProfile(SpaceSite site) => site.Kind == SpaceSiteKind.Wreck ?
            new() { Name = site.Profile, Ships = site.MaximumShips, ScanSeconds = 10, Lifetime = 600 } :
            SpaceIndustryCatalog.Default.Deposits.Single(x => x.Name == site.Profile);

        private static void CreateEncounterWreck(SpaceEncounter encounter, SpaceEncounterProfile profile)
        {
            if (encounter.WreckCreated) return;
            var siteId = encounter.Id + "/wreck";
            var site = DB.Get<SpaceSite>(siteId);
            if (site == null && DateTime.UtcNow < encounter.CompletedAt.AddSeconds(600))
            {
                site = SpaceWorkClaims.NewWreck(siteId, encounter.AreaResref, profile, encounter.Contributions.Participants.Keys, encounter.CompletedAt);
                site.X = encounter.X; site.Y = encounter.Y; site.Z = encounter.Z;
                DB.Set(site);
            }
            if (site != null)
            {
                for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
                {
                    if (GetResRef(area) != encounter.AreaResref) continue;
                    if (!_sitePools.TryGetValue(area, out var ids)) _sitePools[area] = ids = new();
                    if (!ids.Contains(siteId)) ids.Add(siteId);
                    break;
                }
            }
            encounter.WreckCreated = true; DB.Set(encounter);
        }

        public static SpaceSite GetIndustrySite(uint target)
        {
            if (!GetIsObjectValid(target)) return null;
            var id = GetLocalString(target, "SPACE_SITE_ID");
            return string.IsNullOrEmpty(id) ? null : DB.Get<SpaceSite>(id);
        }

        private static void ProcessIndustrySites()
        {
            var now = DateTime.UtcNow;
            foreach (var (area, ids) in _sitePools.ToArray())
            {
                if (!GetIsObjectValid(area)) continue;
                var active = _playersInSpace.Any(x => GetIsObjectValid(x) && GetArea(x) == area);
                foreach (var id in ids.ToArray())
                {
                    var site = DB.Get<SpaceSite>(id);
                    if (site == null) continue;
                    SettleCompletedClaims(site);
                    var profile = GetSiteProfile(site);
                    var empty = site.Reserves.Values.Sum() <= 1e-9 && !site.Claims.Values.Any(x => x.State == SpaceWorkState.Reserved);
                    if ((site.Kind == SpaceSiteKind.Wreck || site.ActivityId != null) && (empty || now >= site.ExpiresAt))
                    {
                        foreach (var claim in site.Claims.Values.Where(x => x.State == SpaceWorkState.Reserved).Select(x => x.Id).ToArray()) SpaceWorkClaims.Cancel(site, claim);
                        if (_siteObjects.Remove(id, out var expired) && GetIsObjectValid(expired)) { _spaceObjectInstances.Remove(expired); DestroyObject(expired); }
                        if (site.Claims.Count == 0) { DB.Delete<SpaceSite>(id); ids.Remove(id); }
                        else DB.Set(site);
                        continue;
                    }
                    if (empty && now < site.ExpiresAt)
                    { site.ExpiresAt = now; site.RespawnsAt = now.AddSeconds(profile.Respawn); DB.Set(site); }
                    if (now >= site.ExpiresAt)
                    {
                        if (_siteObjects.Remove(id, out var old) && GetIsObjectValid(old)) { _spaceObjectInstances.Remove(old); DestroyObject(old); }
                        if (now < site.RespawnsAt || !active) continue;
                        foreach (var claim in site.Claims.Values.Where(x => x.State == SpaceWorkState.Reserved).Select(x => x.Id).ToArray()) SpaceWorkClaims.Cancel(site, claim);
                        _hazardWarnings.RemoveWhere(x => x.StartsWith(site.Generation + "/", StringComparison.Ordinal));
                        _ventWarnings.RemoveWhere(x => x.StartsWith(site.Generation + "/", StringComparison.Ordinal));
                        var next = SpaceWorkClaims.NewDeposit(id, site.AreaResref, site.Slot, profile, now);
                        next.Claims = site.Claims;
                        var position = GetPositionFromLocation(Walkmesh.GetRandomLocation(area));
                        next.X = position.X; next.Y = position.Y; next.Z = position.Z;
                        site = next; DB.Set(site);
                    }
                    if (!_siteObjects.TryGetValue(id, out var obj) || !GetIsObjectValid(obj))
                    {
                        if (!active) continue;
                        obj = CreateObject(ObjectType.Placeable, site.Blueprint, Location(area, new Vector3((float)site.X, (float)site.Y, (float)site.Z), 0));
                        if (!GetIsObjectValid(obj)) continue;
                        SetLocalString(obj, "SPACE_SITE_ID", id);
                        SetName(obj, profile.Name);
                        _siteObjects[id] = obj;
                    }
                    foreach (var claim in site.Claims.Values.Where(x => x.State == SpaceWorkState.Reserved).ToArray())
                    {
                        var player = GetObjectByUUID(claim.PlayerId);
                        var ship = DB.Get<PlayerShip>(claim.ShipId);
                        if (!GetIsObjectValid(player) || !IsPlayerInSpaceMode(player) || ship?.Status.FlightId != claim.FlightId ||
                            !ship.Status.PaidWorkClaims.Contains(claim.Id) || GetArea(player) != area || GetDistanceBetween(player, obj) > claim.Range || ship.Status.Hull <= 0 ||
                            !ShipFittedStats.Modules(ship.Status).Any(x => x.ItemInstanceId == claim.ModuleId && x.Condition > 0))
                        { SpaceWorkClaims.Cancel(site, claim.Id); DB.Set(site);
                            if (ship!=null) { ship.Status.TemporaryAdjustments.RemoveAll(x=>x.Family==claim.Id);DB.Set(ship); }
                            if (GetIsObjectValid(player)) Stat.ApplyCreatureMovementRate(player);
                            continue; }
                        if (now < claim.CompletesAt) continue;
                        var otherReserved = ReservedSiteCargo(claim.ShipId, claim.Id);
                        if (claim.ReservedCargo > ShipCargo.Available(ship.Status) - otherReserved + 1e-9)
                        { SpaceWorkClaims.Cancel(site, claim.Id); DB.Set(site); continue; }
                        if (SpaceWorkClaims.Complete(site, claim.Id, now, Random.D100(1) / 100.0 - .005, Random.D100(1) / 100.0 - .005))
                        { DB.Set(site); SettleCompletedClaims(site); }
                        else DB.Set(site);
                    }
                    if (site.VentsUntil > now && _ventWarnings.Add(site.Generation + "/" + site.VentsUntil.Ticks))
                    {
                        Messaging.SendMessageNearbyToPlayers(obj, _ => "The deposit is venting! Move beyond 15m before the discharge.", 30f);
                        var generation = site.Generation;
                        DelayCommand(3f, () => ApplySiteHazard(id, generation, 15, obj));
                    }
                    if (site.VentsUntil != default && now >= site.VentsUntil)
                    { site.Stability = Math.Min(100, site.Stability + 20); site.VentsUntil = default; DB.Set(site); }
                    if (profile.HazardSeconds > 0 && now >= site.NextHazardAt.AddSeconds(-3) && _hazardWarnings.Add(site.Generation + "/" + site.NextHazardAt.Ticks))
                    {
                        Messaging.SendMessageNearbyToPlayers(obj, _ => "A hazardous deposit discharge is imminent. Move beyond 15m.", 30f);
                        var generation = site.Generation;
                        DelayCommand(Math.Max(0, (float)(site.NextHazardAt - now).TotalSeconds), () => ApplySiteHazard(id, generation, profile.HazardDamage, obj));
                        site.NextHazardAt = site.NextHazardAt.AddSeconds(profile.HazardSeconds); DB.Set(site);
                    }
                }
            }
        }

        private static void ApplySiteHazard(string id, string generation, int damage, uint obj)
        {
            var site = DB.Get<SpaceSite>(id);
            if (site?.Generation != generation || !GetIsObjectValid(obj)) return;
            foreach (var player in OperatingShipsInArea(obj).Where(x => GetDistanceBetween(x, obj) <= 15).ToArray())
            {
                var status = GetShipStatus(player);
                var effects = ShipTemporaryStats.Current(status, DateTime.UtcNow);
                var mitigation = Math.Clamp(ShipFittedStats.Bonus(status, StatType.ShipEnvironmentalMitigation) + effects.GetValueOrDefault(StatType.ShipEnvironmentalMitigation), 0, .6);
                ShipResources.SpendPrecise(status, ShipResource.Hull, damage * (1 - mitigation));
                PersistShipStatus(player, status);
                ExecuteScript("pc_hull_adjusted", player);
                if (status.Hull <= 0)
                {
                    if (GetIsPC(player)) RescueFittedShipPilot(player);
                    else AssignCommand(obj, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), player));
                }
            }
        }

        private static double ReservedSiteCargo(string shipId, string exclude = null) => FindSpaceSites().SelectMany(x => x.Claims.Values)
            .Where(x => x.ShipId == shipId && x.Id != exclude && !x.CargoSettled).Sum(x => x.ReservedCargo);

        private static bool StartIndustrialOperation(uint player, uint target, ShipStatus.ShipStatusModule fitted, ShipModuleOperation operation, DateTime now)
        {
            if (!GetIsPC(player)) return false;
            var shipId = DB.Get<Player>(GetObjectUUID(player)).ActiveShipId;
            var ship = DB.Get<PlayerShip>(shipId);
            if (operation.Profile.Action == ShipModuleAction.Compression)
            {
                if (ship.Status.Cargo.Values.Where(x => !x.Compressed && x.ContractId == null && SpaceIndustryCatalog.Default.Commodities.ContainsKey(x.Resref)).Sum(x => x.Quantity) < 20)
                { SendMessageToPC(player, "Load at least 20 unpacked ore units before compressing."); return false; }
                ShipModuleActivationPolicy.Pay(ship.Status, fitted, operation, now);
                var remaining = 20.0;
                foreach (var key in ship.Status.Cargo.Where(x => !x.Value.Compressed && x.Value.ContractId == null && SpaceIndustryCatalog.Default.Commodities.ContainsKey(x.Value.Resref)).Select(x => x.Key).OrderBy(x => x).ToArray())
                {
                    var lot = ship.Status.Cargo[key]; var quantity = Math.Min(remaining, lot.Quantity);
                    if (quantity <= 0) break;
                    if (quantity == lot.Quantity) ship.Status.Cargo[key] = lot with { Compressed = true };
                    else
                    {
                        ship.Status.Cargo[key] = lot with { Quantity = lot.Quantity - quantity };
                        ship.Status.Cargo.Add(Guid.NewGuid().ToString(), lot with { Quantity = quantity, Compressed = true });
                    }
                    remaining -= quantity;
                }
                DB.Set(ship); ExecuteScript("pc_target_upd", player); return true;
            }
            var site = GetIndustrySite(target);
            var targetId=GetObjectUUID(target);
            var temporary = operation.Temporary ?? ShipTemporaryStats.Current(ship.Status, now, fitted.ItemInstanceId, targetId);
            var selected=ship.Status.TemporaryAdjustments.Where(x=>x.Stat==StatType.ShipSelectedRecovery && x.ExpiresAt>now && x.Matches(fitted.ItemInstanceId,targetId)).OrderByDescending(x=>x.Amount).Select(x=>x.SelectedConstituent).FirstOrDefault();
            var bonuses = ShipFittedStats.StatUnits.Keys.ToDictionary(x => x, x => ShipFittedStats.Bonus(ship.Status, x) - ShipFittedStats.Penalty(ship.Status, x));
            SpaceWorkClaim claim;
            try
            {
                claim = SpaceWorkClaims.Reserve(site, GetObjectUUID(player), shipId, ship.Status.FlightId, fitted.ItemInstanceId, operation,
                    Math.Max(0, ShipCargo.Available(ship.Status) - ReservedSiteCargo(shipId)), now, bonuses, temporary,
                    temporary.GetValueOrDefault(StatType.ShipCommittedCycleSeconds)>0 ? temporary[StatType.ShipCommittedCycleSeconds] :
                    operation.Profile.Action == ShipModuleAction.Survey && site != null ? Math.Max(operation.Variant.Cycle, GetSiteProfile(site).ScanSeconds) : null, selected, ContractRecoveryRemaining(site));
            }
            catch (InvalidOperationException ex) { SendMessageToPC(player, ex.Message); return false; }
            claim.ActivityId = site.ActivityId;
            ShipModuleActivationPolicy.Pay(ship.Status, fitted, operation, now);
            ShipTemporaryStats.ConsumePaidOperation(ship.Status,fitted.ItemInstanceId,targetId,now);
            if (operation.Profile.MovementLock || temporary.GetValueOrDefault(StatType.ShipCommittedMovementLock)>0) ShipTemporaryStats.Add(ship.Status, StatType.ShipMovementLock, 1, (claim.CompletesAt - now).TotalSeconds, claim.Id, now);
            else if (operation.Profile.WorkingSpeedPenalty > 0) ShipTemporaryStats.Add(ship.Status, StatType.ShipSpeed, -operation.Profile.WorkingSpeedPenalty, (claim.CompletesAt - now).TotalSeconds, claim.Id, now);
            // The finite reserve is durable before accepting another tool's claim; unpaid orphan reservations are released on restart.
            DB.Set(site);
            ship.Status.PaidWorkClaims.Add(claim.Id);
            DB.Set(ship);
            ApplyShipMovementConstraints(player, ship.Status);
            Stat.ApplyCreatureMovementRate(player);
            SendMessageToPC(player, $"{operation.Profile.Name} working: {(claim.CompletesAt - now).TotalSeconds:0.#}s.");
            return true;
        }

        private static void SettleCompletedClaims(SpaceSite site)
        {
            var changed = false;
            var acknowledged = new List<SpaceWorkClaim>();
            foreach (var claim in site.Claims.Values.Where(x => x.State == SpaceWorkState.Completed).ToArray())
            {
                var ship = DB.Get<PlayerShip>(claim.ShipId);
                if (ship == null) continue;
                if (!claim.CargoSettled)
                {
                    SpaceWorkClaims.SettleCargo(ship.Status, claim); DB.Set(ship);
                    claim.CargoSettled = true; changed = true;
                }
                var player = GetObjectByUUID(claim.PlayerId);
                if (!claim.XPSettled && GetIsObjectValid(player) && GetIsPC(player) && !GetIsDead(player))
                {
                    var contractWork=CreditContractWork(site,claim);
                    if(contractWork)
                    {var record=DB.Get<Player>(claim.PlayerId);record.SpaceExperience.Touch(claim.StartedAt,claim.CompletesAt);DB.Set(record);}
                    if (claim.BaseXP > 0 && !contractWork)
                        Skill.GiveSkillXP(player, claim.Action == ShipModuleAction.Survey ? SkillType.Astrometrics : SkillType.SpaceIndustry,
                            claim.BaseXP, applyHenchmanPenalty: false, receiptId: "space-work/" + claim.Id,
                            settleBaseXP: (record, requested) => record.SpaceExperience.Claim(claim.FlightId, claim.Action, requested, claim.StartedAt, claim.CompletesAt));
                    if (claim.BaseXP > 0 && !contractWork && !DB.Get<Player>(claim.PlayerId).SkillXPReceipts.ContainsKey("space-work/" + claim.Id)) continue;
                    claim.XPSettled = true; changed = true;
                    if (claim.Action == ShipModuleAction.Survey)
                        SendMessageToPC(player, $"{site.Profile}: {site.Reserves.Values.Sum():0.#} reserve, hardness {site.Hardness}, stability {site.Stability}. " +
                            string.Join(", ", site.Reserves.Select(x => $"{x.Key.Replace("ore_", "")}: {x.Value:0.#}")));
                    else SendMessageToPC(player, "Recovered cargo: " + (claim.Cargo.Count == 0 ? "no intact component" : string.Join(", ", claim.Cargo.Select(x => $"{x.Value:0.##} {x.Key}"))));
                }
                if (claim.CargoSettled && claim.XPSettled) { site.Claims.Remove(claim.Id); acknowledged.Add(claim); changed = true; }
            }
            if (changed) DB.Set(site);
            // Forget ownership guards only after the site durably acknowledges settlement.
            foreach (var claim in acknowledged)
            {
                var ship = DB.Get<PlayerShip>(claim.ShipId);
                if (ship == null) continue;
                ship.Status.PaidWorkClaims.Remove(claim.Id);
                ship.Status.SettledSiteClaims.Remove(claim.Id);
                DB.Set(ship);
                var player=DB.Get<Player>(claim.PlayerId);
                if(player!=null&&player.SkillXPReceipts.Remove("space-work/"+claim.Id))DB.Set(player);
            }
        }
    }
}
