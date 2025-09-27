using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.World.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Space.Dialog
{
    public class StarportDockDialog: DialogBase
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IPropertyService PropertyService => _serviceProvider.GetRequiredService<IPropertyService>();
        private ISpaceService SpaceService => _serviceProvider.GetRequiredService<ISpaceService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public StarportDockDialog(
            ILogger logger, 
            IDatabaseService db, 
            IDialogService dialogService,
            IDialogBuilder dialogBuilder,
            IServiceProvider serviceProvider) 
            : base(dialogService, dialogBuilder)
        {
            _logger = logger;
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
        }
        
        private class Model
        {
            public PlanetType Planet { get; set; }
            public Location SpaceLocation { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = DialogBuilder
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit);


            return builder.Build();
        }

        private void Initialize()
        {
            var self = OBJECT_SELF;
            var planetType = (PlanetType)GetLocalInt(self, "PLANET_TYPE_ID");
            var spaceWaypointTag = GetLocalString(self, "STARPORT_TELEPORT_WAYPOINT");
            var player = GetPC();

            if (string.IsNullOrWhiteSpace(spaceWaypointTag))
            {
                _logger.Write<ErrorLogGroup>($"{GetName(self)} is missing the local variable 'STARPORT_TELEPORT_WAYPOINT' and cannot be used by players to dock their ships.");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            var spaceWaypoint = GetWaypointByTag(spaceWaypointTag);

            if (!GetIsObjectValid(spaceWaypoint))
            {
                _logger.Write<ErrorLogGroup>($"The waypoint associated with '{GetName(self)}' cannot be found. Did you place it in an area?");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            if (planetType == PlanetType.Invalid)
            {
                _logger.Write<ErrorLogGroup>($"{GetName(self)} is missing the local variable 'PLANET_TYPE_ID' or has an invalid value specified..");
                SendMessageToPC(player, "This docking point is misconfigured. Notify an admin.");
                EndConversation();
                return;
            }

            var model = GetDataModel<Model>();
            model.SpaceLocation = GetLocation(spaceWaypoint);
            model.Planet = planetType;
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var model = GetDataModel<Model>();
            var dockPoints = SpaceService.GetDockPointsByPlanet(model.Planet);

            page.Header = "Please select a location.";

            foreach (var (_, dockPoint) in dockPoints)
            {
                var dockName = dockPoint.IsNPC
                    ? $"[NPC] {dockPoint.Name}"
                    : $"[PC] {GetName(PropertyService.GetRegisteredInstance(dockPoint.PropertyId).Area)}";

                page.AddResponse(dockName, () =>
                {
                    if (EnmityService.HasEnmity(player))
                    {
                        SendMessageToPC(player, ColorToken.Red("You cannot dock while being targeted."));
                        return;
                    }

                    // There's a chance the starport has been picked up since the menu was loaded.
                    // If we can't locate the starport anymore, give an error message to the player.
                    var dbStarport = _db.Get<WorldProperty>(dockPoint.PropertyId);
                    if (!dockPoint.IsNPC)
                    {
                        if (dbStarport == null)
                        {
                            SendMessageToPC(player, ColorToken.Red("This starport is no longer available for docking."));
                            return;
                        }
                    }

                    var spaceArea = GetAreaFromLocation(model.SpaceLocation);
                    var spaceAreaResref = GetResRef(spaceArea);
                    var spacePosition = GetPositionFromLocation(model.SpaceLocation);
                    var spaceOrientation = GetFacingFromLocation(model.SpaceLocation);

                    var landingArea = GetAreaFromLocation(dockPoint.Location);
                    var landingAreaResref = GetResRef(landingArea);
                    var landingPosition = GetPositionFromLocation(dockPoint.Location);
                    var landingOrientation = GetFacingFromLocation(dockPoint.Location);

                    // Clear the ship property's space position and update its last docked position with the new destination.
                    var dbPlayer = _db.Get<Player>(playerId);
                    var dbShip = _db.Get<PlayerShip>(dbPlayer.ActiveShipId);
                    var dbProperty = _db.Get<WorldProperty>(dbShip.PropertyId);
                    dbProperty.Positions.Remove(PropertyLocationType.CurrentPosition);
                    
                    // Docking at an NPC starport will update the safety location to that dock.
                    // In the event that the ship is docked at a player starport and it gets destroyed or
                    // otherwise goes away, the player's ship will return back to the last NPC dock it visited.
                    if (dockPoint.IsNPC)
                    {
                        dbProperty.Positions[PropertyLocationType.LastNPCDockPosition] = new PropertyLocation
                        {
                            AreaResref = landingAreaResref,
                            X = landingPosition.X,
                            Y = landingPosition.Y,
                            Z = landingPosition.Z,
                            Orientation = landingOrientation
                        };
                    }

                    // Unregister from previous player starport, if necessary
                    if (!dbProperty.ChildPropertyIds.ContainsKey(PropertyChildType.RegisteredStarport))
                        dbProperty.ChildPropertyIds[PropertyChildType.RegisteredStarport] = new List<string>();

                    var oldRegistration = dbProperty.ChildPropertyIds[PropertyChildType.RegisteredStarport].FirstOrDefault();
                    if (oldRegistration != null)
                    {
                        var dbOldStarport = _db.Get<WorldProperty>(oldRegistration);
                        if (dbOldStarport != null)
                        {
                            dbOldStarport.ChildPropertyIds[PropertyChildType.Starship].Remove(dbProperty.Id);
                            _db.Set(dbOldStarport);

                            _logger.Write<PropertyLogGroup>($"Unregistered player ship '{dbProperty.CustomName}' ({dbProperty.Id}) from old starport '{dbOldStarport.CustomName}' ({dbOldStarport.Id}).");
                            
                            // Refresh the starport object we're working with in the event the "old" starport
                            // is actually the current one. This ensures we don't get a duplicate starship property Id in the list.
                            if(dbStarport != null && dbOldStarport.Id == dbStarport.Id)
                                dbStarport = _db.Get<WorldProperty>(dockPoint.PropertyId);
                        }

                        dbProperty.ChildPropertyIds[PropertyChildType.RegisteredStarport].Clear();
                    }

                    if (!dockPoint.IsNPC)
                    {
                        // Register this starport to the player ship.
                        dbProperty.ChildPropertyIds[PropertyChildType.RegisteredStarport].Add(dbStarport.Id);

                        // Register this player ship to the starport.
                        if (!dbStarport.ChildPropertyIds.ContainsKey(PropertyChildType.Starship))
                            dbStarport.ChildPropertyIds[PropertyChildType.Starship] = new List<string>();

                        if(!dbStarport.ChildPropertyIds[PropertyChildType.Starship].Contains(dbProperty.Id))
                            dbStarport.ChildPropertyIds[PropertyChildType.Starship].Add(dbProperty.Id);
                        _db.Set(dbStarport);
                    }

                    dbProperty.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
                    {
                        AreaResref = dockPoint.IsNPC ? landingAreaResref : string.Empty,
                        InstancePropertyId = dockPoint.IsNPC ? string.Empty : PropertyService.GetPropertyId(landingArea),
                        X = landingPosition.X,
                        Y = landingPosition.Y,
                        Z = landingPosition.Z,
                        Orientation = landingOrientation
                    };

                    dbProperty.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation
                    {
                        AreaResref = spaceAreaResref,
                        X = spacePosition.X,
                        Y = spacePosition.Y,
                        Z = spacePosition.Z,
                        Orientation = spaceOrientation
                    };

                    _db.Set(dbProperty);

                    SpaceService.WarpPlayerInsideShip(player);
                });
            }
        }
    }
}
