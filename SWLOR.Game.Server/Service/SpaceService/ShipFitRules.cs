namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Which modules a hull accepts. Legacy and revamp ships and modules never mix.
    /// </summary>
    public static class ShipFitRules
    {
        public const string WrongGenerationMessage = "That module doesn't fit this hull.";
        public const string CapitalOnlyMessage = "Capital class modules may only be installed to capital ships.";
        public const string CapitalShipMessage = "Capital ships can only equip capital class modules.";

        /// <summary>
        /// An empty string when the module fits the hull, otherwise the reason it does not. A revamp hull takes only
        /// revamp modules and a legacy hull only legacy modules. Capital-only modules fit only capital hulls; a legacy
        /// capital hull takes only capital modules, while a revamp capital hull also takes ordinary modules.
        /// </summary>
        public static string GetFitError(ShipDetail ship, ShipModuleDetail module)
        {
            if ((ship.Hull != null) != (module.Step != null))
                return WrongGenerationMessage;

            if (module.CapitalClassModule && !ship.CapitalShip)
                return CapitalOnlyMessage;

            if (ship.Hull == null && ship.CapitalShip && !module.CapitalClassModule)
                return CapitalShipMessage;

            return string.Empty;
        }
    }
}
