using NWN.Native.API;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Native;

/// <summary>
/// Reads an object's applied effects straight from the engine. GetFirstEffect and GetNextEffect copy every
/// effect into script and list only part of this array: internal effects and effects pending removal are
/// skipped. A miss here therefore means script iteration cannot find the effect either, while a match must
/// still be confirmed through script.
/// </summary>
public static class AppliedEffects
{
    /// <summary>
    /// Returns false only when no applied effect has the script effect type, as GetEffectType reports it.
    /// </summary>
    public static bool MayHaveScriptType(uint obj, EffectTypeScript type, EffectTypeScript otherType)
    {
        var gameObject = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(obj)?.AsNWSObject();
        if (gameObject == null)
            return true;

        foreach (var effect in gameObject.m_appliedEffects)
        {
            var scriptType = (EffectTypeScript)effect.GetScriptEffectType(0);
            if (scriptType == type || scriptType == otherType)
                return true;
        }

        return false;
    }

    public static bool MayHaveScriptType(uint obj, EffectTypeScript type) => MayHaveScriptType(obj, type, type);

    /// <summary>
    /// Returns false only when no applied effect carries the tag.
    /// </summary>
    public static bool MayHaveTag(uint obj, string tag)
    {
        var gameObject = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(obj)?.AsNWSObject();
        if (gameObject == null)
            return true;

        foreach (var effect in gameObject.m_appliedEffects)
        {
            if (effect.m_sCustomTag.ToString() == tag)
                return true;
        }

        return false;
    }
}
