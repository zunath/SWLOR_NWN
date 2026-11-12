using System;
using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Projects the equipped neck, shoulder and part-skin models into the appearance the engine serializes for a client,
/// then restores the authored model ids.
/// </summary>
public sealed unsafe class NeckAppearanceSerializationSink : INeckAppearanceSerializationSink
{
    public void Write(void* message, void* player, void* itemPointer, nint original)
    {
        var item = CNWSItem.FromPointer(itemPointer);
        var source = item.m_nArmorModelPart[(int)AppearanceArmor.Neck];
        var leftShoulder = item.m_nArmorModelPart[(int)AppearanceArmor.LeftShoulder];
        var rightShoulder = item.m_nArmorModelPart[(int)AppearanceArmor.RightShoulder];
        var parts = PartSkinModelRenderer.Parts;
        Span<ushort> authored = stackalloc ushort[parts.Length];
        for (var index = 0; index < parts.Length; index++)
            authored[index] = item.m_nArmorModelPart[(int)parts[index]];
        try
        {
            try { item.m_nArmorModelPart[(int)AppearanceArmor.Neck] = NeckModelRenderer.GetItemProjection(item); }
            catch (Exception ex) { Log.WriteError(ex, "Could not project the equipped neck model"); }
            try
            {
                item.m_nArmorModelPart[(int)AppearanceArmor.LeftShoulder] = ShoulderModelRenderer.GetItemProjection(item, AppearanceArmor.LeftShoulder);
                item.m_nArmorModelPart[(int)AppearanceArmor.RightShoulder] = ShoulderModelRenderer.GetItemProjection(item, AppearanceArmor.RightShoulder);
            }
            catch (Exception ex) { Log.WriteError(ex, "Could not project the equipped shoulder models"); }
            try
            {
                // Male human shoulders keep the shoulder renderer's projection.
                for (var index = 0; index < parts.Length; index++)
                {
                    var rendered = PartSkinModelRenderer.GetItemProjection(item, parts[index], authored[index]);
                    if (rendered != authored[index])
                        item.m_nArmorModelPart[(int)parts[index]] = rendered;
                }
            }
            catch (Exception ex) { Log.WriteError(ex, "Could not project the equipped part skin models"); }
            ((delegate* unmanaged<void*, void*, void*, void>)original)(message, player, itemPointer);
        }
        finally
        {
            // The synchronous native writer only serializes appearance. Inventory,
            // saves, outfit copies, stats, and subsequent scripts see the authored ID.
            item.m_nArmorModelPart[(int)AppearanceArmor.Neck] = source;
            item.m_nArmorModelPart[(int)AppearanceArmor.LeftShoulder] = leftShoulder;
            item.m_nArmorModelPart[(int)AppearanceArmor.RightShoulder] = rightShoulder;
            for (var index = 0; index < parts.Length; index++)
                item.m_nArmorModelPart[(int)parts[index]] = authored[index];
        }
    }
}
