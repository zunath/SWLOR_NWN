namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>A donor and how many items they gave a project, for the Gazette and the project window.</summary>
    public sealed record CityProjectDonor(string PlayerId, string Label, int Items);
}
