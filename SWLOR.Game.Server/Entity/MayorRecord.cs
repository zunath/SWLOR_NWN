namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The sitting mayor's record on the city, shown to citizens in the Citizenship window and the next election
    /// window. Reset when the seat changes hands.
    /// </summary>
    public class MayorRecord
    {
        /// <summary>Edicts the mayor promised on their winning platform.</summary>
        public int Promised { get; set; }

        /// <summary>Edicts from that platform the mayor repealed.</summary>
        public int Broken { get; set; }
    }
}
