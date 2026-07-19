namespace SWLOR.Game.Server.Service.CurrencyService
{
    public enum CurrencyType
    {
        [Currency("Invalid", "", "")]
        Invalid = 0,
        [Currency("Rebuild Tokens", "iit_gem_235", "Consumed to fully respec your character's build.")]
        RebuildToken = 1,
        [Currency("Perk Refund Tokens", "iit_book_245", "Consumed to refund a single purchased perk.")]
        PerkRefundToken = 2,
        [Currency("Stat Refund Tokens", "iit_book_247", "Consumed to reallocate a single spent stat point.")]
        StatRefundToken = 3,
        [Currency("Kyber Tokens", "iit_gem_185", "Consumed at a Lightsaber Workbench to construct a new saber.")]
        KyberToken = 4,
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
