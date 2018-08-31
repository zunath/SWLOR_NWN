using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.Placeable.StructureSystem.ConstructionSite
{
    public class OnAttacked : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IStructureService _structure;
        private readonly IColorTokenService _color;
        private readonly IItemService _item;
        private readonly IDurabilityService _durability;
        private readonly IPlayerService _player;

        public OnAttacked(INWScript script,
            ISkillService skill,
            IPerkService perk,
            IRandomService random,
            IStructureService structure,
            IColorTokenService color,
            IItemService item,
            IDurabilityService durability,
            IPlayerService player)
        {
            _ = script;
            _skill = skill;
            _perk = perk;
            _random = random;
            _structure = structure;
            _color = color;
            _item = item;
            _durability = durability;
            _player = player;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable oSite = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastAttacker(oSite.Object));
            int constructionSiteID = _structure.GetConstructionSiteID(oSite);

            if (constructionSiteID <= 0)
            {
                oPC.FloatingText("You must select a blueprint before you can build.");
                oPC.ClearAllActions();
                return true;
            }

            NWItem weapon = NWItem.Wrap(_.GetLastWeaponUsed(oPC.Object));
            int weaponType = weapon.BaseItemType;

            if (weaponType != NWScript.BASE_ITEM_LIGHTHAMMER)
            {
                oPC.FloatingText("A hammer must be equipped to build this structure.");
                oPC.ClearAllActions();
                return true;
            }

            // Offhand weapons don't contribute to building.
            if (weapon.Equals(oPC.LeftHand))
            {
                return true;
            }

            if (!_structure.IsConstructionSiteValid(oSite))
            {
                oPC.FloatingText("Construction site is invalid. Please click the construction site to find out more.");
                oPC.ClearAllActions();
                return true;
            }


            Data.Entities.ConstructionSite entity = _structure.GetConstructionSiteByID(constructionSiteID);


            if (weapon.CraftTierLevel < entity.StructureBlueprint.CraftTierLevel)
            {
                oPC.FloatingText("Your hammer cannot be used with this blueprint. (Required Tool Level: " + entity.StructureBlueprint.CraftTierLevel + ")");
                oPC.ClearAllActions();
                return true;
            }

            int rank = _skill.GetPCSkill(oPC, SkillType.Construction).Rank;
            int mangleChance = CalculateMangleChance(oPC, entity.StructureBlueprint.Level, rank);
            bool isMangle = _random.Random(100) + 1 <= mangleChance;
            bool foundResource = false;
            string updateMessage = "You lack the necessary resources...";

            int totalAmount = 0;
            foreach (ConstructionSiteComponent comp in entity.ConstructionSiteComponents)
            {
                if (comp.Quantity > 0 && !foundResource)
                {
                    NWItem item = NWItem.Wrap(_.GetItemPossessedBy(oPC.Object, comp.StructureComponent.Resref));
                    if (item.IsValid)
                    {
                        int reuseChance = isMangle ? 0 : _perk.GetPCPerkLevel(oPC, PerkType.ConservativeConstruction) * 2 + _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus;
                        if (_random.Random(100) + 1 <= reuseChance)
                        {
                            oPC.SendMessage("You conserve a resource...");
                        }
                        else
                        {
                            item.ReduceItemStack();
                        }

                        if (isMangle)
                        {
                            oPC.SendMessage(_color.Red("You mangle a resource due to your lack of skill..."));
                            return true;
                        }

                        string name = _item.GetNameByResref(comp.StructureComponent.Resref);
                        comp.Quantity--;
                        updateMessage = "You need " + comp.Quantity + " " + name + " to complete this project.";
                        foundResource = true;
                    }
                }
                totalAmount += comp.Quantity;
            }
            
            oPC.DelayCommand(() => oPC.SendMessage(updateMessage), 0.75f);
            
            if (totalAmount <= 0)
            {
                _structure.CompleteStructure(oSite);
            }
            else if (foundResource)
            {
                _structure.SaveChanges();
                _durability.RunItemDecay(oPC, weapon);

                if (entity.StructureBlueprint.GivesSkillXP)
                {
                    int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(100, 0, rank);
                    _skill.GiveSkillXP(oPC, SkillType.Construction, xp);
                }

                // Speedy Builder - Grants haste for 8 seconds
                int hasteChance = _perk.GetPCPerkLevel(oPC, PerkType.SpeedyBuilder) * 10;

                if (hasteChance > 0)
                {
                    hasteChance += _perk.GetPCPerkLevel(oPC, PerkType.Lucky) * 2 + oPC.EffectiveLuckBonus;
                }

                PlayerCharacter pcEntity = _player.GetPlayerEntity(oPC);
                if (pcEntity.BackgroundID == (int)BackgroundType.ConstructionBuilder)
                {
                    hasteChance += 10;
                }

                if (_random.Random(100) + 1 <= hasteChance)
                {
                    _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectHaste(), oPC.Object, 8.0f);
                }
            }
            else
            {
                oPC.ClearAllActions();
            }
            return true;
        }


        private int CalculateMangleChance(NWPlayer oPC, int level, int rank)
        {
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.MangleMaster) + (_perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus) / 2;
            int delta = level - rank;
            int perkReduction = perkLevel * 5;
            int mangleChance;
            if (delta <= 3) mangleChance = 0;
            else if (delta <= 4) mangleChance = 15;
            else if (delta <= 5) mangleChance = 25;
            else if (delta <= 6) mangleChance = 50;
            else mangleChance = 95;

            return mangleChance - perkReduction;
        }
    }
}
