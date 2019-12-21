using SWLOR.Game.Server.NWScript.Enumerations;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXDialog
    {
        private const string NWNX_Dialog = "NWNX_Dialog";

        /// <summary>
        /// Get the Node Type of the current text node
        /// </summary>
        /// <returns>A Node Type.  If called out of dialog, returns NWNX_DIALOG_NODE_TYPE_INVALID</returns>
        public static DialogNodeType GetCurrentNodeType()
        {
            string sFunc = "GetCurrentNodeType";

            NWNX_CallFunction(NWNX_Dialog, sFunc);
            return (DialogNodeType)NWNX_GetReturnValueInt(NWNX_Dialog, sFunc);
        }

        /// <summary>
        /// Get the Script Type of the current text node
        /// </summary>
        /// <returns>A Node Type. If called out of dialog, returns NWNX_DIALOG_SCRIPT_TYPE_OTHER</returns>
        public static DialogScriptType GetCurrentScriptType()
        {
            string sFunc = "GetCurrentScriptType";

            NWNX_CallFunction(NWNX_Dialog, sFunc);
            return (DialogScriptType)NWNX_GetReturnValueInt(NWNX_Dialog, sFunc);
        }

        /// <summary>
        /// Get the absolute ID of the current node in the conversation
        /// NWNX_DIALOG_NODE_TYPE_ENTRY_NODE and NWNX_DIALOG_NODE_TYPE_REPLY_NODE nodes
        /// have different namespaces, so they can share the same ID
        /// </summary>
        /// <returns>The absolute ID in the dialog. If called out of dialog, returns -1</returns>
        public static int GetCurrentNodeID()
        {
            string sFunc = "GetCurrentNodeID";

            NWNX_CallFunction(NWNX_Dialog, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Dialog, sFunc);
        }

        /// <summary>
        /// Get the index of the current node in the list of replies/entries.
        /// The index is zero based, and counts items not displayed due to a StartingConditional.
        /// </summary>
        /// <returns>The index of the current node.</returns>
        public static int GetCurrentNodeIndex()
        {
            string sFunc = "GetCurrentNodeIndex";

            NWNX_CallFunction(NWNX_Dialog, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Dialog, sFunc);
        }

        /// <summary>
        /// Get the text of the current node
        /// </summary>
        /// <param name="language">The language of the text.</param>
        /// <param name="gender">The gender for the text.</param>
        /// <returns></returns>
        public static string GetCurrentNodeText(DialogLanguage language = DialogLanguage.English, Gender gender = Gender.Male)
        {
            string sFunc = "GetCurrentNodeText";

            NWNX_PushArgumentInt(NWNX_Dialog, sFunc, (int)gender);
            NWNX_PushArgumentInt(NWNX_Dialog, sFunc, (int)language);
            NWNX_CallFunction(NWNX_Dialog, sFunc);
            return NWNX_GetReturnValueString(NWNX_Dialog, sFunc);
        }

        /// <summary>
        /// Set the text of the current node for given language/gender
        /// This will only work in a starting conditional script (action taken comes after the text is displayed)
        /// </summary>
        /// <param name="text">The text for the node.</param>
        /// <param name="language">The language of the text.</param>
        /// <param name="gender">The gender for the text.</param>
        public static void SetCurrentNodeText(string text, DialogLanguage language = DialogLanguage.English, Gender gender = Gender.Male)
        {
            string sFunc = "SetCurrentNodeText";

            NWNX_PushArgumentInt(NWNX_Dialog, sFunc, (int)gender);
            NWNX_PushArgumentInt(NWNX_Dialog, sFunc, (int)language);
            NWNX_PushArgumentString(NWNX_Dialog, sFunc, text);
            NWNX_CallFunction(NWNX_Dialog, sFunc);
        }

    }
}
