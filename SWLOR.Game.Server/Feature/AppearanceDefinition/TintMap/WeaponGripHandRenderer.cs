using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using CNWSItem = NWN.Native.API.CNWSItem;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

/// <summary>Projects authored hand styles to the current gun's grasp or support pose.</summary>
public static class WeaponGripHandRenderer
{
    private static WeaponGripHandCatalog _catalog = new(Array.Empty<(string, string)>());
    private static readonly (AppearanceArmor Part, string Side)[] Parts =
        { (AppearanceArmor.RightHand, "r"), (AppearanceArmor.LeftHand, "l") };
    private static readonly Dictionary<(uint Creature, AppearanceArmor Part), (uint Item, ushort Source, ushort Render)> Projections = new();
    private static int _pruneAt = 1024;

    [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
    public static void Load()
    {
        var rows = new List<WeaponGripHandRow>();
        for (var row = 0; row < Get2DARowCount("griprender"); row++)
        {
            var weapon = Get2DAString("griprender", "WEAPON", row);
            var model = Get2DAString("griprender", "MODEL", row);
            rows.Add(new WeaponGripHandRow(Get2DAString("griprender", "SOURCE", row),
                Get2DAString("griprender", "RENDER", row),
                string.IsNullOrEmpty(weapon) || weapon == "****" ? (BaseItem)(-1) : Enum.Parse<BaseItem>(weapon, true),
                string.IsNullOrEmpty(model) || model == "****" ? 0 : int.Parse(model)));
        }
        _catalog = new WeaponGripHandCatalog(rows);
    }

    private static string Prefix(uint creature, string side)
    {
        if (!Get2DAString("appearance", "MODELTYPE", (int)GetAppearanceType(creature)).StartsWith("P", StringComparison.OrdinalIgnoreCase))
            return null;
        var race = Get2DAString("appearance", "RACE", (int)GetAppearanceType(creature));
        var gender = GetGender(creature);
        return race.Length == 1 && gender is Gender.Male or Gender.Female
            ? $"p{(gender == Gender.Female ? 'f' : 'm')}{race.ToLowerInvariant()}0_hand{side}" : null;
    }

    private static (BaseItem Type, int Model) Weapon(uint creature, string side)
    {
        var item = GetItemInSlot(side == "r" ? InventorySlot.RightHand : InventorySlot.LeftHand, creature);
        if (!GetIsObjectValid(item) && side == "l")
        {
            var primary = GetItemInSlot(InventorySlot.RightHand, creature);
            if (GetIsObjectValid(primary) && GetBaseItemType(primary) == BaseItem.Rifle) item = primary;
        }
        return GetIsObjectValid(item)
            ? (GetBaseItemType(item), WeaponFit.GetPart(item, AppearanceWeapon.Middle)) : ((BaseItem)(-1), 0);
    }

    private static bool CanRender(uint creature, uint armor, string side) =>
        RobeModelRenderer.GetBasePhenotype(creature) == 0 &&
        Get2DAString("appearance", "MODELTYPE", (int)GetAppearanceType(creature)).StartsWith("P", StringComparison.OrdinalIgnoreCase) &&
        (!GetIsObjectValid(armor) || GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe) <= 0 ||
         Get2DAString("parts_robe", "HIDEHAND" + side.ToUpperInvariant(),
             GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe)) == "0");

    internal static string GetHandModel(uint creature, CreaturePart part, string side)
    {
        var armor = GetItemInSlot(InventorySlot.Chest, creature);
        var prefix = Prefix(creature, side);
        if (prefix == null || !CanRender(creature, armor, side)) return string.Empty;
        var natural = GetCreatureBodyPart(part, creature);
        var worn = GetIsObjectValid(armor) ? GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part) : 0;
        var style = worn > 2 ? checked((ushort)worn) : natural;
        if (style <= 0) return string.Empty;
        var model = prefix + style.ToString("D3");
        var weapon = Weapon(creature, side);
        return _catalog.Resolve(model, weapon.Type, side, weapon.Model);
    }

    public static void Apply(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature) return;
        var armor = GetItemInSlot(InventorySlot.Chest, creature);
        var server = NWNXLib.g_pAppManager.m_pServerExoApp;
        var native = server.GetCreatureByGameObjectID(creature);
        if (native?.m_pStats == null) return;
        var refresh = false;
        foreach (var (part, side) in Parts)
        {
            var key = (creature, part);
            var prefix = Prefix(creature, side);
            if (prefix == null)
            {
                if (Projections.Remove(key, out var old)) refresh |= GetIsObjectValid(armor) && old.Item == armor;
                continue;
            }
            var eligible = CanRender(creature, armor, side);
            var weapon = eligible ? Weapon(creature, side) : (Type: (BaseItem)(-1), Model: 0);
            // As with neck/shoulder rendering, the appearance cache is distinct
            // from the stats' authored body parts. Never change the saved hand.
            var natural = native.m_pStats.m_pPartVariation[(int)part];
            var naturalRender = _catalog.ResolveStyle(prefix, natural, weapon.Type, side, weapon.Model);
            if (native.m_cAppearance.m_pPartVariation[(int)part] != naturalRender)
            {
                native.m_cAppearance.m_pPartVariation[(int)part] = naturalRender;
                server.SetForceUpdate();
            }
            var source = GetIsObjectValid(armor)
                ? checked((ushort)GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part)) : (ushort)0;
            var rendered = source > 2 ? _catalog.ResolveStyle(prefix, source, weapon.Type, side, weapon.Model) : source;
            Projections.TryGetValue(key, out var previous);
            if (rendered == source)
            {
                if (Projections.Remove(key)) refresh |= previous.Item == armor;
                continue;
            }
            var projection = (Item: armor, Source: source, Render: rendered);
            if (previous == projection) continue;
            Projections[key] = projection;
            refresh |= GetIsObjectValid(armor);
        }
        if (Projections.Count >= _pruneAt)
        {
            foreach (var key in Projections.Keys.Where(key => !GetIsObjectValid(key.Creature)).ToArray()) Projections.Remove(key);
            _pruneAt = Math.Max(1024, Projections.Count * 2);
        }
        if (refresh) EquippedItemAppearance.Refresh(creature, armor, resetShaderOverrides: false);
    }

    public static ushort GetItemProjection(CNWSItem item, AppearanceArmor part, ushort source)
    {
        if (!Projections.TryGetValue((item.m_oidPossessor, part), out var projection) ||
            projection.Item != item.m_idSelf || projection.Source != source) return source;
        var owner = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(item.m_oidPossessor);
        return owner?.m_cAppearance.m_oidChestItem == item.m_idSelf ? projection.Render : source;
    }
}
