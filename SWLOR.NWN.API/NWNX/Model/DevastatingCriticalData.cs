namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Devastating critical event data struct.
    /// </summary>
    public struct DevastatingCriticalData
    {
        /// <summary>
        /// The weapon object.
        /// </summary>
        public uint Weapon { get; set; }

        /// <summary>
        /// The target object.
        /// </summary>
        public uint Target { get; set; }

        /// <summary>
        /// The damage amount.
        /// </summary>
        public int Damage { get; set; }
    }
}