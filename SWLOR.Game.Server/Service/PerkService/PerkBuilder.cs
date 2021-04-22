using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;

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
            var requirement = new PerkSkillRequirement(skill, requiredRank);
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
            var requirement = new PerkQuestRequirement(questId);
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
            var requirement = new PerkCharacterTypeRequirement(characterType);
            _activeLevel.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds an unlock requirement to purchase the perk.
        /// </summary>
        /// <returns>A perk builder with the configured options.</returns>
        public PerkBuilder RequirementUnlocked()
        {
            var requirement = new PerkUnlockRequirement(_activePerk.Type);
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
        /// Returns a built list of perks.
        /// </summary>
        /// <returns>A list of built perks.</returns>
        public Dictionary<PerkType, PerkDetail> Build()
        {
            return _perks;
        }
    }
}
