namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What a login does with a module removal a restart interrupted (<see cref="ShipModuleRemovalRules"/>).</summary>
    public enum ShipModuleRemovalOutcome
    {
        /// <summary>The character file holds the module: the removal is finished.</summary>
        Finished,

        /// <summary>The ship still has the module and the character file does not: the removal never happened.</summary>
        NeverHappened,

        /// <summary>The ship saved the removal but the character file never got the module: it is given again.</summary>
        GiveAgain
    }
}
