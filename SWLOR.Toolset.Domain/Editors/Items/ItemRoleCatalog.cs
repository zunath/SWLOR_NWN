namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>Every item role and the family-scoped, data-driven classifier for existing content.</summary>
    public static class ItemRoleCatalog
    {
        public const string ConsumableId = "consumable";
        public const string MealId = "meal";
        public const string DeployedDeviceId = "deployed_device";
        public const string DroidPartId = "droid_part";
        public const string IncubationSampleId = "incubation_sample";
        public const string SchematicId = "schematic";
        public const string KeyItemId = "key_item";
        public const string ComponentId = "component";
        public const string EnhancementId = "enhancement";
        public const string CreatureItemId = "creature_item";
        public const string CustomId = "custom";

        /// <summary>Properties whose presence marks an Essence as slotting into another item.</summary>
        private static readonly int[] EnhancementProperties = { 101, 102, 107, 108, 109, 110, 116 };

        public static IReadOnlyList<ItemRole> All { get; } = Build();

        public static ItemRole Custom => Get(CustomId);

        public static ItemRole Get(string id) =>
            All.FirstOrDefault(role => role.Id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "No such item role.");

        public static IReadOnlyList<ItemRole> RolesFor(ItemFamily family) => family switch
        {
            ItemFamily.Miscellaneous => new[]
            {
                Get(ConsumableId), Get(MealId), Get(DeployedDeviceId), Get(DroidPartId),
                Get(IncubationSampleId), Get(SchematicId), Get(KeyItemId), Custom
            },
            ItemFamily.Essence => new[] { Get(ComponentId), Get(EnhancementId), Custom },
            ItemFamily.CreatureItem => new[] { Get(CreatureItemId), Custom },
            ItemFamily.Tool => new[] { Custom },
            _ => Array.Empty<ItemRole>()
        };

        /// <summary>
        /// Reads what an item's own PropertiesList already says about its role. Grenade cannot be
        /// told apart from Consumable by stored data alone - both are just a CastSpell property -
        /// so a grenade blueprint classifies as Consumable until something else distinguishes it.
        /// </summary>
        public static ItemRole Classify(ItemValueStore store, ItemFamily family)
        {
            ArgumentNullException.ThrowIfNull(store);

            if (family == ItemFamily.CreatureItem)
                return Get(CreatureItemId);

            if (store.HasProperty(122) || store.HasProperty(123) || store.HasProperty(124) || store.HasProperty(121))
                return Get(DroidPartId);

            if (store.HasProperty(127) || store.HasProperty(128) || store.HasProperty(129))
                return Get(IncubationSampleId);

            if (store.HasProperty(130))
                return Get(SchematicId);

            if (store.HasProperty(106) || store.HasProperty(108))
                return Get(MealId);

            if (store.HasProperty(15))
                return Get(ConsumableId);

            if (family == ItemFamily.Essence)
                return EnhancementProperties.Any(store.HasProperty) ? Get(EnhancementId) : Get(ComponentId);

            return Custom;
        }

        /// <summary>Stat groups a role's selection reveals beyond the family's own primary groups.</summary>
        public static IReadOnlyList<ItemStatGroup> GroupsUnlockedBy(string roleId) => roleId switch
        {
            MealId => new[] { ItemStatGroup.Bonuses, ItemStatGroup.Enhancements },
            DroidPartId => new[] { ItemStatGroup.Droid },
            IncubationSampleId => new[] { ItemStatGroup.Incubation },
            SchematicId => new[] { ItemStatGroup.Crafting },
            _ => Array.Empty<ItemStatGroup>()
        };

        private static List<ItemRole> Build() => new()
        {
            new ItemRole
            {
                Id = ConsumableId, DisplayName = "Consumable",
                Summary = "A single-use item that casts a spell effect (CastSpell) when used."
            },
            new ItemRole
            {
                Id = MealId, DisplayName = "Meal",
                Summary = "Food or drink; grants a temporary bonus and its own enhancement slots."
            },
            new ItemRole
            {
                Id = DeployedDeviceId, DisplayName = "Deployed Device",
                Summary = "Placed in the world rather than consumed or worn."
            },
            new ItemRole
            {
                Id = DroidPartId, DisplayName = "Droid Part",
                Summary = "Installs into a droid chassis, contributing a droid stat, instruction, or personality."
            },
            new ItemRole
            {
                Id = IncubationSampleId, DisplayName = "Incubation DNA",
                Summary = "DNA used to incubate a beast."
            },
            new ItemRole
            {
                Id = SchematicId, DisplayName = "Schematic",
                Summary = "Unlocks a craftable blueprint at the recorded level."
            },
            new ItemRole
            {
                Id = KeyItemId, DisplayName = "Key Item",
                Summary = "Tracks quest or unlock progress; carries no combat stats."
            },
            new ItemRole
            {
                Id = ComponentId, DisplayName = "Component",
                Summary = "A crafting input with no enhancement slot of its own."
            },
            new ItemRole
            {
                Id = EnhancementId, DisplayName = "Enhancement",
                Summary = "Slots into another item's enhancement socket."
            },
            new ItemRole
            {
                Id = CreatureItemId, DisplayName = "Creature Item",
                Summary = "A creature weapon or skin; its stats are NPC-facing."
            },
            new ItemRole
            {
                Id = CustomId, DisplayName = "Custom", AllowsVariables = true,
                Summary = "No recognized behavior; every property is exposed for direct editing."
            }
        };
    }
}
