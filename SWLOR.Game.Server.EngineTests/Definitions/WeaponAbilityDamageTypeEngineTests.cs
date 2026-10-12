using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class WeaponAbilityDamageTypeEngineTests
    {
        private static uint _observedTarget = OBJECT_INVALID;
        private static int _fireDamage;
        private static int _physicalDamage;

        [EngineTest("Weapon abilities deal the equipped weapon's elemental damage type", Category = "WeaponAbilityDamageType", TimeoutSeconds = 30f)]
        public static Task ElementalWeaponRetypesAbilityDamage(EngineTestContext ctx)
        {
            return RunDoubleShot(ctx, CombatDamageType.Fire);
        }

        [EngineTest("Weapon abilities stay Physical on a weapon without a damage type", Category = "WeaponAbilityDamageType", TimeoutSeconds = 30f)]
        public static Task UntypedWeaponKeepsPhysicalAbilityDamage(EngineTestContext ctx)
        {
            return RunDoubleShot(ctx, CombatDamageType.Physical);
        }

        private static async Task RunDoubleShot(EngineTestContext ctx, CombatDamageType weaponDamageType)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -2f);
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            var pistol = await ctx.EquipItemAsync(caster, "b_pistol", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                for (var ip = GetFirstItemProperty(pistol); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(pistol))
                {
                    if (GetItemPropertyType(ip) == ItemPropertyType.WeaponDamageType)
                        RemoveItemProperty(pistol, ip);
                }

                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 20), pistol);
                if (weaponDamageType != CombatDamageType.Physical)
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)weaponDamageType), pistol);
            });
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(weaponDamageType, WeaponDamage.GetDamageType(pistol), "Pistol damage type");

            foreach (var creature in new[] { caster, target })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                Stat.SetNPCMaxHitPoints(creature, 20000, true);
                SetAILevel(creature, AILevel.VeryLow);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 60f);
            }
            ctx.MakeHostile(target);
            ctx.SetNPCPerkLevel(caster, PerkType.DoubleShot, 1);

            var doubleShot = Ability.GetAbilityDetail(FeatType.DoubleShot1);
            _observedTarget = target;
            _fireDamage = 0;
            _physicalDamage = 0;
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                Ability.BeginAbilityImpact(caster, doubleShot);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => doubleShot.ImpactAction(caster, target, 1, GetLocation(target)));
                }
                finally
                {
                    Ability.EndAbilityImpact(caster);
                }

                await ctx.WaitUntilAsync(() => _fireDamage + _physicalDamage > 0, 10f, "Double Shot damage to land");
                await ctx.WaitFrameAsync();

                if (weaponDamageType == CombatDamageType.Fire)
                {
                    ctx.Assert(_fireDamage > 0, "Double Shot deals Fire damage from a Fire pistol");
                    ctx.AssertEqual(0, _physicalDamage, "Physical damage from a Fire pistol");
                }
                else
                {
                    ctx.Assert(_physicalDamage > 0, "Double Shot deals Physical damage from an untyped pistol");
                    ctx.AssertEqual(0, _fireDamage, "Fire damage from an untyped pistol");
                }
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                _observedTarget = OBJECT_INVALID;
            }
        }

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void ObserveAbilityDamage()
        {
            if (OBJECT_SELF != _observedTarget || _observedTarget == OBJECT_INVALID)
                return;

            _fireDamage += Math.Max(0, GetDamageDealtByType(DamageType.Fire));
            _physicalDamage += Math.Max(0, GetDamageDealtByType(CombatDamageType.Physical.GetNWScriptDamageType()));
        }
    }
}
