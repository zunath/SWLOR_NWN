namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum PropertyPublicType
    {
        /// <summary>
        /// Unspecified defaults to always private.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Adjustable means the setting can change, default is private
        /// </summary>
        Adjustable = 1,

        /// <summary>
        /// Property is always public and cannot change.
        /// </summary>
        AlwaysPublic = 2,

        /// <summary>
        /// Property is always private and cannot change.
        /// </summary>
        AlwaysPrivate = 3,
    }
}
