using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature
{
    public static class LightsaberAudio
    {
        /// <summary>
        /// When a lightsaber or saberstaff is equipped, play an audio sound of the saber turning on and then apply
        /// an effect which plays the saber humming sound effect.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void EquipLightsaber()
        {
            var player = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();
            var type = GetBaseItemType(item);

            if (type != BaseItem.Lightsaber &&
                type != BaseItem.Saberstaff)
                return;

            // The hum would tell everyone nearby where a player in stealth is. It is tied to stealth
            // mode only, because leaving stealth is what gives it back (RestoreAfterHidden).
            if (!GetActionMode(player, ActionMode.Stealth))
                ApplyHum(player);

            // The ignition sound is heard only by a hidden player (in stealth or invisible).
            if (Stealth.IsConcealed(player))
            {
                if (GetIsPC(player))
                    PlayerPlugin.PlaySound(player, "saberon", player);
                return;
            }

            AssignCommand(player, () => PlaySound("saberon"));
        }

        private static void ApplyHum(uint player)
        {
            var effect = EffectVisualEffect(VisualEffect.LightsaberHum);
            effect = TagEffect(effect, "LIGHTSABER_HUM");
            ApplyEffectToObject(DurationType.Permanent, effect, player);
        }

        /// <summary>Silences every saber hum while the player is in stealth.</summary>
        public static void SilenceWhileHidden(uint player)
        {
            RemoveHum(player, OBJECT_INVALID, silenceAll: true);
        }

        /// <summary>
        /// Gives each held lightsaber or saberstaff its hum back once the player leaves stealth.
        /// </summary>
        public static void RestoreAfterHidden(uint player)
        {
            if (!GetIsObjectValid(player) || GetActionMode(player, ActionMode.Stealth))
                return;

            var missing = CountHeldSabers(player) - CountHums(player);
            for (var hum = 0; hum < missing; hum++)
                ApplyHum(player);
        }

        private static int CountHeldSabers(uint player, uint ignoredItem = OBJECT_INVALID)
        {
            var count = 0;
            foreach (var slot in new[] { InventorySlot.RightHand, InventorySlot.LeftHand })
            {
                var equipped = GetItemInSlot(slot, player);
                if (equipped == ignoredItem) continue;
                var type = GetBaseItemType(equipped);
                if (type == BaseItem.Lightsaber || type == BaseItem.Saberstaff) count++;
            }

            return count;
        }

        private static int CountHums(uint player)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player)?.AsNWSCreature();
            if (creature == null) return 0;

            var count = 0;
            foreach (var effect in creature.m_appliedEffects)
            {
                if (effect.m_sCustomTag.ToString() == "LIGHTSABER_HUM") count++;
            }

            return count;
        }

        /// <summary>
        /// When a lightsaber or saberstaff is unequipped, remove the audio sound of the saber humming and then
        /// play an audio sound of the saber turning off.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void UnequipLightsaber()
        {
            var player = GetPCItemLastUnequippedBy();
            var item = GetPCItemLastUnequipped();
            var type = GetBaseItemType(item);

            if (type != BaseItem.Lightsaber &&
                type != BaseItem.Saberstaff)
                return;

            var removed = RemoveHum(player, item);
            if (Stealth.IsConcealed(player))
            {
                if (GetIsPC(player))
                    PlayerPlugin.PlaySound(player, "saberoff", player);
            }
            else if (removed)
                AssignCommand(player, () => PlaySound("saberoff"));
        }

        internal static bool RemoveHum(uint player, uint unequippedItem = OBJECT_INVALID, bool silenceAll = false)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var creature = server.GetGameObject(player)?.AsNWSCreature();
            if (creature == null) return false;

            // One hum belongs to each equipped saber. Clear all stale duplicates when
            // unequipping, without silencing a second saber that is still being held.
            // The unequip event can fire before the inventory slot is cleared.
            var retained = silenceAll ? 0 : CountHeldSabers(player, unequippedItem);
            var removed = new List<ulong>();
            foreach (var effect in creature.m_appliedEffects)
            {
                if (effect.m_sCustomTag.ToString() != "LIGHTSABER_HUM") continue;
                if (retained > 0) retained--;
                else removed.Add(effect.m_nID);
            }
            if (removed.Count == 0) return false;

            // Script removal queues a later update; finish all removals before export.
            foreach (var id in removed) creature.RemoveEffectById(id);
            var timer = server.GetActiveTimer(player);
            creature.UpdateEffectList(timer.GetWorldTimeCalendarDay(), timer.GetWorldTimeTimeOfDay());
            Log.WriteStructured(LogGroup.Server,
                "Removed {RemovedEffectCount} lightsaber hum effects from character {PlayerId}.",
                removed.Count, GetObjectUUID(player));
            return true;
        }
    }
}
