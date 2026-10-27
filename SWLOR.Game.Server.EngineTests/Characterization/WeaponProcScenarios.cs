using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Characterization.Support;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Characterization;

/// <summary>
/// Output locking for on-hit procs: an attacker carrying chance-based riders (a flat auto-attack damage bonus, a bleed
/// and a poison chance, and a stamina restore chance) swings at a fixed target through the native hooks with the shared
/// Random seeded. Each row is one swing: the attack result, the damage rolled, the attacker's stamina and the status
/// effects the target carries afterwards, so a proc that fires, or stops firing, changes the snapshot.
/// </summary>
public static class WeaponProcScenarios
{
    private const int Seed = 20261029;
    private const int Swings = 24;
    private const int AttackResultRegularHit = 1;
    private const int AttackResultCriticalHit = 3;
    private const int AttackResultMiss = 4;
    private const int AttackResultAutomaticHit = 7;
    private const int StaminaDeficit = 40;
    private const string ProcGroup = "characterization-procs";

    [EngineTest("Characterization: weapon procs - melee", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Melee(EngineTestContext ctx) =>
        Run(ctx, "Melee", "b_longsword", SkillType.Vibroblade, false, 1.5f);

    [EngineTest("Characterization: weapon procs - ranged", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Ranged(EngineTestContext ctx) =>
        Run(ctx, "Ranged", "b_rifle", SkillType.Rifle, true, 8f);

    private static async Task Run(EngineTestContext ctx, string label, string resref, SkillType skill, bool ranged, float distance)
    {
        using var clock = new FrozenClock();
        using var arena = await ScenarioArena.CreateAsync(ctx);
        var scenario = arena.Context;
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);

        var attacker = await ScenarioActors.SpawnQuietAsync(scenario, ScenarioActors.Humanoid, -0.5f, 0f);
        var target = await ScenarioActors.SpawnQuietAsync(scenario, ScenarioActors.Humanoid, -0.5f + distance, 0f);
        await ScenarioActors.MakeDummyAsync(scenario, target);
        var weapon = await scenario.EquipItemAsync(attacker, resref, InventorySlot.RightHand);
        scenario.AssertEqual(skill, Skill.GetWeaponSkill(attacker, weapon), $"{resref} belongs to the {skill} skill");

        scenario.SetNPCResources(attacker, 100, 100);
        // Every proc is a temporary modifier in one group, so the chance and its payload read as one source.
        TemporaryStatModifier.Add(attacker, StatType.AutoAttackDamageBonus, 9, 3600f, ProcGroup);
        TemporaryStatModifier.Add(attacker, StatType.AutoAttackDamageBonusChance, 50, 3600f, ProcGroup);
        TemporaryStatModifier.Add(attacker, StatType.DamageDealtBleedChance, 35, 3600f, ProcGroup);
        TemporaryStatModifier.Add(attacker, StatType.DamageDealtPoisonChance, 25, 3600f, ProcGroup);
        TemporaryStatModifier.Add(attacker, StatType.AutoAttackStaminaRestoreChance, 60, 3600f, ProcGroup);
        TemporaryStatModifier.Add(attacker, StatType.AutoAttackStaminaRestore, 4, 3600f, ProcGroup);
        var maximumStamina = Stat.GetMaxStamina(attacker);
        SetLocalInt(attacker, "STAMINA", maximumStamina - StaminaDeficit);
        await scenario.WaitFrameAsync();

        var table = new SnapshotTable(
            "ResolveAttackRoll + GetDamageRoll + Combat.ApplyDamageDealtEffects",
            $"Swings a {skill} weapon, carrying chance-based on-hit riders, at a fixed target through the native hooks and records each swing.",
            $"Attacker and target are two {ScenarioActors.Humanoid}, {distance} m apart, the target held still; the attacker wields {resref}, " +
            $"has {maximumStamina - StaminaDeficit} of {maximumStamina} stamina and carries a 9 damage bonus (50%), bleed (35%), poison (25%) and a " +
            $"4 stamina restore (60%) rider. Random is seeded with {Seed} once, then {Swings} swings are resolved in one step. " +
            "Result 1 is a hit, 3 a critical hit, 4 a miss; damage is 0 unless the swing landed.",
            new[] { "swing" },
            new[] { "result", "damage", "attackerStamina", "targetEffects" },
            Seed) { AllowConstantOutputs = true };

        var results = new List<(int Result, int Damage, int Stamina, List<string> Effects)>();
        await scenario.ExecuteInCreatureContextAsync(attacker, () =>
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var native = server.GetGameObject(attacker).AsNWSCreature();
            var defender = server.GetGameObject(target).AsNWSObject();
            native.m_pcCombatRound.StartCombatRound(target);
            scenario.SeedRandom(Seed);
            for (var swing = 0; swing < Swings; swing++)
            {
                native.m_pcCombatRound.m_nCurrentAttack = 0;
                var attack = native.m_pcCombatRound.GetAttack(0);
                attack.m_bRangedAttack = ranged ? 1 : 0;
                native.ResolveAttackRoll(defender);
                var result = (int)attack.m_nAttackResult;
                var landed = result is AttackResultRegularHit or AttackResultCriticalHit or AttackResultAutomaticHit;
                var damage = landed
                    ? native.m_pStats.GetDamageRoll(defender, 0, result == AttackResultCriticalHit ? 1 : 0, 0, 0, 0)
                    : 0;
                results.Add((result, damage, Stat.GetCurrentStamina(attacker),
                    StatusEffect.GetCreatureStatusEffects(target).GetAllEffects()
                        .Select(effect => effect.Name).OrderBy(name => name, StringComparer.Ordinal).ToList()));
            }
        });

        for (var swing = 0; swing < results.Count; swing++)
        {
            var (result, damage, stamina, effects) = results[swing];
            table.Add(swing + 1, result switch
            {
                AttackResultRegularHit => "hit",
                AttackResultCriticalHit => "critical",
                AttackResultMiss => "miss",
                AttackResultAutomaticHit => "automatic hit",
                _ => "result " + result
            }, damage, stamina, effects);
        }

        ScenarioSnapshot.Verify("Combat", "WeaponProcs." + label, table);
    }
}
