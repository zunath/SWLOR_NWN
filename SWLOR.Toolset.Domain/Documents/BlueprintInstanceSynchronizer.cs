using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Compares and rebuilds placed instances from a blueprint while retaining the fields that
    /// belong to the placement itself. Explicit "Update instances" actions use the rebuild path;
    /// blueprint renames use the reference-only path so placement overrides remain untouched.
    /// </summary>
    public static class BlueprintInstanceSynchronizer
    {
        public static string ListFieldName(ResourceType type) => type switch
        {
            ResourceType.Utc => "Creature List",
            ResourceType.Utd => "Door List",
            ResourceType.Uti => "List",
            ResourceType.Utp => "Placeable List",
            ResourceType.Uts => "SoundList",
            ResourceType.Utm => "StoreList",
            ResourceType.Utt => "TriggerList",
            ResourceType.Utw => "WaypointList",
            _ => throw new ArgumentOutOfRangeException(
                nameof(type), type, $"{type} does not have placed instances.")
        };

        public static JsonGffStruct BuildExpected(
            ResourceType type,
            JsonGffDocument blueprint,
            JsonGffStruct placedInstance,
            string targetResRef,
            Func<string, JsonGffDocument?>? loadItemBlueprint = null)
        {
            ArgumentNullException.ThrowIfNull(blueprint);
            ArgumentNullException.ThrowIfNull(placedInstance);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetResRef);

            if (type == ResourceType.Utm)
            {
                return StoreInstanceSynchronizer.BuildExpected(
                    blueprint,
                    placedInstance,
                    targetResRef,
                    loadItemBlueprint ?? (_ => null));
            }

            var (x, y, z) = InstanceFieldMap.GetPosition(type, placedInstance);
            var (xOrientation, yOrientation) =
                InstanceFieldMap.GetOrientation(type, placedInstance);
            var expected = InstanceFieldMap.CreateInstance(
                type,
                blueprint,
                targetResRef,
                x,
                y,
                z,
                xOrientation,
                yOrientation);

            // These values are authored on a placement, not inherited from its blueprint. A
            // template update must not turn a carefully sized trigger back into the default square
            // or discard a builder's per-instance visual transform.
            if (type == ResourceType.Utt)
                PreserveField(placedInstance, expected, "Geometry");
            PreserveField(placedInstance, expected, "VisualTransform");
            PreserveField(placedInstance, expected, "VisTransformList");
            return expected;
        }

        public static bool IsCurrent(
            ResourceType type,
            JsonGffDocument blueprint,
            JsonGffStruct placedInstance,
            string targetResRef,
            Func<string, JsonGffDocument?>? loadItemBlueprint = null) =>
            StoreInstanceSynchronizer.Equivalent(
                placedInstance,
                BuildExpected(type, blueprint, placedInstance, targetResRef, loadItemBlueprint));

        /// <summary>
        /// Replaces every placement referencing <paramref name="sourceResRef"/> and returns the
        /// number changed. The caller owns the surrounding edit/construction scope.
        /// </summary>
        public static int Synchronize(
            ResourceType type,
            JsonGffDocument blueprint,
            JsonGffDocument git,
            string sourceResRef,
            string targetResRef,
            Func<string, JsonGffDocument?>? loadItemBlueprint = null)
        {
            var list = git.Root.GetOrNull(ListFieldName(type));
            if (list?.Elements == null)
                return type == ResourceType.Uti
                    ? SynchronizeEmbeddedItemReferences(git.Root, sourceResRef, targetResRef)
                    : 0;

            var replacements = new List<(int Index, JsonGffStruct Value)>();
            for (var index = 0; index < list.Elements.Count; index++)
            {
                var instance = list.Elements[index];
                if (!string.Equals(
                        InstanceFieldMap.GetTemplateResRef(type, instance),
                        sourceResRef,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                replacements.Add((
                    index,
                    BuildExpected(
                        type,
                        blueprint,
                        instance,
                        targetResRef,
                        loadItemBlueprint)));
            }

            foreach (var replacement in replacements.OrderByDescending(value => value.Index))
            {
                list.RemoveElementAt(replacement.Index);
                list.InsertElement(replacement.Index, replacement.Value);
            }

            return replacements.Count + (type == ResourceType.Uti
                ? SynchronizeEmbeddedItemReferences(git.Root, sourceResRef, targetResRef)
                : 0);
        }

        /// <summary>
        /// Changes only blueprint identity references for a rename and returns the number changed.
        /// All other instance-authored fields remain exactly as the builder saved them. The caller
        /// owns the surrounding edit/construction scope.
        /// </summary>
        public static int RenameReferences(
            ResourceType type,
            JsonGffDocument git,
            string sourceResRef,
            string targetResRef)
        {
            ArgumentNullException.ThrowIfNull(git);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceResRef);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetResRef);

            var updated = 0;
            var list = git.Root.GetOrNull(ListFieldName(type));
            if (list?.Elements != null)
            {
                var identityField = InstanceFieldMap.GetInstanceTemplateField(type);
                foreach (var instance in list.Elements)
                {
                    if (!string.Equals(
                            InstanceFieldMap.GetTemplateResRef(type, instance),
                            sourceResRef,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    instance.SetString(identityField, GffFieldType.ResRef, targetResRef);
                    updated++;
                }
            }

            return updated + (type == ResourceType.Uti
                ? SynchronizeEmbeddedItemReferences(git.Root, sourceResRef, targetResRef)
                : 0);
        }

        private static int SynchronizeEmbeddedItemReferences(
            JsonGffStruct gitRoot,
            string sourceResRef,
            string targetResRef)
        {
            return RewriteItemResourceFields(gitRoot, sourceResRef, targetResRef);
        }

        private static int RewriteItemResourceFields(
            JsonGffStruct value,
            string sourceResRef,
            string targetResRef)
        {
            var updated = 0;
            foreach (var (name, field) in value.Entries)
            {
                if ((name is "InventoryRes" or "EquippedRes") &&
                    field.Type == GffFieldType.ResRef &&
                    string.Equals(
                        field.GetString(), sourceResRef, StringComparison.OrdinalIgnoreCase))
                {
                    field.SetString(targetResRef);
                    updated++;
                }

                if (field.Struct != null)
                    updated += RewriteItemResourceFields(field.Struct, sourceResRef, targetResRef);
                if (field.Elements != null)
                {
                    foreach (var element in field.Elements)
                    {
                        // Placed creatures, placeables, and stores expand inventory items inline.
                        // Those full UTI structs use TemplateResRef, but their containing list name
                        // identifies them as items. Rewriting every TemplateResRef recursively would
                        // also rename a creature/placeable blueprint that shared this ResRef.
                        if (name is "ItemList" or "Equip_ItemList" &&
                            string.Equals(
                                element.GetStringOrNull("TemplateResRef"),
                                sourceResRef,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            element.SetString(
                                "TemplateResRef", GffFieldType.ResRef, targetResRef);
                            updated++;
                        }

                        updated += RewriteItemResourceFields(element, sourceResRef, targetResRef);
                    }
                }
            }

            return updated;
        }

        private static void PreserveField(
            JsonGffStruct source,
            JsonGffStruct target,
            string fieldName)
        {
            if (source.GetOrNull(fieldName) is not { } sourceField)
                return;

            using var construction = EditScope.EnterConstruction();
            target.Remove(fieldName);
            target.Add(fieldName, InstanceFieldMap.CloneField(sourceField));
        }
    }
}
