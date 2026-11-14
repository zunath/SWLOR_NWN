using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityBountyService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// City bounties on live players: a bounty pays out when its target is defeated, and PvP damage opens the combat
    /// window that holds back turning the PvP flag off.
    /// </summary>
    public static class CityBountyEngineTests
    {
        private const string Category = "CityBounty";
        private const int LethalDamage = 5000;
        private const int Reward = 5000;
        private const int Treasury = 20000;

        [EngineTest("A bounty on a defeated target pays its collector, releases the escrow and starts the identity's week", Category = Category, TimeoutSeconds = 60f)]
        public static async Task BountyPaysWhenTheTargetIsDefeated(EngineTestContext ctx)
        {
            using var mayor = await PlayerAbilityFixture.CreateAsync(ctx, 8f);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            using var hunter = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var city = new WorldProperty
            {
                Id = "engine-bounty-city/" + mayor.Id,
                PropertyType = PropertyType.City,
                CustomName = "Engine Bounty City",
                OwnerPlayerId = mayor.Id,
                Treasury = Treasury
            };
            DB.Set(city);
            string bountyId = null;

            try
            {
                OptIn(target);
                var error = string.Empty;
                await ctx.ExecuteInCreatureContextAsync(mayor.Creature, () =>
                    error = CityBounties.Post(mayor.Creature, city.Id, target.Creature, string.Empty, Reward));
                ctx.AssertEqual(string.Empty, error, "the mayor can post a bounty on a flagged target who accepted the notice");

                var bounty = CityBounties.GetOpenForCity(city.Id).Single();
                bountyId = bounty.Id;
                ctx.AssertEqual(Treasury - Reward, DB.Get<WorldProperty>(city.Id).Treasury, "the reward leaves the ordinary treasury");
                ctx.Assert(DB.Get<WorldProperty>(city.Id).Escrow.ContainsKey(bounty.Id), "and is held in escrow");
                ctx.Assert(bounty.TargetNotified, "the target online is told at once");

                var goldBefore = GetGold(hunter.Creature);
                await ctx.ExecuteInCreatureContextAsync(hunter.Creature, () =>
                    Combat.ApplyTriggeredDamage(hunter.Creature, target.Creature, LethalDamage, CombatDamageType.Physical));

                await ctx.WaitUntilAsync(() => CityBounties.Get(bounty.Id) == null, 10f, "the bounty to be claimed");
                ctx.Assert(!DB.Get<WorldProperty>(city.Id).Escrow.ContainsKey(bounty.Id), "the escrow is paid out");
                ctx.AssertEqual(Treasury - Reward, DB.Get<WorldProperty>(city.Id).Treasury, "the treasury is not refunded");
                ctx.Assert(DB.Get<Player>(target.Id).LastBountyClaimedUtc.HasValue, "the identity's week starts");
                ctx.Assert(GetGold(hunter.Creature) - goldBefore == Reward || PendingCredits.GetTotal(hunter.Id) == Reward,
                    "the collector is paid, or owed the credits until they next log in");
            }
            finally
            {
                if (bountyId != null)
                    DB.Delete<CityBounty>(bountyId);
                DB.Delete<WorldProperty>(city.Id);
            }
        }

        [EngineTest("PvP damage opens the combat window, which holds back turning the PvP flag off until it closes", Category = Category, TimeoutSeconds = 40f)]
        public static async Task CombatWindowHoldsBackTheFlagOff(EngineTestContext ctx)
        {
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            using var attacker = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            OptIn(target);
            ctx.Assert(!PvpCombatWindow.IsOpen(target.Creature), "no window is open before a fight");

            await ctx.ExecuteInCreatureContextAsync(attacker.Creature, () =>
                Combat.ApplyTriggeredDamage(attacker.Creature, target.Creature, 1, CombatDamageType.Physical));
            ctx.Assert(PvpCombatWindow.IsOpen(target.Creature), "PvP damage taken opens the target's window");
            ctx.Assert(PvpCombatWindow.IsOpen(attacker.Creature), "and the attacker's");

            await ctx.ExecuteInCreatureContextAsync(target.Creature, () =>
                PlayerList.SetFlag(target.Creature, PlayerListFlagType.PvP, false));
            ctx.Assert(PlayerList.GetFlags(target.Creature).Contains(PlayerListFlagType.PvP), "the flag stays in effect during the window");
            ctx.Assert(DB.Get<Player>(target.Id).PvpCombatRequests.IsFlagOffQueued, "the request is saved");

            PvpCombatWindow.CloseForTesting(target.Id);
            await ctx.ExecuteInCreatureContextAsync(target.Creature, () => PvpCombatWindow.ApplyQueued(target.Creature));
            ctx.Assert(!PlayerList.GetFlags(target.Creature).Contains(PlayerListFlagType.PvP), "the flag turns off once the window has closed");
            ctx.Assert(!DB.Get<Player>(target.Id).PvpCombatRequests.HasAny, "and nothing is left waiting");
        }

        /// <summary>The target shows the PvP flag and has accepted the bounty notice, as a player who agreed to bounties would.</summary>
        private static void OptIn(PlayerAbilityFixture target)
        {
            target.Update(player =>
            {
                player.BountyNoticeAcceptedUtc = DateTime.UtcNow;
                player.PlayerListFlags = new System.Collections.Generic.List<PlayerListFlagType> { PlayerListFlagType.PvP };
            });
        }
    }
}
