namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// Rolls Huttball dice with the server's random number generator.
    /// </summary>
    public class HuttballDice : IHuttballDice
    {
        public int Roll(int sides)
        {
            return Random.Next(1, sides + 1);
        }
    }
}
