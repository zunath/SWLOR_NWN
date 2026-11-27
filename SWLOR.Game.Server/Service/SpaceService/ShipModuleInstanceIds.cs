namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The instance id that keys a fitted module's fitting, removal and receipt records. Ships fitted before modules saved
    /// one hold modules without it; such a module gets a stable id that is saved before the module leaves the ship.
    /// </summary>
    public static class ShipModuleInstanceIds
    {
        /// <summary>Gives a module without an instance id the one <paramref name="newId"/> makes. True when it assigned one.</summary>
        public static bool AssignMissing(ShipStatus.ShipStatusModule module, Func<string> newId)
        {
            if (module == null || !string.IsNullOrWhiteSpace(module.ItemInstanceId))
                return false;

            module.ItemInstanceId = newId();
            return true;
        }
    }
}
