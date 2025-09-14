using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Display floaty text above the specified creature.
        ///   The text will also appear in the chat buffer of each player that receives the
        ///   floaty text.
        ///   - nStrRefToDisplay: String ref (therefore text is translated)
        ///   - oCreatureToFloatAbove
        ///   - bBroadcastToFaction: If this is TRUE then only creatures in the same faction
        ///   as oCreatureToFloatAbove
        ///   will see the floaty text, and only if they are within range (30 metres).
        /// </summary>
        public static void FloatingTextStrRefOnCreature(int nStrRefToDisplay, uint oCreatureToFloatAbove,
            bool bBroadcastToFaction = true)
        {
            global::NWN.Core.NWScript.FloatingTextStrRefOnCreature(nStrRefToDisplay, oCreatureToFloatAbove, bBroadcastToFaction ? 1 : 0);
        }

        /// <summary>
        ///   Display floaty text above the specified creature.
        ///   The text will also appear in the chat buffer of each player that receives the
        ///   floaty text.
        ///   - sStringToDisplay: String
        ///   - oCreatureToFloatAbove
        ///   - bBroadcastToFaction: If this is TRUE then only creatures in the same faction
        ///   as oCreatureToFloatAbove
        ///   will see the floaty text, and only if they are within range (30 metres).
        /// </summary>
        public static void FloatingTextStringOnCreature(string sStringToDisplay, uint oCreatureToFloatAbove,
            bool bBroadcastToFaction = true)
        {
            global::NWN.Core.NWScript.FloatingTextStringOnCreature(sStringToDisplay, oCreatureToFloatAbove, bBroadcastToFaction ? 1 : 0);
        }

        /// <summary>
        /// Displays sMsg on oPC's screen.
        /// The message is displayed on top of whatever is on the screen, including UI elements
        ///  nX, nY - coordinates of the first character to be displayed. The value is in terms
        ///           of character 'slot' relative to the nAnchor anchor point.
        ///           If the number is negative, it is applied from the bottom/right.
        ///  nAnchor - SCREEN_ANCHOR_* constant
        ///  fLife - Duration in seconds until the string disappears.
        ///  nRGBA, nRGBA2 - Colors of the string in 0xRRGGBBAA format. String starts at nRGBA,
        ///                  but as it nears end of life, it will slowly blend into nRGBA2.
        ///  nID - Optional ID of a string. If not 0, subsequent calls to PostString will
        ///        remove the old string with the same ID, even if it's lifetime has not elapsed.
        ///        Only positive values are allowed.
        ///  sFont - If specified, use this custom font instead of default console font.
        /// </summary>
        /// <param name="PC"></param>
        /// <param name="Msg"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="anchor"></param>
        /// <param name="life"></param>
        /// <param name="RGBA"></param>
        /// <param name="RGBA2"></param>
        /// <param name="ID"></param>
        /// <param name="font"></param>
        public static void PostString(uint PC, string Msg, int X = 0, int Y = 0, ScreenAnchor anchor = ScreenAnchor.TopLeft,
            float life = 10.0f, int RGBA = 2147418367, int RGBA2 = 2147418367, int ID = 0, string font = "")
        {
            global::NWN.Core.NWScript.PostString(PC, Msg, X, Y, (int)anchor, life, RGBA, RGBA2, ID, font);
        }

        /// <summary>
        /// Sets oObject's hilite color to nColor
        /// The nColor format is 0xRRGGBB; -1 clears the color override.
        /// </summary>
        public static void SetObjectHiliteColor(uint oObject, int nColor = -1)
        {
            global::NWN.Core.NWScript.SetObjectHiliteColor(oObject, nColor);
        }

        /// <summary>
        /// Sets the cursor (MOUSECURSOR_*) to use when hovering over oObject
        /// </summary>
        public static void SetObjectMouseCursor(uint oObject, MouseCursor nCursor = MouseCursor.Invalid)
        {
            global::NWN.Core.NWScript.SetObjectMouseCursor(oObject, (int)nCursor);
        }

        /// <summary>
        /// Makes oPC load texture sNewName instead of sOldName.
        /// If oPC is OBJECT_INVALID, it will apply the override to all active players
        /// Setting sNewName to "" will clear the override and revert to original.
        /// void SetTextureOverride();
        /// </summary>
        public static void SetTextureOverride(string OldName, string NewName = "", uint PC = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetTextureOverride(OldName, NewName, PC);
        }

        /// <summary>
        ///   Gets a visual transform on the given object.
        ///   - oObject can be any valid Creature, Placeable, Item or Door.
        ///   - nTransform is one of OBJECT_VISUAL_TRANSFORM_*
        ///   Returns the current (or default) value.
        /// </summary>
        public static float GetObjectVisualTransform(uint oObject, ObjectVisualTransform nTransform)
        {
            return global::NWN.Core.NWScript.GetObjectVisualTransform(oObject, (int)nTransform);
        }

        /// <summary>
        /// Sets a visual transform on the given object.
        /// - oObject can be any valid Creature, Placeable, Item or Door.
        /// - nTransform is one of OBJECT_VISUAL_TRANSFORM_*
        /// - fValue depends on the transformation to apply.
        /// - nScope is one of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_* and specific to the object type being VT'ed.
        /// - nBehaviorFlags: bitmask of OBJECT_VISUAL_TRANSFORM_BEHAVIOR_*.
        /// - nRepeats: If > 0: N times, jump back to initial/from state after completing the transform. If -1: Do forever.
        /// Returns the old/previous value.
        /// </summary>
        public static float SetObjectVisualTransform(
            uint oObject,
            ObjectVisualTransform nTransform,
            float fValue,
            Lerp nLerpType = Lerp.None,
            float fLerpDuration = 0.0f,
            bool bPauseWithGame = true,
            ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Base,
            ObjectVisualTransformBehaviorType nBehaviorFlags = ObjectVisualTransformBehaviorType.Default,
            int nRepeats = 0)
        {
            return global::NWN.Core.NWScript.SetObjectVisualTransform(oObject, (int)nTransform, fValue, (int)nLerpType, fLerpDuration, bPauseWithGame ? 1 : 0, (int)nScope, (int)nBehaviorFlags, nRepeats);
        }

        /// <summary>
        ///   Sets an integer material shader uniform override.
        ///   - sMaterial needs to be a material on that object.
        ///   - sParam needs to be a valid shader parameter already defined on the material.
        /// </summary>
        public static void SetMaterialShaderUniformInt(uint oObject, string sMaterial, string sParam, int nValue)
        {
            global::NWN.Core.NWScript.SetMaterialShaderUniformInt(oObject, sMaterial, sParam, nValue);
        }

        /// <summary>
        ///   Sets a vec4 material shader uniform override.
        ///   - sMaterial needs to be a material on that object.
        ///   - sParam needs to be a valid shader parameter already defined on the material.
        ///   - You can specify a single float value to set just a float, instead of a vec4.
        /// </summary>
        public static void SetMaterialShaderUniformVec4(uint oObject, string sMaterial, string sParam, float fValue1,
            float fValue2 = 0.0f, float fValue3 = 0.0f, float fValue4 = 0.0f)
        {
            global::NWN.Core.NWScript.SetMaterialShaderUniformVec4(oObject, sMaterial, sParam, fValue1, fValue2, fValue3, fValue4);
        }

        /// <summary>
        ///   Resets material shader parameters on the given object:
        ///   - Supply a material to only reset shader uniforms for meshes with that material.
        ///   - Supply a parameter to only reset shader uniforms of that name.
        ///   - Supply both to only reset shader uniforms of that name on meshes with that material.
        /// </summary>
        public static void ResetMaterialShaderUniforms(uint oObject, string sMaterial = "", string sParam = "")
        {
            global::NWN.Core.NWScript.ResetMaterialShaderUniforms(oObject, sMaterial, sParam);
        }

        /// <summary>
        /// Sets whether or not oCreatures's nIconId is flashing in their GUI icon bar.  If oCreature does not
        /// have an icon associated with nIconId, nothing happens. This function does not add icons to 
        /// oCreatures's GUI icon bar. The icon will flash until the underlying effect is removed or this 
        /// function is called again with bFlashing = FALSE.
        /// - oCreature: Player object to affect
        /// - nIconId: Referenced to effecticons.2da or EFFECT_ICON_*
        /// - bFlashing: TRUE to force an existing icon to flash, FALSE to to stop.
        /// </summary>
        public static void SetEffectIconFlashing(uint oCreature, int nIconId, bool bFlashing = true)
        {
            global::NWN.Core.NWScript.SetEffectIconFlashing(oCreature, nIconId, bFlashing ? 1 : 0);
        }

        /// <summary>
        ///   Immediately unsets a VTs for the given object, with no lerp.
        ///   * nScope: one of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_, or -1 for all scopes
        ///   Returns TRUE only if transforms were successfully removed (valid object, transforms existed).
        /// </summary>
        public static bool ClearObjectVisualTransform(uint oObject, ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Invalid)
        {
            return global::NWN.Core.NWScript.ClearObjectVisualTransform(oObject, (int)nScope) != 0;
        }

        /// <summary>
        /// Sets the distance (in meters) at which oObject info will be sent to clients (default 45.0)
        /// This is still subject to other limitations, such as perception ranges for creatures
        /// Note: Increasing visibility ranges of many objects can have a severe negative effect on
        ///       network latency and server performance, and rendering additional objects will
        ///       impact graphics performance of clients. Use cautiously.
        /// </summary>
        public static void SetObjectVisibleDistance(uint oObject, float fDistance = 45.0f)
        {
            global::NWN.Core.NWScript.SetObjectVisibleDistance(oObject, fDistance);
        }

        /// <summary>
        /// Gets oObject's visible distance, as set by SetObjectVisibleDistance()
        /// Returns -1.0f on error
        /// </summary>
        public static float GetObjectVisibleDistance(uint oObject)
        {
            return global::NWN.Core.NWScript.GetObjectVisibleDistance(oObject);
        }

        /// <summary>
        /// Replaces oObject's animation sOld with sNew.
        /// Specifying sNew = "" will restore the original animation.
        /// </summary>
        public static void ReplaceObjectAnimation(uint oObject, string sOld, string sNew = "")
        {
            global::NWN.Core.NWScript.ReplaceObjectAnimation(oObject, sOld, sNew);
        }
    }
}
