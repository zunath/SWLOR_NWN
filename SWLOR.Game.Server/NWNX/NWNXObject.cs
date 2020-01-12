using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.NWScript;
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
        public static int GetLocalVariableCount(NWGameObject obj)
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
        public static LocalVariable GetLocalVariable(NWGameObject obj, int index)
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
        public static NWGameObject StringToObject(string id)
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
        public static void SetPosition(NWGameObject obj, Vector pos)
        {
            string sFunc = "SetPosition";

            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.X);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.Y);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.Z);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);

        }

        /// <summary>
        /// Sets the provided object's current hit points to the provided value.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="hp"></param>
        public static void SetCurrentHitPoints(NWGameObject creature, int hp)
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
        public static void SetMaxHitPoints(NWGameObject creature, int hp)
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
        public static string Serialize(NWGameObject obj)
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
        public static NWGameObject Deserialize(string serialized)
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
        public static string GetDialogResref(NWGameObject obj)
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
        public static void SetDialogResref(NWGameObject obj, string dialog)
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
        public static void SetAppearance(NWGameObject obj, int app)
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
        public static int GetAppearance(NWGameObject obj)
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
        public static bool GetHasVisualEffect(NWGameObject obj, int nVFX)
        {
            string sFunc = "GetHasVisualEffect";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, nVFX);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc) == 1;
        }

        /// <summary>
        /// Return true if an item of baseitem type can fit in object's inventory
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="baseitem"></param>
        /// <returns></returns>
        public static bool CheckFit(NWGameObject item, int baseitem)
        {
            string sFunc = "CheckFit";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, baseitem);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, item);

            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc) == 1;
        }

        /// <summary>
        /// Return damage immunity (in percent) against given damage type
        /// Use DAMAGE_TYPE_* constants for damageType
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="damageType"></param>
        /// <returns></returns>
        public static int GetDamageImmunity(NWGameObject obj, int damageType)
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
        public static void AddToArea(NWGameObject obj, NWGameObject area, Vector pos)
        {
            string sFunc = "AddToArea";

            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.Z);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.Y);
            NWNX_PushArgumentFloat(NWNX_Object, sFunc, pos.X);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, area);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Set placeable as static or not.
        /// Will not update for PCs until they re-enter the area
        /// </summary>
        /// <param name="obj">The object</param>
        /// <returns>true or false</returns>
        public static bool GetPlaceableIsStatic(NWGameObject obj)
        {
            string sFunc = "GetPlaceableIsStatic";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Object, sFunc) == 1;
        }

        /// <summary>
        /// Set placeable as static or not
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="isStatic">true or false</param>
        public static void SetPlaceableIsStatic(NWGameObject obj, bool isStatic)
        {
            string sFunc = "SetPlaceableIsStatic";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, isStatic ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Gets if a door/placeable auto-removes the key after use.
        /// </summary>
        /// <param name="obj">The object</param>
        /// <returns>true or false</returns>
        public static bool GetAutoRemoveKey(NWGameObject obj)
        {
            string sFunc = "GetAutoRemoveKey";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Object, sFunc) == 1;
        }

        /// <summary>
        /// Sets if a door/placeable auto-removes the key after use
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="bRemoveKey">true or false</param>
        public static void SetAutoRemoveKey(NWGameObject obj, bool bRemoveKey)
        {
            string sFunc = "SetAutoRemoveKey";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, bRemoveKey ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);

            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Get the geometry of a trigger
        /// </summary>
        /// <param name="oTrigger">The trigger object</param>
        /// <returns>A string of vertex positions</returns>
        public static string GetTriggerGeometry(NWGameObject oTrigger)
        {
            string sFunc = "GetTriggerGeometry";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, oTrigger);
            NWNX_CallFunction(NWNX_Object, sFunc);

            return NWNX_GetReturnValueString(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Set the geometry of a trigger with a list of vertex positions.
        /// sGeometry Needs to be in the following format -> {x.x, y.y, z.z} or {x.x, y.y}
        /// Example Geometry: "{1.0, 1.0, 0.0}{4.0, 1.0, 0.0}{4.0, 4.0, 0.0}{1.0, 4.0, 0.0}"
        /// The Z position is optional and will be calculated dynamically based
        /// on terrain height if it's not provided.
        /// The minimum number of vertices is 3.
        /// </summary>
        /// <param name="oTrigger">The trigger object.</param>
        /// <param name="sGeometry">The geometry. Refer to summary for format of string.</param>
        public static void SetTriggerGeometry(NWGameObject oTrigger, string sGeometry)
        {
            string sFunc = "SetTriggerGeometry";

            NWNX_PushArgumentString(NWNX_Object, sFunc, sGeometry);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, oTrigger);
            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Add an effect to an object that displays an icon and has no other effect.
        /// See effecticons.2da for a list of possible effect icons.
        /// </summary>
        /// <param name="obj">The object to apply the effect</param>
        /// <param name="nIcon">The icon id.</param>
        /// <param name="fDuration">If specified the effect will be temporary and last this length in seconds, otherwise the effect will be permanent.</param>
        public static void AddIconEffect(NWGameObject obj, int nIcon, float fDuration = 0.0f)
        {
            string sFunc = "AddIconEffect";

            NWNX_PushArgumentFloat(NWNX_Object, sFunc, fDuration);
            NWNX_PushArgumentInt(NWNX_Object, sFunc, nIcon);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Remove an icon effect from an object that was added by the NWNX_Object_AddIconEffect() function.
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="nIcon">The icon id.</param>
        public static void RemoveIconEffect(NWGameObject obj, int nIcon)
        {
            string sFunc = "RemoveIconEffect";

            NWNX_PushArgumentInt(NWNX_Object, sFunc, nIcon);
            NWNX_PushArgumentObject(NWNX_Object, sFunc, obj);
            NWNX_CallFunction(NWNX_Object, sFunc);
        }

        /// <summary>
        /// Export an object to the UserDirectory/nwnx folder
        /// </summary>
        /// <param name="sFileName">The filename without extension, 16 or less characters.</param>
        /// <param name="oObject">The object to export. Valid object types: Creature, Item, Placeable, Waypoint, Door, Store, Trigger</param>
        public static void Export(string sFileName, NWGameObject oObject)
        {
            string sFunc = "Export";

            NWNX_PushArgumentObject(NWNX_Object, sFunc, oObject);
            NWNX_PushArgumentString(NWNX_Object, sFunc, sFileName);
            NWNX_CallFunction(NWNX_Object, sFunc);
        }
    }
}
