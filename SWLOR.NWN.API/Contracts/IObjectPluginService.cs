using System.Numerics;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.Contracts
{
    public interface IObjectPluginService
    {
        /// <summary>
        /// Retrieves the total number of local variables stored on the specified object.
        /// </summary>
        /// <param name="obj">The object to query. Must be a valid object with local variables.</param>
        /// <returns>The number of local variables on the object. Returns 0 if no variables exist.</returns>
        /// <remarks>
        /// This function counts all local variables that have been set on the object.
        /// Variables with default values (0, 0.0, "", OBJECT_INVALID) are not counted as they are considered "not set".
        /// Use GetLocalVariable() to retrieve individual variables by index.
        /// </remarks>
        int GetLocalVariableCount(uint obj);

        /// <summary>
        /// Retrieves a local variable from the specified object by its index position.
        /// </summary>
        /// <param name="obj">The object to query. Must be a valid object with local variables.</param>
        /// <param name="index">The zero-based index of the variable to retrieve. Must be between 0 and GetLocalVariableCount() - 1.</param>
        /// <returns>A LocalVariable struct containing the variable's key and type information.</returns>
        /// <remarks>
        /// This function retrieves local variables by their position in the object's variable list.
        /// Index bounds: 0 <= index < GetLocalVariableCount().
        /// As of build 8193.14, local variables no longer have strict ordering, so iteration order may change when variables are modified.
        /// This function takes O(n) time complexity, where n is the number of locals on the object.
        /// Variables with default values (0/0.0/""/OBJECT_INVALID) are not returned as they are considered "not set".
        /// Will return type UNKNOWN for cassowary variables.
        /// For better performance, use individual GetLocalXxx() functions which are O(1) time complexity.
        /// </remarks>
        ObjectPluginService.LocalVariable GetLocalVariable(uint obj, int index);

        /// <summary>
        /// Sets the position of the specified object to the provided 3D coordinates.
        /// </summary>
        /// <param name="obj">The object to move. Must be a valid object that can be positioned.</param>
        /// <param name="pos">The new 3D position vector (x, y, z coordinates).</param>
        /// <param name="updateSubareas">If true and obj is a creature, any triggers/traps at the new position will fire their events.</param>
        /// <remarks>
        /// This function immediately moves the object to the specified position in the world.
        /// If updateSubareas is true and the object is a creature, it will trigger any area effects (triggers, traps) at the new location.
        /// The object must be in a valid area for positioning to work correctly.
        /// Use with caution as this can bypass normal movement restrictions and collision detection.
        /// </remarks>
        void SetPosition(uint obj, Vector3 pos, bool updateSubareas = true);

        /// <summary>
        /// Sets the provided object's current hit points to the provided value.
        /// </summary>
        /// <param name="creature">The object.</param>
        /// <param name="hp">The hit points.</param>
        void SetCurrentHitPoints(uint creature, int hp);

        /// <summary>
        /// Set object's maximum hit points; will not work on PCs.
        /// </summary>
        /// <param name="creature">The object.</param>
        /// <param name="hp">The maximum hit points.</param>
        void SetMaxHitPoints(uint creature, int hp);

        /// <summary>
        /// Serialize the full object (including locals, inventory, etc) to base64 string.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>A base64 string representation of the object.</returns>
        string Serialize(uint obj);

        /// <summary>
        /// Deserialize the object. The object will be created outside of the world and needs to be manually positioned at a location/inventory.
        /// </summary>
        /// <param name="serialized">The base64 string.</param>
        /// <returns>The object.</returns>
        uint Deserialize(string serialized);

        /// <summary>
        /// Returns the dialog resref of the object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The name of the dialog resref.</returns>
        string GetDialogResref(uint obj);

        /// <summary>
        /// Sets the dialog resref of the object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="dialog">The name of the dialog resref.</param>
        void SetDialogResref(uint obj, string dialog);

        /// <summary>
        /// Set obj's appearance. Will not update for PCs until they re-enter the area.
        /// </summary>
        /// <param name="placeable">The placeable.</param>
        /// <param name="appearance">The appearance id.</param>
        void SetAppearance(uint placeable, int appearance);

        /// <summary>
        /// Get obj's appearance.
        /// </summary>
        /// <param name="obj">The placeable.</param>
        /// <returns>The appearance id.</returns>
        int GetAppearance(uint obj);

        /// <summary>
        /// Return true if obj has visual effect nVFX applied to it.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="nVfx">The visual effect id.</param>
        /// <returns>True if the object has the visual effect applied to it.</returns>
        bool GetHasVisualEffect(uint obj, int nVfx);

        /// <summary>
        /// Return damage immunity (in percent) against given damage type.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="damageType">The damage type to check for immunity. Use DAMAGE_TYPE_* constants.</param>
        /// <returns>Damage immunity as a percentage.</returns>
        int GetDamageImmunity(uint obj, int damageType);

        /// <summary>
        /// Add or move obj to area at pos.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="area">The area.</param>
        /// <param name="pos">The position.</param>
        void AddToArea(uint obj, uint area, Vector3 pos);

        /// <summary>
        /// Set placeable as or not. Will not update for PCs until they re-enter the area.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>True if placeable is static.</returns>
        bool GetPlaceableIsStatic(uint obj);

        /// <summary>
        /// Set placeable as or not.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="isStatic">True/False.</param>
        /// <remarks>Will not update for PCs until they re-enter the area.</remarks>
        void SetPlaceableIsStatic(uint obj, bool isStatic);

        /// <summary>
        /// Gets if a door/placeable auto-removes the key after use.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>True/False or -1 on error.</returns>
        bool GetAutoRemoveKey(uint obj);

        /// <summary>
        /// Sets if a door/placeable auto-removes the key after use.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="bRemoveKey">True/False.</param>
        void SetAutoRemoveKey(uint obj, bool bRemoveKey);

        /// <summary>
        /// Get the geometry of a trigger.
        /// </summary>
        /// <param name="oTrigger">The trigger object.</param>
        /// <returns>A string of vertex positions.</returns>
        string GetTriggerGeometry(uint oTrigger);

        /// <summary>
        /// Set the geometry of a trigger with a list of vertex positions.
        /// </summary>
        /// <param name="oTrigger">The trigger object.</param>
        /// <param name="sGeometry">Needs to be in the following format -> {x.x, y.y, z.z} or {x.x, y.y}. Example Geometry: "{1.0, 1.0, 0.0}{4.0, 1.0, 0.0}{4.0, 4.0, 0.0}{1.0, 4.0, 0.0}". The Z position is optional and will be calculated dynamically based on terrain height if it's not provided. The minimum number of vertices is 3.</param>
        void SetTriggerGeometry(uint oTrigger, string sGeometry);

        /// <summary>
        /// Export an object to the UserDirectory/nwnx folder.
        /// </summary>
        /// <param name="oObject">The object to export. Valid object types: Creature, Item, Placeable, Waypoint, Door, Store, Trigger.</param>
        /// <param name="sFileName">The filename without extension, 16 or less characters.</param>
        /// <param name="sAlias">The alias of the resource directory to add the .git file to. Default: UserDirectory/nwnx.</param>
        void Export(uint oObject, string sFileName, string sAlias = "NWNX");

        /// <summary>
        /// Get an object's integer variable variableName.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        /// <returns>The value or 0 on error.</returns>
        int GetInt(uint obj, string variableName);

        /// <summary>
        /// Set an object's integer variable variableName to newValue. Toggle persistence with persist.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="variableName">The variable name.</param>
        /// <param name="newValue">The integer value to set.</param>
        /// <param name="persist">When true, the value is persisted to GFF, this means that it'll be saved in the .bic file of a player's character or when an object is serialized.</param>
        void SetInt(uint obj, string variableName, int newValue, bool persist);

        /// <summary>
        /// Delete an object's integer variable variableName.
        /// </summary>
        /// <param name="obj">The object to delete the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        void DeleteInt(uint obj, string variableName);

        /// <summary>
        /// Get an object's string variable variableName.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        /// <returns>The value or "" on error.</returns>
        string GetString(uint obj, string variableName);

        /// <summary>
        /// Set an object's string variable variableName to newValue. Toggle persistence with persist.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="variableName">The variable name.</param>
        /// <param name="newValue">The string value to set.</param>
        /// <param name="persist">When true, the value is persisted to GFF, this means that it'll be saved in the .bic file of a player's character or when an object is serialized.</param>
        void SetString(uint obj, string variableName, string newValue, bool persist);

        /// <summary>
        /// Delete an object's string variable variableName.
        /// </summary>
        /// <param name="obj">The object to delete the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        void DeleteString(uint obj, string variableName);

        /// <summary>
        /// Get an object's float variable variableName.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        /// <returns>The value or 0.0f on error.</returns>
        float GetFloat(uint obj, string variableName);

        /// <summary>
        /// Set an object's float variable variableName to newValue. Toggle persistence with persist.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="variableName">The variable name.</param>
        /// <param name="newValue">The float value to set.</param>
        /// <param name="persist">When true, the value is persisted to GFF, this means that it'll be saved in the .bic file of a player's character or when an object is serialized.</param>
        void SetFloat(uint obj, string variableName, float newValue, bool persist);

        /// <summary>
        /// Delete an object's float variable variableName.
        /// </summary>
        /// <param name="obj">The object to delete the variable from.</param>
        /// <param name="variableName">The variable name.</param>
        void DeleteFloat(uint obj, string variableName);

        /// <summary>
        /// Delete any variables that match regexString set by SetInt, SetFloat, or SetString.
        /// </summary>
        /// <param name="obj">The object to delete the variables from.</param>
        /// <param name="regexString">The regular expression, for example .*Test.* removes every variable that has Test in it.</param>
        void DeleteVarRegex(uint obj, string regexString);

        /// <summary>
        /// Get if vPosition is inside oTrigger's geometry.
        /// </summary>
        /// <param name="obj">The trigger.</param>
        /// <param name="position">The position.</param>
        /// <returns>True if vPosition is inside oTrigger's geometry.</returns>
        /// <remarks>The Z value of vPosition is ignored.</remarks>
        bool GetPositionIsInTrigger(uint obj, Vector3 position);

        /// <summary>
        /// Gets the given object's internal type (NWNX_OBJECT_TYPE_INTERNAL_*).
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>The object's type (NWNX_OBJECT_TYPE_INTERNAL_*).</returns>
        InternalObjectType GetInternalObjectType(uint oObject);

        /// <summary>
        /// Have oObject acquire oItem.
        /// </summary>
        /// <param name="oObject">The object receiving oItem, must be a Creature, Placeable, Store or Item.</param>
        /// <param name="oItem">The item.</param>
        /// <returns>True on success.</returns>
        /// <remarks>Useful to give deserialized items to an object, may not work if oItem is already possessed by an object.</remarks>
        bool AcquireItem(uint oObject, uint oItem);

        /// <summary>
        /// Checks for specific spell immunity. Should only be called in spellscripts.
        /// </summary>
        /// <param name="oDefender">The object defending against the spell.</param>
        /// <param name="oCaster">The object casting the spell.</param>
        /// <param name="nSpellId">The casted spell id. Default value is -1, which corresponds to the normal game behaviour.</param>
        /// <returns>-1 if defender has no immunity, 2 if the defender is immune.</returns>
        int DoSpellImmunity(uint oDefender, uint oCaster, int nSpellId = -1);

        /// <summary>
        /// Checks for spell school/level immunities and mantles. Should only be called in spellscripts.
        /// </summary>
        /// <param name="oDefender">The object defending against the spell.</param>
        /// <param name="oCaster">The object casting the spell.</param>
        /// <param name="nSpellId">The casted spell id. Default value is -1, which corresponds to the normal game behaviour.</param>
        /// <param name="nSpellLevel">The level of the casted spell. Default value is -1, which corresponds to the normal game behaviour.</param>
        /// <param name="nSpellSchool">The school of the casted spell (SPELL_SCHOOL_* constant). Default value is -1, which corresponds to the normal game behaviour.</param>
        /// <returns>-1 defender no immunity. 2 if immune. 3 if immune, but the immunity has a limit (example: mantles).</returns>
        int DoSpellLevelAbsorption(uint oDefender, uint oCaster, int nSpellId = -1, int nSpellLevel = -1, int nSpellSchool = -1);

        /// <summary>
        /// Get an object's hit points.
        /// </summary>
        /// <param name="creature">The object.</param>
        /// <returns>The hit points.</returns>
        /// <remarks>Unlike the native GetCurrentHitpoints function, this excludes temporary hitpoints.</remarks>
        int GetCurrentHitPoints(uint creature);

        /// <summary>
        /// Get if oDoor has a visible model.
        /// </summary>
        /// <param name="oDoor">The door.</param>
        /// <returns>True if oDoor has a visible model.</returns>
        bool GetDoorHasVisibleModel(uint oDoor);

        /// <summary>
        /// Get if oObject is destroyable.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>True if oObject is destroyable.</returns>
        bool GetIsDestroyable(uint oObject);

        /// <summary>
        /// Clear all spell effects oObject has applied to others.
        /// </summary>
        /// <param name="oObject">The object that applied the spell effects.</param>
        void ClearSpellEffectsOnOthers(uint oObject);

        /// <summary>
        /// Peek at the UUID of oObject without assigning one if it does not have one.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>The UUID or "" when the object does not have or cannot have an UUID.</returns>
        string PeekUUID(uint oObject);

        /// <summary>
        /// Sets if a placeable has an inventory.
        /// </summary>
        /// <param name="obj">The placeable.</param>
        /// <param name="bHasInventory">True/False.</param>
        /// <remarks>Only works on placeables.</remarks>
        void SetHasInventory(uint obj, bool bHasInventory);

        /// <summary>
        /// Get the current animation of oObject.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>-1 on error or the engine animation constant.</returns>
        /// <remarks>The returned value will be an engine animation constant, not a NWScript ANIMATION_ constant.</remarks>
        int GetCurrentAnimation(uint oObject);

        /// <summary>
        /// Gets the AI level of an object.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>The AI level (AI_LEVEL_* -1 to 4).</returns>
        int GetAILevel(uint oObject);

        /// <summary>
        /// Sets the AI level of an object.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <param name="nLevel">The level to set (AI_LEVEL_* -1 to 4).</param>
        void SetAILevel(uint oObject, int nLevel);

        /// <summary>
        /// Retrieves the Map Note (AKA Map Pin) from a waypoint - Returns even if currently disabled.
        /// </summary>
        /// <param name="oObject">The Waypoint object.</param>
        /// <param name="nID">The Language ID (default English).</param>
        /// <param name="nGender">0 = Male, 1 = Female.</param>
        string GetMapNote(uint oObject, int nID = 0, int nGender = 0);

        /// <summary>
        /// Sets a Map Note (AKA Map Pin) to any waypoint, even if no previous map note. Only updates for clients on area-load. Use SetMapPinEnabled() as required.
        /// </summary>
        /// <param name="oObject">The Waypoint object.</param>
        /// <param name="sMapNote">The contents to set as the Map Note.</param>
        /// <param name="nID">The Language ID (default English).</param>
        /// <param name="nGender">0 = Male, 1 = Female.</param>
        void SetMapNote(uint oObject, string sMapNote, int nID = 0, int nGender = 0);

        /// <summary>
        /// Gets the last spell cast feat of the object.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>The feat ID, or 65535 when not cast by a feat, or -1 on error.</returns>
        /// <remarks>Should be called in a spell script.</remarks>
        int GetLastSpellCastFeat(uint oObject);

        /// <summary>
        /// Sets the last object that triggered door or placeable trap.
        /// </summary>
        /// <param name="oObject">Door or placeable object.</param>
        /// <param name="oLast">Object that last triggered trap.</param>
        /// <remarks>Should be retrieved with GetEnteringObject.</remarks>
        void SetLastTriggered(uint oObject, uint oLast);

        /// <summary>
        /// Gets the remaining duration of the AoE object.
        /// </summary>
        /// <param name="oAoE">The AreaOfEffect object.</param>
        /// <returns>The remaining duration, in seconds, or zero on failure.</returns>
        float GetAoEObjectDurationRemaining(uint oAoE);

        /// <summary>
        /// Sets conversations started by the object to be private or not.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <param name="bPrivate">True/False.</param>
        /// <remarks>ActionStartConversation()'s bPrivateConversation parameter will overwrite this flag.</remarks>
        void SetConversationPrivate(uint oObject, bool bPrivate);

        /// <summary>
        /// Sets the radius of a circle AoE object.
        /// </summary>
        /// <param name="oAoE">The AreaOfEffect object.</param>
        /// <param name="fRadius">The radius, must be bigger than 0.0f.</param>
        void SetAoEObjectRadius(uint oAoE, float fRadius);

        /// <summary>
        /// Gets the radius of a circle AoE object.
        /// </summary>
        /// <param name="oAoE">The AreaOfEffect object.</param>
        /// <returns>The radius or 0.0f on error.</returns>
        float GetAoEObjectRadius(uint oAoE);

        /// <summary>
        /// Gets whether the last spell cast of the object was spontaneous.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>True if the last spell was cast spontaneously.</returns>
        /// <remarks>Should be called in a spell script.</remarks>
        bool GetLastSpellCastSpontaneous(uint oObject);

        /// <summary>
        /// Gets the last spell cast domain level.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <returns>Domain level of the cast spell, 0 if not a domain spell.</returns>
        /// <remarks>Should be called in a spell script.</remarks>
        int GetLastSpellCastDomainLevel(uint oObject);

        /// <summary>
        /// Force the given object to carry the given UUID. Any other object currently owning the UUID is stripped of it.
        /// </summary>
        /// <param name="oObject">The object.</param>
        /// <param name="sUUID">The UUID to force.</param>
        void ForceAssignUUID(uint oObject, string sUUID);

        /// <summary>
        /// Returns how many items are in the object's inventory.
        /// </summary>
        /// <param name="oObject">A creature, placeable, item or store.</param>
        /// <returns>Returns a count of how many items are in the object's inventory.</returns>
        int GetInventoryItemCount(uint oObject);

        /// <summary>
        /// Override the projectile visual effect of ranged/throwing weapons and spells.
        /// </summary>
        /// <param name="oCreature">The creature.</param>
        /// <param name="nProjectileType">A projectile type constant or -1 to remove the override.</param>
        /// <param name="nProjectilePathType">A projectile path type constant or -1 to ignore.</param>
        /// <param name="nSpellID">A spell constant. -1 to ignore.</param>
        /// <param name="bPersist">Whether the override should persist to the .bic file (for PCs).</param>
        /// <remarks>
        /// Persistence is enabled after a server reset by the first use of this function. Recommended to trigger on a dummy target OnModuleLoad to enable persistence.
        /// This will override all spell projectile VFX from the creature until the override is removed.
        /// </remarks>
        void OverrideSpellProjectileVFX(uint oCreature, int nProjectileType = -1, int nProjectilePathType = -1, int nSpellID = -1, bool bPersist = false);

        /// <summary>
        /// Returns true if the last spell was cast instantly. This function should only be called in a spell script.
        /// </summary>
        /// <returns>True if the last spell was instant.</returns>
        /// <remarks>To initialize the hooks used by this function it is recommended to call this function once in your module load script.</remarks>
        bool GetLastSpellInstant();

        /// <summary>
        /// Sets the creator of a trap on door, placeable, or trigger. Also changes trap Faction to that of the new Creator.
        /// </summary>
        /// <param name="oObject">Door, placeable or trigger (trap) object.</param>
        /// <param name="oCreator">The new creator of the trap. Any non-creature creator will assign OBJECT_INVALID (similar to toolset-laid traps).</param>
        /// <remarks>
        /// Triggers (ground traps) will instantly update colour (Green/Red). Placeable/doors will not change if client has already seen them.
        /// </remarks>
        void SetTrapCreator(uint oObject, uint oCreator);

        /// <summary>
        /// Return the name of the object for the specified language.
        /// </summary>
        /// <param name="oObject">An object.</param>
        /// <param name="nLanguage">A PLAYER_LANGUAGE constant.</param>
        /// <param name="nGender">Gender to use, 0 or 1.</param>
        /// <returns>The localized string.</returns>
        string GetLocalizedName(uint oObject, int nLanguage, int nGender = 0);

        /// <summary>
        /// Set the name of the object as set in the toolset for the specified language.
        /// </summary>
        /// <param name="oObject">An object.</param>
        /// <param name="sName">New value to set.</param>
        /// <param name="nLanguage">A PLAYER_LANGUAGE constant.</param>
        /// <param name="nGender">Gender to use, 0 or 1.</param>
        /// <remarks>You may have to SetName(oObject, "") for the translated string to show.</remarks>
        void SetLocalizedName(uint oObject, string sName, int nLanguage, int nGender = 0);
    }
}