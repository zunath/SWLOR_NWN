using SWLOR.Game.Server.Core;
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

            var effect = EffectVisualEffect(VisualEffect.LightsaberHum);
            effect = TagEffect(effect, "LIGHTSABER_HUM");

            ApplyEffectToObject(DurationType.Permanent, effect, player);
            AssignCommand(player, () => PlaySound("saberon"));
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

            if (RemoveHum(player))
                AssignCommand(player, () => PlaySound("saberoff"));
        }

        internal static bool RemoveHum(uint player)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var creature = server.GetGameObject(player)?.AsNWSCreature();
            if (creature == null) return false;

            foreach (var effect in creature.m_appliedEffects)
            {
                if (effect.m_sCustomTag.ToString() == "LIGHTSABER_HUM")
                {
                    // The script removal queues a later update, allowing an intervening
                    // character export to retain the hum after the saber is unequipped.
                    creature.RemoveEffectById(effect.m_nID);
                    var timer = server.GetActiveTimer(player);
                    creature.UpdateEffectList(timer.GetWorldTimeCalendarDay(), timer.GetWorldTimeTimeOfDay());
                    return true;
                }
            }
            return false;
        }
    }
}
