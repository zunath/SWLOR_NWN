using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public class ObjectPlugin
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLocalVariableCount");
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLocalVariable");
            NWNCore.NativeFunctions.nwnxPushInt(index);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();

            var lv = new LocalVariable
            {
                Key = NWNCore.NativeFunctions.nwnxPopString(),
                Type = (LocalVariableType)NWNCore.NativeFunctions.nwnxPopInt()
            };
            return lv;
        }

        // Set the provided object's position to the provided vector.
        public static void SetPosition(uint obj, Vector3 pos, bool updateSubareas = true)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPosition");
            NWNCore.NativeFunctions.nwnxPushInt(updateSubareas ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushFloat(pos.X);
            NWNCore.NativeFunctions.nwnxPushFloat(pos.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(pos.Z);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Sets the provided object's current hit points to the provided value.
        public static void SetCurrentHitPoints(uint creature, int hp)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCurrentHitPoints");
            NWNCore.NativeFunctions.nwnxPushInt(hp);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set object's maximum hit points; will not work on PCs.
        public static void SetMaxHitPoints(uint creature, int hp)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMaxHitPoints");
            NWNCore.NativeFunctions.nwnxPushInt(hp);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Serialize the full object (including locals, inventory, etc) to base64 string
        public static string Serialize(uint obj)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "Serialize");
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Deserialize the object. The object will be created outside of the world and
        // needs to be manually positioned at a location/inventory.
        public static uint Deserialize(string serialized)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "Deserialize");
            NWNCore.NativeFunctions.nwnxPushString(serialized);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        // Returns the dialog resref of the object.
        public static string GetDialogResref(uint obj)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDialogResref");
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Sets the dialog resref of the object.
        public static void SetDialogResref(uint obj, string dialog)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDialogResref");
            NWNCore.NativeFunctions.nwnxPushString(dialog);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set obj's appearance. Will not update for PCs until they
        // re-enter the area.
        public static void SetAppearance(uint placeable, int appearance)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAppearance");
            NWNCore.NativeFunctions.nwnxPushInt(appearance);
            NWNCore.NativeFunctions.nwnxPushObject(placeable);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get obj's appearance
        public static int GetAppearance(uint obj)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAppearance");
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Return true if obj has visual effect nVFX applied to it
        public static int GetHasVisualEffect(uint obj, int nVfx)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHasVisualEffect");
            NWNCore.NativeFunctions.nwnxPushInt(nVfx);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Return damage immunity (in percent) against given damage type
        // Use DAMAGE_TYPE_* constants for damageType
        public static int GetDamageImmunity(uint obj, int damageType)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDamageImmunity");
            NWNCore.NativeFunctions.nwnxPushInt(damageType);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// Add or move obj to area at pos
        public static void AddToArea(uint obj, uint area, Vector3 pos)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddToArea");
            NWNCore.NativeFunctions.nwnxPushFloat(pos.Z);
            NWNCore.NativeFunctions.nwnxPushFloat(pos.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(pos.X);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set placeable as static or not.
        // Will not update for PCs until they re-enter the area
        public static bool GetPlaceableIsStatic(uint obj)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlaceableIsStatic");
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() != 0;
        }

        // Set placeable as static or not
        public static void SetPlaceableIsStatic(uint obj, bool isStatic)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlaceableIsStatic");
            NWNCore.NativeFunctions.nwnxPushInt(isStatic ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets if a door/placeable auto-removes the key after use.
        public static bool GetAutoRemoveKey(uint obj)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAutoRemoveKey");
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() != 0;
        }

        // Sets if a door/placeable auto-removes the key after use
        public static void SetAutoRemoveKey(uint obj, bool bRemoveKey)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAutoRemoveKey");
            NWNCore.NativeFunctions.nwnxPushInt(bRemoveKey ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get the geometry of a trigger
        public static string GetTriggerGeometry(uint oTrigger)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTriggerGeometry");
            NWNCore.NativeFunctions.nwnxPushObject(oTrigger);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Set the geometry of a trigger with a list of vertex positions.
        // sGeometry Needs to be in the following format -> {x.x, y.y, z.z} or {x.x, y.y}
        // Example Geometry: "{1.0, 1.0, 0.0}{4.0, 1.0, 0.0}{4.0, 4.0, 0.0}{1.0, 4.0, 0.0}"
        // The Z position is optional and will be calculated dynamically based
        // on terrain height if it's not provided.
        // The minimum number of vertices is 3.
        public static void SetTriggerGeometry(uint oTrigger, string sGeometry)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetTriggerGeometry");
            NWNCore.NativeFunctions.nwnxPushString(sGeometry);
            NWNCore.NativeFunctions.nwnxPushObject(oTrigger);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Export an object to the UserDirectory/nwnx folder
        public static void Export(uint oObject, string sFileName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "Export");
            NWNCore.NativeFunctions.nwnxPushString(sFileName);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get an object's integer variable variableName.
        public static int GetInt(uint obj, string variableName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetInt");
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set an object's integer variable variableName to newValue. Toggle persistence with persist.
        public static void SetInt(uint obj, string variableName, int newValue, bool persist)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetInt");
            NWNCore.NativeFunctions.nwnxPushInt(persist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(newValue);
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Delete an object's integer variable variableName.
        public static void DeleteInt(uint obj, string variableName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteInt");
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get an object's string variable variableName.
        public static string GetString(uint obj, string variableName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetString");
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Set an object's string variable variableName to newValue. Toggle persistence with persist.
        public static void SetString(uint obj, string variableName, string newValue, bool persist)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetString");
            NWNCore.NativeFunctions.nwnxPushInt(persist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushString(newValue);
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Delete an object's string variable variableName.
        public static void DeleteString(uint obj, string variableName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteString");
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get an object's float variable variableName.
        public static float GetFloat(uint obj, string variableName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFloat");
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopFloat();
        }

        // Set an object's float variable variableName to newValue. Toggle persistence with persist.
        public static void SetFloat(uint obj, string variableName, float newValue, bool persist)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFloat");
            NWNCore.NativeFunctions.nwnxPushInt(persist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushFloat(newValue);
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Delete an object's float variable variableName.
        public static void DeleteFloat(uint obj, string variableName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteFloat");
            NWNCore.NativeFunctions.nwnxPushString(variableName);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Delete any variables that match regexString set by SetInt, SetFloat, or SetString.
        public static void DeleteVarRegex(uint obj, string regexString)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeleteVarRegex");
            NWNCore.NativeFunctions.nwnxPushString(regexString);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPositionIsInTrigger");
            NWNCore.NativeFunctions.nwnxPushFloat(position.Z);
            NWNCore.NativeFunctions.nwnxPushFloat(position.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(position.X);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() != 0;
        }

        /// <summary>
        /// Gets the given object's internal type (NWNX_OBJECT_TYPE_INTERNAL_*)
        /// oObject The object.
        /// The object's type (NWNX_OBJECT_TYPE_INTERNAL_*)
        /// </summary>
        public static int GetInternalObjectType(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetInternalObjectType");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AcquireItem");
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }


        /// @brief Checks for specific spell immunity. Should only be called in spellscripts
        /// @param oDefender The object defending against the spell.
        /// @param oCaster The object casting the spell.
        /// @return -1 if defender has no immunity, 2 if the defender is immune
        public static int DoSpellImmunity(uint oDefender, uint oCaster)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DoSpellImmunity");

            NWNCore.NativeFunctions.nwnxPushObject(oCaster);
            NWNCore.NativeFunctions.nwnxPushObject(oDefender);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Checks for spell school/level immunities and mantles. Should only be called in spellscripts
        /// @param oDefender The object defending against the spell.
        /// @param oCaster The object casting the spell.
        /// @return -1 defender no immunity. 2 if immune. 3 if immune, but the immunity has a limit (example: mantles)
        public static int DoSpellLevelAbsorption(uint oDefender, uint oCaster)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DoSpellLevelAbsorption");
            NWNCore.NativeFunctions.nwnxPushObject(oCaster);
            NWNCore.NativeFunctions.nwnxPushObject(oDefender);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentHitPoints");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetDoorHasVisibleModel(uint oDoor)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDoorHasVisibleModel");
            NWNCore.NativeFunctions.nwnxPushObject(oDoor);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetIsDestroyable(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetIsDestroyable");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }


        public static void ClearSpellEffectsOnOthers(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ClearSpellEffectsOnOthers");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static string PeekUUID(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PeekUUID");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static void SetHasInventory(uint obj, bool bHasInventory)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetHasInventory");
            NWNCore.NativeFunctions.nwnxPushInt(bHasInventory ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCurrentAnimation(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentAnimation");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetAILevel(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAILevel");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetAILevel(uint oObject, int nLevel)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAILevel");
            NWNCore.NativeFunctions.nwnxPushInt(nLevel);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static string GetMapNote(uint oObject, int nID = 0, int nGender = 0)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMapNote");
            NWNCore.NativeFunctions.nwnxPushInt(nGender);
            NWNCore.NativeFunctions.nwnxPushInt(nID);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static void SetMapNote(uint oObject, string sMapNote, int nID = 0, int nGender = 0)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMapNote");
            NWNCore.NativeFunctions.nwnxPushInt(nGender);
            NWNCore.NativeFunctions.nwnxPushInt(nID);
            NWNCore.NativeFunctions.nwnxPushString(sMapNote);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}