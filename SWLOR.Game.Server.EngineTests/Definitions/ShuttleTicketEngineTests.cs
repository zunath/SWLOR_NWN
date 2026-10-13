using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ShuttleTicketEngineTests
    {
        [EngineTest("Saved shuttle tickets keep ownership and refund once across session reminders", Category = "ShuttleTicket")]
        public static async Task ReturningTicketAndRefund(EngineTestContext ctx)
        {
            var passenger = ctx.SpawnCreature("civilian");
            var other = ctx.SpawnCreature("civilian", 2f);
            await ctx.DelaySecondsAsync(1f);
            var passengerId = GetObjectUUID(passenger);
            var otherId = GetObjectUUID(other);
            var origin = PlanetType.CZ220;
            var destination = PlanetType.Viscara;
            var ride = new ShuttleRide(passengerId)
            {
                Status = ShuttleRideStatus.Ticketed, Origin = origin, Destination = destination,
                FarePaid = 25, TaxPaid = 5
            };
            var otherRide = new ShuttleRide(otherId)
            {
                Status = ShuttleRideStatus.Ticketed, Origin = origin, Destination = destination,
                FarePaid = 25
            };
            DB.Set(ride);
            DB.Set(otherRide);
            var gold = GetGold(passenger);
            try
            {
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    BoardingTimes.Remove(passengerId);
                    // Yield to end the script context, clearing DB's per-context object cache.
                    await ctx.WaitFrameAsync();
                    var saved = Shuttle.GetRide(passengerId);
                    ctx.AssertEqual(ride.Id, saved.Id, "Reload keeps the same paid ticket");
                    ctx.AssertEqual(passengerId, saved.PlayerId, "Reload preserves ticket ownership");
                    ctx.AssertEqual(25, saved.FarePaid, "Reload preserves the refundable fare");
                    ctx.AssertEqual(5, saved.TaxPaid, "Reload preserves the non-refundable tax");

                    var departure = ShuttleSchedule.GetNextDepartureUtc(origin, destination, DateTime.UtcNow);
                    var login = departure.AddSeconds(-1);
                    Invoke("RestoreTicketedSession", passenger, login);
                    ctx.AssertEqual(login.AddSeconds(60), BoardingTimes[passengerId], "Every login grants a fresh full boarding-call interval");
                    var advertised = Shuttle.GetNextDepartureUtc(origin, destination, passengerId);
                    ctx.Assert(advertised >= login.AddSeconds(60), "Ticket status advertises an eligible departure");
                    await ctx.WaitFrameAsync();
                    ctx.AssertEqual(ShuttleRideStatus.Ticketed, Shuttle.GetRide(passengerId).Status,
                        "A session reminder preserves the unboarded ticket");
                    ctx.AssertEqual(gold, GetGold(passenger), "Session recovery neither charges nor refunds credits");
                    ctx.AssertEqual(1L, DB.SearchCount(new DBQuery<ShuttleRide>().AddFieldSearch(nameof(ShuttleRide.PlayerId), passengerId, false)),
                        "Repeated recovery never duplicates a ticket");
                }

                ctx.Assert(Shuttle.RefundTicket(passenger), "The retained ticket can be cancelled before boarding");
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(gold + 25, GetGold(passenger), "Cancellation refunds only the fare");
                ctx.Assert(Shuttle.GetRide(passengerId) == null, "Cancellation remains deleted after reload");
                ctx.Assert(!BoardingTimes.ContainsKey(passengerId), "Cancellation clears automatic boarding eligibility");
                ctx.Assert(!Shuttle.RefundTicket(passenger), "Cancellation cannot refund twice");
                ctx.AssertEqual(gold + 25, GetGold(passenger), "Repeated cancellation never duplicates credits");
                ctx.AssertEqual(otherId, Shuttle.GetRide(otherId).PlayerId, "Cancellation does not affect another passenger's ticket");
                ctx.SetResultDetail("NPC actors exercise saved ticket reminders and refunds; connected-client logout/login is not simulated.");
            }
            finally
            {
                Cleanup(ride);
                Cleanup(otherRide);
            }
        }

        [EngineTest("A shuttle purchase during login delay keeps its confirmed boarding time", Category = "ShuttleTicket")]
        public static async Task PurchaseDuringLoginDelay(EngineTestContext ctx)
        {
            var passenger = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(1f);
            var playerId = GetObjectUUID(passenger);
            var ride = new ShuttleRide(playerId)
            {
                Status = ShuttleRideStatus.Ticketed,
                Origin = PlanetType.CZ220,
                Destination = PlanetType.Viscara,
                FarePaid = 25
            };
            DB.Set(ride);
            try
            {
                // Mirror the session eligibility recorded by TryPurchaseTicket while
                // the login reminder is still pending. No connected client is required.
                var purchasedUtc = DateTime.UtcNow;
                BoardingTimes[playerId] = purchasedUtc;
                await ctx.WaitFrameAsync();
                Invoke("RestoreTicketedSession", passenger, purchasedUtc.AddSeconds(2));
                ctx.AssertEqual(purchasedUtc, BoardingTimes[playerId],
                    "The delayed login reminder preserves the explicit purchase's boarding time");
                var confirmed = ShuttleSchedule.GetNextDepartureUtc(ride.Origin, ride.Destination, purchasedUtc);
                ctx.AssertEqual(confirmed, ShuttleSchedule.GetNextDepartureUtc(ride.Origin, ride.Destination, purchasedUtc, BoardingTimes[playerId]),
                    "The confirmed departure is not moved to a later flight");
                ctx.AssertEqual(ride.Id, Shuttle.GetRide(playerId).Id, "The paid ticket remains unchanged");
            }
            finally
            {
                Cleanup(ride);
            }
        }

        [EngineTest("Offline shuttle arrivals retain their destination until login resolution", Category = "ShuttleTicket")]
        public static async Task OfflineArrivalUntilLogin(EngineTestContext ctx)
        {
            var passenger = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(1f);
            var playerId = GetObjectUUID(passenger);
            var player = new Player(playerId) { LocationAreaResref = GetResRef(ctx.Arena), RebuildComplete = true };
            var other = new Player(Guid.NewGuid().ToString()) { LocationAreaResref = "untouched_area" };
            DB.Set(player);
            DB.Set(other);
            var waypoint = GetWaypointByTag(Planet.GetPlanetByType(PlanetType.Viscara).LandingWaypointTag);
            ctx.Assert(GetIsObjectValid(waypoint), "The destination landing waypoint is available");
            var landingPosition = GetPosition(waypoint);
            var gold = GetGold(passenger);
            ShuttleRide ride = null;
            try
            {
                for (var trip = 0; trip < 2; trip++)
                {
                    var departure = DateTime.UtcNow.AddMinutes(-2);
                    ride = new ShuttleRide(playerId)
                    {
                        Status = ShuttleRideStatus.InTransit,
                        Origin = PlanetType.CZ220,
                        Destination = PlanetType.Viscara,
                        DepartureUtc = departure,
                        ArrivalUtc = departure.AddMinutes(1),
                        FlightId = ShuttleSchedule.BuildFlightId(PlanetType.CZ220, PlanetType.Viscara, departure),
                        FarePaid = 25
                    };
                    DB.Set(ride);
                    // Arrival and a subsequent server recovery use the same offline delivery
                    // path. Neither may consume the notice before this passenger logs in.
                    for (var recovery = 0; recovery < 2; recovery++)
                    {
                        Invoke("DeliverOfflinePassenger", Shuttle.GetRide(playerId));
                        await ctx.WaitFrameAsync();
                        var savedRide = Shuttle.GetRide(playerId);
                        ctx.AssertEqual(ride.Id, savedRide.Id, "Offline delivery preserves the pending arrival");
                        ctx.AssertEqual(playerId, savedRide.PlayerId, "The arrival remains owned by its passenger");
                        ctx.AssertEqual(ride.Destination, savedRide.Destination, "The destination survives reload");
                        ctx.AssertEqual(1L, DB.SearchCount(new DBQuery<ShuttleRide>().AddFieldSearch(nameof(ShuttleRide.PlayerId), playerId, false)),
                            "Repeated recovery retains exactly one arrival record");
                        var savedPlayer = DB.Get<Player>(playerId);
                        ctx.AssertEqual(GetResRef(GetArea(waypoint)), savedPlayer.LocationAreaResref, "Offline delivery saves the landing area");
                        ctx.AssertEqual(landingPosition.X, savedPlayer.LocationX, "Offline delivery saves landing X");
                        ctx.AssertEqual(landingPosition.Y, savedPlayer.LocationY, "Offline delivery saves landing Y");
                    }

                    Invoke("CompleteRideOnLogin", passenger, Shuttle.GetRide(playerId));
                    await ctx.WaitFrameAsync();
                    ctx.Assert(Shuttle.GetRide(playerId) == null, "Login consumes the completed ride after announcing arrival");
                    ctx.AssertEqual(gold, GetGold(passenger), "Arrival cannot refund a used fare");
                    ctx.AssertEqual("untouched_area", DB.Get<Player>(other.Id).LocationAreaResref,
                        "Another player's saved location is unchanged");
                }
                ctx.SetResultDetail("NPC actor exercises offline delivery and login completion through the real service and Redis; connected-client notices are not visually verified.");
            }
            finally
            {
                if (ride != null) Cleanup(ride);
                DB.Delete<Player>(playerId);
                DB.Delete<Player>(other.Id);
            }
        }

        private static Dictionary<string, DateTime> BoardingTimes =>
            (Dictionary<string, DateTime>)typeof(Shuttle).GetField("_boardingNotBeforeUtc", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        private static void Invoke(string method, params object[] arguments) =>
            typeof(Shuttle).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);

        private static void Cleanup(ShuttleRide ride)
        {
            DB.Delete<ShuttleRide>(ride.Id);
            BoardingTimes.Remove(ride.PlayerId);
            var holders = (Dictionary<string, (PlanetType Origin, PlanetType Destination)>)typeof(Shuttle)
                .GetField("_ticketHolders", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            holders.Remove(ride.PlayerId);
        }
    }
}
