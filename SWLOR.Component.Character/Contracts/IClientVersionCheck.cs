namespace SWLOR.Component.Character.Contracts
{
    public interface IClientVersionCheck
    {
        /// <summary>
        /// When a player connects to the server, perform a version check on their client.
        /// All of the NUI window features require version 8193.33 or higher but we restrict to 8193.34 or higher
        /// due to fixes applied in .34.
        /// </summary>
        void CheckVersion();
    }
}