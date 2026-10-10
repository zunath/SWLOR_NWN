using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Saved melee damage and requirements migrate without losing customization", Category = "MigrationMeleeWeapon")]
    public static async Task SavedMeleeBonusesAndRetry(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var profiles = new[]
        {
            (Resref: "b_longsword", Skill: SkillType.Vibroblade, OldDamage: 24, NewDamage: 27, Delay: 23),
            (Resref: "b_knife", Skill: SkillType.Vibroknife, OldDamage: 22, NewDamage: 24, Delay: 22),
            (Resref: "b_greatsword", Skill: SkillType.HeavyVibroblade, OldDamage: 43, NewDamage: 47, Delay: 30),
            (Resref: "b_spear", Skill: SkillType.Spear, OldDamage: 43, NewDamage: 45, Delay: 28),
            (Resref: "b_twinblade", Skill: SkillType.TwinBlade, OldDamage: 27, NewDamage: 27, Delay: 23),
            (Resref: "b_katar", Skill: SkillType.Katar, OldDamage: 19, NewDamage: 21, Delay: 22),
            (Resref: "b_staff", Skill: SkillType.Staff, OldDamage: 24, NewDamage: 27, Delay: 27),
        };

        foreach (var profile in profiles)
        {
            var item = await CreateItemAsync(ctx, owner, profile.Resref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var original = new List<SWLOR.NWN.API.Engine.ItemProperty>();
                for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                    original.Add(property);

                // Stamp historical saved rows, including damage subtypes removed
                // from the current 2DA. Current constructors cannot build them.
                AddLegacyProperty(item, ItemPropertyType.DMG, (int)CombatDamageType.Physical, 34, profile.OldDamage);
                AddLegacyProperty(item, ItemPropertyType.DMG, (int)CombatDamageType.Force, 34, 4);
                AddLegacyProperty(item, ItemPropertyType.DMG, (int)CombatDamageType.Fire, 34, 3);
                AddLegacyProperty(item, ItemPropertyType.UseLimitationPerk, 6, 33, 5);
                // Retain an installed custom Accuracy bonus while the remaining
                // legacy damage and equipment requirement properties migrate.
                AddLegacyProperty(item, ItemPropertyType.Accuracy, 65535, 45, 8);
                foreach (var property in original)
                    Invoke("MigrationObject", "RemoveProperty", item, property);
                SetName(item, "Custom " + profile.Resref);
                SetLocalString(item, "MELEE_MIGRATION_FIXTURE", "preserve");
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var saved = Deserialize(ctx, ObjectPlugin.Serialize(item));
                ctx.AssertEqual(profile.OldDamage + 7, MeleePropertyTotal(saved, ItemPropertyType.DMG), "Fixture retains all three legacy damage components");
                ctx.AssertEqual(8, MeleePropertyTotal(saved, ItemPropertyType.Accuracy), "Fixture retains its installed accuracy bonus");

                var migrated = MigrateSerialized(ctx, saved);
                ctx.AssertEqual(profile.NewDamage, MeleePropertyTotal(migrated, ItemPropertyType.DMG), profile.Resref + " rebalanced damage includes the bonuses");
                ctx.AssertEqual(8, MeleePropertyTotal(migrated, ItemPropertyType.Accuracy), "Installed accuracy survives damage and requirement conversion");
                ctx.AssertEqual(profile.Delay, PropertyValue(migrated, ItemPropertyType.Delay), "Current melee delay is present");
                ctx.AssertEqual(40, PropertyValue(migrated, ItemPropertyType.RequiresSkill, (int)profile.Skill), "Correct melee skill requirement is present");
                ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.UseLimitationPerk), "No unrelated retired perk requirement remains");
                ctx.AssertEqual("Custom " + profile.Resref, GetName(migrated), "Custom name survives");
                ctx.AssertEqual("preserve", GetLocalString(migrated, "MELEE_MIGRATION_FIXTURE"), "Unrelated local data survives");
                if (profile.Skill == SkillType.Vibroknife)
                    ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.WeaponDamageType), "Vibroknives use physical damage");
                else
                    AssertDamageType(ctx, migrated, CombatDamageType.Fire);

                var properties = DescribeProperties(migrated);
                var retried = MigrateSerialized(ctx, migrated);
                ctx.AssertEqual(properties, DescribeProperties(retried), "Save and retry do not remove bonuses or rescale damage again");
                ctx.Log($"Verified {profile.Resref}: DMG {profile.OldDamage + 7} -> {profile.NewDamage}, Accuracy 8, {profile.Skill} 40.");
            });
            await ctx.WaitFrameAsync();
        }
    }

    private static int MeleePropertyTotal(uint item, ItemPropertyType type)
    {
        var total = 0;
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            if (GetItemPropertyType(property) == type) total += GetItemPropertyCostTableValue(property);
        return total;
    }
}
