using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;
using Player = SWLOR.Game.Server.Entity.Player;
using PlayerShip = SWLOR.Game.Server.Entity.PlayerShip;
using WorldProperty = SWLOR.Game.Server.Entity.WorldProperty;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Who a Comms message reaches: the DMs who read it or whose filter hides it, and whether a party member is in range of the
    /// sender (the same planet, space area, event area or starship). Moved here unchanged from the chat service.
    /// </summary>
    public class CommunicationAudienceService : ICommunicationAudienceService
    {
        private readonly IDBService _db;
        private readonly IDMPresenceService _dmPresence;
        private readonly IDMPreferenceService _dmPreference;
        private readonly IPartyService _party;
        private readonly IPlanetService _planet;
        private readonly IAreaService _area;
        private readonly IPropertyService _property;
        private readonly ISpaceService _space;

        public CommunicationAudienceService(
            IDBService db,
            IDMPresenceService dmPresence,
            IDMPreferenceService dmPreference,
            IPartyService party,
            IPlanetService planet,
            IAreaService area,
            IPropertyService property,
            ISpaceService space)
        {
            _db = db;
            _dmPresence = dmPresence;
            _dmPreference = dmPreference;
            _party = party;
            _planet = planet;
            _area = area;
            _property = property;
            _space = space;
        }

        /// <summary>
        /// Splits the online DMs into those who read a Comms message and those whose Comms filter hides it.
        /// Each DM is identified by the creature they control, since chat sent to a possessing DM's avatar is dropped.
        /// </summary>
        /// <param name="sender">The creature that sent the Comms message.</param>
        /// <param name="onlinePlayers">Every object returned by GetFirstPC/GetNextPC.</param>
        public (List<uint> Readers, HashSet<uint> Muted) GetDMCommsAudience(uint sender, List<uint> onlinePlayers)
        {
            var readers = new List<uint>();
            var muted = new HashSet<uint>();
            // Party membership belongs to each player's own character, which is the avatar for a possessing DM.
            var senderCharacter = _dmPresence.GetDM(sender);

            foreach (var player in onlinePlayers)
            {
                if (!_dmPresence.IsControlledByDM(player))
                    continue;

                var controlled = _dmPresence.GetControlledCreature(player);
                if (_dmPreference.Get(controlled).CommsFilter == DMCommsFilter.AllParties ||
                    _party.IsInParty(senderCharacter, player))
                {
                    readers.Add(controlled);
                }
                else
                {
                    muted.Add(controlled);
                }
            }

            return (readers, muted);
        }

        public bool IsCommsReceiverInRange(uint sender, uint receiver)
        {
            if (!GetIsObjectValid(receiver))
                return false;

            if (GetIsDM(receiver) || GetIsDMPossessed(receiver))
                return true;

            if (IsSameStarshipComms(sender, receiver))
                return true;

            var senderArea = GetArea(sender);
            var receiverArea = GetArea(receiver);
            if (!GetIsObjectValid(senderArea) || !GetIsObjectValid(receiverArea))
                return false;

            if (IsSpaceCommsArea(senderArea))
                return senderArea == receiverArea;

            if (IsEventCommsArea(senderArea))
                return IsEventCommsArea(receiverArea);

            var senderPlanet = ResolveCommsPlanet(senderArea);
            if (senderPlanet != PlanetType.Invalid)
            {
                return ResolveCommsPlanet(receiverArea) == senderPlanet;
            }

            return senderArea == receiverArea;
        }

        private bool IsSpaceCommsArea(uint area)
        {
            return GetLocalBool(area, "SPACE") || GetName(area).StartsWith("Space -");
        }

        private bool IsEventCommsArea(uint area)
        {
            return GetLocalBool(area, ChatVariables.EventCommsAreaVariable);
        }

        private PlanetType ResolveCommsPlanet(uint area)
        {
            var planet = _planet.GetPlanetType(area);
            if (planet != PlanetType.Invalid)
                return planet;

            var propertyId = _property.GetPropertyId(area);
            return ResolveCommsPlanetForPropertyId(propertyId, new HashSet<string>());
        }

        private PlanetType ResolveCommsPlanetForPropertyId(
            string propertyId,
            HashSet<string> visitedPropertyIds)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return PlanetType.Invalid;

            var property = _db.Get<WorldProperty>(propertyId);
            return ResolveCommsPlanetForProperty(property, visitedPropertyIds);
        }

        private PlanetType ResolveCommsPlanetForProperty(
            WorldProperty property,
            HashSet<string> visitedPropertyIds)
        {
            if (property == null || !visitedPropertyIds.Add(property.Id))
                return PlanetType.Invalid;

            if (property.PropertyType == PropertyType.City)
                return ResolveCommsPlanetForAreaResref(property.ParentPropertyId);

            if (property.PropertyType == PropertyType.Starship)
            {
                if (property.Positions.TryGetValue(PropertyLocationType.CurrentPosition, out var currentPosition))
                {
                    var currentPlanet = ResolveCommsPlanetForLocation(currentPosition, visitedPropertyIds);
                    return currentPlanet;
                }

                if (property.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dockPosition))
                {
                    return ResolveCommsPlanetForLocation(dockPosition, visitedPropertyIds);
                }

                return PlanetType.Invalid;
            }

            if (string.IsNullOrWhiteSpace(property.ParentPropertyId))
                return PlanetType.Invalid;

            var parentPlanet = ResolveCommsPlanetForPropertyId(property.ParentPropertyId, visitedPropertyIds);
            return parentPlanet != PlanetType.Invalid
                ? parentPlanet
                : ResolveCommsPlanetForAreaResref(property.ParentPropertyId);
        }

        private PlanetType ResolveCommsPlanetForLocation(
            PropertyLocation location,
            HashSet<string> visitedPropertyIds)
        {
            if (location == null)
                return PlanetType.Invalid;

            if (!string.IsNullOrWhiteSpace(location.InstancePropertyId))
                return ResolveCommsPlanetForPropertyId(location.InstancePropertyId, visitedPropertyIds);

            return ResolveCommsPlanetForAreaResref(location.AreaResref);
        }

        private PlanetType ResolveCommsPlanetForAreaResref(string areaResref)
        {
            if (string.IsNullOrWhiteSpace(areaResref))
                return PlanetType.Invalid;

            var area = _area.GetAreaByResref(areaResref);
            if (GetIsObjectValid(area))
            {
                var planet = _planet.GetPlanetType(area);
                if (planet != PlanetType.Invalid)
                    return planet;
            }

            return _planet.GetPlanetTypeByAreaResref(areaResref);
        }

        private bool IsSameStarshipComms(uint sender, uint receiver)
        {
            var senderShipPropertyId = ResolveStarshipPropertyIdForComms(sender);
            if (string.IsNullOrWhiteSpace(senderShipPropertyId))
                return false;

            return ResolveStarshipPropertyIdForComms(receiver) == senderShipPropertyId;
        }

        private string ResolveStarshipPropertyIdForComms(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return string.Empty;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer != null && !string.IsNullOrWhiteSpace(dbPlayer.ActiveShipId) && IsPlayerInSpaceMode(player))
            {
                var dbShip = _db.Get<PlayerShip>(dbPlayer.ActiveShipId);
                if (dbShip != null)
                    return dbShip.PropertyId;
            }

            var area = GetArea(player);
            var propertyId = _property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(propertyId))
                return string.Empty;

            var property = _db.Get<WorldProperty>(propertyId);
            return property?.PropertyType == PropertyType.Starship
                ? property.Id
                : string.Empty;
        }

        /// <summary>True when the player is flying their ship: <c>Space.IsPlayerInSpaceMode</c>, which reads the static database.</summary>
        protected virtual bool IsPlayerInSpaceMode(uint player)
        {
            return _space.IsPlayerInSpaceMode(player);
        }
    }
}
