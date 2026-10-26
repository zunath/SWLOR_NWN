using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using DamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Saber Ward re-types a share of every incoming physical hit into a separate Force hit. Native
/// weapon swings resolve their damage outside any script, so the converted Force hit must still be
/// dealt when it is deferred from the damage-roll hook (it used to be dropped, which made Saber Ward
/// a flat physical reduction against ordinary attacks).
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
                // Large physical hits, so every hit's 30 percent share is at least one point.
                ReplaceProperty(weapon, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 60));
                ReplaceProperty(weapon, ItemPropertyType.WeaponDamageType,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical));
                foreach (var creature in new[] { attacker, target })
                {
                    ctx.SuppressNPCNaturalRegen(creature);
                    Stat.SetNPCMaxHitPoints(creature, 20000, true);
                }
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
            AssignCommand(attacker, () => ClearAllActions());
            // The converted Force hit is dealt on the frame after its weapon hit.
            await ctx.DelaySecondsAsync(1f);

            var physicalHits = Impacts.Where(impact => impact.Physical > 0).ToList();
            var forceHits = Impacts.Where(impact => impact.Force > 0).ToList();
            ctx.Log($"physicalHits=[{string.Join(", ", physicalHits.Select(hit => hit.Physical))}], " +
                    $"forceHits=[{string.Join(", ", forceHits.Select(hit => hit.Force))}]");
            ctx.Assert(physicalHits.All(hit => hit.Force == 0),
                "the converted share is dealt as its own Force hit, not inside the weapon hit");
            ctx.Assert(physicalHits.All(hit => hit.Physical >= 4), "the fixture's weapon hits are large enough to convert");
            ctx.AssertEqual(physicalHits.Count, forceHits.Count,
                "every converted weapon hit deals its Force share");
            var forceShare = forceHits.Sum(hit => hit.Force) * 100 /
                             (forceHits.Sum(hit => hit.Force) + physicalHits.Sum(hit => hit.Physical));
            ctx.Assert(forceShare is >= 15 and <= 45,
                $"about 30 percent of the warded damage arrives as Force (was {forceShare} percent)");
        }
        finally
        {
            Combat.SetAutoAttackHitResolutionOverride(null);
            if (GetIsObjectValid(attacker)) AssignCommand(attacker, () => ClearAllActions());
            _observedAttacker = OBJECT_INVALID;
            _observedTarget = OBJECT_INVALID;
            Impacts.Clear();
        }
    }

    private static void ReplaceProperty(uint weapon, ItemPropertyType type, ItemProperty replacement)
    {
        var old = new List<ItemProperty>();
        for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
            if (GetItemPropertyType(property) == type) old.Add(property);
        foreach (var property in old) RemoveItemProperty(weapon, property);
        AddItemProperty(DurationType.Permanent, replacement, weapon);
    }
}
