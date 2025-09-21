using System;

namespace SWLOR.Game.Server.Service.CurrencyService
{
    public enum CurrencyType
    {
        [Currency("Invalid")]
        Invalid = 0,
        [Currency("Rebuild Tokens")]
        RebuildToken = 1,
        [Currency("Perk Refund Tokens")]
        PerkRefundToken = 2,
        [Currency("Stat Refund Tokens")]
        StatRefundToken = 3,
    }

    public class CurrencyAttribute : Attribute
    {
        public string Name { get; set; }

        public CurrencyAttribute(string name)
        {
            Name = name;
        }
    }
}
