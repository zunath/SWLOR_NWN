namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>Materials a cancelled project owes one donor: they go to that donor's overflow store.</summary>
    public sealed record CityProjectRefund(string DonorId, string Resref, int Count);
}
