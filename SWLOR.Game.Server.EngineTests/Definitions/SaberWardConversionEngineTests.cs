using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using DamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Saber Ward re-types a share of every incoming physical hit into a separate Force hit. Native
/// weapon swings resolve their damage in a script context with no object, so the converted Force hit
/// must still be dealt when it is deferred from the damage-roll hook (it used to be dropped, which made
/// Saber Ward a flat physical reduction against ordinary attacks).
/// </summary>
public static class SaberWardConversionEngineTests
{
    private const int Seed = 20261026;
    private static uint _observedTarget = OBJECT_INVALID;
    private static uint _observedAttacker = OBJECT_INVALID;
    private static readonly List<(int Physical, int Force)> Impacts = new();

    [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
    public static void ObserveImpact()
    {
        if (OBJECT_SELF != _observedTarget || GetLastDamager(OBJECT_SELF) != _observedAttacker) return;
        var physical = Math.Max(0, GetDamageDealtByType(DamageType.BaseWeapon)) +
                       Math.Max(0, GetDamageDealtByType(DamageType.Slashing)) +
                       Math.Max(0, GetDamageDealtByType(DamageType.Piercing)) +
                       Math.Max(0, GetDamageDealtByType(DamageType.Bludgeoning));
        Impacts.Add((physical, Math.Max(0, GetDamageDealtByType(DamageType.Force))));
    }

    [EngineTest("Saber Ward deals the converted share of ordinary weapon hits as Force damage", Category = "SaberWard", TimeoutSeconds = 90f)]
    public static async Task OrdinaryHitsDealConvertedForce(EngineTestContext ctx)
    {
        var attacker = ctx.SpawnCreature("nw_bandit001", -0.5f);
        var target = ctx.SpawnCreature("nw_bandit001", 1.0f);
        await ctx.DelaySecondsAsync(1f);
        var weapon = await ctx.EquipItemAsync(attacker, "b_longsword", InventorySlot.RightHand);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(target, () =>
            {
                foreach (var creature in new[] { attacker, target })
                {
                    // No AI, so only the commanded swings land and none start after the attack is cleared.
                    SetEventScript(creature, EventScript.Creature_OnHeartbeat, "");
                    SetEventScript(creature, EventScript.Creature_OnNotice, "");
                    SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "");
                    SetEventScript(creature, EventScript.Creature_OnEndCombatRound, "");
                    ctx.SuppressNPCNaturalRegen(creature);
                    Stat.SetNPCMaxHitPoints(creature, 20000, true);
                }
                // Large physical hits, so every hit's 30 percent share is at least one point.
                BiowareXP2.IPSafeAddItemProperty(weapon, ItemPropertyCustom(ItemPropertyType.DMG, -1, 60),
                    0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
                BiowareXP2.IPSafeAddItemProperty(weapon, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical),
                    0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
                TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 60f);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
                ctx.Assert(StatusEffect.ApplyStatusEffect(target, target, new SaberWardStatusEffect(30, 6, 9), 60f),
                    "Saber Ward IV applies to the defender");
                ctx.MakeHostile(target);
            });

            Impacts.Clear();
            _observedAttacker = attacker;
            _observedTarget = target;
            ctx.SeedRandom(Seed);
            Combat.SetAutoAttackHitResolutionOverride(true);
            AssignCommand(attacker, () => ActionAttack(target));

            await ctx.WaitUntilAsync(() => Impacts.Count(impact => impact.Physical > 0) >= 4, 60f,
                "four of the attacker's weapon hits to land on the warded defender");
            AssignCommand(attacker, () => ClearAllActions(true));
            // The Force share is dealt the frame after the damage roll; the engine applies the weapon
            // damage itself at the swing's impact. Let the last swing settle either way.
            await ctx.DelaySecondsAsync(3f);

            var physicalHits = Impacts.Where(impact => impact.Physical > 0).ToList();
            var forceHits = Impacts.Where(impact => impact.Force > 0).ToList();
            ctx.Log($"physicalHits=[{string.Join(", ", physicalHits.Select(hit => hit.Physical))}], " +
                    $"forceHits=[{string.Join(", ", forceHits.Select(hit => hit.Force))}]");
            ctx.Assert(physicalHits.All(hit => hit.Force == 0),
                "the converted share is dealt as its own Force hit, not inside the weapon hit");
            ctx.Assert(physicalHits.All(hit => hit.Physical >= 4), "the fixture's weapon hits are large enough to convert");
            // A swing whose damage was rolled when the attack was cleared may have dealt its Force share
            // without its weapon damage landing.
            ctx.Assert(forceHits.Count >= physicalHits.Count && forceHits.Count <= physicalHits.Count + 1,
                $"every converted weapon hit deals its Force share ({physicalHits.Count} weapon hits, {forceHits.Count} Force hits)");
            var forceShare = forceHits.Sum(hit => hit.Force) * 100 /
                             (forceHits.Sum(hit => hit.Force) + physicalHits.Sum(hit => hit.Physical));
            ctx.Assert(forceShare is >= 15 and <= 45,
                $"about 30 percent of the warded damage arrives as Force (was {forceShare} percent)");
        }
        finally
        {
            Combat.SetAutoAttackHitResolutionOverride(null);
            if (GetIsObjectValid(attacker)) AssignCommand(attacker, () => ClearAllActions(true));
            _observedAttacker = OBJECT_INVALID;
            _observedTarget = OBJECT_INVALID;
            Impacts.Clear();
        }
    }
}
