using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;

using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Core.Log;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class BeastEggItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            BeastEgg();

            return _builder.Build();
        }

        private void BeastEgg()
        {
            _builder.Create(BeastMastery.BeastEggResref)
                .Delay(4f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, index) =>
                {
                    var tame = Perk.GetPerkLevel(user, PerkType.Tame);

                    if (tame <= 0)
                    {
                        return "Perk 'Tame' level 1 is required to use a beast egg.";
                    }

                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);

                    if (dbPlayer == null)
                    {
                        return "Only players may use this item.";
                    }

                    if (!string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId))
                    {
                        return "You already have a beast.";
                    }

                    var maxBeasts = 1 + Perk.GetPerkLevel(user, PerkType.Stabling);
                    var dbQuery = new DBQuery<Beast>()
                        .AddFieldSearch(nameof(Beast.OwnerPlayerId), playerId, false);
                    var beastCount = (int)DB.SearchCount(dbQuery);
                    if (beastCount >= maxBeasts)
                    {
                        return $"You have already tamed the maximum number of beasts your perks support.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);

                    var type = BeastType.Invalid;
                    var attackPurity = 0;
                    var accuracyPurity = 0;
                    var evasionPurity = 0;
                    var learningPurity = 0;
                    var physicalDefensePurity = 0;
                    var forceDefensePurity = 0;
                    var iceDefensePurity = 0;
                    var fireDefensePurity = 0;
                    var poisonDefensePurity = 0;
                    var electricDefensePurity = 0;
                    var fortitudePurity = 0;
                    var reflexPurity = 0;
                    var willPurity = 0;
                    var xpPenalty = 0;

                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        // Value is divided by 10 because the item property Ids scale from 0-1000 (represents 0.0% to 100.0%)
                        var costId = GetItemPropertyCostTableValue(ip) / 10; 
                        var subType = GetItemPropertySubType(ip);
                        switch (GetItemPropertyType(ip))
                        {
                            case ItemPropertyType.DNAType:
                                type = (BeastType)subType;
                                break;
                            case ItemPropertyType.Incubation:
                            {
                                switch ((IncubationStatType)subType)
                                {
                                    case IncubationStatType.AttackPurity:
                                        attackPurity = costId;
                                        break;
                                    case IncubationStatType.AccuracyPurity:
                                        accuracyPurity = costId;
                                        break;
                                    case IncubationStatType.EvasionPurity:
                                        evasionPurity = costId;
                                        break;
                                    case IncubationStatType.LearningPurity:
                                        learningPurity = costId;
                                        break;
                                    case IncubationStatType.PhysicalDefensePurity:
                                        physicalDefensePurity = costId;
                                        break;
                                    case IncubationStatType.ForceDefensePurity:
                                        forceDefensePurity = costId;
                                        break;
                                    case IncubationStatType.FireDefensePurity:
                                        fireDefensePurity = costId;
                                        break;
                                    case IncubationStatType.PoisonDefensePurity:
                                        poisonDefensePurity = costId;
                                        break;
                                    case IncubationStatType.ElectricalDefensePurity:
                                        electricDefensePurity = costId;
                                        break;
                                    case IncubationStatType.IceDefensePurity:
                                        iceDefensePurity = costId;
                                        break;
                                    case IncubationStatType.FortitudePurity:
                                        fortitudePurity = costId;
                                        break;
                                    case IncubationStatType.ReflexPurity:
                                        reflexPurity = costId;
                                        break;
                                    case IncubationStatType.WillPurity:
                                        willPurity = costId;
                                        break;
                                    case IncubationStatType.XPPenalty:
                                        xpPenalty = costId;
                                        break;
                                    }

                                break;
                            }
                        }
                    }

                    if (type == BeastType.Invalid)
                    {
                        SendMessageToPC(user, $"Unable to use beast egg. Beast Id is invalid. Report to an admin.");
                        Log.Write(LogGroup.Incubation, $"Player '{GetName(user)}' ({GetObjectUUID(user)}) failed to use beast egg item '{GetName(item)}' because its type is invalid.");
                        return;
                    }

                    var (likedFood, hatedFood) = BeastMastery.GetLikedAndHatedFood();
                    var beastDetail = BeastMastery.GetBeastDetail(type);

                    var dbBeast = new Beast
                    {
                        Name = beastDetail.Name,
                        OwnerPlayerId = playerId,
                        Level = 1,
                        UnallocatedSP = 1,
                        IsDead = false,
                        Type = type,
                        FavoriteFood = likedFood,
                        HatedFood = hatedFood,

                        AttackPurity = attackPurity,
                        AccuracyPurity = accuracyPurity,
                        EvasionPurity = evasionPurity,
                        LearningPurity = learningPurity,

                        XPPenaltyPercent = xpPenalty,

                        DefensePurities = new Dictionary<CombatDamageType, int>
                        {
                            { CombatDamageType.Physical, physicalDefensePurity },
                            { CombatDamageType.Force, forceDefensePurity },
                            { CombatDamageType.Fire, fireDefensePurity },
                            { CombatDamageType.Ice, iceDefensePurity },
                            { CombatDamageType.Poison, poisonDefensePurity },
                            { CombatDamageType.Electrical, electricDefensePurity },
                        },

                        SavingThrowPurities = new Dictionary<SavingThrow, int>
                        {
                            { SavingThrow.Fortitude, fortitudePurity},
                            { SavingThrow.Will, willPurity},
                            { SavingThrow.Reflex, reflexPurity},
                        }
                    };

                    DB.Set(dbBeast);

                    dbPlayer.ActiveBeastId = dbBeast.Id;
                    DB.Set(dbPlayer);

                    DestroyObject(item);
                });
        }
    }
}
