using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service.ItemModService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ItemModDefinition
{
    public class KillerItemModDefinition: IItemModListDefinition
    {
        private readonly ItemModBuilder _builder = new ItemModBuilder();

        public Dictionary<string, ItemModDetail> BuildItemMods()
        {
            CreateMod("HUMAN_KILLER", "Human Killer", RacialType.Human);
            CreateMod("ANIMAL_KILLER", "Animal Killer", RacialType.Animal);
            CreateMod("BEAST_KILLER", "Beast Killer", RacialType.Beast);
            CreateMod("VERMIN_KILLER", "Vermin Killer", RacialType.Vermin);
            CreateMod("UNDEAD_KILLER", "Undead Killer", RacialType.Undead);
            CreateMod("ROBOT_KILLER", "Robot Killer", RacialType.Robot);
            CreateMod("BOTHAN_KILLER", "Bothan Killer", RacialType.Bothan);
            CreateMod("CHISS_KILLER", "Chiss Killer", RacialType.Chiss);
            CreateMod("ZABRAK_KILLER", "Zabrak Killer", RacialType.Zabrak);
            CreateMod("WOOKIEE_KILLER", "Wookiee Killer", RacialType.Wookiee);
            CreateMod("TWILEK_KILLER", "Twi'lek Killer", RacialType.Twilek);
            CreateMod("CYBORG_KILLER", "Cyborg Killer", RacialType.Cyborg);
            CreateMod("CATHAR_KILLER", "Cathar Killer", RacialType.Cathar);
            CreateMod("TRANDOSHAN_KILLER", "Trandoshan Killer", RacialType.Trandoshan);
            CreateMod("MIRIALAN_KILLER", "Mirialan Killer", RacialType.Mirialan);
            CreateMod("ECHANI_KILLER", "Echani Killer", RacialType.Echani);
            CreateMod("MONCALAMARI_KILLER", "Mon Calamari Killer", RacialType.MonCalamari);
            CreateMod("UGNAUGHT_KILLER", "Ugnaught Killer", RacialType.Ugnaught);

            return _builder.Build();
        }

        private void CreateMod(string key, string name, RacialType racialType)
        {
            _builder.Create(key)
                .Name(name)
                .ApplyAction((user, mod, item) =>
                {
                    var amount = 1;

                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.AttackBonusVsRacialGroup)
                        {
                            var existingRacialType = (RacialType)GetItemPropertySubType(ip);
                            if (existingRacialType == racialType)
                            {
                                var existingBonus = GetItemPropertyCostTableValue(ip);
                                amount += existingBonus;
                            }
                        }
                    }

                    var newIP = ItemPropertyAttackBonusVsRace(racialType, amount);
                    BiowareXP2.IPSafeAddItemProperty(item, newIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
                });
        }
    }
}
