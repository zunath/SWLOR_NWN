using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Characterization.Support;
using SWLOR.Game.Server.EngineTests.Definitions;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Characterization;

/// <summary>
/// Output locking for ordinary weapon attacks: for each weapon class a fixed attacker swings at a fixed target through
/// the real native attack-roll and damage-roll hooks, the way the engine resolves each swing of a combat round, with
/// the shared Random seeded. Every row is one swing: whether it hit, crit or missed, the physical damage it rolled and
/// what the target carried afterwards. Swings are resolved one after another in a single step, so no frame, animation
/// or timer can change how many happen.
/// </summary>
public static class WeaponAttackScenarios
{
    private const int Seed = 20261029;
    private const int Swings = 30;
    private const int AttackResultRegularHit = 1;
    private const int AttackResultDeflect = 2;
    private const int AttackResultCriticalHit = 3;
    private const int AttackResultMiss = 4;
    private const int AttackResultAutomaticHit = 7;
    private const float MeleeDistance = 1.5f;
    private const float RangedDistance = 8f;

    private sealed record WeaponClass(string Name, SkillType Skill, string Resref, bool Ranged);

    private static readonly WeaponClass[] Classes =
    {
        new("Vibroblade", SkillType.Vibroblade, "b_longsword", false),
        new("Vibroknife", SkillType.Vibroknife, "b_knife", false),
        new("Lightsaber", SkillType.Lightsaber, "vet_trnsaber", false),
        new("Heavy Vibroblade", SkillType.HeavyVibroblade, "b_greatsword", false),
        new("Spear", SkillType.Spear, "b_spear", false),
        new("Twin Blade", SkillType.TwinBlade, "b_twinblade", false),
        new("Saberstaff", SkillType.Saberstaff, "vet_trnsabstaff", false),
        new("Katar", SkillType.Katar, "b_katar", false),
        new("Staff", SkillType.Staff, "b_staff", false),
        new("Pistol", SkillType.Pistol, "b_pistol", true),
        new("Rifle", SkillType.Rifle, "b_rifle", true),
        new("Throwing", SkillType.Throwing, "b_shuriken", true),
        new("Unarmed", SkillType.Unarmed, "t_pglove", false),
        new("Twin Pistol", SkillType.TwinPistol, "b_twinpistol", true)
    };

    [EngineTest("Characterization: weapon attacks - Vibroblade", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Vibroblade(EngineTestContext ctx) => Run(ctx, Classes[0]);

    [EngineTest("Characterization: weapon attacks - Vibroknife", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Vibroknife(EngineTestContext ctx) => Run(ctx, Classes[1]);

    [EngineTest("Characterization: weapon attacks - Lightsaber", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Lightsaber(EngineTestContext ctx) => Run(ctx, Classes[2]);

    [EngineTest("Characterization: weapon attacks - Heavy Vibroblade", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task HeavyVibroblade(EngineTestContext ctx) => Run(ctx, Classes[3]);

    [EngineTest("Characterization: weapon attacks - Spear", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Spear(EngineTestContext ctx) => Run(ctx, Classes[4]);

    [EngineTest("Characterization: weapon attacks - Twin Blade", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task TwinBlade(EngineTestContext ctx) => Run(ctx, Classes[5]);

    [EngineTest("Characterization: weapon attacks - Saberstaff", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Saberstaff(EngineTestContext ctx) => Run(ctx, Classes[6]);

    [EngineTest("Characterization: weapon attacks - Katar", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Katar(EngineTestContext ctx) => Run(ctx, Classes[7]);

    [EngineTest("Characterization: weapon attacks - Staff", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Staff(EngineTestContext ctx) => Run(ctx, Classes[8]);

    [EngineTest("Characterization: weapon attacks - Pistol", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Pistol(EngineTestContext ctx) => Run(ctx, Classes[9]);

    [EngineTest("Characterization: weapon attacks - Rifle", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Rifle(EngineTestContext ctx) => Run(ctx, Classes[10]);

    [EngineTest("Characterization: weapon attacks - Throwing", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Throwing(EngineTestContext ctx) => Run(ctx, Classes[11]);

    [EngineTest("Characterization: weapon attacks - Unarmed", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task Unarmed(EngineTestContext ctx) => Run(ctx, Classes[12]);

    [EngineTest("Characterization: weapon attacks - Twin Pistol", Category = "Characterization", TimeoutSeconds = 180f)]
    public static Task TwinPistol(EngineTestContext ctx) => Run(ctx, Classes[13]);

    private static async Task Run(EngineTestContext ctx, WeaponClass weaponClass)
    {
        using var clock = new FrozenClock();
        using var arena = await ScenarioArena.CreateAsync(ctx);
        var scenario = arena.Context;
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);

        var distance = weaponClass.Ranged ? RangedDistance : MeleeDistance;
        var attacker = await ScenarioActors.SpawnQuietAsync(scenario, ScenarioActors.Humanoid, -0.5f, 0f);
        var target = await ScenarioActors.SpawnQuietAsync(scenario, ScenarioActors.Humanoid, -0.5f + distance, 0f);
        await ScenarioActors.MakeDummyAsync(scenario, target);
        var weapon = await scenario.EquipItemAsync(attacker, weaponClass.Resref, InventorySlot.RightHand);
        scenario.AssertEqual(weaponClass.Skill, Skill.GetWeaponSkill(attacker, weapon),
            $"{weaponClass.Resref} belongs to the {weaponClass.Name} skill");
        // The owned Twin Pistol item creates its linked off-hand gun once equipped; both swing together.
        if (weaponClass.Skill == SkillType.TwinPistol)
            await TwinPistolEquipmentEngineTests.WaitForPair(scenario, attacker, weapon);
        await scenario.WaitFrameAsync();

        var fixture = $"accuracy {Stat.GetAccuracy(attacker, weapon, AbilityType.Invalid, SkillType.Invalid)}, " +
                      $"evasion {Stat.GetEvasion(target, SkillType.Invalid)}, " +
                      $"weapon DMG {WeaponDamage.GetEffectiveDMG(attacker, weapon)}, " +
                      $"base item {GetBaseItemType(weapon)}, properties{ItemDescription.DescribeProperties(weapon)}";

        // Each class draws its own sequence, so the classes do not all hit and miss on the same swings.
        var seed = Seed + Array.IndexOf(Classes, weaponClass);
        var table = new SnapshotTable(
            "ResolveAttackRoll + GetDamageRoll",
            $"Swings a {weaponClass.Name} weapon at a fixed target through the native attack-roll and damage-roll hooks and " +
            "records each swing: the attack result and the physical damage rolled for a landed swing.",
            $"Attacker and target are two {ScenarioActors.Humanoid}, {distance} m apart, the target held still; " +
            $"the attacker wields {weaponClass.Resref} ({fixture}). Random is seeded with {seed} once, then {Swings} swings are " +
            "resolved in one step. Result 1 is a hit, 3 a critical hit, 4 a miss, 2 a deflection; damage is 0 unless the swing landed.",
            new[] { "swing" },
            new[] { "result", "damage" },
            seed);

        var results = new List<(int Result, int Damage)>();
        await scenario.ExecuteInCreatureContextAsync(attacker, () =>
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var native = server.GetGameObject(attacker).AsNWSCreature();
            var defender = server.GetGameObject(target).AsNWSObject();
            native.m_pcCombatRound.StartCombatRound(target);
            scenario.SeedRandom(seed);
            for (var swing = 0; swing < Swings; swing++)
            {
                native.m_pcCombatRound.m_nCurrentAttack = 0;
                var attack = native.m_pcCombatRound.GetAttack(0);
                attack.m_bRangedAttack = weaponClass.Ranged ? 1 : 0;
                native.ResolveAttackRoll(defender);
                var result = (int)attack.m_nAttackResult;
                var landed = result is AttackResultRegularHit or AttackResultCriticalHit or AttackResultAutomaticHit;
                var damage = landed
                    ? native.m_pStats.GetDamageRoll(defender, 0, result == AttackResultCriticalHit ? 1 : 0, 0, 0, 0)
                    : 0;
                results.Add((result, damage));
            }
        });

        for (var swing = 0; swing < results.Count; swing++)
            table.Add(swing + 1, ResultName(results[swing].Result), results[swing].Damage);

        ScenarioSnapshot.Verify("Combat", "WeaponAttacks." + weaponClass.Name.Replace(" ", string.Empty), table);
    }

    private static string ResultName(int result)
    {
        return result switch
        {
            AttackResultRegularHit => "hit",
            AttackResultDeflect => "deflect",
            AttackResultCriticalHit => "critical",
            AttackResultMiss => "miss",
            AttackResultAutomaticHit => "automatic hit",
            _ => "result " + result
        };
    }
}
