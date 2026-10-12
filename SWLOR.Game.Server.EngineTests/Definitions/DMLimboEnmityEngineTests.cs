using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DMLimboEnmityEngineTests
    {
        private const byte DungeonMasterLimboMinor = 0x26;

        [EngineTest("DM limbo removes the creature from every enmity table", Category = "DMLimbo", TimeoutSeconds = 45f)]
        public static async Task LimboClearsEnmity(EngineTestContext ctx)
        {
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx, 6f);
            var limboed = ctx.SpawnCreature("civilian", 2f);
            var bystander = ctx.SpawnCreature("civilian", -2f);
            await ctx.DelaySecondsAsync(1f);

            try
            {
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                {
                    Enmity.ModifyEnmity(bystander, limboed, 50);
                    Enmity.ModifyEnmity(limboed, bystander, 50);
                    ctx.Assert(Enmity.GetEnmityTable(bystander).ContainsKey(limboed), "setup puts the creature on the bystander's enmity table");

                    SendLimbo(ctx, dm.Creature, limboed);

                    AssertInLimbo(ctx, limboed);
                    ctx.AssertEqual(0, Enmity.GetEnmityTable(limboed).Count, "limboed creature's enmity table size");
                    ctx.Assert(!Enmity.GetEnmityTable(bystander).ContainsKey(limboed), "limboed creature is removed from the bystander's enmity table");
                });
            }
            finally
            {
                await DestroyLimboedAsync(ctx, dm.Creature, limboed);
            }
        }

        [EngineTest("DM group limbo clears every target's enmity and keeps unrelated enmity", Category = "DMLimbo", TimeoutSeconds = 45f)]
        public static async Task GroupLimboClearsOnlyTargetEnmity(EngineTestContext ctx)
        {
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx, 6f);
            var first = ctx.SpawnCreature("civilian", 2f);
            var second = ctx.SpawnCreature("civilian", 2f, 2f);
            var bystander = ctx.SpawnCreature("civilian", -2f);
            var unrelated = ctx.SpawnCreature("civilian", -2f, 2f);
            await ctx.DelaySecondsAsync(1f);

            try
            {
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                {
                    foreach (var target in new[] { first, second })
                    {
                        Enmity.ModifyEnmity(bystander, target, 50);
                        Enmity.ModifyEnmity(target, bystander, 50);
                    }
                    Enmity.ModifyEnmity(unrelated, bystander, 30);

                    SendLimbo(ctx, dm.Creature, first, second);

                    foreach (var target in new[] { first, second })
                    {
                        AssertInLimbo(ctx, target);
                        ctx.AssertEqual(0, Enmity.GetEnmityTable(target).Count, "limboed creature's enmity table size");
                        ctx.Assert(!Enmity.GetEnmityTable(bystander).ContainsKey(target), "limboed creature is removed from the bystander's enmity table");
                    }
                    ctx.AssertEqual(30, Enmity.GetEnmityTable(bystander).GetValueOrDefault(unrelated), "enmity from a creature that was not limboed");
                });
            }
            finally
            {
                await DestroyLimboedAsync(ctx, dm.Creature, first, second);
            }
        }

        // Limboed creatures are outside the arena, so destroy them from a live creature's context.
        private static Task DestroyLimboedAsync(EngineTestContext ctx, uint dm, params uint[] creatures)
            => ctx.ExecuteInCreatureContextAsync(dm, () =>
            {
                foreach (var creature in creatures)
                    DestroyObject(creature);
            });

        private static void AssertInLimbo(EngineTestContext ctx, uint creature)
        {
            ctx.Assert(GetIsObjectValid(creature), "limboed creature is still valid");
            ctx.AssertEqual(OBJECT_INVALID, GetArea(creature), "limboed creature's area");
            ctx.Assert(NWNXLib.g_pAppManager.m_pServerExoApp.GetModule().IsObjectInLimbo(creature) != 0, "creature is in the module's limbo list");
        }

        /// <summary>
        /// Delivers a DM client's limbo message through the engine handler that NWNX hooks for the limbo events.
        /// </summary>
        private static unsafe void SendLimbo(EngineTestContext ctx, uint dm, params uint[] targets)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var player = server.GetClientObjectByObjectId(dm);
            ctx.Assert(player != null && player.Pointer != IntPtr.Zero, "DM fixture has a registered client");
            var info = server.GetNetLayer().GetPlayerInfo(player.m_nPlayerID);
            ctx.Assert(info != null && info.Pointer != IntPtr.Zero, "DM fixture has a net layer slot");

            // Client payload: length (byte section + 3), target count when grouped, object IDs, one fragment byte.
            var isGroup = targets.Length > 1;
            var words = 1 + (isGroup ? 1 : 0) + targets.Length;
            var size = (uint)(words * sizeof(uint) + 1);
            var payload = (uint*)NativeMemory.AllocZeroed(size);
            var next = 0;
            payload[next++] = (uint)(words * sizeof(uint) + 3);
            if (isGroup)
                payload[next++] = (uint)targets.Length;
            foreach (var target in targets)
                payload[next++] = target;

            // The pinned engine's CNWSMessage is 0x70 bytes; NWNX_SWIG_DotNET's constructor allocates 0x68.
            var program = NativeLibrary.GetMainProgramHandle();
            var construct = (delegate* unmanaged<void*, void>)NativeLibrary.GetExport(program, "_ZN11CNWSMessageC1Ev");
            var destruct = (delegate* unmanaged<void*, void>)NativeLibrary.GetExport(program, "_ZN11CNWSMessageD1Ev");
            var memory = NativeMemory.AllocZeroed(0x70);
            var previousPrivileges = info.m_bGameMasterPrivileges;
            try
            {
                construct(memory);
                using var message = CNWSMessage.FromPointer(memory);
                try
                {
                    info.m_bGameMasterPrivileges = 1;
                    message.SetReadMessage((byte*)payload, size, 0xffffffff, 1);
                    var handled = message.HandlePlayerToServerDungeonMasterMessage(player, DungeonMasterLimboMinor, isGroup ? 1 : 0);
                    ctx.Assert(handled != 0, "engine consumed the whole DM limbo message");
                }
                finally
                {
                    info.m_bGameMasterPrivileges = previousPrivileges;
                    destruct(memory);
                }
            }
            finally
            {
                NativeMemory.Free(memory);
                NativeMemory.Free(payload);
            }
        }
    }
}
