namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>Whether a held Edict is working or still waiting on another city. Values are saved with cities.</summary>
    public enum EdictState
    {
        Invalid = 0,

        /// <summary>An Alliance waiting for the other mayor to enact one naming this city. It takes a slot and costs nothing.</summary>
        Pending = 1,

        /// <summary>Enacted. It works unless a pause covers it.</summary>
        Active = 2
    }
}
