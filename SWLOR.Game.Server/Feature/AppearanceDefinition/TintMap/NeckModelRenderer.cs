using System;
using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class NeckModelRenderer
    {
        private static NeckModelCatalog _catalog = new(Array.Empty<(string, string, string)>());
        private static readonly Dictionary<uint, (uint Item, ushort Source, ushort Render)> Projections = new();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
        {
            var rows = new List<(string, string, string)>();
            for (var row = 0; row < Get2DARowCount("neckrender"); row++)
                rows.Add((Get2DAString("neckrender", "BODY", row), Get2DAString("neckrender", "SOURCE", row),
                    Get2DAString("neckrender", "RENDER", row)));
            _catalog = new NeckModelCatalog(rows);
        }

        public static void Apply(uint creature)
        {
            var body = NeckAppearance.GetBodyModel(creature);
            if (string.IsNullOrEmpty(body))
            {
                Projections.Remove(creature);
                return;
            }
            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            var armorStyle = GetIsObjectValid(armor)
                ? GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck) : 0;
            var naked = checked((ushort)GetCreatureBodyPart(CreaturePart.Neck, creature));
            var desired = Resolve(creature, body, naked);
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var native = server.GetCreatureByGameObjectID(creature);
            if (native?.m_pStats == null)
                return;
            if (native.m_cAppearance.m_pPartVariation[(int)AppearanceArmor.Neck] != desired)
            {
                native.m_cAppearance.m_pPartVariation[(int)AppearanceArmor.Neck] = desired;
                server.SetForceUpdate();
            }

            // Equipped armor is serialized separately; its neck wins over the body.
            // Cache a projection for the packet writer, leaving all persistent fields alone.
            var renderedArmor = Resolve(creature, body, checked((ushort)armorStyle));
            Projections.TryGetValue(creature, out var previous);
            var projection = (Item: armor, Source: checked((ushort)armorStyle), Render: renderedArmor);
            if (previous == projection)
                return;
            if (Projections.Count >= 1024)
                foreach (var stale in Projections.Keys.Where(id => !GetIsObjectValid(id)).ToArray())
                    Projections.Remove(stale);
            Projections[creature] = projection;
            if (GetIsObjectValid(armor) &&
                (renderedArmor != armorStyle || previous.Item == armor && previous.Render != previous.Source))
            {
                // Publish the cache before refreshing: the nested tint application sees
                // the same projection and cannot start another equipment refresh.
                EquippedItemAppearance.Refresh(creature, armor, resetShaderOverrides: false);
            }
        }

        private static ushort Resolve(uint creature, string body, ushort source) =>
            string.IsNullOrEmpty(body) || source == 0 ? source :
                _catalog.Resolve(body, NeckAppearance.GetModel(creature, source), source);

        public static ushort GetItemProjection(CNWSItem item)
        {
            var source = item.m_nArmorModelPart[(int)AppearanceArmor.Neck];
            if (!Projections.TryGetValue(item.m_oidPossessor, out var projection) ||
                projection.Item != item.m_idSelf || projection.Source != source)
                return source;
            var owner = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(item.m_oidPossessor);
            return owner?.m_cAppearance.m_oidChestItem == item.m_idSelf ? projection.Render : source;
        }
    }
}
