using System.Numerics;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the ObjectPlugin for testing purposes.
    /// Provides comprehensive object management functionality including local variable manipulation,
    /// object positioning, serialization, and advanced object properties.
    /// </summary>
    public class ObjectPluginMock: IObjectPluginService
    {
        // Mock data storage
        private readonly Dictionary<uint, ObjectData> _objectData = new();
        private readonly Dictionary<uint, Dictionary<string, LocalVariableData>> _localVariables = new();
        private uint _nextObjectId = 0x7F000001;

        /// <summary>
        /// Retrieves the total number of local variables stored on the specified object.
        /// </summary>
        /// <param name="obj">The object to query. Must be a valid object with local variables.</param>
        /// <returns>The number of local variables on the object. Returns 0 if no variables exist.</returns>
        public int GetLocalVariableCount(uint obj)
        {
            return _localVariables.TryGetValue(obj, out var variables) ? variables.Count : 0;
        }

        /// <summary>
        /// Retrieves a local variable from the specified object by its index position.
        /// </summary>
        /// <param name="obj">The object to query. Must be a valid object with local variables.</param>
        /// <param name="index">The zero-based index of the variable to retrieve. Must be between 0 and GetLocalVariableCount() - 1.</param>
        /// <returns>A LocalVariable struct containing the variable's key and type information.</returns>
        public ObjectPluginService.LocalVariable GetLocalVariable(uint obj, int index)
        {
            if (!_localVariables.TryGetValue(obj, out var variables) || index < 0 || index >= variables.Count)
            {
                return new ObjectPluginService.LocalVariable { Key = "", Type = LocalVariableType.Int };
            }

            var variable = variables.Values.ElementAt(index);
            return new ObjectPluginService.LocalVariable
            {
                Key = variable.Key,
                Type = variable.Type
            };
        }

        /// <summary>
        /// Sets the position of the specified object to the provided 3D coordinates.
        /// </summary>
        /// <param name="obj">The object to move. Must be a valid object that can be positioned.</param>
        /// <param name="pos">The new 3D position vector (x, y, z coordinates).</param>
        /// <param name="updateSubareas">If true and obj is a creature, any triggers/traps at the new position will fire their events.</param>
        public void SetPosition(uint obj, Vector3 pos, bool updateSubareas = true)
        {
            GetObjectData(obj).Position = pos;
        }

        /// <summary>
        /// Sets the provided object's current hit points to the provided value.
        /// </summary>
        /// <param name="creature">The object.</param>
        /// <param name="hp">The hit points.</param>
        public void SetCurrentHitPoints(uint creature, int hp)
        {
            GetObjectData(creature).CurrentHitPoints = Math.Max(0, hp);
        }

        /// <summary>
        /// Set object's maximum hit points; will not work on PCs.
        /// </summary>
        /// <param name="creature">The object.</param>
        /// <param name="hp">The maximum hit points.</param>
        public void SetMaxHitPoints(uint creature, int hp)
        {
            GetObjectData(creature).MaxHitPoints = Math.Max(1, hp);
        }

        /// <summary>
        /// Serialize the full object (including locals, inventory, etc) to base64 string.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>A base64 string representation of the object.</returns>
        public string Serialize(uint obj)
        {
            // Mock implementation - returns a mock serialized string
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"MockSerializedObject_{obj}"));
        }

        /// <summary>
        /// Deserialize a base64 string to create a new object.
        /// </summary>
        /// <param name="serialized">The base64 string representation of the object.</param>
        /// <returns>The object ID of the deserialized object, or OBJECT_INVALID on failure.</returns>
        public uint Deserialize(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
                return OBJECT_INVALID;

            // Mock implementation - creates a new object ID
            var newObjectId = _nextObjectId++;
            _objectData[newObjectId] = new ObjectData();
            return newObjectId;
        }

        /// <summary>
        /// Gets the position of the specified object.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The 3D position vector of the object.</returns>
        public Vector3 GetPosition(uint obj)
        {
            return GetObjectData(obj).Position;
        }

        /// <summary>
        /// Gets the current hit points of the specified object.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The current hit points of the object.</returns>
        public int GetCurrentHitPoints(uint obj)
        {
            return GetObjectData(obj).CurrentHitPoints;
        }

        /// <summary>
        /// Gets the maximum hit points of the specified object.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The maximum hit points of the object.</returns>
        public int GetMaxHitPoints(uint obj)
        {
            return GetObjectData(obj).MaxHitPoints;
        }

        /// <summary>
        /// Sets a local integer variable on the specified object.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="value">The integer value to set.</param>
        public void SetLocalInt(uint obj, string key, int value)
        {
            SetLocalVariable(obj, key, LocalVariableType.Int, value.ToString());
        }

        /// <summary>
        /// Gets a local integer variable from the specified object.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="defaultValue">The default value to return if the variable doesn't exist.</param>
        /// <returns>The integer value of the variable, or the default value if not found.</returns>
        public int GetLocalInt(uint obj, string key, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;

            if (_localVariables.TryGetValue(obj, out var variables) &&
                variables.TryGetValue(key, out var variable) &&
                variable.Type == LocalVariableType.Int)
            {
                return int.TryParse(variable.Value, out var result) ? result : defaultValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets a local float variable on the specified object.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="value">The float value to set.</param>
        public void SetLocalFloat(uint obj, string key, float value)
        {
            SetLocalVariable(obj, key, LocalVariableType.Float, value.ToString());
        }

        /// <summary>
        /// Gets a local float variable from the specified object.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="defaultValue">The default value to return if the variable doesn't exist.</param>
        /// <returns>The float value of the variable, or the default value if not found.</returns>
        public float GetLocalFloat(uint obj, string key, float defaultValue = 0.0f)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;

            if (_localVariables.TryGetValue(obj, out var variables) &&
                variables.TryGetValue(key, out var variable) &&
                variable.Type == LocalVariableType.Float)
            {
                return float.TryParse(variable.Value, out var result) ? result : defaultValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets a local string variable on the specified object.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="value">The string value to set.</param>
        public void SetLocalString(uint obj, string key, string value)
        {
            SetLocalVariable(obj, key, LocalVariableType.String, value ?? "");
        }

        /// <summary>
        /// Gets a local string variable from the specified object.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="defaultValue">The default value to return if the variable doesn't exist.</param>
        /// <returns>The string value of the variable, or the default value if not found.</returns>
        public string GetLocalString(uint obj, string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue ?? "";

            if (_localVariables.TryGetValue(obj, out var variables) &&
                variables.TryGetValue(key, out var variable) &&
                variable.Type == LocalVariableType.String)
            {
                return variable.Value;
            }

            return defaultValue ?? "";
        }

        /// <summary>
        /// Sets a local object variable on the specified object.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="value">The object value to set.</param>
        public void SetLocalObject(uint obj, string key, uint value)
        {
            SetLocalVariable(obj, key, LocalVariableType.Object, value.ToString());
        }

        /// <summary>
        /// Gets a local object variable from the specified object.
        /// </summary>
        /// <param name="obj">The object to get the variable from.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="defaultValue">The default value to return if the variable doesn't exist.</param>
        /// <returns>The object value of the variable, or the default value if not found.</returns>
        public uint GetLocalObject(uint obj, string key, uint defaultValue = OBJECT_INVALID)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;

            if (_localVariables.TryGetValue(obj, out var variables) &&
                variables.TryGetValue(key, out var variable) &&
                variable.Type == LocalVariableType.Object)
            {
                return uint.TryParse(variable.Value, out var result) ? result : defaultValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Deletes a local variable from the specified object.
        /// </summary>
        /// <param name="obj">The object to delete the variable from.</param>
        /// <param name="key">The variable key/name to delete.</param>
        public void DeleteLocalVariable(uint obj, string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (_localVariables.TryGetValue(obj, out var variables))
            {
                variables.Remove(key);
            }
        }

        /// <summary>
        /// Deletes all local variables from the specified object.
        /// </summary>
        /// <param name="obj">The object to clear variables from.</param>
        public void ClearLocalVariables(uint obj)
        {
            if (_localVariables.TryGetValue(obj, out var variables))
            {
                variables.Clear();
            }
        }

        /// <summary>
        /// Gets the object's name.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The name of the object.</returns>
        public string GetName(uint obj)
        {
            return GetObjectData(obj).Name;
        }

        /// <summary>
        /// Sets the object's name.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="name">The new name for the object.</param>
        public void SetName(uint obj, string name)
        {
            GetObjectData(obj).Name = name ?? "";
        }

        /// <summary>
        /// Gets the object's tag.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The tag of the object.</returns>
        public string GetTag(uint obj)
        {
            return GetObjectData(obj).Tag;
        }

        /// <summary>
        /// Sets the object's tag.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="tag">The new tag for the object.</param>
        public void SetTag(uint obj, string tag)
        {
            GetObjectData(obj).Tag = tag ?? "";
        }

        /// <summary>
        /// Gets the object's resref.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The resref of the object.</returns>
        public string GetResRef(uint obj)
        {
            return GetObjectData(obj).ResRef;
        }

        /// <summary>
        /// Sets the object's resref.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="resRef">The new resref for the object.</param>
        public void SetResRef(uint obj, string resRef)
        {
            GetObjectData(obj).ResRef = resRef ?? "";
        }

        /// <summary>
        /// Gets the object's description.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The description of the object.</returns>
        public string GetDescription(uint obj)
        {
            return GetObjectData(obj).Description;
        }

        /// <summary>
        /// Sets the object's description.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="description">The new description for the object.</param>
        public void SetDescription(uint obj, string description)
        {
            GetObjectData(obj).Description = description ?? "";
        }

        /// <summary>
        /// Gets the object's appearance.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The appearance of the object.</returns>
        public int GetAppearance(uint obj)
        {
            return GetObjectData(obj).Appearance;
        }

        /// <summary>
        /// Sets the object's appearance.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="appearance">The new appearance for the object.</param>
        public void SetAppearance(uint obj, int appearance)
        {
            GetObjectData(obj).Appearance = appearance;
        }

        /// <summary>
        /// Gets the object's size.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The size of the object.</returns>
        public int GetSize(uint obj)
        {
            return GetObjectData(obj).Size;
        }

        /// <summary>
        /// Sets the object's size.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="size">The new size for the object.</param>
        public void SetSize(uint obj, int size)
        {
            GetObjectData(obj).Size = size;
        }

        /// <summary>
        /// Gets the object's facing.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The facing of the object.</returns>
        public float GetFacing(uint obj)
        {
            return GetObjectData(obj).Facing;
        }

        /// <summary>
        /// Sets the object's facing.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="facing">The new facing for the object.</param>
        public void SetFacing(uint obj, float facing)
        {
            GetObjectData(obj).Facing = facing;
        }

        /// <summary>
        /// Gets the object's area.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The area of the object.</returns>
        public uint GetArea(uint obj)
        {
            return GetObjectData(obj).Area;
        }

        /// <summary>
        /// Sets the object's area.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="area">The new area for the object.</param>
        public void SetArea(uint obj, uint area)
        {
            GetObjectData(obj).Area = area;
        }

        /// <summary>
        /// Gets the object's creator.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The creator of the object.</returns>
        public uint GetCreator(uint obj)
        {
            return GetObjectData(obj).Creator;
        }

        /// <summary>
        /// Sets the object's creator.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="creator">The new creator for the object.</param>
        public void SetCreator(uint obj, uint creator)
        {
            GetObjectData(obj).Creator = creator;
        }

        /// <summary>
        /// Gets the object's owner.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The owner of the object.</returns>
        public uint GetOwner(uint obj)
        {
            return GetObjectData(obj).Owner;
        }

        /// <summary>
        /// Sets the object's owner.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="owner">The new owner for the object.</param>
        public void SetOwner(uint obj, uint owner)
        {
            GetObjectData(obj).Owner = owner;
        }

        /// <summary>
        /// Gets the object's parent.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The parent of the object.</returns>
        public uint GetParent(uint obj)
        {
            return GetObjectData(obj).Parent;
        }

        /// <summary>
        /// Sets the object's parent.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="parent">The new parent for the object.</param>
        public void SetParent(uint obj, uint parent)
        {
            GetObjectData(obj).Parent = parent;
        }

        /// <summary>
        /// Gets the object's children.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The children of the object.</returns>
        public List<uint> GetChildren(uint obj)
        {
            return new List<uint>(GetObjectData(obj).Children);
        }

        /// <summary>
        /// Adds a child to the object.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="child">The child to add.</param>
        public void AddChild(uint obj, uint child)
        {
            var children = GetObjectData(obj).Children;
            if (!children.Contains(child))
            {
                children.Add(child);
            }
        }

        /// <summary>
        /// Removes a child from the object.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="child">The child to remove.</param>
        public void RemoveChild(uint obj, uint child)
        {
            GetObjectData(obj).Children.Remove(child);
        }

        /// <summary>
        /// Gets the object's inventory.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The inventory of the object.</returns>
        public List<uint> GetInventory(uint obj)
        {
            return new List<uint>(GetObjectData(obj).Inventory);
        }

        /// <summary>
        /// Adds an item to the object's inventory.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="item">The item to add.</param>
        public void AddToInventory(uint obj, uint item)
        {
            var inventory = GetObjectData(obj).Inventory;
            if (!inventory.Contains(item))
            {
                inventory.Add(item);
            }
        }

        /// <summary>
        /// Removes an item from the object's inventory.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="item">The item to remove.</param>
        public void RemoveFromInventory(uint obj, uint item)
        {
            GetObjectData(obj).Inventory.Remove(item);
        }

        /// <summary>
        /// Gets the object's equipped items.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The equipped items of the object.</returns>
        public Dictionary<EquipSlot, uint> GetEquippedItems(uint obj)
        {
            return new Dictionary<EquipSlot, uint>(GetObjectData(obj).EquippedItems);
        }

        /// <summary>
        /// Equips an item to the object.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="item">The item to equip.</param>
        /// <param name="slot">The slot to equip the item to.</param>
        public void EquipItem(uint obj, uint item, EquipSlot slot)
        {
            GetObjectData(obj).EquippedItems[slot] = item;
        }

        /// <summary>
        /// Unequips an item from the object.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="slot">The slot to unequip the item from.</param>
        public void UnequipItem(uint obj, EquipSlot slot)
        {
            GetObjectData(obj).EquippedItems.Remove(slot);
        }

        /// <summary>
        /// Gets the object's effects.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The effects of the object.</returns>
        public List<uint> GetEffects(uint obj)
        {
            return new List<uint>(GetObjectData(obj).Effects);
        }

        /// <summary>
        /// Adds an effect to the object.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="effect">The effect to add.</param>
        public void AddEffect(uint obj, uint effect)
        {
            var effects = GetObjectData(obj).Effects;
            if (!effects.Contains(effect))
            {
                effects.Add(effect);
            }
        }

        /// <summary>
        /// Removes an effect from the object.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="effect">The effect to remove.</param>
        public void RemoveEffect(uint obj, uint effect)
        {
            GetObjectData(obj).Effects.Remove(effect);
        }

        /// <summary>
        /// Gets the object's actions.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The actions of the object.</returns>
        public List<uint> GetActions(uint obj)
        {
            return new List<uint>(GetObjectData(obj).Actions);
        }

        /// <summary>
        /// Adds an action to the object.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="action">The action to add.</param>
        public void AddAction(uint obj, uint action)
        {
            var actions = GetObjectData(obj).Actions;
            if (!actions.Contains(action))
            {
                actions.Add(action);
            }
        }

        /// <summary>
        /// Removes an action from the object.
        /// </summary>
        /// <param name="obj">The object to modify.</param>
        /// <param name="action">The action to remove.</param>
        public void RemoveAction(uint obj, uint action)
        {
            GetObjectData(obj).Actions.Remove(action);
        }

        /// <summary>
        /// Gets the object's variables.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The variables of the object.</returns>
        public Dictionary<string, LocalVariableData> GetVariables(uint obj)
        {
            return new Dictionary<string, LocalVariableData>(_localVariables.TryGetValue(obj, out var variables) ? variables : new Dictionary<string, LocalVariableData>());
        }

        /// <summary>
        /// Gets the object's data for testing verification.
        /// </summary>
        /// <param name="obj">The object to query.</param>
        /// <returns>The object data for the specified object.</returns>
        public ObjectData GetObjectDataForTesting(uint obj)
        {
            return GetObjectData(obj);
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _objectData.Clear();
            _localVariables.Clear();
            _nextObjectId = 0x7F000001;
        }

        /// <summary>
        /// Creates a new mock object for testing.
        /// </summary>
        /// <returns>The object ID of the newly created object.</returns>
        public uint CreateMockObject()
        {
            var newObjectId = _nextObjectId++;
            _objectData[newObjectId] = new ObjectData();
            return newObjectId;
        }

        /// <summary>
        /// Gets the object data for the specified object, creating it if it doesn't exist.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The object data for the specified object.</returns>
        private ObjectData GetObjectData(uint obj)
        {
            if (!_objectData.TryGetValue(obj, out var data))
            {
                data = new ObjectData();
                _objectData[obj] = data;
            }
            return data;
        }

        /// <summary>
        /// Sets a local variable on the specified object.
        /// </summary>
        /// <param name="obj">The object to set the variable on.</param>
        /// <param name="key">The variable key/name.</param>
        /// <param name="type">The type of the variable.</param>
        /// <param name="value">The value to set.</param>
        private void SetLocalVariable(uint obj, string key, LocalVariableType type, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (!_localVariables.ContainsKey(obj))
            {
                _localVariables[obj] = new Dictionary<string, LocalVariableData>();
            }

            _localVariables[obj][key] = new LocalVariableData
            {
                Key = key,
                Type = type,
                Value = value
            };
        }

        // Missing methods from interface
        public string GetDialogResref(uint obj)
        {
            return GetObjectData(obj).DialogResref;
        }

        public void SetDialogResref(uint obj, string dialog)
        {
            GetObjectData(obj).DialogResref = dialog ?? "";
        }

        public bool GetHasVisualEffect(uint obj, int effect)
        {
            return GetObjectData(obj).VisualEffects.Contains(effect);
        }

        public int GetDamageImmunity(uint obj, int damageType)
        {
            return GetObjectData(obj).DamageImmunities.TryGetValue(damageType, out var immunity) ? immunity : 0;
        }

        public void AddToArea(uint obj, uint area, Vector3 pos)
        {
            var objectData = GetObjectData(obj);
            objectData.Area = area;
            objectData.Position = pos;
        }

        public bool GetPlaceableIsStatic(uint obj)
        {
            return GetObjectData(obj).IsStatic;
        }

        public void SetPlaceableIsStatic(uint obj, bool isStatic)
        {
            GetObjectData(obj).IsStatic = isStatic;
        }

        public bool GetAutoRemoveKey(uint obj)
        {
            return GetObjectData(obj).AutoRemoveKey;
        }

        public void SetAutoRemoveKey(uint obj, bool autoRemove)
        {
            GetObjectData(obj).AutoRemoveKey = autoRemove;
        }

        public string GetTriggerGeometry(uint obj)
        {
            return GetObjectData(obj).TriggerGeometry;
        }

        public void SetTriggerGeometry(uint obj, string geometry)
        {
            GetObjectData(obj).TriggerGeometry = geometry ?? "";
        }

        public void Export(uint obj, string filename, string tag)
        {
            // Mock implementation - in real tests, this would export the object
        }

        public int GetInt(uint obj, string key)
        {
            return GetObjectData(obj).IntVariables.TryGetValue(key, out var value) ? value : 0;
        }

        public void SetInt(uint obj, string key, int value, bool persist = false)
        {
            GetObjectData(obj).IntVariables[key] = value;
        }

        public void DeleteInt(uint obj, string key)
        {
            GetObjectData(obj).IntVariables.Remove(key);
        }

        public string GetString(uint obj, string key)
        {
            return GetObjectData(obj).StringVariables.TryGetValue(key, out var value) ? value : "";
        }

        public void SetString(uint obj, string key, string value, bool persist = false)
        {
            GetObjectData(obj).StringVariables[key] = value ?? "";
        }

        public void DeleteString(uint obj, string key)
        {
            GetObjectData(obj).StringVariables.Remove(key);
        }

        public float GetFloat(uint obj, string key)
        {
            return GetObjectData(obj).FloatVariables.TryGetValue(key, out var value) ? value : 0.0f;
        }

        public void SetFloat(uint obj, string key, float value, bool persist = false)
        {
            GetObjectData(obj).FloatVariables[key] = value;
        }

        public void DeleteFloat(uint obj, string key)
        {
            GetObjectData(obj).FloatVariables.Remove(key);
        }

        public void DeleteVarRegex(uint obj, string regex)
        {
            // Mock implementation - in real tests, this would delete variables matching regex
        }

        public bool GetPositionIsInTrigger(uint obj, Vector3 pos)
        {
            // Mock implementation - in real tests, this would check if position is in trigger
            return false;
        }

        public InternalObjectType GetInternalObjectType(uint obj)
        {
            return (InternalObjectType)GetObjectData(obj).InternalObjectType;
        }

        public bool AcquireItem(uint obj, uint item)
        {
            var objectData = GetObjectData(obj);
            objectData.Inventory.Add(item);
            return true;
        }

        public int DoSpellImmunity(uint obj, uint caster, int spell)
        {
            // Mock implementation - in real tests, this would handle spell immunity
            return 0;
        }

        public int DoSpellLevelAbsorption(uint obj, uint caster, int spell, int level, int maxLevel)
        {
            // Mock implementation - in real tests, this would handle spell level absorption
            return 0;
        }

        public bool GetDoorHasVisibleModel(uint obj)
        {
            return GetObjectData(obj).HasVisibleModel;
        }

        public bool GetIsDestroyable(uint obj)
        {
            return GetObjectData(obj).IsDestroyable;
        }

        public void ClearSpellEffectsOnOthers(uint obj)
        {
            // Mock implementation - in real tests, this would clear spell effects
        }

        public string PeekUUID(uint obj)
        {
            return GetObjectData(obj).UUID;
        }

        public void SetHasInventory(uint obj, bool hasInventory)
        {
            GetObjectData(obj).HasInventory = hasInventory;
        }

        public int GetCurrentAnimation(uint obj)
        {
            return GetObjectData(obj).CurrentAnimation;
        }

        public int GetAILevel(uint obj)
        {
            return GetObjectData(obj).AILevel;
        }

        public void SetAILevel(uint obj, int level)
        {
            GetObjectData(obj).AILevel = level;
        }

        public string GetMapNote(uint obj, int note, int noteType)
        {
            return GetObjectData(obj).MapNotes.TryGetValue(note, out var noteText) ? noteText : "";
        }

        public void SetMapNote(uint obj, string note, int noteType, int noteId)
        {
            GetObjectData(obj).MapNotes[noteId] = note ?? "";
        }

        public int GetLastSpellCastFeat(uint obj)
        {
            return GetObjectData(obj).LastSpellCastFeat;
        }

        public void SetLastTriggered(uint obj, uint trigger)
        {
            GetObjectData(obj).LastTriggered = trigger;
        }

        public float GetAoEObjectDurationRemaining(uint obj)
        {
            return GetObjectData(obj).AoEDurationRemaining;
        }

        public void SetConversationPrivate(uint obj, bool isPrivate)
        {
            GetObjectData(obj).ConversationPrivate = isPrivate;
        }

        public void SetAoEObjectRadius(uint obj, float radius)
        {
            GetObjectData(obj).AoERadius = radius;
        }

        public float GetAoEObjectRadius(uint obj)
        {
            return GetObjectData(obj).AoERadius;
        }

        public bool GetLastSpellCastSpontaneous(uint obj)
        {
            return GetObjectData(obj).LastSpellCastSpontaneous;
        }

        public int GetLastSpellCastDomainLevel(uint obj)
        {
            return GetObjectData(obj).LastSpellCastDomainLevel;
        }

        public void ForceAssignUUID(uint obj, string uuid)
        {
            GetObjectData(obj).UUID = uuid ?? "";
        }

        public int GetInventoryItemCount(uint obj)
        {
            return GetObjectData(obj).Inventory.Count;
        }

        public void OverrideSpellProjectileVFX(uint obj, int projectile, int impact, int beam, bool enable)
        {
            // Mock implementation - in real tests, this would override spell projectile VFX
        }

        public bool GetLastSpellInstant()
        {
            // Mock implementation - in real tests, this would return the last spell instant
            return false;
        }

        public void SetTrapCreator(uint obj, uint creator)
        {
            GetObjectData(obj).TrapCreator = creator;
        }

        public string GetLocalizedName(uint obj, int gender, int strRef)
        {
            return GetObjectData(obj).LocalizedNames.TryGetValue(strRef, out var name) ? name : "";
        }

        public void SetLocalizedName(uint obj, string name, int gender, int strRef)
        {
            GetObjectData(obj).LocalizedNames[strRef] = name ?? "";
        }

        // Constants
        private const uint OBJECT_INVALID = 0x7F000000;

        // Helper classes
        public class ObjectData
        {
            public Vector3 Position { get; set; } = Vector3.Zero;
            public int CurrentHitPoints { get; set; } = 100;
            public int MaxHitPoints { get; set; } = 100;
            public string Name { get; set; } = "";
            public string Tag { get; set; } = "";
            public string ResRef { get; set; } = "";
            public string Description { get; set; } = "";
            public int Appearance { get; set; } = 0;
            public int Size { get; set; } = 0;
            public float Facing { get; set; } = 0.0f;
            public uint Area { get; set; } = OBJECT_INVALID;
            public uint Creator { get; set; } = OBJECT_INVALID;
            public uint Owner { get; set; } = OBJECT_INVALID;
            public uint Parent { get; set; } = OBJECT_INVALID;
            public List<uint> Children { get; set; } = new();
            public List<uint> Inventory { get; set; } = new();
            public Dictionary<EquipSlot, uint> EquippedItems { get; set; } = new();
            public List<uint> Effects { get; set; } = new();
            public List<uint> Actions { get; set; } = new();
            
            // Additional properties for new methods
            public string DialogResref { get; set; } = "";
            public List<int> VisualEffects { get; set; } = new();
            public Dictionary<int, int> DamageImmunities { get; set; } = new();
            public bool IsStatic { get; set; } = false;
            public bool AutoRemoveKey { get; set; } = false;
            public string TriggerGeometry { get; set; } = "";
            public Dictionary<string, int> IntVariables { get; set; } = new();
            public Dictionary<string, string> StringVariables { get; set; } = new();
            public Dictionary<string, float> FloatVariables { get; set; } = new();
            public int InternalObjectType { get; set; } = 0;
            public bool HasVisibleModel { get; set; } = true;
            public bool IsDestroyable { get; set; } = true;
            public string UUID { get; set; } = "";
            public bool HasInventory { get; set; } = false;
            public int CurrentAnimation { get; set; } = 0;
            public int AILevel { get; set; } = 0;
            public Dictionary<int, string> MapNotes { get; set; } = new();
            public int LastSpellCastFeat { get; set; } = 0;
            public uint LastTriggered { get; set; } = OBJECT_INVALID;
            public float AoEDurationRemaining { get; set; } = 0.0f;
            public bool ConversationPrivate { get; set; } = false;
            public float AoERadius { get; set; } = 0.0f;
            public bool LastSpellCastSpontaneous { get; set; } = false;
            public int LastSpellCastDomainLevel { get; set; } = 0;
            public uint TrapCreator { get; set; } = OBJECT_INVALID;
            public Dictionary<int, string> LocalizedNames { get; set; } = new();
        }

        public class LocalVariableData
        {
            public string Key { get; set; } = "";
            public LocalVariableType Type { get; set; } = LocalVariableType.Int;
            public string Value { get; set; } = "";
        }

        public struct LocalVariable
        {
            public string Key;
            public LocalVariableType Type;
        }

        public enum EquipSlot
        {
            Head = 0,
            Chest = 1,
            Boots = 2,
            Arms = 3,
            RightHand = 4,
            LeftHand = 5,
            Cloak = 6,
            LeftRing = 7,
            RightRing = 8,
            Neck = 9,
            Belt = 10,
            Arrows = 11,
            Bullets = 12,
            Bolts = 13
        }
    }
}
