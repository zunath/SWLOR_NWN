

namespace SWLOR.Game.Server.Core.NWNX
{
    public enum DetectionMethod
    {
        Heard,
        Seen
    }

    public static class Reveal
    {
        private const string PLUGIN_NAME = "NWNX_Reveal";

        // Selectively reveals the character to an observer until the next time they stealth out of sight.
        // hiding The creature who is stealthed.
        // observer The creature to whom the hider is revealed.
        // detectionMethod Can be specified to determine whether the hidden creature is seen or heard.
        public static void RevealTo(uint hiding, uint observer,
            DetectionMethod detection = DetectionMethod.Heard)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RevealTo");
            Internal.NativeFunctions.nwnxPushInt((int)detection);
            Internal.NativeFunctions.nwnxPushObject(observer);
            Internal.NativeFunctions.nwnxPushObject(hiding);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets whether a character remains visible to their party through stealth.
        // hiding: The creature who is stealthed.
        // reveal: TRUE for visible.
        // detection: Can be specified to determine whether the hidden creature is seen or heard.
        public static void RevealToParty(uint hiding, int reveal, DetectionMethod detection = DetectionMethod.Heard)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRevealToParty");
            Internal.NativeFunctions.nwnxPushInt((int)detection);
            Internal.NativeFunctions.nwnxPushInt(reveal);
            Internal.NativeFunctions.nwnxPushObject(hiding);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}