using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Lockboxes are rare loot drops opened with the Espionage skill's Slicing perk line. Each tier
    /// requires the matching Slicing perk rank. A successful d100 roll destroys the box, grants
    /// Espionage XP, and spawns loot from the matching ESPIONAGE_LOCKBOX_# table (see
    /// LockboxLootTableDefinition). A failed roll keeps the box and applies a short per-item retry
    /// lockout so the player can't spam attempts.
    /// </summary>
    public class LockboxItemDefinition: IItemListDefinition
    {
        private const string RetryAfterVariable = "LOCKBOX_RETRY_AFTER";
        private const int RetryLockoutSeconds = 30;

        // Success formula: d100() <= BaseSuccessChance + (Lockpicking stat + PER modifier) * StatScalingMultiplier - tier * TierPenaltyPerTier,
        // clamped to [MinSuccessChance, MaxSuccessChance].
        private const int BaseSuccessChance = 50;
        private const int StatScalingMultiplier = 2;
        private const int TierPenaltyPerTier = 10;
        private const int MinSuccessChance = 5;
        private const int MaxSuccessChance = 95;

        // Espionage skill rank required to unlock each Slicing tier (mirrors the RequirementSkill values
        // on PerkType.Slicing in EspionagePerkDefinition). Used to scale XP off a level-vs-rank delta,
        // the same approach Fishing/Gathering use for their skill-up grants.
        private static readonly int[] TierSkillRequirement = { 8, 22, 30, 42, 48 };

        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Lockbox("lockbox_t1", 1);
            Lockbox("lockbox_t2", 2);
            Lockbox("lockbox_t3", 3);
            Lockbox("lockbox_t4", 4);
            Lockbox("lockbox_t5", 5);

            return _builder.Build();
        }

        private void Lockbox(string resref, int tier)
        {
            _builder.Create(resref)
                .Delay(2f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (Perk.GetPerkLevel(user, PerkType.Slicing) < tier)
                    {
                        return "You lack the Slicing expertise to crack this lockbox.";
                    }

                    if (GetLocalInt(item, RetryAfterVariable) > (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    {
                        return "The lock is still jammed from your last attempt. Wait a moment before trying again.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var successChance = CalculateSuccessChance(user, tier);

                    if (d100() <= successChance)
                    {
                        if (GetIsPC(user) && !GetIsDM(user))
                        {
                            GrantSlicingXP(user, tier);
                        }

                        var lootTable = Loot.GetLootTableByName($"ESPIONAGE_LOCKBOX_{tier}");
                        var loot = lootTable.GetRandomItem();
                        var quantity = Random.Next(loot.MaxQuantity) + 1;
                        CreateItemOnObject(loot.Resref, user, quantity);

                        DestroyObject(item);

                        SendMessageToPC(user, "You crack the lockbox open and recover its contents.");
                    }
                    else
                    {
                        SetLocalInt(item, RetryAfterVariable, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + RetryLockoutSeconds);
                        SendMessageToPC(user, "You fail to crack the lockbox. The lock jams - try again in a moment.");
                    }
                });
        }

        /// <summary>
        /// Combines the Lockpicking stat and Perception scaling into a single success chance, penalized
        /// by lock tier and clamped to a sane range. Kept in one method per the single-formula rule.
        /// </summary>
        private static int CalculateSuccessChance(uint user, int tier)
        {
            var lockpicking = Stat.GetStatAdjustment(user, StatType.Lockpicking);
            var perceptionModifier = GetAbilityModifier(AbilityType.Perception, user);
            var chance = BaseSuccessChance + (lockpicking + perceptionModifier) * StatScalingMultiplier - tier * TierPenaltyPerTier;

            return Math.Clamp(chance, MinSuccessChance, MaxSuccessChance);
        }

        private static void GrantSlicingXP(uint user, int tier)
        {
            var playerId = GetObjectUUID(user);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbSkill = dbPlayer.Skills[SkillType.Espionage];
            var delta = TierSkillRequirement[tier - 1] - dbSkill.Rank;
            var xp = Skill.GetDeltaXP(delta);

            Skill.GiveSkillXP(user, SkillType.Espionage, xp, false, false);
        }
    }
}
