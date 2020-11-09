using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Resistance
    {
        private class ResistanceDetail
        {
            private float _amount;

            public float Amount
            {
                get => _amount;
                set
                {
                    _amount = value;

                    if (_amount < 0.1f)
                        _amount = 0.1f;
                    else if (_amount > 30f)
                        _amount = 30f;
                }
            }
            public DateTime Expiration { get; set; }

            public ResistanceDetail()
            {
                Amount = 1.0f;
                Expiration = DateTime.UtcNow.AddMinutes(1);
            }
        }

        private static readonly Dictionary<uint, Dictionary<ResistanceType, ResistanceDetail>> _creatureResistances = new Dictionary<uint, Dictionary<ResistanceType, ResistanceDetail>>();

        /// <summary>
        /// Adjusts the resistance of a creature by a specified amount.
        /// Note that their resistance can never fall below 0.1 or rise above 30.
        /// </summary>
        /// <param name="creature">The creature to adjust.</param>
        /// <param name="type">The type of resistance to adjust.</param>
        /// <param name="amount">The amount to adjust by. Cannot equal zero.</param>
        public static void ModifyResistance(uint creature, ResistanceType type, float amount)
        {
            if (amount == 0.0f) return;

            if(!_creatureResistances.ContainsKey(creature))
                _creatureResistances[creature] = new Dictionary<ResistanceType, ResistanceDetail>();

            if(!_creatureResistances[creature].ContainsKey(type))
                _creatureResistances[creature][type] = new ResistanceDetail();

            _creatureResistances[creature][type].Amount += amount;
            _creatureResistances[creature][type].Expiration = DateTime.UtcNow.AddMinutes(1);
        }

        /// <summary>
        /// Retrieves the amount of resistance a creature has towards a type of resistance.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of resistance to retrieve.</param>
        /// <returns>A value between 0.1 and 30 which determines resistance level.</returns>
        public static float GetResistance(uint creature, ResistanceType type)
        {
            // todo: magic bonus 
            var tempResistance = GetTemporaryResistance(creature, type);
            var skinAdjustment = GetSkinResistance(creature, type);
            var resistance = tempResistance + skinAdjustment;

            if (resistance < 0.1f)
                resistance = 0.1f;
            else if (resistance > 30f)
                resistance = 30f;

            return resistance;
        }

        /// <summary>
        /// Retrieves the temporary resistance a creature has towards a specific resistance type.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of resistance to retrieve.</param>
        /// <returns>A value representing the creature's temporary resistance.</returns>
        private static float GetTemporaryResistance(uint creature, ResistanceType type)
        {
            if (!_creatureResistances.ContainsKey(creature)) return 1.0f;
            if (!_creatureResistances[creature].ContainsKey(type)) return 1.0f;

            var tempResistance = _creatureResistances[creature][type];
            if (DateTime.UtcNow > tempResistance.Expiration) return 1.0f;

            return tempResistance.Amount;
        }

        /// <summary>
        /// Retrieves the resistance adjustment granted by a creature's skin.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of resistance to retrieve.</param>
        /// <returns>A value representing the skin's bonus or penalty toward a resistance. Returns 0.0 if non-existent.</returns>
        private static float GetSkinResistance(uint creature, ResistanceType type)
        {
            if (GetIsPC(creature) || GetIsDM(creature)) return 0.0f;

            var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);
            if (!GetIsObjectValid(skin)) return 0.0f;

            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.Invalid) continue;
                var ipResistanceType = (ResistanceType) GetItemPropertySubType(ip);
                if (ipResistanceType != type) continue;

                var amountId = GetItemPropertyCostTableValue(ip);
                var amount = 0;

                // Positive numbers range from IDs 1 to 30.
                if (amountId <= 30)
                {
                    amount = amountId;
                }
                // Negative numbers range from IDs 31 to 60.
                else if (amountId >= 31)
                {
                    amount = -(amountId - 30);
                }

                return amount * 0.1f;
            }

            return 0.0f;
        }

        /// <summary>
        /// Removes a creature's resistances from the cache.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        private static void RemoveResistance(uint creature)
        {
            if (!_creatureResistances.ContainsKey(creature)) return;
            _creatureResistances.Remove(creature);
        }

        /// <summary>
        /// When a creature dies, remove it from the cache.
        /// </summary>
        [NWNEventHandler("crea_death")]
        public static void CreatureDeath()
        {
            var creature = OBJECT_SELF;
            RemoveResistance(creature);
        }

        /// <summary>
        /// When a player leaves, remove it from the cache.
        /// </summary>
        [NWNEventHandler("mod_exit")]
        public static void PlayerExit()
        {
            var player = GetExitingObject();
            RemoveResistance(player);
        }

        /// <summary>
        /// When a player dies, remove it from the cache.
        /// </summary>
        [NWNEventHandler("mod_death")]
        public static void PlayerDeath()
        {
            var player = GetLastPlayerDied();
            RemoveResistance(player);
        }
    }
}
