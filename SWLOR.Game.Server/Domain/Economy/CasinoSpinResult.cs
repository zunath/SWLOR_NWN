namespace SWLOR.Game.Server.Domain.Economy
{
    public sealed record CasinoSpinResult(bool Succeeded, int Payout, string Message);
}
