namespace SWLOR.NWN.API.Constants
{
    /// <summary>
    /// Constants for test project names to be used with InternalsVisibleTo attributes.
    /// This allows for centralized management of test project names and makes them easily maintainable.
    /// </summary>
    internal static class TestProjectNames
    {
        // Component Test Projects
        public const string ComponentAbility = "SWLOR.Test.Component.Ability";
        public const string ComponentAdmin = "SWLOR.Test.Component.Admin";
        public const string ComponentAI = "SWLOR.Test.Component.AI";
        public const string ComponentAssociate = "SWLOR.Test.Component.Associate";
        public const string ComponentCharacter = "SWLOR.Test.Component.Character";
        public const string ComponentCombat = "SWLOR.Test.Component.Combat";
        public const string ComponentCommunication = "SWLOR.Test.Component.Communication";
        public const string ComponentCrafting = "SWLOR.Test.Component.Crafting";
        public const string ComponentInventory = "SWLOR.Test.Component.Inventory";
        public const string ComponentMarket = "SWLOR.Test.Component.Market";
        public const string ComponentMigration = "SWLOR.Test.Component.Migration";
        public const string ComponentPerk = "SWLOR.Test.Component.Perk";
        public const string ComponentProperties = "SWLOR.Test.Component.Properties";
        public const string ComponentQuest = "SWLOR.Test.Component.Quest";
        public const string ComponentSkill = "SWLOR.Test.Component.Skill";
        public const string ComponentSpace = "SWLOR.Test.Component.Space";
        public const string ComponentStatusEffect = "SWLOR.Test.Component.StatusEffect";
        public const string ComponentWorld = "SWLOR.Test.Component.World";

        // Shared Test Projects
        public const string SharedAbstractions = "SWLOR.Test.Shared.Abstractions";
        public const string SharedCaching = "SWLOR.Test.Shared.Caching";
        public const string SharedCore = "SWLOR.Test.Shared.Core";
        public const string SharedDomain = "SWLOR.Test.Shared.Domain";
        public const string SharedEvents = "SWLOR.Test.Shared.Events";
        public const string SharedUI = "SWLOR.Test.Shared.UI";

        // Integration Test Project
        public const string Integration = "SWLOR.Test.Integration";

        /// <summary>
        /// Gets all test project names as an array for easy iteration.
        /// </summary>
        public static readonly string[] All = new[]
        {
            // Component Test Projects
            ComponentAbility,
            ComponentAdmin,
            ComponentAI,
            ComponentAssociate,
            ComponentCharacter,
            ComponentCombat,
            ComponentCommunication,
            ComponentCrafting,
            ComponentInventory,
            ComponentMarket,
            ComponentMigration,
            ComponentPerk,
            ComponentProperties,
            ComponentQuest,
            ComponentSkill,
            ComponentSpace,
            ComponentStatusEffect,
            ComponentWorld,

            // Shared Test Projects
            SharedAbstractions,
            SharedCaching,
            SharedCore,
            SharedDomain,
            SharedEvents,
            SharedUI,

            // Integration Test Project
            Integration
        };
    }
}
