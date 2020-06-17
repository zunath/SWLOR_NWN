namespace SWLOR.Game.Server.NWN
{
    public partial class _
    {
        /// <summary>
        ///   Determine whether oObject is in conversation.
        /// </summary>
        public static bool IsInConversation(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(445);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }
    }
}