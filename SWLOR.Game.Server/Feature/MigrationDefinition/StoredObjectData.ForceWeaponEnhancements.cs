using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal sealed partial class StoredObjectData
    {
        internal static string RemoveForceWeaponEnhancements(string data)
        {
            var document = new StoredObjectData(Convert.FromBase64String(data));
            var visited = new HashSet<Node>();
            bool Migrate(Node node)
            {
                if (!visited.Add(node)) return false;
                var changed = false;
                var baseItem = node.Fields.SingleOrDefault(x => x.Name == "BaseItem");
                if (baseItem != null)
                {
                    var nameField = node.Fields.SingleOrDefault(x => x.Name == "LocalizedName" && x.Type == 12);
                    var names = ReadLocalizedNames(nameField);
                    var variables = node.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
                    var noEconomy = variables?.Children.Any(variable =>
                        ReadVariableName(variable) == Item.NoEconomyVariable && ReadVariableInteger(variable) == 1) == true;
                    var restricted = Item.IsEconomyRestricted((BaseItem)ReadScalar(baseItem),
                        names.FirstOrDefault() ?? "Saved item", noEconomy, hasInventoryIcon: true);
                    if (!restricted)
                    {
                        var properties = node.Fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15);
                        if (properties != null)
                        {
                            foreach (var property in properties.Children.ToArray())
                            {
                                var type = property.Fields.SingleOrDefault(x => x.Name == "PropertyName");
                                var subtype = property.Fields.SingleOrDefault(x => x.Name == "Subtype");
                                if (type == null || subtype == null) continue;
                                if (ReadScalar(type) == (int)ItemPropertyType.WeaponDamageType &&
                                    ReadScalar(subtype) == (int)CombatDamageType.Force)
                                {
                                    properties.Children.Remove(property);
                                    changed = true;
                                }
                                else if (ReadScalar(type) == (int)ItemPropertyType.WeaponEnhancement && ReadScalar(subtype) == 19)
                                {
                                    subtype.Data = BitConverter.GetBytes((int)EnhancementSubType.DMG);
                                    changed = true;
                                }
                            }
                        }
                        changed |= ReplaceLocalizedNames(nameField);
                        if (variables != null)
                            foreach (var variable in variables.Children)
                            {
                                var value = variable.Fields.SingleOrDefault(x => x.Name == "Value");
                                if (value == null) continue;
                                if (ReadVariableName(variable) == "BLUEPRINT_RECIPE_ID" && value.Type == 5)
                                {
                                    var recipe = (RecipeType)ReadScalar(value);
                                    var replacement = ForceWeaponEnhancementMigration.GetReplacementRecipe(recipe);
                                    if (recipe == replacement) continue;
                                    value.Data = BitConverter.GetBytes((int)replacement);
                                    changed = true;
                                }
                                else if (ReadVariableName(variable) == "RECIPES" && value.Type == 10)
                                {
                                    var recipes = Encoding.UTF8.GetString(value.Data.AsSpan(4));
                                    var replacement = ForceWeaponEnhancementMigration.MigrateRecipeList(recipes);
                                    if (recipes == replacement) continue;
                                    var text = Encoding.UTF8.GetBytes(replacement);
                                    value.Data = BitConverter.GetBytes(text.Length).Concat(text).ToArray();
                                    changed = true;
                                }
                            }
                    }
                    // Controller contents remain independent of the controller's economy classification.
                    if (variables != null)
                        foreach (var variable in variables.Children.Where(x => ReadVariableName(x) == "CONSTRUCTED_DROID"))
                        {
                            var value = variable.Fields.SingleOrDefault(x => x.Name == "Value" && x.Type == 10);
                            if (value == null) continue;
                            var original = Encoding.UTF8.GetString(value.Data.AsSpan(4));
                            var migrated = ForceWeaponEnhancementMigration.MigrateDroid(original);
                            if (original == migrated) continue;
                            var text = Encoding.UTF8.GetBytes(migrated);
                            value.Data = BitConverter.GetBytes(text.Length).Concat(text).ToArray();
                            changed = true;
                        }
                }
                foreach (var list in node.Fields.Where(IsInventoryList))
                    foreach (var item in list.Children) changed |= Migrate(item);
                return changed;
            }
            return Migrate(document._root) ? document.Serialize() : data;
        }

        private static int ReadScalar(Field field) => BinaryPrimitives.ReadInt32LittleEndian(field.Data);
        private static string ReadVariableName(Node variable)
        {
            var name = variable.Fields.SingleOrDefault(x => x.Name == "Name" && x.Type == 10);
            return name == null ? null : Encoding.UTF8.GetString(name.Data.AsSpan(4));
        }
        private static int ReadVariableInteger(Node variable)
        {
            var type = variable.Fields.SingleOrDefault(x => x.Name == "Type");
            var value = variable.Fields.SingleOrDefault(x => x.Name == "Value" && x.Type == 5);
            return type != null && ReadScalar(type) == 1 && value != null ? ReadScalar(value) : 0;
        }

        private static List<string> ReadLocalizedNames(Field field)
        {
            var names = new List<string>();
            if (field == null) return names;
            using var reader = new BinaryReader(new MemoryStream(field.Data), Encoding.UTF8);
            reader.ReadUInt32(); // Payload length.
            reader.ReadUInt32(); // String reference.
            var count = reader.ReadUInt32();
            for (var i = 0u; i < count; i++)
            {
                reader.ReadUInt32(); // Language/gender ID.
                names.Add(Encoding.UTF8.GetString(reader.ReadBytes(checked((int)reader.ReadUInt32()))));
            }
            return names;
        }

        private static bool ReplaceLocalizedNames(Field field)
        {
            if (field == null) return false;
            var changed = false;
            using var reader = new BinaryReader(new MemoryStream(field.Data), Encoding.UTF8);
            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output, Encoding.UTF8, true);
            writer.Write(reader.ReadUInt32());
            writer.Write(reader.ReadUInt32());
            var count = reader.ReadUInt32();
            writer.Write(count);
            for (var i = 0u; i < count; i++)
            {
                writer.Write(reader.ReadUInt32());
                var bytes = reader.ReadBytes(checked((int)reader.ReadUInt32()));
                var name = Encoding.UTF8.GetString(bytes);
                var replacement = ForceWeaponEnhancementMigration.GetReplacementName(name);
                changed |= name != replacement;
                var result = name == replacement ? bytes : Encoding.UTF8.GetBytes(replacement);
                writer.Write(result.Length);
                writer.Write(result);
            }
            if (!changed) return false;
            field.Data = output.ToArray();
            BinaryPrimitives.WriteInt32LittleEndian(field.Data, field.Data.Length - 4);
            return true;
        }
    }
}
