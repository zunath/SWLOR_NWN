namespace SWLOR.Game.Server.Service.PropertyService
{
    public class PropertyLocation
    {
        public PropertyLocation()
        {
            AreaResref = string.Empty;
            InstancePropertyId = string.Empty;
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

        /// <summary>
        /// Gets or sets the property Id of the instance.
        /// This will be an empty string if location is within a normal game area.
        /// </summary>
        public string InstancePropertyId { get; set; }

        /// <summary>
        /// Produces a shallow copy of this <see cref="PropertyLocation"/>. Because every field is
        /// either a primitive or an immutable string, a shallow copy is a full structural copy
        /// for all practical purposes.
        ///
        /// This exists specifically so callers that want to assign one Positions entry to another
        /// (e.g. DockPosition = LastNPCDockPosition) can do so without aliasing the two
        /// dictionary entries to the same reference. Aliasing would allow later in-place
        /// mutations of one entry (see _11_RefundMobilityAndUpdateVelesStarport for an example
        /// of such a migration) to silently corrupt the other.
        /// </summary>
        public PropertyLocation Clone()
        {
            return new PropertyLocation
            {
                X = X,
                Y = Y,
                Z = Z,
                Orientation = Orientation,
                AreaResref = AreaResref,
                InstancePropertyId = InstancePropertyId
            };
        }
    }
}
