namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// Rolls the dice for a Huttball match, so tests can script every roll.
    /// </summary>
    public interface IHuttballDice
    {
        /// <summary>
        /// Rolls one die with the given number of sides and returns 1 through <paramref name="sides"/>.
        /// </summary>
        int Roll(int sides);
    }
}
