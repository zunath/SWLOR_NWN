using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Makes oPC enter a targeting mode, letting them select an object as a target
        /// If a PC selects a target, it will trigger the module OnPlayerTarget event.
        /// </summary>
        public static void EnterTargetingMode(uint oPC, ObjectType nValidObjectTypes = ObjectType.All, MouseCursor nMouseCursorId = MouseCursor.Magic, MouseCursor nBadTargetCursor = MouseCursor.NoMagic)
        {
            global::NWN.Core.NWScript.EnterTargetingMode(oPC, (int)nValidObjectTypes, (int)nMouseCursorId, (int)nBadTargetCursor);
        }

        /// <summary>
        /// Gets the target object in the module OnPlayerTarget event.
        /// Returns the area object when the target is the ground.
        /// </summary>
        public static uint GetTargetingModeSelectedObject()
        {
            return global::NWN.Core.NWScript.GetTargetingModeSelectedObject();
        }

        /// <summary>
        /// Gets the target position in the module OnPlayerTarget event.
        /// </summary>
        public static Vector3 GetTargetingModeSelectedPosition()
        {
            return global::NWN.Core.NWScript.GetTargetingModeSelectedPosition();
        }

        /// <summary>
        /// Gets the player object that triggered the OnPlayerTarget event.
        /// </summary>
        public static uint GetLastPlayerToSelectTarget()
        {
            return global::NWN.Core.NWScript.GetLastPlayerToSelectTarget();
        }

        /// <summary>
        /// Sets the spell targeting data manually for the player. This data is usually specified in spells.2da.
        /// This data persists through spell casts; you're overwriting the entry in spells.2da for this session.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// - nSpell: SPELL_*
        /// - nShape: SPELL_TARGETING_SHAPE_*
        /// - nFlags: SPELL_TARGETING_FLAGS_*
        /// </summary>
        public static void SetSpellTargetingData(uint oPlayer, Spell nSpell, int nShape, float fSizeX, float fSizeY, int nFlags)
        {
            global::NWN.Core.NWScript.SetSpellTargetingData(oPlayer, (int)nSpell, nShape, fSizeX, fSizeY, nFlags);
        }

        /// <summary>
        /// Sets the spell targeting data which is used for the next call to EnterTargetingMode() for this player.
        /// If the shape is set to SPELL_TARGETING_SHAPE_NONE and the range is provided, the dotted line range indicator will still appear.
        /// - nShape: SPELL_TARGETING_SHAPE_*
        /// - nFlags: SPELL_TARGETING_FLAGS_*
        /// - nSpell: SPELL_* (optional, passed to the shader but does nothing by default, you need to edit the shader to use it)
        /// - nFeat: FEAT_* (optional, passed to the shader but does nothing by default, you need to edit the shader to use it)
        /// </summary>
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
