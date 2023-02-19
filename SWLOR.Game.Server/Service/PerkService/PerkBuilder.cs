using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkBuilder
    {
        private readonly Dictionary<PerkType, PerkDetail> _perks = new Dictionary<PerkType, PerkDetail>();
        private PerkDetail _activePerk;
        private PerkLevel _activeLevel;

        /// <summary>
        /// Creates a new perk.
        /// </summary>
        /// <param name="category">The category under which this perk is grouped.</param>
        /// <param name="type">The type of perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder Create(PerkCategoryType category, PerkType type)
        {
            _activeLevel = null;

            _activePerk = new PerkDetail
            {
                Category = category,
                Type = type,
                IsActive = true
            };
            _perks[type] = _activePerk;

            return this;
        }

        /// <summary>
        /// Sets the name of a perk which will be displayed to the player.
        /// </summary>
        /// <param name="name">The name of the perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder Name(string name)
        {
            _activePerk.Name = name;
            return this;
        }

        /// <summary>
        /// Sets the description of the perk or the perk level.
        /// </summary>
        /// <param name="description">The description to set.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder Description(string description)
        {
            if(_activeLevel == null)
            {
                _activePerk.Description = description;
            }
            else
            {
                _activeLevel.Description = description;
            }

            return this;
        }

        /// <summary>
        /// Deactivates the perk which will prevent players from purchasing and using it.
        /// </summary>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder Inactive()
        {
            _activePerk.IsActive = false;
            return this;
        }

        /// <summary>
        /// Creates a new perk level on the active perk we're building.
        /// </summary>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder AddPerkLevel()
        {
            var level = _activePerk.PerkLevels.Count + 1;
            _activeLevel = new PerkLevel();
            _activePerk.PerkLevels[level] = _activeLevel;

            return this;
        }

        /// <summary>
        /// Sets the amount of SP it costs to purchase this perk level.
        /// </summary>
        /// <param name="price">The price to purchase this perk level.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder Price(int price)
        {
            _activeLevel.Price = price;
            return this;
        }

        /// <summary>
        /// Sets the number of droid AI slots needed to equip a droid with this perk.
        /// If unspecified, the perk will be unavailable to droids.
        /// </summary>
        /// <param name="aiSlots">The amount of AI slots needed to equip a droid with this perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder DroidAISlots(int aiSlots)
        {
            _activeLevel.DroidAISlots = aiSlots;
            return this;
        }

        /// <summary>
        /// Sets the group associated with this perk. This determines which window the perk shows up in.
        /// For example, if you set it to Beast then the perk will only show up in the Beast perk menu.
        /// If this is unset, it defaults to a player perk.
        /// </summary>
        /// <param name="groupType">The type of group to assign</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder GroupType(PerkGroupType groupType)
        {
            _activePerk.GroupType = groupType;
            return this;
        }

        /// <summary>
        /// Adds a feat to grant to the player when the perk is purchased.
        /// </summary>
        /// <param name="feat">The feat to grant</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder GrantsFeat(FeatType feat)
        {
            _activeLevel.GrantedFeats.Add(feat);
            return this;
        }

        /// <summary>
        /// Adds a skill requirement to purchase and use the perk.
        /// </summary>
        /// <param name="skill">The skill to require</param>
        /// <param name="requiredRank">The number of ranks to require</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder RequirementSkill(SkillType skill, int requiredRank)
        {
            var requirement = new PerkRequirementSkill(skill, requiredRank);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds a quest requirement to purchase and use the perk.
        /// </summary>
        /// <param name="questId">The quest Id to require.</param>
        /// <returns>A perk builder with the configured options.</returns>
        public PerkBuilder RequirementQuest(string questId)
        {
            var requirement = new PerkRequirementQuest(questId);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds a character type requirement to purchase and use the perk.
        /// </summary>
        /// <param name="characterType"></param>
        /// <returns></returns>
        public PerkBuilder RequirementCharacterType(CharacterType characterType)
        {
            var requirement = new PerkRequirementCharacterType(characterType);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds an unlock requirement to purchase the perk.
        /// </summary>
        /// <returns>A perk builder with the configured options.</returns>
        public PerkBuilder RequirementUnlocked()
        {
            var requirement = new PerkRequirementUnlock(_activePerk.Type);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds a requirement that the player must have leveled a specific other perk.
        /// </summary>
        /// <param name="mustHavePerkType">The type of perk the player must have.</param>
        /// <param name="mustHavePerkLevel">Optionally, the level of the perk required.</param>
        /// <returns>A perk builder with the configured options.</returns>
        public PerkBuilder RequirementMustHavePerk(PerkType mustHavePerkType, int mustHavePerkLevel = 0)
        {
            var requirement = new PerkRequirementMustHavePerk(mustHavePerkType, mustHavePerkLevel);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds a requirement that the player cannot have a specific other perk.
        /// </summary>
        /// <param name="cannotHavePerkType">The type of perk the player cannot have.</param>
        /// <returns>A perk builder with the configured options.</returns>
        public PerkBuilder RequirementCannotHavePerk(PerkType cannotHavePerkType)
        {
            var requirement = new PerkRequirementCannotHavePerk(cannotHavePerkType);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds a requirement that the beast must meet a level requirement.
        /// </summary>
        /// <param name="level">The level to require</param>
        /// <returns>A perk builder with the configured options.</returns>
        public PerkBuilder RequirementBeastLevel(int level)
        {
            var requirement = new PerkRequirementBeastLevel(level);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds a requirement that the beast must be of a certain role.
        /// </summary>
        /// <param name="role">The type of role to require</param>
        /// <returns>A perk builder with the configured options.</returns>
        public PerkBuilder RequirementBeastRole(BeastRoleType role)
        {
            var requirement = new PerkRequirementBeastRole(role);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds an action to run when an item is equipped and the player has this perk.
        /// </summary>
        /// <param name="equipAction">The action to run when an item is equipped.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder TriggerEquippedItem(PerkTriggerEquippedAction equipAction)
        {
            _activePerk.EquippedTriggers.Add(equipAction);
            return this;
        }

        /// <summary>
        /// Adds an action to run when an item is unequipped and the player has this perk.
        /// </summary>
        /// <param name="unequipAction">The action to run when an item is unequipped.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder TriggerUnequippedItem(PerkTriggerUnequippedAction unequipAction)
        {
            _activePerk.UnequippedTriggers.Add(unequipAction);
            return this;
        }

        /// <summary>
        /// Adds an action to run when this perk is purchased.
        /// </summary>
        /// <param name="purchaseAction">The action to run when this perk is purchased.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder TriggerPurchase(PerkTriggerPurchasedRefundedAction purchaseAction)
        {
            _activePerk.PurchasedTriggers.Add(purchaseAction);
            return this;
        }

        /// <summary>
        /// Adds an action to run when this perk is refunded.
        /// </summary>
        /// <param name="refundAction">The action to run when this perk is refunded.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder TriggerRefund(PerkTriggerPurchasedRefundedAction refundAction)
        {
            _activePerk.RefundedTriggers.Add(refundAction);
            return this;
        }

        /// <summary>
        /// Adds a requirement check for purchasing this perk. Check must pass otherwise the
        /// purchase action will fail.
        /// </summary>
        /// <param name="requirementAction">The action to run when a player attempts to purchase this perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder PurchaseRequirement(PerkPurchaseRequirementAction requirementAction)
        {
            _activePerk.PurchaseRequirement = requirementAction;
            return this;
        }

        /// <summary>
        /// Adds a requirement check for refunding this perk. Check must pass otherwise the
        /// refund action will fail.
        /// </summary>
        /// <param name="requirementAction">The action to run when a player attempts to refund this perk.</param>
        /// <returns>A perk builder with the configured options</returns>
        public PerkBuilder RefundRequirement(PerkRefundRequirementAction requirementAction)
        {
            _activePerk.RefundRequirement = requirementAction;
            return this;
        }

        /// <summary>
        /// Returns a built list of perks.
        /// </summary>
        /// <returns>A list of built perks.</returns>
        public Dictionary<PerkType, PerkDetail> Build()
        {
            // Determine the icon to display within the perk menus.
            // The first feat's icon will be used if found.
            // If not found, it will fall back to the 'default_perk' icon instead.
            foreach (var (_, detail) in _perks)
            {
                detail.IconResref = "default_perk";
                foreach (var (_, perkLevel) in detail.PerkLevels)
                {
                    var feat = perkLevel.GrantedFeats.FirstOrDefault();
                    if (feat == default)
                        continue;

                    var resref = Get2DAString("feat", "ICON", (int)feat);
                    if (!string.IsNullOrWhiteSpace(resref))
                    {
                        detail.IconResref = resref;
                        break;
                    }
                }
            }

            return _perks;
        }
    }
}
