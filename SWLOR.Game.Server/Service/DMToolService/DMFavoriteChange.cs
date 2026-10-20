namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// The result of adding or removing a DM Creator favorite.
    /// </summary>
    public enum DMFavoriteChange
    {
        Added = 0,
        Removed = 1,
        Full = 2,
        NotSaved = 3
    }
}
