namespace SWLOR.Game.Server.Service.CurrencyService
{
    public enum CurrencyType
    {
        [Currency("Invalid", "", "")]
        Invalid = 0,
        [Currency("Rebuild Tokens", "cur_rebuild", "Consumed to fully respec your character's build.")]
        RebuildToken = 1,
        [Currency("Perk Refund Tokens", "cur_perkrfnd", "Consumed to refund a single purchased perk.")]
        PerkRefundToken = 2,
        [Currency("Stat Refund Tokens", "cur_statrfnd", "Consumed to reallocate a single spent stat point.")]
        StatRefundToken = 3,
        [Currency("Kyber Tokens", "cur_kyber", "Consumed at a Lightsaber Workbench to construct a new saber.")]
        KyberToken = 4,
        [Currency("Language Tokens", "cur_language", "Consumed to instantly master one language of your choice.")]
        LanguageToken = 5,
        [Currency("Republic Military Tokens", "iit_coin_001", "Earned from Republic Military agents. Spent at their requisitions.")]
        RepublicMilitaryToken = 6,
        [Currency("Czerka Tokens", "iit_coin_002", "Earned from Czerka Corporation agents. Spent at their requisitions.")]
        CzerkaToken = 7,
        [Currency("Smuggler Tokens", "iit_coin_003", "Earned from Smugglers' agents. Spent at their requisitions.")]
        SmugglerToken = 8,
        [Currency("Hutt Cartel Tokens", "iit_coin_004", "Earned from Hutt Cartel agents. Spent at their requisitions.")]
        HuttCartelToken = 9,
        [Currency("Mandalorian Tokens", "iit_coin_005", "Earned from Mandalorian agents. Spent at their requisitions.")]
        MandalorianToken = 10,
        [Currency("Sith Military Tokens", "iit_coin_006", "Earned from Sith Military agents. Spent at their requisitions.")]
        SithMilitaryToken = 11,
    }

    public class CurrencyAttribute : Attribute
    {
        public string Name { get; set; }
        public string IconResref { get; set; }
        public string Description { get; set; }

        public CurrencyAttribute(string name, string iconResref, string description)
        {
            Name = name;
            IconResref = iconResref;
            Description = description;
        }
    }
}
