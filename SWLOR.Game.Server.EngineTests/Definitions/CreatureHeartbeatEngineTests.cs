using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class CreatureHeartbeatEngineTests
{
    private static unsafe FunctionHook* _commandHook;
    private static unsafe FunctionHook* _scriptHook;
    private static readonly List<string> Errors = new();
    private static readonly HashSet<string> ScriptsRun = new();
    private static uint _observedGuildmaster = OBJECT_INVALID;
    private static int _managedHeartbeatCalls;

    [NWNEventHandler(ScriptName.OnCreatureHeartbeatAfter)]
    public static void ObserveManagedHeartbeat()
    {
        if (OBJECT_SELF == _observedGuildmaster)
            _managedHeartbeatCalls++;
    }

    [EngineTest("Guildmaster configured heartbeat bypasses stock AI and restores resources", Category = "CreatureHeartbeat", TimeoutSeconds = 30f)]
    public static async Task ConfiguredGuildmasterHeartbeat(EngineTestContext ctx)
    {
        var guildmaster = ctx.SpawnCreature("eng_guildmaster");
        var ordinaryNpc = ctx.SpawnCreature("nw_rat001", 3f);
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(guildmaster, () =>
        {
            ctx.AssertEqual(ScriptName.OnCreatureHeartbeatAfter,
                GetEventScript(guildmaster, EventScript.Creature_OnHeartbeat),
                "The deployed Engineering Guild Master palette selects the managed heartbeat.");
            var adjust = typeof(Spawn).GetMethod("AdjustScripts", BindingFlags.Static | BindingFlags.NonPublic);
            ctx.Assert(adjust != null, "Spawn normalization is available to the regression fixture.");
            adjust.Invoke(null, new object[] { guildmaster });
            adjust.Invoke(null, new object[] { ordinaryNpc });
            ctx.AssertEqual(ScriptName.OnCreatureHeartbeatAfter,
                GetEventScript(guildmaster, EventScript.Creature_OnHeartbeat),
                "Respawn/DM-spawn normalization retains the managed heartbeat.");
            ctx.AssertEqual("x2_def_heartbeat", GetEventScript(ordinaryNpc, EventScript.Creature_OnHeartbeat),
                "Ordinary NPCs retain their stock ambient heartbeat wrapper.");

            SetAILevel(guildmaster, AILevel.High);
            ctx.SetNPCResources(guildmaster, 10, 10);
            SetLocalInt(guildmaster, "FP", 0);
            SetLocalInt(guildmaster, "STAMINA", 0);
            _observedGuildmaster = guildmaster;
            _managedHeartbeatCalls = 0;
            ObserveVM();
            try
            {
                using var script = new CExoString(GetEventScript(guildmaster, EventScript.Creature_OnHeartbeat));
                ctx.Assert(NWNXLib.g_pVirtualMachine.RunScript(script, guildmaster, 1, 0) != 0,
                    "The configured creature event completes through the native script dispatcher.");
                ctx.AssertEqual(1, _managedHeartbeatCalls, "The managed SWLOR heartbeat executes exactly once.");
                ctx.AssertEqual(1, GetLocalInt(guildmaster, "FP"), "Heartbeat restores one FP.");
                ctx.AssertEqual(1, GetLocalInt(guildmaster, "STAMINA"), "Heartbeat restores one stamina.");
                ctx.Assert(!ScriptsRun.Contains("nw_c2_default1") && !ScriptsRun.Contains("x2_def_heartbeat"),
                    "The configured service NPC never enters either stock heartbeat interpreter path.");
                ctx.AssertEqual(0, Errors.Count, "The configured managed heartbeat reports no native VM errors.");
                ctx.SetResultDetail("The Engineering Guild Master retains crea_hb_aft through spawn normalization; native dispatch runs the managed heartbeat once and restores FP/STM without entering nw_c2_default1 or x2_def_heartbeat. Ordinary NPCs keep the stock wrapper.");
            }
            finally
            {
                StopObservingVM();
                _observedGuildmaster = OBJECT_INVALID;
            }
        });
    }

    [EngineTest("Guildmaster native heartbeats complete without VM errors", Category = "CreatureHeartbeat", TimeoutSeconds = 45f)]
    public static async Task GuildmasterHeartbeat(EngineTestContext ctx)
    {
        var guildmaster = ctx.SpawnCreature("eng_guildmaster");
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(guildmaster, () =>
        {
            ObserveVM();
            try
            {
                foreach (var aiLevel in new[] { AILevel.VeryLow, AILevel.Low, AILevel.High })
                {
                    SetAILevel(guildmaster, aiLevel);
                    foreach (var flags in new[] { 0, 0x00080000, 0x00200000, 0x00800000, 0x04000000 })
                    {
                        // The stock include stores mobile, immobile, avian, and fast-buff
                        // spawn conditions in this bit field. Cover the ordinary idle path
                        // as well as its optional ambient branches and x2 wrapper.
                        SetLocalInt(guildmaster, "NW_GENERIC_MASTER", flags);
                        SetLocalInt(guildmaster, "NW_ANIM_CONDITION", 0x00000002);
                        foreach (var scriptName in new[] { "nw_c2_default1", "x2_def_heartbeat" })
                        {
                            using var script = new CExoString(scriptName);
                            for (var repeat = 0; repeat < 20; repeat++)
                            {
                                ClearAllActions();
                                var result = NWNXLib.g_pVirtualMachine.RunScript(script, guildmaster, 1, 0);
                                ctx.Assert(result != 0,
                                    $"{scriptName} completes at AI {aiLevel}, flags 0x{flags:x}: {string.Join("; ", Errors)}");
                            }
                        }
                    }
                }
                ctx.Assert(ScriptsRun.Contains("nw_c2_default1"), "The native heartbeat bytecode actually executed.");
                ctx.Assert(ScriptsRun.Contains("x2_def_heartbeat"), "The native x2 wrapper actually executed.");
                ctx.AssertEqual(0, Errors.Count, "No heartbeat or nested native command reports a VM error: " + string.Join("; ", Errors));
                ctx.SetResultDetail("The Engineering Guild Master completes 600 native heartbeat invocations through the direct script and stock x2 wrapper at three AI levels, including mobile, immobile, avian, fast-buff, and ordinary idle conditions, without native VM command or script errors.");
            }
            finally { StopObservingVM(); }
        });
    }

    private static void ObserveVM()
    {
        unsafe { RegisterVMHooks(); }
    }

    private static unsafe void RegisterVMHooks()
    {
        Errors.Clear();
        ScriptsRun.Clear();
        var program = NativeLibrary.GetMainProgramHandle();
        delegate* unmanaged<void*, int, int, int> command = &ExecuteCommand;
        delegate* unmanaged<void*, int, int> script = &RunScriptFile;
        _commandHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
            "_ZN26CNWSVirtualMachineCommands14ExecuteCommandEii"), (IntPtr)command, HookOrder.Late);
        _scriptHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
            "_ZN15CVirtualMachine13RunScriptFileEi"), (IntPtr)script, HookOrder.Late);
    }

    private static void StopObservingVM()
    {
        unsafe { ReturnVMHooks(); }
    }

    private static unsafe void ReturnVMHooks()
    {
        if (_scriptHook != null) NWNXAPI.ReturnFunctionHook(_scriptHook);
        if (_commandHook != null) NWNXAPI.ReturnFunctionHook(_commandHook);
        _scriptHook = null;
        _commandHook = null;
    }

    [UnmanagedCallersOnly]
    private static unsafe int ExecuteCommand(void* commands, int command, int parameters)
    {
        var original = (delegate* unmanaged<void*, int, int, int>)_commandHook->m_trampoline;
        var result = original(commands, command, parameters);
        if (result < 0)
            Errors.Add($"Command {command}, arguments {parameters}: VM error {result}");
        return result;
    }

    [UnmanagedCallersOnly]
    private static unsafe int RunScriptFile(void* machine, int instructionPointer)
    {
        var vm = CVirtualMachine.FromPointer(machine);
        var script = vm.m_pVirtualMachineScript[vm.m_nRecursionLevel];
        var name = script.m_sScriptName.ToString();
        ScriptsRun.Add(name);
        var original = (delegate* unmanaged<void*, int, int>)_scriptHook->m_trampoline;
        var result = original(machine, instructionPointer);
        if (result < 0)
            Errors.Add($"Script {name}, instruction {script.m_nInstructPtr}: VM error {result}");
        return result;
    }
}
