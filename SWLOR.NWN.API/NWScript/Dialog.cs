namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Determine whether oObject is in conversation.
        /// </summary>
        public static bool IsInConversation(uint oObject)
        {
            return NWN.Core.NWScript.IsInConversation(oObject) != 0;
        }
    }
}