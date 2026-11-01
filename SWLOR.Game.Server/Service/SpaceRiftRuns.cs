using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Rift Runs (plan section 31). Using a Rift Key in space opens a private pocket, an instance of the
    /// <see cref="RiftRunTuning.TemplateResref"/> area, for the user's ship and up to two party ships nearby. Its three
    /// chambers' waves come one at a time; clearing a chamber pulls the party into the next. Clearing the last within the
    /// 15-minute timer pays by the key's tier and the time left and returns the party where it opened the pocket. When the
    /// timer runs out the pocket collapses and destroys every ship in it, so everyone aboard ejects in escape pods with
    /// the normal XP debt (SpaceDeath). Runs live in memory: a restart empties the pockets and the ships go back to their
    /// docks at boot.
    /// </summary>
    public static partial class Space
    {
        private static readonly Dictionary<string, RiftRun> _riftRuns = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleRiftRuns()
        {
            Scheduler.ScheduleRepeating(ProcessRiftRuns, TimeSpan.FromSeconds(1));
        }

        /// <summary>The run a ship is in: the pilot's own, or the one of the ship whose station a crew member holds.</summary>
        public static RiftRun FindRiftRun(uint player)
        {
            var ship = GetFiringShip(player);
            return _riftRuns.Values.FirstOrDefault(x => x.Phase != RiftRunPhase.Closing && x.Pilots.Contains(ship));
        }

        /// <summary>Why a Rift Key cannot open a pocket for this user now, or empty.</summary>
        public static string GetRiftKeyRefusal(uint user, uint item) =>
            GetRiftOpenRefusal(user, RiftRunRules.FindKey(GetResRef(item)));

        /// <summary>Why a pocket of this key's tier cannot open for this user now, or empty.</summary>
        private static string GetRiftOpenRefusal(uint user, RiftKeyDetail key)
        {
            var entry = GetRiftEntryRefusal(user);
            return string.IsNullOrEmpty(entry)
                ? RiftRunRules.Message(RiftRunRules.GetOpenRefusal(key, _riftRuns.Count))
                : entry;
        }

        /// <summary>
        /// Why a ship cannot go into a pocket now, or empty. The key user and every party ship entering with them pass
        /// these checks, so a party member never pulls a ship out of an Interdictor's hold or a fight.
        /// </summary>
        private static string GetRiftEntryRefusal(uint pilot)
        {
            // An Interdictor's hold keeps a ship from opening or entering a pocket (W11k).
            var hold = GetInterdictionRefusal(pilot);
            if (!string.IsNullOrEmpty(hold))
                return hold;

            var refusal = RiftRunRules.GetEntryRefusal(
                IsPlayerInSpaceMode(pilot),
                GetCloakPhase(pilot) != ShipCloakPhase.Off,
                Enmity.HasEnmity(pilot),
                FindRiftRun(pilot) != null || IsRiftPocket(GetArea(pilot)));
            return RiftRunRules.Message(refusal);
        }

        /// <summary>Whether an area is a Rift Run pocket.</summary>
        public static bool IsRiftPocket(uint area) => GetIsObjectValid(area) && RiftRunRules.IsPocketTemplate(GetResRef(area));

        /// <summary>Opens a pocket for the key user and the party ships nearby, and spends the key.</summary>
        public static void StartRiftRun(uint user, uint item)
        {
            var refusal = GetRiftKeyRefusal(user, item);
            if (!string.IsNullOrEmpty(refusal))
            {
                SendMessageToPC(user, ColorToken.Red(refusal));
                return;
            }

            OpenRiftRun(user, RiftRunRules.FindKey(GetResRef(item)), item);
        }

        /// <summary>
        /// Opens a pocket of <paramref name="key"/>'s tier for the user and the party ships nearby. The key item is spent
        /// when the pocket opens; a DM opening one passes <see cref="OBJECT_INVALID"/> and no key is spent.
        /// </summary>
        private static bool OpenRiftRun(uint user, RiftKeyDetail key, uint item)
        {
            var nearby = new List<(uint Ship, float Distance)>();
            for (var member = GetFirstFactionMember(user); GetIsObjectValid(member); member = GetNextFactionMember(user))
            {
                if (member == user || GetIsDM(member) || !IsPlayerInSpaceMode(member) || GetArea(member) != GetArea(user) ||
                    FindRiftRun(member) != null)
                {
                    continue;
                }

                nearby.Add((member, GetDistanceBetween(member, user)));
            }

            var (pilots, leftOut) = RiftRunRules.PickParty(user, nearby, GetRiftEntryRefusal);
            var area = Area.CreateInstance(key.Template, $"rift_pocket_{Guid.NewGuid():N}", $"Rift Pocket (Tier {key.TierLabel})");
            if (!GetIsObjectValid(area))
            {
                SendMessageToPC(user, ColorToken.Red("The rift flickers and will not open. Your key is unspent."));
                Log.Write(LogGroup.Error, $"Rift Run: could not create an instance of {key.Template}.");
                return false;
            }

            if (GetIsObjectValid(item))
                Item.ReduceItemStack(item, 1);

            // A party ship that fails an entry check stays where it is, and its pilot hears why.
            foreach (var (pilot, refusal) in leftOut)
                SendMessageToPC(pilot, ColorToken.Red(refusal));

            var now = DateTime.UtcNow;
            var run = new RiftRun
            {
                Key = key,
                Ships = pilots.Count,
                StartedAt = now,
                NextAt = now.AddSeconds(RiftRunTuning.WaveDelaySeconds),
                Area = area,
                Pilots = pilots,
            };
            _riftRuns[run.Id] = run;

            for (var index = 0; index < pilots.Count; index++)
            {
                var pilot = pilots[index];
                run.Origins[pilot] = GetLocation(pilot);
                if (TryGetFlownShip(pilot, out var flown))
                {
                    run.ShipIds[pilot] = flown.Id;
                    run.PilotIds[flown.Id] = GetObjectUUID(pilot);
                }
                JumpIntoRiftRoom(run, pilot, index);
                foreach (var player in GetShipCrewPlayers(pilot))
                {
                    SendMessageToPC(player, ColorToken.Cyan(
                        $"{(GetIsObjectValid(item) ? GetName(user) : "A DM")} opens a tier {key.TierLabel} rift pocket. Clear its three chambers within " +
                        $"{RiftRunTuning.TimerSeconds / 60} minutes, or it collapses with you inside."));
                }
            }

            Log.Write(LogGroup.Space, $"RIFT RUN STARTED - {PlayerName.GetAuditName(user)} - {key.Resref}, {pilots.Count} ship(s): " +
                                      string.Join(", ", pilots.Select(PlayerName.GetAuditName)));
            return true;
        }

        /// <summary>
        /// A pilot logging out of a run, or docking their ship aboard another ship: their ship waits in the run for them
        /// to rejoin (W11k). This runs at logout because the run's next check can still see the leaving pilot's creature
        /// after its ship is unregistered, and would then drop the pilot without keeping their ship (W11m live check).
        /// </summary>
        private static void MarkRiftRunPilotAway(uint pilot)
        {
            foreach (var run in _riftRuns.Values.Where(x => x.Pilots.Contains(pilot)))
                RiftRunRules.MarkAway(run, pilot);
        }

        /// <summary>A run's waiting ship that left the pocket aboard its host and undocked elsewhere is out of the run.</summary>
        private static void ForgetRiftRunShip(string shipId)
        {
            foreach (var run in _riftRuns.Values)
                RiftRunRules.Forget(run, shipId);
        }

        /// <summary>The open run a logged-out pilot's ship waits in, or null (W11k).</summary>
        private static RiftRun FindRejoinableRiftRun(string shipId) =>
            _riftRuns.Values.FirstOrDefault(x => RiftRunRules.CanRejoin(x, shipId) && GetIsObjectValid(x.Area));

        /// <summary>
        /// Where a pilot taking the controls of a ship waiting in an open Rift Run launches: the run's current chamber.
        /// Null when the ship is in no open run, so the launch goes where the ship is as usual.
        /// </summary>
        private static Location GetRiftRunRejoinLocation(string shipId)
        {
            var run = FindRejoinableRiftRun(shipId);
            if (run == null)
                return null;

            var (dx, dy) = RiftRunRules.ArrivalOffset(run.Pilots.Count);
            return RiftRoomLocation(run, run.Room, dx, dy);
        }

        /// <summary>A pilot launched back into the pocket their ship waits in: they are one of the run's ships again.</summary>
        private static void RejoinRiftRun(uint pilot, string shipId)
        {
            var run = FindRejoinableRiftRun(shipId);
            if (run == null || GetArea(pilot) != run.Area || !RiftRunRules.Rejoin(run, pilot, shipId))
                return;

            run.PilotIds[shipId] = GetObjectUUID(pilot);

            var left = RiftRunRules.FormatTimer(RiftRunRules.SecondsLeft(run.StartedAt, DateTime.UtcNow));
            SendMessageToPC(pilot, ColorToken.Cyan(
                $"You rejoin the Rift Run in chamber {run.Room + 1} of {RiftRunTuning.Rooms.Count}, the {RiftRunTuning.Rooms[run.Room].Name}: {left} left."));
            Log.Write(LogGroup.Space, $"RIFT RUN REJOINED - {PlayerName.GetAuditName(pilot)} - {run.Key.Resref}, chamber {run.Room + 1}");
        }

        private static Location RiftRoomLocation(RiftRun run, int room, float dx = 0f, float dy = 0f)
        {
            var waypoint = GetNearestObjectByTag($"{RiftRunTuning.RoomWaypointPrefix}{room + 1}", GetFirstObjectInArea(run.Area));
            var (x, y, z) = RiftRunRules.RoomPoint(run.Key.Template, room);
            var position = GetIsObjectValid(waypoint) && GetArea(waypoint) == run.Area ? GetPosition(waypoint) : Vector3(x, y, z);
            return Location(run.Area, Vector3(position.X + dx, position.Y + dy, position.Z), 0f);
        }

        private static void JumpIntoRiftRoom(RiftRun run, uint pilot, int index)
        {
            var (dx, dy) = RiftRunRules.ArrivalOffset(index);
            var location = RiftRoomLocation(run, run.Room, dx, dy);
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Unsummon), GetLocation(pilot));
            AssignCommand(pilot, () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });
            // Runs from the run check (a scheduler task), where DelayCommand never runs.
            Scheduler.Schedule(() =>
            {
                if (GetIsObjectValid(pilot))
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Magblue), pilot);
            }, TimeSpan.FromSeconds(0.5));
        }

        private static void ProcessRiftRuns()
        {
            foreach (var run in _riftRuns.Values.ToList())
            {
                try
                {
                    ProcessRiftRun(run);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Rift Run {run.Id} failed: {ex}");
                }
            }
        }

        private static void ProcessRiftRun(RiftRun run)
        {
            var now = DateTime.UtcNow;
            if (run.Phase == RiftRunPhase.Closing)
            {
                CloseRiftPocket(run);
                return;
            }

            // Ships that died (and left in pods), logged out or were moved out of the pocket are out of the run. A pilot
            // who logged out can rejoin while the run is open (W11k): their ship waits in the pocket.
            if ((now - run.StartedAt).TotalSeconds >= RiftRunRules.ArrivalGraceSeconds)
            {
                foreach (var pilot in run.Pilots.Where(x => !GetIsObjectValid(x)).ToList())
                    RiftRunRules.MarkAway(run, pilot);
                run.Pilots.RemoveAll(x => RiftRunRules.HasLeftPocket(
                    GetIsObjectValid(x), IsPlayerInSpaceMode(x), !GetIsObjectValid(GetArea(x)), GetArea(x) == run.Area));
            }

            run.Enemies.RemoveAll(x => !GetIsObjectValid(x) || GetIsDead(x));
            var secondsLeft = RiftRunRules.SecondsLeft(run.StartedAt, now);
            if (run.Phase != RiftRunPhase.Returning)
            {
                foreach (var warning in RiftRunRules.DueWarnings(run, secondsLeft))
                    MessageRiftRun(run, ColorToken.Orange($"The rift pocket is failing: {RiftRunRules.FormatTimer(warning)} left."));
            }

            switch (RiftRunRules.Step(run, run.Enemies.Count, run.Pilots.Count, now))
            {
                case RiftRunAction.SpawnWave:
                    SpawnRiftWave(run);
                    break;
                case RiftRunAction.RoomCleared:
                    MessageRiftRun(run, ColorToken.Cyan($"The {RiftRunTuning.Rooms[run.Room].Name} is clear. The rift pulls you deeper..."));
                    break;
                case RiftRunAction.PullToNextRoom:
                    for (var index = 0; index < run.Pilots.Count; index++)
                        JumpIntoRiftRoom(run, run.Pilots[index], index);
                    MessageRiftRun(run, ColorToken.Cyan($"Chamber {run.Room + 1} of {RiftRunTuning.Rooms.Count}: the {RiftRunTuning.Rooms[run.Room].Name}."));
                    break;
                case RiftRunAction.Complete:
                    PayRiftRun(run);
                    break;
                case RiftRunAction.ReturnHome:
                    foreach (var pilot in run.Pilots.ToList())
                        ReturnFromRiftPocket(run, pilot);
                    break;
                case RiftRunAction.Collapse:
                    CollapseRiftPocket(run);
                    break;
                case RiftRunAction.Abandon:
                    Log.Write(LogGroup.Space, $"RIFT RUN ABANDONED - {run.Key.Resref}, no ships left in the pocket");
                    break;
            }
        }

        private static void SpawnRiftWave(RiftRun run)
        {
            var tags = RiftRunRules.WaveTags(run.Key, run.Room, run.Wave, run.Ships);
            for (var index = 0; index < tags.Count; index++)
            {
                var (dx, dy) = RiftRunRules.EnemyOffset(index, tags.Count);
                var ship = CreateObject(ObjectType.Creature, tags[index], RiftRoomLocation(run, run.Room, dx, dy));
                if (GetIsObjectValid(ship))
                    run.Enemies.Add(ship);
            }

            if (run.Enemies.Count > 0)
            {
                run.FailedSpawns = 0;
                return;
            }

            // An empty wave would count as cleared: the wave is tried again shortly, and after a few failures the run
            // ends with the ships sent home, unpaid but unharmed.
            Log.Write(LogGroup.Error, $"Rift Run {run.Id}: no ship of wave {run.Wave} in chamber {run.Room} could be created ({string.Join(", ", tags)}).");
            run.FailedSpawns++;
            if (run.FailedSpawns < MaxFailedSpawns)
            {
                run.Phase = RiftRunPhase.Waiting;
                run.NextAt = DateTime.UtcNow.AddSeconds(RiftRunTuning.WaveDelaySeconds);
                return;
            }

            run.Phase = RiftRunPhase.Returning;
            run.NextAt = DateTime.UtcNow.AddSeconds(RiftRunTuning.ReturnDelaySeconds);
            MessageRiftRun(run, ColorToken.Red("The rift pocket destabilizes before its defenders can form. The run is over, and the rift returns you home."));
            Log.Write(LogGroup.Space, $"RIFT RUN ENDED - {run.Key.Resref}, chamber {run.Room + 1}: waves could not be created");
        }

        /// <summary>Waves in a row that may fail to spawn before a run is ended.</summary>
        private const int MaxFailedSpawns = 3;

        /// <summary>
        /// Pays each ship that finished: credits shared by the players aboard, item pulls to the pilot's hold. Every share
        /// is saved as pending before any is handed out, so a restart mid-payout leaves the rest to collect at login.
        /// </summary>
        private static void PayRiftRun(RiftRun run)
        {
            var secondsLeft = run.FinishedSecondsLeft;
            var credits = RiftRunRules.Credits(run.Key, secondsLeft);
            var pulls = RiftRunRules.Pulls(secondsLeft);
            var table = Loot.GetLootTableByName(run.Key.LootTable);
            var state = IncursionRecord;
            state.PendingRiftPayouts ??= new Dictionary<string, IncursionPayout>();
            var collecting = new Dictionary<string, uint>();
            foreach (var pilot in run.Pilots)
            {
                var items = Enumerable.Range(0, pulls).Select(_ => table.GetRandomItem().Resref).ToList();
                var nextKey = RiftRunRules.NextKey(run.Key);
                if (nextKey != null && Random.Next(100) < RiftRunTuning.NextKeyPercent)
                    items.Add(nextKey.Resref);

                var pilotId = GetObjectUUID(pilot);
                CapitalBattleRules.AddPending(state.PendingRiftPayouts, pilotId, new IncursionPayout
                {
                    Items = items.Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
                    RegionName = run.Key.Name
                });
                if (GetIsObjectValid(pilot))
                    collecting[pilotId] = pilot;

                var aboard = GetShipCrewPlayers(pilot);
                foreach (var (playerId, share) in RiftRunRules.ShareCredits(credits, aboard.Select(GetObjectUUID).ToList()))
                {
                    var player = aboard.First(x => GetObjectUUID(x) == playerId);
                    CapitalBattleRules.AddPending(state.PendingRiftPayouts, playerId, new IncursionPayout { Credits = share, RegionName = run.Key.Name });
                    collecting[playerId] = player;
                    SendMessageToPC(player, ColorToken.Green(
                        $"The pocket is cleared with {RiftRunRules.FormatTimer(secondsLeft)} to spare. The rift returns you in {RiftRunTuning.ReturnDelaySeconds:0} seconds."));
                }

                Log.Write(LogGroup.Space, $"RIFT RUN CLEARED - {PlayerName.GetAuditName(pilot)} - {run.Key.Resref}, {secondsLeft} s left, " +
                                          $"{credits} credits, {string.Join(", ", items)}");
            }

            SaveIncursions();
            foreach (var (playerId, player) in collecting)
                CollectPendingPayout(player, state.PendingRiftPayouts, playerId, "Rift Run spoils", "RIFT RUN PAYOUT");
        }

        /// <summary>A player whose Rift Run spoils were saved but not handed out (a restart mid-payout) collects them at login.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void DeliverPendingRiftRunPayout()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            if (IncursionRecord.PendingRiftPayouts == null || !IncursionRecord.PendingRiftPayouts.ContainsKey(playerId))
                return;

            // The payout stays pending until delivery: a player who disconnects within the delay collects it at the next login.
            DelayCommand(6f, () => CollectPendingPayout(player, IncursionRecord.PendingRiftPayouts, playerId,
                "Rift Run spoils", "RIFT RUN PAYOUT"));
        }

        /// <summary>
        /// A logged-out pilot's ship in a failed run takes what a ship destroyed online takes (<see cref="ApplyDeath"/>):
        /// its cargo spills into the dying pocket, it is left at 1 hull and no shield for the repair bill, and its pilot
        /// takes the XP debt at their next login. Modules stay installed, as no player destroyed it.
        /// </summary>
        private static void ForfeitAwayShip(RiftRun run, PlayerShip ship, Location location)
        {
            WreckShipInPocket(ship, location);
            var pilotId = OweAwayShipLoss(run, ship.Id);
            Log.Write(LogGroup.Space, $"RIFT RUN - away ship {ship.Id} lost in a failed run; pilot {pilotId ?? "unknown"} owes the XP debt at login");
        }

        /// <summary>A ship lost in a failed run's collapse: its cargo spills and it is left at 1 hull and no shield.</summary>
        private static void WreckShipInPocket(PlayerShip ship, Location location)
        {
            Trade.SpillCargo(ship, location);
            ship.Status.PendingRiders?.Clear();
            ship.Status.Hull = 1;
            ship.Status.Shield = 0;
            DB.Set(ship);
        }

        /// <summary>The run's pilot of a ship lost while they were away takes the XP debt at their next login. Returns their id.</summary>
        private static string OweAwayShipLoss(RiftRun run, string shipId)
        {
            var dbPlayer = run.PilotIds.TryGetValue(shipId, out var pilotId) ? DB.Get<Player>(pilotId) : null;
            if (dbPlayer != null)
            {
                dbPlayer.PendingShipLossMessage = "The Rift Run failed while you were away. Your ship was wrecked in the collapsing pocket and towed home at 1 hull, its cargo lost.";
                DB.Set(dbPlayer);
            }

            return pilotId;
        }

        /// <summary>The failed run whose pocket is collapsing in this area (<see cref="RiftRunRules.WrecksDockedShips"/>), or null.</summary>
        private static RiftRun FindCollapsingRiftRun(uint area) =>
            GetIsObjectValid(area)
                ? _riftRuns.Values.FirstOrDefault(x => x.Area == area && RiftRunRules.WrecksDockedShips(x))
                : null;

        /// <summary>
        /// A host destroyed in a collapsing pocket takes the ships docked aboard down with it instead of launching them
        /// into the pocket. Each is wrecked as a ship lost in the collapse, leaves the host and returns to its last
        /// dock, and leaves the run. The players in their interiors join <paramref name="riders"/>, who ride the host's
        /// escape pods with its crew and take the XP debt; a ship's run pilot who rides no pod (logged out) takes it at
        /// their next login.
        /// </summary>
        private static void WreckShipsDockedInCollapse(RiftRun run, List<PlayerShip> visitors, Location location,
            ICollection<string> aboardHostIds, ICollection<uint> riders)
        {
            foreach (var visitor in visitors)
            {
                if (Property.TryGetLoadedInstance(visitor.PropertyId, out var instance))
                {
                    foreach (var player in GetPlayersInArea(instance.Area).Where(GetIsObjectValid))
                    {
                        if (!riders.Contains(player))
                            riders.Add(player);
                    }
                }

                WreckShipInPocket(visitor, location);
                ClearDockedState(visitor);
                DB.Set(visitor);
                var property = DB.Get<WorldProperty>(visitor.PropertyId);
                if (property != null && property.Positions.Remove(PropertyLocationType.CurrentPosition))
                    DB.Set(property);
                RiftRunRules.Forget(run, visitor.Id);

                var pilotId = run.PilotIds.TryGetValue(visitor.Id, out var id) ? id : null;
                var podded = pilotId != null &&
                             (aboardHostIds.Contains(pilotId) || riders.Any(x => GetObjectUUID(x) == pilotId));
                if (pilotId != null && !podded)
                    OweAwayShipLoss(run, visitor.Id);

                Log.Write(LogGroup.Space, $"RIFT RUN - ship {visitor.Id} docked aboard a ship destroyed in the collapsing pocket was wrecked with it; " +
                                          $"pilot {pilotId ?? "unknown"} {(podded ? "rides the host's escape pods" : "owes the XP debt at login")}");
            }
        }

        private static void ReturnFromRiftPocket(RiftRun run, uint pilot)
        {
            if (!GetIsObjectValid(pilot) || GetArea(pilot) != run.Area)
                return;

            var origin = run.Origins.TryGetValue(pilot, out var location) && GetIsObjectValid(GetAreaFromLocation(location))
                ? location
                : null;
            if (origin == null)
                return;

            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Unsummon), GetLocation(pilot));
            AssignCommand(pilot, () =>
            {
                ClearAllActions();
                JumpToLocation(origin);
            });
        }

        /// <summary>The timer ran out: every ship still in the pocket is destroyed, and everyone aboard ejects in escape pods.</summary>
        private static void CollapseRiftPocket(RiftRun run)
        {
            MessageRiftRun(run, ColorToken.Red("The rift pocket collapses around you! Your ship breaks apart."));
            foreach (var pilot in run.Pilots.Where(x => GetIsObjectValid(x) && GetArea(x) == run.Area).ToList())
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), pilot);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), pilot);
            }

            Log.Write(LogGroup.Space, $"RIFT RUN COLLAPSED - {run.Key.Resref}, chamber {run.Room + 1}, {run.Pilots.Count} ship(s) destroyed");
        }

        /// <summary>
        /// A logged-out pilot's host lost in a failed run is lost with everyone aboard, as a host destroyed online is
        /// (<see cref="ApplyDeath"/>): every player in its interior (its own crew and passengers, and those who came
        /// aboard with a docked ship) rides an escape pod to its dock and takes the XP debt. The ships docked aboard are
        /// wrecked with it (<see cref="WreckShipsDockedInCollapse"/>), the players in their interiors ride the pods too,
        /// and a docked ship's run pilot who rides no pod takes the debt at their next login.
        /// </summary>
        private static void LoseAwayHostWithEveryoneAboard(RiftRun run, PlayerShip host, Location location)
        {
            var riders = new List<uint>();
            if (Property.TryGetLoadedInstance(host.PropertyId, out var hostInstance))
                riders.AddRange(GetPlayersInArea(hostInstance.Area).Where(GetIsObjectValid));

            var visitors = GetDockedShips(host.Id);
            if (visitors.Count > 0)
                WreckShipsDockedInCollapse(run, visitors, location, riders.Select(GetObjectUUID).ToList(), riders);

            if (riders.Count == 0)
                return;

            var hostProperty = DB.Get<WorldProperty>(host.PropertyId);
            if (hostProperty != null && TryResolveEscapePodDestination(hostProperty, out var podLocation, out var podType))
            {
                EjectEscapePods(riders, OBJECT_INVALID, podLocation, podType);
                return;
            }

            // No dock can be found: as for a host destroyed online, the riders die.
            Log.Write(LogGroup.Space, $"RIFT RUN - ship {host.Id} was lost in a failed run with no dock to eject the players aboard to.");
            foreach (var rider in riders)
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), rider);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), rider);
            }
        }

        /// <summary>
        /// Removes a finished pocket once no player is in it. Ships left in it by pilots who logged out are sent back to
        /// their docks; when the run failed they first take the losses of a ship destroyed in the collapse
        /// (<see cref="ForfeitAwayShip"/>).
        /// </summary>
        private static void CloseRiftPocket(RiftRun run)
        {
            if (!GetIsObjectValid(run.Area))
            {
                _riftRuns.Remove(run.Id);
                return;
            }

            foreach (var enemy in run.Enemies.Where(GetIsObjectValid))
                DestroyObject(enemy);
            run.Enemies.Clear();

            foreach (var (shipId, clone) in _shipClones.Where(x => GetIsObjectValid(x.Value) && GetArea(x.Value) == run.Area).ToList())
            {
                _shipClones.Remove(shipId);
                var dbShip = DB.Get<PlayerShip>(shipId);
                if (run.Failed && dbShip != null)
                {
                    ForfeitAwayShip(run, dbShip, GetLocation(clone));
                    LoseAwayHostWithEveryoneAboard(run, dbShip, GetLocation(clone));
                }

                DestroyObject(clone);
                var dbProperty = dbShip == null ? null : DB.Get<WorldProperty>(dbShip.PropertyId);
                if (dbProperty != null && dbProperty.Positions.Remove(PropertyLocationType.CurrentPosition))
                    DB.Set(dbProperty);
                Log.Write(LogGroup.Space, $"RIFT RUN - unpiloted ship {shipId} returned to its dock from a closed pocket");
            }

            for (var obj = GetFirstObjectInArea(run.Area); GetIsObjectValid(obj); obj = GetNextObjectInArea(run.Area))
            {
                if (GetIsPC(obj))
                    return;
            }

            if (DestroyArea(run.Area) == 1)
                _riftRuns.Remove(run.Id);
        }

        private static void MessageRiftRun(RiftRun run, string message)
        {
            foreach (var pilot in run.Pilots.Where(GetIsObjectValid))
            {
                foreach (var player in GetShipCrewPlayers(pilot))
                    SendMessageToPC(player, message);
            }
        }

        /// <summary>The Ship HUD's Rift Run line, or null outside a run.</summary>
        public static string GetRiftRunStatusText(uint player)
        {
            var run = FindRiftRun(player);
            if (run == null)
                return null;

            return run.Phase == RiftRunPhase.Returning
                ? "cleared, returning"
                : $"chamber {run.Room + 1}/{RiftRunTuning.Rooms.Count}, {RiftRunRules.FormatTimer(RiftRunRules.SecondsLeft(run.StartedAt, DateTime.UtcNow))} left";
        }
    }
}
