using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public class Object
    {
        private const string PLUGIN_NAME = "NWNX_Object";

        public static int LocalVarTypeInt { get; } = 1;
        public static int LocalVarTypeFloat { get; } = 2;
        public static int LocalVarTypeObject { get; } = 4;
        public static int LocalVarTypeString { get; } = 3;
        public static int LocalVarTypeLocation { get; } = 5;

        // Gets the count of all local variables on the provided object.
        public static int GetLocalVariableCount(uint obj)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLocalVariableCount");
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }


        // @brief Gets the local variable at the provided index of the provided object.
        // @param obj The object.
        // @param index The index.
        // @note Index bounds: 0 >= index < NWNX_Object_GetLocalVariableCount().
        // @note As of build 8193.14 local variables no longer have strict ordering.
        //       this means that any change to the variables can result in drastically
        //       different order when iterating.
        // @note As of build 8193.14, this function takes O(n) time, where n is the number
        //       of locals on the object. Individual variable access with GetLocalXxx()
        //       is now O(1) though.
        // @note As of build 8193.14, this function may return variable type UNKNOWN
        //       if the value is the default (0/0.0/""/OBJECT_INVALID) for the type.
        // @return An NWNX_Object_LocalVariable struct.
        public static LocalVariable GetLocalVariable(uint obj, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLocalVariable");
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();

            var lv = new LocalVariable
            {
                Key = Internal.NativeFunctions.nwnxPopString(),
                Type = (LocalVariableType)Internal.NativeFunctions.nwnxPopInt()
            };
            return lv;
        }

        // Set the provided object's position to the provided vector.
        public static void SetPosition(uint obj, Vector3 pos, bool updateSubareas = true)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPosition");
            Internal.NativeFunctions.nwnxPushInt(updateSubareas ? 1 : 0);
            Internal.NativeFunctions.nwnxPushFloat(pos.X);
            Internal.NativeFunctions.nwnxPushFloat(pos.Y);
            Internal.NativeFunctions.nwnxPushFloat(pos.Z);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets the provided object's current hit points to the provided value.
        public static void SetCurrentHitPoints(uint creature, int hp)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCurrentHitPoints");
            Internal.NativeFunctions.nwnxPushInt(hp);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set object's maximum hit points; will not work on PCs.
        public static void SetMaxHitPoints(uint creature, int hp)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMaxHitPoints");
            Internal.NativeFunctions.nwnxPushInt(hp);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Serialize the full object (including locals, inventory, etc) to base64 string
        public static string Serialize(uint obj)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "Serialize");
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Deserialize the object. The object will be created outside of the world and
        // needs to be manually positioned at a location/inventory.
        public static uint Deserialize(string serialized)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "Deserialize");
            Internal.NativeFunctions.nwnxPushString(serialized);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }

        // Returns the dialog resref of the object.
        public static string GetDialogResref(uint obj)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDialogResref");
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Sets the dialog resref of the object.
        public static void SetDialogResref(uint obj, string dialog)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDialogResref");
            Internal.NativeFunctions.nwnxPushString(dialog);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set obj's appearance. Will not update for PCs until they
        // re-enter the area.
        public static void SetAppearance(uint placeable, int appearance)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAppearance");
            Internal.NativeFunctions.nwnxPushInt(appearance);
            Internal.NativeFunctions.nwnxPushObject(placeable);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get obj's appearance
        public static int GetAppearance(uint obj)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAppearance");
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Return true if obj has visual effect nVFX applied to it
        public static int GetHasVisualEffect(uint obj, int nVfx)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHasVisualEffect");
            Internal.NativeFunctions.nwnxPushInt(nVfx);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Return true if an item of baseitem type can fit in object's inventory
        public static int CheckFit(uint obj, int baseitem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CheckFit");
            Internal.NativeFunctions.nwnxPushInt(baseitem);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Return damage immunity (in percent) against given damage type
        // Use DAMAGE_TYPE_* constants for damageType
        public static int GetDamageImmunity(uint obj, int damageType)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDamageImmunity");
            Internal.NativeFunctions.nwnxPushInt(damageType);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// Add or move obj to area at pos
        public static void AddToArea(uint obj, uint area, Vector3 pos)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddToArea");
            Internal.NativeFunctions.nwnxPushFloat(pos.Z);
            Internal.NativeFunctions.nwnxPushFloat(pos.Y);
            Internal.NativeFunctions.nwnxPushFloat(pos.X);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set placeable as static or not.
        // Will not update for PCs until they re-enter the area
        public static bool GetPlaceableIsStatic(uint obj)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlaceableIsStatic");
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() != 0;
        }

        // Set placeable as static or not
        public static void SetPlaceableIsStatic(uint obj, bool isStatic)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlaceableIsStatic");
            Internal.NativeFunctions.nwnxPushInt(isStatic ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets if a door/placeable auto-removes the key after use.
        public static bool GetAutoRemoveKey(uint obj)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAutoRemoveKey");
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() != 0;
        }

        // Sets if a door/placeable auto-removes the key after use
        public static void SetAutoRemoveKey(uint obj, bool bRemoveKey)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAutoRemoveKey");
            Internal.NativeFunctions.nwnxPushInt(bRemoveKey ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the geometry of a trigger
        public static string GetTriggerGeometry(uint oTrigger)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTriggerGeometry");
            Internal.NativeFunctions.nwnxPushObject(oTrigger);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Set the geometry of a trigger with a list of vertex positions.
        // sGeometry Needs to be in the following format -> {x.x, y.y, z.z} or {x.x, y.y}
        // Example Geometry: "{1.0, 1.0, 0.0}{4.0, 1.0, 0.0}{4.0, 4.0, 0.0}{1.0, 4.0, 0.0}"
        // The Z position is optional and will be calculated dynamically based
        // on terrain height if it's not provided.
        // The minimum number of vertices is 3.
        public static void SetTriggerGeometry(uint oTrigger, string sGeometry)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetTriggerGeometry");
            Internal.NativeFunctions.nwnxPushString(sGeometry);
            Internal.NativeFunctions.nwnxPushObject(oTrigger);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Add an effect to an object that displays an icon and has no other effect.
        // See effecticons.2da for a list of possible effect icons.
        public static void AddIconEffect(uint obj, int nIcon, float fDuration = 0f)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddIconEffect");
            Internal.NativeFunctions.nwnxPushFloat(fDuration);
            Internal.NativeFunctions.nwnxPushInt(nIcon);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Remove an icon effect from an object that was added by the NWNX_Object_AddIconEffect() function.
        public static void RemoveIconEffect(uint obj, int nIcon)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveIconEffect");
            Internal.NativeFunctions.nwnxPushInt(nIcon);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Export an object to the UserDirectory/nwnx folder
        public static void Export(uint oObject, string sFileName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "Export");
            Internal.NativeFunctions.nwnxPushString(sFileName);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get an object's integer variable variableName.
        public static int GetInt(uint obj, string variableName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetInt");
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set an object's integer variable variableName to newValue. Toggle persistence with persist.
        public static void SetInt(uint obj, string variableName, int newValue, bool persist)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetInt");
            Internal.NativeFunctions.nwnxPushInt(persist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(newValue);
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Delete an object's integer variable variableName.
        public static void DeleteInt(uint obj, string variableName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteInt");
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get an object's string variable variableName.
        public static string GetString(uint obj, string variableName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetString");
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Set an object's string variable variableName to newValue. Toggle persistence with persist.
        public static void SetString(uint obj, string variableName, string newValue, bool persist)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetString");
            Internal.NativeFunctions.nwnxPushInt(persist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushString(newValue);
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Delete an object's string variable variableName.
        public static void DeleteString(uint obj, string variableName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteString");
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get an object's float variable variableName.
        public static float GetFloat(uint obj, string variableName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFloat");
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopFloat();
        }

        // Set an object's float variable variableName to newValue. Toggle persistence with persist.
        public static void SetFloat(uint obj, string variableName, float newValue, bool persist)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFloat");
            Internal.NativeFunctions.nwnxPushInt(persist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushFloat(newValue);
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Delete an object's float variable variableName.
        public static void DeleteFloat(uint obj, string variableName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteFloat");
            Internal.NativeFunctions.nwnxPushString(variableName);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Delete any variables that match regexString set by SetInt, SetFloat, or SetString.
        public static void DeleteVarRegex(uint obj, string regexString)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteVarRegex");
            Internal.NativeFunctions.nwnxPushString(regexString);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// <summary>
        /// Get if vPosition is inside oTrigger's geometry.
        /// The Z value of vPosition is ignored.
        /// oTrigger The trigger.
        /// vPosition The position.
        /// TRUE if vPosition is inside oTrigger's geometry.
        /// </summary>
        public static bool GetPositionIsInTrigger(uint obj, Vector3 position)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPositionIsInTrigger");
            Internal.NativeFunctions.nwnxPushFloat(position.Z);
            Internal.NativeFunctions.nwnxPushFloat(position.Y);
            Internal.NativeFunctions.nwnxPushFloat(position.X);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() != 0;
        }

        /// <summary>
        /// Gets the given object's internal type (NWNX_OBJECT_TYPE_INTERNAL_*)
        /// oObject The object.
        /// The object's type (NWNX_OBJECT_TYPE_INTERNAL_*)
        /// </summary>
        public static int GetInternalObjectType(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetInternalObjectType");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// <summary>
        /// Have oObject acquire oItem.
        /// Useful to give deserialized items to an object, may not work if oItem is already possessed by an object.
        /// oObject The object receiving oItem, must be a Creature, Placeable, Store or Item
        /// oItem The item.
        /// TRUE on success.
        /// </summary>
        public static int AcquireItem(uint oObject, uint oItem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AcquireItem");
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }


        /// @brief Checks for specific spell immunity. Should only be called in spellscripts
        /// @param oDefender The object defending against the spell.
        /// @param oCaster The object casting the spell.
        /// @return -1 if defender has no immunity, 2 if the defender is immune
        public static int DoSpellImmunity(uint oDefender, uint oCaster)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DoSpellImmunity");

            Internal.NativeFunctions.nwnxPushObject(oCaster);
            Internal.NativeFunctions.nwnxPushObject(oDefender);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// @brief Checks for spell school/level immunities and mantles. Should only be called in spellscripts
        /// @param oDefender The object defending against the spell.
        /// @param oCaster The object casting the spell.
        /// @return -1 defender no immunity. 2 if immune. 3 if immune, but the immunity has a limit (example: mantles)
        public static int DoSpellLevelAbsorption(uint oDefender, uint oCaster)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DoSpellLevelAbsorption");
            Internal.NativeFunctions.nwnxPushObject(oCaster);
            Internal.NativeFunctions.nwnxPushObject(oDefender);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public struct LocalVariable
        {
            public LocalVariableType Type;
            public string Key;
        }

        /// @brief Get an object's hit points.
        /// @note Unlike the native GetCurrentHitpoints function, this excludes temporary hitpoints.
        /// @param obj The object.
        /// @return The hit points.
        public static int GetCurrentHitPoints(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentHitPoints");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetDoorHasVisibleModel(uint oDoor)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDoorHasVisibleModel");
            Internal.NativeFunctions.nwnxPushObject(oDoor);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetIsDestroyable(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetIsDestroyable");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }


        public static void ClearSpellEffectsOnOthers(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ClearSpellEffectsOnOthers");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static string PeekUUID(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PeekUUID");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopString();
        }

        public static void SetHasInventory(uint obj, bool bHasInventory)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetHasInventory");
            Internal.NativeFunctions.nwnxPushInt(bHasInventory ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCurrentAnimation(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentAnimation");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetAILevel(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAILevel");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetAILevel(uint oObject, int nLevel)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAILevel");
            Internal.NativeFunctions.nwnxPushInt(nLevel);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static string GetMapNote(uint oObject, int nID = 0, int nGender = 0)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMapNote");
            Internal.NativeFunctions.nwnxPushInt(nGender);
            Internal.NativeFunctions.nwnxPushInt(nID);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopString();
        }

        public static void SetMapNote(uint oObject, string sMapNote, int nID = 0, int nGender = 0)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMapNote");
            Internal.NativeFunctions.nwnxPushInt(nGender);
            Internal.NativeFunctions.nwnxPushInt(nID);
            Internal.NativeFunctions.nwnxPushString(sMapNote);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}