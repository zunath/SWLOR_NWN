namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Settling a module removal from its database record (<see cref="Entity.ShipModuleRemoval"/>). The record is saved
    /// before the ship drops the module and deleted after the character file holds it, so only one copy ever exists:
    /// another refitter can only take a module the ship still has, and then the record is stale.
    /// </summary>
    public static class ShipModuleRemovalRules
    {
        public static ShipModuleRemovalOutcome Settle(bool characterHoldsModule, bool shipStillHasModule)
        {
            if (characterHoldsModule)
                return ShipModuleRemovalOutcome.Finished;

            return shipStillHasModule ? ShipModuleRemovalOutcome.NeverHappened : ShipModuleRemovalOutcome.GiveAgain;
        }
    }
}
