using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Delegates;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Contracts
{
    /// <summary>
    /// Interface for building perk definitions.
    /// </summary>
    public interface IPerkBuilder
    {
        /// <summary>
        /// Creates a new perk.
        /// </summary>
        /// <param name="category">The category under which this perk is grouped.</param>
        /// <param name="type">The type of perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder Create(PerkCategoryType category, PerkType type);

        /// <summary>
        /// Sets the name of a perk which will be displayed to the player.
        /// </summary>
        /// <param name="name">The name of the perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder Name(string name);

        /// <summary>
        /// Sets the description of the perk or the perk level.
        /// </summary>
        /// <param name="description">The description to set.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder Description(string description);

        /// <summary>
        /// Deactivates the perk which will prevent players from purchasing and using it.
        /// </summary>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder Inactive();

        /// <summary>
        /// Creates a new perk level on the active perk we're building.
        /// </summary>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder AddPerkLevel();

        /// <summary>
        /// Sets the amount of SP it costs to purchase this perk level.
        /// </summary>
        /// <param name="price">The price to purchase this perk level.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder Price(int price);

        /// <summary>
        /// Sets the number of droid AI slots needed to equip a droid with this perk.
        /// If unspecified, the perk will be unavailable to droids.
        /// </summary>
        /// <param name="aiSlots">The amount of AI slots needed to equip a droid with this perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder DroidAISlots(int aiSlots);

        /// <summary>
        /// Sets the group associated with this perk. This determines which window the perk shows up in.
        /// For example, if you set it to Beast then the perk will only show up in the Beast perk menu.
        /// If this is unset, it defaults to a player perk.
        /// </summary>
        /// <param name="groupType">The type of group to assign</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder GroupType(PerkGroupType groupType);

        /// <summary>
        /// Adds a feat to grant to the player when the perk is purchased.
        /// </summary>
        /// <param name="feat">The feat to grant</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder GrantsFeat(FeatType feat);

        /// <summary>
        /// Adds a skill requirement to purchase and use the perk.
        /// </summary>
        /// <param name="skill">The skill to require</param>
        /// <param name="requiredRank">The number of ranks to require</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder RequirementSkill(SkillType skill, int requiredRank);

        /// <summary>
        /// Adds a quest requirement to purchase and use the perk.
        /// </summary>
        /// <param name="questId">The quest Id to require.</param>
        /// <returns>A perk builder with the configured options.</returns>
        IPerkBuilder RequirementQuest(string questId);

        /// <summary>
        /// Adds a character type requirement to purchase and use the perk.
        /// </summary>
        /// <param name="characterType"></param>
        /// <returns></returns>
        IPerkBuilder RequirementCharacterType(CharacterType characterType);

        /// <summary>
        /// Adds an unlock requirement to purchase the perk.
        /// </summary>
        /// <returns>A perk builder with the configured options.</returns>
        IPerkBuilder RequirementUnlocked();

        /// <summary>
        /// Adds a requirement that the player must have leveled a specific other perk.
        /// </summary>
        /// <param name="mustHavePerkType">The type of perk the player must have.</param>
        /// <param name="mustHavePerkLevel">Optionally, the level of the perk required.</param>
        /// <returns>A perk builder with the configured options.</returns>
        IPerkBuilder RequirementMustHavePerk(PerkType mustHavePerkType, int mustHavePerkLevel = 0);

        /// <summary>
        /// Adds a requirement that the player cannot have a specific other perk.
        /// </summary>
        /// <param name="cannotHavePerkType">The type of perk the player cannot have.</param>
        /// <returns>A perk builder with the configured options.</returns>
        IPerkBuilder RequirementCannotHavePerk(PerkType cannotHavePerkType);

        /// <summary>
        /// Adds a requirement that the beast must meet a level requirement.
        /// </summary>
        /// <param name="level">The level to require</param>
        /// <returns>A perk builder with the configured options.</returns>
        IPerkBuilder RequirementBeastLevel(int level);

        /// <summary>
        /// Adds a requirement that the beast must be of a certain role.
        /// </summary>
        /// <param name="role">The type of role to require</param>
        /// <returns>A perk builder with the configured options.</returns>
        IPerkBuilder RequirementBeastRole(BeastRoleType role);

        /// <summary>
        /// Adds an action to run when an item is equipped and the player has this perk.
        /// </summary>
        /// <param name="equipAction">The action to run when an item is equipped.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder TriggerEquippedItem(PerkTriggerEquippedAction equipAction);

        /// <summary>
        /// Adds an action to run when an item is unequipped and the player has this perk.
        /// </summary>
        /// <param name="unequipAction">The action to run when an item is unequipped.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder TriggerUnequippedItem(PerkTriggerUnequippedAction unequipAction);

        /// <summary>
        /// Adds an action to run when this perk is purchased.
        /// </summary>
        /// <param name="purchaseAction">The action to run when this perk is purchased.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder TriggerPurchase(PerkTriggerPurchasedRefundedAction purchaseAction);

        /// <summary>
        /// Adds an action to run when this perk is refunded.
        /// </summary>
        /// <param name="refundAction">The action to run when this perk is refunded.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder TriggerRefund(PerkTriggerPurchasedRefundedAction refundAction);

        /// <summary>
        /// Adds a requirement check for purchasing this perk. Check must pass otherwise the
        /// purchase action will fail.
        /// </summary>
        /// <param name="requirementAction">The action to run when a player attempts to purchase this perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder PurchaseRequirement(PerkPurchaseRequirementAction requirementAction);

        /// <summary>
        /// Adds a requirement check for refunding this perk. Check must pass otherwise the
        /// refund action will fail.
        /// </summary>
        /// <param name="requirementAction">The action to run when a player attempts to refund this perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        IPerkBuilder RefundRequirement(PerkRefundRequirementAction requirementAction);

        /// <summary>
        /// Returns a built list of perks.
        /// </summary>
        /// <returns>A list of built perks.</returns>
        Dictionary<PerkType, PerkDetail> Build();
    }
}
