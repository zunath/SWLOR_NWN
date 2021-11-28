namespace SWLOR.Game.Server.Service.PropertyService
{
    public class PropertyLocation
    {
        public PropertyLocation()
        {
            AreaResref = string.Empty;
        }

        /// <summary>
        /// Gets or sets the X position of this property location.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Gets or sets the Y position of this property location.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Gets or sets the Z position of this property location.
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// Gets or sets the facing/orientation of this property location.
        /// </summary>
        public float Orientation { get; set; }

        /// <summary>
        /// Gets or sets the area resref of this property location.
        /// This will be an empty string if location is within an instance.
        /// </summary>
        public string AreaResref { get; set; }
    }
}
