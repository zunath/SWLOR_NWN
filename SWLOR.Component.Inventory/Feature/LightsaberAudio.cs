using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Inventory.Feature
{
    public class LightsaberAudio
    {
        public LightsaberAudio(IEventAggregator eventAggregator)
        {
            // Subscribe to events
            eventAggregator.Subscribe<OnModuleEquip>(e => EquipLightsaber());
            eventAggregator.Subscribe<OnModuleUnequip>(e => UnequipLightsaber());
        }

        /// <summary>
        /// When a lightsaber or saberstaff is equipped, play an audio sound of the saber turning on and then apply
        /// an effect which plays the saber humming sound effect.
        /// </summary>
        public void EquipLightsaber()
        {
            var player = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();
            var type = GetBaseItemType(item);

            if (type != BaseItemType.Lightsaber &&
                type != BaseItemType.Saberstaff)
                return;

            var effect = EffectVisualEffect(VisualEffectType.LightsaberHum);
            effect = TagEffect(effect, "LIGHTSABER_HUM");
            
            ApplyEffectToObject(DurationType.Permanent, effect, player);
            AssignCommand(player, () => PlaySound("saberon"));
        }

        /// <summary>
        /// When a lightsaber or saberstaff is unequipped, remove the audio sound of the saber humming and then
        /// play an audio sound of the saber turning off.
        /// </summary>
        public void UnequipLightsaber()
        {
            var player = GetPCItemLastUnequippedBy();
            var item = GetPCItemLastUnequipped();
            var type = GetBaseItemType(item);

            if (type != BaseItemType.Lightsaber &&
                type != BaseItemType.Saberstaff)
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
