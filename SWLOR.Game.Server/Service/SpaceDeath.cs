using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// A destroyed player ship (plan section 9, Death). Everyone aboard is ejected alive in escape pods to the dock
    /// the ship is towed to and takes the normal ground XP debt. Modules drop only when another player destroys the
    /// ship in Wild Space; everywhere else they stay installed and the owner pays the repair bill. The cargo always
    /// spills into cargo pods anyone can scoop.
    /// </summary>
    public static partial class Space
    {
        /// <summary>
        /// Area local int. Set to 1 on every Wild Space area (plan section 23, the six systems in
        /// <see cref="WildSpaceTuning.Systems"/>). A player ship that another player destroys there can drop its modules.
        /// </summary>
        public const string WildSpaceVariable = "WILD_SPACE";

        /// <summary>Whether an area is a Wild Space system.</summary>
        public static bool IsWildSpace(uint area)
        {
            return GetIsObjectValid(area) && GetLocalBool(area, WildSpaceVariable);
        }

        /// <summary>
        /// Applies death to a creature. For a pilot in space mode this destroys the ship:
        ///     - Everyone aboard (the pilot, station crew, viewport watchers and interior passengers) is ejected
        ///       alive in escape pods to the dock the ship is towed to and takes the normal XP debt.
        ///     - The modules drop (65% each, the rest are lost) only when another player destroyed the ship in Wild
        ///       Space. Otherwise they stay installed.
        ///     - The cargo spills into cargo pods at the wreck on every death, PvE or PvP.
        ///     - The ship is left at 1 hull and no shield and returns to the dock; the owner pays the repair bill.
        /// NPCs aboard are unaffected. Called by Death.OnPlayerDeath for every player death.
        /// </summary>
        /// <param name="creature">The creature that died.</param>
        /// <returns>true when the creature was a pilot ejected alive, so the ground death flow must not run.</returns>
        public static bool ApplyDeath(uint creature)
        {
            _pendingShipLaunches.Remove(creature);

            // A crew member killed some other way (a DM, a script) leaves their station.
            if (IsAtCrewStation(creature))
                LeaveCrewStation(creature, returnToConsole: false);

            if (!IsPlayerInSpaceMode(creature))
                return false;

            // Ships docked aboard detach where this one died and take back the players who belong to them. Those
            // players are not part of the escape pods: no XP debt and no jump to the dock.
            var returnedToDockedShips = DetachDockedShips(creature);
            _shipCombatAt.Remove(creature);

            var playerId = GetObjectUUID(creature);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbPlayerShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
            var dbProperty = DB.Get<WorldProperty>(dbPlayerShip.PropertyId);
            var deathLocation = GetLocation(creature);

            var killer = TakeShipKiller(creature);
            var killerIsPlayer = PvpCombat.IsPlayerAgainstPlayer(killer, creature);
            var modulesDrop = SpaceDeathRules.ModulesDrop(IsWildSpace(GetArea(creature)), killerIsPlayer);

            var hasDestination = TryResolveEscapePodDestination(dbProperty, out var podLocation, out var podType);

            // Everyone at the stations and viewports of the ship leaves the station where the ship died.
            var stationCrew = _crewAssignments
                .Where(x => x.Value.Pilot == creature && !returnedToDockedShips.Contains(x.Key))
                .Select(x => x.Key)
                .ToList();
            foreach (var crewMember in stationCrew)
            {
                LeaveCrewStation(crewMember, returnToConsole: false);
            }

            // Droids stop working and go back to their owners.
            UnloadDroidCrew(creature);
            UnregisterShipPilot(creature);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), creature);

            // The flight ends with the ship, as it does when the pilot leaves space: auras, orders and statuses go.
            SpaceAuras.RemoveAuras(creature);
            SpaceHostileAbilityRewards.Clear(creature);
            SpaceSustainedFire.Clear(creature);
            SpaceOrdnance.Clear(creature);
            SpaceBulwark.Clear(creature);
            SpaceCommand.Clear(creature);
            StatusEffect.ClearStatusEffectsOnDeath(creature);
            dbPlayerShip.Status.PendingRiders?.Clear();
            Trade.SpillCargo(dbPlayerShip, deathLocation);

            if (modulesDrop)
            {
                DropShipModules(dbPlayerShip.Status, deathLocation);

                // The owner always loses every module on this death, whether or not it dropped.
                dbPlayerShip.Status.HighPowerModules.Clear();
                dbPlayerShip.Status.LowPowerModules.Clear();
                dbPlayerShip.Status.MidPowerModules.Clear();
                dbPlayerShip.PlayerHotBars.Clear();
            }
            else
            {
                // The modules stay installed; keep the hot bar of the ship for the next flight.
                dbPlayerShip.PlayerHotBars[playerId] = CreaturePlugin.SerializeQuickbar(creature);
            }

            ReturnDroidCrew(dbPlayerShip, "was destroyed");
            dbPlayerShip.Status.PowerRouting = ShipPowerRouting.Balanced;
            dbPlayerShip.Status.PowerRoutingBonusPercent = 0;

            ClearShipToggleState(creature, dbPlayerShip.Status);
            dbPlayerShip.Status.Hull = 1;
            dbPlayerShip.Status.Shield = 0;

            // A revamp ship falls back to its bare hull stats when it has no modules.
            RecalculateShipStats(dbPlayerShip.Status, GetAbilityScore(creature, AbilityType.Willpower));

            // Exit space mode
            ClearCurrentTarget(creature);
            RestoreCharacterAppearance(creature, dbPlayer);
            DeleteLocalFloat(creature, Stat.ShipMovementSpeedVariable);
            Stat.ApplyCreatureMovementRate(creature);
            Enmity.RemoveCreatureEnmity(creature);

            // Remove all module feats from the player.
            foreach (var (feat, _) in ShipModuleFeats)
            {
                CreaturePlugin.RemoveFeat(creature, feat);
            }

            // Load the player hot bar.
            if (string.IsNullOrWhiteSpace(dbPlayer.SerializedHotBar) ||
                !CreaturePlugin.DeserializeQuickbar(creature, dbPlayer.SerializedHotBar))
            {
                // Deserialization failed. Clear out the hot bar and start fresh.
                for (var slot = 0; slot <= 35; slot++)
                {
                    PlayerPlugin.SetQuickBarSlot(creature, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }

                dbPlayer.SerializedHotBar = CreaturePlugin.SerializeQuickbar(creature);
            }

            _shipClones.Remove(dbPlayer.ActiveShipId);
            ClearShipModuleRecasts(creature, dbPlayer);
            dbPlayer.ActiveShipId = System.Guid.Empty.ToString();

            // Removing the current position of the ship will automatically send it back to the last dock it was at.
            if (dbProperty.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
            {
                dbProperty.Positions.Remove(PropertyLocationType.CurrentPosition);
            }

            // Update the changes
            DB.Set(dbProperty);
            DB.Set(dbPlayerShip);
            DB.Set(dbPlayer);

            // The pilot is no longer in space mode; stop tracking them and refresh space-dependent UI.
            _playersInSpace.Remove(creature);
            ExecuteScript("space_exit", creature);

            // Everyone inside the ship instance rides the pods too, found by area because the instance player list
            // is not reliable.
            var passengers = new List<uint>();
            if (Property.TryGetLoadedInstance(dbPlayerShip.PropertyId, out var instance))
            {
                passengers.AddRange(GetPlayersInArea(instance.Area)
                    .Where(x => GetIsObjectValid(x) && !returnedToDockedShips.Contains(x)));
            }

            DestroyPilotClone(creature);

            var manifest = EscapePodRules.BuildManifest(creature, stationCrew.Where(GetIsObjectValid), passengers);
            if (!hasDestination)
            {
                // No dock can be found. The pilot takes the ground death flow, as before pods existed.
                Log.Write(LogGroup.Space, $"Ship {dbPlayerShip.Id} was destroyed with no dock to eject to. Its passengers were killed.");
                foreach (var rider in manifest.Where(x => x != creature))
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), rider);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), rider);
                    FloatingTextStringOnCreature(ColorToken.Red("The ship has exploded!"), rider, false);
                }

                return false;
            }

            EjectEscapePods(manifest, creature, podLocation, podType);
            return true;
        }

        /// <summary>
        /// Rolls each installed module for a drop and leaves the ones that drop at the wreck, where the destroyer
        /// (or anyone else) can loot them.
        /// </summary>
        private static void DropShipModules(ShipStatus status, Location deathLocation)
        {
            var modules = status.HighPowerModules.Values
                .Concat(status.LowPowerModules.Values)
                .Concat(status.MidPowerModules.Values)
                .ToList();

            foreach (var shipModule in modules)
            {
                if (SpaceDeathRules.ModuleDrops(Random.D100(1)))
                {
                    var deserialized = ObjectPlugin.Deserialize(shipModule.SerializedItem);
                    CopyObject(deserialized, deathLocation);
                    DestroyObject(deserialized);
                }

                var moduleDetails = GetShipModuleDetailByItemTag(shipModule.ItemTag);
                moduleDetails.ModuleUnequippedAction?.Invoke(status, shipModule.ModuleBonus);
            }
        }

        /// <summary>
        /// Where the pods land: the dock the ship is towed to, or its last NPC dock when that dock is a player
        /// starport that is not loaded.
        /// </summary>
        private static bool TryResolveEscapePodDestination(WorldProperty property, out Location location, out EscapePodDestinationType type)
        {
            location = default;
            property.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dock);
            property.Positions.TryGetValue(PropertyLocationType.LastNPCDockPosition, out var safeDock);

            var dockReachable = TryGetDockLocation(dock, out var dockLocation);
            var safeReachable = TryGetDockLocation(safeDock, out var safeLocation);
            type = EscapePodRules.ChooseDestination(dockReachable, safeReachable);

            switch (type)
            {
                case EscapePodDestinationType.Dock:
                    location = dockLocation;
                    return true;
                case EscapePodDestinationType.SafeDock:
                    location = safeLocation;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetDockLocation(PropertyLocation dock, out Location location)
        {
            location = default;
            if (dock == null)
                return false;

            uint area;
            if (string.IsNullOrWhiteSpace(dock.AreaResref))
            {
                if (string.IsNullOrWhiteSpace(dock.InstancePropertyId) ||
                    !Property.TryGetLoadedInstance(dock.InstancePropertyId, out var instance))
                {
                    return false;
                }

                area = instance.Area;
            }
            else
            {
                area = Area.GetAreaByResref(dock.AreaResref);
            }

            if (!GetIsObjectValid(area))
                return false;

            location = Location(area, Vector3(dock.X, dock.Y, dock.Z), dock.Orientation);
            return true;
        }

        /// <summary>
        /// Puts everyone on the manifest alive at the dock and charges the ground XP debt. The pilot died with the
        /// ship, so they are revived first.
        /// </summary>
        private static void EjectEscapePods(List<uint> manifest, uint pilot, Location dock, EscapePodDestinationType type)
        {
            var dockName = GetName(GetAreaFromLocation(dock));
            var message = type == EscapePodDestinationType.Dock
                ? $"Your ship has been destroyed. Your escape pod lands at {dockName}."
                : $"Your ship has been destroyed. Its dock cannot be reached, so your escape pod lands at {dockName}.";

            foreach (var rider in manifest)
            {
                if (!GetIsObjectValid(rider))
                    continue;

                if (rider == pilot)
                {
                    ApplyEffectToObject(DurationType.Instant, EffectResurrection(), rider);
                    PlayerTemporaryEffects.RestoreGhostAfterRevive(rider);
                    using (CombatRecorder.BeginRespawnAttribution())
                        ApplyEffectToObject(DurationType.Instant, EffectHeal(EscapePodRules.GetEjectionHitPoints(GetMaxHitPoints(rider))), rider);
                }

                else
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), rider);
                }

                var rescued = rider;
                AssignCommand(rescued, () =>
                {
                    ClearAllActions(true);
                    JumpToLocation(dock);
                });

                SendMessageToPC(rider, ColorToken.Red(message));

                // DMs ride along but carry no XP debt.
                if (!GetIsDM(rider) && !GetIsDMPossessed(rider))
                {
                    var debt = Death.ApplyPenalties(rider);
                    Log.Write(LogGroup.Death, $"SHIP DESTROYED - {PlayerName.GetAuditName(rider)} - escape pod to {dockName} - XP debt {debt}");
                }
            }
        }
    }
}
