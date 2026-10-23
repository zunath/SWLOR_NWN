using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Moves unedited copies of player blueprints onto the appearance editor parts their blueprints now use.
/// Weapons list bottom, middle and top parts as model * 10 + color; armor lists every part in
/// <see cref="AppearanceArmor"/> order. A copy converts only while every part its blueprint changed still
/// holds the old value; other parts, such as a robe a droid body cannot show, are left as they are.
/// </summary>
public static class ItemAppearanceCatalogMigration
{
    private const int WeaponParts = 3;
    private const int ArmorParts = (int)AppearanceArmor.Num;

    public static IReadOnlyDictionary<string, (BaseItem BaseItem, int[] Previous, int[] Current)> Revisions { get; } =
        new Dictionary<string, (BaseItem, int[], int[])>(StringComparer.OrdinalIgnoreCase)
        {
            ["absdef_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 265 }),
            ["adamguard_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 37 }),
            ["adrenal_injector"] = (BaseItem.ShortSpear, new[] { 244, 141, 93 }, new[] { 243, 141, 93 }),
            ["alpharhy_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 56 }),
            ["apexbite_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 57 }),
            ["asc_chn_tunic"] = (BaseItem.Armor, new[] { 205, 205, 16, 16, 17, 17, 117, 170, 0, 129, 1, 1, 1, 1, 0, 0, 1, 1, 236 }, new[] { 205, 205, 16, 16, 17, 17, 117, 171, 0, 129, 1, 1, 1, 1, 0, 0, 1, 1, 236 }),
            ["asc_greatsword"] = (BaseItem.GreatSword, new[] { 192, 191, 191 }, new[] { 192, 181, 191 }),
            ["asc_knife"] = (BaseItem.Dagger, new[] { 11, 14, 11 }, new[] { 11, 24, 11 }),
            ["black_cleaver"] = (BaseItem.GreatSword, new[] { 192, 191, 232 }, new[] { 192, 181, 232 }),
            ["bol_rifle"] = (BaseItem.Rifle, new[] { 11, 243, 11 }, new[] { 11, 241, 11 }),
            ["byysk_gswd"] = (BaseItem.GreatSword, new[] { 71, 53, 15 }, new[] { 72, 43, 15 }),
            ["byysk_pistol"] = (BaseItem.Sling, new[] { 11, 234, 11 }, new[] { 11, 231, 11 }),
            ["cad_rifle"] = (BaseItem.Rifle, new[] { 31, 164, 23 }, new[] { 11, 161, 11 }),
            ["cap_gswd"] = (BaseItem.GreatSword, new[] { 71, 51, 15 }, new[] { 72, 41, 15 }),
            ["cap_pistol"] = (BaseItem.Sling, new[] { 11, 233, 11 }, new[] { 11, 223, 11 }),
            ["dan_heavy_armor"] = (BaseItem.Armor, new[] { 50, 50, 12, 12, 4, 4, 1, 246, 0, 4, 1, 1, 55, 55, 0, 0, 3, 3, 192 }, new[] { 50, 50, 12, 12, 4, 4, 1, 247, 0, 4, 1, 1, 55, 55, 0, 0, 3, 3, 192 }),
            ["dancer_f_2"] = (BaseItem.Armor, new[] { 156, 156, 1, 1, 52, 52, 1, 170, 0, 101, 1, 1, 1, 1, 0, 0, 1, 1, 166 }, new[] { 156, 156, 1, 1, 52, 52, 1, 172, 0, 101, 1, 1, 1, 1, 0, 0, 1, 1, 166 }),
            ["darkhung_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 79 }),
            ["deadhand_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 75 }),
            ["deccommand_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 12 }),
            ["duel_fang"] = (BaseItem.Longsword, new[] { 231, 253, 131 }, new[] { 232, 253, 131 }),
            ["eclipse_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 267 }),
            ["embunker_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 16 }),
            ["emcocktail_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 264 }),
            ["forcebane_l1"] = (BaseItem.ShortSpear, new[] { 244, 141, 93 }, new[] { 141, 141, 93 }),
            ["forcebane_l2"] = (BaseItem.ShortSpear, new[] { 244, 141, 93 }, new[] { 104, 141, 93 }),
            ["forcebane_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 154 }),
            ["forcebane_w1"] = (BaseItem.ShortSpear, new[] { 244, 141, 93 }, new[] { 103, 141, 93 }),
            ["forcebeast_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 174 }),
            ["frontier_helmet"] = (BaseItem.Helmet, new[] { 96 }, new[] { 268 }),
            ["gatehook_spear"] = (BaseItem.ShortSpear, new[] { 244, 141, 93 }, new[] { 102, 141, 93 }),
            ["gr_tunic"] = (BaseItem.Armor, new[] { 205, 205, 16, 16, 17, 17, 117, 170, 0, 129, 1, 1, 1, 1, 0, 0, 1, 1, 236 }, new[] { 205, 205, 16, 16, 17, 17, 117, 167, 0, 129, 1, 1, 1, 1, 0, 0, 1, 1, 236 }),
            ["guardmst_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 187 }),
            ["gutter_staff"] = (BaseItem.QuarterStaff, new[] { 62, 51, 111 }, new[] { 53, 51, 41 }),
            ["h_greatsword_4"] = (BaseItem.GreatSword, new[] { 192, 191, 191 }, new[] { 192, 201, 191 }),
            ["h_knife_1"] = (BaseItem.Dagger, new[] { 11, 13, 11 }, new[] { 11, 23, 11 }),
            ["h_knife_4"] = (BaseItem.Dagger, new[] { 11, 14, 11 }, new[] { 11, 22, 11 }),
            ["h_longsword_4"] = (BaseItem.Longsword, new[] { 91, 51, 15 }, new[] { 92, 51, 15 }),
            ["h_pistol_1"] = (BaseItem.Sling, new[] { 11, 204, 11 }, new[] { 11, 203, 11 }),
            ["headwind_visor"] = (BaseItem.Helmet, new[] { 223 }, new[] { 277 }),
            ["holdline_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 263 }),
            ["infconduit_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 302 }),
            ["invinc_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 15 }),
            ["killbeacon_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 28 }),
            ["killbox_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 1 }),
            ["lastword_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 9 }),
            ["lightstand_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 123 }),
            ["line_visor"] = (BaseItem.Helmet, new[] { 209 }, new[] { 271 }),
            ["lowblow_visor"] = (BaseItem.Helmet, new[] { 223 }, new[] { 83 }),
            ["mando_pistol"] = (BaseItem.Sling, new[] { 11, 204, 11 }, new[] { 11, 191, 11 }),
            ["oneshot_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 274 }),
            ["overbarr_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 35 }),
            ["perflurry_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 276 }),
            ["pit_shockpike"] = (BaseItem.ShortSpear, new[] { 244, 141, 93 }, new[] { 101, 141, 93 }),
            ["pitwarden_pike"] = (BaseItem.ShortSpear, new[] { 244, 141, 93 }, new[] { 94, 141, 93 }),
            ["poach_pistol"] = (BaseItem.Sling, new[] { 11, 233, 11 }, new[] { 11, 243, 11 }),
            ["primover_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 54 }),
            ["proto_greatsword"] = (BaseItem.GreatSword, new[] { 192, 191, 191 }, new[] { 192, 182, 191 }),
            ["proto_knife"] = (BaseItem.Dagger, new[] { 11, 14, 11 }, new[] { 11, 21, 11 }),
            ["pulse_conduct"] = (BaseItem.QuarterStaff, new[] { 232, 234, 253 }, new[] { 53, 252, 253 }),
            ["raider_gswd"] = (BaseItem.GreatSword, new[] { 192, 191, 191 }, new[] { 192, 183, 191 }),
            ["raider_knife"] = (BaseItem.Dagger, new[] { 11, 14, 11 }, new[] { 11, 28, 11 }),
            ["raider_longsword"] = (BaseItem.Longsword, new[] { 91, 51, 15 }, new[] { 93, 51, 15 }),
            ["raider_pistol"] = (BaseItem.Sling, new[] { 11, 213, 11 }, new[] { 11, 221, 11 }),
            ["rainsteel_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 74 }),
            ["redcircle_star"] = (BaseItem.Shuriken, new[] { 62 }, new[] { 42 }),
            ["redvein_pistol"] = (BaseItem.Sling, new[] { 231, 101, 61 }, new[] { 11, 101, 11 }),
            ["sabcycl_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 7 }),
            ["sabstorm_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 299 }),
            ["sandstorm_gaffi"] = (BaseItem.QuarterStaff, new[] { 62, 11, 11 }, new[] { 53, 11, 11 }),
            ["sc_greatswor"] = (BaseItem.GreatSword, new[] { 154, 154, 154 }, new[] { 154, 153, 154 }),
            ["scav_visor"] = (BaseItem.Helmet, new[] { 223 }, new[] { 110 }),
            ["scraplock_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 269 }),
            ["servo_pistol"] = (BaseItem.Sling, new[] { 11, 221, 71 }, new[] { 11, 241, 11 }),
            ["soulasc_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 262 }),
            ["sump_vknife"] = (BaseItem.Dagger, new[] { 81, 94, 93 }, new[] { 61, 71, 73 }),
            ["sysshut_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 2 }),
            ["thermdet_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 119 }),
            ["unbrbeast_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 275 }),
            ["unmovctr_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 8 }),
            ["untinst_l5"] = (BaseItem.Helmet, new[] { 223 }, new[] { 73 }),
            ["vitrupt_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 11 }),
            ["worldbrk_l7"] = (BaseItem.Helmet, new[] { 223 }, new[] { 109 }),
        };

    internal static bool IsLegacyAppearance(uint item)
    {
        if (!TryGetRevision(item, out var revision))
            return false;
        var parts = ReadParts(item, revision.Previous.Length);
        return Enumerable.Range(0, parts.Length)
            .All(index => revision.Current[index] == revision.Previous[index] || parts[index] == revision.Previous[index]);
    }

    internal static bool ConvertItem(uint item)
    {
        if (!IsLegacyAppearance(item))
            return false;
        var revision = Revisions[GetResRef(item)];
        for (var index = 0; index < revision.Current.Length; index++)
            if (revision.Current[index] != revision.Previous[index])
                WritePart(item, revision.Current.Length, index, revision.Current[index]);

        // Stored copies load outside any area; only a wearer in the world has clients to refresh.
        var wearer = GetItemPossessor(item);
        var liveWearer = GetIsObjectValid(GetArea(wearer)) &&
                         (GetIsPC(wearer) && !GetIsDM(wearer) && !GetIsDMPossessed(wearer) || Droid.IsDroid(wearer));
        if (liveWearer && Enumerable.Range(0, NumberOfInventorySlots).Any(slot => GetItemInSlot((InventorySlot)slot, wearer) == item))
            EquippedItemAppearance.Refresh(wearer, item, resetShaderOverrides: false);
        return true;
    }

    internal static int[] ReadParts(uint item, int count) =>
        Enumerable.Range(0, count).Select(index => count switch
        {
            WeaponParts => GetItemAppearance(item, ItemAppearanceType.WeaponModel, index) * 10 +
                           GetItemAppearance(item, ItemAppearanceType.WeaponColor, index),
            ArmorParts => GetItemAppearance(item, ItemAppearanceType.ArmorModel, index),
            _ => GetItemAppearance(item, ItemAppearanceType.SimpleModel, index),
        }).ToArray();

    private static bool TryGetRevision(uint item, out (BaseItem BaseItem, int[] Previous, int[] Current) revision)
    {
        revision = default;
        return GetIsObjectValid(item) && GetObjectType(item) == ObjectType.Item &&
               Revisions.TryGetValue(GetResRef(item), out revision) && revision.BaseItem == GetBaseItemType(item);
    }

    private static void WritePart(uint item, int count, int index, int value)
    {
        switch (count)
        {
            case WeaponParts:
                EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponModel, index, value / 10);
                EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponColor, index, value % 10);
                break;
            case ArmorParts:
                // A cosmetic part change keeps the armor's value and weight.
                EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorModel, index, value);
                break;
            default:
                EquippedItemAppearance.Set(item, ItemAppearanceType.SimpleModel, index, value);
                break;
        }
    }
}
