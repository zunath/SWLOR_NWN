using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Reads a droid's controller item: its item properties and the constructed droid saved on it. It takes no service, so
    /// <see cref="CompanionLookup"/> (which Character's services take) and <see cref="DroidService"/> share it without a
    /// construction cycle (supply rule 2 of MaintainabilityRefactorDomainOrder.md).
    /// </summary>
    public sealed class DroidControllerReader
    {
        public const string DroidControlItemVariable = "ACTIVE_DROID_ITEM";
        public const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

        private readonly Dictionary<int, int> _levelsByTier = new()
        {
            [1] = 5,
            [2] = 15,
            [3] = 25,
            [4] = 35,
            [5] = 45
        };

        /// <summary>The level a droid of a tier has, 0 for a tier that has none.</summary>
        public int GetLevelOfTier(int tier) => _levelsByTier.GetValueOrDefault(tier);

        /// <summary>The controller item of a droid, OBJECT_INVALID when it has none.</summary>
        public uint GetControllerItem(uint droid)
        {
            return GetLocalObject(droid, DroidControlItemVariable);
        }

        /// <summary>
        /// Loads item property details from a droid's controller item.
        /// </summary>
        /// <param name="controller">The controller item to read from.</param>
        /// <returns>Droid item property details.</returns>
        public DroidItemPropertyDetails LoadDroidItemPropertyDetails(uint controller)
        {
            var details = new DroidItemPropertyDetails();
            for (var ip = GetFirstItemProperty(controller); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(controller))
            {
                var type = GetItemPropertyType(ip);

                if (type == ItemPropertyType.DroidStat)
                {
                    var subType = (DroidStatSubType)GetItemPropertySubType(ip);
                    var value = GetItemPropertyCostTableValue(ip);

                    switch (subType)
                    {
                        case DroidStatSubType.Tier:
                            details.Tier = Math.Clamp(value, 1, 5);
                            break;
                        case DroidStatSubType.AISlots:
                            details.AISlots += value;
                            break;
                        case DroidStatSubType.HP:
                            details.HP += value;
                            break;
                        case DroidStatSubType.STM:
                            details.STM += value;
                            break;
                        case DroidStatSubType.MGT:
                            details.MGT += value;
                            break;
                        case DroidStatSubType.PER:
                            details.PER += value;
                            break;
                        case DroidStatSubType.VIT:
                            details.VIT += value;
                            break;
                        case DroidStatSubType.WIL:
                            details.WIL += value;
                            break;
                        case DroidStatSubType.AGI:
                            details.AGI += value;
                            break;
                        case DroidStatSubType.SOC:
                            details.SOC += value;
                            break;
                        case DroidStatSubType.Armor:
                            AddSkillBonus(details.Skills, SkillType.Armor, value);
                            break;
                        case DroidStatSubType.ResistanceFire:
                            AddResistanceBonus(details.Resistances, ResistanceType.Fire, value);
                            break;
                        case DroidStatSubType.ResistancePoison:
                            AddResistanceBonus(details.Resistances, ResistanceType.Poison, value);
                            break;
                        case DroidStatSubType.ResistanceElectrical:
                            AddResistanceBonus(details.Resistances, ResistanceType.Electrical, value);
                            break;
                        case DroidStatSubType.ResistanceIce:
                            AddResistanceBonus(details.Resistances, ResistanceType.Ice, value);
                            break;
                        case DroidStatSubType.ResistanceMind:
                            AddResistanceBonus(details.Resistances, ResistanceType.Mind, value);
                            break;
                        case DroidStatSubType.ResistanceMobility:
                            AddResistanceBonus(details.Resistances, ResistanceType.Mobility, value);
                            break;
                        case DroidStatSubType.ResistanceTrauma:
                            AddResistanceBonus(details.Resistances, ResistanceType.Trauma, value);
                            break;
                        case DroidStatSubType.ResistanceDisruption:
                            AddResistanceBonus(details.Resistances, ResistanceType.Disruption, value);
                            break;
                        case DroidStatSubType.Vibroblade:
                            AddSkillBonus(details.Skills, SkillType.Vibroblade, value);
                            break;
                        case DroidStatSubType.Vibroknife:
                            AddSkillBonus(details.Skills, SkillType.Vibroknife, value);
                            break;
                        // Droids cannot use saber weapons. Items built before the skills were removed may still carry them.
                        case DroidStatSubType.Lightsaber:
                        case DroidStatSubType.Saberstaff:
                            break;
                        case DroidStatSubType.HeavyVibroblade:
                            AddSkillBonus(details.Skills, SkillType.HeavyVibroblade, value);
                            break;
                        case DroidStatSubType.Spear:
                            AddSkillBonus(details.Skills, SkillType.Spear, value);
                            break;
                        case DroidStatSubType.TwinBlade:
                            AddSkillBonus(details.Skills, SkillType.TwinBlade, value);
                            break;
                        case DroidStatSubType.Katar:
                            AddSkillBonus(details.Skills, SkillType.Katar, value);
                            break;
                        case DroidStatSubType.Staff:
                            AddSkillBonus(details.Skills, SkillType.Staff, value);
                            break;
                        case DroidStatSubType.Pistol:
                            AddSkillBonus(details.Skills, SkillType.Pistol, value);
                            break;
                        case DroidStatSubType.Rifle:
                            AddSkillBonus(details.Skills, SkillType.Rifle, value);
                            break;
                        case DroidStatSubType.Throwing:
                            AddSkillBonus(details.Skills, SkillType.Throwing, value);
                            break;
                        case DroidStatSubType.Unarmed:
                            AddSkillBonus(details.Skills, SkillType.Unarmed, value);
                            break;
                        case DroidStatSubType.TwinPistol:
                            AddSkillBonus(details.Skills, SkillType.TwinPistol, value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                else if (type == ItemPropertyType.DroidPersonality)
                {
                    var personalityType = (DroidPersonalityType)GetItemPropertySubType(ip);
                    details.PersonalityType = personalityType;
                }
            }


            details.Level = _levelsByTier[details.Tier];

            var constructedDroid = LoadConstructedDroid(controller);
            details.CustomName = constructedDroid.Name;
            details.Perks = DroidInstructions.SelectActive(constructedDroid.ActivePerks, details.Tier, details.AISlots)
                .ToDictionary(instruction => instruction.Perk, instruction => instruction.Level);

            return details;
        }

        public static void AddSkillBonus(IDictionary<SkillType, int> skills, SkillType skill, int value)
        {
            if (!skills.ContainsKey(skill))
                skills[skill] = value;
            else
                skills[skill] += value;
        }

        public static void AddResistanceBonus(IDictionary<ResistanceType, int> resistances, ResistanceType type, int value)
        {
            if (!resistances.ContainsKey(type))
                resistances[type] = value;
            else
                resistances[type] += value;
        }

        /// <summary>
        /// Loads constructed droid information stored as a local variable on the controller item.
        /// If this doesn't exist, a new object will be returned.
        /// </summary>
        /// <param name="controller">The controller item to read from.</param>
        /// <returns>A ConstructedDroid object.</returns>
        public ConstructedDroid LoadConstructedDroid(uint controller)
        {
            var constructedDroid = new ConstructedDroid();
            var serialized = GetLocalString(controller, ConstructedDroidVariable);
            if (!string.IsNullOrWhiteSpace(serialized))
            {
                constructedDroid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized) ?? new ConstructedDroid();
            }

            constructedDroid.LearnedPerks ??= new List<DroidPerk>();
            constructedDroid.ActivePerks ??= new List<DroidPerk>();
            constructedDroid.EquippedItems ??= new Dictionary<InventorySlot, string>();
            constructedDroid.Inventory ??= new Dictionary<string, string>();
            constructedDroid.AppearanceParts ??= new Dictionary<CreaturePart, int>();
            constructedDroid.TintOverrides ??= new Dictionary<string, int>();

            return constructedDroid;
        }
    }
}
