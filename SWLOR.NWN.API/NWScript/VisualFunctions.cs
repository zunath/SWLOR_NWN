using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Display floaty text above the specified creature.
        /// The text will also appear in the chat buffer of each player that receives the
        /// floaty text.
        /// </summary>
        /// <param name="nStrRefToDisplay">String ref (therefore text is translated)</param>
        /// <param name="oCreatureToFloatAbove">The creature to display the text above</param>
        /// <param name="bBroadcastToFaction">If this is TRUE then only creatures in the same faction
        /// as oCreatureToFloatAbove will see the floaty text, and only if they are within range (30 metres)</param>
        public static void FloatingTextStrRefOnCreature(int nStrRefToDisplay, uint oCreatureToFloatAbove,
            bool bBroadcastToFaction = true)
        {
            global::NWN.Core.NWScript.FloatingTextStrRefOnCreature(nStrRefToDisplay, oCreatureToFloatAbove, bBroadcastToFaction ? 1 : 0);
        }

        /// <summary>
        /// Display floaty text above the specified creature.
        /// The text will also appear in the chat buffer of each player that receives the
        /// floaty text.
        /// </summary>
        /// <param name="sStringToDisplay">String to display</param>
        /// <param name="oCreatureToFloatAbove">The creature to display the text above</param>
        /// <param name="bBroadcastToFaction">If this is TRUE then only creatures in the same faction
        /// as oCreatureToFloatAbove will see the floaty text, and only if they are within range (30 metres)</param>
        public static void FloatingTextStringOnCreature(string sStringToDisplay, uint oCreatureToFloatAbove,
            bool bBroadcastToFaction = true)
        {
            global::NWN.Core.NWScript.FloatingTextStringOnCreature(sStringToDisplay, oCreatureToFloatAbove, bBroadcastToFaction ? 1 : 0);
        }

        /// <summary>
        /// Displays a message on the player's screen.
        /// The message is displayed on top of whatever is on the screen, including UI elements.
        /// </summary>
        /// <param name="PC">The player character to display the message to</param>
        /// <param name="Msg">The message to display</param>
        /// <param name="X">X coordinate of the first character to be displayed. The value is in terms
        /// of character 'slot' relative to the anchor point. If negative, applied from the right</param>
        /// <param name="Y">Y coordinate of the first character to be displayed. The value is in terms
        /// of character 'slot' relative to the anchor point. If negative, applied from the bottom</param>
        /// <param name="anchor">Screen anchor point constant</param>
        /// <param name="life">Duration in seconds until the string disappears</param>
        /// <param name="RGBA">Color of the string in 0xRRGGBBAA format</param>
        /// <param name="RGBA2">End color in 0xRRGGBBAA format. String starts at RGBA but slowly blends into RGBA2 as it nears end of life</param>
        /// <param name="ID">Optional ID of a string. If not 0, subsequent calls to PostString will
        /// remove the old string with the same ID, even if its lifetime has not elapsed. Only positive values allowed</param>
        /// <param name="font">If specified, use this custom font instead of default console font</param>
        public static void PostString(uint PC, string Msg, int X = 0, int Y = 0, ScreenAnchor anchor = ScreenAnchor.TopLeft,
            float life = 10.0f, int RGBA = 2147418367, int RGBA2 = 2147418367, int ID = 0, string font = "")
        {
            global::NWN.Core.NWScript.PostString(PC, Msg, X, Y, (int)anchor, life, RGBA, RGBA2, ID, font);
        }

        /// <summary>
        /// Sets the object's hilite color.
        /// </summary>
        /// <param name="oObject">The object to set the hilite color for</param>
        /// <param name="nColor">Color in 0xRRGGBB format; -1 clears the color override</param>
        public static void SetObjectHiliteColor(uint oObject, int nColor = -1)
        {
            global::NWN.Core.NWScript.SetObjectHiliteColor(oObject, nColor);
        }

        /// <summary>
        /// Sets the cursor to use when hovering over the object.
        /// </summary>
        /// <param name="oObject">The object to set the mouse cursor for</param>
        /// <param name="nCursor">The mouse cursor type to use when hovering over the object</param>
        public static void SetObjectMouseCursor(uint oObject, MouseCursor nCursor = MouseCursor.Invalid)
        {
            global::NWN.Core.NWScript.SetObjectMouseCursor(oObject, (int)nCursor);
        }

        /// <summary>
        /// Makes a player load a different texture instead of the original.
        /// </summary>
        /// <param name="OldName">The original texture name to replace</param>
        /// <param name="NewName">The new texture name to load instead. Setting to empty string will clear the override and revert to original</param>
        /// <param name="PC">The player character to apply the override to. If OBJECT_INVALID, applies to all active players</param>
        public static void SetTextureOverride(string OldName, string NewName = "", uint PC = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetTextureOverride(OldName, NewName, PC);
        }

        /// <summary>
        /// Gets a visual transform on the given object.
        /// </summary>
        /// <param name="oObject">Any valid Creature, Placeable, Item or Door</param>
        /// <param name="nTransform">One of OBJECT_VISUAL_TRANSFORM_* constants</param>
        /// <returns>The current (or default) value of the visual transform</returns>
        public static float GetObjectVisualTransform(uint oObject, ObjectVisualTransform nTransform)
        {
            return global::NWN.Core.NWScript.GetObjectVisualTransform(oObject, (int)nTransform);
        }

        /// <summary>
        /// Sets a visual transform on the given object.
        /// </summary>
        /// <param name="oObject">Any valid Creature, Placeable, Item or Door</param>
        /// <param name="nTransform">One of OBJECT_VISUAL_TRANSFORM_* constants</param>
        /// <param name="fValue">Value depends on the transformation to apply</param>
        /// <param name="nLerpType">Lerp type for the transformation</param>
        /// <param name="fLerpDuration">Duration of the lerp transformation</param>
        /// <param name="bPauseWithGame">Whether to pause the transformation when the game is paused</param>
        /// <param name="nScope">One of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_* constants, specific to the object type being transformed</param>
        /// <param name="nBehaviorFlags">Bitmask of OBJECT_VISUAL_TRANSFORM_BEHAVIOR_* constants</param>
        /// <param name="nRepeats">If > 0: N times, jump back to initial/from state after completing the transform. If -1: Do forever</param>
        /// <returns>The old/previous value of the visual transform</returns>
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
        /// Sets an integer material shader uniform override.
        /// </summary>
        /// <param name="oObject">The object to set the shader uniform on</param>
        /// <param name="sMaterial">Material on that object</param>
        /// <param name="sParam">Valid shader parameter already defined on the material</param>
        /// <param name="nValue">Integer value to set</param>
        public static void SetMaterialShaderUniformInt(uint oObject, string sMaterial, string sParam, int nValue)
        {
            global::NWN.Core.NWScript.SetMaterialShaderUniformInt(oObject, sMaterial, sParam, nValue);
        }

        /// <summary>
        /// Sets a vec4 material shader uniform override.
        /// </summary>
        /// <param name="oObject">The object to set the shader uniform on</param>
        /// <param name="sMaterial">Material on that object</param>
        /// <param name="sParam">Valid shader parameter already defined on the material</param>
        /// <param name="fValue1">First float value (required)</param>
        /// <param name="fValue2">Second float value (optional, defaults to 0.0f)</param>
        /// <param name="fValue3">Third float value (optional, defaults to 0.0f)</param>
        /// <param name="fValue4">Fourth float value (optional, defaults to 0.0f). You can specify a single float value to set just a float, instead of a vec4</param>
        public static void SetMaterialShaderUniformVec4(uint oObject, string sMaterial, string sParam, float fValue1,
            float fValue2 = 0.0f, float fValue3 = 0.0f, float fValue4 = 0.0f)
        {
            global::NWN.Core.NWScript.SetMaterialShaderUniformVec4(oObject, sMaterial, sParam, fValue1, fValue2, fValue3, fValue4);
        }

        /// <summary>
        /// Resets material shader parameters on the given object.
        /// </summary>
        /// <param name="oObject">The object to reset shader uniforms on</param>
        /// <param name="sMaterial">Supply a material to only reset shader uniforms for meshes with that material</param>
        /// <param name="sParam">Supply a parameter to only reset shader uniforms of that name. Supply both to only reset shader uniforms of that name on meshes with that material</param>
        public static void ResetMaterialShaderUniforms(uint oObject, string sMaterial = "", string sParam = "")
        {
            global::NWN.Core.NWScript.ResetMaterialShaderUniforms(oObject, sMaterial, sParam);
        }

        /// <summary>
        /// Sets whether or not the creature's icon is flashing in their GUI icon bar.
        /// If the creature does not have an icon associated with the icon ID, nothing happens.
        /// This function does not add icons to the creature's GUI icon bar.
        /// The icon will flash until the underlying effect is removed or this function is called again with bFlashing = FALSE.
        /// </summary>
        /// <param name="oCreature">Player object to affect</param>
        /// <param name="nIconId">Referenced to effecticons.2da or EFFECT_ICON_* constants</param>
        /// <param name="bFlashing">TRUE to force an existing icon to flash, FALSE to stop</param>
        public static void SetEffectIconFlashing(uint oCreature, int nIconId, bool bFlashing = true)
        {
            global::NWN.Core.NWScript.SetEffectIconFlashing(oCreature, nIconId, bFlashing ? 1 : 0);
        }

        /// <summary>
        /// Immediately unsets visual transforms for the given object, with no lerp.
        /// </summary>
        /// <param name="oObject">The object to clear visual transforms from</param>
        /// <param name="nScope">One of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_ constants, or Invalid for all scopes</param>
        /// <returns>TRUE only if transforms were successfully removed (valid object, transforms existed)</returns>
        public static bool ClearObjectVisualTransform(uint oObject, ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Invalid)
        {
            return global::NWN.Core.NWScript.ClearObjectVisualTransform(oObject, (int)nScope) != 0;
        }

        /// <summary>
        /// Sets the distance (in meters) at which object info will be sent to clients.
        /// This is still subject to other limitations, such as perception ranges for creatures.
        /// Note: Increasing visibility ranges of many objects can have a severe negative effect on
        /// network latency and server performance, and rendering additional objects will
        /// impact graphics performance of clients. Use cautiously.
        /// </summary>
        /// <param name="oObject">The object to set the visible distance for</param>
        /// <param name="fDistance">Distance in meters (default 45.0)</param>
        public static void SetObjectVisibleDistance(uint oObject, float fDistance = 45.0f)
        {
            global::NWN.Core.NWScript.SetObjectVisibleDistance(oObject, fDistance);
        }

        /// <summary>
        /// Gets the object's visible distance, as set by SetObjectVisibleDistance().
        /// </summary>
        /// <param name="oObject">The object to get the visible distance for</param>
        /// <returns>The visible distance in meters, or -1.0f on error</returns>
        public static float GetObjectVisibleDistance(uint oObject)
        {
            return global::NWN.Core.NWScript.GetObjectVisibleDistance(oObject);
        }

        /// <summary>
        /// Replaces the object's animation with a new one.
        /// </summary>
        /// <param name="oObject">The object to replace the animation for</param>
        /// <param name="sOld">The old animation name to replace</param>
        /// <param name="sNew">The new animation name. Specifying empty string will restore the original animation</param>
        public static void ReplaceObjectAnimation(uint oObject, string sOld, string sNew = "")
        {
            global::NWN.Core.NWScript.ReplaceObjectAnimation(oObject, sOld, sNew);
        }
    }
}
