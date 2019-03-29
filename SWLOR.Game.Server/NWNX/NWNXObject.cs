using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXObject
    {
        private const string NWNX_Object = "NWNX_Object";

        /// <summary>
        /// Gets the count of all local variables on the provided object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetLocalVariableCount(NWObject obj)
        {
            string sFunc = "GetLocalVariableCount";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Returns a local variable at the specified index.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static LocalVariable GetLocalVariable(NWObject obj, int index)
        {
            string sFunc = "GetLocalVariable";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);

            LocalVariable var = new LocalVariable();
            var.Key = NWNX_GetReturnValueString(NWNX_Object, sFunc);
            var.Type = (LocalVariableType)NWNX_GetReturnValueInt(NWNX_Object, sFunc);
            return var;
        }

        /// <summary>
        /// Returns an object from the provided object ID.
        /// This is the counterpart to ObjectToString.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static NWObject StringToObject(string id)
        {
            string sFunc = "StringToObject";

            NWNX_PushArgumentString(NWNX_Object, sFunc, id);
            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueObject(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Set the provided object's position to the provided vector.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pos"></param>
        public static void SetPosition(NWObject obj, Vector pos)
        {
            string sFunc = "SetPosition";

            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_X);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_Y);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_Z);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);

        }

        /// <summary>
        /// Sets the provided object's current hit points to the provided value.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="hp"></param>
        public static void SetCurrentHitPoints(NWCreature creature, int hp)
        {
            string sFunc = "SetCurrentHitPoints";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, hp);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Set object's maximum hit points; will not work on PCs.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="hp"></param>
        public static void SetMaxHitPoints(NWCreature creature, int hp)
        {
            string sFunc = "SetMaxHitPoints";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, hp);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, creature);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Serialize the full object (including locals, inventory, etc) to base64 string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Serialize(NWObject obj)
        {
            string sFunc = "Serialize";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Deserialize the object. The object will be created outside of the world and
        /// needs to be manually positioned at a location/inventory.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static NWObject Deserialize(string serialized)
        {
            string sFunc = "Deserialize";

            NWNX_PushArgumentString(NWNX_Object, sFunc, serialized);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueObject(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Returns the dialog resref of the object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetDialogResref(NWObject obj)
        {
            string sFunc = "GetDialogResref";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Sets the dialog resref of the object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dialog"></param>
        public static void SetDialogResref(NWObject obj, string dialog)
        {
            string sFunc = "SetDialogResref";

            NWNX_PushArgumentString(NWNX_Object, sFunc, dialog);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Set obj's appearance. Will not update for PCs until they
        /// re-enter the area.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="app"></param>
        public static void SetAppearance(NWObject obj, int app)
        {
            string sFunc = "SetAppearance";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, app);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Get obj's appearance
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetAppearance(NWObject obj)
        {
            string sFunc = "GetAppearance";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Return true if obj has visual effect nVFX applied to it
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nVFX"></param>
        /// <returns></returns>
        public static bool GetHasVisualEffect(NWObject obj, int nVFX)
        {
            string sFunc = "GetHasVisualEffect";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, nVFX);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc) == _.TRUE;
        }

        /// <summary>
        /// Return true if an item of baseitem type can fit in object's inventory
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="baseitem"></param>
        /// <returns></returns>
        public static bool CheckFit(NWItem item, int baseitem)
        {
            string sFunc = "CheckFit";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, baseitem);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, item);

            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc) == _.TRUE;
        }

        /// <summary>
        /// Return damage immunity (in percent) against given damage type
        /// Use DAMAGE_TYPE_* constants for damageType
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="damageType"></param>
        /// <returns></returns>
        public static int GetDamageImmunity(NWObject obj, int damageType)
        {
            string sFunc = "GetDamageImmunity";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, damageType);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Add or move obj to area at pos
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="area"></param>
        /// <param name="pos"></param>
        public static void AddToArea(NWObject obj, NWArea area, Vector pos)
        {
            string sFunc = "AddToArea";

            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_Z);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_Y);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.m_X);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, area);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);
        }


    }
}
