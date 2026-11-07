namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>How a party works a boardable derelict (plan section 28).</summary>
    public enum DerelictMode
    {
        None = 0,

        /// <summary>A Small or Medium ship docked and its party boarded on foot.</summary>
        Boarding = 1,

        /// <summary>A ship holds beside the derelict while its posted droids strip it on a timer.</summary>
        SalvageTeam = 2,
    }
}
