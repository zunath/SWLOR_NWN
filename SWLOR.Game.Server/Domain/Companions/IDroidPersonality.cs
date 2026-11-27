namespace SWLOR.Game.Server.Domain.Companions
{
    public interface IDroidPersonality
    {
        string GreetingPhrase();
        string DeathPhrase();
        string DismissedPhrase();
    }
}
