using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ShuttleStatusConsoleEngineTests
    {
        [EngineTest("The shuttle status console spawns at the cabin console point instead of the boarding divider", Category = "ShuttleTicket")]
        public static async Task ConsoleSpawnsAtConsolePoint(EngineTestContext ctx)
        {
            var departure = DateTime.UtcNow.AddMinutes(5);
            var flightId = ShuttleSchedule.BuildFlightId(PlanetType.CZ220, PlanetType.Viscara, departure);
            var flight = typeof(Shuttle)
                .GetMethod("GetOrCreateFlight", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { flightId, PlanetType.CZ220, PlanetType.Viscara, departure, departure.AddMinutes(2) });
            var area = (uint)flight.GetType().GetProperty("Area").GetValue(flight);
            try
            {
                await ctx.WaitFrameAsync();
                var console = (uint)flight.GetType().GetProperty("Console").GetValue(flight);
                ctx.Assert(GetIsObjectValid(console), "The flight instance has a status console");
                ctx.AssertEqual("Shuttle Status Console", GetName(console), "The console is the shuttle status console");
                ctx.AssertEqual(area, GetArea(console), "The console stands in the flight instance");

                var position = GetPosition(console);
                ctx.Assert(Math.Abs(position.X - 45f) < 0.05f && Math.Abs(position.Y - 37f) < 0.05f,
                    $"The console stands at the cabin console point (45, 37), not the boarding point (45, 26); was ({position.X:0.00}, {position.Y:0.00})");
                ctx.Assert(Math.Abs(GetFacing(console) - 90f) < 1f,
                    $"The console keeps the console point's facing (90); was {GetFacing(console):0.0}");
                ctx.SetResultDetail($"Console at ({position.X:0.00}, {position.Y:0.00}) facing {GetFacing(console):0.0} in {GetResRef(area)}.");
            }
            finally
            {
                ((IDictionary)typeof(Shuttle).GetField("_activeFlights", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null))
                    .Remove(flightId);
                if (GetIsObjectValid(area))
                    DestroyArea(area);
            }
        }
    }
}
