using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class DNAExtractorItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            DNAExtractor();

            return _builder.Build();
        }

        private void DNAExtractor()
        {
            _builder.Create("dna_extractor_1", "dna_extractor_2", "dna_extractor_3", "dna_extractor_4", "dna_extractor_5")
                .Delay(10f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, index) =>
                {
                    var itemResref = GetResRef(item);
                    var targetResref = GetResRef(target);
                    var level = GetLocalInt(target, BeastMastery.BeastLevelVariable);
                    var perkLevel = Perk.GetPerkLevel(user, PerkType.DNAManipulation);
                    var maxLevel = perkLevel * 10;
                    var chargesRequired = 1 + level / 10;
                    var charges = GetItemCharges(item);
                    var requiredExtractorLevel = level / 10;
                    var extractorLevel = Convert.ToInt32(itemResref.Substring(itemResref.Length - 1, 1));
                    var beastTypeId = GetLocalInt(target, BeastMastery.BeastTypeVariable);

                    if (targetResref != BeastMastery.ExtractCorpseObjectResref)
                    {
                        return "You may only extract DNA from empty corpses.";
                    }

                    if (beastTypeId <= 0)
                    {
                        return "You cannot extract DNA from that corpse.";
                    }

                    if (!Enum.IsDefined(typeof(BeastType), beastTypeId))
                    {
                        return "You cannot extract DNA from that corpse.";
                    }

                    if (level > maxLevel)
                    {
                        return "Insufficient 'DNA Manipulation' perk level.";
                    }

                    if (charges < chargesRequired)
                    {
                        return $"Not enough charges remaining on your extractor. Required: {chargesRequired}. You have: {charges}.";
                    }

                    if (extractorLevel < requiredExtractorLevel)
                    {
                        return $"Insufficient extractor level. Required: {requiredExtractorLevel}.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, $"Your extraction target is no longer valid.");
                        return;
                    }

                    const int BaseStatChance = 5;
                    var level = GetLocalInt(target, BeastMastery.BeastLevelVariable);
                    var chargesRequired = 1 + level / 10;
                    var charges = GetItemCharges(item) - chargesRequired;
                    var beastType = (BeastType)GetLocalInt(target, BeastMastery.BeastTypeVariable);
                    var beastDetail = BeastMastery.GetBeastDetail(beastType);
                    var social = GetAbilityScore(user, AbilityType.Social) - 10;
                    if (social < 0)
                        social = 0;
                    var statChance = (int)(social * 0.75f) + BaseStatChance;
                    var itemResref = GetResRef(item);
                    var extractorLevel = Convert.ToInt32(itemResref.Substring(itemResref.Length - 1, 1));
                    var requiredExtractorLevel = level / 10;
                    var deltaBonus = (extractorLevel - requiredExtractorLevel) * 10;

                    SetItemCharges(item, charges);

                    var structureBonus = 0;
                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.StructureBonus)
                        {
                            structureBonus += GetItemPropertyCostTableValue(ip);
                        }
                    }

                    var possibleStats = new List<IncubationStatType>
                    {
                        IncubationStatType.AttackPurity,
                        IncubationStatType.AccuracyPurity,
                        IncubationStatType.EvasionPurity,
                        IncubationStatType.LearningPurity,
                        IncubationStatType.PhysicalDefensePurity,
                        IncubationStatType.ForceDefensePurity,
                        IncubationStatType.FireDefensePurity,
                        IncubationStatType.PoisonDefensePurity,
                        IncubationStatType.ElectricalDefensePurity,
                        IncubationStatType.IceDefensePurity,
                        IncubationStatType.FortitudePurity,
                        IncubationStatType.ReflexPurity,
                        IncubationStatType.WillPurity,
                        IncubationStatType.XPPenalty
                    };

                    var itemProperties = new List<ItemProperty>
                    {
                        ItemPropertyCustom(ItemPropertyType.DNAType, (int)beastType)
                    };

                    foreach (var possibleStat in possibleStats)
                    {
                        if (Random.D100(1) <= statChance)
                        {
                            const int Maximum = 200;
                            var minimum = 1;

                            if (possibleStat != IncubationStatType.XPPenalty)
                            {
                                minimum += structureBonus * 5;
                                if (minimum > 150)
                                    minimum = 150;
                            }

                            var amount = Random.Next(minimum, Maximum + deltaBonus) + 1;
                            var ip = ItemPropertyCustom(ItemPropertyType.Incubation, (int)possibleStat, amount);
                            itemProperties.Add(ip);
                        }
                    }

                    var dna = CreateItemOnObject(BeastMastery.DNAResref, user);

                    foreach (var ip in itemProperties)
                    {
                        BiowareXP2.IPSafeAddItemProperty(dna, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                    }

                    SetName(dna, $"Beast DNA: {beastDetail.Name}");
                    DestroyObject(target);
                    var body = GetLocalObject(target, Loot.CorpseBodyVariable);
                    AssignCommand(body, () => SetIsDestroyable());
                    DestroyObject(body);
                });
        }
    }
}
