using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Space
    {
        public const int MaxRegisteredShips = 10;

        private static readonly Dictionary<string, ShipDetail> _ships = new Dictionary<string, ShipDetail>();
        private static readonly Dictionary<string, ShipModuleDetail> _shipModules = new Dictionary<string, ShipModuleDetail>();
        private static readonly List<ShipModuleFeat> _shipModuleFeats = ShipModuleFeat.GetAll();

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
                    // Give warning if ship module is active and short name is longer than GUI will allow.
                    if (moduleDetail.ShortName.Length > 14 &&
                        !moduleDetail.IsPassive)
                    {
                        Log.Write(LogGroup.Space, $"Ship module with short name {moduleDetail.ShortName} is longer than 14 characters. Short names should be no more than 14 characters so they display on the UI properly.", true);
                    }

                    _shipModules.Add(moduleType, moduleDetail);
                }
            }
        }

        /// <summary>
        /// Retrieves a ship's detail by type.
        /// </summary>
        /// <param name="itemTag">The item tag to search for.</param>
        /// <returns>A ship detail matching the type.</returns>
        public static ShipDetail GetShipDetailByItemTag(string itemTag)
        {
            return _ships[itemTag];
        }

        /// <summary>
        /// Retrieves a ship module's detail by type.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship module to search for.</param>
        /// <returns>A ship module detail matching the type.</returns>
        public static ShipModuleDetail GetShipModuleDetailByItemTag(string itemTag)
        {
            return _shipModules[itemTag];
        }

        /// <summary>
        /// Determines whether an item's tag is registered to a ship module.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship to search for.</param>
        /// <returns>true if registered, false otherwise</returns>
        public static bool IsRegisteredShip(string itemTag)
        {
            return _ships.ContainsKey(itemTag);
        }

        /// <summary>
        /// Determines whether an item's tag is registered to a ship module.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship module to search for.</param>
        /// <returns>true if registered, false otherwise</returns>
        public static bool IsRegisteredShipModule(string itemTag)
        {
            return _shipModules.ContainsKey(itemTag);
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
            var shipDetail = _ships[dbPlayerShip.ItemTag];

            // Update player appearance to match that of the ship.
            SetCreatureAppearanceType(player, shipDetail.Appearance);

            // Set active ship Id and serialize the player's hot bar.
            dbPlayer.SerializedHotBar = Creature.SerializeQuickbar(player);
            dbPlayer.ActiveShipId = shipId;

            // Load ship modules as feats.
            var allModules = dbPlayerShip.HighPowerModules.Concat(dbPlayerShip.LowPowerModules).ToList();
            for (var index = 0; index < _shipModuleFeats.Count-1; index++)
            {
                // No more feats to add to the player. Exit early.
                if (index + 1 > allModules.Count) break;

                var equippedModule = allModules[index];
                var shipModuleDetail = _shipModules[equippedModule.Value.ItemTag];

                // Passive modules shouldn't be converted to feats.
                if (shipModuleDetail.IsPassive) continue;

                // Convert current ship module to feat.
                var shipModuleFeat = _shipModuleFeats[index];
                Creature.AddFeat(player, shipModuleFeat.Feat);

                // Rename the feat to match the configured name on the ship module.
                Player.SetTlkOverride(player, shipModuleFeat.NameTlkId, shipModuleDetail.Name);
                Player.SetTlkOverride(player, shipModuleFeat.DescriptionTlkId, shipModuleDetail.Description);

                index++;
            }

            // Load the player's ship hot bar.
            if (string.IsNullOrWhiteSpace(dbPlayerShip.SerializedHotBar) ||
                !Creature.DeserializeQuickbar(player, dbPlayerShip.SerializedHotBar))
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

            // Remove all module feats from the player.
            foreach (var moduleFeat in _shipModuleFeats)
            {
                Creature.RemoveFeat(player, moduleFeat.Feat);
            }

            // Load the player's hot bar.
            if (string.IsNullOrWhiteSpace(dbPlayer.SerializedHotBar) ||
                !Creature.DeserializeQuickbar(player, dbPlayer.SerializedHotBar))
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

        /// <summary>
        /// Determines if player can still use their ship.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="playerShipId">The Id of the player's ship to check</param>
        /// <returns>true if all requirements are met, false otherwise</returns>
        public static bool CanPlayerUseShip(uint player, Guid playerShipId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);

            if (!dbPlayer.Ships.ContainsKey(playerShipId)) return false;

            var playerShip = dbPlayer.Ships[playerShipId];
            var shipDetails = _ships[playerShip.ItemTag];

            // Check ship requirements
            foreach (var (perkType, requiredLevel) in shipDetails.RequiredPerks)
            {
                if (!dbPlayer.Perks.ContainsKey(perkType)) return false;

                if (dbPlayer.Perks[perkType] < requiredLevel) return false;
            }

            foreach (var (itemTag, _) in playerShip.HighPowerModules)
            {
                if (!CanPlayerUseShipModule(player, itemTag)) return false;
            }

            foreach (var (itemTag, _) in playerShip.LowPowerModules)
            {
                if (!CanPlayerUseShipModule(player, itemTag)) return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if player can use a ship module by its tag.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="itemTag">The ship module item tag</param>
        /// <returns>true if player can use the module, false otherwise</returns>
        public static bool CanPlayerUseShipModule(uint player, string itemTag)
        {
            if (!_shipModules.ContainsKey(itemTag)) return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);
            var shipModule = _shipModules[itemTag];

            foreach (var (perkType, requiredLevel) in shipModule.RequiredPerks)
            {
                if (!dbPlayer.Perks.ContainsKey(perkType)) return false;

                if (dbPlayer.Perks[perkType] < requiredLevel) return false;
            }

            return true;
        }

        /// <summary>
        /// When a ship module item is examined,
        /// append the configured description to the item's description and add prerequisite perk item properties.
        /// </summary>
        [NWNEventHandler("examine_bef")]
        public static void ExamineShipModuleItem()
        {
            var item = StringToObject(Events.GetEventData("EXAMINEE_OBJECT_ID"));

            // Must be an item
            if (GetObjectType(item) != ObjectType.Item) return;

            // Tag must be registered with the system.
            var itemTag = GetTag(item);
            if (!_shipModules.ContainsKey(itemTag)) return;

            var moduleDetail = GetShipModuleDetailByItemTag(itemTag);

            // A description must have been set.
            if (string.IsNullOrWhiteSpace(moduleDetail.Description)) return;

            // Append the ship module's description to the item's description.
            var description = GetDescription(item);
            description += moduleDetail.Description + "\n";
            SetDescription(item, description);

            // Apply item properties defined in code.
            foreach (var (perkType, level) in moduleDetail.RequiredPerks)
            {
                var ip = ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, (int)perkType, level);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            }
        }

        /// <summary>
        /// When a ship item is examined, add the prerequisite perk item properties.
        /// </summary>
        [NWNEventHandler("examine_bef")]
        public static void ExamineShipItem()
        {
            var item = StringToObject(Events.GetEventData("EXAMINEE_OBJECT_ID"));

            // Must be an item
            if (GetObjectType(item) != ObjectType.Item) return;

            // Tag must be registered with the system.
            var itemTag = GetTag(item);
            if (!_ships.ContainsKey(itemTag)) return;
            var shipDetail = _ships[itemTag];

            foreach (var (perkType, level) in shipDetail.RequiredPerks)
            {
                var ip = ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, (int)perkType, level);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            }
        }
        

    }
}
