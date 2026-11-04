using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Crafting stations that the crafting revamp left unusable can be used again. Using a placeable walks the user to its model's use
    /// point first, so a station whose use point sits against a wall, or a full-size station placed where a small pot stood on a table,
    /// never runs its script: the player walks up and stops, and clicking it does nothing. Each case walks a creature from open floor
    /// in front of the station, where a player arrives, and checks that the station registers the use.
    /// </summary>
    public static class CraftingStationUseEngineTests
    {
        private const string User = "civilian";
        private const float UseSeconds = 15f;

        private static readonly StationCase[] Cases =
        {
            // Veles shops: the forge model's use point is on its other side, so these benches face the room instead of the wall.
            new("veles_shops", "smithery_bench", 22.5f, 88.0f, 24.5f, 84.0f),
            new("veles_shops", "smithery_bench", 26.5f, 88.0f, 24.5f, 84.0f),

            // Korriban landing kitchen: the galley model replaced two pots standing on a table and a grill tucked behind a shelf.
            new("korribanlandingp", "cookpot", 151.9f, 95.6f, 154.1f, 95.6f),
            new("korribanlandingp", "cookpot", 151.9f, 97.3f, 154.1f, 97.3f),
            new("korribanlandingp", "cookpot", 150.6f, 93.7f, 152.2f, 93.6f),

            // Mandalorian refineries: they had no use script, only the retired container scripts.
            new("sol_mandaloriani", "cft_forge", 125.6f, 46.1f, 126.9f, 47.6f),
            new("sol_mandaloriani", "cft_forge", 115.9f, 85.5f, 117.5f, 86.9f),
            new("sol_mandaloriani", "cft_forge", 124.1f, 84.5f, 122.7f, 85.0f)
        };

        [EngineTest("Crafting stations the revamp left unusable can be used from the floor in front of them", Category = "CraftingStationUse", TimeoutSeconds = 120f)]
        public static async Task StationsCanBeUsed(EngineTestContext ctx)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var attempts = new List<(StationCase Case, uint Station, uint User)>();
            foreach (var stationCase in Cases)
            {
                var area = FindArea(stationCase.Area);
                ctx.Assert(GetIsObjectValid(area), $"The area '{stationCase.Area}' exists");
                var station = FindStation(area, stationCase);
                ctx.Assert(GetIsObjectValid(station), $"{stationCase} is placed");

                var user = CreateObject(ObjectType.Creature, User, Location(area, new Vector3(stationCase.StartX, stationCase.StartY, 0f), 0f));
                ctx.Assert(GetIsObjectValid(user), $"A user spawns in front of {stationCase}");
                ctx.Track(user);
                attempts.Add((stationCase, station, user));
            }

            foreach (var (_, station, user) in attempts)
            {
                server.GetPlaceableByGameObjectID(station).m_oidLastUser = OBJECT_INVALID;
                var target = station;
                AssignCommand(user, () => ActionInteractObject(target));
            }

            for (var elapsed = 0f; elapsed < UseSeconds && attempts.Any(attempt => !Used(server, attempt.Station, attempt.User)); elapsed += 0.5f)
                await ctx.DelaySecondsAsync(0.5f);

            var unusable = attempts
                .Where(attempt => !Used(server, attempt.Station, attempt.User))
                .Select(attempt =>
                {
                    var position = GetPosition(attempt.User);
                    return $"{attempt.Case}: the user stopped at ({position.X:F2}, {position.Y:F2})";
                })
                .ToList();
            ctx.Assert(unusable.Count == 0, $"Every station is used; unusable: {string.Join("; ", unusable)}");
        }

        private static bool Used(CServerExoApp server, uint station, uint user) =>
            server.GetPlaceableByGameObjectID(station).m_oidLastUser == user;

        private static uint FindArea(string resref)
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (GetResRef(area) == resref)
                    return area;
            }

            return OBJECT_INVALID;
        }

        private static uint FindStation(uint area, StationCase stationCase)
        {
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetObjectType(obj) != ObjectType.Placeable || GetTag(obj) != stationCase.Tag)
                    continue;

                var position = GetPosition(obj);
                if (MathF.Abs(position.X - stationCase.X) < 0.1f && MathF.Abs(position.Y - stationCase.Y) < 0.1f)
                    return obj;
            }

            return OBJECT_INVALID;
        }

        private sealed record StationCase(string Area, string Tag, float X, float Y, float StartX, float StartY)
        {
            public override string ToString() => $"{Area} {Tag} at ({X:F1}, {Y:F1})";
        }
    }
}
