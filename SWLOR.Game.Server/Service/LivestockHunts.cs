using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Hunts at a city's corrals (City Affairs, "Predators at the corrals"). An official calls one in the situation window: a
    /// wave of predators stands at the corrals for 30 minutes. The animals cannot be harmed (they are plot creatures). Players
    /// who drive the wave off win the hunt; if any predator still stands when the time is up, the hunt is lost and every animal
    /// in the city gives no products for 72 hours. A hunt starts only when an official chooses it, so attacks happen only while
    /// players are online. The record (<see cref="LivestockHunt"/>) lets a restart pick an unfinished hunt up again, with a new
    /// wave.
    /// </summary>
    public static class LivestockHunts
    {
        private const string HuntVariable = "LIVESTOCK_HUNT";
        private const float CheckSeconds = 5f;
        private const float WaveRadius = 4f;

        private sealed class Wave
        {
            public int Expected;
            public readonly HashSet<string> SpawnedCorrals = new();
            public readonly List<uint> Predators = new();
        }

        private static readonly Dictionary<string, Wave> _waves = new();

        /// <summary>Calls a hunt at the city's corrals. A city has one at a time; a second call changes nothing.</summary>
        public static void Start(WorldProperty city, DateTime now)
        {
            if (GetActive(city.Id) != null)
                return;

            var hunt = new LivestockHunt { CityId = city.Id, StartedUtc = now, EndsUtc = LivestockRules.GetHuntEndUtc(now) };
            DB.Set(hunt);
            Log.Write(LogGroup.Property, $"A hunt was called at the corrals of city {city.Id} until {hunt.EndsUtc:u}.");

            Begin(hunt, city);
        }

        public static LivestockHunt GetActive(string cityId)
        {
            return string.IsNullOrWhiteSpace(cityId)
                ? null
                : DB.Search(new DBQuery<LivestockHunt>().AddFieldSearch(nameof(LivestockHunt.CityId), cityId, false)).FirstOrDefault();
        }

        /// <summary>A hunt's wave stands at every corral the city has that is in the world. The rest spawn when their corral does.</summary>
        private static void Begin(LivestockHunt hunt, WorldProperty city)
        {
            var corrals = Corrals.GetCorralsOfCity(city.Id);
            _waves[hunt.Id] = new Wave { Expected = LivestockRules.GetHuntWaveSize(corrals.Count) };

            foreach (var corral in corrals)
            {
                var placeable = Property.GetPlaceableByPropertyId(corral.Id);
                if (GetIsObjectValid(placeable))
                    SpawnFor(hunt, city, corral, placeable);
            }

            DelayCommand(CheckSeconds, () => Check(hunt.Id));
        }

        /// <summary>A corral spawns (placed, moved, or its city loading) while a hunt is on: its share of the wave stands there.</summary>
        public static void OnCorralSpawned(WorldProperty corral, uint placeable)
        {
            var hunt = GetActive(corral.ParentPropertyId);
            if (hunt == null || hunt.EndsUtc <= DateTime.UtcNow)
                return;

            var city = DB.Get<WorldProperty>(corral.ParentPropertyId);
            if (city == null)
                return;

            var resumed = !_waves.ContainsKey(hunt.Id);
            if (resumed)
            {
                _waves[hunt.Id] = new Wave { Expected = LivestockRules.GetHuntWaveSize(Corrals.GetCorralsOfCity(city.Id).Count) };
                DelayCommand(CheckSeconds, () => Check(hunt.Id));
            }

            SpawnFor(hunt, city, corral, placeable);
        }

        private static void SpawnFor(LivestockHunt hunt, WorldProperty city, WorldProperty corral, uint placeable)
        {
            var wave = _waves[hunt.Id];
            if (!wave.SpawnedCorrals.Add(corral.Id))
                return;

            var corrals = Corrals.GetCorralsOfCity(city.Id);
            var index = Math.Max(0, corrals.FindIndex(other => other.Id == corral.Id));
            var share = wave.Expected / Math.Max(1, corrals.Count) + (index < wave.Expected % Math.Max(1, corrals.Count) ? 1 : 0);
            var resref = LivestockRules.GetPredatorResref(CityOfficers.GetCityLevel(city));
            var center = GetPosition(placeable);
            var area = GetArea(placeable);
            for (var predator = 0; predator < share; predator++)
            {
                var angle = predator * (2f * MathF.PI / Math.Max(1, share));
                var position = Vector3(center.X + MathF.Cos(angle) * WaveRadius, center.Y + MathF.Sin(angle) * WaveRadius, center.Z);
                var creature = CreateObject(ObjectType.Creature, resref, Location(area, position, angle * 180f / MathF.PI + 180f));
                if (!GetIsObjectValid(creature))
                {
                    Log.Write(LogGroup.Property, $"The hunt predator '{resref}' could not be created at corral {corral.Id}.");
                    continue;
                }

                SetLocalString(creature, HuntVariable, hunt.Id);
                wave.Predators.Add(creature);
            }
        }

        /// <summary>Judges the hunt every few seconds: won when every predator it spawned is down, lost when its time is up.</summary>
        private static void Check(string huntId)
        {
            var hunt = DB.Get<LivestockHunt>(huntId);
            if (hunt == null || !_waves.TryGetValue(huntId, out var wave))
                return;

            var alive = wave.Predators.Count(predator => GetIsObjectValid(predator) && !GetIsDead(predator));
            var spawned = wave.Predators.Count;
            if (spawned >= wave.Expected && LivestockRules.IsHuntWon(spawned, alive))
            {
                Resolve(hunt, won: true);
                return;
            }

            if (DateTime.UtcNow >= hunt.EndsUtc)
            {
                Resolve(hunt, won: false);
                return;
            }

            DelayCommand(CheckSeconds, () => Check(huntId));
        }

        private static void Resolve(LivestockHunt hunt, bool won)
        {
            // The record goes first, so a stop part way never resolves the hunt twice.
            DB.Delete<LivestockHunt>(hunt.Id);
            RemoveWave(hunt.Id);

            var city = DB.Get<WorldProperty>(hunt.CityId);
            if (city == null)
                return;

            if (won)
            {
                CityGazette.Post(city.Id, CityGazetteEntryType.Decision, "A hunt drove the predators from the city's corrals.");
                Corrals.NotifyOwnersSafe(city);
            }
            else
            {
                Corrals.ApplyPredatorPenalty(city, DateTime.UtcNow);
                CityGazette.Post(city.Id, CityGazetteEntryType.Decision, $"A hunt failed to drive the predators from the city's corrals. The city's animals give no products for {LivestockRules.PenaltyHours} hours.");
            }

            Log.Write(LogGroup.Property, $"The hunt at the corrals of city {city.Id} was {(won ? "won" : "lost")}.");
        }

        /// <summary>A hunt that cannot go on (its city was deleted): its wave goes, with no outcome.</summary>
        public static void Cancel(LivestockHunt hunt)
        {
            DB.Delete<LivestockHunt>(hunt.Id);
            RemoveWave(hunt.Id);
        }

        private static void RemoveWave(string huntId)
        {
            if (!_waves.Remove(huntId, out var wave))
                return;

            foreach (var predator in wave.Predators.Where(GetIsObjectValid))
                DestroyObject(predator);
        }

        /// <summary>
        /// At server start, a hunt that ended while the server was down ends with no outcome: its wave could not be fought. One
        /// that is still on is picked up again when its corrals spawn, with a new wave and the time that is left.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ProcessAtServerStart()
        {
            var now = DateTime.UtcNow;
            var count = checked((int)DB.SearchCount(new DBQuery<LivestockHunt>()));
            if (count <= 0)
                return;

            foreach (var hunt in DB.Search(new DBQuery<LivestockHunt>().AddPaging(count, 0)).ToList())
            {
                if (hunt.EndsUtc > now)
                    continue;

                DB.Delete<LivestockHunt>(hunt.Id);
                CityGazette.Post(hunt.CityId, CityGazetteEntryType.Decision, "The predators moved on from the city's corrals.");
                Log.Write(LogGroup.Property, $"The hunt at the corrals of city {hunt.CityId} ended while the server was down, with no outcome.");
            }
        }
    }
}
