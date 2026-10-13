using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.NWN.API.NWNX;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Removes queued-deleted properties from a fresh food portion before its snapshot is saved.</summary>
    internal static class CraftedFoodSerialization
    {
        public static void Prepare(uint item)
        {
            if (!GetIsObjectValid(item) || GetTag(item) != "FOOD" || GetItemStackSize(item) != 1 ||
                GetObjectType(GetItemPossessor(item)) != ObjectType.Placeable)
                throw new InvalidOperationException("Only food portions in neutral assembly storage can be prepared for saving.");

            var visibleIds = new HashSet<ulong>();
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                visibleIds.Add(ulong.Parse(ItemPropertyPlugin.UnpackIP(property).Id));

            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var nativeItem = server.GetGameObject(item)?.AsNWSItem()
                ?? throw new InvalidOperationException("A crafted food portion was lost before saving.");
            var staleIds = new HashSet<ulong>();
            for (var index = 0; index < nativeItem.m_lstActiveProperties.Count; index++)
            {
                var property = nativeItem.GetActiveProperty(index);
                if (!visibleIds.Contains(property.m_nID))
                    staleIds.Add(property.m_nID);
            }
            for (var index = 0; index < nativeItem.m_lstPassiveProperties.Count; index++)
            {
                var property = nativeItem.GetPassiveProperty(index);
                if (!visibleIds.Contains(property.m_nID))
                    staleIds.Add(property.m_nID);
            }

            // The script iterator already omits removed enhancements, but native serialization
            // can still read their saved entries. Source IDs connect those entries to their effects.
            if (staleIds.Count > 0)
            {
                var effectIds = new List<ulong>();
                foreach (var effect in nativeItem.m_appliedEffects)
                    if (staleIds.Contains(effect.m_nItemPropertySourceId))
                        effectIds.Add(effect.m_nID);
                foreach (var effectId in effectIds)
                    nativeItem.RemoveEffectById(effectId);
                var timer = server.GetActiveTimer(item);
                nativeItem.UpdateEffectList(timer.GetWorldTimeCalendarDay(), timer.GetWorldTimeTimeOfDay());
                foreach (var effect in nativeItem.m_appliedEffects)
                    if (staleIds.Contains(effect.m_nItemPropertySourceId))
                        throw new InvalidOperationException("A removed crafted food property still has a backing effect.");
            }

            for (var index = nativeItem.m_lstActiveProperties.Count - 1; index >= 0; index--)
                if (!visibleIds.Contains(nativeItem.GetActiveProperty(index).m_nID))
                    nativeItem.RemoveActiveProperty(index);
            for (var index = nativeItem.m_lstPassiveProperties.Count - 1; index >= 0; index--)
                if (!visibleIds.Contains(nativeItem.GetPassiveProperty(index).m_nID))
                    nativeItem.RemovePassiveProperty(index);

            var savedIds = new HashSet<ulong>();
            for (var index = 0; index < nativeItem.m_lstActiveProperties.Count; index++)
                savedIds.Add(nativeItem.GetActiveProperty(index).m_nID);
            for (var index = 0; index < nativeItem.m_lstPassiveProperties.Count; index++)
                savedIds.Add(nativeItem.GetPassiveProperty(index).m_nID);
            if (!savedIds.SetEquals(visibleIds))
                throw new InvalidOperationException("A crafted food snapshot does not match its completed properties.");
        }
    }
}
