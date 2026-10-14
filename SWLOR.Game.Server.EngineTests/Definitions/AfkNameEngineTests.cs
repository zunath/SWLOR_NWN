using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class AfkNameEngineTests
    {
        [EngineTest("AFK preserves private names, disguises and chat overrides", Category = "PlayerName", TimeoutSeconds = 45f)]
        public static async Task AfkPreservesIdentity(EngineTestContext ctx)
        {
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            using var bystander = await PlayerAbilityFixture.CreateAsync(ctx, -3f);
            var command = new AfkChatCommand().BuildChatCommands()["afk"];
            var snapshots = new List<object>();
            var suffix = " " + ColorToken.Purple("[AFK]");
            PlayerDisguise disguise = null;
            try
            {
                var targetRecord = DB.Get<Player>(target.Id);
                targetRecord.Settings.ShowOwnDescriptor = false;
                DB.Set(targetRecord);
                PlayerDescriptor.SetUnknownDisplayName(target.Creature, "A quiet traveler");
                PlayerDescriptor.SetUnknownDisplayName(bystander.Creature, "A quiet traveler");
                var canonicalName = GetName(target.Creature);
                var identityKey = Disguise.GetIdentityKey(target.Creature);
                ctx.AssertEqual(string.Empty, command.ValidateArguments(target.Creature), "command validation");

                await ToggleAsync();
                ctx.Assert(PlayerName.IsAfk(target.Creature), "first /afk enables status");
                AssertOverride("unknown", ColorToken.Gray("A quiet traveler") + suffix);
                ctx.AssertEqual(canonicalName + suffix, RenamePlugin.GetPCNameOverride(target.Creature, target.Creature), "self override");
                ctx.AssertEqual(PlayerName.GetDisplayName(observer.Creature, bystander.Creature),
                    PlayerName.GetDisplayName(observer.Creature, target.Creature), "AFK cannot disambiguate identical tell identities");
                ctx.AssertEqual(identityKey, Disguise.GetIdentityKey(target.Creature), "AFK cannot change identity");

                PlayerName.SetKnownName(observer.Creature, target.Creature, "Vessa Tal");
                AssertOverride("known", PlayerName.GetDisplayName(observer.Creature, target.Creature) + suffix);
                ctx.Assert(!string.IsNullOrEmpty(PlayerName.ValidateKnownNameAssignment(observer.Creature, bystander.Creature, "Vessa Tal")),
                    "AFK cannot bypass private-label uniqueness");
                PlayerDescriptor.SetUnknownDisplayName(target.Creature, "A seasoned traveler");
                AssertOverride("renamed", PlayerName.GetDisplayName(observer.Creature, target.Creature) + suffix);
                PlayerName.SendChatMessageWithChatNameOverride(observer.Creature, target.Creature, () =>
                    ctx.AssertEqual("Vessa Tal" + suffix, RenamePlugin.GetPCNameOverride(target.Creature, observer.Creature), "temporary chat override"));
                AssertOverride("chat-restored", PlayerName.GetDisplayName(observer.Creature, target.Creature) + suffix);
                PlayerName.SendChatMessageWithChatNameOverride(target.Creature, target.Creature, () =>
                    ctx.AssertEqual(canonicalName + suffix, RenamePlugin.GetPCNameOverride(target.Creature, target.Creature), "self chat override"));
                ctx.AssertEqual(canonicalName + suffix, RenamePlugin.GetPCNameOverride(target.Creature, target.Creature), "self chat restore");

                await ctx.ExecuteInCreatureContextAsync(target.Creature, () =>
                {
                    disguise = Disguise.CreateDisguise(target.Creature);
                    ctx.Assert(disguise != null, "create disguise");
                    disguise.Descriptor = "A hooded courier";
                    disguise.ScrambleAccountId = true;
                    DB.Set(disguise);
                    ctx.AssertEqual(string.Empty, Disguise.Activate(target.Creature, disguise.Id).ErrorMessage, "activate disguise while AFK");
                });
                AssertOverride("disguised-unknown", ColorToken.Gray("A hooded courier") + suffix);
                ctx.Assert(!PlayerName.TryGetKnownName(observer.Creature, target.Creature, out _), "owner label must not leak through disguise");
                PlayerName.SetKnownName(observer.Creature, target.Creature, "Masked Courier");
                AssertOverride("disguised-known", PlayerName.GetDisplayName(observer.Creature, target.Creature) + suffix);
                await ToggleAsync();
                ctx.Assert(!PlayerName.IsAfk(target.Creature), "second /afk disables status");
                AssertOverride("disguised-returned", PlayerName.GetDisplayName(observer.Creature, target.Creature));
                await ToggleAsync();
                await ctx.ExecuteInCreatureContextAsync(target.Creature, () =>
                    ctx.Assert(Disguise.Deactivate(target.Creature), "deactivate disguise while AFK"));
                AssertOverride("undisguised", PlayerName.GetDisplayName(observer.Creature, target.Creature) + suffix);
                ctx.Assert(PlayerName.TryGetKnownName(observer.Creature, target.Creature, out var knownName), "owner label retained");
                ctx.AssertEqual("Vessa Tal", knownName, "AFK is never saved in a label");
                ctx.AssertEqual("A seasoned traveler", DB.Get<Player>(target.Id).UnknownDisplayName, "AFK is never saved in a descriptor");
                ctx.AssertEqual(canonicalName, GetName(target.Creature), "canonical name retained");
                PlayerName.ForgetKnownName(observer.Creature, target.Creature);
                AssertOverride("forgotten", ColorToken.Gray("A seasoned traveler") + suffix);
                await ToggleAsync();
                AssertOverride("returned", ColorToken.Gray("A seasoned traveler"));

                var directory = ApplicationSettings.Get().EngineTestResultsDirectory;
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "afk-name-snapshots.json"), JsonSerializer.Serialize(snapshots));
                }
            }
            finally
            {
                if (PlayerName.IsAfk(target.Creature))
                    PlayerName.ToggleAfk(target.Creature);
                if (disguise != null)
                    DB.Delete<PlayerDisguise>(disguise.Id);
                foreach (var names in DB.Search(new DBQuery<PlayerKnownName>()
                             .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observer.Id, false)
                             .AddPaging(50, 0)).ToList())
                    DB.Delete<PlayerKnownName>(names.Id);
            }

            Task ToggleAsync() => ctx.ExecuteInCreatureContextAsync(target.Creature, () =>
                command.DoAction(target.Creature, OBJECT_INVALID, null));

            void AssertOverride(string label, string expected)
            {
                var actual = RenamePlugin.GetPCNameOverride(target.Creature, observer.Creature);
                ctx.AssertEqual(expected, actual, label);
                snapshots.Add(new { Label = label, Name = actual });
            }
        }
    }
}
