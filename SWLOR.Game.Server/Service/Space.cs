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
using Object = SWLOR.Game.Server.Core.NWNX.Object;

namespace SWLOR.Game.Server.Service
{
    public static class Space
    {
        public const int MaxRegisteredShips = 10;

        private static readonly Dictionary<string, ShipDetail> _ships = new Dictionary<string, ShipDetail>();
        private static readonly Dictionary<string, ShipModuleDetail> _shipModules = new Dictionary<string, ShipModuleDetail>();
        private static readonly Dictionary<uint, ShipStatus> _shipNPCs = new Dictionary<uint, ShipStatus>();
        private static readonly Dictionary<string, ShipEnemyDetail> _shipEnemies = new Dictionary<string, ShipEnemyDetail>();

        public static Dictionary<Feat, ShipModuleFeat> ShipModuleFeats { get; } = ShipModuleFeat.GetAll();

        /// <summary>
        /// When the module loads, cache all space data into memory.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void CacheData()
        {
            LoadShips();
            LoadShipModules();
            LoadShipEnemies();

            Console.WriteLine($"Loaded {_ships.Count} ships.");
            Console.WriteLine($"Loaded {_shipModules.Count} ship modules.");
            Console.WriteLine($"Loaded {_shipEnemies.Count} ship enemies.");
        }

        /// <summary>
        /// Loads all of the implementations of IShipListDefinition into the cache.
        /// </summary>
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

        /// <summary>
        /// Loads all of the implementations of IShipModuleListDefinition into the cache.
        /// </summary>
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
        /// Loads all of the implementations of IShipEnemyListDefinition into the cache.
        /// </summary>
        private static void LoadShipEnemies()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IShipEnemyListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IShipEnemyListDefinition)Activator.CreateInstance(type);
                var ships = instance.BuildShipEnemies();

                foreach (var (creatureTag, enemy) in ships)
                {
                    _shipEnemies.Add(creatureTag, enemy);
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
        private static ShipStatus GetCurrentTarget(uint player)
        {
            var target = GetLocalObject(player, "SPACE_TARGET");
            return GetShipStatus(target);
        }

        /// <summary>
        /// Clears a player's current target.
        /// </summary>
        /// <param name="player">The player whose target will be cleared.</param>
        private static void ClearCurrentTarget(uint player)
        {
            // Remove the VFX from the current target if it exists.
            var currentTarget = GetCurrentTarget(player);
            if (GetIsObjectValid(currentTarget.Creature))
            {
                Player.ApplyLoopingVisualEffectToObject(player, currentTarget.Creature, VisualEffect.None);
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

            var target = StringToObject(Events.GetEventData("TARGET"));
            var currentTarget = GetCurrentTarget(player);
            
            // Targeted the same object - remove it.
            if (currentTarget.Creature == target)
            {
                ClearCurrentTarget(player);
            }
            // Targeted something new. Remove existing target and pick the new one.
            else
            {
                ClearCurrentTarget(player);
                SetCurrentTarget(player, target);
            }
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

            foreach(var (_, shipModule) in allModules)
            {
                var shipModuleDetail = _shipModules[shipModule.ItemTag];

                // Passive modules shouldn't be converted to feats.
                if (shipModuleDetail.IsPassive) continue;

                // Convert current ship module to feat.
                Creature.AddFeat(player, shipModule.AssignedShipModuleFeat);

                // Rename the feat to match the configured name on the ship module.
                var shipModuleFeat = ShipModuleFeats[shipModule.AssignedShipModuleFeat];
                Player.SetTlkOverride(player, shipModuleFeat.NameTlkId, shipModuleDetail.Name);
                Player.SetTlkOverride(player, shipModuleFeat.DescriptionTlkId, shipModuleDetail.Description);
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
            foreach (var (feat, _) in ShipModuleFeats)
            {
                Creature.RemoveFeat(player, feat);
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
        
        /// <summary>
        /// When a ship module's feat is used, execute the currently equipped module's custom code.
        /// </summary>
        [NWNEventHandler("feat_use_bef")]
        public static void HandleShipModuleFeats()
        {
            var feat = (Feat)Convert.ToInt32(Events.GetEventData("FEAT_ID"));

            if (!ShipModuleFeats.ContainsKey(feat)) return;
            
            var activator = OBJECT_SELF;
            var activatorShipStatus = GetShipStatus(activator);
            
            // Check high powered modules
            var shipModule = activatorShipStatus
                .HighPowerModules
                .SingleOrDefault(x => x.Key == feat);

            // Not found in high powered modules, check low now.
            if (shipModule.Value == null)
            {
                shipModule = activatorShipStatus
                    .LowPowerModules
                    .SingleOrDefault(x => x.Key == feat);
            }

            // Neither high nor low had this feat.Log an error.
            if (shipModule.Value == null)
            {
                Log.Write(LogGroup.Error, $"Failed to locate matching ship module by its feat for player {GetName(activator)}");
                SendMessageToPC(activator, "Unable to use that module.");
                return;
            }

            // Found the ship module. Run validation checks.
            var shipModuleDetails = _shipModules[shipModule.Value.ItemTag];

            // Check capacitor requirements
            var requiredCapacitor = shipModuleDetails.CalculateCapacitorAction?.Invoke(activatorShipStatus);
            if (requiredCapacitor != null && activatorShipStatus.Capacitor < requiredCapacitor)
            {
                SendMessageToPC(activator, $"Your ship does not have enough capacitor to use that module. (Required: {requiredCapacitor})");
                return;
            }

            // Check recast requirements
            var now = DateTime.UtcNow;
            if (shipModule.Value.RecastTime > now)
            {
                SendMessageToPC(activator, "That module is not ready.");
                return;
            }

            // Check for a valid target.
            var target = GetCurrentTarget(activator);
            if (!GetIsObjectValid(target.Creature))
            {
                SendMessageToPC(activator, "Target not selected.");
                return;
            }

            // Validation succeeded, run the module-specific code now.
            shipModuleDetails.ModuleActivatedAction?.Invoke(activatorShipStatus, target);
            
            // Update the recast timer.
            if (shipModuleDetails.CalculateRecastAction != null)
            {
                var recastSeconds = shipModuleDetails.CalculateRecastAction(activatorShipStatus);
                var recastTimer = now.AddSeconds(recastSeconds);
                shipModule.Value.RecastTime = recastTimer;
            }

            // Reduce capacitor
            if (requiredCapacitor != null)
            {
                activatorShipStatus.Capacitor -= (int) requiredCapacitor;
            }

            // Update changes for players. No need to update NPCs as they are already stored in memory cache.
            if (GetIsPC(activator))
            {
                ApplyShipStatusPropertiesToPlayerEntity(activatorShipStatus, activator);
            }
        }

        /// <summary>
        /// When the player's heartbeat fires, recover capacitor and shield.
        /// </summary>
        [NWNEventHandler("interval_pc_1s")]
        public static void PlayerShipRecovery()
        {
            var player = OBJECT_SELF;

            // Not in space mode, exit early.
            if (!IsPlayerInSpaceMode(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);
            var playerShip = dbPlayer.Ships[dbPlayer.ActiveShipId];
            var shipDetail = _ships[playerShip.ItemTag];

            // Shield recovery
            playerShip.ShieldCycle++;
            var rechargeRate = shipDetail.ShieldRechargeRate - playerShip.ShieldCycleBonus;
            if (rechargeRate <= 0)
                rechargeRate = 1;

            if (playerShip.ShieldCycle >= rechargeRate)
            {
                playerShip.Shield++; // todo: shield recovery calculation

                // Clamp shield to max.
                if (playerShip.Shield > shipDetail.MaxShield + playerShip.MaxShieldBonus)
                    playerShip.Shield = shipDetail.MaxShield + playerShip.MaxShieldBonus;

                playerShip.ShieldCycle = 0;
            }

            // Capacitor recovery
            playerShip.Capacitor++; // todo: capacitor recovery calculation

            // Clamp capacitor to max.
            if (playerShip.Capacitor > shipDetail.MaxCapacitor + playerShip.MaxCapacitorBonus)
                playerShip.Capacitor = shipDetail.MaxCapacitor + playerShip.MaxCapacitorBonus;

            // Update changes
            DB.Set(playerId, dbPlayer);
        }

        /// <summary>
        /// When a creature spawns, track it in the cache.
        /// </summary>
        [NWNEventHandler("crea_spawn")]
        public static void CreatureSpawn()
        {
            var creature = OBJECT_SELF;
            var creatureTag = GetTag(creature);

            // Not registered with the space system. Exit early.
            if (!_shipEnemies.ContainsKey(creatureTag)) return;

            var registeredEnemyType = _shipEnemies[creatureTag];

            var shipStatus = new ShipStatus
            {
                Creature = creature,
                Capacitor = registeredEnemyType.Capacitor, 
                Hull = registeredEnemyType.Hull, 
                Shield = registeredEnemyType.Shield,
                
                Evasion = registeredEnemyType.Evasion,
                Accuracy = registeredEnemyType.Accuracy,

                EMDefense = registeredEnemyType.EMDefense,
                ExplosiveDefense = registeredEnemyType.ExplosiveDefense,
                ThermalDefense = registeredEnemyType.ThermalDefense
            };

            // Attach the modules to the ship status.
            var featCount = 0;
            foreach (var itemTag in registeredEnemyType.HighPoweredModules)
            {
                var feat = ShipModuleFeats.ElementAt(featCount).Key;
                shipStatus.HighPowerModules.Add(feat, new ShipStatus.ShipStatusModule
                {
                    ItemTag = itemTag,
                    RecastTime = DateTime.UtcNow
                });

                featCount++;
            }
            foreach (var itemTag in registeredEnemyType.LowPowerModules)
            {
                var feat = ShipModuleFeats.ElementAt(featCount).Key;
                shipStatus.LowPowerModules.Add(feat, new ShipStatus.ShipStatusModule
                {
                    ItemTag = itemTag,
                    RecastTime = DateTime.UtcNow
                });

                featCount++;
            }

            _shipNPCs[creature] = shipStatus;
        }

        /// <summary>
        /// When a creature dies, remove it from the cache.
        /// </summary>
        [NWNEventHandler("crea_death")]
        public static void CreatureDeath()
        {
            var creature = OBJECT_SELF;

            if (_shipNPCs.ContainsKey(creature))
            {
                _shipNPCs.Remove(creature);
            }
        }

        private static void ApplyShipStatusPropertiesToPlayerEntity(ShipStatus shipStatus, uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);
            var dbPlayerShip = dbPlayer.Ships[dbPlayer.ActiveShipId];

            dbPlayerShip.Shield = shipStatus.Shield;
            dbPlayerShip.Hull = shipStatus.Hull;
            dbPlayerShip.Capacitor = shipStatus.Capacitor;

            foreach (var (_, shipModule) in shipStatus.HighPowerModules)
            {
                // todo: map the recast changes back to entity
            }
            foreach (var (_, shipModule) in shipStatus.LowPowerModules)
            {
                // todo: map the recast changes back to entity
            }

            DB.Set(playerId, dbPlayer);
        }

        /// <summary>
        /// Retrieves the ship status of a given creature.
        /// If creature is an NPC, it will be retrieved from the cache.
        /// If creature is a PC, it will be retrieved from the database.
        /// </summary>
        /// <param name="creature">The creature to get the status of</param>
        /// <returns>A ship status containing current statistics about a creature's ship.</returns>
        private static ShipStatus GetShipStatus(uint creature)
        {
            var shipStatus = new ShipStatus();

            if (!GetIsObjectValid(creature))
            {
                return new ShipStatus();
            }

            // Player ship statuses must be mapped from their database records.
            if (GetIsPC(creature))
            {
                var targetPlayerId = GetObjectUUID(creature);
                var dbTargetPlayer = DB.Get<Entity.Player>(targetPlayerId);
                var dbPlayerShip = dbTargetPlayer.Ships[dbTargetPlayer.ActiveShipId];
                var shipDetail = _ships[dbPlayerShip.ItemTag];

                shipStatus.Creature = creature;
                shipStatus.Shield = dbPlayerShip.Shield;
                shipStatus.Hull = dbPlayerShip.Hull;
                shipStatus.Capacitor = dbPlayerShip.Capacitor;
                
                shipStatus.Accuracy = shipDetail.Accuracy + dbPlayerShip.AccuracyBonus;
                shipStatus.Evasion = shipDetail.Evasion + dbPlayerShip.EvasionBonus;

                shipStatus.ThermalDefense = shipDetail.ThermalDefense + dbPlayerShip.ThermalDefenseBonus;
                shipStatus.ExplosiveDefense = shipDetail.ExplosiveDefense + dbPlayerShip.ExplosiveDefenseBonus;
                shipStatus.EMDefense = shipDetail.EMDefense + dbPlayerShip.EMDefenseBonus;

                foreach (var (_, shipModule) in dbPlayerShip.HighPowerModules)
                {
                    shipStatus.HighPowerModules.Add(shipModule.AssignedShipModuleFeat, new ShipStatus.ShipStatusModule
                    {
                        ItemTag = shipModule.ItemTag,
                        RecastTime = shipModule.RecastTime
                    });
                }

                foreach (var (_, shipModule) in dbPlayerShip.LowPowerModules)
                {
                    shipStatus.LowPowerModules.Add(shipModule.AssignedShipModuleFeat, new ShipStatus.ShipStatusModule
                    {
                        ItemTag = shipModule.ItemTag,
                        RecastTime = shipModule.RecastTime
                    });
                }
            }
            // NPC ship statuses are stored directly in cache so we can return them immediately.
            else
            {
                shipStatus = _shipNPCs.ContainsKey(creature) 
                    ? _shipNPCs[creature] 
                    : new ShipStatus();
            }

            return shipStatus;
        }

        /// <summary>
        /// Calculates attacker's chance to hit target.
        /// </summary>
        /// <param name="attacker">The creature attacking.</param>
        /// <param name="target">The creature being targeted.</param>
        public static int CalculateChanceToHit(uint attacker, uint target)
        {
            var attackerShipStatus = GetShipStatus(attacker);
            var targetShipStatus = GetShipStatus(target);
            
            var delta = attackerShipStatus.Accuracy - targetShipStatus.Evasion;
            var chanceToHit = 75 + delta * 0.5f;
            return (int)chanceToHit;
        }
        
        /// <summary>
        /// Applies damage to a ship target. Damage will first be taken to the shields.
        /// When shields reaches zero, damage will be taken on the hull.
        /// When hull reaches zero, the ship will explode.
        /// </summary>
        /// <param name="attacker">The attacking ship</param>
        /// <param name="target">The defending, targeted ship</param>
        /// <param name="amount">The amount of damage to apply to the target.</param>
        public static void ApplyShipDamage(uint attacker, uint target, int amount)
        {
            if (amount < 0) return;

            var targetShipStatus = GetShipStatus(target);
            var remainingDamage = amount;
            // First deal damage to target's shields.
            if (remainingDamage <= targetShipStatus.Shield)
            {
                // Shields have enough to cover the attack.
                targetShipStatus.Shield -= remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                remainingDamage -= targetShipStatus.Shield;
                targetShipStatus.Shield = 0;
            }


            // If damage is remaining, deal it to the hull.
            if (remainingDamage > 0)
            {
                targetShipStatus.Hull -= remainingDamage;
            }

            // Safety clamping
            if (targetShipStatus.Shield < 0)
                targetShipStatus.Shield = 0;
            if (targetShipStatus.Hull < 0)
                targetShipStatus.Hull = 0;

            // Apply death if shield and hull have reached zero.
            if (targetShipStatus.Shield <= 0 && targetShipStatus.Hull <= 0)
            {
                ApplyDeath(target);
            }
            else
            {
                if (GetIsPC(target))
                {
                    var targetPlayerId = GetObjectUUID(target);
                    var dbTargetPlayer = DB.Get<Entity.Player>(targetPlayerId);
                    var dbPlayerShip = dbTargetPlayer.Ships[dbTargetPlayer.ActiveShipId];

                    dbPlayerShip.Shield = targetShipStatus.Shield;
                    dbPlayerShip.Hull = targetShipStatus.Hull;

                    DB.Set(targetPlayerId, dbTargetPlayer);
                }
                else
                {
                    // Update NPC cache.
                    _shipNPCs[target] = targetShipStatus;
                }
            }

            // Notify nearby players of damage taken by target.
            Messaging.SendMessageNearbyToPlayers(attacker, $"{GetName(attacker)} deals {amount} damage to {GetName(target)}.");
        }

        /// <summary>
        /// Applies death to a creature.
        /// If this is a PC, their ship will be destroyed.
        /// If this is an NPC, they will be killed and explode in spectacular fashion.
        /// </summary>
        /// <param name="creature">The creature who will be killed.</param>
        private static void ApplyDeath(uint creature)
        {
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), creature);

            // When a player dies, they have a chance to drop every module installed on their ship.
            // The ship also gets permanently destroyed and removed from their database record.
            // Finally, they return to their stored respawn point.
            if (GetIsPC(creature))
            {
                var deathLocation = GetLocation(creature);
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Entity.Player>(playerId);
                var dbPlayerShip = dbPlayer.Ships[dbPlayer.ActiveShipId];
                const int ChanceToDropModule = 65;

                // Give a chance to drop each installed module.
                foreach (var (_, shipModule) in dbPlayerShip.HighPowerModules)
                {
                    if (Random.D100(1) <= ChanceToDropModule)
                    {
                        var deserialized = Object.Deserialize(shipModule.SerializedItem);
                        CopyObject(deserialized, deathLocation);
                        DestroyObject(deserialized);
                    }
                }

                foreach (var (_, shipModule) in dbPlayerShip.LowPowerModules)
                {
                    if (Random.D100(1) <= ChanceToDropModule)
                    {
                        var deserialized = Object.Deserialize(shipModule.SerializedItem);
                        CopyObject(deserialized, deathLocation);
                        DestroyObject(deserialized);
                    }
                }

                // Exit space mode
                ExitSpaceMode(creature);

                // Jump player to their respawn point.
                var respawnArea = Cache.GetAreaByResref(dbPlayer.RespawnAreaResref);
                var respawnLocation = Location(
                    respawnArea,
                    Vector3(
                        dbPlayer.RespawnLocationX, 
                        dbPlayer.RespawnLocationY, 
                        dbPlayer.RespawnLocationZ),
                    dbPlayer.RespawnLocationOrientation);

                AssignCommand(creature, () =>
                {
                    ActionJumpToLocation(respawnLocation);
                });

                // Remove the destroyed ship from the player's data.
                dbPlayer.Ships.Remove(dbPlayer.ActiveShipId);
                dbPlayer.ActiveShipId = Guid.Empty;
                dbPlayer.SelectedShipId = Guid.Empty;

                // Update the changes
                DB.Set(playerId, dbPlayer);
            }
            // Simply kill NPCs
            else
            {
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), creature);
            }
        }
    }
}
