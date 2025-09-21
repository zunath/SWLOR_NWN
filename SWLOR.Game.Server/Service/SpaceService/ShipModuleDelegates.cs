using SWLOR.Shared.Core.Delegates;

namespace SWLOR.Game.Server.Service.SpaceService
{
    // Re-export the delegates from the shared location for backward compatibility
    using ShipModuleEquippedDelegate = SWLOR.Shared.Core.Delegates.ShipModuleEquippedDelegate;
    using ShipModuleUnequippedDelegate = SWLOR.Shared.Core.Delegates.ShipModuleUnequippedDelegate;
    using ShipModuleCalculateRecastDelegate = SWLOR.Shared.Core.Delegates.ShipModuleCalculateRecastDelegate;
    using ShipModuleCalculateCapacitorDelegate = SWLOR.Shared.Core.Delegates.ShipModuleCalculateCapacitorDelegate;
    using ShipModuleActivatedDelegate = SWLOR.Shared.Core.Delegates.ShipModuleActivatedDelegate;
    using ShipModuleValidationDelegate = SWLOR.Shared.Core.Delegates.ShipModuleValidationDelegate;
    using ShipModuleCalculateMaxDistanceDelegate = SWLOR.Shared.Core.Delegates.ShipModuleCalculateMaxDistanceDelegate;
}
