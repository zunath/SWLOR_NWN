using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class AnimationReplacementEngineTests
{
    private static readonly string[] Sources =
    {
        "custom1start", "custom1lp", "custom1end", "dodges", "xbowr", "plpause1"
    };

    private static readonly FeatType[] Rotation =
    {
        FeatType.ForceBurst1, FeatType.Benevolence3, FeatType.CreepingTerror3,
        FeatType.ForceSanctuary1, FeatType.Flamethrower3, FeatType.Renewal3,
        FeatType.SonicBurst3, FeatType.PressTheAttack3, FeatType.RadiantLance3,
        FeatType.MedKit4, FeatType.FragGrenade3, FeatType.KoltoMist2,
        FeatType.ForceDrain3, FeatType.RousingShout3, FeatType.PurifyingWave1,
        FeatType.GuardianWard4, FeatType.IncendiaryField3, FeatType.ChargeOrder2,
        FeatType.NightmareField1, FeatType.Shielding3, FeatType.FieldRecovery2
    };

    [EngineTest("Animation replacement native packet boundary and lifecycle matrix", Category = "AnimationReplacementWire", TimeoutSeconds = 120f)]
    public static async Task NativePackets(EngineTestContext ctx)
    {
        LogRotation(ctx);
        var creatures = Enumerable.Range(0, 30).Select(i => ctx.SpawnCreature("nw_bandit001", i % 5, i / 5)).ToArray();
        await ctx.DelaySecondsAsync(1f);
        var snapshots = 0;
        uint largest = 0;
        await InScript(ctx, () =>
        {
            foreach (var creature in creatures)
            {
                SetAILevel(creature, AILevel.VeryLow);
                ClearAllActions(true, creature);
            }
            // Includes the full 16-character source AND target boundary. The names
            // need not exist in a model: this case tests the wire format only.
            foreach (var length in new[] { 1, 8, 13, 15, 16 })
            foreach (var mappingCount in new[] { 1, 3, 6, 28 })
            {
                var keys = Enumerable.Range(0, mappingCount)
                    .Select(i => ("s" + i.ToString("D2")).PadRight(length < 3 ? 3 : length, 's')).ToArray();
                var target = new string('a', length);
                foreach (var creature in creatures)
                {
                    foreach (var key in keys) ReplaceObjectAnimation(creature, key, target);
                    // Clearing missing keys must leave this map intact.
                    foreach (var key in Sources) ReplaceObjectAnimation(creature, key, "");
                }
                Check(mappingCount, target);
                foreach (var creature in creatures)
                    foreach (var key in keys) ReplaceObjectAnimation(creature, key, target); // repeated identical sets
                Check(mappingCount, target);
                foreach (var creature in creatures)
                    foreach (var key in keys) ReplaceObjectAnimation(creature, key, "");
                Check(0, "");
                // Set and clear before any serializer has observed the map.
                foreach (var creature in creatures)
                    foreach (var key in keys)
                    {
                        ReplaceObjectAnimation(creature, key, target);
                        ReplaceObjectAnimation(creature, key, "");
                    }
                Check(0, "");
                ctx.Log($"WIRE length={length} mappings={mappingCount} creatures=30 accepted all four lifecycle states");
            }
            void Check(int expectedCount, string expectedTarget)
            {
                foreach (var creature in creatures)
                {
                    var obj = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
                    var map = Native.WeaponAttackAnimation.ReadAnimationReplacements(obj);
                    ctx.AssertEqual(expectedCount, map.Count, "Native map has the requested number of entries");
                    ctx.Assert(map.Values.All(value => value == expectedTarget), "Native replacement names retain every requested character");
                }
                largest = Math.Max(largest, AnimationReplacementPacket.AssertRoundTrip(ctx, creatures));
                snapshots++;
            }
        });
        ctx.SetResultDetail($"{snapshots} native message round trips; 30 updates/message; largest {largest} bytes; name lengths 1/8/13/15/16; mapping counts 1/3/6/28. Native framing only; connected-client stability is unverified.");
    }

    // Explicitly opt in. This probe does not create, log in, move, or control a player.
    // The operator connects normally; disposable NPCs are spawned near that player.
    [EngineTest("Animation replacement connected-client controlled probe", Category = "AnimationReplacementClient", TimeoutSeconds = 600f)]
    public static async Task ConnectedClient(EngineTestContext ctx)
    {
        if (ApplicationSettings.Get().EngineTestFilter?.Contains("AnimationReplacementClient", StringComparison.OrdinalIgnoreCase) != true)
            ctx.Skip("Requires an explicit AnimationReplacementClient filter and an operator-connected client.");
        var mode = Environment.GetEnvironmentVariable("SWLOR_ANIMATION_PROBE_MODE") ?? "named";
        ctx.Assert(new[] { "baseline", "missing", "sameframe", "separate", "replace", "persistent", "named" }.Contains(mode), "Recognized probe mode");
        var name = Environment.GetEnvironmentVariable("SWLOR_ANIMATION_PROBE_NAME") ?? "sw_guardward";
        var clip = new AnimationClip(name, ReadNumber("DURATION", 2.55f, .01f, 30f));
        var secondName = Environment.GetEnvironmentVariable("SWLOR_ANIMATION_PROBE_SECOND_NAME") ?? "sw_fiel_reco";
        var second = new AnimationClip(secondName, ReadNumber("SECOND_DURATION", 3.2f, .01f, 30f));
        var alternate = string.Equals(Environment.GetEnvironmentVariable("SWLOR_ANIMATION_PROBE_ALTERNATE"), "true", StringComparison.OrdinalIgnoreCase);
        var seconds = ReadNumber("SECONDS", 65f, 1f, 300f);
        var interval = ReadNumber("INTERVAL", 4f, .01f, 30f);
        var count = (int)ReadNumber("CREATURES", 1f, 1f, 30f);
        LogRotation(ctx);
        Console.WriteLine("[ANIMATION_PROBE] WAIT_CLIENT: connect normally; probe will spawn NPCs nearby");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetFirstPC()) && GetIsObjectValid(GetArea(GetFirstPC())), 180f, "operator-connected player");
        var player = GetFirstPC();
        var location = GetLocation(player);
        var position = GetPositionFromLocation(location);
        var area = GetArea(player);
        var creatures = new List<uint>();
        for (var i = 0; i < count; i++)
        {
            var point = position;
            point.X += 3f + i % 5;
            point.Y += i / 5;
            var creature = CreateObject(ObjectType.Creature, "nw_bandit001", Location(area, point, 180f));
            ctx.Assert(GetIsObjectValid(creature), "Probe NPC spawned near operator");
            ctx.Track(creature);
            ChangeToStandardFaction(creature, StandardFaction.Defender);
            SetAILevel(creature, AILevel.VeryLow);
            SetPlotFlag(creature, true);
            creatures.Add(creature);
        }
        await ctx.DelaySecondsAsync(1f);
        await InScript(ctx, () =>
        {
            foreach (var creature in creatures)
            {
                SetAILevel(creature, AILevel.VeryLow);
                SetPlotFlag(creature, true);
                ClearAllActions(true, creature);
            }
        });
        await ctx.DelaySecondsAsync(3f);
        var watch = Stopwatch.StartNew();
        var iterations = 0;
        var calls = 0;
        var nextReport = 10d;
        Console.WriteLine($"[ANIMATION_PROBE] BEGIN utc={DateTime.UtcNow:O} mode={mode} creatures={count} interval={interval} clip={clip.Name} lengths={clip.Name.Length}/{clip.StartName.Length}/{clip.EndName.Length} duration={clip.Duration} second={second.Name} secondLengths={second.Name.Length}/{second.StartName.Length}/{second.EndName.Length} alternate={alternate || mode == "replace"} seconds={seconds}");
        try
        {
            while (watch.Elapsed.TotalSeconds < seconds)
            {
                ctx.Assert(GetIsObjectValid(player) && GetArea(player) == area,
                    "Operator remains connected in the probe area; inspect client logs if this failed");
                await InScript(ctx, () =>
                {
                    var current = (alternate || mode == "replace") && iterations % 2 != 0 ? second : clip;
                    foreach (var creature in creatures)
                    {
                        if (mode == "named") NamedAnimation.Play(creature, current);
                        else if (mode == "baseline") PlayAnimationOn(creature);
                        else if (mode == "missing")
                        {
                            foreach (var source in Sources) Replace(creature, source, "");
                        }
                        else
                        {
                            if (mode != "persistent" || iterations == 0)
                            {
                                // Alternate between installed clips without removing keys.
                                Replace(creature, Sources[0], current.StartName);
                                Replace(creature, Sources[1], current.Name);
                                Replace(creature, Sources[2], current.EndName);
                            }
                            PlayAnimationOn(creature);
                            if (mode == "sameframe")
                                foreach (var source in Sources) Replace(creature, source, "");
                        }
                    }
                });
                if (mode == "separate")
                {
                    await ctx.DelaySecondsAsync(interval / 2f);
                    await InScript(ctx, ClearMappings);
                    await ctx.DelaySecondsAsync(interval / 2f);
                }
                else await ctx.DelaySecondsAsync(interval);
                iterations++;
                if (watch.Elapsed.TotalSeconds >= nextReport)
                {
                    Console.WriteLine($"[ANIMATION_PROBE] PROGRESS utc={DateTime.UtcNow:O} iterations={iterations} actualPerCreatureRate={iterations / watch.Elapsed.TotalSeconds:F3}");
                    nextReport += 10;
                }
            }
        }
        finally
        {
            await InScript(ctx, () =>
            {
                foreach (var creature in creatures) NamedAnimation.ClearOnDeath(creature);
                ClearMappings();
            });
        }
        var detail = $"mode={mode}; creatures={count}; iterations={iterations}; actual cycles/creature/s={iterations / watch.Elapsed.TotalSeconds:F3}; total cycles/s={iterations * count / watch.Elapsed.TotalSeconds:F3}; explicit raw replacement calls={calls}. Client warnings/stalls require operator log review; server completion does not certify client health.";
        ctx.Log(detail);
        ctx.SetResultDetail(detail);
        Console.WriteLine($"[ANIMATION_PROBE] END utc={DateTime.UtcNow:O} {detail}");

        void Replace(uint creature, string source, string target)
        {
            ReplaceObjectAnimation(creature, source, target);
            calls++;
        }
        void ClearMappings()
        {
            foreach (var creature in creatures)
                foreach (var source in Sources) Replace(creature, source, "");
        }
        void PlayAnimationOn(uint creature) => AssignCommand(creature, () => PlayAnimation(Animation.PointForward, 1f, clip.Duration));
    }

    private static float ReadNumber(string suffix, float fallback, float min, float max)
    {
        var text = Environment.GetEnvironmentVariable("SWLOR_ANIMATION_PROBE_" + suffix);
        if (text == null) return fallback;
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value) || value < min || value > max)
            throw new ArgumentException($"SWLOR_ANIMATION_PROBE_{suffix} must be between {min} and {max}.");
        return value;
    }

    private static async Task InScript(EngineTestContext ctx, Action action)
    {
        var complete = false;
        Exception failure = null;
        AssignCommand(GetModule(), () =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
            finally { complete = true; }
        });
        var watch = Stopwatch.StartNew();
        while (!complete)
        {
            ctx.Assert(watch.Elapsed.TotalSeconds < 3, "Diagnostic script callback executed");
            await ctx.WaitFrameAsync();
        }
        if (failure != null) throw failure;
    }

    private static void LogRotation(EngineTestContext ctx)
    {
        foreach (var feat in Rotation)
        {
            var ability = Ability.GetAbilityDetail(feat);
            var clip = ability.PreviewAnimation;
            ctx.Log($"ROTATION {feat}: catalog={clip?.Name} lengths={clip?.Name.Length}/{clip?.StartName.Length}/{clip?.EndName.Length} " +
                $"npcActivation={AbilityAnimationBinding.ActivationClip(ability, false)?.Name ?? "native"} " +
                $"npcImpact={AbilityAnimationBinding.ImpactClip(ability, false)?.Name ?? "native"} " +
                $"playerActivation={AbilityAnimationBinding.ActivationClip(ability, true)?.Name ?? "native"} " +
                $"playerImpact={AbilityAnimationBinding.ImpactClip(ability, true)?.Name ?? "native"}");
        }
    }
}
