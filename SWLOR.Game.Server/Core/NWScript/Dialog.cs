namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Determine whether oObject is in conversation.
        /// </summary>
        public static bool IsInConversation(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(445);
            return VM.StackPopInt() != 0;
        }
    }
}