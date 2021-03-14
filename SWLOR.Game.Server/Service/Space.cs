using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Space
    {
        private static readonly Dictionary<ShipType, ShipDetail> _ships = new Dictionary<ShipType, ShipDetail>();
        private static readonly Dictionary<ShipModuleType, ShipModuleDetail> _shipModules = new Dictionary<ShipModuleType, ShipModuleDetail>();

        /// <summary>
        /// When the module loads, cache all space data into memory.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void CacheData()
        {
            LoadShips();
            LoadShipModules();

            Console.WriteLine($"Loaded {_ships.Count} ships.");
            Console.WriteLine($"Loaded {_shipModules.Count} ship modules.");
        }

        private static void LoadShips()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IShipListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IShipListDefinition)Activator.CreateInstance(type);
                var ships = instance.BuildShips();

                foreach (var (shipType, shipDetail) in ships)
                {
                    _ships.Add(shipType, shipDetail);
                }
            }
        }

        private static void LoadShipModules()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IShipModuleListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IShipModuleListDefinition)Activator.CreateInstance(type);
                var shipModules = instance.BuildShipModules();

                foreach (var (moduleType, moduleDetail) in shipModules)
                {
                    _shipModules.Add(moduleType, moduleDetail);
                }
            }
        }

        /// <summary>
        /// Retrieves a ship's detail by type.
        /// </summary>
        /// <param name="type">The type of ship to search for.</param>
        /// <returns>A ship detail matching the type.</returns>
        public static ShipDetail GetShipDetailByType(ShipType type)
        {
            return _ships[type];
        }

        /// <summary>
        /// Retrieves a ship module's detail by type.
        /// </summary>
        /// <param name="type">The type of ship module to search for.</param>
        /// <returns>A ship module detail matching the type.</returns>
        public static ShipModuleDetail GetShipModuleDetailByType(ShipModuleType type)
        {
            return _shipModules[type];
        }

        /// <summary>
        /// Sets a player's current target.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        private static void SetCurrentTarget(uint player, uint target)
        {
            // Set the VFX to the new target.
            Player.ApplyLoopingVisualEffectToObject(player, target, VisualEffect.Vfx_Target_Marker);
            SetLocalObject(player, "SPACE_TARGET", target);
        }

        /// <summary>
        /// Retrieves the currently selected target of a player. Returns OBJECT_INVALID if not set.
        /// </summary>
        /// <param name="player">The player whose target to retrieve.</param>
        /// <returns>The selected target or OBJECT_INVALID.</returns>
        private static uint GetCurrentTarget(uint player)
        {
            return GetLocalObject(player, "SPACE_TARGET");
        }

        /// <summary>
        /// Clears a player's current target.
        /// </summary>
        /// <param name="player">The player whose target will be cleared.</param>
        private static void ClearCurrentTarget(uint player)
        {
            // Remove the VFX from the current target if it exists.
            var currentTarget = GetCurrentTarget(player);
            if (GetIsObjectValid(currentTarget))
            {
                Player.ApplyLoopingVisualEffectToObject(player, currentTarget, VisualEffect.None);
            }

            DeleteLocalObject(player, "SPACE_TARGET");
        }

        /// <summary>
        /// Handles swapping a player's target to the object they attempted to attack using NWN's combat system.
        /// </summary>
        [NWNEventHandler("input_atk_bef")]
        public static void SelectTarget()
        {
            var player = OBJECT_SELF;

            if (!IsPlayerInSpaceMode(player)) return;
            Events.SkipEvent();

            ClearCurrentTarget(player);
            var target = StringToObject(Events.GetEventData("TARGET"));
            SetCurrentTarget(player, target);
        }

        /// <summary>
        /// Checks whether a player is in Space mode.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if player is in space mode, false otherwise</returns>
        public static bool IsPlayerInSpaceMode(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);
            return dbPlayer.ActiveShipId != Guid.Empty;
        }

        /// <summary>
        /// Makes the player enter space mode which changes the player's appearance, loads the ship's hot bar, etc.
        /// </summary>
        /// <param name="player">The player entering space mode.</param>
        /// <param name="shipId">The Id of the ship to enter space with.</param>
        public static void EnterSpaceMode(uint player, Guid shipId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);
            var dbPlayerShip = dbPlayer.Ships[shipId];
            var shipDetail = _ships[dbPlayerShip.Type];

            // Update player appearance to match that of the ship.
            SetCreatureAppearanceType(player, shipDetail.Appearance);

            // Set active ship Id and serialize the player's hot bar.
            dbPlayer.SerializedHotBar = Creature.SerializeQuickbar(player);
            dbPlayer.ActiveShipId = shipId;

            // Load the player's ship hot bar.
            if (!Creature.DeserializeQuickbar(player, dbPlayerShip.SerializedHotBar))
            {
                // Deserialization failed. Clear out the player's hot bar and start fresh.
                for (var slot = 0; slot <= 35; slot++)
                {
                    Player.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }

                dbPlayer.Ships[shipId].SerializedHotBar = Creature.SerializeQuickbar(player);
            }

            DB.Set(playerId, dbPlayer);
        }

        /// <summary>
        /// Makes the player exit space mode which reverts the player's appearance, loads the character's hot bar, etc.
        /// </summary>
        /// <param name="player">The player exiting space mode.</param>
        public static void ExitSpaceMode(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);
            var shipId = dbPlayer.ActiveShipId;

            ClearCurrentTarget(player);
            SetCreatureAppearanceType(player, dbPlayer.OriginalAppearanceType);

            // Save the ship's hot bar and unassign the active ship Id.
            dbPlayer.Ships[shipId].SerializedHotBar = Creature.SerializeQuickbar(player);
            dbPlayer.ActiveShipId = Guid.Empty;

            // Load the player's hot bar.
            if (!Creature.DeserializeQuickbar(player, dbPlayer.SerializedHotBar))
            {
                // Deserialization failed. Clear out the player's hot bar and start fresh.
                for (var slot = 0; slot <= 35; slot++)
                {
                    Player.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }

                dbPlayer.SerializedHotBar = Creature.SerializeQuickbar(player);
            }
            
            DB.Set(playerId, dbPlayer);
        }
    }
}
