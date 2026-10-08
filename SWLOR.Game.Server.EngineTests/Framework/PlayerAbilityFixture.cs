using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Headless player creature with persisted build state. Exercises GetIsPC and DB
    /// paths; it does not simulate a connected client, quickbar UI, or client input.
    /// </summary>
    public sealed class PlayerAbilityFixture : IDisposable
    {
        public uint Creature { get; }
        public string Id { get; }
        private readonly int _wasPlayer;
        private CNWSPlayer _client;
        private bool _recordWriteAttempted;
        private bool _clientRegistrationAttempted;
        private bool _disposed;

        internal enum IdentitySetupStage
        {
            RecordPersisted,
            PlayerFlagApplied,
            ClientCreated,
            ClientRegistered,
        }

        private sealed class WorldPlayerCreature : CNWSCreature
        {
            public WorldPlayerCreature() : base(OBJECT_INVALID, 0, 1) { }
            public void TransferOwnershipToServer() => swigCMemOwn = false;
        }

        private unsafe PlayerAbilityFixture(uint creature)
        {
            Creature = creature;
            Id = GetObjectUUID(creature);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
            _wasPlayer = native.m_bPlayerCharacter;
        }

        /// <summary>
        /// Installs persistence and native identity inside CreateAsync's rollback scope.
        /// Stage callbacks let engine tests interrupt partial setup before arena entry.
        /// </summary>
        private unsafe void InstallIdentity(Action<PlayerAbilityFixture, IdentitySetupStage> afterStage)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(Creature).AsNWSCreature();
            var record = new Player(Id)
            {
                Name = "Engine test player", RebuildComplete = true,
                CharacterType = Enumeration.CharacterType.ForceSensitive,
                MaxHP = 1000, HP = 1000, MaxFP = 10000, FP = 9999,
                MaxStamina = 10000, Stamina = 9999,
                Attack = 150, Accuracy = 150, ForceAttack = 150, Evasion = 100,
                TotalSPAcquired = Skill.TotalSkillPointCap - Skill.StartingSkillPoints,
            };
            foreach (var skill in Enum.GetValues<SkillType>())
                record.Skills[skill] = new PlayerSkill();
            foreach (var skill in new[] { SkillType.Mimicry, SkillType.BeastMastery, SkillType.FirstAid, SkillType.Force, SkillType.Vibroblade })
                record.Skills[skill].Rank = 50;
            foreach (var stat in record.BaseStats.Keys.ToArray())
                record.BaseStats[stat] = GetAbilityScore(Creature, stat);
            record.Perks[PerkType.CombatAnalyzer] = 1;
            record.Perks[PerkType.AnalyzerMemory] = 1;
            record.Perks[PerkType.Tame] = 5;
            // DB.Set writes the search index before JSON, so even a failed write needs cleanup.
            _recordWriteAttempted = true;
            DB.Set(record);
            afterStage?.Invoke(this, IdentitySetupStage.RecordPersisted);
            native.m_bPlayerCharacter = 1;
            afterStage?.Invoke(this, IdentitySetupStage.PlayerFlagApplied);
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            uint clientId = 1;
            while (server.GetClientObjectByPlayerId(clientId) is { } existing && existing.Pointer != nint.Zero)
                clientId++;
            _client = new CNWSPlayer(clientId);
            afterStage?.Invoke(this, IdentitySetupStage.ClientCreated);
            _client.SetGameObject(server.GetGameObject(Creature).AsNWSObject());
            _client.m_oidPCObject = Creature;
            _client.m_bIsPrimaryPlayer = 1;
            _clientRegistrationAttempted = true;
            server.GetPlayerList().Add(_client);
            afterStage?.Invoke(this, IdentitySetupStage.ClientRegistered);
        }

        /// <summary>
        /// Enters the arena and installs a persisted native player identity, rolling back
        /// identity if any setup or arena-entry step fails.
        /// </summary>
        public static Task<PlayerAbilityFixture> CreateAsync(EngineTestContext context, float xOffset = 0f)
            => CreateAsync(context, xOffset, null);

        /// <summary>
        /// Creates a fixture with interruption points for partial-setup cleanup checks.
        /// </summary>
        internal static async Task<PlayerAbilityFixture> CreateAsync(EngineTestContext context, float xOffset,
            Action<PlayerAbilityFixture, IdentitySetupStage> afterStage)
        {
            var template = context.SpawnCreature("civilian", xOffset);
            // OnSpawn's delayed stat initialization must finish before PC state is installed.
            await context.DelaySecondsAsync(1f);
            var creature = OBJECT_INVALID;
            await context.ExecuteInCreatureContextAsync(template, () =>
                creature = LoadPlayerCreature(context, template));
            context.Track(creature);
            context.Assert(creature < OBJECT_INVALID, "player fixture uses a live world object ID, not a saved-character object ID");
            context.SuppressNPCNaturalRegen(creature);
            var fixture = new PlayerAbilityFixture(creature);
            try
            {
                // AddToArea holds a creature with a registered client out of the area until that
                // client reports the area loaded, which a headless client never does.
                await context.ExecuteInCreatureContextAsync(template, () => EnterArena(context, creature, template));
                context.Assert(IsListedInArena(context, creature), "fixture is in the arena's native object list");
                fixture.InstallIdentity(afterStage);
                context.Assert(GetIsPC(creature), "fixture follows the native player branch");
                DestroyObject(template);
                await context.WaitFrameAsync();
                return fixture;
            }
            catch (Exception setupFailure)
            {
                try { fixture.Dispose(); }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException("Player fixture setup and identity cleanup both failed.", setupFailure, cleanupFailure);
                }
                throw;
            }
        }

        private static unsafe uint LoadPlayerCreature(EngineTestContext context, uint template)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var native = server.GetGameObject(template).AsNWSCreature();
            using var file = new CResGFF();
            using var root = new CResStruct();
            using var fileType = new CExoString("BIC ");
            using var format = new CExoString("V2.0");
            context.Assert(file.CreateGFFFile(root, fileType, format) != 0, "create the native player export");
            context.Assert(native.SaveCreature(file, root, 0, 0, 1, 0) != 0, "export the player fixture template");
            fixed (byte* label = Encoding.ASCII.GetBytes("IsPC\0"))
                file.WriteFieldBYTE(root, 1, label);
            using var player = new WorldPlayerCreature();
            context.Assert(player.LoadCreature(file, root, 0, 0, 0, 0) != 0, "load the creature with native PC inventory semantics");
            // LoadCreature restores the template's area ID, which makes AddToArea skip the area's
            // object list. The engine corrupts that list's heap block when such a creature moves.
            player.SetArea(null);
            // IsPC is for player inventory loading. Until InstallIdentity the creature is an NPC;
            // Dispose restores that, and the engine ignores DestroyObject on player-flagged creatures.
            player.m_bPlayerCharacter = 0;
            // The area's normal DestroyObject/DestroyArea cleanup owns the live creature.
            player.TransferOwnershipToServer();
            return player.m_idSelf;
        }

        private static unsafe void EnterArena(EngineTestContext context, uint creature, uint template)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var position = GetPosition(template);
            // No area-enter event: the fixture is placed in the arena, not a player arriving.
            server.GetGameObject(creature).AsNWSCreature().AddToArea(
                server.GetGameObject(context.Arena).AsNWSArea(), position.X, position.Y, position.Z, 0, 0);
        }

        internal static bool IsListedInArena(EngineTestContext context, uint creature)
        {
            for (var obj = GetFirstObjectInArea(context.Arena); GetIsObjectValid(obj); obj = GetNextObjectInArea(context.Arena))
            {
                if (obj == creature)
                    return true;
            }

            return false;
        }

        public void Update(Action<Player> update)
        {
            var record = DB.Get<Player>(Id);
            update(record);
            DB.Set(record);
        }

        /// <summary>
        /// Releases completed or partial identity setup once. Every cleanup stage is
        /// attempted even if an earlier stage fails; the arena context owns the creature.
        /// </summary>
        public unsafe void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            try
            {
                try
                {
                    if (_clientRegistrationAttempted)
                        NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Remove(_client);
                }
                finally
                {
                    if (_client != null)
                    {
                        _client.m_oidPCObject = OBJECT_INVALID;
                        _client.Dispose();
                        _client = null;
                    }
                }
            }
            finally
            {
                try
                {
                    if (GetIsObjectValid(Creature))
                        NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(Creature).AsNWSCreature().m_bPlayerCharacter = _wasPlayer;
                }
                finally
                {
                    if (_recordWriteAttempted)
                        DB.Delete<Player>(Id);
                }
            }
        }
    }
}
