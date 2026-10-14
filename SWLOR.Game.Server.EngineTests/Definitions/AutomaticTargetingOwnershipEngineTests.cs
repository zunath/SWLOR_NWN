using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NWN.Native.API;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class AutomaticTargetingOwnershipEngineTests
{
    private const FeatType Support = FeatType.RousingShout1;
    private const FeatType Enemy = FeatType.ForceDrain1;
    private const FeatType Technique = FeatType.ToxicSpitTechnique;

    [EngineTest("Automatic targeting toggles preserve canonical perk ownership and level history", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task TogglesAndPerkRanks(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        PreparePerks(fixture);
        Perk.RestorePlayerFeats(player);
        CreaturePlugin.AddFeat(player, FeatType.Alertness);
        PlayerPlugin.SetQuickBarSlot(player, 17, PlayerQuickBarSlot.UseFeat(Support));
        PlayerPlugin.SetQuickBarSlot(player, 18, PlayerQuickBarSlot.UseFeat(Enemy));
        PlayerPlugin.SetQuickBarSlot(player, 19, PlayerQuickBarSlot.UseSkill(0));
        var unrelated = JsonConvert.SerializeObject(PlayerPlugin.GetQuickBarSlot(player, 19));
        var originalLevelCount = CreaturePlugin.GetFeatCountByLevel(player, 1);

        for (var cycle = 1; cycle <= 3; cycle++)
        {
            foreach (var enabled in new[] { true, false })
            {
                SetPreferences(player, enabled, enabled);
                Perk.RestorePlayerFeats(player);
                Perk.RestorePlayerFeats(player);
                AssertOwnership(ctx, player, Support, enabled, $"cycle {cycle}: support");
                AssertOwnership(ctx, player, Enemy, enabled, $"cycle {cycle}: enemy");
                AssertSlot(ctx, player, 17, enabled ? Alias(Support) : Support);
                AssertSlot(ctx, player, 18, enabled ? Alias(Enemy) : Enemy);
                ctx.AssertEqual(originalLevelCount, CreaturePlugin.GetFeatCountByLevel(player, 1), "toggles and login repair do not append level feats");
                AssertCanonicalLevelRow(ctx, player, Support, 1);
                AssertCanonicalLevelRow(ctx, player, Enemy, 1);
                ctx.Assert(CreaturePlugin.GetKnowsFeat(player, FeatType.Alertness), "unrelated feat remains owned");
                ctx.AssertEqual(unrelated, JsonConvert.SerializeObject(PlayerPlugin.GetQuickBarSlot(player, 19)), "unrelated shortcut remains unchanged");
            }
            await ctx.WaitFrameAsync();
        }

        SetPreferences(player, true, false);
        foreach (var rank in new[] { 2, 3 })
        {
            fixture.Update(record => record.Perks[PerkType.RousingShout] = rank);
            Perk.SyncGrantedFeats(player, PerkType.RousingShout, rank, true);
            var current = rank == 2 ? FeatType.RousingShout2 : FeatType.RousingShout3;
            AssertOwnership(ctx, player, current, true, $"rank {rank}");
            foreach (var previous in new[] { Support, FeatType.RousingShout2 }.Where(feat => feat != current))
                ctx.Assert(!QuickCast.KnowsFeat(player, previous), $"rank {rank}: superseded {previous} is revoked in both forms");
            var count = CreaturePlugin.GetFeatCountByLevel(player, 1);
            Perk.SyncGrantedFeats(player, PerkType.RousingShout, rank, true);
            ctx.AssertEqual(count, CreaturePlugin.GetFeatCountByLevel(player, 1), "repeated rank sync does not append another level row");
            AssertCanonicalLevelRow(ctx, player, current, 1);
        }

        fixture.Update(record => record.Perks.Remove(PerkType.RousingShout));
        Perk.SyncGrantedFeats(player, PerkType.RousingShout, 0, true);
        SetPreferences(player, false, false);
        foreach (var feat in new[] { Support, FeatType.RousingShout2, FeatType.RousingShout3 })
            ctx.Assert(!QuickCast.KnowsFeat(player, feat), $"rank-zero revocation cannot be undone by disabling targeting: {feat}");
        AssertOwnership(ctx, player, Enemy, false, "unrelated perk after revocation");
        ctx.SetResultDetail("Three toggle cycles and repeated production perk restoration preserve canonical level rows; rank upgrades and rank-zero revocation remove both native variants.");
    }

    [EngineTest("Saved native and space quickbars normalize to current targeting preferences", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task SavedQuickbarNormalization(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        PreparePerks(fixture);
        Perk.RestorePlayerFeats(player);
        PlayerPlugin.SetQuickBarSlot(player, 17, PlayerQuickBarSlot.UseFeat(Support));
        PlayerPlugin.SetQuickBarSlot(player, 18, PlayerQuickBarSlot.UseFeat(Enemy));
        PlayerPlugin.SetQuickBarSlot(player, 19, PlayerQuickBarSlot.UseSkill(0));
        var unrelated = JsonConvert.SerializeObject(PlayerPlugin.GetQuickBarSlot(player, 19));

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            foreach (var savedEnabled in new[] { true, false })
            {
                SetPreferences(player, savedEnabled, savedEnabled);
                using var saved = new NativeSnapshot(ctx, player, false);
                var spaceSnapshot = CreaturePlugin.SerializeQuickbar(player);
                ctx.Assert(!string.IsNullOrEmpty(spaceSnapshot), "space quickbar serialization produces a snapshot");
                SetPreferences(player, !savedEnabled, !savedEnabled);
                PlayerPlugin.SetQuickBarSlot(player, 17, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                saved.LoadQuickbar(player);
                AssertSlot(ctx, player, 17, savedEnabled ? Support : Alias(Support));
                AssertSlot(ctx, player, 18, savedEnabled ? Enemy : Alias(Enemy));
                ctx.AssertEqual(unrelated, JsonConvert.SerializeObject(PlayerPlugin.GetQuickBarSlot(player, 19)), "native load preserves unrelated slot data");

                PlayerPlugin.SetQuickBarSlot(player, 17, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                ctx.Assert(CreaturePlugin.DeserializeQuickbar(player, spaceSnapshot), "space quickbar snapshot reloads through NWNX");
                AssertSlot(ctx, player, 17, savedEnabled ? Support : Alias(Support));
                AssertSlot(ctx, player, 18, savedEnabled ? Enemy : Alias(Enemy));
                ctx.AssertEqual(unrelated, JsonConvert.SerializeObject(PlayerPlugin.GetQuickBarSlot(player, 19)), "space load preserves unrelated slot data");
                AssertOwnership(ctx, player, Support, !savedEnabled, "loaded support ownership");
                AssertOwnership(ctx, player, Enemy, !savedEnabled, "loaded enemy ownership");
            }
            await ctx.WaitFrameAsync();
        }
        ctx.SetResultDetail("Two cycles through native SaveQuickButtons/LoadQuickButtons and NWNX space snapshots normalize both alias-to-original and original-to-alias without an explicit post-load ApplyPreferences call.");
    }

    [EngineTest("Automatic targeting survives two native BIC player-login cycles", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task CharacterLoginRoundTrips(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        PreparePerks(fixture);
        SetPreferences(fixture.Creature, true, true);
        Perk.RestorePlayerFeats(fixture.Creature);
        CreaturePlugin.AddFeat(fixture.Creature, FeatType.Alertness);
        PlayerPlugin.SetQuickBarSlot(fixture.Creature, 17, PlayerQuickBarSlot.UseFeat(Alias(Support)));
        PlayerPlugin.SetQuickBarSlot(fixture.Creature, 18, PlayerQuickBarSlot.UseFeat(Alias(Enemy)));
        PlayerPlugin.SetQuickBarSlot(fixture.Creature, 19, PlayerQuickBarSlot.UseSkill(0));
        var unrelated = JsonConvert.SerializeObject(PlayerPlugin.GetQuickBarSlot(fixture.Creature, 19));
        var levelCount = CreaturePlugin.GetFeatCountByLevel(fixture.Creature, 1);
        var player = fixture.Creature;
        var id = fixture.Id;
        NativeLoginIdentity restored = null;
        try
        {
            for (var cycle = 1; cycle <= 2; cycle++)
            {
                using var saved = new NativeSnapshot(ctx, player, true);
                var persisted = ReadPersisted(id);
                var next = new NativeLoginIdentity(ctx, saved, persisted);
                restored?.Dispose();
                restored = next;
                player = restored.Creature;
                id = restored.Id;
                ctx.Assert(QuickCast.KnowsFeat(player, Support) && QuickCast.KnowsFeat(player, Enemy), "native player stats retain both purchased abilities before login repair");
                Perk.RestorePlayerFeats(player);
                QuickCast.ApplyPreferences(player);
                Perk.RestorePlayerFeats(player);
                AssertOwnership(ctx, player, Support, true, $"login cycle {cycle}: support");
                AssertOwnership(ctx, player, Enemy, true, $"login cycle {cycle}: enemy");
                AssertSlot(ctx, player, 17, Alias(Support));
                AssertSlot(ctx, player, 18, Alias(Enemy));
                ctx.AssertEqual(levelCount, CreaturePlugin.GetFeatCountByLevel(player, 1), "login restoration does not append level feats");
                AssertCanonicalLevelRow(ctx, player, Support, 1);
                AssertCanonicalLevelRow(ctx, player, Enemy, 1);
                ctx.Assert(CreaturePlugin.GetKnowsFeat(player, FeatType.Alertness), "native BIC reload preserves unrelated feats");
                ctx.AssertEqual(unrelated, JsonConvert.SerializeObject(PlayerPlugin.GetQuickBarSlot(player, 19)), "native BIC reload preserves unrelated shortcuts");
                SetPreferences(player, false, false);
                AssertOwnership(ctx, player, Support, false, "reloaded character can disable support targeting");
                AssertSlot(ctx, player, 17, Support);
                SetPreferences(player, true, true);
                await ctx.WaitFrameAsync();
                var record = ReadPersisted(id);
                ctx.Assert(record.Settings.SelfQuickCastEnabled && record.Settings.EnemyQuickCastEnabled, "both opt-ins remain persisted after reload and changes");
                ctx.AssertEqual(1, record.Perks[PerkType.RousingShout], "support perk ownership remains canonical");
                ctx.AssertEqual(1, record.Perks[PerkType.ForceDrain], "enemy perk ownership remains canonical");
            }
        }
        finally { restored?.Dispose(); }
        ctx.SetResultDetail("Real SaveCreature BIC exports load into fresh native stats through ReadStatsFromGff, with fresh PC identities and Redis-roundtripped records on both cycles; production login restoration preserves feats, canonical level rows and quickbars.");
    }

    [EngineTest("Mimicry targeting aliases retain canonical loadouts and revoke cleanly", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task MimicryOwnership(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var record = DB.Get<Player>(fixture.Id);
        record.LearnedTechniques[Technique] = DateTime.UtcNow;
        MimicryLoadouts.EnsureStartingBuild(record);
        var empty = record.MimicryLoadouts.Single().Id;
        ctx.Assert(MimicryLoadouts.Save(record, null, "Targeted", new[] { Technique }, out var targeted, out var saveError), $"save technique build: {saveError}");
        DB.Set(record);
        var levelCount = CreaturePlugin.GetFeatCountByLevel(player, 1);
        PlayerPlugin.SetQuickBarSlot(player, 19, PlayerQuickBarSlot.UseSkill(0));

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            SetPreferences(player, false, true);
            ctx.Assert(Mimicry.ApplyLoadout(player, targeted.Id, new[] { Technique }, out var applyError), $"apply targeted build: {applyError}");
            AssertOwnership(ctx, player, Technique, true, $"Mimicry cycle {cycle}");
            PlayerPlugin.SetQuickBarSlot(player, 17, PlayerQuickBarSlot.UseFeat(Alias(Technique)));
            ctx.Assert(Mimicry.ApplyLoadout(player, empty, Array.Empty<FeatType>(), out var emptyError), $"apply empty build: {emptyError}");
            ctx.Assert(!QuickCast.KnowsFeat(player, Technique), "unequipped technique loses both native variants");
            ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(player, 17).ObjectType, "alias shortcut is removed on unequip");
            await ctx.WaitFrameAsync();
            record = ReadPersisted(fixture.Id);
            var saved = record.MimicryLoadouts.Single(loadout => loadout.Id == targeted.Id);
            ctx.AssertEqual(Technique, saved.HotbarSlots[17], "captured alias shortcut persists its canonical technique ID");
            ctx.Assert(record.LearnedTechniques.ContainsKey(Technique) && !record.LearnedTechniques.Keys.Any(QuickCastFeatMap.IsAlias), "learned ownership remains canonical");
            ctx.Assert(record.MimicryLoadouts.SelectMany(loadout => loadout.Techniques).All(feat => !QuickCastFeatMap.IsAlias(feat)), "all saved builds use canonical IDs");
            DB.Set(record);
            ctx.Assert(Mimicry.ApplyLoadout(player, targeted.Id, new[] { Technique }, out applyError), $"restore targeted build: {applyError}");
            AssertSlot(ctx, player, 17, Alias(Technique));
            SetPreferences(player, false, false);
            AssertOwnership(ctx, player, Technique, false, "equipped technique follows disabled targeting");
            AssertSlot(ctx, player, 17, Technique);
            ctx.Assert(Mimicry.UnequipTechnique(player, Technique), "production single-technique unequip succeeds");
            ctx.Assert(!QuickCast.KnowsFeat(player, Technique), "single-technique unequip revokes canonical ownership");
            ctx.AssertEqual(0, DB.Get<Player>(fixture.Id).EquippedTechniques.Count, "unequip clears the canonical equipped list");
            ctx.AssertEqual(levelCount, CreaturePlugin.GetFeatCountByLevel(player, 1), "technique toggles and loadouts do not create level-history entries");
            ctx.AssertEqual(QuickBarSlotType.Skill, PlayerPlugin.GetQuickBarSlot(player, 19).ObjectType, "unrelated shortcut survives Mimicry changes");
        }
        ctx.SetResultDetail("Two production ApplyLoadout/capture/restore/UnequipTechnique cycles keep learned, equipped and saved-build IDs canonical while alias ownership and duplicate shortcuts follow targeting preferences.");
    }

    private static void PreparePerks(PlayerAbilityFixture fixture)
        => fixture.Update(record =>
        {
            record.Perks.Clear();
            record.Perks[PerkType.RousingShout] = 1;
            record.Perks[PerkType.ForceDrain] = 1;
        });

    private static void SetPreferences(uint player, bool self, bool enemy)
    {
        var record = DB.Get<Player>(GetObjectUUID(player));
        record.Settings.SelfQuickCastEnabled = self;
        record.Settings.EnemyQuickCastEnabled = enemy;
        DB.Set(record);
        QuickCast.ApplyPreferences(player);
    }

    private static Player ReadPersisted(string id) => JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(id));

    private static FeatType Alias(FeatType original)
        => QuickCastFeatMap.TryGetAlias(original, out var alias) ? alias : throw new InvalidOperationException($"No targeting alias for {original}.");

    private static void AssertOwnership(EngineTestContext ctx, uint player, FeatType original, bool automatic, string label)
    {
        ctx.Assert(QuickCast.KnowsFeat(player, original), $"{label}: canonical ability is known");
        ctx.AssertEqual(!automatic, CreaturePlugin.GetKnowsFeat(player, original), $"{label}: original native ownership");
        ctx.AssertEqual(automatic, CreaturePlugin.GetKnowsFeat(player, Alias(original)), $"{label}: alias native ownership");
    }

    private static void AssertSlot(EngineTestContext ctx, uint player, int index, FeatType feat)
    {
        var slot = PlayerPlugin.GetQuickBarSlot(player, index);
        ctx.AssertEqual(QuickBarSlotType.Feat, slot.ObjectType, $"slot {index} remains a feat shortcut");
        ctx.AssertEqual((int)feat, slot.INTParam1, $"slot {index} feat ID");
    }

    private static void AssertCanonicalLevelRow(EngineTestContext ctx, uint player, FeatType feat, int expected)
    {
        var rows = Enumerable.Range(0, CreaturePlugin.GetFeatCountByLevel(player, 1))
            .Select(index => CreaturePlugin.GetFeatByLevel(player, 1, index)).ToList();
        ctx.AssertEqual(expected, rows.Count(candidate => candidate == feat), $"canonical {feat} level-row count");
        ctx.Assert(!rows.Any(QuickCastFeatMap.IsAlias), "level history contains no presentation aliases");
    }

    private sealed class NativeSnapshot : IDisposable
    {
        public CResGFF File { get; } = new();
        public CResStruct Root { get; } = new();

        public unsafe NativeSnapshot(EngineTestContext ctx, uint player, bool character)
        {
            try
            {
                using var type = new CExoString("BIC ");
                using var format = new CExoString("V2.0");
                ctx.Assert(File.CreateGFFFile(Root, type, format) != 0, "create native snapshot");
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
                if (character)
                    ctx.Assert(native.SaveCreature(File, Root, 0, 0, 1, 0) != 0, "save a native player BIC");
                else
                    native.SaveQuickButtons(File, Root);
            }
            catch { Dispose(); throw; }
        }

        public unsafe void LoadQuickbar(uint player)
            => NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature().LoadQuickButtons(File, Root);

        public void Dispose()
        {
            Root.Dispose();
            File.Dispose();
        }
    }

    // These receivers never enter an area or take actions. Native ownership stays with
    // this scope; only their fake player identity is registered, as in PlayerAbilityFixture.
    private sealed class NativeLoginIdentity : IDisposable
    {
        private readonly CNWSCreature _native;
        private CNWSPlayer _client;
        private bool _recordAttempted;
        private bool _registered;
        private bool _disposed;
        public uint Creature => _native.m_idSelf;
        public string Id { get; private set; }

        public unsafe NativeLoginIdentity(EngineTestContext ctx, NativeSnapshot saved, Player persisted)
        {
            _native = new CNWSCreature(OBJECT_INVALID, 0, 1);
            try
            {
                // Actual CNWSPlayer login reads stats directly rather than LoadCreature.
                ctx.AssertEqual(0u, _native.m_pStats.ReadStatsFromGff(saved.File, saved.Root, _native.m_cAppearance, 0, 1, 0, 1), "native player stats load succeeds");
                Id = GetObjectUUID(Creature);
                ctx.Assert(!string.IsNullOrWhiteSpace(Id) && Id != persisted.Id, "login receiver has a separate disposable database identity");
                persisted.Id = Id;
                _recordAttempted = true;
                DB.Set(persisted);
                _native.m_bPlayerCharacter = 1;
                var server = NWNXLib.g_pAppManager.m_pServerExoApp;
                uint clientId = 1;
                while (server.GetClientObjectByPlayerId(clientId) is { } existing && existing.Pointer != nint.Zero)
                    clientId++;
                _client = new CNWSPlayer(clientId);
                _client.SetGameObject(server.GetGameObject(Creature).AsNWSObject());
                _client.m_oidPCObject = Creature;
                _client.m_bIsPrimaryPlayer = 1;
                _registered = true;
                server.GetPlayerList().Add(_client);
                ctx.Assert(GetIsPC(Creature), "fresh native login receiver has PC identity");
                _native.LoadQuickButtons(saved.File, saved.Root);
            }
            catch { Dispose(); throw; }
        }

        public unsafe void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            try
            {
                if (_registered)
                    NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Remove(_client);
            }
            finally
            {
                try
                {
                    if (_client != null)
                    {
                        _client.m_oidPCObject = OBJECT_INVALID;
                        _client.Dispose();
                    }
                }
                finally
                {
                    try { if (_recordAttempted) DB.Delete<Player>(Id); }
                    finally
                    {
                        _native.m_bPlayerCharacter = 0;
                        _native.Dispose();
                    }
                }
            }
        }
    }
}
