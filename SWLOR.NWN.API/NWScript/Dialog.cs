namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Determine whether oObject is in conversation.
        /// </summary>
        public static bool IsInConversation(uint oObject)
        {
            return global::NWN.Core.NWScript.IsInConversation(oObject) != 0;
        }
    }
}