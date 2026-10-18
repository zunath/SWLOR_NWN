using System;
using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Projects equipped armor parts whose external skins would bind to shifted bones on a robe body
    /// onto aliases compiled for that body's skeleton. Necks and male human shoulders have their own renderers.
    /// Naked body parts keep their authored IDs: the appearance editor and droid records read them directly.
    /// </summary>
    public static class PartSkinModelRenderer
    {
        private static PartSkinModelCatalog _catalog = new(
            Array.Empty<(string, string)>(), Array.Empty<(string, string, string)>());
        public static readonly AppearanceArmor[] Parts = PartSkinModelCatalog.PartNames.Keys.ToArray();
        // Holds only parts that currently render through an alias.
        private static readonly Dictionary<(uint Creature, AppearanceArmor Part), (uint Item, ushort Source, ushort Render)> Projections = new();
        private static int _pruneAt = 1024;

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
        {
            var layouts = new List<(string, string)>();
            for (var row = 0; row < Get2DARowCount("partlayout"); row++)
                layouts.Add((Get2DAString("partlayout", "BODY", row), Get2DAString("partlayout", "LAYOUT", row)));
            var renders = new List<(string, string, string)>();
            for (var row = 0; row < Get2DARowCount("partrender"); row++)
                renders.Add((Get2DAString("partrender", "LAYOUT", row), Get2DAString("partrender", "SOURCE", row),
                    Get2DAString("partrender", "RENDER", row)));
            _catalog = new PartSkinModelCatalog(layouts, renders);
        }

        public static void Apply(uint creature)
        {
            var body = NeckAppearance.GetBodyModel(creature);
            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            if (string.IsNullOrEmpty(body) || !GetIsObjectValid(armor))
            {
                foreach (var part in Parts)
                    Projections.Remove((creature, part));
                return;
            }
            var refresh = false;
            foreach (var part in Parts)
            {
                var source = checked((ushort)GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part));
                var rendered = _catalog.Resolve(body, part, source);
                var key = (creature, part);
                var projected = Projections.TryGetValue(key, out var previous);
                if (rendered == source)
                {
                    // A dropped alias must be resent for the same armor (robe removed or part changed).
                    if (Projections.Remove(key))
                        refresh |= previous.Item == armor;
                    continue;
                }
                var projection = (Item: armor, Source: source, Render: rendered);
                if (projected && previous == projection)
                    continue;
                Projections[key] = projection;
                refresh = true;
            }
            if (Projections.Count >= _pruneAt)
            {
                foreach (var key in Projections.Keys.Where(key => !GetIsObjectValid(key.Creature)).ToArray())
                    Projections.Remove(key);
                // Grow past the live population so a full cache is not rescanned on every call.
                _pruneAt = Math.Max(1024, Projections.Count * 2);
            }
            // Publish every projection before refreshing, so nested tint application terminates.
            if (refresh)
                EquippedItemAppearance.Refresh(creature, armor, resetShaderOverrides: false);
        }

        public static ushort GetItemProjection(CNWSItem item, AppearanceArmor part, ushort source)
        {
            if (!Projections.TryGetValue((item.m_oidPossessor, part), out var projection) ||
                projection.Item != item.m_idSelf || projection.Source != source)
                return source;
            var owner = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(item.m_oidPossessor);
            return owner?.m_cAppearance.m_oidChestItem == item.m_idSelf ? projection.Render : source;
        }
    }
}
