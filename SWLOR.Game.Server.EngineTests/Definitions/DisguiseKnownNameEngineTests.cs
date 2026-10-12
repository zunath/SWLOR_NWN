using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DisguiseKnownNameEngineTests
    {
        private const string RealName = "Vessa Tal";
        private const string OnlineConflict = "You already use that name for another character who is online.";

        [EngineTest("A disguise and its owner can share an observer's label", Category = "PlayerName", TimeoutSeconds = 45f)]
        public static async Task DisguiseAndOwnerCanShareLabel(EngineTestContext ctx)
        {
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            using var bystander = await PlayerAbilityFixture.CreateAsync(ctx, -3f);
            PlayerDisguise disguise = null;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(target.Creature, () =>
                    disguise = Disguise.CreateDisguise(target.Creature));
                ctx.Assert(disguise != null, "target can create a disguise");
                await ActivateAsync(ctx, target, disguise);

                ctx.AssertEqual(string.Empty, await NameAsync(ctx, observer, target, RealName),
                    "label the worn disguise with the owner's real name");
                ctx.AssertEqual(Disguise.BuildIdentityKey(disguise.Id), Disguise.GetIdentityKey(target.Creature),
                    "label is stored on the disguise identity");

                await DeactivateAsync(ctx, target);
                ctx.AssertEqual(string.Empty, await NameAsync(ctx, observer, target, RealName),
                    "label the undisguised owner with the disguise's label");
                AssertKnownName(ctx, observer, target, RealName, "undisguised owner label");

                Disguise.ResetActivationCooldowns(target.Creature);
                await ActivateAsync(ctx, target, disguise);
                AssertKnownName(ctx, observer, target, RealName, "disguise keeps its own label");
                ctx.AssertEqual(string.Empty, await NameAsync(ctx, observer, target, "Masked Courier"),
                    "relabel the worn disguise");
                ctx.AssertEqual(string.Empty, await NameAsync(ctx, observer, target, RealName),
                    "label the worn disguise with the owner's existing label");

                ctx.AssertEqual(OnlineConflict, await NameAsync(ctx, observer, bystander, RealName),
                    "label held by the online disguise");
                await DeactivateAsync(ctx, target);
                ctx.AssertEqual(OnlineConflict, await NameAsync(ctx, observer, bystander, RealName),
                    "label held by the online owner");
                ctx.Assert(!PlayerName.TryGetKnownName(observer.Creature, bystander.Creature, out _),
                    "rejected label is not saved");
            }
            finally
            {
                // The fixtures delete their Player records, which hold the active disguise state.
                if (disguise != null)
                    DB.Delete<PlayerDisguise>(disguise.Id);

                foreach (var knownNames in DB.Search(new DBQuery<PlayerKnownName>()
                             .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observer.Id, false)).ToList())
                    DB.Delete<PlayerKnownName>(knownNames.Id);
            }
        }

        private static async Task<string> NameAsync(EngineTestContext ctx, PlayerAbilityFixture observer,
            PlayerAbilityFixture target, string name)
        {
            var error = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
            {
                error = PlayerName.ValidateKnownNameAssignment(observer.Creature, target.Creature, name);
                if (string.IsNullOrEmpty(error))
                    PlayerName.SetKnownName(observer.Creature, target.Creature, name);
            });
            return error;
        }

        private static async Task ActivateAsync(EngineTestContext ctx, PlayerAbilityFixture target, PlayerDisguise disguise)
        {
            var error = "not run";
            await ctx.ExecuteInCreatureContextAsync(target.Creature, () =>
                error = Disguise.Activate(target.Creature, disguise.Id).ErrorMessage);
            ctx.AssertEqual(string.Empty, error, "disguise activation");
        }

        private static async Task DeactivateAsync(EngineTestContext ctx, PlayerAbilityFixture target)
        {
            var deactivated = false;
            await ctx.ExecuteInCreatureContextAsync(target.Creature, () =>
                deactivated = Disguise.Deactivate(target.Creature));
            ctx.Assert(deactivated, "disguise deactivates");
            ctx.AssertEqual(target.Id, Disguise.GetIdentityKey(target.Creature), "owner identity after deactivation");
        }

        private static void AssertKnownName(EngineTestContext ctx, PlayerAbilityFixture observer,
            PlayerAbilityFixture target, string expected, string label)
        {
            ctx.Assert(PlayerName.TryGetKnownName(observer.Creature, target.Creature, out var knownName), label);
            ctx.AssertEqual(expected, knownName, label);
        }
    }
}
