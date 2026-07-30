namespace SWLOR.NWN.API.NWNX.Enum
{
    public enum PlayerNameOverrideType
    {
        /// <summary>
        /// Don't rename the community name.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Generate a random string for the community name.
        /// </summary>
        Obfuscate = 1,

        /// <summary>
        /// Use the character name as the community name.
        /// </summary>
        Override = 2,

        /// <summary>
        /// Use the value of the NWNX_RENAME_ANONYMOUS_NAME environment variable.
        /// </summary>
        Anonymous = 3
    }
}
