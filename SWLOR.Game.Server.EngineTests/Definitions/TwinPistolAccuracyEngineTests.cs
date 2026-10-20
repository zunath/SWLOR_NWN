using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolAccuracyEngineTests
{
    [EngineTest("Balanced Grip grants general accuracy with a rifle", Category = "TwinPistolCrossSkillAccuracy", TimeoutSeconds = 45f)]
    public static Task RifleAccuracy(EngineTestContext ctx) => VerifyAccuracy(ctx, "b_rifle", false);

    [EngineTest("Balanced Grip grants general accuracy with a two-handed melee weapon", Category = "TwinPistolCrossSkillAccuracy", TimeoutSeconds = 45f)]
    public static Task TwoHandedAccuracy(EngineTestContext ctx) => VerifyAccuracy(ctx, "b_greatsword", false);

    [EngineTest("Balanced Grip benefits both Twin Pistol hands once without doubling the global bonus", Category = "TwinPistolCrossSkillAccuracy", TimeoutSeconds = 45f)]
    public static Task PairedAccuracy(EngineTestContext ctx) => VerifyAccuracy(ctx, "b_twinpistol", true);

    private static async Task VerifyAccuracy(EngineTestContext ctx, string blueprint, bool paired)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Perks.Clear();
            foreach (var skill in record.Skills.Values) skill.Rank = 50;
        });
        var main = await ctx.EquipItemAsync(actor, blueprint, InventorySlot.RightHand);
        if (paired) await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        var weapons = paired ? new[] { main, GetItemInSlot(InventorySlot.LeftHand, actor) } : new[] { main };
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            var before = weapons.Select(weapon => WeaponShotProfile.Capture(actor, weapon, SkillType.TwinPistol).Accuracy).ToArray();
            var forceBefore = Stat.GetAccuracy(actor, main, AbilityType.Willpower, SkillType.Force,
                ignoreWeaponAccuracyStatOverride: true);
            var globalBefore = Stat.GetStatAdjustment(actor, StatType.Accuracy);
            player.Update(record => record.Perks[PerkType.BalancedGrip] = 1);
            ctx.AssertEqual(globalBefore + 5, Stat.GetStatAdjustment(actor, StatType.Accuracy),
                "Purchasing Balanced Grip adds one general accuracy bonus without checking an equipment slot");
            for (var index = 0; index < weapons.Length; index++)
                ctx.AssertEqual(before[index] + 5,
                    WeaponShotProfile.Capture(actor, weapons[index], SkillType.TwinPistol).Accuracy,
                    "Each real weapon receives five accuracy, never ten when two weapons are equipped");
            ctx.AssertEqual(forceBefore + 5,
                Stat.GetAccuracy(actor, main, AbilityType.Willpower, SkillType.Force, ignoreWeaponAccuracyStatOverride: true),
                "General accuracy also benefits Force accuracy without requiring a Twin Pistol ability");
            player.Update(record => record.Perks.Remove(PerkType.BalancedGrip));
            ctx.AssertEqual(globalBefore, Stat.GetStatAdjustment(actor, StatType.Accuracy),
                "Removing the perk removes the general bonus without leaving a stale off-hand adjustment");
            ctx.SetResultDetail($"{blueprint}: +5 ACC on {weapons.Length} weapon(s) and Force accuracy; no off-hand requirement or doubled bonus.");
        });
    }
}
