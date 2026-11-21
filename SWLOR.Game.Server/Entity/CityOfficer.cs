namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// An officer the mayor appointed, kept on the city by player id. The appointment time lets a level drop end the
    /// newest appointments first.
    /// </summary>
    public class CityOfficer
    {
        public CityOfficer()
        {
            Title = string.Empty;
        }

        /// <summary>The title the mayor gave, such as Minister of Trade.</summary>
        public string Title { get; set; }

        public DateTime AppointedUtc { get; set; }
    }
}
