using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Raises the damage of rifles, saberstaves and twin blades held in the main hand or carried, folding their
/// physical damage properties into one, and marks each upgraded weapon so it is never raised twice.
/// </summary>
internal sealed class _8_UpgradeWeapons : ICharacterMigration
{
    private const string DamageUpgradeVariable = "WEAPON_DAMAGE_UPGRADED";

    private static readonly Dictionary<string, (int Old, int New)> ItemReplace = new()
    {
        { "tit_rifle", (12, 15) }, { "cap_rifle", (15, 20) }, { "mando_rifle", (13, 17) }, { "h_rifle_2", (14, 24) },
        { "del_rifle", (17, 27) }, { "poach_rifle", (18, 29) }, { "byysk_rifle", (18, 29) }, { "h_rifle_3", (19, 32) },
        { "proto_rifle", (21, 34) }, { "raider_rifle", (22, 36) }, { "h_rifle_4", (23, 38) }, { "oph_rifle", (26, 39) },
        { "h_rifle_5", (28, 42) }, { "b_twinblade", (6, 8) }, { "trn_saberstaff_1", (6, 8) }, { "twin_elec_1", (6, 8) },
        { "tit_twinblade", (10, 13) }, { "trn_saberstaff_2", (10, 13) }, { "twin_elec_2", (10, 13) }, { "sith_twinblade", (11, 15) },
        { "cap_twinblade", (13, 15) }, { "cap_saberstaff", (13, 15) }, { "mando_twinblade", (11, 13) }, { "mando_sabstaff", (11, 13) },
        { "h_twinblade_2", (11, 16) }, { "h_twinelec_2", (11, 16) }, { "del_twinblade", (15, 18) }, { "trn_saberstaff_3", (15, 18) },
        { "twin_elec_3", (15, 18) }, { "byysk_twinblade", (17, 19) }, { "poach_twinblade", (16, 19) }, { "h_twinblade_3", (16, 20) },
        { "h_twinelec_3", (16, 20) }, { "proto_twinblade", (19, 22) }, { "trn_saberstaff_4", (19, 22) }, { "twin_elec_4", (19, 22) },
        { "raider_twinblade", (20, 24) }, { "h_twinblade_4", (20, 25) }, { "h_twinelec_4", (20, 25) }, { "oph_twinblade", (24, 27) },
        { "trn_saberstaff_5", (24, 27) }, { "twin_elec_5", (24, 27) }, { "h_twinblade_5", (25, 28) }, { "h_twinelec_5", (25, 28) },
    };

    public int Version => 8;

    public void Migrate(SavedCharacter character)
    {
        var mainHand = character.GetEquipped(InventorySlot.RightHand);
        if (mainHand != null)
            Update(mainHand);
        foreach (var item in character.CarriedRecursive.ToArray())
            Update(item);
    }

    private static void Update(SavedItem item)
    {
        if (item.GetLocalInt(DamageUpgradeVariable) != 0)
            return;
        var baseItem = item.BaseItem;
        if (!Item.RifleBaseItemTypes.Contains(baseItem) && !Item.SaberstaffBaseItemTypes.Contains(baseItem) &&
            !Item.TwinBladeBaseItemTypes.Contains(baseItem))
            return;

        var (oldDamage, newDamage) = ItemReplace.TryGetValue(item.ResRef, out var change)
            ? change
            : baseItem == BaseItem.Saberstaff ? (0, 3) : (0, 0); // Saberstaves outside the list gain 3 DMG.
        var damage = newDamage - oldDamage;
        if (damage <= 0)
            return;

        foreach (var property in item.Properties.Where(property =>
                     property.Type == ItemPropertyType.DMG && property.Subtype == (int)CombatDamageType.Physical).ToArray())
        {
            damage += property.CostValue;
            item.RemoveProperty(property);
        }
        item.AddProperty(ItemPropertyType.DMG, -1, damage);
        item.SetLocalInt(DamageUpgradeVariable, 1);
    }
}
