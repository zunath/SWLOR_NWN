using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class WeaponAbilityDamageTypeEngineTests
    {
        private static readonly CombatDamageType[] ObservedTypes =
        {
            CombatDamageType.Physical,
            CombatDamageType.Force,
            CombatDamageType.Fire,
            CombatDamageType.Ice
        };

        private static readonly Dictionary<CombatDamageType, int> _damage = new();
        private static uint _observedTarget = OBJECT_INVALID;

        [EngineTest("Weapon abilities deal the equipped weapon's elemental damage type", Category = "WeaponAbilityDamageType", TimeoutSeconds = 30f)]
        public static async Task ElementalWeaponRetypesAbilityDamage(EngineTestContext ctx)
        {
            var (caster, target, _) = await CreateFixture(ctx, "b_pistol", CombatDamageType.Fire);
            await ApplyImpact(ctx, caster, target, FeatType.DoubleShot1, PerkType.DoubleShot, target);
            AssertOnlyDamageType(ctx, CombatDamageType.Fire, "Double Shot from a Fire pistol");
        }

        [EngineTest("Weapon abilities stay Physical on a weapon without a damage type", Category = "WeaponAbilityDamageType", TimeoutSeconds = 30f)]
        public static async Task UntypedWeaponKeepsPhysicalAbilityDamage(EngineTestContext ctx)
        {
            var (caster, target, _) = await CreateFixture(ctx, "b_pistol", CombatDamageType.Physical);
            await ApplyImpact(ctx, caster, target, FeatType.DoubleShot1, PerkType.DoubleShot, target);
            AssertOnlyDamageType(ctx, CombatDamageType.Physical, "Double Shot from an untyped pistol");
        }

        [EngineTest("Area weapon abilities outside the shared weapon base use the weapon's damage type", Category = "WeaponAbilityDamageType", TimeoutSeconds = 30f)]
        public static async Task AreaWeaponAbilityUsesWeaponDamageType(EngineTestContext ctx)
        {
            var (caster, target, _) = await CreateFixture(ctx, "nw_wswls001", CombatDamageType.Fire);
            await ApplyImpact(ctx, caster, target, FeatType.SavageCleave1, PerkType.SavageCleave, caster);
            AssertOnlyDamageType(ctx, CombatDamageType.Fire, "Savage Cleave from a Fire vibroblade");
        }

        [EngineTest("Weapon abilities authored as Force stay Force on an elemental weapon", Category = "WeaponAbilityDamageType", TimeoutSeconds = 30f)]
        public static async Task AuthoredForceAbilityKeepsForce(EngineTestContext ctx)
        {
            var (caster, target, _) = await CreateFixture(ctx, "sabstorm_l1", CombatDamageType.Fire);
            await ApplyImpact(ctx, caster, target, FeatType.Epicenter1, PerkType.Epicenter, caster);
            AssertOnlyDamageType(ctx, CombatDamageType.Force, "Epicenter from a Fire lightsaber");
        }

        [EngineTest("Queued weapon abilities use the damage type of the weapon that triggered them", Category = "WeaponAbilityDamageType", TimeoutSeconds = 30f)]
        public static async Task QueuedAbilityUsesTriggeringWeaponDamageType(EngineTestContext ctx)
        {
            var (caster, target, main) = await CreateFixture(ctx, "nw_wswss001", CombatDamageType.Fire);
            var off = await ctx.EquipItemAsync(caster, "nw_wswss001", InventorySlot.LeftHand);
            await ctx.ExecuteInCreatureContextAsync(caster, () => ConfigureWeapon(off, CombatDamageType.Ice));
            await ctx.WaitFrameAsync();

            var ability = Ability.GetAbilityDetail(FeatType.PathogenStrike1);
            ctx.SetNPCPerkLevel(caster, PerkType.PathogenStrike, 1);
            await ObserveImpact(ctx, target, async () =>
            {
                Ability.BeginAbilityImpact(caster, ability, triggeringWeapon: off);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => ability.ImpactAction(caster, target, 1, GetLocation(target)));
                }
                finally
                {
                    Ability.EndAbilityImpact(caster);
                }
            });
            AssertOnlyDamageType(ctx, CombatDamageType.Ice, "Pathogen Strike delivered by the Cold off-hand knife");
            ctx.Assert(GetIsObjectValid(main), "Fire main-hand knife stays equipped");
        }

        [EngineTest("Queued natural weapon abilities deal the natural weapon's damage type", Category = "WeaponAbilityDamageType", TimeoutSeconds = 30f)]
        public static async Task QueuedNaturalWeaponAbilityUsesNaturalWeaponDamageType(EngineTestContext ctx)
        {
            var beast = ctx.SpawnCreature("kath_hound", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.WaitFrameAsync();
            var claw = GetItemInSlot(InventorySlot.CreatureRight, beast);
            ctx.Assert(GetIsObjectValid(claw), "Creature blueprint provides an equipped natural weapon");
            await ctx.ExecuteInCreatureContextAsync(beast, () => ConfigureWeapon(claw, CombatDamageType.Fire));
            await ctx.WaitFrameAsync();
            foreach (var creature in new[] { beast, target })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                Stat.SetNPCMaxHitPoints(creature, 20000, true);
                SetAILevel(creature, AILevel.VeryLow);
            }
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
            ctx.MakeHostile(target);
            ctx.SetNPCPerkLevel(beast, PerkType.Bite, 1);

            var bite = Ability.GetAbilityDetail(FeatType.Bite1);
            await ObserveImpact(ctx, target, async () =>
            {
                Ability.BeginAbilityImpact(beast, bite, triggeringWeapon: claw);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(beast,
                        () => bite.ImpactAction(beast, target, 1, GetLocation(target)));
                }
                finally
                {
                    Ability.EndAbilityImpact(beast);
                }
            });
            AssertOnlyDamageType(ctx, CombatDamageType.Fire, "Bite delivered by a Fire natural weapon");
        }

        [EngineTest("Queued weapon abilities consumed by a landed attack deal the weapon's damage type", Category = "WeaponAbilityDamageType", TimeoutSeconds = 45f)]
        public static async Task LandedQueuedAbilityUsesWeaponDamageType(EngineTestContext ctx)
        {
            var (caster, target, knife) = await CreateFixture(ctx, "nw_wswss001", CombatDamageType.Fire);
            ctx.ApplyStandardOnHitProperty(knife);
            ctx.SetNPCResources(caster, 100, 100);
            ctx.SetNPCPerkLevel(caster, PerkType.PathogenStrike, 1);
            ResetDamage(target);
            Combat.SetAutoAttackHitResolutionOverride(true);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    CreaturePlugin.AddFeat(caster, FeatType.PathogenStrike1);
                    ctx.Assert(UsePerkFeat.TryUseAbility(caster, target, FeatType.PathogenStrike1, GetLocation(target)), "Pathogen Strike queues");
                    ActionAttack(target);
                });
                await ctx.WaitUntilAsync(() => !UsePerkFeat.HasQueuedWeaponAbility(caster), 20f, "Pathogen Strike to consume on a landed hit");
                await ctx.ExecuteInCreatureContextAsync(caster, () => ClearAllActions());
                await ctx.WaitUntilAsync(() => _damage[CombatDamageType.Fire] > 0, 5f, "Fire damage to land");
                await ctx.WaitFrameAsync();
                AssertOnlyDamageType(ctx, CombatDamageType.Fire, "Pathogen Strike and auto-attacks from a Fire knife");
            }
            finally
            {
                AssignCommand(caster, () => ClearAllActions());
                Combat.SetAutoAttackHitResolutionOverride(null);
                Combat.SetAbilityHitResolutionOverride(null);
                _observedTarget = OBJECT_INVALID;
            }
        }

        private static async Task<(uint Caster, uint Target, uint Weapon)> CreateFixture(
            EngineTestContext ctx,
            string weaponResref,
            CombatDamageType weaponDamageType)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.WaitFrameAsync();
            var weapon = await ctx.EquipItemAsync(caster, weaponResref, InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(caster, () => ConfigureWeapon(weapon, weaponDamageType));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(weaponDamageType, WeaponDamage.GetDamageType(weapon), $"{weaponResref} damage type");

            foreach (var creature in new[] { caster, target })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                Stat.SetNPCMaxHitPoints(creature, 20000, true);
                SetAILevel(creature, AILevel.VeryLow);
            }
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
            TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 60f);
            ctx.MakeHostile(target);

            return (caster, target, weapon);
        }

        private static void ConfigureWeapon(uint weapon, CombatDamageType damageType)
        {
            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.WeaponDamageType)
                    RemoveItemProperty(weapon, ip);
            }

            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 20), weapon);
            if (damageType != CombatDamageType.Physical)
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)damageType), weapon);
        }

        private static async Task ApplyImpact(
            EngineTestContext ctx,
            uint caster,
            uint target,
            FeatType feat,
            PerkType perk,
            uint impactTarget)
        {
            ctx.SetNPCPerkLevel(caster, perk, 1);
            var ability = Ability.GetAbilityDetail(feat);
            await ObserveImpact(ctx, target, async () =>
            {
                Ability.BeginAbilityImpact(caster, ability);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => ability.ImpactAction(caster, impactTarget, 1, GetLocation(impactTarget)));
                }
                finally
                {
                    Ability.EndAbilityImpact(caster);
                }
            });
        }

        private static async Task ObserveImpact(EngineTestContext ctx, uint target, Func<Task> impact)
        {
            ResetDamage(target);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                await impact();
                await ctx.WaitUntilAsync(() => _damage.Values.Sum() > 0, 10f, "ability damage to land");
                await ctx.WaitFrameAsync();
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                _observedTarget = OBJECT_INVALID;
            }
        }

        private static void ResetDamage(uint target)
        {
            foreach (var type in ObservedTypes)
                _damage[type] = 0;
            _observedTarget = target;
        }

        private static void AssertOnlyDamageType(EngineTestContext ctx, CombatDamageType expected, string label)
        {
            ctx.Assert(_damage[expected] > 0, $"{label} deals {expected} damage");
            foreach (var type in ObservedTypes.Where(type => type != expected))
                ctx.AssertEqual(0, _damage[type], $"{label} {type} damage");
        }

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void ObserveAbilityDamage()
        {
            if (_observedTarget == OBJECT_INVALID || OBJECT_SELF != _observedTarget)
                return;

            foreach (var type in ObservedTypes)
                _damage[type] += Math.Max(0, GetDamageDealtByType(type.GetNWScriptDamageType()));
        }
    }
}
