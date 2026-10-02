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
        private readonly CNWSPlayer _client;

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
                record.BaseStats[stat] = GetAbilityScore(creature, stat);
            record.Perks[PerkType.CombatAnalyzer] = 1;
            record.Perks[PerkType.AnalyzerMemory] = 1;
            record.Perks[PerkType.Tame] = 5;
            DB.Set(record);
            native.m_bPlayerCharacter = 1;
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            uint clientId = 1;
            while (server.GetClientObjectByPlayerId(clientId) is { } existing && existing.Pointer != nint.Zero)
                clientId++;
            _client = new CNWSPlayer(clientId);
            _client.SetGameObject(NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSObject());
            _client.m_oidPCObject = creature;
            _client.m_bIsPrimaryPlayer = 1;
            NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Add(_client);
        }

        public static async Task<PlayerAbilityFixture> CreateAsync(EngineTestContext context, float xOffset = 0f)
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
                context.Assert(GetIsPC(creature), "fixture follows the native player branch");
                await context.ExecuteInCreatureContextAsync(template, () => EnterArena(context, creature, template));
                DestroyObject(template);
                await context.WaitFrameAsync();
                return fixture;
            }
            catch
            {
                fixture.Dispose();
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
            // The area's normal DestroyObject/DestroyArea cleanup owns the live creature.
            player.TransferOwnershipToServer();
            return player.m_idSelf;
        }

        private static unsafe void EnterArena(EngineTestContext context, uint creature, uint template)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var position = GetPosition(template);
            server.GetGameObject(creature).AsNWSCreature().AddToArea(
                server.GetGameObject(context.Arena).AsNWSArea(), position.X, position.Y, position.Z);
        }

        public void Update(Action<Player> update)
        {
            var record = DB.Get<Player>(Id);
            update(record);
            DB.Set(record);
        }

        public unsafe void Dispose()
        {
            NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Remove(_client);
            _client.m_oidPCObject = OBJECT_INVALID;
            _client.Dispose();
            if (GetIsObjectValid(Creature))
                NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(Creature).AsNWSCreature().m_bPlayerCharacter = _wasPlayer;
            DB.Delete<Player>(Id);
        }
    }
}
