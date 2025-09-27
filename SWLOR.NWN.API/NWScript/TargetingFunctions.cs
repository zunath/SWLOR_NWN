using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Makes a player character enter a targeting mode, letting them select an object as a target.
        /// If a PC selects a target, it will trigger the module OnPlayerTarget event.
        /// </summary>
        /// <param name="oPC">The player character to enter targeting mode</param>
        /// <param name="nValidObjectTypes">The valid object types that can be targeted</param>
        /// <param name="nMouseCursorId">The mouse cursor to display when hovering over valid targets</param>
        /// <param name="nBadTargetCursor">The mouse cursor to display when hovering over invalid targets</param>
        public static void EnterTargetingMode(uint oPC, ObjectType nValidObjectTypes = ObjectType.All, MouseCursor nMouseCursorId = MouseCursor.Magic, MouseCursor nBadTargetCursor = MouseCursor.NoMagic)
        {
            global::NWN.Core.NWScript.EnterTargetingMode(oPC, (int)nValidObjectTypes, (int)nMouseCursorId, (int)nBadTargetCursor);
        }

        /// <summary>
        /// Gets the target object in the module OnPlayerTarget event.
        /// Returns the area object when the target is the ground.
        /// </summary>
        /// <returns>The selected target object, or the area object when targeting the ground</returns>
        public static uint GetTargetingModeSelectedObject()
        {
            return global::NWN.Core.NWScript.GetTargetingModeSelectedObject();
        }

        /// <summary>
        /// Gets the target position in the module OnPlayerTarget event.
        /// </summary>
        /// <returns>The selected target position as a Vector3</returns>
        public static Vector3 GetTargetingModeSelectedPosition()
        {
            return global::NWN.Core.NWScript.GetTargetingModeSelectedPosition();
        }

        /// <summary>
        /// Gets the player object that triggered the OnPlayerTarget event.
        /// </summary>
        /// <returns>The player object that last selected a target</returns>
        public static uint GetLastPlayerToSelectTarget()
        {
            return global::NWN.Core.NWScript.GetLastPlayerToSelectTarget();
        }

        /// <summary>
        /// Sets the spell targeting data manually for the player.
        /// This data is usually specified in spells.2da.
        /// This data persists through spell casts; you're overwriting the entry in spells.2da for this session.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// </summary>
        /// <param name="oPlayer">The player to set the targeting data for</param>
        /// <param name="nSpell">SPELL_* constant</param>
        /// <param name="nShape">SPELL_TARGETING_SHAPE_* constant</param>
        /// <param name="fSizeX">Size X for the targeting area</param>
        /// <param name="fSizeY">Size Y for the targeting area</param>
        /// <param name="nFlags">SPELL_TARGETING_FLAGS_* constants</param>
        public static void SetSpellTargetingData(uint oPlayer, Spell nSpell, int nShape, float fSizeX, float fSizeY, int nFlags)
        {
            global::NWN.Core.NWScript.SetSpellTargetingData(oPlayer, (int)nSpell, nShape, fSizeX, fSizeY, nFlags);
        }

        /// <summary>
        /// Sets the spell targeting data which is used for the next call to EnterTargetingMode() for this player.
        /// If the shape is set to SPELL_TARGETING_SHAPE_NONE and the range is provided, the dotted line range indicator will still appear.
        /// </summary>
        /// <param name="oPlayer">The player to set the targeting data for</param>
        /// <param name="nShape">SPELL_TARGETING_SHAPE_* constant</param>
        /// <param name="fSizeX">Size X for the targeting area</param>
        /// <param name="fSizeY">Size Y for the targeting area</param>
        /// <param name="nFlags">SPELL_TARGETING_FLAGS_* constants</param>
        /// <param name="fRange">Range for the targeting area (optional, defaults to 0.0f)</param>
        /// <param name="nSpell">SPELL_* constant (optional, passed to the shader but does nothing by default, you need to edit the shader to use it)</param>
        /// <param name="nFeat">FEAT_* constant (optional, passed to the shader but does nothing by default, you need to edit the shader to use it)</param>
        public static void SetEnterTargetingModeData(
            uint oPlayer,
            int nShape,
            float fSizeX,
            float fSizeY,
            int nFlags,
            float fRange = 0.0f,
            Spell nSpell = Spell.AllSpells,
            FeatType nFeat = FeatType.Invalid)
        {
            global::NWN.Core.NWScript.SetEnterTargetingModeData(oPlayer, nShape, fSizeX, fSizeY, nFlags, fRange, (int)nSpell, (int)nFeat);
        }
    }
}
