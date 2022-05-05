using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature
{
    public static class LightsaberAudio
    {
        /// <summary>
        /// When a lightsaber or saberstaff is equipped, play an audio sound of the saber turning on and then apply
        /// an effect which plays the saber humming sound effect.
        /// </summary>
        [NWNEventHandler("mod_equip")]
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
        [NWNEventHandler("mod_unequip")]
        public static void UnequipLightsaber()
        {
            var player = GetPCItemLastUnequippedBy();
            var item = GetPCItemLastUnequipped();
            var type = GetBaseItemType(item);

            if (type != BaseItem.Lightsaber &&
                type != BaseItem.Saberstaff)
                return;

            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                if (GetEffectTag(effect) == "LIGHTSABER_HUM")
                {
                    RemoveEffect(player, effect);
                    AssignCommand(player, () => PlaySound("saberoff"));
                    return;
                }
            }
            
        }
    }
}
