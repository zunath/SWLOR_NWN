using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Hidden signatures and the Probe Launcher (plan section 24). Each space orbit and Small/Medium zone holds a few
    /// hidden signatures at random points. Nothing exists in the area for one until a party's Probe Launcher signal
    /// reaches 100%; then its site spawns (SpaceScanSites). Signatures live in memory and start over on a restart.
    /// </summary>
    public static partial class Space
    {
        private static readonly Dictionary<string, List<ScanSignature>> _signaturesByArea = new();
        private static readonly Dictionary<string, List<DateTime>> _signatureRespawns = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleScanning()
        {
            Scheduler.ScheduleRepeating(ProcessSignatures, TimeSpan.FromSeconds(ScanningTuning.SiteCheckSeconds), TimeSpan.FromSeconds(30));
        }

        /// <summary>The signatures of an area, hidden and open.</summary>
        public static IReadOnlyList<ScanSignature> GetSignatures(string areaResref) =>
            _signaturesByArea.TryGetValue(areaResref ?? string.Empty, out var list) ? list : (IReadOnlyList<ScanSignature>)Array.Empty<ScanSignature>();

        /// <summary>
        /// Places a hidden signature in its area's list outside the spawn cycle (a DM tool or an engine test). It does
        /// not count toward the area's signature count until the next refill.
        /// </summary>
        public static void PlaceSignature(ScanSignature signature)
        {
            if (!_signaturesByArea.TryGetValue(signature.AreaResref, out var list))
                _signaturesByArea[signature.AreaResref] = list = new List<ScanSignature>();

            list.Add(signature);
        }

        /// <summary>Removes a placed signature and whatever its site spawned.</summary>
        public static void RemoveSignature(ScanSignature signature)
        {
            EndSite(signature);
            if (_signaturesByArea.TryGetValue(signature.AreaResref, out var list))
                list.Remove(signature);
        }

        /// <summary>A signature by its ID, or null when it is gone.</summary>
        public static ScanSignature FindSignature(string signatureId)
        {
            if (string.IsNullOrEmpty(signatureId))
                return null;

            return _signaturesByArea.Values.SelectMany(x => x).FirstOrDefault(x => x.Id == signatureId);
        }

        /// <summary>Tops every scan area up to its signature count, ends signatures that are over and checks open sites.</summary>
        private static void ProcessSignatures()
        {
            var now = DateTime.UtcNow;
            foreach (var detail in ScanningTuning.Areas)
            {
                try
                {
                    ProcessScanArea(detail, now);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Scanning failed in {detail.AreaResref}: {ex}");
                }
            }

            ProcessLingeringSiteShips();
        }

        private static void ProcessScanArea(ScanAreaDetail detail, DateTime now)
        {
            if (!_signaturesByArea.TryGetValue(detail.AreaResref, out var signatures))
                _signaturesByArea[detail.AreaResref] = signatures = new List<ScanSignature>();
            if (!_signatureRespawns.TryGetValue(detail.AreaResref, out var respawns))
                _signatureRespawns[detail.AreaResref] = respawns = new List<DateTime>();

            foreach (var signature in signatures.ToList())
            {
                // Rift ends belong to the rift network (SpaceRifts), which places and ends them.
                if (signature.Type == SignatureType.Rift)
                    continue;

                if (signature.IsRevealed)
                    UpdateSite(signature);

                if (!SignatureSiteRules.IsOver(signature, now))
                    continue;

                EndSite(signature);
                signatures.Remove(signature);
                respawns.Add(now.AddSeconds(ScanningTuning.RespawnSeconds));
            }

            // A fresh area (after a restart) fills at once; later gaps wait for their respawn time.
            while (signatures.Count(x => x.Type != SignatureType.Rift) + respawns.Count < detail.Signatures)
                respawns.Add(now);

            var due = respawns.Where(x => now >= x).ToList();
            if (due.Count == 0)
                return;

            var area = Area.GetAreaByResref(detail.AreaResref);
            if (!GetIsObjectValid(area))
                return;

            foreach (var time in due)
            {
                respawns.Remove(time);
                var signature = SpawnSignature(area, detail, now);
                if (signature != null)
                    signatures.Add(signature);
                else
                    respawns.Add(now.AddSeconds(ScanningTuning.RespawnSeconds));
            }
        }

        private static ScanSignature SpawnSignature(uint area, ScanAreaDetail detail, DateTime now)
        {
            var type = SignatureSiteRules.PickType(detail.Tier, max => Random.Next(max));
            if (type == SignatureType.Invalid)
                return null;

            var position = GetPositionFromLocation(Walkmesh.GetRandomLocation(area));
            if (position.X == 0f && position.Y == 0f)
                return null;

            var locked = type == SignatureType.CacheSite && SpaceSlicingRules.RollsLockedCache(Random.Next(100));
            return SignatureSiteRules.Create(Guid.NewGuid().ToString(), detail.AreaResref, detail.Tier, type,
                position.X, position.Y, position.Z, now, locked, detail.WildSpace);
        }

        /// <summary>The player IDs of a player's party (the player included), DMs left out.</summary>
        public static HashSet<string> GetScanPartyIds(uint player)
        {
            var party = new HashSet<string>();
            if (!GetIsObjectValid(player) || !GetIsPC(player))
                return party;

            for (var member = GetFirstFactionMember(player); GetIsObjectValid(member); member = GetNextFactionMember(player))
            {
                if (!GetIsDM(member))
                    party.Add(GetObjectUUID(member));
            }

            party.Add(GetObjectUUID(player));
            return party;
        }

        // Probe Launcher

        /// <summary>Why a Probe Launcher cannot run here, or empty.</summary>
        public static string ProbeLauncherError(uint ship)
        {
            if (!GetIsPC(ship))
                return "Only player ships scan for signatures.";

            return SignatureSiteRules.GetArea(GetResRef(GetArea(ship))) == null
                ? "There is nothing to scan for in this system."
                : string.Empty;
        }

        /// <summary>
        /// One Probe Launcher cycle: adds the cycle's signal for the ship's pilot to every hidden signature in the
        /// area, tells the pilot (and the crew member running it) about each threshold crossed, and opens every site
        /// that reaches 100%.
        /// </summary>
        public static void RunProbeCycle(uint ship, ShipStatus status, ShipModuleStep step)
        {
            if (!GetIsPC(ship))
                return;

            var areaResref = GetResRef(GetArea(ship));
            var signatures = GetSignatures(areaResref).Where(x => !x.Completed).ToList();
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), ship, 1.0f);
            if (signatures.Count == 0)
                return;

            var hull = FindShipDetail(status.ItemTag)?.Hull;
            var hullBonus = hull == null ? 0 : ScanningRules.HullScanBonusPercent(hull.Role, hull.Manufacturer, hull.Tag) +
                                               WildSpaceRules.HullScanBonusPercent(hull.Tag, IsWildSpace(GetArea(ship)));
            var tierEarly = hull != null && ScanningRules.ShowsTierEarly(hull.Tag);
            var signal = ScanningRules.SignalPerCycle(
                step.ScanStrength,
                GetScanOperatorEspionage(ship),
                hullBonus,
                Stat.GetStatAdjustment(ship, StatType.SpaceScanStrengthPercent),
                SpaceCrew.GetDroidSurveyBonusPercent(SpaceCrew.GetFireContext(ship)?.Droid),
                SpaceCrew.GetFireEffectiveness(ship));

            var scannerId = GetObjectUUID(ship);
            var operatorCreature = SpaceCrew.GetCreditedPlayer(ship);
            foreach (var signature in signatures)
            {
                var (before, after) = ScanningRules.AddSignal(signature, scannerId, signal);
                var message = ScanningRules.ThresholdMessage(signature, ScanningRules.CrossedLevel(before, after), tierEarly);
                if (message != null)
                {
                    SendMessageToPC(ship, ColorToken.Cyan(message));
                    if (operatorCreature != ship && GetIsPC(operatorCreature))
                        SendMessageToPC(operatorCreature, ColorToken.Cyan(message));
                }

                if (after >= ScanningTuning.InfoLocatedPercent && !signature.IsRevealed)
                    RevealSite(signature, ship);
            }
        }

        /// <summary>
        /// Whether a ship's Probe Launcher has work where it is: scanning works here and a signature is still short of
        /// 100% for this ship. A droid running Survey Protocol switches the launcher on only then.
        /// </summary>
        public static bool HasSomethingToScan(uint ship)
        {
            if (!string.IsNullOrEmpty(ProbeLauncherError(ship)))
                return false;

            var scannerId = GetObjectUUID(ship);
            return GetSignatures(GetResRef(GetArea(ship)))
                .Any(x => !x.Completed && x.Signal.GetValueOrDefault(scannerId) < x.SignalRequired);
        }

        /// <summary>
        /// The Espionage rank of whoever runs the launcher right now: the crew member at the Sensors seat, otherwise
        /// the pilot. A droid has no Espionage.
        /// </summary>
        private static int GetScanOperatorEspionage(uint ship)
        {
            var source = SpaceCrew.GetStatSource(ship);
            return GetIsObjectValid(source) && GetIsPC(source) ? GetPlayerSkillRank(source, SkillType.Espionage) : 0;
        }

        /// <summary>The Scanner tab's rows for a player: the signatures of their area their party has signal on.</summary>
        public static List<ScannerRow> GetScannerRows(uint player)
        {
            var area = GetArea(player);
            var status = GetShipStatus(player);
            var hull = status == null ? null : FindShipDetail(status.ItemTag)?.Hull;
            var width = GetAreaSize(Dimension.Width, area) * 10f;
            var height = GetAreaSize(Dimension.Height, area) * 10f;
            return ScanningRules.BuildRows(
                GetSignatures(GetResRef(area)),
                GetScanPartyIds(player),
                hull != null && ScanningRules.ShowsTierEarly(hull.Tag),
                width,
                height);
        }

        /// <summary>Whether a player's ship has a Probe Launcher fitted, and the best one's step.</summary>
        public static ShipModuleStep GetBestProbeLauncher(ShipStatus status)
        {
            if (status == null)
                return null;

            return status.HighPowerModules.Values
                .Concat(status.MidPowerModules.Values)
                .Concat(status.LowPowerModules.Values)
                .Select(x => FindShipModuleDetail(x.ItemTag))
                .Where(x => x?.Type == ShipModuleType.ProbeLauncher && x.Step != null)
                .Select(x => x.Step)
                .OrderByDescending(x => x.Rank)
                .FirstOrDefault();
        }
    }
}
